using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Avatars.ScriptableObjects;

namespace SakuraScript.OnOffMenu {
    [AddComponentMenu("SakuraComponents/SakuraOnOffMenuNDMF")]
    [DisallowMultipleComponent]
    public class SakuraOnOffMenuNDMF : MonoBehaviour, VRC.SDKBase.IEditorOnly 
    {
        [Header("Mandatory (必ず指定してください)")]
        [Tooltip("オンオフするオブジェクト")]	
        public GameObject m_OnOffTarget;

        [Tooltip("最初から表示しておきたいもの(服など)はチェックあり\n最初は消しておくもの（パーティクルなど）はチェックなし")]	
        public bool m_ShowOnLoad = true;

        [Header("Optional (変更しなくてもOK）")]
        [Tooltip("メニュー項目名（空なら自動生成）")]	
        public string m_MenuTitle;

        [Tooltip("メニューを追加する場所（Noneなら自動処理）")]	
        public VRCExpressionsMenu m_MenuPlace;

        [Tooltip("パラメーター名（空なら自動生成）\n（他のアニメーション等で操作したいなら指定する）")]	
        public string m_ParameterName;
        [Tooltip("パラメーター名を内部値にするかどうか\n（他のアニメーション等で操作したいなら外す）")]	
        public bool m_InternalValue = true;

        [Tooltip("パラメーターを保存するかどうか")]	
        public bool m_SaveParameter = true;
    }
}
