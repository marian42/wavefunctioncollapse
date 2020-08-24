using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class TakeScreenshot : MonoBehaviour {
    private Camera screenshotCamera;

    public KeyCode Key;

    public int Counter;

    public string Filename = "Screenshots/Screenshot_{0}.jpg";
    
    public bool CaptureOnClick = false;

    void Update() {
        if (Input.GetKeyDown(this.Key)) {
            this.Capture();
        }

        if (this.CaptureOnClick && Input.GetMouseButtonDown(0)) {
            this.Capture();
        }
    }

    private string getFilename() {
        return this.Filename.Replace("{0}", this.Counter.ToString("D4"));
    }

    public void Capture() {
        if (this.screenshotCamera == null) {
            this.screenshotCamera = this.GetComponent<Camera>();
        }

        while (System.IO.File.Exists(this.getFilename())) {
            this.Counter++;
        }
		var filename = this.getFilename();
		System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
		ScreenCapture.CaptureScreenshot(filename);
        Debug.Log("Saved screenshot: " + filename);
        this.Counter++;
    }
}
