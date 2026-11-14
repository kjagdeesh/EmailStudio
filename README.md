# ✉️ Email Studio

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WebAssembly-512bd4?logo=blazor&logoColor=white)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![GitHub Pages](https://img.shields.io/badge/Deploy-GitHub%20Pages-222222?logo=githubpages&logoColor=white)](https://pages.github.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

**Email Studio** is a modern, privacy-first, browser-local email template designer built with **.NET 10** and **Blazor WebAssembly**. It allows designers, developers, and teams to build responsive, brand-aligned email templates with live visual previews and export them directly to production-ready **HTML**, portable **JSON**, or self-contained **C# static classes**.

Everything runs directly inside the client's browser using client-side WebAssembly and IndexedDB — no external backend, tracking, or cloud database required.

---

## 🌟 Key Features

### 🎨 Visual & Modular Template Designer
- **120+ Starter Templates:** Out-of-the-box designs across multiple categories including *Reports*, *Stocks*, *Tables*, *Announcements*, *Newsletters*, *Transactions*, and *Onboarding*.
- **32 Curated Color Themes:** One-click themes (*Indigo*, *Ocean*, *Forest*, *Sunset*, *Midnight*, *Citrus*, *Rose*, and more) with automatic WCAG contrast calculation for accessible typography.
- **Drag & Drop Blocks:** Reorder, customize, duplicate, and toggle block visibility seamlessly.
- **Responsive Preview:** Toggle between desktop and mobile viewport sizes with real-time live preview.

### 🧩 Rich Content Blocks
- **Branded Headers:** Customizable logo images or dynamic monogram badges with brand initials, kicker titles, and navigation links.
- **Typography & Content:** Multi-line text blocks with automatic line-break handling and styled section headers.
- **Call-to-Action Buttons:** High-converting button blocks with customizable URLs, background accents, and hover styling.
- **Adaptive Images:** Support for remote HTTPS image URLs alongside automatic branded placeholders when an image URL is pending.
- **Data Tables:** Pipe-delimited (`|`) format supporting *Classic*, *Striped*, and *Accent Header* styles (up to 100 rows and 12 columns).
- **Data Visualizations & Charts:** Inline charts rendered using lightweight HTML tables and SVG (with accessible fallback data tables):
  - Bar Charts & Comparison Charts
  - Forecast Projections (marking projected trends)
  - Line & Area Charts
  - Pie & Doughnut Charts
- **Dividers & Signatures:** Clean visual dividers and standardized team sign-offs with social and website links.

---

## 🚀 Multi-Format Export

Email Studio provides three export options tailored for both marketing workflows and software development:

### 1. Production HTML (`.html`)
Generates single-file, standards-compliant HTML emails with embedded inline styles, table-based layouts, and cross-client compatibility (Outlook, Gmail, Apple Mail, etc.).

### 2. Template JSON (`.json`)
Exports the entire design schema for backups, version control, or importing into another browser or Email Studio instance.

### 3. C# Static Class Generator (`.cs`)
Converts the entire email design into a **self-contained C# static class** using **string interpolation** (`$"""..."""`) and parameterized variables for every single text string, image URL, action link, and color.

#### Example Generated C# Usage:

```csharp
using EmailStudio.Templates;

// Option 1: Generate with default values
string html = WelcomeEmailTemplate.Render();

// Option 2: Override specific variables via parameters
string customHtml = WelcomeEmailTemplate.Render(
    brandName: "Acme Corp",
    headerTitle_1: "Welcome aboard, John!",
    buttonUrl_3: "https://acme.com/activate?token=xyz",
    imageUrl_4: "https://cdn.acme.com/welcome-hero.png"
);

// Option 3: Populate using strongly-typed EmailModel
var model = new WelcomeEmailTemplate.EmailModel
{
    BrandName = "Acme Corp",
    HeaderTitle_1 = "Welcome aboard!",
    TextBody_2 = "Thank you for joining our community.",
    ButtonText_3 = "Get Started",
    ButtonUrl_3 = "https://acme.com/start"
};

string emailHtml = WelcomeEmailTemplate.Render(model);

// Send directly via SendGrid, MailKit, FluentEmail, or AWS SES
await mailer.SendAsync("john@example.com", "Welcome!", emailHtml);
```

#### Why export as C#?
- **Zero Dependencies:** The exported `.cs` file requires only standard .NET namespaces (`System`, `System.Net`, `System.Text`).
- **High Performance:** Uses `StringBuilder` and string interpolation with compile-time safety.
- **Dynamic Content:** Every heading, paragraph, button, image URL, and table data point is exposed as a parameter or model property.
- **Safety:** Automatic HTML encoding (`WebUtility.HtmlEncode`) protects against injection vulnerabilities.

---

## 💾 Local Storage & Privacy

- **100% Client-Side:** Templates are stored exclusively in your browser's **IndexedDB** (`email-studio` database). No server tracking, analytics, or remote database storage.
- **Backup & Restore:** Use **Back up library** to export a comprehensive JSON archive (up to 10 MB) of all your custom designs.
- **Import:** Migrate legacy `App_Data/templates.json` files or restore previously exported libraries seamlessly.

---

## 🛠️ Project Structure

```text
EmailStudio/
├── .github/
│   └── workflows/
│       └── pages.yml             # GitHub Actions CI/CD for automated Pages deployment
├── Checks/
│   ├── EmailStudio.Checks.csproj # Automated regression and validation tests
│   └── Program.cs
├── Components/
│   ├── Layout/                   # Blazor Shell & Navigation layouts
│   ├── Pages/
│   │   ├── Home.razor            # Main editor & template library view
│   │   └── NotFound.razor
│   ├── AboutStudio.razor         # About page and developer portfolio
│   ├── ColorField.razor          # Custom color picker component
│   ├── StudioDocumentation.razor # Interactive guide and documentation
│   ├── StudioIcon.razor          # Feather-style SVG icons
│   └── StudioLanding.razor       # Welcome hero and onboarding
├── Models/
│   └── EmailTemplate.cs          # Core template, block, and theme schemas
├── Services/
│   ├── BrowserTemplateStorage.cs # IndexedDB storage implementation via JSInterop
│   ├── CSharpEmailExporter.cs    # C# static class generator engine
│   ├── EmailRenderer.cs          # Standalone HTML email rendering engine
│   ├── ITemplateStorage.cs       # Storage abstraction interface
│   ├── StarterTemplates.cs       # 120 curated seed templates
│   └── TemplateStore.cs          # State management, validation & palettes
├── wwwroot/
│   ├── app.css                   # Custom responsive stylesheet
│   ├── email-studio.js           # IndexedDB & file download helpers
│   ├── index.html                # Single-page application host
│   └── favicon.png
├── EmailStudio.csproj            # .NET 10 Blazor WebAssembly SDK project
├── EmailStudio.sln
└── README.md
```

---

## 💻 Running Locally

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download) or higher

### Build & Run
```powershell
# Navigate into the project folder
cd EmailStudio

# Run the Blazor WebAssembly application
dotnet run
```
Open the localhost URL printed in your console (e.g., `https://localhost:5001` or `http://localhost:5000`).

### Run Automated Tests & Checks
```powershell
dotnet run --project Checks/EmailStudio.Checks.csproj
```

---

## 🌐 Deploying to GitHub Pages

This repository includes an automated GitHub Actions workflow (`.github/workflows/pages.yml`) that builds and deploys the Blazor WebAssembly application directly to GitHub Pages on every push to the `main` branch.

### One-Time Setup on GitHub:
1. Go to your repository on GitHub: `https://github.com/<your-username>/EmailStudio/settings/pages`.
2. Under **Build and deployment**:
   - Change **Source** to **"GitHub Actions"**.
3. Push changes or run the workflow manually from the **Actions** tab.

Your site will be live at:
`https://<your-username>.github.io/EmailStudio/`

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
