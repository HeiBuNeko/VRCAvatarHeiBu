using satania.tabakosystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using VRC.SDK3.Dynamics.Contact.Components;
using static net.satania_shopping.tabakosystem.editor.TabakoPrefabDataContainer;
using static net.satania_shopping.tabakosystem.runtime.TabakoScriptBehaviour;
using net.satania_shopping.tabakosystem.runtime;
using static VRC.Dynamics.ContactBase;
using static VRC.Dynamics.ContactReceiver;

using BehaviourHandGesture = net.satania_shopping.tabakosystem.runtime.TabakoScriptBehaviour.BehaviourHandGesture;

namespace net.satania_shopping.tabakosystem.editor
{
    public static class Extensions
    {
        public static Vector3 GetLocalScale(this Transform transform)
        {
            if (transform == null)
                return new Vector3(0, 0, 0);

            return transform.localScale;
        }

        public static Placement GetPlacement(this Transform transform)
        {
            if (transform == null)
                return null;

            return new Placement()
            {
                localPosition = transform.localPosition,
                localRotation = transform.localRotation
            };
        }

        public static Placement GetPlacement(this Transform transform, Transform root)
        {
            if (transform == null)
                return null;

            if (root == null)
                return transform.GetPlacement();

            Vector3 localPosition = root.InverseTransformPoint(transform.position);
            Quaternion localRotation = Quaternion.Inverse(root.rotation) * transform.rotation;

            return new Placement()
            {
                localPosition = localPosition,
                localRotation = localRotation
            };
        }

        public static void SetLocalScale(this Transform transform, Vector3 localScale)
        {
            if (transform == null)
                return;

            transform.localScale = localScale;
        }

        public static void SetPlacement(this Transform transform, Placement placement)
        {
            if (transform == null)
                return;

            transform.localPosition = placement.localPosition;
            transform.localRotation = placement.localRotation;
        }

        public static void SetPlacement(this Transform transform, Placement placement, Transform root)
        {
            if (transform == null)
                return;

            // ローカル座標をグローバル座標に変換
            Vector3 globalPosition = root.TransformPoint(placement.localPosition);

            // ローカル回転をグローバル回転に変換
            Quaternion globalRotation = root.rotation * placement.localRotation;

            transform.position = globalPosition;
            transform.rotation = globalRotation;
        }

        public static void SetReceiverData(this VRCContactReceiver vrcContactReceiver, ContactReceiver receiverData)
        {
            //vrcContactReceiver.rootTransform = receiverData.rootTransform;
            vrcContactReceiver.shapeType = receiverData.shapeType;
            vrcContactReceiver.radius = receiverData.radius;
            vrcContactReceiver.height = receiverData.height;
            vrcContactReceiver.position = receiverData.position;
            vrcContactReceiver.rotation = receiverData.rotation;
            //vrcContactReceiver.localOnly = receiverData.localOnly;
            //vrcContactReceiver.collisionTags = new List<string>(receiverData.collisionTags);
            //vrcContactReceiver.allowInit = receiverData.allowInit;
            //vrcContactReceiver.playerId = receiverData.playerId;
            //vrcContactReceiver.allowSelf = receiverData.allowSelf;
            //vrcContactReceiver.allowOthers = receiverData.allowOthers;
            //vrcContactReceiver.receiverType = receiverData.receiverType;
            //vrcContactReceiver.parameter = receiverData.parameter;
        }

        public static void SetSenderData(this VRCContactSender vrcContactSender, ContactSender senderData)
        {
            //vrcContactSender.rootTransform = senderData.rootTransform;
            vrcContactSender.shapeType = senderData.shapeType;
            vrcContactSender.radius = senderData.radius;
            vrcContactSender.height = senderData.height;
            vrcContactSender.position = senderData.position;
            vrcContactSender.rotation = senderData.rotation;
            //vrcContactSender.localOnly = senderData.localOnly;
            //vrcContactSender.collisionTags = new List<string>(senderData.collisionTags);
            //vrcContactSender.allowInit = senderData.allowInit;
            //vrcContactSender.playerId = senderData.playerId;
        }
    }

    public sealed class TabakoPrefabDataContainer
    {
        public class Placement
        {
            public Vector3 localPosition;
            public Quaternion localRotation;

            public static Placement Identity()
            {
                return new Placement()
                {
                    localPosition = new Vector3(0, 0, 0),
                    localRotation = new Quaternion(0, 0, 0, 0)
                };
            }
        }

        public class ContactBase
        {
            //public const float MAX_SIZE = 6;
            //public const int MAX_COLLISION_TAGS = 16;

            public Transform rootTransform;
            public ShapeType shapeType;
            public float radius;
            public float height;
            public Vector3 position;
            public Quaternion rotation;
            public bool localOnly;
            public List<string> collisionTags;
            public bool allowInit;
            public int playerId;

            //public static ContactBase GetContactData(VRCContactBase @base)
            //{
            //    if (@base == null)
            //        return null;

            //    ContactBase data = new ContactBase();

            //    data.rootTransform = @base.rootTransform;
            //    data.shapeType = @base.shapeType;
            //    data.radius = @base.radius;
            //    data.height = @base.height;
            //    data.position = @base.position;
            //    data.rotation = @base.rotation;
            //    data.localOnly = @base.localOnly;
            //    data.collisionTags = new List<string>(@base.collisionTags);
            //    data.allowInit = @base.allowInit;
            //    data.playerId = @base.playerId;

            //    return data;
            //}
        }

        public class ContactReceiver : ContactBase
        {
            public bool allowSelf;
            public bool allowOthers;

            public ReceiverType receiverType;
            public string parameter;

            public static ContactReceiver GetReceiverData(VRCContactReceiver receiver)
            {
                if (receiver == null)
                    return null;

                ContactReceiver receiverData = new ContactReceiver();

                receiverData.rootTransform = receiver.rootTransform;
                receiverData.shapeType = receiver.shapeType;
                receiverData.radius = receiver.radius;
                receiverData.height = receiver.height;
                receiverData.position = receiver.position;
                receiverData.rotation = receiver.rotation;
                receiverData.localOnly = receiver.localOnly;
                receiverData.collisionTags = new List<string>(receiver.collisionTags);
                receiverData.allowInit = receiver.allowInit;
                receiverData.playerId = receiver.playerId;
                receiverData.allowSelf = receiver.allowSelf;
                receiverData.allowOthers = receiver.allowOthers;
                receiverData.receiverType = receiver.receiverType;
                receiverData.parameter = receiver.parameter;

                return receiverData;
            }
        }

        public class ContactSender : ContactBase
        {
            public static ContactSender GetSenderData(VRCContactSender sender)
            {
                var data = new ContactSender();

                data.rootTransform = sender.rootTransform;
                data.shapeType = sender.shapeType;
                data.radius = sender.radius;
                data.height = sender.height;
                data.position = sender.position;
                data.rotation = sender.rotation;
#if VRC_AVATARS_3_7_6_OR_NEWER
                data.localOnly = sender.localOnly;
#endif
                data.collisionTags = new List<string>(sender.collisionTags);
                data.allowInit = sender.allowInit;
                data.playerId = sender.playerId;

                return data;
            }

        }

        public PrefabVersion PrefabVersion = PrefabVersion._1;
        public BehaviourHandGesture GestureCase = BehaviourHandGesture.thumbsup;
        public BehaviourHandGesture GestureLighter = BehaviourHandGesture.rocknroll;
        public BehaviourHandGesture GestureFire = BehaviourHandGesture.fist;
        public BehaviourHandGesture GestureRestore = BehaviourHandGesture.rocknroll;
        public BehaviourHandGesture GestureSmoke = BehaviourHandGesture.victory;
        public BehaviourHandGesture GestureSwap = BehaviourHandGesture.thumbsup;

        public Placement RootPlacement;
        public Vector3? RootScale;

        public Vector3? CaseScale = null; //ケース追従先
        public Vector3? TabakoScale = null; //タバコ追従先
        public Vector3? LighterScale = null; //ライター追従先


        public Placement CS_Case_R;
        public Placement CS_Case_L;
        public Placement CS_Tabako_R;
        public Placement CS_Tabako_L;
        public Placement CS_Tabako_Mouth;
        public Placement CS_Lighter_R;
        public Placement CS_Lighter_L;
        public Placement CS_Mouth;

        public ContactReceiver Pocket_R_Receiver;
        public Placement Pocket_R_Receiver_Place;

        public ContactReceiver Pocket_L_Receiver;
        public Placement Pocket_L_Receiver_Place;

        private static TabakoPrefabDataContainer GetOlder1_3_2(Transform prefabRoot)
        {
            TabakoPrefabDataContainer dataContainer = new TabakoPrefabDataContainer();
            dataContainer.PrefabVersion = PrefabVersion._1;

            dataContainer.RootPlacement = prefabRoot.GetPlacement();
            dataContainer.RootScale = prefabRoot.GetLocalScale();

            TabakoScript tabakoScript = prefabRoot.GetComponent<TabakoScript>();
            if (tabakoScript != null)
            {
                dataContainer.GestureCase = (BehaviourHandGesture)tabakoScript.gesture_case;
                dataContainer.GestureLighter = (BehaviourHandGesture)tabakoScript.gesture_lighter;
                dataContainer.GestureFire = (BehaviourHandGesture)tabakoScript.gesture_fire;
                dataContainer.GestureRestore = (BehaviourHandGesture)tabakoScript.gesture_restore;
                //dataContainer.GestureSmoke = (HandGesture)tabakoScript.gesture_smoke;
                dataContainer.GestureSwap = (BehaviourHandGesture)tabakoScript.gesture_swap;
            }

            //オブジェクト/タバコ/タバコ本体　スケール
            //オブジェクト/ケース/ケース本体　スケール
            //オブジェクト/ライター/ライター本体　スケール
            Transform OBJ_T_Case = F("オブジェクト/ケース/ケース本体");
            Transform OBJ_T_Tabako = F("オブジェクト/タバコ/タバコ本体");
            Transform OBJ_T_Lighter = F("オブジェクト/ライター/ライター本体");

            dataContainer.CaseScale = OBJ_T_Case.GetLocalScale();
            dataContainer.TabakoScale = OBJ_T_Tabako.GetLocalScale();
            dataContainer.LighterScale = OBJ_T_Lighter.GetLocalScale();

            //追従先/ケース/ケース_右手　位置、回転
            //追従先/ケース/ケース_左手　位置、回転
            Transform CS_Case_R = F("追従先/ケース/ケース_右手");
            Transform CS_Case_L = F("追従先/ケース/ケース_左手");

            dataContainer.CS_Case_R = CS_Case_R.GetPlacement(prefabRoot);
            dataContainer.CS_Case_L = CS_Case_L.GetPlacement(prefabRoot);

            //追従先/タバコ/タバコ_右手　位置、回転
            //追従先/タバコ/タバコ_左手　位置、回転
            Transform CS_Tabako_R = F("追従先/タバコ/タバコ_右手");
            Transform CS_Tabako_L = F("追従先/タバコ/タバコ_左手");

            dataContainer.CS_Tabako_R = CS_Tabako_R.GetPlacement(prefabRoot);
            dataContainer.CS_Tabako_L = CS_Tabako_L.GetPlacement(prefabRoot);

            //追従先/タバコ/タバコ_首/タバコ_頭　位置、回転
            //追従先/タバコ/タバコ_頭　位置、回転
            Transform CS_Tabako_M = F("追従先/タバコ/タバコ_首/タバコ_頭");
            if (CS_Tabako_M == null)
                CS_Tabako_M = F("追従先/タバコ/タバコ_頭");

            dataContainer.CS_Tabako_Mouth = CS_Tabako_M.GetPlacement(prefabRoot);

            //追従先/ライター/ライター_右手　位置、回転
            //追従先/ライター/ライター_左手　位置、回転
            Transform CS_Lighter_R = F("追従先/ライター/ライター_右手");
            Transform CS_Lighter_L = F("追従先/ライター/ライター_左手");
            dataContainer.CS_Lighter_R = CS_Lighter_R.GetPlacement(prefabRoot);
            dataContainer.CS_Lighter_L = CS_Lighter_L.GetPlacement(prefabRoot);

            //追従先/口から出す煙 位置、回転
            Transform CS_Mouth_Smoke = F("追従先/口から出す煙");
            dataContainer.CS_Mouth = CS_Mouth_Smoke.GetPlacement(prefabRoot);

            //コンタクト/ポケット_右手 VRCContactReceiver
            //コンタクト/ポケット_左手 VRCContactReceiver
            Transform Cnt_Pocket_R = F("コンタクト/ポケット_右手");
            dataContainer.Pocket_R_Receiver_Place = Cnt_Pocket_R.GetPlacement(prefabRoot);
            dataContainer.Pocket_R_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_R?.GetComponent<VRCContactReceiver>());

            Transform Cnt_Pocket_L = F("コンタクト/ポケット_左手");
            dataContainer.Pocket_L_Receiver_Place = Cnt_Pocket_L.GetPlacement(prefabRoot);
            dataContainer.Pocket_L_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_L?.GetComponent<VRCContactReceiver>());

            return dataContainer;

            Transform F(string n) => prefabRoot.transform.Find(n);
        }

        private static TabakoPrefabDataContainer GetOlder1_5_4(Transform prefabRoot)
        {
            TabakoPrefabDataContainer dataContainer = new TabakoPrefabDataContainer();
            dataContainer.PrefabVersion = PrefabVersion._2;

            dataContainer.RootPlacement = prefabRoot.GetPlacement();
            dataContainer.RootScale = prefabRoot.GetLocalScale();

            TabakoScript tabakoScript = prefabRoot.GetComponent<TabakoScript>();
            if (tabakoScript != null)
            {
                dataContainer.GestureCase = (BehaviourHandGesture)tabakoScript.gesture_case;
                dataContainer.GestureLighter = (BehaviourHandGesture)tabakoScript.gesture_lighter;
                dataContainer.GestureFire = (BehaviourHandGesture)tabakoScript.gesture_fire;
                dataContainer.GestureRestore = (BehaviourHandGesture)tabakoScript.gesture_restore;
                //dataContainer.GestureSmoke = (HandGesture)tabakoScript.gesture_smoke;
                dataContainer.GestureSwap = (BehaviourHandGesture)tabakoScript.gesture_swap;
            }

            //オブジェクト/タバコ/タバコ本体　スケール
            //オブジェクト/ケース/ケース本体　スケール
            //オブジェクト/ライター/ライター本体　スケール
            Transform OBJ_T_Case = F("オブジェクト/ケース/ケース本体");
            Transform OBJ_T_Tabako = F("オブジェクト/タバコ/タバコ本体");
            Transform OBJ_T_Lighter = F("オブジェクト/ライター/ライター本体");

            dataContainer.CaseScale = OBJ_T_Case.GetLocalScale();
            dataContainer.TabakoScale = OBJ_T_Tabako.GetLocalScale();
            dataContainer.LighterScale = OBJ_T_Lighter.GetLocalScale();

            //追従先/ケース/ケース_右手　位置、回転
            //追従先/ケース/ケース_左手　位置、回転
            Transform CS_Case_R = F("追従先/ケース/ケース_右手");
            Transform CS_Case_L = F("追従先/ケース/ケース_左手");

            dataContainer.CS_Case_R = CS_Case_R.GetPlacement(prefabRoot);
            dataContainer.CS_Case_L = CS_Case_L.GetPlacement(prefabRoot);

            //追従先/タバコ/タバコ_右手　位置、回転
            //追従先/タバコ/タバコ_左手　位置、回転
            Transform CS_Tabako_R = F("追従先/タバコ/タバコ_右手");
            Transform CS_Tabako_L = F("追従先/タバコ/タバコ_左手");

            dataContainer.CS_Tabako_R = CS_Tabako_R.GetPlacement(prefabRoot);
            dataContainer.CS_Tabako_L = CS_Tabako_L.GetPlacement(prefabRoot);

            //追従先/タバコ/タバコ_頭　位置、回転
            Transform CS_Tabako_M = F("追従先/タバコ/タバコ_頭");
            dataContainer.CS_Tabako_Mouth = CS_Tabako_M.GetPlacement(prefabRoot);

            //追従先/ライター/ライター_右手　位置、回転
            //追従先/ライター/ライター_左手　位置、回転
            Transform CS_Lighter_R = F("追従先/ライター/ライター_右手");
            Transform CS_Lighter_L = F("追従先/ライター/ライター_左手");
            dataContainer.CS_Lighter_R = CS_Lighter_R.GetPlacement(prefabRoot);
            dataContainer.CS_Lighter_L = CS_Lighter_L.GetPlacement(prefabRoot);

            //追従先/口から出す煙 位置、回転
            Transform CS_Mouth_Smoke = F("追従先/口から出す煙");
            dataContainer.CS_Mouth = CS_Mouth_Smoke.GetPlacement(prefabRoot);

            //コンタクト/ポケット_右手 VRCContactReceiver
            //コンタクト/ポケット_左手 VRCContactReceiver
            Transform Cnt_Pocket_R = F("コンタクト/ポケット_右手");
            dataContainer.Pocket_R_Receiver_Place = Cnt_Pocket_R.GetPlacement(prefabRoot);
            dataContainer.Pocket_R_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_R?.GetComponent<VRCContactReceiver>());

            Transform Cnt_Pocket_L = F("コンタクト/ポケット_左手");
            dataContainer.Pocket_L_Receiver_Place = Cnt_Pocket_L.GetPlacement(prefabRoot);
            dataContainer.Pocket_L_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_L?.GetComponent<VRCContactReceiver>());

            return dataContainer;

            Transform F(string n) => prefabRoot.transform.Find(n);
        }

        private static TabakoPrefabDataContainer GetNewer2_0_0(Transform prefabRoot)
        {
            TabakoPrefabDataContainer dataContainer = new TabakoPrefabDataContainer();
            dataContainer.PrefabVersion = PrefabVersion._3;

            dataContainer.RootPlacement = prefabRoot.GetPlacement();
            dataContainer.RootScale = prefabRoot.GetLocalScale();

            TabakoScriptBehaviour tabakoScript = prefabRoot.GetComponent<TabakoScriptBehaviour>();
            if (tabakoScript != null)
            {
                dataContainer.GestureCase = tabakoScript.G_Case;
                dataContainer.GestureLighter = tabakoScript.G_Lighter;
                dataContainer.GestureFire = tabakoScript.G_Fire;
                dataContainer.GestureRestore = tabakoScript.G_Restore;
                dataContainer.GestureSmoke = tabakoScript.G_Smoke;
                dataContainer.GestureSwap = tabakoScript.G_Swap;
            }

            //スケール用
            //オブジェクト/ケース/ケース追従先
            //オブジェクト/タバコ/タバコ追従先
            //オブジェクト/ライター/ライター追従先

            Transform OBJ_T_Case = F("オブジェクト/ケース/ケース追従先");
            Transform OBJ_T_Tabako = F("オブジェクト/タバコ/タバコ追従先");
            Transform OBJ_T_Lighter = F("オブジェクト/ライター/ライター追従先");

            dataContainer.CaseScale = OBJ_T_Case.GetLocalScale();
            dataContainer.TabakoScale = OBJ_T_Tabako.GetLocalScale();
            dataContainer.LighterScale = OBJ_T_Lighter.GetLocalScale();

            //追従先/ケース/ケース_右手
            //追従先/ケース/ケース_左手

            Transform CS_Case_R = F("追従先/ケース/ケース_右手");
            Transform CS_Case_L = F("追従先/ケース/ケース_左手");

            dataContainer.CS_Case_R = CS_Case_R.GetPlacement(prefabRoot);
            dataContainer.CS_Case_L = CS_Case_L.GetPlacement(prefabRoot);

            //追従先/タバコ/タバコ_右手
            //追従先/タバコ/タバコ_左手
            //追従先/タバコ/タバコ_頭

            Transform CS_Tabako_R = F("追従先/タバコ/タバコ_右手");
            Transform CS_Tabako_L = F("追従先/タバコ/タバコ_左手");
            Transform CS_Tabako_M = F("追従先/タバコ/タバコ_頭");

            dataContainer.CS_Tabako_R = CS_Tabako_R.GetPlacement(prefabRoot);
            dataContainer.CS_Tabako_L = CS_Tabako_L.GetPlacement(prefabRoot);
            dataContainer.CS_Tabako_Mouth = CS_Tabako_M.GetPlacement(prefabRoot);

            //追従先/ライター/ライター_右手
            //追従先/ライター/ライター_左手

            Transform CS_Lighter_R = F("追従先/ライター/ライター_右手");
            Transform CS_Lighter_L = F("追従先/ライター/ライター_左手");

            dataContainer.CS_Lighter_R = CS_Lighter_R.GetPlacement(prefabRoot);
            dataContainer.CS_Lighter_L = CS_Lighter_L.GetPlacement(prefabRoot);

            //追従先/口
            Transform CS_Mouth = F("追従先/口");
            dataContainer.CS_Mouth = CS_Mouth.GetPlacement(prefabRoot);

            //コンタクト/右手ポケット Receiver
            //コンタクト/左手ポケット Receiver
            Transform Cnt_Pocket_R = F("コンタクト/右手ポケット");
            dataContainer.Pocket_R_Receiver_Place = Cnt_Pocket_R.GetPlacement(prefabRoot);
            dataContainer.Pocket_R_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_R.GetComponent<VRCContactReceiver>());

            Transform Cnt_Pocket_L = F("コンタクト/左手ポケット");
            dataContainer.Pocket_L_Receiver_Place = Cnt_Pocket_L.GetPlacement(prefabRoot);
            dataContainer.Pocket_L_Receiver = ContactReceiver.GetReceiverData(Cnt_Pocket_L.GetComponent<VRCContactReceiver>());

            return dataContainer;

            Transform F(string n) => prefabRoot.transform.Find(n);
        }

        public static TabakoPrefabDataContainer GetDataContainer(Transform prefabRoot, PrefabVersion version)
        {
            switch (version)
            {
                case PrefabVersion._1: return GetOlder1_3_2(prefabRoot);
                case PrefabVersion._2: return GetOlder1_5_4(prefabRoot);
                case PrefabVersion._3: return GetNewer2_0_0(prefabRoot);
            }

            return null;
        }

        public void OutputLogValues()
        {
            Type type = GetType();

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            Debug.Log($"--- Fields of {type.Name} ---");
            foreach (FieldInfo field in type.GetFields(flags))
            {
                Debug.Log($"Field: {field.Name}, Type: {field.FieldType}, Value: {field.GetValue(this)}");
            }

            Debug.Log($"--- Properties of {type.Name} ---");
            foreach (PropertyInfo prop in type.GetProperties(flags))
            {
                Debug.Log($"Property: {prop.Name}, Type: {prop.PropertyType}, Value: {prop.GetValue(this)}");
            }

            Debug.Log($"--- Methods of {type.Name} ---");

            foreach (MethodInfo method in type.GetMethods(flags))
            {
                Debug.Log($"Method: {method.Name}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prefab">読み込み済みのプレハブ</param>
        public void CreatePrefabByContainer(GameObject prefab)
        {
            TabakoScriptBehaviour tabakoScript = prefab.GetComponent<TabakoScriptBehaviour>();
            Transform prefabRoot = prefab.transform;

            prefabRoot.SetPlacement(RootPlacement);

            prefabRoot.SetLocalScale((Vector3)RootScale);

            //ハンドジェスチャー適用
            if (tabakoScript != null)
            {
                tabakoScript.G_Case = GestureCase;
                tabakoScript.G_Lighter = GestureLighter;
                tabakoScript.G_Fire = GestureFire;
                tabakoScript.G_Restore = GestureRestore;
                tabakoScript.G_Smoke = GestureSmoke;
                tabakoScript.G_Swap = GestureSwap;
            }

            //スケール適用
            Transform OBJ_T_Case = F("オブジェクト/ケース/ケース追従先");
            Transform OBJ_T_Tabako = F("オブジェクト/タバコ/タバコ追従先");
            Transform OBJ_T_Lighter = F("オブジェクト/ライター/ライター追従先");

            if (CaseScale != null) OBJ_T_Case.SetLocalScale((Vector3)CaseScale);
            if (TabakoScale != null) OBJ_T_Tabako.SetLocalScale((Vector3)TabakoScale);
            if (LighterScale != null) OBJ_T_Lighter.SetLocalScale((Vector3)LighterScale);

            Transform _CS_Case_R = F("追従先/ケース/ケース_右手");
            Transform _CS_Case_L = F("追従先/ケース/ケース_左手");

            if (CS_Case_R != null) _CS_Case_R.SetPlacement(CS_Case_R, prefabRoot);
            if (CS_Case_L != null) _CS_Case_L.SetPlacement(CS_Case_L, prefabRoot);

            Transform _CS_Tabako_R = F("追従先/タバコ/タバコ_右手");
            Transform _CS_Tabako_L = F("追従先/タバコ/タバコ_左手");
            Transform _CS_Tabako_M = F("追従先/タバコ/タバコ_頭");

            if (CS_Tabako_R != null) _CS_Tabako_R.SetPlacement(CS_Tabako_R, prefabRoot);
            if (CS_Tabako_L != null) _CS_Tabako_L.SetPlacement(CS_Tabako_L, prefabRoot);
            if (CS_Tabako_Mouth != null) _CS_Tabako_M.SetPlacement(CS_Tabako_Mouth, prefabRoot);

            Transform _CS_Lighter_R = F("追従先/ライター/ライター_右手");
            Transform _CS_Lighter_L = F("追従先/ライター/ライター_左手");

            if (CS_Lighter_R != null) _CS_Lighter_R.SetPlacement(CS_Lighter_R, prefabRoot);
            if (CS_Lighter_L != null) _CS_Lighter_L.SetPlacement(CS_Lighter_L, prefabRoot);

            Transform _CS_Mouth = F("追従先/口");
            if (CS_Mouth != null) _CS_Mouth.SetPlacement(CS_Mouth, prefabRoot);

            //コンタクト/右手ポケット Receiver       
            Transform Cnt_Pocket_R = F("コンタクト/右手ポケット");
            //コンタクト/左手ポケット Receiver
            Transform Cnt_Pocket_L = F("コンタクト/左手ポケット");

            if (Pocket_R_Receiver_Place != null) Cnt_Pocket_R.SetPlacement(Pocket_R_Receiver_Place, prefabRoot);
            if (Pocket_L_Receiver_Place != null) Cnt_Pocket_L.SetPlacement(Pocket_L_Receiver_Place, prefabRoot);

            VRCContactReceiver Cnt_Pocket_R_Receiver = Cnt_Pocket_R.GetComponent<VRCContactReceiver>();
            VRCContactReceiver Cnt_Pocket_L_Receiver = Cnt_Pocket_L.GetComponent<VRCContactReceiver>();

            if (Cnt_Pocket_R_Receiver != null && Pocket_R_Receiver != null)
                Cnt_Pocket_R_Receiver.SetReceiverData(Pocket_R_Receiver);

            if (Cnt_Pocket_L_Receiver != null && Pocket_L_Receiver != null)
                Cnt_Pocket_L_Receiver.SetReceiverData(Pocket_L_Receiver);

            Transform F(string n) => prefab.transform.Find(n);
        }
    }
}