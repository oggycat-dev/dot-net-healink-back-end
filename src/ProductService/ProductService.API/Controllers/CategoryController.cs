using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAuthMicroservice.Commons.Extensions;
using ProductAuthMicroservice.Commons.Models;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.CreateCategory;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.DeleteCategory;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.Commands.UpdateCategory;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.DTOs;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategories;
using ProductAuthMicroservice.ProductService.Application.Features.Categories.Queries.GetCategoryById;
using Swashbuckle.AspNetCore.Annotations;

namespace ProductAuthMicroservice.ProductService.API.Controllers;

/// <summary>
/// Controller for managing product categories
/// </summary>
[ApiController]
[Route("api/cms/categories")]
[ApiExplorerSettings(GroupName = "v1")]
[Commons.Configurations.Tags("CMS", "CMS_Categories")]
[SwaggerTag("This API is used for Category management in Product Service")]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all categories with pagination and filtering
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/categories?page=1&amp;pageSize=10&amp;search=electronics&amp;parentCategoryId=null&amp;includeSubCategories=true&amp;includeProductsCount=true&amp;sortBy=name&amp;isAscending=true
    ///     
    /// Query Parameters:
    /// - page: Page number (default: 1)
    /// - pageSize: Page size (default: 10, max: 100)
    /// - search: Search term for category name or description
    /// - parentCategoryId: Filter by parent category ID (null for root categories)
    /// - includeSubCategories: Include subcategories in response
    /// - includeProductsCount: Include products count for each category
    /// - sortBy: Sort field (name, createdAt, updatedAt)
    /// - isAscending: Sort order (true for ascending)
    /// - status: Filter by status (Active, Inactive)
    /// - rootCategoriesOnly: Get only root categories
    /// </remarks>
    /// <param name="filter">Category filter parameters</param>
    /// <returns>Paginated list of categories</returns>
    /// <response code="200">Categories retrieved successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginationResult<CategoryItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginationResult<CategoryItem>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(PaginationResult<CategoryItem>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginationResult<CategoryItem>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginationResult<CategoryItem>), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get all categories with pagination and filtering",
        Description = "Retrieve categories with optional filtering, pagination, and sorting",
        OperationId = "GetCategories",
        Tags = new[] { "CMS", "CMS_Categories" }
    )]
    public async Task<IActionResult> GetCategories([FromQuery] CategoryFilter filter)
    {
        // Validate and sanitize filter
        if (filter.PageSize > 100) filter.PageSize = 100;
        if (filter.PageSize < 1) filter.PageSize = 10;
        if (filter.Page < 1) filter.Page = 1;

        var query = new GetCategoriesQuery(filter);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     GET /api/categories/{id}?includeSubCategories=true&amp;includeProductsCount=true
    ///     
    /// </remarks>
    /// <param name="id">Category ID</param>
    /// <param name="includeSubCategories">Include subcategories</param>
    /// <param name="includeProductsCount">Include products count</param>
    /// <returns>Category details</returns>
    /// <response code="200">Category retrieved successfully</response>
    /// <response code="404">Category not found</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get category by ID",
        Description = "Retrieve a specific category by its ID",
        OperationId = "GetCategoryById",
        Tags = new[] { "CMS", "CMS_Categories" }
    )]
    public async Task<IActionResult> GetCategoryById(
        Guid id,
        [FromQuery] bool includeSubCategories = true,
        [FromQuery] bool includeProductsCount = true)
    {
        var query = new GetCategoryByIdQuery(id, includeSubCategories, includeProductsCount);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/categories
    ///     {
    ///        "name": "Electronics",
    ///        "description": "Electronic devices and accessories",
    ///        "parent_category_id": null,
    ///        "image_path": "/images/categories/electronics.jpg"
    ///     }
    ///     
    /// </remarks>
    /// <param name="request">Category creation request</param>
    /// <returns>Created category</returns>
    /// <response code="201">Category created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="409">Category name already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Result<CategoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Create a new category",
        Description = "Create a new product category",
        OperationId = "CreateCategory",
        Tags = new[] { "CMS", "CMS_Categories" }
    )]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryRequestDto request)
    {
        var command = new CreateCategoryCommand(request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return CreatedAtAction(
            nameof(GetCategoryById),
            new { id = result.Data!.Id },
            result);
    }

    /// <summary>
    /// Update an existing category
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     PUT /api/categories/{id}
    ///     {
    ///        "name": "Electronics &amp; Gadgets",
    ///        "description": "Electronic devices, gadgets and accessories",
    ///        "parent_category_id": null,
    ///        "image_path": "/images/categories/electronics-updated.jpg"
    ///     }
    ///     
    /// </remarks>
    /// <param name="id">Category ID</param>
    /// <param name="request">Category update request</param>
    /// <returns>Updated category</returns>
    /// <response code="200">Category updated successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Category not found</response>
    /// <response code="409">Category name already exists</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(Result<CategoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Result), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Update an existing category",
        Description = "Update a product category",
        OperationId = "UpdateCategory",
        Tags = new[] { "CMS", "CMS_Categories" }
    )]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryRequestDto request)
    {
        var command = new UpdateCategoryCommand(id, request);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    /// <remarks>
    /// Sample request:
    /// 
    ///     DELETE /api/categories/{id}
    ///     
    /// Note: Category can only be deleted if it has no subcategories and no products.
    /// This operation performs a soft delete.
    /// </remarks>
    /// <param name="id">Category ID</param>
    /// <returns>Deletion result</returns>
    /// <response code="200">Category deleted successfully</response>
    /// <response code="400">Cannot delete category (has subcategories or products)</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="403">Forbidden</response>
    /// <response code="404">Category not found</response>
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
        Summary = "Delete a category",
        Description = "Soft delete a product category",
        OperationId = "DeleteCategory",
        Tags = new[] { "CMS", "CMS_Categories" }
    )]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return StatusCode(result.GetHttpStatusCode(), result);
        }

        return Ok(result);
    }
}
