using MediatR;
using Microsoft.AspNetCore.Mvc;
using Products.Application;

namespace Products.API.Controllers;

// API-facing DTO: keeps the HTTP contract separate from the Application command.
public record CreateProductRequest(string Name, decimal Price);

[ApiController]
[Route("api/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var products = await sender.Send(new GetProductsQuery(page, pageSize), cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(new CreateProductCommand(request.Name, request.Price), cancellationToken);
        var created = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }
}
