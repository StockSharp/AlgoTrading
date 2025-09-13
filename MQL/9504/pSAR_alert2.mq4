//+------------------------------------------------------------------+
//|                                                                  |
//|                      Copyright © 2010,                           |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright " www.Forexyangu.com"
#property link      "http://www.metaquotes.net"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
/*****************************************************-----READ THIS-------******************************************************
 *******************************************************************************************************************************/
 //-----------------------------------------------------------------------------------------------------------------------------
/*DONATE TO SUPPORT MY FREE PROJECTS AND TO RECEIVE NON OPEN PROJECTS AND ADVANCED VERSIONS OF EXISTING PROJECTS WHEN AVAILABLE: 
//------------------------------------------------------------------------------------------------------------------------------
__my moneybookers email is admin@forexyangu.com anyone can easily join moneybookers at www.moneybookers.com and pay people via their 
email through numerous payment methods__*/
//------------------------------------------------------------------------------------------------------------------------------
//SUPPORT AND INQUIRIES EMAIL:        admin@forexyangu.com
//------------------------------------------------------------------------------------------------------------------------------
/*******************************************************************************************************************************
 *************************************************--------END------*************************************************************/
extern string DonateTo ="Moneybookers: admin@forexyangu.com";
extern int RunIntervalSeconds =10; //Run EA in intervals of... seconds
extern double Step    =0.02;   //Parabolic setting
extern double Maximum =0.2;    //Parabolic setting
extern string ContactMe ="admin@forexyangu.com"; // support email

 
 

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+


int start()
  {
  //alert criteria
   
 if ((iSAR(NULL, 0,Step,Maximum, 0)<iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)>iClose(NULL,0,1))) //Signal Buy
 {

   
   PlaySound("alert.wav");
   Alert("Alarm triggered by ",Symbol());
     // Alarm
 
 }
 if ((iSAR(NULL, 0,Step,Maximum, 0)>iClose(NULL,0,0))&&(iSAR(NULL, 0,Step,Maximum, 1)<iClose(NULL,0,1))) //Signal Sell
 {

   
   PlaySound("alert.wav");
   Alert("Alarm triggered by ",Symbol());
      // Alarm 
 

 }


Sleep(RunIntervalSeconds*1000);

//----------
return(0);
  }