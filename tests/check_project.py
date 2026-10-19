from pathlib import Path
import json
from tree_sitter import Language, Parser
import tree_sitter_c_sharp

base=Path('ArcSquadUnity')
parser=Parser(Language(tree_sitter_c_sharp.language()))
sources=list((base/'Assets/ArcSquad').rglob('*.cs'))
assert len(sources)>=7, (len(sources),'scripts missing')
for path in sources:
    src=path.read_bytes()
    tree=parser.parse(src)
    assert not tree.root_node.has_error, f'c# syntax errors in {path}'
    print('C# parsed:',path.name, len(src),'bytes')
names={p.name for p in sources}
assert {'ArcPilot.cs','ArcDrone.cs','ArcMission.cs','ArcLobby.cs','ArcSquadSceneFactory.cs','SquadRules.cs'}<=names
assert (base/'Assets/ArcSquad/Shaders/NeonToon.shader').exists()
contents=json.loads((base/'Packages/manifest.json').read_text())['dependencies']
assert contents['com.unity.netcode.gameobjects'].startswith('2.')
assert contents['com.unity.transport'].startswith('2.')
assert (base/'ProjectSettings/ProjectVersion.txt').read_text().startswith('m_EditorVersion: 6000.')
assert '[Rpc(SendTo.Server)]' in (base/'Assets/ArcSquad/Scripts/ArcPilot.cs').read_text()
assert 'NetworkVariable' in (base/'Assets/ArcSquad/Scripts/ArcMission.cs').read_text()
models=list((base/'Assets/ArcSquad/Art/Kenney/Models').glob('*.obj'))
if models:
    assert len(models)>=20
    for model in models:
        data=model.read_text(errors='replace')
        assert any(line.startswith('v ') for line in data.splitlines()), model
        assert any(line.startswith('f ') for line in data.splitlines()), model
print('STATIC SOURCE VALIDATION OK; meshes:',len(models))
print('NOTE: Unity Editor compilation/network play test NOT performed here')
