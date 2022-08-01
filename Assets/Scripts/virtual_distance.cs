using System.Collections;
using System;
using UnityEngine;
using System.Text;
using System.IO;

public class virtual_distance : MonoBehaviour
{
    private float distance;
    public float fre;
    // public int SegmentLength;
    public bool Recording = false;
    public string motionname;
    public Vector3 angle;
    private Vector3 ang;
    float time = 0;
    private string path;
    private string SensorName;
    private string SensorType;
    private string SensorDatabase;
    private string filename; 
    
    private bool ClearFiles = false;

    void Start()
    {
        //***define the save path***//
        SensorName = transform.name;
        SensorType = transform.parent.name;
        SensorDatabase = @"Assets/VirtualData";

        //Initializing and clear the files

        string Database = Path.Combine(SensorDatabase, motionname);
        Debug.Log("Initalizing Database is:"+Database);
        Database = Path.Combine(Database, SensorType);
        if (Directory.Exists(Database))
            {
                //Directory.CreateDirectory(Database);
                if (ClearFiles == false)
                {
                    var files = Directory.GetFiles(Database); 
                    for (int index = 0; index < files.Length; ++index)
                        {
                            //Debug.Log("file is"+files[index]);
                            File.Delete(files[index]);
                        }   
                }
            }

    }


    // Update is called once per frame
    void Update()
    {
        //Quaternion com_quater = com.rotation;
        ang = new Vector3(angle.x, angle.y, angle.z);
        //com_quater.eulerAngles = ang + com_quater.eulerAngles;
        //transform.rotation = com_quater;
        RaycastHit hit;
        Vector3 fwd = transform.TransformDirection(Vector3.forward);
        Vector3 bck = transform.TransformDirection(Vector3.back);
        Vector3 left = transform.TransformDirection(Vector3.left);
        Vector3 right = transform.TransformDirection(Vector3.right);
        Vector3 up = transform.TransformDirection(Vector3.up);
        Vector3 down = transform.TransformDirection(Vector3.down);
        if (Physics.Raycast(transform.position, fwd, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.red);
        if (Physics.Raycast(transform.position, bck, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.red);
        if (Physics.Raycast(transform.position, left, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.gray);
        if (Physics.Raycast(transform.position, right, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.red);
        if (Physics.Raycast(transform.position, up, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.white);
        if (Physics.Raycast(transform.position, down, out hit, 20))
           Debug.DrawLine(transform.position, hit.point, Color.yellow);
        if (Physics.Raycast(transform.position, down, out hit, 30))
        {
            Debug.DrawLine(transform.position, hit.point, Color.red);
            //distance = Mathf.Abs(transform.position.y - hit.point.y);
            distance = hit.distance;
            //Debug.Log(distance);
            if (distance > 4.4) //if the detected distance over the highest range of acutal sensor
            {
                distance = 4.4f; // make it equal to the highest range
            }
            if (distance < 0.5) //if the detected distance over the lowest range of acutal sensor
                distance = 0.5f; // make it equal to the lowest range
        }
        else
        {
            distance = 4.4f;
        }
        //Debug.Log("distance is" + distance);
        if (Recording == true)
        {
            
            filename =  SensorName + motionname + ".csv";
            
            string store_path = Path.Combine(SensorDatabase, motionname);
            store_path = Path.Combine(store_path, SensorType);
            if (!Directory.Exists(store_path))
            {
                Directory.CreateDirectory(store_path);
            }
            path = Path.Combine(store_path, filename);
            recording(path);  
        }
    
    }

    private void recording(string path)
    {

        if (!File.Exists(path))
            File.Create(path).Close();

        StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);

        time = time + Time.deltaTime;

        sw.Write(Time.frameCount + ",");
        sw.Write(time + ",");


        sw.Write(distance + ",");
        sw.Write("\r\n");

        sw.Flush();
        sw.Close();

    }

}

