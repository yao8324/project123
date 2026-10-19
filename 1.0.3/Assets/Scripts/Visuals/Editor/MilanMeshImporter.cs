using System;
using System.IO;
using System.IO.Compression;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace ArcaneArchive.Visuals.Editor
{
    // Native, lossless mesh payload exported from the evaluated Blender scene.
    [ScriptedImporter(1, "mlmesh")]
    public sealed class MilanMeshImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            using var file = File.OpenRead(ctx.assetPath);
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            using var r = new BinaryReader(zip);
            if (r.ReadUInt32() != 0x314D4C4D) throw new InvalidDataException("Not an MLM1 mesh");
            int vc = r.ReadInt32(), ic = r.ReadInt32();
            if (vc <= 0 || vc > 20000000 || ic <= 0 || ic > 100000000 || ic % 3 != 0)
                throw new InvalidDataException("Invalid mesh dimensions");
            byte[] bytes = r.ReadBytes(checked(vc * 32));
            if (bytes.Length != vc * 32) throw new EndOfStreamException();
            var floats = new float[vc * 8]; Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            var p = new Vector3[vc]; var n = new Vector3[vc]; var uv = new Vector2[vc];
            for (int i = 0, j = 0; i < vc; i++, j += 8)
            {
                p[i] = new Vector3(floats[j], floats[j+1], floats[j+2]);
                n[i] = new Vector3(floats[j+3], floats[j+4], floats[j+5]);
                uv[i] = new Vector2(floats[j+6], floats[j+7]);
            }
            bytes = r.ReadBytes(checked(ic * 4));
            if (bytes.Length != ic * 4) throw new EndOfStreamException();
            var indices = new int[ic]; Buffer.BlockCopy(bytes, 0, indices, 0, bytes.Length);
            foreach (int index in indices) if (index < 0 || index >= vc) throw new InvalidDataException("Mesh index out of range");
            var mesh = new Mesh {name = Path.GetFileNameWithoutExtension(ctx.assetPath), indexFormat = IndexFormat.UInt32};
            mesh.vertices = p; mesh.normals = n; mesh.uv = uv; mesh.triangles = indices;
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            ctx.AddObjectToAsset("mesh", mesh); ctx.SetMainObject(mesh);
        }
    }
}
