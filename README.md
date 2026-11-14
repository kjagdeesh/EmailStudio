# Email Studio

A browser-local .NET 10 Blazor WebAssembly email template designer.

## Run

Install the .NET 10 SDK, then from this directory run:

```powershell
dotnet run
```

Open the localhost address printed by the application. Templates are stored in the browser's IndexedDB for that site, not on the server. They are separate for each browser, profile, and site origin, and can be lost when browser data is cleared or evicted. Use **Back up library** to download a JSON backup regularly. Library backups are limited to 10 MB; larger libraries must be exported as individual designs. Anyone using the same browser profile can access its library.

To migrate an existing server library, use **Import JSON** and select the old `App_Data\templates.json` (or its configured equivalent). Importing a library preserves existing browser designs: identical designs are skipped and changed designs with matching IDs are added as copies. Library imports are limited to 10 MB.

Run the focused checks with `dotnet run --project Checks\EmailStudio.Checks.csproj`.

## Deploy to GitHub Pages

Push this project to a GitHub repository, enable **Settings → Pages → Build and deployment → GitHub Actions**, and run the included **Deploy to GitHub Pages** workflow. It runs on pushes to `main` and can also be started manually. It publishes only the static WebAssembly assets and configures the correct base path for a project or user site. The workflow does not upload the old server library. Import it separately in the deployed site if needed.

## Create and reuse

The home page introduces Email Studio and links to the template library and editor. The **Documentation** tab in the app contains a guide to editing, tables, charts, importing, exporting, and local storage. The About page includes a local snapshot of the developer's pinned GitHub projects; follow its profile link for the current selection.

Browse or search the 120 starter templates, open one to edit it, duplicate it, or create a new template. Each starter includes an editable ABC Company identity, a dummy monogram logo, a sign-off, and an image placeholder. Changes are not saved automatically: select **Save design** before leaving or exporting. Apply one of 32 palettes, or set the page, surface, text, accent, and individual block colors. Applying a theme replaces all block colors; changing a global surface, text, or accent color applies it to every block. Add and remove blocks using the content panel; reorder them by dragging the handle or using the move buttons, which also work with a keyboard and on touch devices. Select a block to edit it.

Table content uses one row per line with columns separated by `|`; the first row is the header. Choose a classic, striped, or accent-header table design. Tables support up to 100 rows and 12 columns. Chart content uses one `label,value` pair per line, with up to 50 non-negative numeric values. Forecast chart rows may add `,forecast` to mark projections; the app displays provided values and does not calculate forecasts. Bar, comparison, and forecast charts use HTML tables. Line, area, pie, and doughnut charts use inline SVG with a visible data table as a fallback; some email clients strip SVG. Charts are static, not interactive.

Export the saved template as HTML for use in an email workflow or JSON for later editing. Import accepts JSON exported by Email Studio (up to 1 MB) and creates a new copy. Arbitrary HTML and third-party template formats cannot be imported. Add publicly accessible HTTP(S) URLs for logos and photos; image blocks show branded placeholder artwork until a photo URL is provided. Email clients vary in their support for styling, so send a test email before using an export.
