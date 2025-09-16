using FluentValidation;

namespace ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.CreateProduct;

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Request)
            .NotNull().WithMessage("Product request is required");

        When(x => x.Request != null, () =>
        {
            RuleFor(x => x.Request.Name)
                .NotEmpty().WithMessage("Product name is required")
                .Length(2, 200).WithMessage("Product name must be between 2 and 200 characters");

            RuleFor(x => x.Request.Description)
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");

            RuleFor(x => x.Request.Price)
                .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.Request.DiscountPrice)
                .GreaterThanOrEqualTo(0).WithMessage("Discount price must be greater than or equal to 0")
                .When(x => x.Request.DiscountPrice.HasValue);

            RuleFor(x => x.Request.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stock quantity must be greater than or equal to 0");

            RuleFor(x => x.Request.CategoryId)
                .NotEmpty().WithMessage("Category ID is required");

            RuleFor(x => x.Request.PreOrderReleaseDate)
                .GreaterThan(DateTime.UtcNow).WithMessage("Pre-order release date must be in the future")
                .When(x => x.Request.IsPreOrder && x.Request.PreOrderReleaseDate.HasValue);

            // Business rule: If it's pre-order, release date should be provided
            RuleFor(x => x.Request.PreOrderReleaseDate)
                .NotNull().WithMessage("Pre-order release date is required for pre-order products")
                .When(x => x.Request.IsPreOrder);

            // Business rule: Discount price should be less than original price
            RuleFor(x => x.Request.DiscountPrice)
                .LessThan(x => x.Request.Price).WithMessage("Discount price must be less than original price")
                .When(x => x.Request.DiscountPrice.HasValue);
        });
    }
}
