using System.Collections.Generic;
using UnityEngine;

// 入口用の判定を見た目から分離し、BoxColliderのCenter/Sizeで発動範囲を調整する。
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class SkyshaftEntranceTrigger : MonoBehaviour
{
    [Tooltip("再生するスカイシャフト。未指定なら親から探す")]
    public SkyshaftPrototype effect;

    [Tooltip("このタグを持つオブジェクトをPlayerと判定。子Colliderから親階層も調べます。")]
    public string playerTag = "Player";

    [Tooltip("有効なら一度だけ発動。無効なら範囲を完全に出てから再進入すると再発動")]
    public bool triggerOnce = true;

    // 手足など複数のColliderが入っても、同じ進入で何度も再生しない。
    readonly HashSet<Collider> overlapping = new HashSet<Collider>();
    bool activated;

    void Reset()
    {
        effect = GetComponentInParent<SkyshaftPrototype>();

        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(4f, 4f, 1f);
        ConfigureBody();
    }

    void Awake()
    {
        if (effect == null) 
            effect = GetComponentInParent<SkyshaftPrototype>();

        // TriggerなのでPlayerを押し返さない。判定側にRigidbodyを持たせ、相手の構成への依存を減らす。
        GetComponent<BoxCollider>().isTrigger = true;
        ConfigureBody();
    }

    void ConfigureBody()
    {
        var body = GetComponent<Rigidbody>();

        // 重力や衝突で入口が動かないよう、物理判定だけに利用する。
        body.isKinematic = true;
        body.useGravity = false;
    }

    Transform FindPlayer(Collider other)
    {
        // シーン上のPlayer参照は不要。生成が遅いPlayerも、接触時にタグで判別する。
        if (string.IsNullOrEmpty(playerTag)) return null;
        // タグ文字列を比較することで、未登録のタグ名でも例外で停止しない。
        for (var current = other.transform; current != null; current = current.parent)
        {
            if (current.tag == playerTag)
                return current;
        }

        return null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (effect == null || !effect.isActiveAndEnabled) 
            return;

        var enteredPlayer = FindPlayer(other);
        if (enteredPlayer == null) 
            return;

        // 無効化・破棄されたColliderが残っていたら除去し、再進入を妨げないようにする。
        overlapping.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        if (!overlapping.Add(other) || overlapping.Count != 1)
            return;

        if ((triggerOnce && activated) || effect.IsPlaying)
            return;

        // 毎回接触したPlayerを採用するため、Playerが再生成されても参照設定は不要。
        effect.SetTaggedCharacter(enteredPlayer);
        
        activated = true;
        effect.PlayWhiteout();
    }

    void OnTriggerExit(Collider other)
    {
        // Playerの全Colliderが出るまでは、再進入による再発動を受け付けない。
        overlapping.Remove(other);
    }

    void OnDisable()
    {
        overlapping.Clear();
    }

    public void ResetTrigger()
    {
        // 演出側のReset Previewから呼び、一度だけ発動した入口を再利用できるようにする。
        activated = false;
        overlapping.Clear();
    }

    void OnDrawGizmosSelected()
    {
        // 選択中は実際のBoxCollider範囲を表示。親の回転・拡大率も反映する。
        var box = GetComponent<BoxCollider>();
        if (box == null) 
            return;

        var previousMatrix = Gizmos.matrix;
        var previousColor = Gizmos.color;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.5f, 0.15f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(box.center, box.size);
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }
}
