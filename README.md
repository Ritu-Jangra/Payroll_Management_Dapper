# 💼 Payroll Management System

A web-based **Payroll Management System** built using **ASP.NET Core Web API, ASP.NET Core MVC, Dapper ORM, and SQL Server** to manage employees, salaries and leave requests with secure role-based access control.

## 🛠️ Tech Stack

- **Backend:** ASP.NET Core Web API (.NET 8 / .NET 9)
- **Frontend:** ASP.NET Core MVC
- **Database:** SQL Server
- **ORM / Micro-ORM:** Dapper
- **Authentication:** JWT and Refresh Tokens
- **Validation:** FluentValidation
- **API Documentation:** Swagger / OpenAPI
- **Data Access:** Dapper with SQL queries and stored procedures

## ✨ Features

### 👨‍💼 Employee Management
- Create, view, update, and manage employee records.
- Assign reporting managers to employees.
- View employee profiles based on user permissions.

### 💰 Payroll Management
- Maintain employee salary structures.
- Manage allowances and deductions.
- Calculate employee net salaries.
- View salary information based on assigned roles.

### 🏖️ Leave Management
- Apply for leave and track request status.
- Enable managers to approve or reject team leave requests.
- Allow administrators to manage manager leave requests.
- Prevent unauthorized approvals and self-approval.

### 🔐 Role-Based Access Control
- Separate permissions for Admin, Manager, and Employee.
- Restrict access to employee, salary, and leave records.
- Secure application pages and API endpoints based on user roles.

### 🔑 Secure Authentication
- JWT-based authentication.
- Refresh token support.
- Role-based authorization.
- Protected API endpoints and application resources.

### ⚙️ Data Handling
- Efficient database operations using Dapper.
- Parameterized SQL queries to help prevent SQL injection.
- Pagination, filtering, and sorting.
- Request validation using FluentValidation.
- Centralized exception handling.
- Swagger / OpenAPI documentation.

## 👥 User Roles and Permissions

| Feature | Admin | Manager | Employee |
|---|---|---|---|
| Manage employees | ✅ | ❌ | ❌ |
| Manage salary structures | ✅ | ❌ | ❌ |
| View all employee records | ✅ | ❌ | ❌ |
| View team members | ✅ | ✅ | ❌ |
| View own profile | ✅ | ✅ | ✅ |
| View own salary | ✅ | ✅ | ✅ |
| Apply for leave | ✅ | ✅ | ✅ |
| Approve team leave | ❌ | ✅ | ❌ |
| Approve manager leave | ✅ | ❌ | ❌ |
| Track leave status | ✅ | ✅ | ✅ |

## 🏗️ Architecture

The application uses a layered architecture to separate presentation, business logic, and database access.

```text
ASP.NET Core MVC Frontend
          |
          | HTTP Requests
          v
ASP.NET Core Web API
          |
          v
     Controllers
          |
          v
       Services
          |
          v
     Repositories
          |
          v
        Dapper
          |
          v
      SQL Server
```

### Architecture Components

- **ASP.NET Core MVC:** Provides the user interface using Models, Views, and Controllers.
- **ASP.NET Core Web API:** Exposes endpoints for employee, payroll, and leave management operations.
- **Controllers:** Handle incoming requests and return appropriate responses.
- **Services:** Contain business logic and application workflows.
- **Repositories:** Encapsulate database access and execute SQL queries using Dapper.
- **Dapper:** Maps SQL query results to C# objects with minimal overhead.
- **SQL Server:** Stores employee, salary, payroll, and leave records.

## 🔄 Leave Approval Workflow

1. Employees submit leave requests to their assigned reporting managers.
2. Managers review, approve, or reject team leave requests.
3. Managers submit their own leave requests to the Admin.
4. Admins approve or reject manager leave requests.
5. Users can track the status of their leave requests.
6. Only authorized users can approve or reject requests.
7. Self-approval is not allowed.

## 🚀 Getting Started

### Prerequisites

Install the following tools before running the application:

- [.NET SDK](https://dotnet.microsoft.com/download)
- [SQL Server](https://www.microsoft.com/en-us/sql-server)
- [SQL Server Management Studio (SSMS)](https://learn.microsoft.com/en-us/ssms/install/install)
- Visual Studio or Visual Studio Code

### 1. Clone the Repository

```bash
git clone YOUR_REPOSITORY_URL
cd YOUR_PROJECT_FOLDER
```

Replace the placeholders with your actual repository URL and project folder name.

### 2. Configure the Database

Update the SQL Server connection string in the relevant `appsettings.json` file.

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=PayrollManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Replace `YOUR_SERVER_NAME` with your SQL Server instance name.

Ensure that the required database, tables, and stored procedures exist if your application uses them.

### 3. Restore Dependencies

Open a terminal in the solution directory and run:

```bash
dotnet restore
dotnet build
```

### 4. Run the Web API

Navigate to the directory containing the Web API `.csproj` file.

```bash
dotnet run
```

Note the API URL and port displayed in the terminal.

### 5. Configure and Run the MVC Frontend

Update the MVC project's configuration with the correct Web API base URL if the MVC application communicates with the API over HTTP.

Navigate to the directory containing the MVC `.csproj` file and run:

```bash
dotnet restore
dotnet run
```

Open the MVC application using the local URL displayed in the terminal.

> **Note:** If the MVC application accesses SQL Server directly through Dapper instead of calling the Web API, configure its database connection string accordingly. Adjust the startup commands and configuration to match your solution structure.

## 🔌 API Documentation

Swagger / OpenAPI can be used to explore and test API endpoints when enabled in the Web API project.

After starting the API, open the configured Swagger URL, typically:

`https://localhost:<port>/swagger`

Replace `<port>` with the actual port shown in the terminal.

## 🔒 Security Considerations

- Use parameterized SQL queries with Dapper.
- Enforce authentication and role-based authorization.
- Protect sensitive employee and salary information.
- Store secrets and connection strings securely.
- Validate incoming requests before processing them.
- Prevent unauthorized leave approvals and self-approval.
- Use HTTPS in production environments.
- Implement secure refresh token expiration and revocation where applicable.

## 📁 Project Structure

A typical project structure may look like this:

```text
Payroll-Management-System/
├── PayrollManagement.API/
│   ├── Controllers/
│   ├── Services/
│   ├── Repositories/
│   ├── Models/
│   ├── DTOs/
│   ├── Validators/
│   ├── Program.cs
│   └── appsettings.json
│
├── PayrollManagement.MVC/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/
│   ├── wwwroot/
│   ├── Program.cs
│   └── appsettings.json
│
└── PayrollManagement.sln
```

This is an example structure; your actual folder and project names may differ.

## 👩‍💻 Author

**Ritu Jangra**

- GitHub: [@Ritu-Jangra](https://github.com/Ritu-Jangra)

## 🤝 Contributing

Contributions and suggestions are welcome.

1. Fork the repository.
2. Create a feature branch.
3. Implement your changes.
4. Commit and push your changes.
5. Open a pull request.

## 📄 License

No license information is specified. Add a `LICENSE` file if you intend to distribute this project under an open-source license.

## ⭐ Support

If you find this project useful, consider giving the repository a star on GitHub!
