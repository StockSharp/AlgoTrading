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
extern string Visit ="www.ForexYangu.com";
extern int RunIntervalSeconds =10; //Run EA in intervals of... seconds
extern int MA1    =1;   //ma setting
extern int Shift1 =0;    //ma setting
extern int MA2    =14;   //ma setting
extern int Shift2 =10;    //ma setting
extern string ContactMe ="admin@forexyangu.com"; // support email



//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+


int start()
  {
  //alert criteria
  
double MA1_bc=iMA(NULL,0,MA1,Shift1,0,4,0);
double MA2_bc=iMA(NULL,0,MA2,Shift2,0,4,0);
double MA1_bp=iMA(NULL,0,MA1,Shift1,0,4,1);
double MA2_bp=iMA(NULL,0,MA2,Shift2,0,4,1);
double MA1_bl=iMA(NULL,0,MA1,Shift1,0,4,2);
double MA2_bl=iMA(NULL,0,MA2,Shift2,0,4,2);
double MA1_bl2=iMA(NULL,0,MA1,Shift1,0,4,3);
double MA2_bl2=iMA(NULL,0,MA2,Shift2,0,4,3);
  
  
   
 if ((MA1_bc>MA2_bc)&&(MA1_bp>MA2_bp)&&(MA1_bl<MA2_bl))
 {

   
   PlaySound("alert.wav");
   Alert("Alarm triggered by ",Symbol());
     // Alarm
 
 }
 if ((MA1_bc<MA2_bc)&&(MA1_bp<MA2_bp)&&(MA1_bl>MA2_bl))
 {

   
   PlaySound("alert.wav");
   Alert("Alarm triggered by ",Symbol());
      // Alarm 
 

 }


Sleep(RunIntervalSeconds*1000);

//----------
return(0);
  }