using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.CreateProduct;
using ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.DeleteProduct;
using ProductAuthMicroservice.ProductService.Application.Features.Products.Commands.UpdateProduct;
using ProductAuthMicroservice.ProductService.Application.Features.Products.DTOs;
using ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProduct;
using ProductAuthMicroservice.ProductService.Application.Features.Products.Queries.GetProducts;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductAuthMicroservice.ProductService.API.Controllers;

/// <summary>
/// Controller for managing products
/// </summary>
[ApiController]
[Route("api/cms/products")]
[ApiExplorerSettings(GroupName = "v1")]
[Commons.Configurations.Tags("CMS", "CMS_Products")]
[SwaggerTag("This API is used for Product management in Product Service")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all products with pagination and filtering
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/products?page=1&amp;pageSize=10&amp;search=smartphone&amp;categoryId=null&amp;minPrice=100&amp;maxPrice=2000&amp;isPreOrder=false&amp;inStock=true&amp;sortBy=name&amp;isAscending=true
    ///     
    /// Query Parameters:
    /// - page: Page number (default: 1)
    /// - pageSize: Page size (default: 10, max: 100)
    /// - search: Search term for product name or description
    /// - categoryId: Filter by category ID
    /// - minPrice: Minimum price filter
    /// - maxPrice: Maximum price filter
    /// - isPreOrder: Filter by pre-order status
    /// - inStock: Filter by stock availability
    /// - minStock: Minimum stock quantity
    /// - maxStock: Maximum stock quantity
    /// - sortBy: Sort field (name, price, createdAt, updatedAt)
    /// - isAscending: Sort order (true for ascending)
    /// - status: Filter by status (Active, Inactive)
    /// </remarks>
    /// <param name="filter">Product filter parameters</param>
    /// <returns>Paginated list of products</returns>
    /// <response code="200">Products retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<ProductItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<ProductItem>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PaginationResult<ProductItem>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<ProductItem>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginationResult<ProductItem>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all products with pagination and filtering",
        Description = "Retrieve products with optional filtering, pagination, and sorting",
        OperationId = "GetProducts",
        Tags = new[] { "CMS", "CMS_Products" }
    )]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilter filter)
    {
        // Validate and sanitize filter
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (filter.Page < 1) filter.Page = 1;

        var query = new GetProductsQuery(filter);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/products/{id}
    ///     
    /// </remarks>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    /// <response code="200">Product retrieved successfully</response>
    /// <response code="404">Product not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get product by ID",
        Description = "Retrieve a specific product by its ID",
        OperationId = "GetProductById",
        Tags = new[] { "CMS", "CMS_Products" }
    )]
    public async Task<IActionResult> GetProductById(Guid id)
    {
        var query = new GetProductQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/products
    ///     {
    ///        "name": "iPhone 15 Pro",
    ///        "description": "Latest iPhone with advanced features",
    ///        "price": 999.99,
    ///        "discount_price": 899.99,
    ///        "stock_quantity": 100,
    ///        "category_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "is_pre_order": false
    ///     }
    ///     
    /// </remarks>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Category not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Result<ProductResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Create a new product",
        Description = "Create a new product with the provided information",
        OperationId = "CreateProduct",
        Tags = new[] { "CMS", "CMS_Products" }
    )]
    public async Task<IActionResult> CreateProduct([FromBody] ProductRequestDto request)
    {
        var command = new CreateProductCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return CreatedAtAction(
            nameof(GetProductById),
            new { id = result.Data!.Id },
            result);
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/products/{id}
    ///     {
    ///        "name": "iPhone 15 Pro Max",
    ///        "description": "Latest iPhone with advanced features and larger screen",
    ///        "price": 1199.99,
    ///        "discount_price": 1099.99,
    ///        "stock_quantity": 50,
    ///        "category_id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///        "is_pre_order": false
    ///     }
    ///     
    /// </remarks>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(Result<ProductResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update an existing product",
        Description = "Update a product with the provided information",
        OperationId = "UpdateProduct",
        Tags = new[] { "CMS", "CMS_Products" }
    )]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductRequestDto request)
    {
        var command = new UpdateProductCommand(id, request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/products/{id}
    ///     
    /// Note: This operation performs a soft delete.
    /// </remarks>
    /// <param name="id">Product ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Product deleted successfully</response>
    /// <response code="400">Cannot delete product (validation failed)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Product not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Delete a product",
        Description = "Soft delete a product",
        OperationId = "DeleteProduct",
        Tags = new[] { "CMS", "CMS_Products" }
    )]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }
}
