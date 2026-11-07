#!/bin/sh
# Bridge used as DotnetCommand by MonoGame.Content.Builder.Task (3.8.1.303).
#
# The package target invokes:
#   $(DotnetCommand) $(MGCBCommand) $(MonoGameMGCBAdditionalArguments) \
#     /@:".../Content.mgcb" /platform:DesktopGL /outputDir:"..." \
#     /intermediateDir:"..." /workingDir:"..."
# with legacy XNA-style /option:value arguments, but the dotnet-mgcb CLI
# (3.8.1.303) only accepts long-form --option=value. When invoked as
# "mgcb ..." this bridge translates the arguments and delegates to
# tools/mgcb_build.py; any other subcommand ("tool restore", ...) is passed
# straight through to the real dotnet executable.
case "$1" in
  mgcb)
    mgcbfile=""
    platform=""
    outdir=""
    interdir=""
    workdir=""
    for a in "$@"; do
      case "$a" in
        /@:*|--@=*)                        mgcbfile=${a#*@:}; mgcbfile=${mgcbfile#--@=} ;;
        /platform:*|--platform=*)          platform=${a##*:} ;;
        /outputDir:*|--outputDir=*)        outdir=${a##*:} ;;
        /intermediateDir:*|--intermediateDir=*) interdir=${a##*:} ;;
        /workingDir:*|--workingDir=*)      workdir=${a##*:} ;;
      esac
    done
    [ -n "$mgcbfile" ] || { echo "mgcb bridge: missing .mgcb file argument" >&2; exit 1; }
    [ -f "$mgcbfile" ] || { echo "mgcb bridge: $mgcbfile not found" >&2; exit 1; }
    exec python3 "$(dirname "$0")/mgcb_build.py" "$mgcbfile" "${platform:-DesktopGL}" "$outdir" "$interdir" "$workdir"
    ;;
  *)
    if command -v dotnet >/dev/null 2>&1; then
      exec dotnet "$@"
    else
      exec /opt/data/home/.dotnet/dotnet "$@"
    fi
    ;;
esac
