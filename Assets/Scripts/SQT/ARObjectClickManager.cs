using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ARObjectClickManager : MonoBehaviour
{
    private Camera arCamera;

    public Image propsImage;
    public Sprite leafSprite;


    void Start()
    {
        arCamera = Camera.main;

        if (arCamera == null)
        {
            Debug.LogError("❌ AR Camera not found");
        }
        else
        {
            Debug.Log("✅ Camera found: " + arCamera.name);
        }
    }


    void Update()
    {
        // PC测试
        if (Mouse.current != null &&
           Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("🖱 Mouse Click");
            CheckClick(Mouse.current.position.ReadValue());
        }


        // 手机测试
        if (Touchscreen.current != null &&
           Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            Debug.Log("📱 Touch Click");

            CheckClick(
                Touchscreen.current.primaryTouch.position.ReadValue()
            );
        }
    }



    void CheckClick(Vector2 position)
    {
        Debug.Log("Screen Position: " + position);


        Ray ray = arCamera.ScreenPointToRay(position);

        Debug.DrawRay(
            ray.origin,
            ray.direction * 100,
            Color.red,
            2f
        );


        RaycastHit hit;


        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log(
                "✅ Ray Hit: " + hit.collider.gameObject.name
            );


            Debug.Log(
                "Tag: " + hit.collider.gameObject.tag
            );


            if (hit.collider.CompareTag("Leaf"))
            {
                Debug.Log("🍃 Leaf detected");

                CollectLeaf(hit.collider.gameObject);
            }
            else
            {
                Debug.Log("❌ Not Leaf");
            }

        }
        else
        {
            Debug.Log("❌ Ray hit nothing");
        }
    }



    void CollectLeaf(GameObject leaf)
    {
        Debug.Log("🎉 Leaf Collected");


        if (propsImage != null)
        {
            propsImage.sprite = leafSprite;
            propsImage.color = Color.white;

            Debug.Log("✅ PropsBar Updated");
        }
        else
        {
            Debug.Log("❌ PropsImage missing");
        }


        leaf.SetActive(false);

        Debug.Log("✅ Leaf Disabled");
    }
}