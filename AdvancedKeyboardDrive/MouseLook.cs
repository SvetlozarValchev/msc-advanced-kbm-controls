using UnityEngine;
using System.Collections;
using HutongGames.PlayMaker;

namespace AdvancedKBMControls
{
    public class MouseLook : MonoBehaviour
    {
        public float sensitivityX = 15f;
        public float sensitivityY = 15f;
        public float minimumX = -360f;
        public float maximumX = 360f;
        public float minimumY = -60f;
        public float maximumY = 60f;
        public MouseLook.RotationAxes axes;
        public float rotationY;

        private bool f_isActive = true;

        private void Update()
        {
            if (FsmVariables.GlobalVariables.FindFsmBool("PlayerInMenu").Value || !f_isActive)
            {
                return;
            }

            if (this.axes == MouseLook.RotationAxes.MouseXAndY)
            {
                float y = this.transform.localEulerAngles.y + Input.GetAxis("Mouse X") * this.sensitivityX;
                this.rotationY += Input.GetAxis("Mouse Y") * this.sensitivityY;
                this.rotationY = Mathf.Clamp(this.rotationY, this.minimumY, this.maximumY);
                this.transform.localEulerAngles = new Vector3(-this.rotationY, y, 0.0f);
            }
            else if (this.axes == MouseLook.RotationAxes.MouseX)
            {
                this.transform.Rotate(0.0f, Input.GetAxis("Mouse X") * this.sensitivityX, 0.0f);
            }
            else
            {
                this.rotationY += Input.GetAxis("Mouse Y") * this.sensitivityY;
                this.rotationY = Mathf.Clamp(this.rotationY, this.minimumY, this.maximumY);
                this.transform.localEulerAngles = new Vector3(-this.rotationY, this.transform.localEulerAngles.y, 0.0f);
            }
        }

        private void Start()
        {
            if (!(bool)((Object)this.GetComponent<Rigidbody>()))
                return;
            this.GetComponent<Rigidbody>().freezeRotation = true;
        }

        public void SetState(bool state)
        {
            if (state)
            {
                f_isActive = true;
            } else
            {
                f_isActive = false;
            }
        }

        public void UpdateAndDisable()
        {
            f_isActive = false;

            if (this.axes == MouseLook.RotationAxes.MouseY)
            {
                this.rotationY += Input.GetAxis("Mouse Y") * this.sensitivityY;
                this.rotationY = Mathf.Clamp(this.rotationY, this.minimumY, this.maximumY);
                this.transform.localEulerAngles = new Vector3(-this.rotationY, this.transform.localEulerAngles.y, 0.0f);
            }
        }

        public enum RotationAxes
        {
            MouseXAndY,
            MouseX,
            MouseY,
        }
    }
}