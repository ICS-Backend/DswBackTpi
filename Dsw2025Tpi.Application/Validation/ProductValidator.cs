using Dsw2025Tpi.Application.Dtos;
using Dsw2025Tpi.Application.Exceptions;
using InvalidDataException = Dsw2025Tpi.Application.Exceptions.InvalidDataException;

namespace Dsw2025Tpi.Application.Validation
{
    public static class ProductValidator
    {
        /// <summary>
        /// Validación común para crear y modificar productos.
        /// </summary>
        public static void Validate(ProductModel.RequestProductModel request)
        {
            if (string.IsNullOrWhiteSpace(request.Sku))
                throw new BadRequestException("El SKU es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.InternalCode))
                throw new BadRequestException("El código interno es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new BadRequestException("El nombre es obligatorio.");

            if (string.IsNullOrWhiteSpace(request.Description))
                throw new BadRequestException("La descripción es obligatoria.");

            if (request.CurrentUnitPrice <= 0)
                throw new InvalidDataException("El precio debe ser un valor positivo.");

            if (request.StockQuantity < 0)
                throw new InvalidDataException("El stock no puede ser negativo.");
        }

        /// <summary>
        /// Validación para crear un producto: además de lo común, el stock inicial debe ser mayor a cero.
        /// (Al modificar se sigue permitiendo stock 0, por ejemplo un producto agotado.)
        /// </summary>
        public static void ValidateCreate(ProductModel.RequestProductModel request)
        {
            Validate(request);

            if (request.StockQuantity <= 0)
                throw new InvalidDataException("El stock inicial debe ser mayor a cero.");
        }
    }
}
