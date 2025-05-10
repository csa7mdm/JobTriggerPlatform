# JobTriggerPlatform Test Suite

This project contains unit tests for the JobTriggerPlatform application to ensure code quality and maintain at least 70% code coverage.

## Test Structure

The tests are organized to mirror the application's structure:

- **Domain Tests**: Tests for domain entities like `ApplicationUser` and `ApplicationRole`
- **Application Tests**: Tests for application layer components 
  - `PluginParameter` and `PluginResult` tests
  - `IJobTriggerPlugin` interface tests
- **Infrastructure Tests**: Tests for infrastructure components like `RoleSeeder`, `UserSeeder`, and `DatabaseInitializer`
- **WebApi Tests**: Tests for API controllers and related components
  - **Controllers**: Tests for all API controllers
  - **Endpoints**: Tests for minimal API endpoints
  - **Helpers**: Tests for helper classes like `QrCodeGenerator`

## Running Tests

### Using Visual Studio

1. Open the solution in Visual Studio
2. Right-click on the `JobTriggerPlatform.Tests` project in Solution Explorer
3. Select "Run Tests"

### Using the Command Line

Run the tests from the root directory of the project:

```bash
dotnet test src/JobTriggerPlatform.Tests/JobTriggerPlatform.Tests.csproj
```

## Generating Code Coverage Report

The test project is already configured with Coverlet for code coverage. To generate a code coverage report:

```bash
dotnet test src/JobTriggerPlatform.Tests/JobTriggerPlatform.Tests.csproj /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura
```

This will generate a `coverage.cobertura.xml` file in the test project directory. You can use tools like ReportGenerator to convert this to a more readable HTML report:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"src/JobTriggerPlatform.Tests/coverage.cobertura.xml" -targetdir:"coverage-report" -reporttypes:Html
```

Then open the generated HTML report in your browser.

## Important Notes on Class Usage

The application uses these primary classes for plugin functionality:

- `PluginParameter`: For defining parameters for jobs (replaces the older `JobTriggerParameter`)
- `PluginResult`: For returning results from job execution (replaces the older `JobTriggerResult`)
- `ParameterType`: Enum defined in `PluginParameter.cs` that specifies parameter types

Make sure to use these classes consistently in tests and implementation code to avoid duplicate type definition errors.

## Achieving 70% Code Coverage

The current test suite is designed to achieve at least 70% code coverage across the codebase. The main areas covered are:

1. **Controllers**: All controller endpoints have tests for both success and failure scenarios
2. **Domain Models**: Essential properties and methods are tested
3. **Infrastructure Components**: Core functionality is tested
4. **Application Abstractions**: Interfaces and model classes are thoroughly tested

### Key Areas Tested for Maximum Coverage

- Authentication & Authorization
- Role and User Management
- Job Access Control
- Job Triggering
- Two-Factor Authentication
- Plugin Parameters and Results

### Further Increasing Coverage

To further increase coverage beyond 70%, consider adding:

1. More integration tests using `WebApplicationFactory`
2. Tests for edge cases in authorization policies
3. Additional tests for middleware components
4. Tests for helper methods that aren't directly exposed through controllers

## Maintaining Tests

When adding new features:

1. Add corresponding test cases
2. Run the coverage report to ensure coverage remains above 70%
3. If coverage drops, add additional tests targeting uncovered code

## Notes for Debugging Tests

- Most tests use mocking (via Moq) to isolate components
- For controllers, authentication is simulated using `ClaimsPrincipal`
- Helper classes in `Helpers` folder are used to simplify test setup
- Be careful about type name clashes - always use the types defined in the application rather than redefining them

