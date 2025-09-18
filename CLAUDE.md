# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NodeSwap is a Windows-only Node.js version manager written in C# (.NET 8.0). It's similar to NVM but specifically designed for Windows. The application uses symlinks to manage different Node.js versions and requires administrator privileges for symlink creation.

## Architecture

### Solution Structure
- **NodeSwap** - Main console application (.NET 8.0-windows)
- **NodeSwap.Tests** - MSTest unit tests using Shouldly assertions
- **NodeSwap.Installer** - WiX installer project for Windows

### Core Components
- **Program.cs** - Entry point with dependency injection setup using Microsoft.Extensions.DependencyInjection
- **GlobalContext.cs** - Shared configuration and paths (storage, symlink, version tracking)
- **Commands/** - CLI command implementations using DotMake.CommandLine with full dependency injection
- **Interfaces/** - Abstraction interfaces for external dependencies (file system, process elevation, etc.)
- **Services/** - Service implementations wrapping external dependencies
- **NodeJs.cs** & **NodeJsWebApi.cs** - Node.js version management and API integration
- **Utils/** - Helper utilities for console output, process elevation, and list operations

### Dependency Injection Architecture
The application uses comprehensive dependency injection for improved testability:

#### Interfaces (NodeSwap/Interfaces/)
- **IProcessElevation** - Windows process elevation and administrator checks
- **IConsoleWriter** - Console output abstraction
- **IFileSystem** - File system operations (read/write/delete/symlinks)
- **INodeJsWebApi** - Node.js API integration for version lookup and downloads
- **INodeJs** - Local Node.js version management

#### Services (NodeSwap/Services/)
- **ProcessElevationService** - Windows-specific elevation implementation
- **ConsoleWriterService** - Console.WriteLine wrapper
- **FileSystemService** - System.IO wrapper with symlink support
- **NodeJsWebApiService** - HTTP-based Node.js API client
- **NodeJsService** - Local version management implementation

### Key Dependencies
- **DotMake.CommandLine** - Primary CLI framework
- **System.CommandLine** - Additional command line support
- **NuGet.Versioning** - Version parsing and comparison
- **ShellProgressBar** - Progress indication for downloads
- **Microsoft.Extensions.DependencyInjection** - Dependency injection container

## Development Commands

### Building
```bash
dotnet build NodeSwap.sln
dotnet build -c Release NodeSwap.sln
```

### Testing
```bash
dotnet test NodeSwap.Tests/
dotnet test --verbosity normal
```

### Publishing
```bash
dotnet publish NodeSwap/NodeSwap.csproj -c Release
```

### Running Locally
```bash
dotnet run --project NodeSwap/
```

## Environment Requirements

### Prerequisites
- .NET 8.0 runtime or SDK
- Windows operating system (uses Windows-specific symlinks)
- `NODESWAP_STORAGE` environment variable must be set to a valid directory path

### Admin Privileges
The `use` command requires administrator privileges to create/update symlinks. The application will prompt for elevation when needed.

## Key Implementation Details

### Version Management
- Node.js versions are downloaded and stored in `%NODESWAP_STORAGE%`
- Active version tracked via symlink at `%NODESWAP_STORAGE%/current`
- Version history maintained in `last-used` and `previous-used` files
- Supports fuzzy version matching (e.g., "22" → "22.x.x")

### Command Structure
All commands use dependency injection and inherit from DotMake.CommandLine patterns:
- `list` - Show installed versions
- `avail [min_version]` - Show available downloads
- `install <version>` - Download and install Node.js version
- `uninstall <version>` - Remove installed version
- `use <version>` - Switch active version (requires admin)
- `prev` - Switch to previously used version
- `file` - Use or create a .nodeswap file to manage Node.js version for the current directory

#### Command Dependencies
All commands now accept required dependencies through constructor injection:
- **UseCommand** - Takes GlobalContext, INodeJs, IProcessElevation, IConsoleWriter, IFileSystem
- **InstallCommand** - Takes GlobalContext, INodeJsWebApi, INodeJs, IConsoleWriter, IFileSystem
- **PrevCommand** - Takes GlobalContext, INodeJs, IProcessElevation, IConsoleWriter, IFileSystem
- **FileCommand** - Takes GlobalContext, INodeJs, IProcessElevation, IConsoleWriter, IFileSystem

### Testing Architecture

#### Test Framework
- **MSTest** framework with **Shouldly** assertions
- Comprehensive mock-based testing for isolation
- 60+ unit tests covering all commands and utilities

#### Mock Services (NodeSwap.Tests/TestUtils/MockServices.cs)
Custom mock implementations for complete test isolation:
- **MockProcessElevation** - Configurable administrator status and elevation behavior
- **MockConsoleWriter** - Captures output and error messages for verification
- **MockFileSystem** - In-memory file system with symlink simulation
- **MockNodeJs** - Configurable installed versions and active version tracking
- **MockNodeJsWebApi** - Configurable API responses and exception testing

#### Test Structure
- **Command Tests** - All commands have comprehensive test coverage using mocks
- **Utility Tests** - Version parsing, list operations, and helper functions
- **Integration-style Tests** - Some tests use MockServices with dependency injection
- **Error Scenario Testing** - Exception handling, validation, and edge cases

#### Key Test Files
- **FileCommandTests.cs** - Tests for .nodeswap file management (6 tests)
- **UseCommandTests.cs** - Tests for version switching with mocks (7 tests)
- **UseCommandTestsWithMocks.cs** - Additional comprehensive use command tests (10 tests)
- **InstallCommandTestsWithMocks.cs** - Installation command tests (8 tests)
- **PrevCommandTests.cs** - Previous version switching tests (6 tests)
- **ListCommandTests.cs**, **UninstallCommandTests.cs**, **AvailCommandTests.cs** - Additional command coverage

#### Running Tests
```bash
# Run all tests
dotnet test NodeSwap.Tests/ --verbosity normal

# Run specific test class
dotnet test NodeSwap.Tests/ --filter "FileCommandTests" --verbosity normal

# Run tests before making changes to core logic
dotnet test NodeSwap.Tests/
```