//+------------------------------------------------------------------+
//|                                                  EA_MA_Email.mq4 |
//|                                               erekit83@yahoo.com |
//|                                        http://100k2.blogspot.com |
//+------------------------------------------------------------------+
#property copyright "erekit83@yahoo.com"
#property link      "http://100k2.blogspot.com"

//---- input parameters
extern bool SendEmailAlert = True;
extern bool m20_50   = false;
extern bool m20_100  = false;
extern bool m20_200  = false;
extern bool m50_100  = false;
extern bool m50_200  = true;
extern bool m100_200 = false;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   
double ma_20 = iMA(NULL, 0, 20, 0, MODE_EMA, PRICE_OPEN, 0);
double ma_50 = iMA(NULL, 0, 50, 0, MODE_EMA, PRICE_OPEN, 0);
double ma_100 = iMA(NULL, 0, 100, 0, MODE_EMA, PRICE_OPEN, 0);
double ma_200 = iMA(NULL, 0, 200, 0, MODE_EMA, PRICE_OPEN, 0);
double ma_20_p = iMA(NULL, 0, 20, 0, MODE_EMA, PRICE_OPEN, 1);
double ma_50_p = iMA(NULL, 0, 50, 0, MODE_EMA, PRICE_OPEN, 1);
double ma_100_p = iMA(NULL, 0, 100, 0, MODE_EMA, PRICE_OPEN, 1);
double ma_200_p = iMA(NULL, 0, 200, 0, MODE_EMA, PRICE_OPEN, 1);

string ChartPeriod;

if (Period()==1)      ChartPeriod="PERIOD_M1";
if (Period()==5)      ChartPeriod="PERIOD_M5";
if (Period()==15)     ChartPeriod="PERIOD_M15";
if (Period()==30)     ChartPeriod="PERIOD_M30";
if (Period()==60)     ChartPeriod="PERIOD_H1";
if (Period()==240)    ChartPeriod="PERIOD_H4";
if (Period()==1440)   ChartPeriod="PERIOD_D1";
if (Period()==10080)  ChartPeriod="PERIOD_W1";
if (Period()==43200)  ChartPeriod="PERIOD_MN1";



if ( SendEmailAlert == True )
{  

  if(m20_50 == true) 
  { if ( ma_20_p  < ma_50_p   &&  ma_20  > ma_50)  SendMail(Symbol() + " 20>50 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_20_p  > ma_50_p   &&  ma_20  < ma_50)  SendMail(Symbol() + " 20<50 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    
    }
    
     if(m20_100 == true) 
  { if ( ma_20_p  < ma_100_p   &&  ma_20  > ma_100)  SendMail(Symbol() + " 20>100 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_20_p  > ma_100_p   &&  ma_20  < ma_100)  SendMail(Symbol() + " 20<100 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    
    }
    
     if(m20_200 == true) 
  { if ( ma_20_p  < ma_200_p   &&  ma_20  > ma_200)  SendMail(Symbol() + " 20>200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_20_p  > ma_200_p   &&  ma_20  < ma_200)  SendMail(Symbol() + " 20<200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    
    }
    
     if(m50_100 == true) 
  { if ( ma_50_p  < ma_100_p   &&  ma_50  > ma_100)  SendMail(Symbol() + " 50>100 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_50_p  > ma_100_p    &&  ma_50 < ma_100)  SendMail(Symbol() + " 50<100 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    
    }
    
     if(m50_200 == true) 
  { if ( ma_50_p  < ma_200_p   &&  ma_50  > ma_200)  SendMail(Symbol() + " 50>200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_50_p  > ma_200_p   &&  ma_50 < ma_200)   SendMail(Symbol() + " 50<200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    
    }
    
    
     if(m100_200 == true) 
  { if ( ma_100_p  < ma_200_p   &&  ma_100  > ma_200)  SendMail(Symbol() + " 100>200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    if ( ma_100_p  > ma_200_p   &&  ma_100  < ma_200)  SendMail(Symbol() + " 100<200 " + ChartPeriod ,"Date and Time : "+TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS)+" \n"+"Currency      : "+Symbol()    +"");
    }
 
}






//----
   return(0);
  }
//+------------------------------------------------------------------+