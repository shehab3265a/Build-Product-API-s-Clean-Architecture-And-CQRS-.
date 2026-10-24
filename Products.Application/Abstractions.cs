using Products.Domain.Entities;

namespace Products.Application;

// Defined in Application, implemented in Infrastructure (Dependency Inversion).
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken);
    void Add(Product product);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
