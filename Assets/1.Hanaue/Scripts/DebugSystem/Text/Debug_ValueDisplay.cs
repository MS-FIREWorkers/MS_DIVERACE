/*
    Debug_ValueDisplay
    20261021  hanaue sho
    特定の変数の値を表示する
    TextMeshPro に対応
 */
using System.Reflection;
using System;
using TMPro;
using UnityEngine;

public class Debug_ValueDisplay : MonoBehaviour
{
    // --------------------------------------------------
    // ----- Target -----
    // --------------------------------------------------
    [Header("取得対象")]
    [Tooltip("対象のスクリプトを設定")]
    [SerializeField] private MonoBehaviour _targetScript;
    [Tooltip("対象の変数名を設定")]
    [SerializeField] private string _variableName;

    // --------------------------------------------------
    // ----- Text -----
    // --------------------------------------------------
    [Header("表示設定")]
    [Tooltip("前文")]
    [SerializeField] private string _preText = "";
    [Tooltip("後文")]
    [SerializeField] private string _sufText = "";

    // --------------------------------------------------
    // ----- Private Property -----
    // --------------------------------------------------
    private TMP_Text _targetText;
    private FieldInfo _field;
    private PropertyInfo _property;


    // --------------------------------------------------
    // ----- Unity Events -----
    // --------------------------------------------------
    private void Awake()
    {
        // 同じGameObjectにアタッチされているTextMeshProを取得
        _targetText = GetComponent<TMP_Text>();

        // 対象スクリプトが未設定、または変数名が未入力の場合は処理を終了
        if (_targetScript == null || string.IsNullOrEmpty(_variableName))
        {
            return;
        }

        // 対象スクリプトの型情報を取得
        Type type = _targetScript.GetType();

        // リフレクションで検索するメンバーの条件を指定
        const BindingFlags flags = BindingFlags.Instance |       // インスタンスメンバーを対象
                                   BindingFlags.Public |         // publicメンバーを対象
                                   BindingFlags.NonPublic |      // privateやprotectedなども対象
                                   BindingFlags.DeclaredOnly;    // 現在のクラスで宣言されたメンバーのみ対象

        // 対象スクリプトとその継承元クラスを順番に検索
        while (type != null && type != typeof(object))
        {
            // 指定した名前のフィールド（変数）を検索
            _field = type.GetField(_variableName, flags);

            // フィールドが見つかった場合は検索を終了
            if (_field != null)
            {
                break;
            }

            // フィールドが見つからなかった場合はプロパティを検索
            _property = type.GetProperty(_variableName, flags);

            // プロパティが存在し、引数が不要で、値を読み取れる場合は検索を終了
            if (_property != null && _property.GetIndexParameters().Length == 0 && _property.GetGetMethod(true) != null)
            {
                break;
            }

            // 有効なプロパティが見つからなかったため情報をリセット
            _property = null;

            // 継承元のクラスに移動して検索を続ける
            type = type.BaseType;
        }
    }

    private void Update()
    {
        // 対象スクリプトまたは表示先のTextMeshProが存在しない場合は処理しない
        if (_targetScript == null || _targetText == null)
        {
            return;
        }

        // リフレクションによる値の取得で例外が発生する可能性があるためtry-catchを使用
        try
        {
            // 取得した値を格納する変数
            // object型を使うことでint、float、boolなど異なる型の値を扱える
            object value;

            if (_field != null)
            {
                // フィールドから現在の値を取得
                value = _field.GetValue(_targetScript);
            }
            else if (_property != null)
            {
                // プロパティから現在の値を取得
                value = _property.GetValue(_targetScript);
            }
            else
            {
                // 指定したフィールド・プロパティが見つからなかった場合
                _targetText.text = "Not Found";
                return;
            }

            // 取得した値を文字列に変換し、前後に指定した文章を追加して表示
            // ?. : valueがnullでなければToString()を実行
            // ?? : valueがnullの場合は文字列"null"を使用
            _targetText.text = _preText + (value?.ToString() ?? "null") + _sufText;
        }
        catch (Exception)
        {
            // 値の取得中に例外が発生した場合はエラーメッセージを表示
            _targetText.text = "Read Error";
        }
    }
}
