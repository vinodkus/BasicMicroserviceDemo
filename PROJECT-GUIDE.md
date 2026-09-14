# BasicMicroservicesDemo — Complete Beginner Guide

Yeh document poore project ko simple Hindi/English mein explain karta hai.
Pehle yeh padho, phir code kholo. Tab har file samajh aa jayegi.

---

## 1. Yeh project kya hai?

Yeh ek **very basic .NET Microservices demo** hai.

Isme **2 alag Web APIs** hain:

| Service | Port | Kaam | Database |
|---|---|---|---|
| **ProductService** | http://localhost:5001 | Products create/read | `productservice.db` |
| **OrderService** | http://localhost:5002 | Orders create/read | `orderservice.db` |

Dono **alag programs** hain. Alag ports. Alag databases.
Ek dusre se **HTTP** se baat karte hain.

Abhi is project mein **nahi** hai:

- Docker
- Kubernetes
- RabbitMQ / Kafka
- Redis
- API Gateway
- Authentication / JWT

Pehle sirf basic idea clear karna hai.

---

## 2. Microservice kya hota hai? (simple)

**Normal Web API** mein products + orders **ek hi project** mein hote hain.
Ek hi port. Ek hi database. Direct method call.

**Microservices** mein har bada kaam **alag service** hota hai.

Is project mein:

- Product related kaam = ProductService
- Order related kaam = OrderService

Jaise do alag dukanein:

- Ek dukan sirf products bechti hai
- Dusri dukan orders leti hai
- Order dukan, product dukan se **phone** karke price poochti hai

Yahan "phone" = **HTTP request** (`HttpClient`)

---

## 3. Sabse important flow

Jab aap order create karte ho:

```
Aap (Swagger / browser)
        ↓
   POST /api/orders
        ↓
   OrderService  (port 5002)
        ↓
   HTTP GET /api/products/{id}
        ↓
   ProductService  (port 5001)
        ↓
   Product price wapas
        ↓
   OrderService calculate karta hai:
   TotalAmount = Price × Quantity
        ↓
   Order OrderService ke database mein save
        ↓
   Aapko created order milta hai
```

Yaad rakhne wali baat:

**OrderService ke paas products ki list nahi hai.**
Uske paas sirf `ProductId` hota hai.
Price usko ProductService se maangni padti hai.

---

## 4. Folder structure

```
BasicMicroservicesDemo/
│
├── BasicMicroservicesDemo.sln          ← Visual Studio / Cursor solution
├── PROJECT-GUIDE.md                    ← yeh document
│
├── ProductService/                     ← pehli independent service
│   ├── Controllers/
│   │   └── ProductsController.cs       ← APIs: GET/POST products
│   ├── Models/
│   │   └── Product.cs                  ← Id, Name, Price
│   ├── Data/
│   │   └── ProductDbContext.cs         ← ProductService ka database
│   ├── Migrations/                     ← database tables ka history
│   ├── Program.cs                      ← service start yahan se hota hai
│   ├── appsettings.json                ← connection string
│   └── productservice.db               ← SQLite database file
│
└── OrderService/                       ← doosri independent service
    ├── Controllers/
    │   └── OrdersController.cs         ← APIs: GET/POST orders
    ├── Models/
    │   ├── Order.cs                    ← Id, ProductId, Quantity, TotalAmount
    │   ├── CreateOrderRequest.cs       ← client kya bhejta hai
    │   └── ProductDto.cs               ← ProductService ke response ka copy
    ├── Services/
    │   └── ProductApiClient.cs         ← HTTP se ProductService ko call
    ├── Data/
    │   └── OrderDbContext.cs           ← OrderService ka database
    ├── Migrations/
    ├── Program.cs
    ├── appsettings.json                ← Order DB + ProductService URL
    └── orderservice.db
```

Shared class library **nahi** hai.
Dono services ke models alag-alag copy hain. Yeh Microservices ka beginner rule hai:
**har service apna model khud rakhti hai.**

---

## 5. ProductService — file by file

### `Models/Product.cs`

Product ka data:

- `Id` — database banati hai
- `Name` — jaise Laptop
- `Price` — jaise 50000

Yeh class OrderService ke saath share **nahi** hoti.

### `Data/ProductDbContext.cs`

Yeh ProductService ka **database door** hai.

- `DbSet<Product> Products` = database ki `Products` table
- `HasData(...)` = pehli baar 3 sample products insert:

  1. Laptop — 50000
  2. Mouse — 1000
  3. Keyboard — 2000

OrderService is DbContext ko use **nahi** kar sakta.

### `Controllers/ProductsController.cs`

Teen APIs:

| Method | URL | Kaam |
|---|---|---|
| GET | `/api/products` | saare products |
| GET | `/api/products/{id}` | ek product. **OrderService isi ko call karta hai** |
| POST | `/api/products` | naya product save |

Controller `ProductDbContext` use karta hai.

- `ToListAsync()` — saari rows padho
- `FindAsync(id)` — ek row padho
- `Add()` + `SaveChangesAsync()` — naya product save

### `Program.cs`

Service start hote hi yeh hota hai:

1. Port **5001** set
2. Controllers enable
3. Swagger enable
4. SQLite database connect (`ProductDatabase` connection string)
5. App run

### `appsettings.json`

```json
"ConnectionStrings": {
  "ProductDatabase": "Data Source=productservice.db"
}
```

Connection string C# code mein hard-code nahi hai.

`appsettings.Development.json` Development environment ke liye same setting override kar sakta hai.

---

## 6. OrderService — file by file

### `Models/Order.cs`

Order ka data:

- `Id`
- `ProductId` — sirf ID, poora Product nahi
- `Quantity`
- `TotalAmount`

Product ka Name/Price yahan store **nahi** hota.

### `Models/CreateOrderRequest.cs`

Client yeh JSON bhejta hai:

```json
{
  "productId": 1,
  "quantity": 2
}
```

`TotalAmount` client nahi bhejta. OrderService khud calculate karta hai.

### `Models/ProductDto.cs`

ProductService se HTTP response padhne ke liye chhoti copy.

Yeh ProductService ke `Product` class ko share nahi karti.
OrderService ne apni alag class banayi.

### `Services/ProductApiClient.cs`

Yeh project ka **sabse important Microservices class** hai.

Kaam:

```
GET http://localhost:5001/api/products/{id}
```

Phir 3 possible results:

| Result | Matlab | OrderService kya return karega |
|---|---|---|
| Found | product mil gaya | order create |
| NotFound | product exist nahi | HTTP 404 |
| Unavailable | ProductService band / error | HTTP 503 |

`HttpClient` .NET ka tool hai jo HTTP request bhejta hai.
Hum `IHttpClientFactory` se usko banate hain. Yeh best practice hai.

### `Data/OrderDbContext.cs`

OrderService ka **apna** database.

- `DbSet<Order> Orders` = `Orders` table
- Yahan Products table **nahi** hai

Galati mat karna: OrderService mein `ProductDbContext` use nahi hota.

### `Controllers/OrdersController.cs`

Teen APIs:

| Method | URL | Kaam |
|---|---|---|
| GET | `/api/orders` | saare orders |
| GET | `/api/orders/{id}` | ek order |
| POST | `/api/orders` | naya order |

`POST /api/orders` ke andar yeh steps hain:

1. `productId` aur `quantity` receive
2. `ProductApiClient` se ProductService call
3. Product price lo
4. `TotalAmount = Price * Quantity`
5. Order ko OrderService database mein save
6. Created order return

Agar ProductService product nahi dhundhta → **404**
Agar ProductService band hai → **503**

### `Program.cs`

1. Port **5002** set
2. Controllers + Swagger
3. Apna SQLite database (`OrderDatabase`)
4. `ProductApiClient` register, BaseUrl = `http://localhost:5001`

### `appsettings.json`

```json
"ConnectionStrings": {
  "OrderDatabase": "Data Source=orderservice.db"
},
"ProductService": {
  "BaseUrl": "http://localhost:5001"
}
```

Do alag cheezein:

- `OrderDatabase` = OrderService ka data kahan save hoga
- `ProductService:BaseUrl` = ProductService kahan chal raha hai

---

## 7. Databases — beginner explanation

Pehle data **RAM / in-memory list** mein tha.
Service restart → data gayab.

Ab data **SQLite file** mein hai.

| Service | File | Table |
|---|---|---|
| ProductService | `productservice.db` | Products |
| OrderService | `orderservice.db` | Orders |

SQLite ek simple local database hai. Ek file. SQL Server install nahi chahiye.

**Kyun 2 databases?**

Microservices rule:

> Har service apna data khud rakhti hai.

Agar dono ek hi database share karein, to services truly independent nahi rahengi.

Isliye:

- Product change = sirf ProductService ka DB
- Order change = sirf OrderService ka DB
- Price chahiye = HTTP call, database share nahi

**Entity Framework Core (EF Core)**  
C# objects ko database rows mein convert karta hai.

Aap likhte ho:

```csharp
_db.Products.Add(product);
await _db.SaveChangesAsync();
```

EF Core peeche SQL chalata hai.

---

## 8. Migration kya hai?

Migration = database table ka version.

Jab aap model change karte ho (naya field), tab naya migration banate ho.

Pehla migration already ban chuka hai: `InitialCreate`

Usne yeh kiya:

- ProductService mein `Products` table + 3 sample products
- OrderService mein `Orders` table

### Naya migration banana ho to

Solution folder mein:

```powershell
cd C:\Vinod\CursorAI\Microservice\BasicMicroservicesDemo

dotnet ef migrations add KuchMeaningfulNaam --project ProductService --startup-project ProductService
dotnet ef migrations add KuchMeaningfulNaam --project OrderService --startup-project OrderService
```

### Database update

```powershell
dotnet ef database update --project ProductService --startup-project ProductService
dotnet ef database update --project OrderService --startup-project OrderService
```

`dotnet-ef` tool already machine par installed hai.

---

## 9. Dono services kaise run karein

**Pehle ProductService start karo**, phir OrderService.

### Terminal 1

```powershell
cd C:\Vinod\CursorAI\Microservice\BasicMicroservicesDemo
dotnet run --project ProductService
```

### Terminal 2

```powershell
cd C:\Vinod\CursorAI\Microservice\BasicMicroservicesDemo
dotnet run --project OrderService
```

Phir browser mein:

- Product Swagger: http://localhost:5001/swagger
- Order Swagger: http://localhost:5002/swagger

Check karne ke liye:

```powershell
netstat -ano | findstr ":5001 "
netstat -ano | findstr ":5002 "
```

`LISTENING` dikhe to service chal rahi hai.

Process ka naam dekhne ke liye (PID example `21932`):

```powershell
tasklist | findstr 21932
```

Service band karne ke liye:

```powershell
taskkill /PID 21932 /F
```

---

## 10. Swagger se test kaise karein

### Step 1 — Products dekho

http://localhost:5001/swagger  
`GET /api/products` → Try it out → Execute

Expected:

```json
[
  { "id": 1, "name": "Laptop", "price": 50000 },
  { "id": 2, "name": "Mouse", "price": 1000 },
  { "id": 3, "name": "Keyboard", "price": 2000 }
]
```

### Step 2 — Naya product banao

`POST /api/products`

```json
{
  "name": "Monitor",
  "price": 15000
}
```

Expected:

```json
{ "id": 4, "name": "Monitor", "price": 15000 }
```

### Step 3 — Order banao

http://localhost:5002/swagger  
`POST /api/orders`

```json
{
  "productId": 4,
  "quantity": 2
}
```

Expected:

```json
{
  "id": 1,
  "productId": 4,
  "quantity": 2,
  "totalAmount": 30000
}
```

`30000` = `15000 × 2`  
Price ProductService se aayi.

### Step 4 — Orders dekho

`GET /api/orders`

Order OrderService ke database se aana chahiye.

### Step 5 — Failure test

1. ProductService band karo
2. `POST /api/orders` dubara chalao

Expected: **HTTP 503**

```json
{
  "message": "Cannot reach ProductService. Make sure it is running on http://localhost:5001."
}
```

Matlab OrderService akela order nahi bana sakta.
Usse ProductService chahiye.

Galat product id (jaise 999) bhejo, ProductService chal raha ho, to **HTTP 404** aayega.

---

## 11. Common beginner questions

### ProductService alag kyun hai?

Products ka data aur logic yahin rehta hai.
Baad mein product rules change ho, to OrderService ka code touch nahi karna padta.

### OrderService alag kyun hai?

Orders alag business hain: quantity, total, order list.
Price usko ProductService se maangni padti hai.

### Alag ports kyun?

Dono alag programs hain.
Ek time pe ek program ek port use karta hai.
Isliye 5001 aur 5002.

Real world mein ye alag machines par bhi ho sakte hain.

### HttpClient kya hai?

.NET ka HTTP caller.
Jaise browser API call karta hai, waise hi ek service dusri service ko call karti hai.

### Services independent kyun hain?

- Alag project
- Alag `Program.cs`
- Alag database
- Alag port

Ek service restart ho, doosri chal sakti hai.
Lekin naya order tab fail hoga jab ProductService band ho.

### In-memory aur database mein farak?

| | Pehle (in-memory) | Ab (SQLite) |
|---|---|---|
| Data kahan | RAM | `.db` file |
| Restart ke baad | data gayab | data rehta hai |
| Beginner ke liye | concept seekhne ke liye | real storage seekhne ke liye |

### Normal Web API vs Microservices?

**Normal Web API:** ek project, ek port, ek database, products + orders saath.

**Microservices:** alag APIs, alag ports, alag databases, beech mein HTTP.

Is demo mein wahi dikhaya gaya hai.

---

## 12. Important files jaldi dhundho

Agar confuse ho jao, yeh 6 files pehle padho:

1. `ProductService/Controllers/ProductsController.cs`
2. `ProductService/Data/ProductDbContext.cs`
3. `OrderService/Controllers/OrdersController.cs`
4. `OrderService/Services/ProductApiClient.cs`
5. `OrderService/Data/OrderDbContext.cs`
6. `OrderService/Program.cs`

In 6 files mein poora project ka dimaag hai.

---

## 13. Abhi next level kya nahi karna

Jab tak yeh flow solid na ho, yeh mat add karo:

- Docker / Kubernetes
- RabbitMQ / Kafka
- API Gateway
- JWT / Authentication
- Redis
- Shared class library

Pehle yeh yaad ho jana chahiye:

1. Do independent services
2. Do independent databases
3. OrderService ProductService ko HTTP se call karta hai
4. ProductService band ho to order create fail hota hai
5. Data restart ke baad bhi rehta hai
