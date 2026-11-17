using System;

namespace Dsw2025Tpi.Application.Exceptions;

/// <summary>
/// Se lanza cuando una operación hace referencia a un cliente (CustomerId)
/// que no existe en el repositorio. Hereda de EntityNotFoundException para
/// que GlobalExceptionMiddleware la siga mapeando a 404 sin cambios.
/// </summary>
public class CustomerNotFoundException : EntityNotFoundException
{
    public CustomerNotFoundException(string message) : base(message) { }

    public CustomerNotFoundException(string message, Exception innerException)
        : base(message, innerException) { }
}
