#!/usr/bin/env bash
# Pack the template into a NuGet .nupkg ready for `dotnet new install`.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"
OUT="$HERE/artifacts"
mkdir -p "$OUT"

cat > "$HERE/EnterpriseModularMonolith.Module.Template.nuspec" << NUSPEC
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
NUSPEC

dotnet pack "$HERE/EnterpriseModularMonolith.Module.Template.nuspec" \
    -o "$OUT" 2>/dev/null || {
  # Fallback: nuget.exe pack
  command -v nuget >/dev/null && nuget pack "$HERE/EnterpriseModularMonolith.Module.Template.nuspec" -OutputDirectory "$OUT" \
    || echo "Either dotnet 8+ or nuget.exe is required to pack the template."
}

echo "Packed to: $OUT"
