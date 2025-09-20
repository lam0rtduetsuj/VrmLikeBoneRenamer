// Assets/Editor/VrmLikeBoneRenamer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VrmLikeBoneRenamer
{
    [MenuItem("Tools/Make Bone Names Unique (VRM-style) %#u", priority = 1001)]
    public static void MakeSelectionUnique()
    {
        var go = Selection.activeGameObject;
        if (!go)
        {
            EditorUtility.DisplayDialog(
                "VRM样式重命名",
                "没有选中任何对象，请先在层级视图中选择一个模型根节点。",
                "确定"
            );
            return;
        }

        var animator = go.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman)
        {
            EditorUtility.DisplayDialog(
                "VRM样式重命名",
                $"选中的对象 {go.name} 上没有 Humanoid Animator，无法执行重命名。\n\n" +
                "请确认 root 节点挂载了 Animator 且 Avatar 为 Humanoid。",
                "确定"
            );
            return;
        }

        int renamed = MakeNonHumanoidNamesUnique(go, animator);
        EditorUtility.DisplayDialog(
            "VRM样式重命名",
            $"处理完成，共重命名了 {renamed} 个非Humanoid骨骼。",
            "好的"
        );
    }

    [MenuItem("Tools/Make Bone Names Unique (VRM-style)", validate = true)]
    private static bool ValidateMakeSelectionUnique() => Selection.activeGameObject != null;

    public static int MakeNonHumanoidNamesUnique(GameObject root, Animator animator)
    {
        if (!root || !animator) return 0;

        // 获取 humanoid 映射到的骨骼
        var humanTransforms = new HashSet<Transform>();
        foreach (HumanBodyBones bone in Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (bone == HumanBodyBones.LastBone) continue;
            var t = animator.GetBoneTransform(bone);
            if (t) humanTransforms.Add(t);
        }

        var all = root.GetComponentsInChildren<Transform>(true);
        var nameCount = all.GroupBy(x => x.name).ToDictionary(g => g.Key, g => g.Count());

        int renamed = 0;
        foreach (var t in all)
        {
            if (humanTransforms.Contains(t)) continue; // Humanoid 保留原名

            if (nameCount.TryGetValue(t.name, out int cnt) && cnt > 1)
            {
                if (ForceUniqueName(t, nameCount)) renamed++;
            }
        }
        return renamed;
    }

    private static bool ForceUniqueName(Transform transform, Dictionary<string, int> nameCount)
    {
        for (int i = 2; i < 5000; ++i)
        {
            var newName = $"{transform.name}_{i}";
            if (!nameCount.ContainsKey(newName))
            {
                Undo.RecordObject(transform, "ForceUniqueName (VRM-style)");
                Debug.LogWarning($"force rename {transform.name} => {newName}");
                transform.name = newName;
                nameCount[newName] = 1;
                return true;
            }
        }
        throw new Exception("Failed to create unique name up to _4999");
    }
}
