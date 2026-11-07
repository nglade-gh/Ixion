#!/bin/bash
# ---------------------------------------------------------------------------
# build_and_run.sh
# Nettoie le build, reconstruit, puis lance le jeu.
#
# Usage :
#   ./build_and_run.sh              # lance le jeu normalement
#   ./build_and_run.sh --headless   # lance en mode sans affichage (SDL_VIDEODRIVER=offscreen)
#
# Cela garantit que les XML de config modifiés localement sont bien pris
# en compte, même si le build incrémental de dotnet les rate.
# ---------------------------------------------------------------------------

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
cd "$SCRIPT_DIR"

HEADLESS=""
for arg in "$@"; do
    if [ "$arg" = "--headless" ]; then
        HEADLESS="true"
    fi
done

echo "==> Nettoyage du build (bin/, obj/)..."
rm -rf bin/ obj/

echo ""
echo "==> Reconstruction..."
dotnet build 2>&1 | tail -5
build_status=${PIPESTATUS[0]}

if [ $build_status -ne 0 ]; then
    echo ""
    echo "!!! Build échoué (code $build_status). Annulation." >&2
    exit $build_status
fi

echo ""
echo "==> Lancement du jeu..."
if [ -n "$HEADLESS" ]; then
    export SDL_VIDEODRIVER=offscreen
    dotnet run --no-build
else
    dotnet run
fi
