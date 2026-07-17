using UnityEngine;
using UnityEngine.UI;

public class LeafCollect : MonoBehaviour
{
    public Image propsImage;
    public Sprite leafSprite;


    private void OnMouseDown()
    {
        Debug.Log("Leaf Clicked");


        if (propsImage != null)
        {
            propsImage.sprite = leafSprite;
            propsImage.color = Color.white;
        }


        gameObject.SetActive(false);
    }
}