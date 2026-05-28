# `dotnet new emm-module`

A `dotnet new` template that scaffolds a complete six-project EMM business
module already wired to the solution's BuildingBlocks.

## Install (one time per developer)

From the repo root:

```bash
dotnet new install ./templates/emm-module
```

Verify:

```bash
dotnet new list emm-module
```

## Recommended: use the wrapper script

`dotnet new` only generates files — it doesn't touch the `.sln`. The repo
ships with a thin wrapper that does both in one step. **Use this** for a
seamless experience:

PowerShell (Windows / cross-platform):

```powershell
pwsh scripts/Add-Module.ps1 -Name Invoices
```

Bash (Linux / macOS / WSL):

```bash
./scripts/add-module.sh Invoices
```

What the wrapper does:

1. Runs `dotnet new emm-module --name Invoices` inside `src/Modules/`.
2. Calls `dotnet sln add` for each of the six generated csprojs, placing
   them under a `Modules/Invoices` solution folder so they nest correctly
   in Visual Studio / Rider.
3. Prints the three-line wire-up to-do list and the migration command.

After running, the new projects appear in the Solution Explorer immediately
on next reload.

### Wrapper options

| PowerShell                          | Bash                                | Effect |
|-------------------------------------|-------------------------------------|--------|
| `-Name <Name>`                      | `<Name>` (first positional)         | PascalCase module name (required). |
| `-Description "<text>"`             | `--description "<text>"`            | Goes into every csproj `<Description>`. |
| `-WithSample:$false`                | `--no-sample`                       | Emit a bare skeleton (no sample aggregate/endpoint). |

## Manual route (without the wrapper)

If you'd rather call the template directly and add to the sln yourself:

```bash
# 1. scaffold
cd src/Modules
dotnet new emm-module --name Invoices

# 2. add to the solution (from repo root)
cd ../..
for proj in src/Modules/Invoices/*/*.csproj; do
    dotnet sln EnterpriseModularMonolith.sln add "$proj" --solution-folder "Modules/Invoices"
done
```

PowerShell equivalent:

```powershell
cd src/Modules
dotnet new emm-module --name Invoices
cd ../..
Get-ChildItem src/Modules/Invoices -Recurse -Filter *.csproj | ForEach-Object {
    dotnet sln EnterpriseModularMonolith.sln add $_.FullName --solution-folder "Modules/Invoices"
}
```

## Wire-up after scaffolding (three lines)

1. `Composition/ModuleRegistry.cs` — add `new InvoicesModule(),`
2. `Composition/EndpointsBootstrap.cs` — add `new InvoicesEndpoints(),`
3. Host csproj — add the two `<ProjectReference>` lines for Infrastructure + Presentation.

Then create the migration:

```bash
dotnet ef migrations add Initial_Invoices \
    --project src/Modules/Invoices/Invoices.Infrastructure \
    --startup-project src/Bootstrapper/EnterpriseModularMonolith.Api \
    --context InvoicesDbContext \
    --output-dir Persistence/Migrations
```

## Options on the underlying template

| Flag                    | Default                                                | Effect |
|-------------------------|--------------------------------------------------------|--------|
| `--name <Name>`         | (required)                                             | Replaces every `EmmModule` literal everywhere. PascalCase. |
| `-d, --description <s>` | "Business module for the Enterprise Modular Monolith." | Goes into every csproj `<Description>`. |
| `-s, --with-sample <b>` | `true`                                                 | When `false`, omits the sample aggregate / endpoints. |

## Uninstall

```bash
dotnet new uninstall ./templates/emm-module
```

## Troubleshooting

**`./templates/emm-module is not supported.`** — the path doesn't resolve
to a folder containing `.template.config/template.json`. Run from the repo
root, or use an absolute path.

**`Templates already installed.`** — uninstall an earlier revision with
`dotnet new uninstall ./templates/emm-module` (use the same path you used
to install).

**Generated files don't compile.** — make sure the new module folder is at
`src/Modules/{Name}/`. The csprojs use relative paths
(`..\..\..\BuildingBlocks\...`) that only resolve from that depth. The
wrapper script enforces this automatically.
