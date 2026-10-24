using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Products.Domain.Entities;

namespace Products.Application;

public record ProductDto(int Id, string Name, decimal Price);

public class NotFoundException(string entityName, object key)
    : Exception($"{entityName} with id '{key}' was not found.");

// ── Command: create product ──────────────────────────────────────────────────

// Returns the new product's Id.
public record CreateProductCommand(string Name, decimal Price) : IRequest<int>;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true);
    }
}

public class CreateProductCommandHandler(IProductRepository repository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreateProductCommand, int>
{
    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = Product.Create(request.Name, request.Price);

        repository.Add(product);
        await unitOfWork.SaveChangesAsync(cancellationToken); // Add() alone doesn't persist

        return product.Id;
    }
}

// ── Query: get product by id ─────────────────────────────────────────────────

public record GetProductByIdQuery(int Id) : IRequest<ProductDto>;

public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator() => RuleFor(x => x.Id).GreaterThan(0);
}

public class GetProductByIdQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.Id);

        return new ProductDto(product.Id, product.Name, product.Price);
    }
}

// ── Query: get products (paged) ──────────────────────────────────────────────

public record GetProductsQuery(int Page = 1, int PageSize = 10) : IRequest<IReadOnlyList<ProductDto>>;

public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public class GetProductsQueryHandler(IProductRepository repository)
    : IRequestHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    public async Task<IReadOnlyList<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await repository.GetPagedAsync(request.Page, request.PageSize, cancellationToken);
        return products.Select(p => new ProductDto(p.Id, p.Name, p.Price)).ToList();
    }
}

// ── Validation pipeline: runs every validator BEFORE the handler ─────────────

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
        if (failures.Count > 0)
            throw new ValidationException(failures);

        return await next();
    }
}

// ── Registration ─────────────────────────────────────────────────────────────

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
