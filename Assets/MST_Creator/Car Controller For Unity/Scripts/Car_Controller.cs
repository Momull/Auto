﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class Car_Controller : MonoBehaviour
{
    private PhotonView m_PhotonView;

    //Public Variables
    [Header("Wheel Colliders")]
    public WheelCollider FL;
    public WheelCollider FR;
    public WheelCollider BL;
    public WheelCollider BR;

    [Header("Wheel Transforms")]
    public Transform Fl;
    public Transform Fr;
    public Transform Bl;
    public Transform Br;

    [Header("Wheel Transforms Rotations")]
    public Vector3 FL_Rotation;
    public Vector3 FR_Rotation;
    public Vector3 BL_Rotation;
    public Vector3 BR_Rotation;

    [Header("Car Settings")]
    public float Motor_Torque = 100f;
    public float Max_Steer_Angle = 20f;
    public float  BrakeForce = 150f;

    [Space(3)]

    //These are the speeds for each gear
    //The Brake and Reverse gears appear automatically so don't worry about those
    //The Speeds MUST be in kph
    public List<int> Gears_Speed;

    [Space(3)]

    public float handBrakeFrictionMultiplier = 2;
    private float handBrakeFriction  = 0.05f;
    public float tempo;

    [Header("Boost Settings")]
    public float Boost_Motor_Torque = 300f;
    public float Motor_Torque_Normal = 100f;

    [Header("Audio Settings (Beta)")]
    public bool Enable_Audio;
    public AudioSource Engine_Sound;
    public float Max_Engine_Audio_Pitch;
    public float Min_Engine_Audio_Pitch;
    public float Min_Volume;
    public float Max_Volume;

    [Header("Drift Settings")]
    public bool Set_Drift_Settings_Automatically = true;
    public float Forward_Extremium_Value_When_Drifting;
    public float Sideways_Extremium_Value_When_Drifting;

    [Header("Light Setting(s)")]

    [Header("Lights (With Light Settings)")]
    public bool Enable_Headlights_Lights;
    public bool Enable_Brakelights_Lights;
    public bool Enable_Reverselights_Lights;

    public Light[] HeadLights;
    public Light[] BrakeLights;
    public Light[] ReverseLights;

    [Space(4)]

    [Header("Light (With MeshRenderer")]
    public bool Enable_Headlights_MeshRenderers;
    public bool Enable_Brakelights_MeshRenderers;
    public bool Enable_Reverselights_MeshRenderers;

    public MeshRenderer[] HeadLights_MeshRenderers;
    public MeshRenderer[] BrakeLights_MeshRenderers;
    public MeshRenderer[] ReverseLights_MeshRenderers;

    [Header("Particle System(s) Settings")]
    public bool Use_Particle_Systems;
    public ParticleSystem[] Car_Smoke_From_Silencer;//Sorry, couldn't think of a better name :P

    [Header("UI Settings")]
    public bool Use_TMP;
    public bool Use_Default_UI;

    [Space(3)]

    public bool Show_Speed_In_KPH;
    public Text Speed_Text_UI;
    public TextMeshProUGUI Speed_Text_TMPPro;

    [Space(3)]

    public Text Gear_Text;
    public TextMeshProUGUI Gear_TMPro;

    [Header("Other Settings")]
    public Transform Center_of_Mass;
    public  float frictionMultiplier = 3f;
    public Rigidbody Car_Rigidbody;

    [Header("Debug")] //These are variables that are read only so dont chnage them, they are only there if u wanna use them for UI like speed or RPM;
    public float RPM_FL;
    public float RPM_FR;
    public float RPM_BL;
    public float RPM_BR;

    [Space(8)]

    public float Car_Speed_KPH;
    public float Car_Speed_MPH;

    [Space(4)]

    public string Current_Gear;
    public int Current_Gear_num;

    //private Variables
    private Rigidbody rb;
    private float Brakes = 0f;
    private WheelFrictionCurve  FLforwardFriction, FLsidewaysFriction;
    private WheelFrictionCurve  FRforwardFriction, FRsidewaysFriction;
    private WheelFrictionCurve  BLforwardFriction, BLsidewaysFriction;
    private WheelFrictionCurve  BRforwardFriction, BRsidewaysFriction;

    //Debug Values in Int Form
    int Car_Speed_In_KPH;
    int Car_Speed_In_MPH;

    //Private Audio Variables
    private float Forward_volume;
    private float Reverse_volume;
    private float Reverse_pitch;
    private float Forward_pitch;

    //Hidden Variables
    [HideInInspector]public float currSpeed;

    void Start(){
        m_PhotonView = GetComponent<PhotonView>();

        if(!m_PhotonView.IsMine)
        {
            Destroy(rb);
        }

        SetupCar();
    }

    private void SetupCar()
    {
        //To Prevent The Car From Toppling When Turning Too Much
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = Center_of_Mass.localPosition;

        //Play Car Particle System
        if (Use_Particle_Systems)
        {
            foreach (ParticleSystem P in Car_Smoke_From_Silencer)
            {
                P.Play();
            }
        }

        //Set the current gear to 0
        Current_Gear = "0";
        Current_Gear_num = 0;

        //Here we just set the lights to turn on and off at play.

        //We turn the headlights on here
        if (Enable_Headlights_Lights)
        {
            foreach (Light H in HeadLights)
            {
                H.enabled = true;
            }
        }

        if (Enable_Headlights_MeshRenderers)
        {
            foreach (MeshRenderer HM in HeadLights_MeshRenderers)
            {
                HM.enabled = true;
            }
        }

        //Here we turn the reverse light(s) off
        if (Enable_Reverselights_Lights)
        {
            foreach (Light R in ReverseLights)
            {
                R.enabled = false;
            }
        }

        if (Enable_Reverselights_MeshRenderers)
        {
            foreach (MeshRenderer RM in ReverseLights_MeshRenderers)
            {
                RM.enabled = false;
            }
        }

        //Here we turn off the brakelights
        if (Enable_Brakelights_Lights)
        {
            foreach (Light B in BrakeLights)
            {
                B.enabled = false;
            }
        }

        if (Enable_Brakelights_MeshRenderers)
        {
            foreach (MeshRenderer BM in BrakeLights_MeshRenderers)
            {
                BM.enabled = true;
            }
        }
    }

    public void FixedUpdate(){

        if(!m_PhotonView.IsMine)
        {
            return;
        }

        //Changing Gears

        if (Gears_Speed[Current_Gear_num] < Car_Speed_KPH && Current_Gear_num != Gears_Speed.Count){
            Current_Gear_num++;
            Current_Gear = (Current_Gear_num + 1).ToString();
        }

        if(Gears_Speed[Current_Gear_num] > Car_Speed_KPH && Current_Gear_num != 0){
            Current_Gear_num--;
            Current_Gear = (Current_Gear_num + 1).ToString();
        }

        if(Car_Speed_In_KPH == 0){
            Current_Gear = "0";
        }

        //Setting the gear text to the current gear
        if(Use_TMP){
            Gear_TMPro.SetText(Current_Gear);
        }

        if(Use_Default_UI){
            Gear_Text.text = Current_Gear;
        }

        //Making The Car Move Forward or Backward
        BL.motorTorque = Input.GetAxis("Vertical") * Motor_Torque;
        BR.motorTorque = Input.GetAxis("Vertical") * Motor_Torque;

        //Making The Car Turn
        FL.steerAngle = Input.GetAxis("Horizontal") * Max_Steer_Angle;
        FR.steerAngle = Input.GetAxis("Horizontal") * Max_Steer_Angle;

        //Showing the RPM for the wheels
        RPM_FL = FL.rpm;
        RPM_BL = BL.rpm;
        RPM_FR = FR.rpm;
        RPM_BR = BR.rpm;

        //Changing speed of the car
        Car_Speed_KPH = Car_Rigidbody.velocity.magnitude * 3.6f;
        Car_Speed_MPH = Car_Rigidbody.velocity.magnitude * 2.237f;

        Car_Speed_In_KPH = (int) Car_Speed_KPH;
        Car_Speed_In_MPH = (int) Car_Speed_MPH;

        //Showing Car Speed
        if(Use_Default_UI){
            if(Show_Speed_In_KPH){
                Speed_Text_UI.text = Car_Speed_In_KPH.ToString();
            }

            if(!Show_Speed_In_KPH){
                Speed_Text_UI.text = Car_Speed_In_MPH.ToString();
            }
        }

        if(Use_TMP){
            if(Show_Speed_In_KPH){
                Speed_Text_TMPPro.SetText(Car_Speed_In_KPH.ToString());
            }

            if(!Show_Speed_In_KPH){
                Speed_Text_TMPPro.SetText(Car_Speed_In_MPH.ToString());
            }
        }

        //Make Car Boost
        if(Input.GetKey(KeyCode.LeftShift)){
            //Setting The Motor Torque To The Boost Torque
            Motor_Torque = Boost_Motor_Torque;
        }

        else{
            //Setting The Motor Torque Back To Normal;
            Motor_Torque = Motor_Torque_Normal;
        }

        //Make Car Drift
        WheelHit wheelHit1;
        WheelHit wheelHit2;
        WheelHit wheelHit3;
        WheelHit wheelHit4;

        FL.GetGroundHit(out wheelHit1);
        FR.GetGroundHit(out wheelHit2);
        BL.GetGroundHit(out wheelHit3);
        BR.GetGroundHit(out wheelHit4);

        if(wheelHit1.sidewaysSlip < 0 )	
            tempo = (1 + -Input.GetAxis("Horizontal")) * Mathf.Abs(wheelHit1.sidewaysSlip *handBrakeFrictionMultiplier);

            if(tempo < 0.5) tempo = 0.5f;

        if(wheelHit1.sidewaysSlip > 0 )	
            tempo = (1 + Input.GetAxis("Horizontal") )* Mathf.Abs(wheelHit1.sidewaysSlip *handBrakeFrictionMultiplier);

            if(tempo < 0.5) tempo = 0.5f;

        if(wheelHit1.sidewaysSlip > .99f || wheelHit1.sidewaysSlip < -.99f){
            //handBrakeFriction = tempo * 3;
            float velocity = 0;
            handBrakeFriction = Mathf.SmoothDamp(handBrakeFriction,tempo* 3,ref velocity ,0.1f * Time.deltaTime);
            }

        if(wheelHit2.sidewaysSlip < 0 )	
            tempo = (1 + -Input.GetAxis("Horizontal")) * Mathf.Abs(wheelHit2.sidewaysSlip *handBrakeFrictionMultiplier);

            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit2.sidewaysSlip > 0 )	
            tempo = (1 + Input.GetAxis("Horizontal") )* Mathf.Abs(wheelHit2.sidewaysSlip *handBrakeFrictionMultiplier);
        
            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit2.sidewaysSlip > .99f || wheelHit2.sidewaysSlip < -.99f){
            //handBrakeFriction = tempo * 3;
            float velocity = 0;
            handBrakeFriction = Mathf.SmoothDamp(handBrakeFriction,tempo* 3,ref velocity ,0.1f * Time.deltaTime);
            }

        if(wheelHit3.sidewaysSlip < 0 )	
            tempo = (1 + -Input.GetAxis("Horizontal")) * Mathf.Abs(wheelHit3.sidewaysSlip *handBrakeFrictionMultiplier) ;
        
            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit3.sidewaysSlip > 0 )	
            tempo = (1 + Input.GetAxis("Horizontal") )* Mathf.Abs(wheelHit3.sidewaysSlip *handBrakeFrictionMultiplier);
        
            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit3.sidewaysSlip > .99f || wheelHit3.sidewaysSlip < -.99f){
            //handBrakeFriction = tempo * 3;
            float velocity = 0;
            handBrakeFriction = Mathf.SmoothDamp(handBrakeFriction,tempo* 3,ref velocity ,0.1f * Time.deltaTime);
            }

        if(wheelHit4.sidewaysSlip < 0 )	
            tempo = (1 + -Input.GetAxis("Horizontal")) * Mathf.Abs(wheelHit4.sidewaysSlip *handBrakeFrictionMultiplier) ;
        
            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit4.sidewaysSlip > 0 )	
            tempo = (1 + Input.GetAxis("Horizontal") )* Mathf.Abs(wheelHit4.sidewaysSlip *handBrakeFrictionMultiplier);
        
            if(tempo < 0.5) tempo = 0.5f;
        
        if(wheelHit4.sidewaysSlip > .99f || wheelHit4.sidewaysSlip < -.99f){
            //handBrakeFriction = tempo * 3;
            float velocity = 0;
            handBrakeFriction = Mathf.SmoothDamp(handBrakeFriction,tempo* 3,ref velocity ,0.1f * Time.deltaTime);
            }

        else{
            handBrakeFriction = tempo;
        }

        if(Input.GetKey(KeyCode.S)){
            //Change gear to "R"
            Current_Gear = "R";
            
            //Enable reverse lights when car is reversing
            if(Enable_Reverselights_Lights){
                foreach(Light RL in ReverseLights){
                    RL.enabled = true;
                }
            }

            if(Enable_Reverselights_MeshRenderers){
                foreach(MeshRenderer RM in ReverseLights_MeshRenderers){
                    RM.enabled = true;
                }
            }
        }

        if(!Input.GetKey(KeyCode.S)){
            if(Enable_Reverselights_Lights){
                foreach(Light Rl in ReverseLights){
                    Rl.enabled = false;
                }
            }

            if(Enable_Reverselights_MeshRenderers){
                foreach(MeshRenderer RM in ReverseLights_MeshRenderers){
                    RM.enabled = false;
                }
            }
        }
    }

    public void Update(){

        //Rotating The Wheels So They Don't Slide
        var pos = Vector3.zero;
        var rot = Quaternion.identity;
        
        FL.GetWorldPose(out pos, out rot);
        Fl.position = pos;
        Fl.rotation = rot * Quaternion.Euler(FL_Rotation);

        FR.GetWorldPose(out pos, out rot);
        Fr.position = pos;
        Fr.rotation = rot * Quaternion.Euler(FR_Rotation);

        BL.GetWorldPose(out pos, out rot);
        Bl.position = pos;
        Bl.rotation = rot * Quaternion.Euler(BL_Rotation);

        BR.GetWorldPose(out pos, out rot);
        Br.position = pos;
        Br.rotation = rot * Quaternion.Euler(BR_Rotation);

        //Make Car Brake
        if(Input.GetKey(KeyCode.Space) == true){
            Brakes = BrakeForce;

            //Set the Current Gear to B
            Current_Gear = "B";

            //Turn on brake lights
            if(Enable_Brakelights_Lights){
                foreach(Light L in BrakeLights){
                    L.enabled = true;
                }
            }

            if(Enable_Brakelights_MeshRenderers){
                foreach(MeshRenderer BM in BrakeLights_MeshRenderers){
                    BM.enabled = true;
                }
            }

            //Drifting and changing wheel collider values
            if(Set_Drift_Settings_Automatically){
                FLforwardFriction = FL.forwardFriction;
                FLsidewaysFriction = FL.sidewaysFriction;

                FLforwardFriction.extremumValue = FLforwardFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
                FLsidewaysFriction.extremumValue = FLsidewaysFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;

                FRforwardFriction = FR.forwardFriction;
                FRsidewaysFriction = FR.sidewaysFriction;

                FRforwardFriction.extremumValue = FRforwardFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
                FRsidewaysFriction.extremumValue = FRsidewaysFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;

                BLforwardFriction = BL.forwardFriction;
                BLsidewaysFriction = BL.sidewaysFriction;

                BLforwardFriction.extremumValue = BLforwardFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
                BLsidewaysFriction.extremumValue = BLsidewaysFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;

                BRforwardFriction = BR.forwardFriction;
                BRsidewaysFriction = BR.sidewaysFriction;

                BRforwardFriction.extremumValue = BRforwardFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
                BRsidewaysFriction.extremumValue = BRsidewaysFriction.asymptoteValue = ((currSpeed * frictionMultiplier) / 300) + 1;
            }

            if(!Set_Drift_Settings_Automatically){
                //Variables
                FLforwardFriction = FL.forwardFriction;
                FLsidewaysFriction = FL.sidewaysFriction;

                FRforwardFriction = FR.forwardFriction;
                FRsidewaysFriction = FR.sidewaysFriction;

                BLforwardFriction = BL.forwardFriction;
                BLsidewaysFriction = BL.sidewaysFriction;

                BRforwardFriction = BR.forwardFriction;
                BRsidewaysFriction = BR.sidewaysFriction;

                //Setting The Extremium values to the ones that the user defined
                FLforwardFriction.extremumValue = Forward_Extremium_Value_When_Drifting;
                FLsidewaysFriction.extremumValue = Sideways_Extremium_Value_When_Drifting;

                FRforwardFriction.extremumValue = Forward_Extremium_Value_When_Drifting;
                FRsidewaysFriction.extremumValue = Sideways_Extremium_Value_When_Drifting;

                BLforwardFriction.extremumValue = Forward_Extremium_Value_When_Drifting;
                BLsidewaysFriction.extremumValue = Sideways_Extremium_Value_When_Drifting;

                BRforwardFriction.extremumValue = Forward_Extremium_Value_When_Drifting;
                BRsidewaysFriction.extremumValue = Sideways_Extremium_Value_When_Drifting;
            }
        }

        else{
            Brakes = 0f;
        }

        FL.brakeTorque = Brakes;
        FR.brakeTorque = Brakes;
        BL.brakeTorque = Brakes;
        BR.brakeTorque = Brakes;

        if(!Input.GetKey(KeyCode.Space)){
            //Turn off brake lights
            if(Enable_Brakelights_Lights){
                foreach(Light L in BrakeLights){
                    L.enabled = false;
                }
            }

            if(Enable_Brakelights_MeshRenderers){
                foreach(MeshRenderer BM in BrakeLights_MeshRenderers){
                    BM.enabled = false;
                }
            }
        }


        if(Enable_Audio == true){
                //Play Car Audio
            if(Input.GetKey(KeyCode.W)){
                //Play Engine Sound
                Engine_Sound.Play();

                //Adjust Engine Sound Volume To Car Motor Torque
                Forward_volume = -1f * (Motor_Torque/BR.motorTorque);

                //Adjust Engine Speed
                Forward_pitch = -1f * (BR.motorTorque/Motor_Torque);

                if(Forward_volume > Max_Volume){
                    Forward_volume = Max_Volume;

                    if(Forward_pitch > Max_Engine_Audio_Pitch){
                        Forward_pitch = Max_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    if(Forward_pitch < Min_Engine_Audio_Pitch){
                        Forward_pitch = Min_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    else{
                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }
                }

                if(Forward_volume < Min_Volume){
                    Forward_volume = Min_Volume;

                    if(Forward_pitch > Max_Engine_Audio_Pitch){
                        Forward_pitch = Max_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    if(Forward_pitch < Min_Engine_Audio_Pitch){
                        Forward_pitch = Min_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    else{
                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }
                }
            }

            if(Input.GetKey(KeyCode.S)){
                //Play Engine Sound
                Engine_Sound.Play();

                //Adjust Engine Sound Volume To Car Motor Torque
                Reverse_volume = Motor_Torque/BR.motorTorque;

                //Adjust Audio To Engine Speed
                Reverse_pitch = -1f * (BR.motorTorque/Motor_Torque);

                if(Forward_volume > Max_Volume){
                    Forward_volume = Max_Volume;

                    if(Forward_pitch > Max_Engine_Audio_Pitch){
                        Forward_pitch = Max_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    if(Forward_pitch < Min_Engine_Audio_Pitch){
                        Forward_pitch = Min_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    else{
                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }
                }

                if(Forward_volume < Min_Volume){
                    Forward_volume = Min_Volume;

                    if(Forward_pitch > Max_Engine_Audio_Pitch){
                        Forward_pitch = Max_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    if(Forward_pitch < Min_Engine_Audio_Pitch){
                        Forward_pitch = Min_Engine_Audio_Pitch;

                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }

                    else{
                        Engine_Sound.volume = Forward_volume;
                        Engine_Sound.pitch = Forward_pitch;
                    }
                }
            }
        }
        
    }
}