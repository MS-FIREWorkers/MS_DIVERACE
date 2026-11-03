/*
    Player_Animation
    20261008  hanaue sho
 */
using UnityEngine;

public class Player_Animation : MonoBehaviour
{
    // --------------------------------------------------
    // ----- PlayerCharacterBody -----
    // --------------------------------------------------
    [Header("PlayerCharacter Body")]
    [SerializeField] private GameObject _body;


    // --------------------------------------------------
    // ----- Unity Event -----
    // --------------------------------------------------
    private void Start()
    {
        if (_body == null)
        {
            Debug.LogError("[Error] PlayerCharacterBody Not Found!");
        }
    }


    // --------------------------------------------------
    // ----- Public Event -----
    // --------------------------------------------------
    public void RotateTo(float input)
    {
        input = Mathf.Clamp01(input);

        float rotationX = Mathf.Lerp(90.0f, 180.0f, input);

        Vector3 euler = new Vector3 (rotationX, 0.0f, 0.0f);
        _body.transform.localRotation = Quaternion.Euler(euler);
    }


}
