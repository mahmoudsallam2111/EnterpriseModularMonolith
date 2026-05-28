#requires -Version 5.1
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$out  = Join-Path $here "artifacts"
New-Item -ItemType Directory -Force -Path $out | Out-Null

@"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd">
  <metadata>
    <id>EnterpriseModularMonolith.Module.Template</id>
    <version>1.0.0</version>
    <description>dotnet new template that scaffolds a six-project EMM business module.</description>
    <authors>EnterpriseModularMonolith</authors>
    <packageTypes>
      <packageType name="Template" />
    </packageTypes>
  </metadata>
</package>
"@ | Set-Content "$here\EnterpriseModularMonolith.Module.Template.nuspec" -Encoding UTF8

nuget pack "$here\EnterpriseModularMonolith.Module.Template.nuspec" -OutputDirectory $out
Write-Host "Packed to: $out"
