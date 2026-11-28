using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RmsErp.Api.Services.Push
{
    /// <summary>Llaves VAPID (sección "Push" de la configuración; en producción por variables Push__*).</summary>
    public class PushOpciones
    {
        public string? PublicKey  { get; set; }   // base64url, 65 bytes (punto sin comprimir)
        public string? PrivateKey { get; set; }   // base64url, 32 bytes
        public string  Subject    { get; set; } = "mailto:sistemas@rmscolombia.com";

        public bool Configurado => !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
    }

    public enum ResultadoPush { Enviado, SuscripcionVencida, Error }

    /// <summary>
    /// Envío de Web Push sin librerías externas: cifrado aes128gcm (RFC 8291) y firma VAPID (RFC 8292)
    /// con la criptografía de .NET.
    /// </summary>
    public class WebPushEmisor
    {
        private readonly PushOpciones _op;
        private readonly HttpClient _http;
        private readonly ILogger<WebPushEmisor> _log;

        public WebPushEmisor(PushOpciones op, IHttpClientFactory http, ILogger<WebPushEmisor> log)
        {
            _op = op;
            _http = http.CreateClient("push");
            _log = log;
        }

        public async Task<ResultadoPush> EnviarAsync(string endpoint, string p256dh, string auth, object contenido, CancellationToken ct = default)
        {
            if (!_op.Configurado) return ResultadoPush.Error;
            try
            {
                var cuerpo = Cifrar(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(contenido)), B64(p256dh), B64(auth));

                using var req = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = new ByteArrayContent(cuerpo) };
                req.Content.Headers.ContentType = new("application/octet-stream");
                req.Content.Headers.ContentEncoding.Add("aes128gcm");
                req.Headers.Add("TTL", "86400");          // el servicio la guarda hasta 1 día si el equipo está apagado
                req.Headers.Add("Urgency", "normal");
                req.Headers.TryAddWithoutValidation("Authorization", $"vapid t={FirmaVapid(endpoint)}, k={_op.PublicKey}");

                using var resp = await _http.SendAsync(req, ct);
                if (resp.IsSuccessStatusCode) return ResultadoPush.Enviado;
                if (resp.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) return ResultadoPush.SuscripcionVencida;

                _log.LogWarning("Push rechazado ({Codigo}) por {Host}: {Detalle}", (int)resp.StatusCode,
                    new Uri(endpoint).Host, await resp.Content.ReadAsStringAsync(ct));
                return ResultadoPush.Error;
            }
            catch (Exception ex)
            {
                _log.LogWarning("No se pudo enviar el push: {Mensaje}", ex.Message);
                return ResultadoPush.Error;
            }
        }

        // ── RFC 8291: cifrado del mensaje para el navegador ─────────────────────
        private static byte[] Cifrar(byte[] mensaje, byte[] uaPublica, byte[] authSecret)
        {
            using var receptor = ECDiffieHellman.Create(Parametros(uaPublica, null));
            using var efimera  = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            var asPublica = PuntoSinComprimir(efimera.ExportParameters(false));
            var ecdh = efimera.DeriveRawSecretAgreement(receptor.PublicKey);

            var prkKey = HMACSHA256.HashData(authSecret, ecdh);
            var ikm    = HMACSHA256.HashData(prkKey, Unir(Encoding.ASCII.GetBytes("WebPush: info\0"), uaPublica, asPublica, new byte[] { 1 }));
            var salt   = RandomNumberGenerator.GetBytes(16);
            var prk    = HMACSHA256.HashData(salt, ikm);
            var cek    = HMACSHA256.HashData(prk, Encoding.ASCII.GetBytes("Content-Encoding: aes128gcm\0\u0001"))[..16];
            var nonce  = HMACSHA256.HashData(prk, Encoding.ASCII.GetBytes("Content-Encoding: nonce\0\u0001"))[..12];

            var plano   = Unir(mensaje, new byte[] { 2 });   // delimitador del último (único) registro
            var cifrado = new byte[plano.Length];
            var tag     = new byte[16];
            using (var aes = new AesGcm(cek, 16)) aes.Encrypt(nonce, plano, cifrado, tag);

            // Encabezado: salt(16) | rs(4, big endian) | idlen(1) | keyid = llave pública efímera(65)
            var rs = new byte[] { 0, 0, 0x10, 0 };   // 4096
            return Unir(salt, rs, new byte[] { (byte)asPublica.Length }, asPublica, cifrado, tag);
        }

        // ── RFC 8292: JWT VAPID firmado con ES256 ───────────────────────────────
        private string FirmaVapid(string endpoint)
        {
            var uri = new Uri(endpoint);
            var encabezado = B64Url(JsonSerializer.SerializeToUtf8Bytes(new { typ = "JWT", alg = "ES256" }));
            var datos = B64Url(JsonSerializer.SerializeToUtf8Bytes(new
            {
                aud = $"{uri.Scheme}://{uri.Host}",
                exp = DateTimeOffset.UtcNow.AddHours(12).ToUnixTimeSeconds(),
                sub = _op.Subject
            }));
            using var ecdsa = ECDsa.Create(Parametros(B64(_op.PublicKey!), B64(_op.PrivateKey!)));
            var firma = ecdsa.SignData(Encoding.ASCII.GetBytes($"{encabezado}.{datos}"), HashAlgorithmName.SHA256); // r|s (64 bytes)
            return $"{encabezado}.{datos}.{B64Url(firma)}";
        }

        // ── Utilidades ──────────────────────────────────────────────────────────
        private static ECParameters Parametros(byte[] publica, byte[]? privada) => new()
        {
            Curve = ECCurve.NamedCurves.nistP256,
            Q = new ECPoint { X = publica[1..33], Y = publica[33..65] },
            D = privada
        };

        private static byte[] PuntoSinComprimir(ECParameters p) => Unir(new byte[] { 4 }, p.Q.X!, p.Q.Y!);

        private static byte[] Unir(params byte[][] partes)
        {
            var r = new byte[partes.Sum(p => p.Length)];
            var i = 0;
            foreach (var p in partes) { Buffer.BlockCopy(p, 0, r, i, p.Length); i += p.Length; }
            return r;
        }

        public static byte[] B64(string s)
        {
            s = s.Trim().Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }

        private static string B64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
