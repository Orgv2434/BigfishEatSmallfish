using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject target;//目标物体
    Vector3 offset;//相机跟随的偏移量
    void Start()
    {
        //保证摄像机看向目标物体，且z轴旋转度是0
        transform.LookAt(target.transform.position);
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);
        //得到摄像机与物体之间的初始偏移量
        offset = target.transform.position - transform.position;
    }

    void LateUpdate()
    {
        Rotate();
        Rollup();
        Follow();
    }

    //摄像机跟随、滚轮缩放功能:

    public float zoomSpeed = 1f; // 视野的缩放速度
    float zoom;//滚轮滚动量
    void Follow()
    {
        //视野缩放
        zoom = Input.GetAxis("Mouse ScrollWheel") * zoomSpeed; // 获取滚轮滚动量
        if (zoom != 0) // 如果有滚动
        {
            offset -= zoom * offset;
        }
        //镜头跟随
        transform.position = target.transform.position - offset;
    }

    //左右旋转、上下旋转功能:

    public float rotationSpeed = 500f;//摄像机旋转速度
    private bool isRotating, lookup = false;
    float mousex, mousey;
    void Rotate()
    {
        if (Input.GetMouseButtonDown(1))//长按鼠标右键
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }
        if (isRotating)
        {
            //得到鼠标x方向移动距离
            mousex = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            //旋转轴的位置是目标物体处，方向是世界坐标系的y轴
            transform.RotateAround(target.transform.position, Vector3.up, mousex);
            //每次旋转后更新偏移量
            offset = target.transform.position - transform.position;
        }
    }
    void Rollup()
    {
        if (Input.GetMouseButtonDown(2))//长按鼠标中键
        {
            lookup = true;
        }
        if (Input.GetMouseButtonUp(2))
        {
            lookup = false;
        }
        if (lookup)
        {
            //得到鼠标y方向移动距离
            mousey = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
            //旋转轴的位置在目标物体处，方向是摄像机的x轴
            transform.RotateAround(target.transform.position, transform.right, mousey);
            //每次旋转后更新偏移量
            offset = target.transform.position - transform.position;
        }

    }
}

