using UnityEngine;

public static class FixCamera
{
    public static string Run()
    {
        var cam = Camera.main;
        if (!cam) return "no camera";
        cam.transform.position = new Vector3(0f, 1.6f, -180f);
        cam.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 5.4f, 0f) - cam.transform.position, Vector3.up);
        return string.Format("pos={0} euler={1}", cam.transform.position.ToString("F1"), cam.transform.eulerAngles.ToString("F2"));
    }
}
