"""Parse Unity scripts as C# 9 using Microsoft's real Roslyn parser.

No Unity API stubs are used; this is syntax validation, not a Unity build.
"""
import json,os,shutil,subprocess,tempfile
from pathlib import Path
root=Path(__file__).resolve().parents[1]
dotnet=shutil.which('dotnet') or str(Path(os.environ.get('DOTNET_ROOT','/workspace/.hina-runtime/dotnet'))/'dotnet')
sdkroot=Path(dotnet).resolve().parent;sdk=sorted((sdkroot/'sdk').glob('8.*'))[-1];roslyn=sdk/'Roslyn/bincore'
refs=sorted((sdkroot/'packs/Microsoft.NETCore.App.Ref').glob('8.*'))[-1]/'ref/net8.0'
with tempfile.TemporaryDirectory(prefix='hina-syntax-') as temp:
    d=Path(temp);source=d/'Check.cs'
    source.write_text('''using System; using System.IO; using System.Linq; using Microsoft.CodeAnalysis; using Microsoft.CodeAnalysis.CSharp;
class Check { static int Main(string[] args) { int failures=0; foreach(var file in args) { var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.CSharp9)); foreach(var error in tree.GetDiagnostics().Where(x=>x.Severity==DiagnosticSeverity.Error)) { Console.WriteLine(file+": "+error);failures++; } } Console.WriteLine(args.Length+" Unity source files parsed, "+failures+" syntax errors. Unity API compilation NOT performed.");return failures==0?0:1; }}''')
    cmd=[dotnet,str(roslyn/'csc.dll'),'/nologo','/target:exe','/out:'+str(d/'Check.dll')]+['/r:'+str(p) for p in refs.glob('*.dll')]+['/r:'+str(roslyn/name) for name in ['Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll']]+[str(source)]
    subprocess.run(cmd,check=True)
    for name in ['Microsoft.CodeAnalysis.dll','Microsoft.CodeAnalysis.CSharp.dll']:shutil.copy2(roslyn/name,d/name)
    (d/'Check.runtimeconfig.json').write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
    files=sorted((root/'ProjectHINA/Assets').rglob('*.cs'))
    result=subprocess.run([dotnet,str(d/'Check.dll')]+[str(p) for p in files])
    (root/'syntax_test_results.json').write_text(json.dumps({'files':len(files),'passed':result.returncode==0,'parser':'Roslyn C# 9','unity_api_compilation_performed':False},indent=2))
    raise SystemExit(result.returncode)
