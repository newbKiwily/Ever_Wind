using System.Collections.Generic;

using UnityEditor;

using UnityEditor.Animations;

using UnityEngine;

namespace EverWind.Gameplay.Editor
{
public static class SharedAliveAnimatorSetup
{
    private const string Folder = "Assets/GamePlayEdit/Animation/Shared";

    private static readonly string[] States = { "Idle", "Run", "CombatIdle", "CombatRun", "Skill1", "Skill2", "Skill3", "Skill4", "Skill5", "Gather", "Craft", "Hit", "Knockback", "Dead" };

    [MenuItem("EverWind/Animation/Build Shared Alive Animator")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/GamePlayEdit/Animation", "Shared");
        }

        string path = Folder + "/Alive.controller";

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Knockback", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ActionRunning", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ActionSlot", AnimatorControllerParameterType.Int);
            controller.AddParameter("ActionStart", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ActionSpeed", AnimatorControllerParameterType.Float);

            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();

            for (int i = 0; i < States.Length; ++i)
            {
                string name = States[i];

                var clip = new AnimationClip { name = name };

                AssetDatabase.CreateAsset(clip, Folder + "/" + name + ".anim");

                var state = machine.AddState(name, new Vector3(300 + i % 4 * 240, i / 4 * 100));

                state.motion = clip;

                if (i >= 4 && i <= 10)
                {
                    state.speedParameter = "ActionSpeed";

                    state.speedParameterActive = true;
                }

                states.Add(name, state);
            }

            machine.defaultState = states["Idle"];

            var death = Transition(machine, states["Dead"]);

            death.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            var knockback = Transition(machine, states["Knockback"]);

            knockback.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
            knockback.AddCondition(AnimatorConditionMode.If, 0, "Knockback");

            var hit = Transition(machine, states["Hit"]);

            Allowed(hit);

            hit.AddCondition(AnimatorConditionMode.If, 0, "Hit");

            for (int slot = 1; slot <= 7; ++slot)
            {
                string name = slot <= 5 ? "Skill" + slot : slot == 6 ? "Gather"
                                                                     : "Craft";
                var start = Transition(machine, states[name]);

                start.canTransitionToSelf = true;

                ActionConditions(start, slot);

                start.AddCondition(AnimatorConditionMode.If, 0, "ActionStart");

                var resume = Transition(machine, states[name]);

                ActionConditions(resume, slot);
            }

            for (int i = 0; i < 4; ++i)
            {
                var transition = Transition(machine, states[States[i]]);

                Allowed(transition);

                transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Hit");
                transition.AddCondition(AnimatorConditionMode.IfNot, 0, "ActionRunning");
                transition.AddCondition(i >= 2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Combat");
                transition.AddCondition(i % 2 == 1 ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, .5f, "Speed");
            }
        }

        var sharedMachine = controller.layers[0].stateMachine;

        if (!System.Array.Exists(controller.parameters, p => p.name == "HitStart"))
        {
            controller.AddParameter("HitStart", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HitSpeed", AnimatorControllerParameterType.Float);

            foreach (var child in sharedMachine.states)
            {
                if (child.state.name != "Hit")
                {
                    continue;
                }

                child.state.speedParameter = "HitSpeed";

                child.state.speedParameterActive = true;

                var restart = sharedMachine.AddAnyStateTransition(child.state);

                restart.hasExitTime = false;

                restart.duration = 0;

                restart.canTransitionToSelf = true;

                Allowed(restart);

                restart.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                restart.AddCondition(AnimatorConditionMode.If, 0, "HitStart");

                var transitions = sharedMachine.anyStateTransitions;

                System.Array.Copy(transitions, 2, transitions, 3, transitions.Length - 3);

                transitions[2] = restart;

                sharedMachine.anyStateTransitions = transitions;
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:ActionVisualProfile", new[] { "Assets/GamePlayEdit/Characters" }))
        {
            var profile = AssetDatabase.LoadAssetAtPath<ActionVisualProfile>(AssetDatabase.GUIDToAssetPath(guid));

            string model = profile.name.Replace("Profile", "");

            string overrideFolder = "Assets/GamePlayEdit/Animation/" + model;

            if (!AssetDatabase.IsValidFolder(overrideFolder))
            {
                AssetDatabase.CreateFolder("Assets/GamePlayEdit/Animation", model);
            }

            string overridePath = overrideFolder + "/" + profile.name + ".overrideController";

            var overrides = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overridePath);

            if (overrides == null)
            {
                overrides = new AnimatorOverrideController(controller);

                AssetDatabase.CreateAsset(overrides, overridePath);
            }

            overrides.runtimeAnimatorController = controller;

            var entries = new List<KeyValuePair<AnimationClip, AnimationClip>>();

            overrides.GetOverrides(entries);

            for (int i = 0; i < entries.Count; ++i)
            {
                var clip = profile.ResolveClip(entries[i].Key.name);

                entries[i] = new KeyValuePair<AnimationClip, AnimationClip>(entries[i].Key, clip);
            }

            overrides.ApplyOverrides(entries);

            profile.Controller = overrides;

            if (profile.Skills == null || profile.Skills.Length != 5)
            {
                profile.Skills = new AnimationClip[5];
            }

            for (int i = 0; i < 5; ++i)
            {
                if (profile.Skills[i] == null)
                {
                    profile.Skills[i] = profile.ResolveClip("Skill" + (i + 1));
                }
            }

            EditorUtility.SetDirty(profile);
            EditorUtility.SetDirty(overrides);
        }

        AssetDatabase.SaveAssets();
    }

    private static AnimatorStateTransition Transition(AnimatorStateMachine machine, AnimatorState state)
    {
        var transition = machine.AddAnyStateTransition(state);

        transition.hasExitTime = false;

        transition.duration = .05f;

        transition.hasFixedDuration = true;

        transition.canTransitionToSelf = false;

        transition.interruptionSource = TransitionInterruptionSource.SourceThenDestination;

        return transition;
    }

    private static void Allowed(AnimatorStateTransition transition)
    {
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Knockback");
    }

    private static void ActionConditions(AnimatorStateTransition transition, int slot)
    {
        Allowed(transition);

        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "Hit");
        transition.AddCondition(AnimatorConditionMode.If, 0, "ActionRunning");
        transition.AddCondition(AnimatorConditionMode.Equals, slot, "ActionSlot");
    }
}
}
