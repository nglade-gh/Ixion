#!/bin/bash
# ---------------------------------------------------------------------------
# sync_shooter_from_chouette.sh
# Synchronise le projet Shooter depuis chouette (timc-dctm-35) vers CETTE machine.
#
# Usage (depuis la machine locale de l'utilisateur) :
#   ./sync_shooter_from_chouette.sh              # cible = répertoire courant
#   ./sync_shooter_from_chouette.sh /chemin/cible/
#
# Ce script se connecte à chouette via SSH/rsync pour pull les fichiers du
# projet MonoGames Shooter.
# ---------------------------------------------------------------------------

# Chemin source sur chouette (chemin hôte, pas Docker)
RSYNC_SRC="chouette:Documents/Prog/dotnet/Programs/MonoGames/Shooter/"

# Répertoire cible (par défaut le dossier du script)
RSYNC_DST="${1:-$(dirname "$0")}"

echo "==> Synchronisation Shooter depuis chouette :"
echo "    Source : $RSYNC_SRC"
echo "    Cible  : $RSYNC_DST"

rsync -P -avz \
    --exclude='bin/' \
    --exclude='obj/' \
    --exclude='Content/obj/' \
    --exclude='*.xnb' \
    --exclude='.git/' \
    --exclude='*.csproj.user' \
    --exclude='*.suo' \
    --exclude='sync_shooter_from_chouette.sh' \
    --exclude='build_and_run.sh' \
    "$RSYNC_SRC" "$RSYNC_DST"

status=$?
if [ $status -eq 0 ]; then
    echo "==> Synchronisation terminée."
    echo ""
    echo "==> Pense à reconstruire le contenu du pipeline :"
    echo "     cd '$RSYNC_DST'"
    echo "     dotnet build"
else
    echo "==> rsync a échoué (code $status)." >&2
fi
exit $status
