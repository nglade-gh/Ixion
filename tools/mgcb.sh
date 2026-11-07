#!/bin/sh
# Wrapper between MonoGame.Content.Builder.Task and the dotnet-mgcb CLI (3.8.1.303).
#
# The MSBuild target invokes:
#   dotnet mgcb /quiet /@:".../Content.mgcb" /platform:DesktopGL /outputDir:"..." \
#               /intermediateDir:"..." /workingDir:"..."
# (legacy XNA-style /option:value syntax), but the dotnet-mgcb CLI only accepts
# the long --option=value form. This wrapper (referenced as DotnetCommand in the
# csproj) translates the arguments and delegates to tools/mgcb_build.py, which
# builds each item of the .mgcb project with a working long-form invocation.
mgcbfile=""
platform=""
outdir=""
interdir=""
workdir=""
for a in "$@"; do
  case "$a" in
    /@:*|--@=*)                     mgcbfile=${a#*@:};  mgcbfile=${mgcbfile#--@=} ;;
    /platform:*|--platform=*)       platform=${a##*:} ;;
    /outputDir:*|--outputDir=*)     outdir=${a##*:} ;;
    /intermediateDir:*|--intermediateDir=*) interdir=${a##*:} ;;
    /workingDir:*|--workingDir=*)   workdir=${a##*:} ;;
  esac
done
[ -n "$mgcbfile" ] || { echo "mgcb wrapper: missing .mgcb file argument" >&2; exit 1; }
[ -f "$mgcbfile" ] || { echo "mgcb wrapper: $mgcbfile not found" >&2; exit 1; }
exec python3 "$(dirname "$0")/mgcb_build.py" "$mgcbfile" "${platform:-DesktopGL}" "$outdir" "$interdir" "$workdir"
