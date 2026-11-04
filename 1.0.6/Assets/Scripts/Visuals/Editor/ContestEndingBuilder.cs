using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace ArcaneArchive.Visuals.Editor
{
    public static class ContestEndingBuilder
    {
        public const string Root = "Assets/Contest20261107/Resources/Ending20261104";
        [MenuItem("IM Monster/Ending/Build ending Timeline")]
        public static void Build()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(Root + "/LegacyEnding.playable");
            if (!timeline)
            { timeline = ScriptableObject.CreateInstance<TimelineAsset>(); AssetDatabase.CreateAsset(timeline, Root + "/LegacyEnding.playable"); }
            foreach (var track in timeline.GetRootTracks().ToArray()) timeline.DeleteTrack(track);
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength; timeline.fixedDuration = ContestEndingCutscene.Duration;
            timeline.editorSettings.frameRate = 30;

            var camera = Clip("Camera");
            float[] times = {0, 5, 10, 13, 18, 23, 26};
            Vector3[] positions = {
                new Vector3(-.70f,1.52f,-1.04f), new Vector3(-.51f,1.40f,-.86f),
                new Vector3(-.39f,1.36f,-.80f), new Vector3(-.30f,1.52f,-1.13f),
                new Vector3(-.85f,2.10f,-1.30f), new Vector3(-1.25f,2.50f,-1.65f), new Vector3(-1.25f,2.50f,-1.65f)};
            Vector3[] targets = {
                new Vector3(0,1.06f,0),new Vector3(0,1.03f,0),new Vector3(0,1.02f,0),
                new Vector3(.12f,1.13f,-.12f),new Vector3(.5f,1.4f,3.8f),new Vector3(.7f,1.45f,5.1f),new Vector3(.7f,1.45f,5.1f)};
            TransformCurves(camera, times, positions, targets);
            Track(timeline, "镜头", camera);

            var lid = Clip("CaseLid");
            Curve(lid, typeof(Transform), "localEulerAnglesRaw.x", new[]{0f,3.5f,6.4f,6.7f,26f}, new[]{68f,68f,-1.3f,0f,0f});
            Curve(lid, typeof(Transform), "m_LocalPosition.x", new[]{0f,26f}, new[]{0f,0f});
            Curve(lid, typeof(Transform), "m_LocalPosition.y", new[]{0f,26f}, new[]{1.055f,1.055f});
            Curve(lid, typeof(Transform), "m_LocalPosition.z", new[]{0f,26f}, new[]{.25f,.25f});
            Track(timeline, "箱盖", lid);
            var package = Clip("SingleBatch");
            Curve(package, typeof(Transform), "m_LocalPosition.x", new[]{0f,5.2f,26f}, new[]{0f,0f,0f});
            Curve(package, typeof(Transform), "m_LocalPosition.y", new[]{0f,1.5f,4.8f,26f}, new[]{1.18f,1.21f,.94f,.94f});
            Curve(package, typeof(Transform), "m_LocalPosition.z", new[]{0f,4.8f,26f}, new[]{-.04f,0f,0f});
            Curve(package, typeof(Transform), "localEulerAnglesRaw.z", new[]{0f,4.8f,26f}, new[]{-4f,0f,0f});
            Track(timeline, "封档包", package);
            var glow = Clip("SealLight");
            Curve(glow, typeof(Light), "m_Intensity", new[]{0f,3f,6.7f,10f,15f,26f}, new[]{.02f,.06f,.13f,.08f,0f,0f});
            Track(timeline, "封印灯光", glow);

            MakeSoundtrack(Root + "/EndingSoundtrack.wav");
            AssetDatabase.ImportAsset(Root + "/EndingSoundtrack.wav", ImportAssetOptions.ForceSynchronousImport);
            var audio = timeline.CreateTrack<AudioTrack>(null, "音乐与撤离脚步");
            var sound = audio.CreateClip<AudioPlayableAsset>(); sound.displayName = "原创合成配乐 · 箱扣 · 远处撤离";
            ((AudioPlayableAsset)sound.asset).clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/EndingSoundtrack.wav");
            sound.start = 0; sound.duration = 26;
            var seal=AssetDatabase.LoadAssetAtPath<Material>(Root+"/SealGlow.mat");
            if(!seal){seal=new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));AssetDatabase.CreateAsset(seal,Root+"/SealGlow.mat");}
            seal.SetFloat("_Surface",1);seal.SetFloat("_SrcBlend",5);seal.SetFloat("_DstBlend",10);seal.SetFloat("_ZWrite",0);
            seal.SetColor("_BaseColor",Color.white);seal.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");seal.renderQueue=3000;
            EditorUtility.SetDirty(seal);
            EditorUtility.SetDirty(timeline); AssetDatabase.SaveAssets();
            Debug.Log("[Ending] Timeline and four animation tracks + original soundtrack built.");
        }

        static AnimationClip Clip(string name)
        {
            string path = Root + "/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (!clip) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.frameRate = 30; clip.name = name; return clip;
        }
        static void Track(TimelineAsset timeline, string name, AnimationClip clip)
        {
            var track = timeline.CreateTrack<AnimationTrack>(null, name);
            track.trackOffset = TrackOffset.ApplyTransformOffsets;
            var item = track.CreateClip(clip); item.start = 0; item.duration = 26; item.displayName = name;
            var asset = (AnimationPlayableAsset)item.asset; asset.removeStartOffset = false;
            EditorUtility.SetDirty(clip);
        }
        static void TransformCurves(AnimationClip clip, float[] times, Vector3[] positions, Vector3[] targets)
        {
            var angles = new Vector3[times.Length];
            for (int i = 0; i < times.Length; i++)
            {
                angles[i] = Quaternion.LookRotation(targets[i] - positions[i]).eulerAngles;
                if (i == 0) { angles[i].x = Mathf.DeltaAngle(0, angles[i].x); angles[i].y = Mathf.DeltaAngle(0, angles[i].y); }
                else
                { angles[i].x = angles[i-1].x + Mathf.DeltaAngle(angles[i-1].x, angles[i].x); angles[i].y = angles[i-1].y + Mathf.DeltaAngle(angles[i-1].y, angles[i].y); }
            }
            for (int axis = 0; axis < 3; axis++)
            {
                string suffix = axis == 0 ? ".x" : axis == 1 ? ".y" : ".z";
                Curve(clip, typeof(Transform), "m_LocalPosition" + suffix, times, positions.Select(p => p[axis]).ToArray());
                Curve(clip, typeof(Transform), "localEulerAnglesRaw" + suffix, times, angles.Select(p => p[axis]).ToArray());
            }
        }
        static void Curve(AnimationClip clip, Type type, string property, float[] times, float[] values)
        {
            var curve = new AnimationCurve(times.Select((t,i) => new Keyframe(t, values[i])).ToArray());
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", type, property), curve);
        }

        static void MakeSoundtrack(string path)
        {
            const int rate = 22050, count = rate * 26;
            var random = new System.Random(20261104);
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1);
                writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
                double[] notes = {146.832, 220, 293.665, 349.228, 440, 523.251, 587.33};
                for (int i = 0; i < count; i++)
                {
                    double t = (double)i / rate, v = 0;
                    for (int n = 0; n < notes.Length; n++)
                    {
                        double attack = .8 + n * 1.65, age = t - attack;
                        if (age > 0) v += Math.Sin(age * notes[n] * Math.PI * 2) * Math.Exp(-age / 5.5) * Math.Min(age * 2, 1) * .035;
                    }
                    double click = t - 6.65;
                    if (click >= 0 && click < .22) v += ((random.NextDouble() * 2 - 1) * .065 + Math.Sin(click * 2 * Math.PI * 180) * .13) * Math.Exp(-click * 35);
                    for (int step = 0; step < 9; step++)
                    {
                        double age = t - (17 + step * .67);
                        if (age > 0 && age < .25) v += (random.NextDouble() * 2 - 1) * .045 * Math.Exp(-age * 22) * Math.Sin(Math.PI * age / .25);
                    }
                    v *= Math.Min(t / 1.2, 1) * Math.Min((26 - t) / 2, 1);
                    writer.Write((short)(Math.Max(-1, Math.Min(1, v)) * short.MaxValue));
                }
            }
        }
    }
}
