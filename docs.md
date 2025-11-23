# Fashion Shop Backend - Technical Documentation



## Table of Contents
1. [Project Scope and Business Requirements](#project-scope-and-business-requirements)
2. [Architecture](#architecture)
3. [Database Design](#database-design)
4. [Technology Stack](#technology-stack)
5. [Business Rules & Validations](#business-rules--validations)
6. [Caching Strategy](#caching-strategy)
7. [Concurrency Handling](#concurrency-handling)
8. [Pagination Strategy](#pagination-strategy)
9. [Improvements](#improvements)

---

## Project Scope and Business Requirements

### 1. Project Overview
The Fashion Shop Backend is a robust, scalable e-commerce API designed to manage products, catalogs, inventory, and orders. It serves as the core engine for a fashion retail application, supporting both authenticated users and guest checkout experiences. The system prioritizes data integrity, performance through caching, and reliable inventory management.

### 2. Core Features & Scope

#### 2.1 Product Management
*   **Catalog Organization**: Products are organized into catalogs (categories).
*   **Flexible Attributes**: Products support dynamic properties (e.g., size, color, material) via JSON storage, allowing for a wide variety of fashion items without schema changes.
*   **Lifecycle Management**: Full CRUD capabilities with Soft Delete to preserve historical data.
*   **Performance**: High-performance retrieval using Redis caching for product listings and details.

#### 2.2 Inventory Management
*   **Real-time Tracking**: Accurate tracking of stock levels for each product.
*   **Concurrency Control**: Optimistic concurrency handling to prevent overselling during high-traffic periods (e.g., flash sales).
*   **Stock Validation**: Strict validation ensures orders cannot be placed for out-of-stock items.
*   **Automatic Replenishment**: Capabilities to add or adjust stock levels seamlessly.

#### 2.3 Order Processing
*   **Dual Checkout Modes**:
    *   **Guest Checkout**: Allows anonymous users to purchase using a unique Guest ID.
    *   **User Checkout**: Supports registered users with order history tracking.
*   **Atomic Transactions**: Order placement and inventory deduction occur within a unified workflow to ensure consistency.
*   **Price Snapshotting**: Captures the exact price of items at the time of purchase, protecting historical order data from future price changes.
*   **Order History**: Users can view their past orders with full details.

#### 2.4 System Architecture
*   **Clean Architecture**: Separation of concerns (Core, Data, Business, API) for maintainability.
*   **Performance Optimization**:
    *   **Caching**: Strategic caching of high-read data (Products, Catalogs).
    *   **Pagination**: Efficient offset-based pagination for large datasets.
*   **Data Integrity**:
    *   **Soft Deletes**: Prevents accidental permanent data loss.
    *   **Audit Trails**: Timestamps for creation, updates, and deletions.

### 3. Key Business Requirements

1.  **Inventory Integrity**: The system MUST prevent negative stock levels. Inventory checks and deductions must be atomic or handled with optimistic locking to support concurrent users.
2.  **Price Stability**: Order totals MUST be calculated server-side using current database prices. Historical orders MUST retain the price at the time of purchase.
3.  **Guest Accessibility**: The system MUST support a seamless checkout experience for non-registered users without forcing account creation.
4.  **Performance**: Public-facing endpoints (Product/Catalog listings) MUST be cached to ensure sub-millisecond response times under load.
5.  **Data Preservation**: Critical entities (Products, Catalogs) MUST NOT be permanently deleted from the database to ensure referential integrity for historical orders.

---

## Architecture

### Overview
The Fashion Shop Backend follows a **Clean Architecture** pattern with clear separation of concerns, organized into four distinct layers:

```
FashionShopAPI (Presentation Layer)
    ↓
FashionShop.Business (Business Logic Layer)
    ↓
FashionShop.Data (Data Access Layer)
    ↓
FashionShop.Core (Domain/Entity Layer)
```

### Layer Responsibilities

#### 1. **FashionShop.Core** (Domain Layer)
- **Purpose**: Contains core business entities and interfaces
- **Dependencies**: None (pure domain models)
- **Contents**:
  - Entity models (User, Product, Catalog, Order, OrderItem, Inventory)
  - Repository interfaces (IRepository<T>)
  - No external dependencies

#### 2. **FashionShop.Data** (Data Access Layer)
- **Purpose**: Handles all database operations and persistence
- **Dependencies**: FashionShop.Core, Entity Framework Core, Npgsql
- **Contents**:
  - `FashionShopDbContext`: EF Core DbContext
  - `EfRepository<T>`: Generic repository implementation
  - Database migrations
  - Entity configurations

#### 3. **FashionShop.Business** (Business Logic Layer)
- **Purpose**: Implements business logic, validations, and orchestration
- **Dependencies**: FashionShop.Core, FashionShop.Data
- **Contents**:
  - Service implementations (ProductService, OrderService, InventoryService, CatalogService)
  - DTOs (Data Transfer Objects)
  - Business rules and validations
  - Caching logic

#### 4. **FashionShopAPI** (Presentation Layer)
- **Purpose**: Exposes RESTful API endpoints
- **Dependencies**: All other layers
- **Contents**:
  - API Controllers
  - Dependency injection configuration
  - Middleware configuration
  - Swagger/OpenAPI setup


## Database Design

### Database: PostgreSQL

### Entity Relationship Diagram

```
┌─────────────┐
│    User     │
│─────────────│
│ Id (PK)     │
│ Username    │
│ PasswordHash│
│ Role        │
└──────┬──────┘
       │
       │ 0..1
       │
       ↓
┌─────────────┐         ┌──────────────┐
│   Order     │────────→│  OrderItem   │
│─────────────│  1..N   │──────────────│
│ Id (PK)     │         │ Id (PK)      │
│ UserId (FK) │         │ OrderId (FK) │
│ GuestId     │         │ ProductId(FK)│
│ CustomerName│         │ Quantity     │
│ PhoneNumber │         │ UnitPrice    │
│ Address     │         └──────┬───────┘
│ OrderDate   │                │
│ TotalAmount │                │
│ Status      │                │
└─────────────┘                │
                               │
                               ↓
┌─────────────┐         ┌──────────────┐
│   Catalog   │────────→│   Product    │
│─────────────│  1..N   │──────────────│
│ Id (PK)     │         │ Id (PK)      │
│ Name        │         │ Name         │
│ Description │         │ Sku          │
└─────────────┘         │ Price        │
                        │ Properties   │ (JSONB)
                        │ CatalogId(FK)│
                        └──────┬───────┘
                               │
                               │ 1..1
                               ↓
                        ┌──────────────┐
                        │  Inventory   │
                        │──────────────│
                        │ Id (PK)      │
                        │ ProductId(FK)│
                        │ Quantity     │
                        │ LastUpdated  │
                        │ RowVersion   │ (Concurrency)
                        └──────────────┘
```

### Tables

#### **Users**
| Column       | Type          | Constraints           |
|--------------|---------------|-----------------------|
| Id           | int           | PRIMARY KEY, IDENTITY |
| Username     | varchar(100)  | NOT NULL, UNIQUE      |
| PasswordHash | varchar(MAX)  | NOT NULL              |
| Role         | varchar(50)   | NOT NULL, DEFAULT 'User' |

#### **Catalogs**
| Column      | Type         | Constraints           |
|-------------|--------------|------------------------|
| Id          | int          | PRIMARY KEY, IDENTITY  |
| Name        | varchar(100) | NOT NULL               |
| Description | varchar(500) | NULL                   |
| CreatedAt   | timestamp    | NOT NULL, DEFAULT UTC_NOW |
| UpdatedAt   | timestamp    | NULL                   |
| IsDeleted   | boolean      | NOT NULL, DEFAULT false |
| DeletedAt   | timestamp    | NULL                   |

#### **Products**
| Column     | Type          | Constraints           |
|------------|---------------|-----------------------|
| Id         | int           | PRIMARY KEY, IDENTITY |
| Name       | varchar(200)  | NOT NULL              |
| Sku        | varchar(50)   | NOT NULL, UNIQUE      |
| Price      | decimal(18,2) | NOT NULL              |
| Properties | jsonb         | NULL                  |
| CatalogId  | int           | FOREIGN KEY → Catalogs.Id |
| CreatedAt  | timestamp     | NOT NULL, DEFAULT UTC_NOW |
| UpdatedAt  | timestamp     | NULL                  |
| IsDeleted  | boolean       | NOT NULL, DEFAULT false |
| DeletedAt  | timestamp     | NULL                  |

**Properties Field**: Stores flexible product attributes as JSONB (e.g., color, size, material)

#### **Inventories**
| Column      | Type      | Constraints                    |
|-------------|-----------|--------------------------------|
| Id          | int       | PRIMARY KEY, IDENTITY          |
| ProductId   | int       | FOREIGN KEY → Products.Id, UNIQUE |
| Quantity    | int       | NOT NULL                       |
| LastUpdated | timestamp | NOT NULL, DEFAULT UTC_NOW      |
| RowVersion  | bytea     | TIMESTAMP (Optimistic Locking) |

#### **Orders**
| Column       | Type          | Constraints           |
|--------------|---------------|-----------------------|
| Id           | int           | PRIMARY KEY, IDENTITY |
| UserId       | int           | FOREIGN KEY → Users.Id, NULL |
| GuestId      | varchar(100)  | NULL                  |
| CustomerName | varchar(200)  | NOT NULL              |
| PhoneNumber  | varchar(50)   | NOT NULL              |
| Address      | varchar(500)  | NOT NULL              |
| OrderDate    | timestamp     | NOT NULL, DEFAULT UTC_NOW |
| TotalAmount  | decimal(18,2) | NOT NULL              |
| Status       | varchar(50)   | NOT NULL, DEFAULT 'Pending' |

**Note**: Either UserId OR GuestId must be present (supports both authenticated and guest orders)

#### **OrderItems**
| Column    | Type          | Constraints           |
|-----------|---------------|-----------------------|
| Id        | int           | PRIMARY KEY, IDENTITY |
| OrderId   | int           | FOREIGN KEY → Orders.Id |
| ProductId | int           | FOREIGN KEY → Products.Id |
| Quantity  | int           | NOT NULL              |
| UnitPrice | decimal(18,2) | NOT NULL              |

### Key Design Decisions

1. **JSONB for Product Properties**: Allows flexible product attributes without schema changes
2. **Separate Inventory Table**: Enables optimistic concurrency control with RowVersion
3. **Guest Orders Support**: UserId is nullable, GuestId allows anonymous purchases
4. **Price Snapshot**: OrderItems store UnitPrice to preserve historical pricing
5. **Soft Relationships**: Catalog relationship is optional (CatalogId nullable)
6. **Soft Delete Pattern**: Products and Catalogs use soft delete (IsDeleted flag) to preserve historical data and maintain referential integrity

---

## Technology Stack

### Core Technologies

#### **1. PostgreSQL**
**Why chosen:**
- **JSONB Support**: Native support for semi-structured data (Product.Properties)
- **ACID Compliance**: Ensures data integrity for financial transactions
- **Advanced Features**: Full-text search, array types, custom functions
- **Performance**: Excellent query optimization and indexing
- **Open Source**: No licensing costs
- **Scalability**: Handles high concurrent loads effectively
- **Reliability**: Battle-tested in production environments

#### **2. Entity Framework Core with Npgsql**
**Why chosen:**
- **ORM Benefits**: Reduces boilerplate data access code
- **Type Safety**: Compile-time query validation
- **Migration Support**: Database versioning and schema evolution
- **LINQ Support**: Expressive query syntax
- **Npgsql Provider**: Optimized PostgreSQL driver with JSONB support
- **Change Tracking**: Automatic detection of entity modifications

#### **3. Redis (StackExchange.Redis)**
**Why chosen:**
- **In-Memory Performance**: Sub-millisecond response times
- **Distributed Caching**: Supports horizontal scaling
- **Data Structures**: Rich data types (strings, hashes, lists, sets)
- **Persistence Options**: Can persist to disk if needed
- **Pub/Sub Support**: For future real-time features
- **Cache Invalidation**: Flexible expiration policies
- **Production Ready**: Used by major companies worldwide

---

## Validation Rules

Validation rules are input constraints that ensure data integrity and format correctness. These are enforced at the API layer before business logic execution.

### Product Validations

#### **Creating or Updating a Product**

| Field | Validation Rule |
|-------|----------------|
| **Name** | • Must be provided (required field)<br>• Cannot exceed 200 characters<br>• Cannot be empty or whitespace only |
| **SKU** | • Must be provided (required field)<br>• Cannot exceed 50 characters<br>• Must be unique across all products in the system<br>• Cannot be empty or whitespace only |
| **Price** | • Must be provided (required field)<br>• Must be greater than zero (0.01 minimum)<br>• Must be a valid decimal number with up to 2 decimal places<br>• Cannot be negative |
| **Properties** | • Optional field<br>• If provided, must be valid JSON format<br>• Can contain any key-value pairs<br>• No size limit enforced at validation layer |
| **CatalogId** | • Optional field<br>• If provided, must be a positive integer<br>• Referenced catalog does not need to exist (nullable foreign key) |

---

### Catalog Validations

#### **Creating or Updating a Catalog**

| Field | Validation Rule |
|-------|----------------|
| **Name** | • Must be provided (required field)<br>• Cannot exceed 100 characters<br>• Cannot be empty or whitespace only |
| **Description** | • Optional field<br>• If provided, cannot exceed 500 characters |

---

### Order Validations

#### **Placing an Order**

| Field | Validation Rule |
|-------|----------------|
| **UserId** | • Optional field<br>• If provided, must be a positive integer<br>• Either UserId OR GuestId must be present (not both null) |
| **GuestId** | • Optional field<br>• If provided, cannot exceed 100 characters<br>• Either UserId OR GuestId must be present (not both null) |
| **CustomerName** | • Must be provided (required field)<br>• Cannot exceed 200 characters<br>• Cannot be empty or whitespace only |
| **PhoneNumber** | • Must be provided (required field)<br>• Cannot exceed 50 characters<br>• Cannot be empty or whitespace only |
| **Address** | • Must be provided (required field)<br>• Cannot exceed 500 characters<br>• Cannot be empty or whitespace only |
| **Items** | • Must be provided (required field)<br>• Must contain at least one order item<br>• Cannot be an empty list |

#### **Order Item Validations**

| Field | Validation Rule |
|-------|----------------|
| **ProductId** | • Must be provided (required field)<br>• Must be greater than zero<br>• Must be a valid integer |
| **Quantity** | • Must be provided (required field)<br>• Must be at least 1<br>• Must be a valid integer<br>• Cannot be negative or zero |

---

### Inventory Validations

#### **Adding Stock**

| Field | Validation Rule |
|-------|----------------|
| **ProductId** | • Must be provided (required field)<br>• Must be greater than zero<br>• Must be a valid integer |
| **Quantity** | • Must be provided (required field)<br>• Must be greater than zero<br>• Must be a valid integer<br>• Cannot be negative |

---

## Business Rules

Business rules are domain-specific logic and constraints that govern how the system operates. These are enforced at the service layer after validation passes.

### Product Management Rules

#### **Product Creation**
1. **SKU Uniqueness**: The system must verify that no other product exists with the same SKU before creating a new product
2. **Catalog Reference**: If a CatalogId is provided, the system allows the product to be created even if the catalog doesn't exist (soft reference)
3. **Cache Invalidation**: Upon successful creation, the system must invalidate the "all products" cache to ensure fresh data on next retrieval

#### **Product Update**
1. **Existence Check**: The product must exist in the database before it can be updated
2. **SKU Uniqueness**: If the SKU is changed, the new SKU must not conflict with existing products
3. **Cache Invalidation**: Upon successful update, the system must invalidate both the "all products" cache and the specific product cache
4. **Catalog Reassignment**: Products can be moved between catalogs or removed from catalogs (CatalogId set to null)

#### **Product Deletion**
1. **Soft Delete Implementation**: Products are soft-deleted (marked as IsDeleted=true) rather than permanently removed
2. **Timestamp Recording**: DeletedAt timestamp is set to the current UTC time when deleted
3. **Query Filtering**: Soft-deleted products are automatically excluded from all queries via global query filters
4. **Historical Data Preservation**: Maintains product data for order history and reporting purposes
5. **Cache Invalidation**: Upon successful deletion, the system must invalidate both the "all products" cache and the specific product cache

---

### Order Management Rules

#### **Order Placement Process**

1. **User Identification**
   - Every order must be associated with either an authenticated user (UserId) or an anonymous guest (GuestId)
   - The system must not allow orders with both UserId and GuestId null
   - Guest orders are tracked via a unique GuestId string (typically a UUID or session identifier)

2. **Stock Availability Verification**
   - Before processing the order, the system must check stock availability for ALL items in the order
   - If any single item has insufficient stock, the entire order must be rejected
   - Stock checks must happen before any inventory deduction to prevent partial fulfillment

3. **Price Calculation**
   - The total order amount is calculated by the system, not provided by the client
   - Each item's price is fetched from the current product price in the database
   - Unit prices are captured at order time and stored in OrderItems (price snapshot)
   - Formula: `TotalAmount = Sum(Product.Price × OrderItem.Quantity)` for all items

4. **Inventory Deduction**
   - Stock deduction happens for each item in the order
   - Deductions are performed sequentially for each product
   - If any deduction fails (due to concurrency or insufficient stock), the entire order transaction should fail
   - The system uses optimistic concurrency control to handle simultaneous stock deductions

5. **Order Status Initialization**
   - All new orders are created with a status of "Pending"
   - Order date is automatically set to the current UTC timestamp
   - Order status can be updated later (future enhancement)

6. **Atomicity**
   - Order creation and inventory deduction must be atomic
   - If inventory deduction fails, the order should not be created
   - Database transactions ensure consistency

#### **Order Item Rules**

1. **Product Existence**: Each ProductId in the order must reference an existing product in the database
2. **Quantity Constraints**: Quantity must be a positive integer (minimum 1)
3. **Price Snapshot**: The current product price is captured and stored with the order item, not referenced dynamically

---

### Inventory Management Rules

#### **Stock Checking**

1. **Availability Determination**
   - The system checks if the available quantity in inventory is greater than or equal to the requested quantity
   - Returns a boolean result (true = sufficient, false = insufficient)
   - Does not modify inventory, only reads current state

#### **Stock Deduction**

1. **Existence Verification**
   - An inventory record must exist for the product before stock can be deducted
   - If no inventory record exists, the operation fails with an error

2. **Sufficient Quantity Check**
   - Before deducting, the system verifies that current quantity >= requested deduction amount
   - If insufficient, the operation fails immediately with a descriptive error message

3. **Concurrency Handling**
   - The system uses optimistic concurrency control via the RowVersion field
   - If a concurrent modification is detected, the operation retries up to 3 times
   - A 50-millisecond delay is introduced between retry attempts
   - After 3 failed attempts, the operation fails with a concurrency error

4. **Timestamp Update**
   - The LastUpdated field is set to the current UTC timestamp on every successful deduction
   - This provides an audit trail of when inventory levels changed

5. **Negative Stock Prevention**
   - The system must never allow inventory quantity to become negative
   - This is enforced through the sufficient quantity check before deduction

#### **Stock Addition**

1. **Auto-Creation**
   - If no inventory record exists for a product, the system automatically creates one with the specified quantity
   - If an inventory record exists, the quantity is incremented by the specified amount

2. **Timestamp Update**
   - The LastUpdated field is set to the current UTC timestamp on every addition
   - Provides tracking of when stock was replenished

3. **No Upper Limit**
   - There is no maximum quantity constraint enforced by the system
   - Warehouse capacity is assumed to be managed externally

---

### Catalog Management Rules

#### **Catalog Creation**

1. **Name Uniqueness**: While not enforced at the database level, catalog names should be unique for better user experience
2. **Empty Catalogs**: Catalogs can be created without any products initially

#### **Catalog Update**

1. **Existence Check**: The catalog must exist before it can be updated
2. **Product Preservation**: Updating a catalog does not affect its associated products
3. **Cache Invalidation**: Both "all catalogs" and specific catalog caches are invalidated on update

#### **Catalog Deletion**

1. **Soft Delete Implementation**: Catalogs are soft-deleted (marked as IsDeleted=true) rather than permanently removed
2. **Timestamp Recording**: DeletedAt timestamp is set to the current UTC time when deleted
3. **Query Filtering**: Soft-deleted catalogs are automatically excluded from all queries via global query filters
4. **Product Preservation**: Associated products remain unchanged when a catalog is soft-deleted
5. **Cache Invalidation**: The "all catalogs" cache is invalidated on deletion

---

## Caching Strategy

### Implementation: Redis Distributed Cache

### Cache Keys Pattern
```
products_all           → List of all products
product_{id}           → Individual product by ID
catalogs_all           → List of all catalogs
catalog_{id}           → Individual catalog by ID
```

### Caching Policies

#### **Product Caching**

**Get All Products:**
```csharp
Cache Key: "products_all"
Absolute Expiration: 1 hour
Sliding Expiration: 10 minutes
```

**Get Product by ID:**
```csharp
Cache Key: "product_{id}"
Absolute Expiration: 1 hour
Sliding Expiration: 10 minutes
```

**Cache Invalidation:**
- **Create Product**: Remove `products_all`
- **Update Product**: Remove `products_all` AND `product_{id}`
- **Delete Product**: Remove `products_all` AND `product_{id}`

#### **Catalog Caching**

**Get All Catalogs:**
```csharp
Cache Key: "catalogs_all"
Absolute Expiration: 2 hours
Sliding Expiration: 20 minutes
```

**Get Catalog by ID:**
```csharp
Cache Key: "catalog_{id}"
Absolute Expiration: 2 hours
Sliding Expiration: 20 minutes
```

**Cache Invalidation:**
- **Create Catalog**: Remove `catalogs_all`
- **Update Catalog**: Remove `catalogs_all` AND `catalog_{id}`
- **Delete Catalog**: Remove `catalogs_all` AND `catalog_{id}`

### Why Not Cache Orders/Inventory?

**Orders:**
- Frequently changing data
- User-specific (not shared across users)
- Caching would provide minimal benefit
- Real-time accuracy is critical

**Inventory:**
- Highly volatile (changes with every order)
- Concurrency conflicts would invalidate cache constantly
- Real-time stock levels are essential for business operations

### Cache-Aside Pattern

The implementation uses the **Cache-Aside (Lazy Loading)** pattern:

1. **Read Operation:**
   - Check cache first
   - If hit: return cached data
   - If miss: query database → cache result → return data

2. **Write Operation:**
   - Update database
   - Invalidate related cache entries
   - Next read will populate cache

### Benefits
- Reduced database load for read-heavy operations
- Improved response times for product/catalog queries
- Automatic cache warming through usage
- Simple invalidation strategy

---

## Concurrency Handling

### Problem Statement
Multiple users might attempt to purchase the same product simultaneously, potentially causing overselling if not handled properly.

### Solution: Optimistic Concurrency Control

#### **Implementation in Inventory Entity**

```csharp
[Timestamp]
public byte[] RowVersion { get; set; }
```

The `RowVersion` field is a PostgreSQL timestamp that:
- Automatically updates on every row modification
- Used by EF Core to detect concurrent modifications
- Throws `DbUpdateConcurrencyException` if conflict detected

#### **Concurrency Handling in InventoryService**

### How It Works

1. **Transaction 1** reads inventory (Quantity=10, RowVersion=v1)
2. **Transaction 2** reads inventory (Quantity=10, RowVersion=v1)
3. **Transaction 1** updates (Quantity=8) → RowVersion becomes v2
4. **Transaction 2** tries to update with RowVersion=v1 → **CONFLICT DETECTED**
5. **Transaction 2** retries, reads fresh data (Quantity=8, RowVersion=v2)
6. **Transaction 2** updates successfully (Quantity=6) → RowVersion becomes v3

### Retry Strategy

- **Max Retries**: 3 attempts
- **Delay**: 50ms between retries
- **Failure**: Throws exception after exhausting retries
- **Success Rate**: High in typical scenarios (low contention)

### Alternative Approaches Considered

1. **Pessimistic Locking (Row-Level Locks)**
   - ❌ Reduces throughput
   - ❌ Risk of deadlocks
   - ❌ Complexity in distributed systems

2. **Database Transactions with Serializable Isolation**
   - ❌ Performance overhead
   - ❌ Higher lock contention
   - ❌ Overkill for this use case

3. **Optimistic Concurrency (Chosen)**
   - ✅ Better performance under low contention
   - ✅ No locks held during business logic
   - ✅ Simple retry mechanism
   - ✅ Scales well

### Edge Cases Handled

- **Insufficient Stock**: Validated before deduction
- **Missing Inventory**: Exception thrown
- **Concurrent Updates**: Retry mechanism
- **Retry Exhaustion**: Clear error message to user

---

## Pagination Strategy

### Current Implementation: Offset-based Pagination

The system currently employs a standard **Offset-based Pagination** strategy. This is implemented via the `PaginationRequest` DTO, which accepts `PageNumber` and `PageSize`.

**How it works:**
The data access layer calculates the number of records to skip based on the formula:
`Skip = (PageNumber - 1) * PageSize`
It then takes the next `PageSize` records.

**Code Reference:**
- **Request**: `FashionShop.Business.DTOs.PaginationRequest`
- **Implementation**: `EfRepository.ListPagedAsync` using `.Skip().Take()`

### Pros & Cons

#### **Pros**
1.  **Simplicity**: Extremely easy to implement and understand. Directly maps to SQL `OFFSET` and `LIMIT`.
2.  **Random Access**: Allows users to jump directly to any specific page (e.g., "Go to Page 5") without traversing previous pages.
3.  **Stateless**: The server does not need to maintain any cursor state; the request contains all necessary information.
4.  **Frontend Friendly**: Easy to build standard pagination UI (Page 1, 2, 3... Next, Last).

#### **Cons**
1.  **Performance at Scale (Deep Pagination)**: Performance degrades linearly as the offset increases. To fetch Page 1000, the database must scan and discard the first 999 pages of results, which can be slow for large datasets.
2.  **Data Inconsistency**:
    - **Skipped Items**: If a new item is added to the top of the list while a user is viewing Page 1, moving to Page 2 might skip an item that was pushed down.
    - **Duplicate Items**: If an item is deleted from Page 1 while the user is viewing it, moving to Page 2 might show an item that was already seen (shifted up).

### Future Improvements

#### **1. Cursor-based Pagination (Keyset Pagination)**
**Recommendation**: Implement for "Infinite Scroll" features or high-volume APIs.
- **Concept**: Instead of "Page 2", the client requests "10 items after Product ID 50".
- **Benefit**: Constant time complexity (O(1)) regardless of how deep you scroll. Eliminates skipped/duplicate item issues during concurrent updates.
- **Trade-off**: Cannot jump to a specific page number.

#### **2. Hybrid Approach**
**Recommendation**: Use Offset for the first few pages and Cursor for deep scrolling.
- **Concept**: Allow random access for the first 10-20 pages (where users mostly stay) and switch to cursor-based logic for deeper navigation to maintain performance.

#### **3. Caching Paged Results**
**Recommendation**: Cache the first page of popular listings.
- **Concept**: Store the result of "Page 1" for popular categories in Redis with a short expiration (e.g., 1-5 minutes).
- **Benefit**: Drastically reduces database load, as the majority of user traffic is often just viewing the first page.

---

## Error Handling

### Standard Error Response Format
```json
{
  "error": "Error message description"
}
```

### HTTP Status Codes Used

- `200 OK`: Successful GET/POST operations
- `201 Created`: Resource created successfully
- `204 No Content`: Successful PUT/DELETE operations
- `400 Bad Request`: Validation errors or business rule violations
- `404 Not Found`: Resource doesn't exist
- `500 Internal Server Error`: Unexpected server errors

---

## Improvements

This section outlines planned improvements and missing critical features for the Fashion Shop application.

Missing: 
- Authentication & Authorization
- Product images and media management

Features:
- Product Reviews & Ratings
- Order Status Tracking & History
- Payment Processing
- Shipping & Delivery Management


## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=FashionShopDb;Username=postgres;Password=admin",
    "RedisConnection": "localhost:6379"
  }
}
```

### Environment Variables (Production)
- Use environment-specific configuration files
- Store sensitive data in secure vaults (Azure Key Vault, AWS Secrets Manager)
- Override connection strings via environment variables

---

## Performance Considerations

### Database
- Indexes on foreign keys (auto-created by EF Core)
- Index on Product.Sku (unique constraint)
- Connection pooling enabled by default
- Async/await for all database operations

### Caching
- Redis for distributed caching
- Cache-aside pattern for optimal performance
- Strategic cache invalidation
- Sliding expiration to keep hot data in cache

### Concurrency
- Optimistic concurrency for inventory
- Minimal lock contention
- Retry mechanism for conflict resolution

---

