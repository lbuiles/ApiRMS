using HotChocolate;
using HotChocolate.Authorization;
using RmsErp.Api.Services.Push;

namespace RmsErp.Api.Queries.Sistema
{
    [ExtendObjectType("Query")]
    public class PushQuery
    {
        /// <summary>
        /// Llave pública VAPID para que el navegador cree la suscripción push.
        /// Null si el servidor no tiene push configurado (el front oculta la opción).
        /// </summary>
        [Authorize]
        public string? PushClavePublica([Service] PushOpciones op) => op.Configurado ? op.PublicKey : null;
    }
}
