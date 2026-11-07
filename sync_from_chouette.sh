#!/bin/sh
# ---------------------------------------------------------------------------
# sync_from_chouette.sh
# Synchronise le répertoire Shooter/ depuis chouette
# (timc-dctm-35) vers CETTE machine.
#
# Usage :
#   ./sync_from_chouette.sh              # cible = répertoire courant
#   ./sync_from_chouette.sh /chemin/cible/
#
# Exclusions : bin/, obj/ (build .NET), .config/, .mgstats, core.*, *.log,
#              node_modules/, build/, .gradle/, .venv/, __pycache__,
#              .git/ (historique), *.nupkg, fichiers temporaires/IDE.
# NB : .git n'est PAS exclu (pour suivre l'historique si la cible est un dépôt).
# ---------------------------------------------------------------------------

RSYNC_SRC="chouette:Documents/Prog/dotnet/Programs/MonoGames/Shooter/"
RSYNC_DST="${1:-./}"

echo "==> Synchronisation : $RSYNC_SRC -> $RSYNC_DST"

rsync -P -av \
    --exclude='bin' \
    --exclude='obj' \
    --exclude='**/bin' \
    --exclude='**/obj' \
    --exclude='.config' \
    --exclude='.mgstats' \
    --exclude='*.nupkg' \
    --exclude='*.nuspec' \
    --exclude='*.cache' \
    --exclude='*.tmp' \
    --exclude='*.log' \
    --exclude='core.*' \
    --exclude='*.swp' \
    --exclude='*.swo' \
    --exclude='*.DS_Store' \
    --exclude='.idea' \
    --exclude='.vscode' \
    --exclude='.vs' \
    --exclude='*.suo' \
    --exclude='node_modules' \
    --exclude='build' \
    --exclude='.gradle' \
    --exclude='.venv' \
    --exclude='__pycache__' \
    --exclude='*.pyc' \
    --exclude='.ipynb_checkpoints' \
    "$RSYNC_SRC" "$RSYNC_DST"

status=$?
if [ $status -eq 0 ]; then
    echo "==> Synchronisation terminée."
else
    echo "==> rsync a échoué (code $status)." >&2
fi
exit $status
