/*
MIT License

Copyright (c) 2023 Sakura (tbbsakura)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

// アバタービルド時の処理

using System;
using UnityEngine;
using nadena.dev.ndmf;
using nadena.dev.modular_avatar.core;

using UnityEditor;
using UnityEditor.Animations;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

using SakuraScript.OnOffMenu;
using SakuraScript.SakuraOnOffMenuBuild;

[assembly: ExportsPlugin(typeof(SakuraOnOffMenuBuild))]

namespace SakuraScript.SakuraOnOffMenuBuild 
{
    public class SakuraOnOffMenuBuild : Plugin<SakuraOnOffMenuBuild>
    {
		string ToStateName( string originalName ) 
		{
			return originalName.Replace( ".", "_" ); // Use of "." is prohibited.
		}

        string GetRelativePathFromRootObject(GameObject target)
        {
			string pathstr = target.name;
			for ( Transform par = target.transform.parent ; par != null ; par = par.parent ) {
				if ( par.parent == null ) break;  // exclude the root object such as VRC Avatar.
				pathstr = par.name +"/" + pathstr;
			}
			return pathstr; 
        }

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving) // 早い段階で差し替え… Generating フェイズでも良いのかも？
                .BeforePlugin("nadena.dev.modular-avatar")
                .Run("Prepare On/Off Menu (BluildPhase.Resolving)", ctx =>
                {
                    // ctx.AvatarRootTransform SakuraOnOffMenuNDMF コンポーネントを持っているものを列挙して処理
                    Component[] onOffMenuComponents;
                    onOffMenuComponents = ctx.AvatarRootTransform.GetComponentsInChildren(typeof(SakuraOnOffMenuNDMF),true);
//                    Debug.Log("SakuraScript: ctx.AvatarRootTransform.name = " + ctx.AvatarRootTransform.name + ", Found Target: " + _OnOffMenuComponents.Length);
                    foreach (var x in onOffMenuComponents )
                    {
                        //Debug.Log("SakuraScript: " + x.name);
                        GenerateOnOff(x.gameObject);
                        GameObject.DestroyImmediate(x);
                    }
                });
        }

        private void GenerateOnOff( GameObject obj )
        {
            if ( obj == null ) return;
            SakuraOnOffMenuNDMF onoffComponent = obj.GetComponent<SakuraOnOffMenuNDMF>() as SakuraOnOffMenuNDMF;
            if ( onoffComponent ==null ) return;
            if ( onoffComponent.m_OnOffTarget == null ) return;

            string targetName = onoffComponent.m_OnOffTarget.name;
            bool internalValue = onoffComponent.m_InternalValue;
            bool showOnLoad = onoffComponent.m_ShowOnLoad;
            bool saved = onoffComponent.m_SaveParameter;

            string defaultSuffix = (showOnLoad) ? "_off" : "_on"; // true のとき offか、onか

            string paramName = onoffComponent.m_ParameterName;
            if ( paramName.Length == 0 ) {
                paramName = targetName + defaultSuffix;
                internalValue = true; // 名前を自動生成したら必ず内部値
            }

            string menuTitle = onoffComponent.m_MenuTitle;
            if ( menuTitle.Length == 0 ) {
                menuTitle = targetName + defaultSuffix;
            }

            // Animator Controller, Layer, Parameter, StateMachine, State, Transiton, Clip
            var newAnimator = obj.AddComponent<Animator>();
            var newCont = new AnimatorController();
            newAnimator.runtimeAnimatorController = newCont;

            GenerateAnims( onoffComponent.m_OnOffTarget, newCont, paramName, showOnLoad);

            // MA Merge Animator, MA Parameters
            SetMAMergeAnimator( obj, newCont );
            SetMAParameter( obj, paramName, internalValue, saved );

            // Menu Item and Installer
            var empty = new GameObject(menuTitle); 
            empty.transform.SetParent(obj.transform);
            SetMAMenu( empty, menuTitle, paramName );

            //  obj に既に MenuInstaller がついている場合はその設定を継承する
            ModularAvatarMenuInstaller menuInst = obj.GetComponent<ModularAvatarMenuInstaller>() as ModularAvatarMenuInstaller;
            if ( menuInst != null ) {
                menuInst.installTargetMenu = onoffComponent.m_MenuPlace;
            }
            else {
                ModularAvatarMenuInstaller menuInstNew = empty.AddComponent<ModularAvatarMenuInstaller>() as ModularAvatarMenuInstaller;
                menuInstNew.installTargetMenu = onoffComponent.m_MenuPlace;
            }
        }

        private void SetMAMergeAnimator( GameObject obj, AnimatorController newCont  )
        {
            ModularAvatarMergeAnimator ani = obj.AddComponent<ModularAvatarMergeAnimator>() as ModularAvatarMergeAnimator;
            ani.animator = newCont;
            ani.deleteAttachedAnimator = true;
            ani.matchAvatarWriteDefaults = true;
            ani.pathMode = MergeAnimatorPathMode.Absolute;
        }

        private void SetMAMenu( GameObject empty, string menuTitle, string paramName )
        {
            ModularAvatarMenuItem item = empty.AddComponent<ModularAvatarMenuItem>() as ModularAvatarMenuItem;
			var control = new VRCExpressionsMenu.Control();
			control.style = 0;
			control.labels = new VRCExpressionsMenu.Control.Label[0];
			control.parameter = new VRCExpressionsMenu.Control.Parameter();
			control.subParameters = new VRCExpressionsMenu.Control.Parameter[0];
			control.name = menuTitle;
			control.parameter.name = paramName;
			control.type = VRCExpressionsMenu.Control.ControlType.Toggle;
			control.value = 1;
			item.Control = control;
        }

		private void SetMAParameter( GameObject obj, string name, bool internalParameter, bool saved )
		{
            ModularAvatarParameters maParam = obj.AddComponent<ModularAvatarParameters>() as ModularAvatarParameters;

			ParameterConfig pc = new ParameterConfig(); // struct
			pc.nameOrPrefix = name;
			pc.isPrefix = false;
			pc.internalParameter = internalParameter;
			pc.syncType = ParameterSyncType.Bool;
            pc.saved = saved;
			maParam.parameters.Add(pc);
		}

        // AnimationClip, Curve, State, Transition を StateMachine に追加
 		private void GenerateAnims(GameObject target, AnimatorController newCont, string paramName, bool defaultOn = true ) 
		{
            string objectPath = GetRelativePathFromRootObject(target);

            newCont.AddParameter( paramName, AnimatorControllerParameterType.Bool );

            AnimatorControllerLayer newlayer = new AnimatorControllerLayer
            {
                name = target.name, 
                defaultWeight = 1,
                stateMachine = new AnimatorStateMachine(),
                blendingMode = AnimatorLayerBlendingMode.Override
            };
            newCont.AddLayer( newlayer );
            newlayer.stateMachine.hideFlags = HideFlags.HideInHierarchy;
            newlayer.stateMachine.name = newlayer.name;

            AnimationClip animOn = new AnimationClip();
            animOn.name = target.name + "_on";
            AnimationClip animOff = new AnimationClip();
            animOff.name = target.name + "_off";

			AnimationCurve curveOn = new AnimationCurve( new Keyframe( 0f, 1f ));
			AnimationCurve curveOff = new AnimationCurve( new Keyframe( 0f, 0f ));

			animOn.SetCurve( objectPath, typeof( GameObject ), "m_IsActive", curveOn );
			animOff.SetCurve( objectPath, typeof( GameObject ), "m_IsActive", curveOff );

			newlayer.stateMachine.AddState( ToStateName(animOff.name));
			newlayer.stateMachine.AddState( ToStateName(animOn.name));
			newlayer.stateMachine.defaultState = newlayer.stateMachine.states[ defaultOn ? 1 : 0 ].state;

			newlayer.stateMachine.states[0].state.motion = animOff;
			newlayer.stateMachine.states[1].state.motion = animOn;
			newlayer.stateMachine.states[0].state.writeDefaultValues = false;
			newlayer.stateMachine.states[1].state.writeDefaultValues = false;

			newlayer.stateMachine.states[0].state.AddTransition( newlayer.stateMachine.states[1].state );
			newlayer.stateMachine.states[1].state.AddTransition( newlayer.stateMachine.states[0].state );
			newlayer.stateMachine.states[0].state.transitions[0].hasExitTime = false;
			newlayer.stateMachine.states[1].state.transitions[0].hasExitTime = false;
            
			if ( defaultOn ) {
				newlayer.stateMachine.states[0].state.transitions[0].AddCondition( AnimatorConditionMode.IfNot, 0, paramName );
				newlayer.stateMachine.states[1].state.transitions[0].AddCondition( AnimatorConditionMode.If, 0, paramName );
			}
			else {
				newlayer.stateMachine.states[0].state.transitions[0].AddCondition( AnimatorConditionMode.If, 0, paramName );
				newlayer.stateMachine.states[1].state.transitions[0].AddCondition( AnimatorConditionMode.IfNot, 0, paramName );
			}

		}

 
    }
}