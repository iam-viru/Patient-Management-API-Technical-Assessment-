# Patient Management API

A .NET 10 ASP.NET Core Web API (no UI) for managing patients and their consultations.
Data is stored in SQL Server through EF Core, patient pictures go to Azure Blob Storage, and a dummy
email is logged when a patient is created. Swagger UI is enabled in every environment, and the site root (`/`)
redirects to it.

- **Swagger:** `https://<app-name>.azurewebsites.net/swagger`
- **Health:** `https://<app-name>.azurewebsites.net/health`

## Client brief mapped to the implementation

| Brief | How it is satisfied |
|-------|---------------------|
| .NET 10 Web API, SQL Server, EF Core | ASP.NET Core controllers on `net10.0`, EF Core SQL Server provider, code-first `InitialCreate` migration |
| Create patient with name, age, gender and picture | `POST /api/patients` (`multipart/form-data`, usable from Swagger as the "form") |
| Picture stored in Azure Blob Storage | Uploaded with `Azure.Storage.Blobs` to a private container; the blob URL is saved as `PhotoUrl` |
| Dummy email after a patient is created | `IEmailService` implemented by `LoggingEmailService`, which writes the email to the log |
| View patient list | `GET /api/patients?page=1&pageSize=10` (paged) |
| Fetch and update patient data | `GET /api/patients/{id}` (includes consultations), `PUT /api/patients/{id}` |
| Fetch and update consultation data | `GET /api/patients/{patientId}/consultations`, `GET` / `PUT /api/consultations/{id}`, plus `POST /api/patients/{patientId}/consultations` to add one |
| Deploy to Azure App Service with a Swagger page | GitHub Actions workflow deploys to App Service; Swagger is on in Production |
| Use AI and follow best practices | DTOs, validation, ProblemDetails, async with `CancellationToken`, Options pattern, health check, automated tests, no secrets in the repo |

## Endpoints

| Method | Route | Purpose | Success |
|--------|-------|---------|---------|
| POST | `/api/patients` | Create a patient (form: `Name`, `Age`, `Gender`, `Email?`, `Photo`); uploads the photo, saves, sends the dummy email | 201 + `Location` |
| GET | `/api/patients` | List patients ordered by id (`page` >= 1, `pageSize` 1-50, default 10) | 200 |
| GET | `/api/patients/{id}` | Get one patient with their consultations | 200 |
| PUT | `/api/patients/{id}` | Replace a patient's details; `Photo` optional (the old picture is kept if omitted, deleted if replaced) | 200 |
| POST | `/api/patients/{patientId}/consultations` | Add a consultation to a patient (JSON) | 201 + `Location` |
| GET | `/api/patients/{patientId}/consultations` | List a patient's consultations, oldest first | 200 |
| GET | `/api/consultations/{id}` | Get one consultation | 200 |
| PUT | `/api/consultations/{id}` | Replace a consultation's details (JSON) | 200 |
| GET | `/health` | Health check, including database connectivity | 200 |

Errors: 400 with `ValidationProblemDetails` for invalid input, 404 `ProblemDetails` for an unknown patient or
consultation, and a generic 500 `ProblemDetails` for anything unexpected.

Validation rules: `Name` required (max 100), `Age` 0-120, `Gender` one of `Male`, `Female`, `Other`, `Email`
optional but must be a valid address, `Photo` `.jpg` / `.jpeg` / `.png` / `.webp` up to 2 MB with a content type
matching the extension. Consultation: `ConsultationDate` required, `Notes` max 2000, `Diagnosis` max 500.

## Tech stack

- .NET 10 (SDK pinned in `global.json`), ASP.NET Core controllers
- EF Core 10 with the SQL Server provider (Azure SQL in the cloud)
- Azure Blob Storage (`Azure.Storage.Blobs`)
- Swashbuckle (Swagger / OpenAPI, with XML doc comments)
- ASP.NET Core health checks (`AddDbContextCheck`)
- xUnit, NSubstitute, `WebApplicationFactory` and SQLite in-memory for tests
- GitHub Actions for CI/CD to Azure App Service

## Project structure

```
src/PatientManagement.Api
  Controllers/      PatientsController, ConsultationsController (thin, delegate to services)
  Data/             AppDbContext, Fluent API configurations, Migrations, design-time factory
  ErrorHandling/    GlobalExceptionHandler (500 ProblemDetails)
  Extensions/       Service registration, Swagger setup, optional migrate-on-startup
  Models/Entities   Patient, Consultation, Gender
  Models/Dtos       Request/response DTOs, pagination, entity-to-DTO mappings
  Options/          BlobStorageOptions, EmailOptions
  Services/         PatientService, ConsultationService, AzureBlobStorageService, LoggingEmailService
  Validation/       AllowedImageAttribute (photo type and size)
tests/PatientManagement.Api.Tests
  Unit tests for services, validation, error handling and the EF model, plus integration tests for every endpoint
.github/workflows/ci-cd.yml
```

## Design decisions

- **Create flow:** validate the form, upload the photo under a generated name (`{guid}{ext}`, never the user's file
  name), save the patient with the blob URL, send the dummy email, return 201 with the `PatientResponse`.
- **Orphan blob cleanup:** if saving to the database fails after the upload, the uploaded blob is deleted
  (best effort) and the original error is rethrown. On update, the old picture is deleted only after the new URL
  has been saved, so a patient never points at a missing blob.
- **Email failure does not fail the request:** the patient is already saved, so an exception from the email service
  is logged and the request still returns 201. With no `Email` on the patient, the email is skipped and logged.
- **DTOs only:** EF entities are never returned; requests and responses are separate DTO types.
- **ProblemDetails everywhere:** validation errors, 404s, empty error responses (unknown route, 405) and unhandled
  exceptions all return ProblemDetails. The global handler logs the exception and returns a generic message, so stack
  traces or SQL errors never reach the client.
- **Private container:** the blob container is created on first upload with no public access. `PhotoUrl` is the
  blob URL, which is not anonymously readable; serving pictures to clients (for example via short-lived SAS URLs) is
  an open question for the client.
- **Configuration:** Options pattern for blob and email settings; read queries use `AsNoTracking()`; everything is
  async with `CancellationToken`.
- **Migrations:** applied with `dotnet ef database update`, or on startup when `Database:ApplyMigrationsOnStartup`
  is `true` (intended for this demo deployment only).

## Assumptions

| # | Assumption |
|---|------------|
| A1 | Patient fields are `Name`, `Age`, `Gender`, `Email` (optional) and `PhotoUrl` |
| A2 | Consultation fields are `ConsultationDate`, `Notes` and `Diagnosis` (plus `Id`, `PatientId`, `CreatedAtUtc`) |
| A3 | One patient has many consultations |
| A4 | "Dummy email" means an `IEmailService` abstraction with a logging implementation |
| A5 | Updates are full replacements with `PUT` |
| A6 | Pictures must be jpg/jpeg, png or webp, max 2 MB |
| A7 | Deliverables are this repository, a live Swagger URL and this README |
| A8 | No authentication (not requested). Automated tests were added even though they were not requested |

## Open questions for the client

1. Which consultation fields are needed, and is a create endpoint wanted?
2. PUT or PATCH for updates?
3. Should the dummy email only be logged or use a real provider, and who should receive it?
4. Picture limits, and should clients get the blob URL or a short-lived SAS URL?
5. Whose Azure subscription should host the deployment?
6. Deliverable format: repository, live URL, or both?
7. Expectations for tests, authentication and pagination.

## Local setup

### Prerequisites

- .NET 10 SDK (see `global.json`)
- SQL Server or SQL Server LocalDB
- An Azure Storage account, or [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) locally
- EF Core CLI: `dotnet tool install --global dotnet-ef`

### Configure secrets

Secrets are kept out of the repository with user secrets. `init` is needed once, because the project has no
`UserSecretsId` yet.

```bash
dotnet user-secrets init --project src/PatientManagement.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\\mssqllocaldb;Database=PatientManagement;Trusted_Connection=True;TrustServerCertificate=True" --project src/PatientManagement.Api
dotnet user-secrets set "BlobStorage:ConnectionString" "UseDevelopmentStorage=true" --project src/PatientManagement.Api
dotnet user-secrets set "BlobStorage:ContainerName" "patient-photos" --project src/PatientManagement.Api
```

`UseDevelopmentStorage=true` targets Azurite; use your storage account's connection string otherwise.
`BlobStorage:ContainerName` already defaults to `patient-photos` in `appsettings.json`.

### Create the database

The design-time DbContext factory has no connection string, so pass it explicitly:

```bash
dotnet ef database update --project src/PatientManagement.Api --connection "<your-connection-string>"
```

Alternatively set `Database:ApplyMigrationsOnStartup` to `true` and the app migrates when it starts.

### Run

```bash
dotnet run --project src/PatientManagement.Api --launch-profile http
```

Open http://localhost:5010/swagger (or http://localhost:5010, which redirects there).

### Test

The tests use SQLite in memory and substitutes for Blob Storage, so they need no SQL Server or Azure account.

```bash
dotnet test
# A single test class or test:
dotnet test --filter "FullyQualifiedName~PatientsControllerTests"
dotnet test --filter "FullyQualifiedName~GlobalExceptionHandlerTests.TryHandleAsync_UnhandledException_Writes500ProblemDetailsWithoutExceptionMessage"
```

## Deploy to Azure App Service

All names below are placeholders; replace them with your own.

### 1. Create the resources

```bash
az login
az group create --name <resource-group> --location <location>

# Azure SQL
az sql server create --resource-group <resource-group> --name <sql-server> --location <location> \
  --admin-user <sql-admin-user> --admin-password "<sql-admin-password>"
az sql db create --resource-group <resource-group> --server <sql-server> --name <database> --service-objective Basic

# Storage account (blob public access disabled; the app creates the private container on first upload)
az storage account create --resource-group <resource-group> --name <storage-account> --location <location> \
  --sku Standard_LRS --allow-blob-public-access false
az storage account show-connection-string --resource-group <resource-group> --name <storage-account> --output tsv

# App Service (Linux, .NET 10)
az appservice plan create --resource-group <resource-group> --name <app-service-plan> --is-linux --sku B1
az webapp create --resource-group <resource-group> --plan <app-service-plan> --name <app-name> --runtime "DOTNETCORE:10.0"
```

### 2. SQL firewall

App Service must be allowed to reach Azure SQL. The `0.0.0.0` rule is Azure's "Allow Azure services and resources
to access this server" switch:

```bash
az sql server firewall-rule create --resource-group <resource-group> --server <sql-server> \
  --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
```

To run `dotnet ef database update` from your machine instead of migrating on startup, also add a rule for your
own public IP.

### 3. Application settings

Use the double-underscore form, which App Service exposes as environment variables:

| Setting | Value |
|---------|-------|
| `ConnectionStrings__DefaultConnection` | `Server=tcp:<sql-server>.database.windows.net,1433;Database=<database>;User ID=<sql-admin-user>;Password=<sql-admin-password>;Encrypt=True;` |
| `BlobStorage__ConnectionString` | Output of `az storage account show-connection-string` |
| `BlobStorage__ContainerName` | `patient-photos` |
| `Database__ApplyMigrationsOnStartup` | `true` |

```bash
az webapp config appsettings set --resource-group <resource-group> --name <app-name> --settings \
  ConnectionStrings__DefaultConnection="<sql-connection-string>" \
  BlobStorage__ConnectionString="<storage-connection-string>" \
  BlobStorage__ContainerName="patient-photos" \
  Database__ApplyMigrationsOnStartup="true"
```

### 4. Deploy with GitHub Actions

`.github/workflows/ci-cd.yml` runs on pushes to `main`, pull requests and manual dispatch. The **build** job
restores, builds in Release, runs the tests, publishes the API and uploads it as an artifact. The **deploy** job runs
only on pushes to `main` and only when `AZURE_WEBAPP_NAME` is set, so CI stays green before Azure exists.

Configure the repository (Settings > Secrets and variables > Actions):

| Kind | Name | Value |
|------|------|-------|
| Variable | `AZURE_WEBAPP_NAME` | `<app-name>` |
| Secret | `AZURE_WEBAPP_PUBLISH_PROFILE` | The publish profile XML from the command below |

```bash
# New App Services disable basic-auth publishing by default; the publish profile needs it enabled
az resource update --resource-group <resource-group> --namespace Microsoft.Web --resource-type basicPublishingCredentialsPolicies \
  --parent sites/<app-name> --name scm --set properties.allow=true
az webapp deployment list-publishing-profiles --resource-group <resource-group> --name <app-name> --xml
```

Then push to `main` to build, test and deploy. (A manual run from the Actions tab builds and tests only; the
deploy job runs for pushes.) A manual alternative without GitHub Actions:

```bash
dotnet publish src/PatientManagement.Api -c Release -o publish
cd publish && zip -r ../api.zip . && cd ..
az webapp deploy --resource-group <resource-group> --name <app-name> --src-path api.zip --type zip
```

### 5. Verify

Open `https://<app-name>.azurewebsites.net/swagger`, then create a patient with a photo, list patients, update the
patient, add and update a consultation, and check `/health`. The dummy email appears in the App Service log stream
(`az webapp log tail --resource-group <resource-group> --name <app-name>`).

## Security notes

- No secrets are committed: `appsettings.json` contains empty connection strings; real values come from user
  secrets locally and App Service application settings in Azure.
- The blob container is private, and stored file names are generated, never taken from the upload.
- Unhandled errors return a generic 500 without exception details; full details go to the server log.
- Future improvements: Managed Identity for Blob Storage and Azure SQL (removing connection strings with keys and
  passwords), OIDC federated credentials for GitHub Actions instead of a publish profile, SAS URLs for pictures,
  authentication and authorization, and running migrations from the pipeline instead of on startup.
