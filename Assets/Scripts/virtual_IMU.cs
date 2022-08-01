using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System.IO;
using System;
using System.Linq;
/// <summary>
/// Author: Nick Xia
/// Time: 2021/11
/// The script to output the position, euler angel, quaternion, accceleration (filtered), angular velocity (filterd) of 'virtual IMU' 
/// The virtual IMU need to be indicated and is supposed to be attached to the targeted body segment
/// (The path of stored .csv file need to be modified.) 
/// [step]:
/// 1. add virtual IMU model as the child-node under the desired body segment
/// 2. attach this script into the virtual sensor
/// 3. configure the related varible such as the name, whether recording, whether use calibration 
/// (if compared data is under the sensor frame, it should be calibrate)
/// 4. There are two-ways calibration methon
///    a. Calibrated to the initial sensor local frame -- this is used to apply the sensor rotation data 
///    b. Calibrated to real time sensor local frame -- this is used to apply the sensor acceleration data
/// </summary>
public class virtual_IMU : MonoBehaviour
{

    
    public float fre;
    public int SegmentLength;
    public bool Recording = false;
    public bool Calibration = true;
    public bool Recording_Position;
    public bool Recording_Acceleration;
    public bool Recording_Quaternion;
    public bool Recording_Euler;
    // public bool Recording_AngularVelocity;
    public string motionname;

    private Vector3 RefVec;
    private Vector3 sensor_rotation;
    private Vector3 sensor_position;
    private Vector3 filterd_vel;
    private Vector3 filterd_acc;
    private Matrix4x4 T_ini_mat;
    private Transform empty; 

    private bool ClearFiles = false;
    //variables for ACCELEROMATOR
    private Vector3 LastPos;
    private Vector3 LastSpeed;
    private Vector3 SensorSpeed;
    private Vector3 SensorAcc;
    private Vector3 Local_SensorAcc;
    private Vector3 DeltaMovement;
    //variables for GYROSCOPE
    private Vector3 LastAng;
    private Vector3 SensorGyro;

    private float ratio = 0.9f;
    float time = 0;
    private string path;
    private string SensorName;
    private string SensorType;
    private string SensorDatabase;
    private string filename; 

    
    int filter_length = 15;
    float[] pos_filter_x = new float[15]; 
    float[] pos_filter_y = new float[15]; 
    float[] pos_filter_z = new float[15];

    float[] rot_filter_x = new float[15]; 
    float[] rot_filter_y = new float[15]; 
    float[] rot_filter_z = new float[15]; 


    void Awake()
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

        //***record the initial altitude of virtual sensor***//
        RefVec = transform.eulerAngles; //obtain the initial euler angle
        //var matrix =  transform.localToWorldMatrix;
        Matrix4x4 ini_mat = Matrix4x4.Rotate(transform.rotation); //transform it into a 4x4 matirces 
        //Debug.Log("from matrices is" + ini_mat);
        T_ini_mat = ini_mat.transpose; //take the inverse
        //Debug.Log("take the inverse is" + T_ini_mat);

        //***clear up the filter buffer***//
        for (int i = 0; i < filter_length; i++)
        {
            pos_filter_x[i] = 0;
            pos_filter_y[i] = 0;
            pos_filter_z[i] = 0;
            rot_filter_x[i] = 0;
            rot_filter_y[i] = 0;
            rot_filter_z[i] = 0;
        }

        
    }
    // Update is called once per frame, the interval between two frames is related to the Fixed delta time
    void FixedUpdate()
    {
        if (Calibration == true)
        {

            Matrix4x4 current_mat = Matrix4x4.Rotate(transform.rotation); //obtain the current rotation matrice
            
            //Debug.Log("current transform's rotation is:" + SensorName + transform.rotation);
            // //***calibrate the data ***//
            //Calibration method 1:calibrate to the sensor local coordinate at the firtst frame//
            var after = new Matrix4x4();

            after.m00 = T_ini_mat.m00 * current_mat.m00 + T_ini_mat.m01 * current_mat.m10 + T_ini_mat.m02 * current_mat.m20;
            after.m01 = T_ini_mat.m00 * current_mat.m01 + T_ini_mat.m01 * current_mat.m11 + T_ini_mat.m02 * current_mat.m21;
            after.m02 = T_ini_mat.m00 * current_mat.m02 + T_ini_mat.m01 * current_mat.m12 + T_ini_mat.m02 * current_mat.m22;

            after.m10 = T_ini_mat.m10 * current_mat.m00 + T_ini_mat.m11 * current_mat.m10 + T_ini_mat.m12 * current_mat.m20;
            after.m11 = T_ini_mat.m10 * current_mat.m01 + T_ini_mat.m11 * current_mat.m11 + T_ini_mat.m12 * current_mat.m21;
            after.m12 = T_ini_mat.m10 * current_mat.m02 + T_ini_mat.m11 * current_mat.m12 + T_ini_mat.m12 * current_mat.m22;

            after.m20 = T_ini_mat.m20 * current_mat.m00 + T_ini_mat.m21 * current_mat.m10 + T_ini_mat.m22 * current_mat.m20;
            after.m21 = T_ini_mat.m20 * current_mat.m01 + T_ini_mat.m21 * current_mat.m11 + T_ini_mat.m22 * current_mat.m21;
            after.m22 = T_ini_mat.m20 * current_mat.m02 + T_ini_mat.m21 * current_mat.m12 + T_ini_mat.m22 * current_mat.m22;

            Vector3 forward;
            forward.x = after.m02;
            forward.y = after.m12;
            forward.z = after.m22;

            Vector3 upwards;
            upwards.x = after.m01;
            upwards.y = after.m11;
            upwards.z = after.m21;

            empty.rotation = Quaternion.LookRotation(forward, upwards);
            sensor_rotation = empty.eulerAngles;

            sensor_position.x = -1 * (T_ini_mat.m00 * transform.position.x + T_ini_mat.m01 * transform.position.y + T_ini_mat.m02 * transform.position.z);
            sensor_position.y = T_ini_mat.m10 * transform.position.x + T_ini_mat.m11 * transform.position.y + T_ini_mat.m12 * transform.position.z;
            sensor_position.z = T_ini_mat.m20 * transform.position.x + T_ini_mat.m21 * transform.position.y + T_ini_mat.m22 * transform.position.z;


            //***calibrate the position data***//
            //Calibration method 2:calibrate to the sensor local coordinate at each time//
            Matrix4x4 T_current_mat = current_mat.transpose;

            // Quaternion global = transform.rotation;
            // Quaternion con_global = Quaternion.Inverse(transform.rotation);
            // sensor_position = con_global *  transform.position;
            // sensor_position =  global * sensor_position;
           
            //Debug.Log(transform.rotation);
            // sensor_position.x = -1 * (T_current_mat.m00 * transform.position.x + T_current_mat.m10 * transform.position.y + T_current_mat.m20 * transform.position.z);
            // sensor_position.y = T_current_mat.m01 * transform.position.x + T_current_mat.m11 * transform.position.y + T_current_mat.m21 * transform.position.z;
            // sensor_position.z = T_current_mat.m02 * transform.position.x + T_current_mat.m12 * transform.position.y + T_current_mat.m22 * transform.position.z;
            
            // sensor_position.x = -1 * (T_current_mat.m00 * transform.position.x + T_current_mat.m01 * transform.position.y + T_current_mat.m02 * transform.position.z);
            // sensor_position.y = T_current_mat.m10 * transform.position.x + T_current_mat.m11 * transform.position.y + T_current_mat.m12 * transform.position.z;
            // sensor_position.z = T_current_mat.m20 * transform.position.x + T_current_mat.m21 * transform.position.y + T_current_mat.m22 * transform.position.z;
            
            // sensor_rotation.x = -1 * (T_current_mat.m00 * transform.rotation.x + T_current_mat.m10 * transform.rotation.y + T_current_mat.m20 * transform.rotation.z);
            // sensor_rotation.y = T_current_mat.m01 * transform.rotation.x + T_current_mat.m11 * transform.rotation.y + T_current_mat.m21 * transform.rotation.z;
            // sensor_rotation.z = T_current_mat.m02 * transform.rotation.x + T_current_mat.m12 * transform.rotation.y + T_current_mat.m22 * transform.rotation.z;
        }
        else
        {
            sensor_rotation = transform.eulerAngles;
            sensor_position = transform.position;
        }

        //***Filtering the position data***//
        for (int i = 0; i < filter_length - 1; i++)
        {
            pos_filter_x[i] = pos_filter_x[i + 1];
            rot_filter_x[i] = rot_filter_x[i + 1];
        }
        pos_filter_x[filter_length - 1] = sensor_position.x;
        rot_filter_x[filter_length - 1] = sensor_rotation.x;
        for (int i = 0; i < filter_length - 1; i++)
        {
            pos_filter_y[i] = pos_filter_y[i + 1];
            rot_filter_y[i] = rot_filter_y[i + 1];
        }
        pos_filter_y[filter_length - 1] = sensor_position.y;
        rot_filter_y[filter_length - 1] = sensor_rotation.y;
        for (int i = 0; i < filter_length - 1; i++)
        {
            pos_filter_z[i] = pos_filter_z[i + 1];
            rot_filter_z[i] = rot_filter_z[i + 1];
        }
        pos_filter_z[filter_length - 1] = sensor_position.z;
        rot_filter_z[filter_length - 1] = sensor_rotation.z;

        float pos_sum_x = 0, rot_sum_x = 0;
        float pos_sum_y = 0, rot_sum_y = 0;
        float pos_sum_z = 0, rot_sum_z = 0;
        for (int i = 0; i < filter_length; i++)
        {
            pos_sum_x = pos_sum_x + pos_filter_x[i];
            pos_sum_y = pos_sum_y + pos_filter_y[i];
            pos_sum_z = pos_sum_z + pos_filter_z[i];

            rot_sum_x = rot_sum_x + rot_filter_x[i];
            rot_sum_y = rot_sum_y + rot_filter_y[i];
            rot_sum_z = rot_sum_z + rot_filter_z[i];
        }
        sensor_position.x = pos_sum_x / filter_length;
        sensor_position.y = pos_sum_y / filter_length;
        sensor_position.z = pos_sum_z / filter_length;

        sensor_rotation.x = rot_sum_x / filter_length;
        sensor_rotation.y = rot_sum_y / filter_length;
        sensor_rotation.z = rot_sum_z / filter_length;

        //***Calculate the angular velocity (via incremental method)***//
        SensorGyro.x = (sensor_rotation.x - LastAng.x) / (1 / fre);
        SensorGyro.y = (sensor_rotation.y - LastAng.y) / (1 / fre);
        SensorGyro.z = (sensor_rotation.z - LastAng.z) / (1 / fre);

        //***Calculate the acceleration (via incremental method)***//
        DeltaMovement.x = sensor_position.x - LastPos.x;
        DeltaMovement.y = sensor_position.y - LastPos.y;
        DeltaMovement.z = sensor_position.z - LastPos.z;
        //***Calculate three-dimensional speed***// 0.9 is the body heigh mapping factor
        SensorSpeed.x = ratio * DeltaMovement.x / (1/fre); // the 'transform.posiiton presents the global position'
        SensorSpeed.y = ratio * DeltaMovement.y / (1/fre);
        SensorSpeed.z = ratio * DeltaMovement.z / (1/fre);
        //***Calculate three-dimensioanl Acceleration***//
        SensorAcc.x = (SensorSpeed.x - LastSpeed.x) / (1 / fre);
        SensorAcc.y = (SensorSpeed.y - LastSpeed.y) / (1 / fre);
        SensorAcc.z = (SensorSpeed.z - LastSpeed.z) / (1 / fre);

        LastAng = sensor_rotation;
        LastPos = sensor_position;
        LastSpeed = SensorSpeed;
        // Debug.Log(SensorAcc);

       

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

    float Variance(float[] source)
    {
        int n = 0;
        float mean = 0;
        float M2 = 0;

        foreach (float x in source)
        {
            n = n + 1;
            float delta = x - mean;
            mean = mean + delta / n;
            M2 += delta * (x - mean);
        }
        return M2 / (n - 1);
    }

    private void recording(string path)
    {

        if (!File.Exists(path))
            File.Create(path).Close();

        StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);

        time = time + Time.deltaTime;

        sw.Write(Time.frameCount + ",");
        sw.Write(time + ",");

        if (Recording_Acceleration == true)
        {
            sw.Write(SensorAcc.x + ",");
            sw.Write(SensorAcc.y + ",");
            sw.Write(SensorAcc.z + ",");
        }

        // if (Recording_AngularVelocity == true)
        // {
        //     sw.Write(SensorGyro.x + ",");
        //     sw.Write(SensorGyro.y + ",");
        //     sw.Write(SensorGyro.z + ",");

        // }

        if (Recording_Position == true)
        {
            //Debug.Log(SensorName + sensor_position.x);
            sw.Write(sensor_position.x + ",");
            sw.Write(sensor_position.y + ",");
            sw.Write(sensor_position.z + ",");
        }

        if (Recording_Quaternion == true)
        {

            sw.Write(transform.rotation.w + ",");
            sw.Write(transform.rotation.x + ",");
            sw.Write(transform.rotation.y + ",");
            sw.Write(transform.rotation.z + ",");
        }
        if (Recording_Euler == true) //doing calibration
        {
            sw.Write(sensor_rotation.x + ",");
            sw.Write(sensor_rotation.y + ",");
            sw.Write(sensor_rotation.z + ",");
        }
        sw.Write("\r\n");

        sw.Flush();
        sw.Close();
    }

}