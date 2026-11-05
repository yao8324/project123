"""Generate local mesh variants for the four 1.0.5 playtest issues.

Original exports remain untouched. Run from the Unity project root, then assign
the variants using the recorded repair manifest. No Blender dependency.
"""
import collections
import gzip
import json
import math
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/Art/MilanLibraryV6/Meshes'
DEST = ROOT / 'Assets/Art/MilanLibraryV6/PlaytestRepairs20261016'
REPORT = ROOT / 'Docs/PlaytestFeedback_20261016'
OVERRIDES = REPORT / 'SourceMeshes'
SOURCE_PATHS = json.loads((OVERRIDES / 'source_paths.json').read_text(encoding='utf-8'))


def read_mesh(number):
    path = (OVERRIDES if str(number) in SOURCE_PATHS else SOURCE) / f'mesh_{number:04}.mlmesh'
    data = gzip.decompress(path.read_bytes())
    magic, nv, ni = struct.unpack_from('<III', data)
    assert magic == 0x314D4C4D
    vertices = [list(struct.unpack_from('<8f', data, 12 + i * 32)) for i in range(nv)]
    return vertices, list(struct.unpack_from(f'<{ni}i', data, 12 + nv * 32))


def components(vertices, triangles):
    parent = list(range(len(vertices)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    def union(a, b):
        parent[find(a)] = find(b)

    positions = {}
    for i, v in enumerate(vertices):
        key = tuple(round(x, 4) for x in v[:3])
        if key in positions:
            union(i, positions[key])
        else:
            positions[key] = i
    for i in range(0, len(triangles), 3):
        union(triangles[i], triangles[i + 1])
        union(triangles[i], triangles[i + 2])
    groups = collections.defaultdict(list)
    for i in range(len(vertices)):
        groups[find(i)].append(i)
    return list(groups.values())


def bounds(vertices, ids):
    return ([min(vertices[i][j] for i in ids) for j in range(3)],
            [max(vertices[i][j] for i in ids) for j in range(3)])


def write_variant(number, vertices, triangles, reason, manifest):
    data = struct.pack('<III', 0x314D4C4D, len(vertices), len(triangles))
    data += b''.join(struct.pack('<8f', *v) for v in vertices)
    data += struct.pack(f'<{len(triangles)}i', *triangles)
    path = DEST / f'mesh_{number:04}_repaired.mlmesh'
    path.write_bytes(gzip.compress(data, mtime=0))
    manifest['meshes'].append(dict(source=SOURCE_PATHS.get(str(number), f'Assets/Art/MilanLibraryV6/Meshes/mesh_{number:04}.mlmesh'),
                                   variant=path.relative_to(ROOT).as_posix(), reason=reason))


def append_box(lo, hi, vertices, triangles):
    for axis in range(3):
        u, w = (axis + 1) % 3, (axis + 2) % 3
        for side in (0, 1):
            normal = [0., 0., 0.]
            normal[axis] = 1. if side else -1.
            base = len(vertices)
            order = [(0, 0), (1, 0), (1, 1), (0, 1)]
            if not side:
                order.reverse()
            for a, b in order:
                p = lo.copy()
                p[axis] = hi[axis] if side else lo[axis]
                p[u] = hi[u] if a else lo[u]
                p[w] = hi[w] if b else lo[w]
                uv = [(p[2] if axis == 0 else p[0]) * .5,
                      (p[2] if axis == 1 else p[1]) * .5]
                vertices.append(p + normal + uv)
            triangles.extend(base + i for i in (0, 1, 2, 0, 2, 3))


def main():
    DEST.mkdir(parents=True, exist_ok=True)
    REPORT.mkdir(parents=True, exist_ok=True)
    manifest = dict(meshes=[], chairs=[])

    # Recess the two stair-adjacent window surrounds within the masonry outer
    # plane (|x|=12.40). Affine depth scaling preserves the arch and closed mesh.
    for num in (490, 501):
        vs, ts = read_mesh(num)
        ratio = (12.38 - 11.97) / (12.58 - 11.97)
        for v in vs:
            v[0] = math.copysign(11.97 + (abs(v[0]) - 11.97) * ratio, v[0])
            v[3] /= ratio
            length = math.sqrt(sum(n * n for n in v[3:6]))
            v[3:6] = [n / length for n in v[3:6]]
        write_variant(num, vs, ts, 'Recess window surround clear of both stair flights', manifest)

    # Horizontal string courses are independent closed boxes. Split only these
    # boxes around the side-window openings, keeping closed end caps, original
    # UV scale, pilasters, column capitals, gallery slab and collision support.
    apertures = [(c - 1.80, c + 1.80) for c in (3, 9, 21, 27, 33, 39)]
    for num in range(212, 228, 2):
        vs, ts = read_mesh(num)
        result_v, result_t = [], []
        count = 0
        for ids in components(vs, ts):
            lo, hi = bounds(vs, ids)
            ids_set = set(ids)
            tris = [ts[i:i+3] for i in range(0, len(ts), 3) if ts[i] in ids_set]
            intervals = [(lo[2], hi[2])]
            is_bar = (abs((lo[0] + hi[0]) / 2) > 11.7 and lo[1] > 1.3
                      and hi[1] - lo[1] < .2 and hi[2] - lo[2] > 2)
            if is_bar:
                for a, b in apertures:
                    intervals = [(l, h) for l0, h0 in intervals
                                 for l, h in ((l0, min(h0, a)), (max(l0, b), h0)) if h - l > .001]
                count += 1
            for left, right in intervals:
                if is_bar:
                    append_box(lo[:2] + [left], hi[:2] + [right], result_v, result_t)
                    continue
                remap = {}
                for i in ids:
                    v = vs[i].copy()
                    remap[i] = len(result_v)
                    result_v.append(v)
                for tri in tris:
                    result_t.extend(remap[i] for i in tri)
        assert count == 3, (num, count)
        write_variant(num, result_v, result_t, 'Interrupt string courses at stained-glass openings', manifest)

    # Exterior facade detailing overlaps the new upper entrance floor. Recess
    # its inward-facing ends behind the interior wall, not the exterior facade.
    for num in (390, 391, 392, 394, 395, 396, 481, 482, 483, 484, 537, 538):
        vs, ts = read_mesh(num)
        changed = 0
        for ids in components(vs, ts):
            lo, hi = bounds(vs, ids)
            if lo[2] < -.05 and -.051 < hi[2] < .61 and hi[1] > 6 and lo[0] < 11.9 and hi[0] > -11.9:
                for i in ids:
                    if vs[i][2] > -.07:
                        vs[i][2] = -.07
                        changed += 1
        if changed:
            write_variant(num, vs, ts, 'Recess facade ends behind upper entrance interior wall', manifest)

    # Each leather component below y=.6 is one seat. Two chairs are merged in
    # each material chunk: never wrap the entire chunk in one large collider.
    for num in (689, 692, 695, 698, 701, 704):
        vs, ts = read_mesh(num)
        for ids in components(vs, ts):
            lo, hi = bounds(vs, ids)
            if hi[1] < .6:
                manifest['chairs'].append(dict(center=[(lo[i]+hi[i])/2 for i in range(3)],
                                                size=[hi[i]-lo[i] for i in range(3)], mesh=num))
    assert len(manifest['chairs']) == 12
    (REPORT / 'repair_manifest.json').write_text(json.dumps(manifest, indent=2), encoding='utf-8')
    print(f'Generated {len(manifest["meshes"])} mesh variants; located {len(manifest["chairs"])} individual seats.')


if __name__ == '__main__':
    main()
