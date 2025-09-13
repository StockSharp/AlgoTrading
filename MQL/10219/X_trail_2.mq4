//+------------------------------------------------------------------+
//|                                                                  |
//|                      Copyright © 2010,                           |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright " www.Forexyangu.com"
#property link      "http://www.forexyangu.com"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
/*****************************************************-----READ THIS-------******************************************************
 *******************************************************************************************************************************/
 //-----------------------------------------------------------------------------------------------------------------------------
/*DONATE AT TO SUPPORT MY FREE PROJECTS AND TO RECEIVE NON OPEN PROJECTS AND ADVANCED VERSIONS OF EXISTING PROJECTS WHEN AVAILABLE: 
//------------------------------------------------------------------------------------------------------------------------------
// Visit www.forexyangu.com to donate via paypal and credit card or by moneybookers send to my email admin@forexyangu.com
//------------------------------------------------------------------------------------------------------------------------------
//SUPPORT AND INQUIRIES EMAIL:        admin@forexyangu.com
//------------------------------------------------------------------------------------------------------------------------------
/*******************************************************************************************************************************
 *************************************************--------END------*************************************************************/
extern string Visit ="www.ForexYangu.com";
extern int RunIntervalSeconds =10; //Run EA in intervals of... seconds
extern string MA1 ="1st Moving average parameters";
extern int MA1_Period    =1;   //ma setting
extern int MA1_Shift =0;    //ma setting
extern int MA1_method=0;    //ma setting
extern int MA1_AppliedPrice=4;
extern string MA2 ="2nd Moving average parameters";
extern int MA2_Period    =14;   //ma setting
extern int MA2_Shift =10;    //ma setting
extern int MA2_method=0;    //ma setting
extern int MA2_AppliedPrice=4;
extern string ContactMe ="admin@forexyangu.com"; // support email



//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+


int start()
  {
  //alert criteria
  
double MA1_bc=iMA(NULL,0,MA1_Period,MA1_Shift,MA1_method,MA1_AppliedPrice,0);
double MA2_bc=iMA(NULL,0,MA2_Period,MA2_Shift,MA2_method,MA2_AppliedPrice,0);
double MA1_bp=iMA(NULL,0,MA1_Period,MA1_Shift,MA1_method,MA1_AppliedPrice,1);
double MA2_bp=iMA(NULL,0,MA2_Period,MA2_Shift,MA2_method,MA2_AppliedPrice,1);
double MA1_bl=iMA(NULL,0,MA1_Period,MA1_Shift,MA1_method,MA1_AppliedPrice,2);
double MA2_bl=iMA(NULL,0,MA2_Period,MA2_Shift,MA2_method,MA2_AppliedPrice,2);
double MA1_bl2=iMA(NULL,0,MA1_Period,MA1_Shift,MA1_method,MA1_AppliedPrice,3);
double MA2_bl2=iMA(NULL,0,MA2_Period,MA2_Shift,MA2_method,MA2_AppliedPrice,3);
  
  
   
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