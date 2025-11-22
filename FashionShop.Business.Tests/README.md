# FashionShop.Business.Tests

This project contains unit tests for the FashionShop.Business layer.

## Test Framework

- **xUnit**: Testing framework
- **Moq**: Mocking library for creating test doubles

## Test Coverage

### ProductServiceTests
Tests for the ProductService class covering:
- ✅ Getting all products (with and without cache)
- ✅ Getting product by ID (found and not found scenarios)
- ✅ Creating new products
- ✅ Updating existing products
- ✅ Cache invalidation on create/update operations

### CatalogServiceTests
Tests for the CatalogService class covering:
- ✅ Getting all catalogs (with and without cache)
- ✅ Getting catalog by ID (found and not found scenarios)
- ✅ Creating new catalogs
- ✅ Updating existing catalogs
- ✅ Deleting catalogs
- ✅ Cache invalidation on CRUD operations

### InventoryServiceTests
Tests for the InventoryService class covering:
- ✅ Checking stock availability
- ✅ Deducting stock (success and failure scenarios)
- ✅ Adding stock (new and existing inventory)
- ✅ Handling insufficient stock errors
- ✅ Handling missing inventory errors
- ✅ Theory-based tests for various quantities

### OrderServiceTests
Tests for the OrderService class covering:
- ✅ Placing valid orders (user and guest orders)
- ✅ Order validation (empty items, missing user/guest ID)
- ✅ Stock validation before order placement
- ✅ Product existence validation
- ✅ Total amount calculation for multiple items
- ✅ Getting order by ID
- ✅ Getting orders by user ID
- ✅ Inventory deduction on order placement

## Running the Tests

### Run all tests
```bash
dotnet test
```

### Run tests with detailed output
```bash
dotnet test --verbosity detailed
```

### Run tests with code coverage
```bash
dotnet test /p:CollectCoverage=true
```

### Run specific test class
```bash
dotnet test --filter "FullyQualifiedName~ProductServiceTests"
```

### Run specific test method
```bash
dotnet test --filter "FullyQualifiedName~ProductServiceTests.GetAllProductsAsync_WhenCacheIsEmpty_ReturnsProductsFromRepository"
```

## Test Structure

Each test follows the **Arrange-Act-Assert** (AAA) pattern:

1. **Arrange**: Set up test data and mock dependencies
2. **Act**: Execute the method being tested
3. **Assert**: Verify the expected outcome

## Mocking Strategy

- Repository interfaces are mocked using Moq
- IDistributedCache is mocked for caching tests
- IInventoryService is mocked for order service tests
- Each test is isolated and doesn't depend on external resources

## Best Practices

- ✅ Each test has a clear, descriptive name
- ✅ Tests are independent and can run in any order
- ✅ Mocks are verified to ensure expected interactions
- ✅ Both success and failure scenarios are tested
- ✅ Edge cases are covered (null values, empty collections, etc.)

## Future Enhancements

- Add integration tests
- Add test coverage reporting
- Add performance tests for caching scenarios
- Add tests for concurrent operations
