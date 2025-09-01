
## AI-Generated Project ⚡️


> **Project Goal:** The primary goal of this project was to use AI tools—specifically Cursor and GitHub Copilot—to generate a complete, production-ready software application. All code, structure, and documentation were produced with the assistance of these tools to showcase the capabilities and best practices of modern AI-driven software development.

---

## ReviewSharp 🚀

ReviewSharp is an extensible ASP.NET MVC application for automated code review of C# projects. It supports file, and zip uploads, performs code quality checks using Roslyn analyzers, and provides detailed review results in a user-friendly interface.

---
## Video Demo 
https://drive.google.com/drive/folders/1z45Czz-6p9XnVLdSYayt2vUgECLXFIpb?usp=sharing  

The demo video showcases how ReviewSharp works in action — from uploading files and reviewing C# code, to viewing the analysis results in a clear and interactive interface.

---
## Features


- 📁 Upload single C# files, or zipped solutions
- 🤖 Automated code analysis using Roslyn
- 🧩 Extensible service architecture for custom review rules
- 📊 Detailed review results by file, including rule name, message, severity, and line number
- 💻 Modern, responsive UI with clear error handling
- 🧪 Comprehensive unit and controller tests

---

## Getting Started

### Prerequisites 🛠️

- .NET 6.0 SDK or later
- Visual Studio 2022 or VS Code
- Windows, macOS, or Linux


### Installation 🏗️

1. Clone the repository:
	```sh
	git clone https://github.com/sabriNoor/Review-Sharp.git
	cd ReviewSharp
	```

2. Navigate to the application folder:
	```sh
	cd ReviewSharpApp
	```

3. Restore dependencies:
	```sh
	dotnet restore
	```

4. Build the project:
	```sh
	dotnet build
	```

5. Run the application:
	```sh
	dotnet run
	```

6. Open your browser and navigate to `http://localhost:5000` (or the port shown in the console).

---

## Usage 📝

- Navigate to the home page.
- Upload a `.cs` file, or a zipped solution.
- View detailed code review results for each file.
- Click on individual files to see line-by-line feedback.

---

## Project Structure 🗂️


```
ReviewSharp/
│
├── ReviewSharpApp/
│   ├── Controllers/         # MVC controllers (main: CodeReviewController)
│   ├── Models/              # Data models for review results and errors
│   ├── Services/            # Business logic and file processing services
│   ├── Views/               # Razor views for UI
│   ├── wwwroot/             # Static files (CSS, JS, libraries)
│
├── ReviewSharpApp.Tests/    # Unit and controller tests
│   ├── ControllerTests/     # Controller test classes
│   ├── ServiceTests/        # Service test classes
│   ├── TestHelpers/         # Test utility classes
│   
```
---

## Testing 🧪

Run all tests with:
```sh
dotnet test
```
---

## Extending ➕

- Add new review services by implementing either `ICodeReviewService` or `ICodeReviewSemanticService` for custom review logic.
- Register your new service in the dependency injection container.

---

## Contributing 🤝

Contributions are welcome! Please open issues or submit pull requests for improvements and bug fixes.

---

## License 📄

This project is licensed under the MIT License.


