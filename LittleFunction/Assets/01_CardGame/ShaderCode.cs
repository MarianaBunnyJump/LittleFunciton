#region

using UnityEngine;
using UnityEngine.UI;

#endregion

public class ShaderCode : MonoBehaviour
{
    private Image image;
    private Material m;
    private CardVisual visual;

    // Start is called before the first frame update
    private void Start()
    {
        image = GetComponent<Image>();
        m = new Material(image.material);
        image.material = m;
        visual = GetComponentInParent<CardVisual>();

        var editions = new string[4];
        editions[0] = "REGULAR";
        editions[1] = "POLYCHROME";
        editions[2] = "REGULAR";
        editions[3] = "NEGATIVE";

        for (var i = 0; i < image.material.enabledKeywords.Length; i++)
        {
            image.material.DisableKeyword(image.material.enabledKeywords[i]);
        }

        image.material.EnableKeyword("_EDITION_" + editions[Random.Range(0, editions.Length)]);
    }

    // Update is called once per frame
    private void Update()
    {
        // Get the current rotation as a quaternion
        var currentRotation = transform.parent.localRotation;

        // Convert the quaternion to Euler angles
        var eulerAngles = currentRotation.eulerAngles;

        // Get the X-axis angle
        var xAngle = eulerAngles.x;
        var yAngle = eulerAngles.y;

        // Ensure the X-axis angle stays within the range of -90 to 90 degrees
        xAngle = ClampAngle(xAngle, -90f, 90f);
        yAngle = ClampAngle(yAngle, -90f, 90);


        m.SetVector("_Rotation", new Vector2(xAngle.MyRemap(-20, 20, -.5f, .5f), yAngle.MyRemap(-20, 20, -.5f, .5f)));
    }

    // Method to clamp an angle between a minimum and maximum value
    private float ClampAngle(float angle, float min, float max)
    {
        if (angle < -180f)
            angle += 360f;
        if (angle > 180f)
            angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
}