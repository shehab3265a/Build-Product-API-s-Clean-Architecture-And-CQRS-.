namespace Products.Domain.Entities;

public class Product
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }

    private Product() { } // for EF Core

    // Business rules live in the Domain: an invalid Product can't be created.
    public static Product Create(string name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be greater than zero.");

        return new Product { Name = name.Trim(), Price = price };
    }
}
