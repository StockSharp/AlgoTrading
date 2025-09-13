//+------------------------------------------------------------------+
//|                                                                  |
//|                      Copyright © 2011,                           |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright " www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

extern string SupportAndMoreVisit ="www.FxAutomated.com";
extern string ForexAutotradingSignals ="www.TradingBug.com";
extern int RunIntervalSeconds =10; //Run EA in intervals of... seconds
extern double Step    =0.02;   //Parabolic setting
extern double Maximum =0.2;    //Parabolic setting

 

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