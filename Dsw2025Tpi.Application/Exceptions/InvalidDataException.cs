namespace Dsw2025Tpi.Application.Exceptions
{
    /// <summary>
    /// Datos numéricos inválidos de un producto (precio o stock).
    /// Hereda de BadRequestException, así el GlobalExceptionMiddleware la responde como 400.
    /// Ojo: existe también System.IO.InvalidDataException; en los archivos que la usen
    /// agregar: using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;
    /// </summary>
    public class InvalidDataException : BadRequestException
    {
        public InvalidDataException(string message) : base(message) { }

        public InvalidDataException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
