using System;

namespace Dsw2025Tpi.Application.Exceptions;

/// <summary>
/// Se lanza cuando se intenta tomar más stock del disponible para un producto.
/// Hereda de BadRequestException para que GlobalExceptionMiddleware la siga
/// mapeando a 400 sin cambios.
/// </summary>
public class InsufficientStockException : BadRequestException
{
    public InsufficientStockException(string message) : base(message) { }

    public InsufficientStockException(string message, Exception innerException)
        : base(message, innerException) { }
}
