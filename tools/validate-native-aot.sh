#!/usr/bin/env bash
set -euo pipefail

RID="${1:-linux-x64}"
CONFIGURATION="${CONFIGURATION:-Release}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT_ROOT="${TMPDIR:-/tmp}/cryptotrading-native-aot"

projects=(
  "src/Api/CryptoTrading.Api.csproj"
  "src/Worker/CryptoTrading.Worker.csproj"
)

echo "CryptoTrading Native AOT validation"
echo "RID: ${RID}"
echo "Configuration: ${CONFIGURATION}"
echo "Output root: ${OUTPUT_ROOT}"

for project in "${projects[@]}"; do
  project_name="$(basename "${project}" .csproj)"
  output_dir="${OUTPUT_ROOT}/${project_name}/${RID}"
  project_path="${ROOT_DIR}/${project}"

  echo
  echo "Restoring ${project_name} for ${RID}..."
  dotnet restore "${project_path}" -r "${RID}" -p:PublishAot=true --force-evaluate

  echo "Publishing ${project_name}..."
  dotnet publish "${project_path}" \
    -c "${CONFIGURATION}" \
    -r "${RID}" \
    --self-contained true \
    -p:PublishAot=true \
    -p:TreatWarningsAsErrors=false \
    --no-restore \
    -o "${output_dir}"

  echo "Published ${project_name} to ${output_dir}"
done

echo
echo "Native AOT validation completed."
