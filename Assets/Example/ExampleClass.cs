/*
    ExampleClass.cs       （※ クラス名）
    20261013  hanaue sho  （※ 作成日時と制作者名）
    命名規則の例          （※ クラスの大まかな説明）
 */
using UnityEngine;

/*
    【概要説明】
    スクリプトの“ヘッダーコメント”と、privateフィールドや関数をわかりやすく区切る“セクションコメント”を必須とする。
    
    また、クラス内の大まかな記載順は以下とする。
    １．変数
    ２．Unity標準の関数
    ３．自作関数
    （※ 自作関数は基本的に使うもの順に、つまり上から読めば処理をなるべく追跡できるようにしましょう）
    （※ public 関数と private 関数の順については厳守する必要はない。ただし、セッターやゲッターはなるべくまとめるようにしましょう）
    （※ 変数内の順は厳密には定義しないが、優先度の高さ順や、インスペクター上での表示の利便性を意識してください）    

    【セクションコメントについて】
    変数や関数を大まかに区切る目印的なもの。全ての関数につける必要はなく、処理の大まかな区分け。
    セクションコメントは以下のように“=”を５０個使い線を引き、中に単語を書き“-”５個ずつで挟む。

    // ==================================================
    // ----- Section Comment -----
    // ==================================================
    
    また、必要に応じて以下のように補足も書く

    // ==================================================
    // ----- Section Comment -----
    // ここに補足を書く
    // ==================================================

    以下は例となるクラス ExampleClass の作成例です。
    また、セクションコメントに補足がある場合は補足部の（※ ）内に記載するが、
    この補足は今回限りなのでコピペで毎回記載しなくてよい。

    【インスペクターに表示される変数について】
    インスペクター上に表示される変数には必ず
    [Header("ヘッダー")] と [Tooltip("ツールチップ（補足）")]
    を付けましょう。
    
 */
/*
    【設計思想について】
    設計については以下を遵守
    ・インスペクターからの手動参照をできる限り無くす
    ・Awake, または Start 関数で参照チェックを行い、Debug.LogWarning または Debug.LogError を必ず呼び出す。
    
    【インスペクターからの手動参照について】
    インスペクターからの手動参照はエラーの温床になりやすく、特にチーム制作では制作者しか修正できない場合が多い。
    これを避けるために、Awake 関数で以下の関数を利用して参照を作る。

    ・特定のコンポーネントを探す場合
    GetComponent<T>()               // 同じ GameObject についているコンポーネントを取得
    GetComponentInChildren<T>()     // 子階層にある特定のコンポーネントを取得（自分自身も検索対象に含む）
    GetComponentsInChildren<T>()    // 子階層にある特定のコンポーネントを複数取得
    GetComponentInParent<T>()       // 親階層にある特定のコンポーネントを取得
    FindFirstObjectByType<T>()      // シーン全体から特定のコンポーネントを取得（最初の１つを取得）
    FindAnyObjectByType<T>()        // シーン全体から特定のコンポーネントを取得（どれでもいいから１つ取得）
    FindObjectsByType<T>()          // シーン全体から同じコンポーネントを全部取得（引数にFindObjectsSortMode.Noneを指定することで順不同で取得可能）

    ・タグから探す
    GameObject.FindWithTag("Tag")   // シーン上から Tag というタグを持つ GameObject を取得
    GameObject.FindGameObjectsWithTag("Tag") // アクティブな GameObject のみを検索対象

    ・GameObject 名から探す場合（非推奨）
    GameObject.Find("Player")       // シーン上から Player という名前の GameObject を取得
    transform.Find("Model")         // 子オブジェクトから Model という名前の transform を取得

    【参照チェックについて】
    外部参照が必要な場合は、取得時に必ず null チェックを行いましょう。
    null だった場合は以下のどちらかのログを表示する。
    
    DebugLog.Warning("[Warning] ...")   // 黄色のログで警告を出す。（※ [Warning] を最初にいれる）
    DebugLog.Error("[Error] ...")       // 赤色のログで警告を出す。実行が一時停止する（※ [Error] を最初にいれる）
    
    使い分けについては、必須の参照には基本的に Error を出す。
    無くても動く参照には Warning で警告を出す。
 */

public class ExampleClass : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Private Propaty -----
    // （※ 困ったらとりあえず 〇〇 Propaty と記載しましょう）
    // --------------------------------------------------
    [Header("Example")]
    [Tooltip("float 型の例")]
    [SerializeField] private float _exampleFloat;
    public float ExampleFloat => _exampleFloat;

    [Tooltip("参照の例")]
    [SerializeField] private ExampleClass _exampleClass;

    // --------------------------------------------------
    // ----- Struct Example -----
    // --------------------------------------------------
    private struct ExampleStruct
    {
        public float Example;
    }

    // --------------------------------------------------
    // ----- Enum Example -----
    // --------------------------------------------------
    private enum ExampleEnum
    {
        None,    // 不都合がない限り None を最初にいれる
        Example,
    }
    [Header("Enum")]
    [Tooltip("Enum 型の例")]
    [SerializeField] private ExampleEnum _exampleEnum;

    // --------------------------------------------------
    // ----- Unity Events -----
    //  （※ Unity 標準の関数群の最初に記載）
    // --------------------------------------------------
    void Start()
    {
        _exampleClass = GameObject.FindAnyObjectByType<ExampleClass>();
        if (_exampleClass == null)
        {
            Debug.LogWarning("[Warning] ExampleClass Not Found!");
        }
    }
    void Update()
    {
        
    }

    // --------------------------------------------------
    // ----- ExampleMethod -----
    // --------------------------------------------------
    private void ExampleMethod(float example)
    {

    }
}
