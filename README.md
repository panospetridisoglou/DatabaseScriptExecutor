# 📂 DatabaseScriptExecutor

A lightweight and flexible .NET 9.0 tool to execute database scripts efficiently.  
Designed for automating schema updates, applying seed data, or running batch SQL scripts — with first-class support for **PostgreSQL**.

---

## 📦 Requirements

- [.NET 9.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/) (running locally or remotely)

---

## 🛠️ Usage

Your SQL scripts should have at the top 4 lines the following comments
<br>
Creation Date and Target Database comments are **required**
<br>
Example:
```sql
-- Creation Date: 2028-09-13
-- Target Database: TEST1
-- Target Table: TABLE1 
-- Creator: p.petridisoglou
```
Run:
```bash
.\DatabaseScriptExecutor.exe "G:\C# Projects\Sql Scripts"
```
> [!Note]
> The execution order is happening based on the Creation Date

## ⚡ Getting Started

Clone the repository:

```bash
git clone https://github.com/your-username/DatabaseScriptExecutor.git
cd DatabaseScriptExecutor
```

Run the project:
```bash
dotnet restore
dotnet run --project src/DatabaseScriptExecutor
```

