#!/usr/bin/env python3
"""Build every item of a .mgcb project file with the dotnet-mgcb CLI (3.8.1.303).

The CLI in this version only accepts long-form options (--option=value). It
supports positional multi-file builds, but each invocation DELETES stale
outputs whose intermediate (.mgstats) records are absent from its own
intermediate dir. Therefore:
  * one --outputDir shared by all items,
  * one --intermediateDir per distinct processorParam group,
  * all items sharing a processorParam in a single invocation.

Usage: mgcb_build.py <content.mgcb> <platform> <outputDir> <intermediateDir> <workingDir>

Must be run from the project directory (MSBuild sets WorkingDirectory to the
csproj folder). Paths are resolved relative to that directory.
"""
import os
import subprocess
import sys
import xml.etree.ElementTree as ET

# Prefer the 'dotnet' executable on PATH; fall back to known container path
import shutil
DOTNET_EXECUTABLE = shutil.which("dotnet") or os.path.join(os.environ.get("DOTNET_ROOT", "/opt/data/home/.dotnet"), "dotnet")

NS = "{MonoGame.ContentPipeline}"


def main() -> int:
    if len(sys.argv) != 6:
        print("usage: mgcb_build.py <content.mgcb> <platform> <outputDir> <intermediateDir> <workingDir>")
        return 2
    mgcb_path, platform, outdir, interdir, workdir = sys.argv[1:6]

    cwd = os.getcwd()
    workdir = workdir.rstrip("/\\")
    # The CLI resolves build paths relative to the process CWD, so express the
    # .mgcb directory (the mgcb workingDir) relative to the project directory.
    wd_rel = os.path.relpath(workdir, cwd) if os.path.isabs(workdir) else workdir
    if wd_rel == ".":
        wd_rel = ""

    root = ET.parse(mgcb_path).getroot()
    items = root.findall(f".//{NS}Item")
    if not items:
        print(f"mgcb wrapper: no <Item> entries found in {mgcb_path}", file=sys.stderr)
        return 1

    # Group items by (processor, processorParam) so each group is one invocation
    # sharing the same intermediate dir.
    groups: dict[tuple[str, str], list[str]] = {}
    for it in items:
        source = it.get("source", "")
        if not source:
            continue
        processor = it.get("processor", "TextureProcessor")
        param = it.get("processorParam") or ""
        build = f"{wd_rel}/{source}".replace("\\", "/") if wd_rel else source.replace("\\", "/")
        groups.setdefault((processor, param), []).append(build)

    interbase = interdir.rstrip("/") + "/" if interdir else ""
    failures = 0
    built = 0
    for index, ((processor, param), builds) in enumerate(sorted(groups.items()), start=1):
        inter = f"{interbase}g{index}" if interbase else f"g{index}"
        cmd = [DOTNET_EXECUTABLE, "mgcb", *builds, f"--config={platform}",
               f"--outputDir={outdir}", f"--intermediateDir={inter}",
               f"--workingDir={wd_rel}/" if wd_rel else "--workingDir=./"]
        if param and processor != "FontDescriptionProcessor":
            cmd.append(f"--processorParam={param}")
        print(f"mgcb wrapper: {len(builds)} item(s) -> {inter}")
        env = os.environ.copy()
        tools_dir = os.path.expanduser("~/.dotnet/tools")
        if tools_dir not in env.get("PATH", ""):
            env["PATH"] = tools_dir + os.pathsep + env.get("PATH", "")
        result = subprocess.run(cmd, cwd=cwd, env=env)
        if result.returncode != 0:
            failures += 1
            print(f"mgcb wrapper: group {index} FAILED (exit {result.returncode})", file=sys.stderr)
        else:
            built += len(builds)

    if failures:
        print(f"mgcb wrapper: {failures} group(s) failed", file=sys.stderr)
        return 1
    print(f"mgcb wrapper: {built} item(s) built successfully")
    return 0


if __name__ == "__main__":
    sys.exit(main())
