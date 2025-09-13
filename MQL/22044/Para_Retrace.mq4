//+------------------------------------------------------------------+
//| Para_Retrace.mq4                                                 |
//| Copyright 2018 , raymondyeung.htc@gmail.com                      |
//| Speed Technology                                                 |
//| Basic on Technical indicators Parabolic Sar                      |
//| Isar_Step=0.01;                                                  |
//| Isar_Max=0.2;                                                    |
//+------------------------------------------------------------------+

#property strict
#include <stdlib.mqh>
#include <WinUser32.mqh>
//+------------------------------------------------------------------+
//| expert initialization_ function                                  |
//+------------------------------------------------------------------+


//+-----------------------------------------------------------------------+
//| Return the local time, Eastern Time (ET),Bid, Ask, High, Low          |
//+-----------------------------------------------------------------------+
string dtbahl()
  {
   string dt="Local Time : "+TimeToStr(LocalTime(),TIME_DATE|TIME_SECONDS)+
             "        raymondyeung.htc@gmail.com"+"\n"+
             "Bid :"+DoubleToStr(Bid,Digits)+" Ask :"+DoubleToStr(Ask,Digits)+
             " High :"+DoubleToStr(High[0],Digits)+" Low :"+DoubleToStr(Low[0],Digits);

   return (dt);
  }
//+------------------------------------------------------------------+
//|Global Variable creation                                          |
//+------------------------------------------------------------------+

bool g_init()
  {
   int MultiPip=1;
   if((Ask-Bid)/Point>10) MultiPip=10;
   bool InitGlobalVar=false;

   if(!GlobalVariableCheck("GMT_Time_Diff"))
     {
      GlobalVariableSet("GMT_Time_Diff",43200);
      //+------------------------------------------------------------------+
      //|  Eastern Time (ET)  GMT-4 HK  time zone GMT+8 ,                  |
      //|   Zone Diff = 12 , 12*3600 = 43200                               |
      //+------------------------------------------------------------------+
      InitGlobalVar=(InitGlobalVar || true);
     }

//+------------------------------------------------------------------+
//|  Stop Loss Pips after Open position                              |
//+------------------------------------------------------------------+
   if(!GlobalVariableCheck("_ParaSL_SL"))
     {
      GlobalVariableSet("_ParaSL_SL",30*MultiPip);
      InitGlobalVar=(InitGlobalVar || true);
      Comment("First time, the PingPong Stop Loss is 100,\npress Function Key F3 to change. (if need)");
      Sleep(1000);
      Comment("");
     }

//+------------------------------------------------------------------+
//|  Take Propfit Pips after Open position                           |
//+------------------------------------------------------------------+
   if(!GlobalVariableCheck("_ParaSL_TP"))
     {
      GlobalVariableSet("_ParaSL_TP",30*MultiPip);
      InitGlobalVar=(InitGlobalVar || true);
      Comment("First time, the PingPong Take Profit is 100,\npress Function Key F3 to change. (if need)");
      Sleep(1000);
      Comment("");
     }

//+------------------------------------------------------------------+
//|  Retrace Entry , Pips with Parabolic Sar                         +
//|                                                                  +
//|  Remark :                                                        +
//|  Limit Sell ; Bid Price with Parabolic Sars Value                +
//|  Limit Buy  ; Ask Price with Parabolic Sars Value                +
//+------------------------------------------------------------------+       
   if(!GlobalVariableCheck("_Para_Diff"))
     {
      GlobalVariableSet("_Para_Diff",0);
      InitGlobalVar=(InitGlobalVar || true);
      Comment("The diff. entry price with the Parabolic Sars Value");
      Sleep(1000);
      Comment("");
     }

//+------------------------------------------------------------------+
//|  Open Lots                                                       +
//+------------------------------------------------------------------+  
   if(!GlobalVariableCheck("_Para_lots"))
     {
      GlobalVariableSet("_Para_lots",0.01);
      InitGlobalVar=(InitGlobalVar || true);
     }

   return(InitGlobalVar);
  }
//+------------------------------------------------------------------+
//| Initialization                                                   |
//+------------------------------------------------------------------+

int init()
  {
   bool p_init=(g_init());

   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }

bool res;
double  Limit_Sell=0,Limit_Buy=0;
string Market_Price="";
int  ticket=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

int start()
  {
//+------------------------------------------------------------------+
//| Below parameter can be change by Users                           |
//| Isar_Step=0.01;                                                  |
//| Isar_Max=0.2;                                                    |
//+------------------------------------------------------------------+

   double Isar_Step,Isar_Max;
   Isar_Step=0.01;
   Isar_Max=0.2;
   double Para_Current;
   Para_Current=iSAR(NULL,0,Isar_Step,Isar_Max,0);
   int Dec_Digit=4;
   int cnt=0,cnt2=0;
   int MultiPip=1;
   if((Ask-Bid)/Point>10) MultiPip=10;
   if((Ask-Bid)/Point>10) Dec_Digit=5;

   string DisplayTime=dtbahl();
   if(Limit_Sell!=0)
      DisplayTime=DisplayTime+"\n"+"Limit sell at "+DoubleToStr(Limit_Sell,Digits);
   if(Limit_Buy!=0)
      DisplayTime=DisplayTime+"\n"+"Limit Buy at "+DoubleToStr(Limit_Buy,Digits);
   Comment(DisplayTime);

   if(GlobalVariableGet("_Para_Diff")!=0)
     {
      if(High[0]<Para_Current && Low[0]<Para_Current)
        {
         Limit_Sell=Para_Current-GlobalVariableGet("_Para_Diff")*Point;
         if(Bid>=Limit_Sell)
           {
            ticket=OrderSend(Symbol(),OP_SELL,GlobalVariableGet("_Para_lots"),Bid,0,0,0," Limit_SELL",255,0,CLR_NONE);

            if(ticket>1 && OrderSelect(ticket,SELECT_BY_TICKET)==true)
              {
               Sleep(2000);
               res=OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice()+GlobalVariableGet("_ParaSL_SL")*Point,OrderOpenPrice()-GlobalVariableGet("_ParaSL_TP")*Point,0,CLR_NONE);
               PlaySound("ok.wav");
               GlobalVariableSet("_Para_Diff",0);
              }
           }
        }
      else
        {
         Limit_Buy=Para_Current+GlobalVariableGet("_Para_Diff")*Point;
         if(Ask<=Limit_Buy)
           {
            ticket=OrderSend(Symbol(),OP_BUY,GlobalVariableGet("_Para_lots"),Ask,0,0,0," Limit_Buy",255,0,CLR_NONE);
            if(ticket>1 && OrderSelect(ticket,SELECT_BY_TICKET)==true)
              {
               Sleep(2000);
               res=OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice()-GlobalVariableGet("_ParaSL_SL")*Point,OrderOpenPrice()+GlobalVariableGet("_ParaSL_TP")*Point,0,CLR_NONE);
               PlaySound("ok.wav");
               GlobalVariableSet("_Para_Diff",0);
              }
           }
        }
     }
   else
     {
      Limit_Sell= 0;
      Limit_Buy = 0;
     }

   Market_Price="Bid: "+DoubleToStr(Bid,Digits)+"   SAR:"+DoubleToStr(Para_Current,Digits)+
                " Bid Diff : "+DoubleToStr((MathAbs(Bid-Para_Current)/Point),0)+" Ask Diff :"+DoubleToStr((MathAbs(Ask-Para_Current)/Point),0);
   ObjectCreate("Market_Price_Label",OBJ_LABEL,0,0,0);
   ObjectSet("Market_Price_Label",OBJPROP_CORNER,2);
   ObjectSet("Market_Price_Label",OBJPROP_XDISTANCE,20);
   ObjectSet("Market_Price_Label",OBJPROP_YDISTANCE,20);
   ObjectSetText("Market_Price_Label",Market_Price,18,"Arial",Blue);
   return(0);

  }
//+------------------------------------------------------------------+