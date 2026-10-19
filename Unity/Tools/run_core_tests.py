"""Compile and execute the real core tests without NuGet dependencies.

Uses a local .NET 8 SDK (DOTNET_ROOT or dotnet on PATH). This validates pure
C# gameplay only; it does NOT claim Unity Editor or rendering validation.
"""
from pathlib import Path
import os,shutil,subprocess,json
root=Path(__file__).resolve().parents[1]
dotnet=shutil.which('dotnet') or str(Path(os.environ.get('DOTNET_ROOT','/workspace/.hina-runtime/dotnet'))/'dotnet')
sdkroot=Path(dotnet).resolve().parent
sdk=sorted((sdkroot/'sdk').glob('8.*'))[-1]
refs=sorted((sdkroot/'packs/Microsoft.NETCore.App.Ref').glob('8.*'))[-1]/'ref/net8.0'
out=root/'Tests/bin/Debug/net8.0';out.mkdir(parents=True,exist_ok=True)
files=[root/'ProjectHINA/Assets/Hina/Scripts/Core/GameModel.cs',root/'Tests/Program.cs']
cmd=[dotnet,str(sdk/'Roslyn/bincore/csc.dll'),'/nologo','/target:exe','/out:'+str(out/'Hina.Core.Tests.dll')]+['/r:'+str(p) for p in refs.glob('*.dll')]+[str(p) for p in files]
subprocess.run(cmd,check=True)
(out/'Hina.Core.Tests.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
raise SystemExit(subprocess.run([dotnet,str(out/'Hina.Core.Tests.dll')]).returncode)
