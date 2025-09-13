//+------------------------------------------------------------------+
//|                                                    Aver12345.mq4 |
//|                                   Copyright © 2011, rockyhoangdn |
//|                                           rockyhoangdn@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, rockyhoangdn"
#property link      "rockyhoangdn@gmail.com"
//+------------------------------------------------------------------+
//| Vars                                                             |
//+------------------------------------------------------------------+
double   iStouchvalue1 = 0;
double   iStouchvalue2 = 0;
double   iStouchvalue3 = 0;
double   iStouchvalue4 = 0;
double   iStouchvalue = 0;
double   iWprvalue1 = 0;
double   iWprvalue2 = 0;
double   iWprvalue3 = 0;
double   iWprvalue4 = 0;
double   iWprvalue = 0;
double   Postzigzagvalue1=0;
double   Postzigzagvalue2=0;
double   Buyvalue = 0, Sellvalue = 0;
double   Signalbuyvalue = 0, Signalsellvalue = 0;
int      Buyposition = 0, Sellposition =0;
int      MagicNumber = 162429;
int      Slippage = 3;
double   StopLoss = 0;//inpip
double   TakeProfit = 0;//in pip
double   Maximum.Ft.Profit = 0;
double   Minimum.Ft.Profit = 0;
int      Buycount=0,Sellcount=0;
double   B1=0,B2=0,B3=0,B4=0,B5=0,S1=9999,S2=9999,S3=9999,S4=9999,S5=9999;
//---- input parameters
extern string LotsOptimized                  = "====== LotsOptimized ======";
extern bool   MoneyManagement = true;
extern bool   UseMinilot=true;
extern double Lots=0.1;
extern double MaximumRisk=0.1;//10%
extern double MinLot=0.1;
extern string Stoploss_Takeprofit            = "====== Setup Stoploss/Takeprofit ======";
extern double MyStopLoss = 800;
extern double MyTakeProfit = 0;
extern double Stopoutlevel=50;
extern string Nextopenposition_in_point      = "====== Nextopenposition in point ======";
extern int    Nextopenposition =150;
extern string Delay_open_in_point            = "====== Delay open in point ======";
extern double Delay = 0;//delay inpoint when signal appearent
extern int    Maxiposition=5;//Maximum=5
extern string Close_Optimized                = "====== Close_Optimized ======";
extern bool   Closebyprofit = false;
extern double Needprofit =1000;//Maximum Profit per Order
extern double Losscan = 500; //Maximum Loss per Order
extern string TimeFrame                      = "====== TimeFrame ======";
extern bool   TimeFrameM1=false;
extern bool   TimeFrameM5=true;//Best timeframe to trade
extern bool   TimeFrameM15=true;
extern bool   TimeFrameM30=false;
extern bool   TimeFrameM60=false;
extern bool   TimeFrameM240=false;
extern string IndicatorsPeriod                      = "====== Indicators Period ======";
extern int    iStochPeriod = 26;
extern int    iWPRPeriod = 26;
extern int    PostzigzagPeirod = 14;

string comments="";
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
//+------------------------------------------------------------------+
//| Select Next open position in point                               |
//+------------------------------------------------------------------+
   if (Symbol() == "USDJYP") Nextopenposition=150;
   if (Symbol() == "EURUSD") Nextopenposition=450;
   if (Symbol() == "EURJYP") Nextopenposition=300;

//+------------------------------------------------------------------+
//| indicator                                                        |
//+------------------------------------------------------------------+

   iStouchvalue1= iStochastic(NULL,0,iStochPeriod,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue2= iStochastic(NULL,0,iStochPeriod*3,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue3= iStochastic(NULL,0,iStochPeriod*6,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue4= iStochastic(NULL,0,iStochPeriod*12,3,3,MODE_SMMA,1,MODE_MAIN,1);  
   iStouchvalue= (iStouchvalue1+iStouchvalue2+iStouchvalue3+iStouchvalue4)/4;
   iWprvalue1= iWPR(NULL,0,iWPRPeriod,1);    
   iWprvalue2= iWPR(NULL,0,iWPRPeriod*3,1);    
   iWprvalue3= iWPR(NULL,0,iWPRPeriod*6,1);    
   iWprvalue4= iWPR(NULL,0,iWPRPeriod*12,1);    
   iWprvalue= (iWprvalue1+iWprvalue2+iWprvalue3+iWprvalue4)/4;    
   Postzigzagvalue1=iCustom(NULL, 0, "Post-zigzag",PostzigzagPeirod,1,1);
   Postzigzagvalue2=iCustom(NULL, 0, "Post-zigzag",PostzigzagPeirod,2,1);
    
      if (iStouchvalue < 0.1 && iWprvalue <- 90 )  
         {
         Buyvalue=1;
         Signalbuyvalue=Ask;
         Signalsellvalue=9999;
         Sellvalue=0; 
         ObjectCreate("Trendup", OBJ_LABEL, 0, 0, 0);
         ObjectSetText("Trendup","UP",25, "Verdana", Lime);
         ObjectSet("Trendup", OBJPROP_CORNER, 0);
         ObjectSet("Trendup", OBJPROP_XDISTANCE, 39);
         ObjectSet("Trendup", OBJPROP_YDISTANCE, 46);
         ObjectDelete("Trend");
         }
      if (iStouchvalue > 99.9 && iWprvalue >-5) 
         {
         Sellvalue=1;
         Signalsellvalue=Bid;
         Signalbuyvalue=0;
         Buyvalue=0;
         ObjectCreate("Trend", OBJ_LABEL, 0, 0, 0);
         ObjectSetText("Trend","DOWN",25, "Verdana", Red);
         ObjectSet("Trend", OBJPROP_CORNER, 0);
         ObjectSet("Trend", OBJPROP_XDISTANCE, 9);
         ObjectSet("Trend", OBJPROP_YDISTANCE, 46);
         ObjectDelete("Trendup");
         }
      if(Buyvalue==1)
         {
         if(((Ask - Signalbuyvalue) < -Delay*Point) && Postzigzagvalue2!=0) 
            {
            Buyvalue=2;
            Alert("Signal Buy at " + Symbol());
            }
         }   
      if(Sellvalue==1)
         {
         if(((Bid - Signalsellvalue) > Delay*Point) && Postzigzagvalue1 !=0) 
            {
            Sellvalue=2;
            Alert("Signal Sell at "+Symbol());
            }
         }   
//----Exit indicator
//+------------------------------------------------------------------+
//| Print last trade info                                            |
//+------------------------------------------------------------------+
      ObjectCreate("BUY POSITION", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("BUY POSITION","++ BUY ++",11, "Verdana", Lime);
      ObjectSet("BUY POSITION", OBJPROP_CORNER, 1);
      ObjectSet("BUY POSITION", OBJPROP_XDISTANCE, 10);
      ObjectSet("BUY POSITION", OBJPROP_YDISTANCE, 10);
      
      ObjectCreate("B1", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("B1",DoubleToStr(B1,5),10, "Verdana", Lime);
      ObjectSet("B1", OBJPROP_CORNER, 1);
      ObjectSet("B1", OBJPROP_XDISTANCE, 10);
      ObjectSet("B1", OBJPROP_YDISTANCE, 30);

      ObjectCreate("B2", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("B2",DoubleToStr(B2,5),10, "Verdana", Lime);
      ObjectSet("B2", OBJPROP_CORNER, 1);
      ObjectSet("B2", OBJPROP_XDISTANCE, 10);
      ObjectSet("B2", OBJPROP_YDISTANCE, 50);

      ObjectCreate("B3", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("B3",DoubleToStr(B3,5),10, "Verdana", Lime);
      ObjectSet("B3", OBJPROP_CORNER, 1);
      ObjectSet("B3", OBJPROP_XDISTANCE, 10);
      ObjectSet("B3", OBJPROP_YDISTANCE, 70);

      ObjectCreate("B4", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("B4",DoubleToStr(B4,5),10, "Verdana", Lime);
      ObjectSet("B4", OBJPROP_CORNER, 1);
      ObjectSet("B4", OBJPROP_XDISTANCE, 10);
      ObjectSet("B4", OBJPROP_YDISTANCE, 90);

      ObjectCreate("B5", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("B5",DoubleToStr(B5,5),10, "Verdana", Lime);
      ObjectSet("B5", OBJPROP_CORNER, 1);
      ObjectSet("B5", OBJPROP_XDISTANCE, 10);
      ObjectSet("B5", OBJPROP_YDISTANCE, 110);

      ObjectCreate("SELL POSITION", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("SELL POSITION","++ SELL ++",11, "Verdana", Red);
      ObjectSet("SELL POSITION", OBJPROP_CORNER, 1);
      ObjectSet("SELL POSITION", OBJPROP_XDISTANCE, 10);
      ObjectSet("SELL POSITION", OBJPROP_YDISTANCE, 130);

      ObjectCreate("S1", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("S1",DoubleToStr(S1,5),10, "Verdana", Red);
      ObjectSet("S1", OBJPROP_CORNER, 1);
      ObjectSet("S1", OBJPROP_XDISTANCE, 10);
      ObjectSet("S1", OBJPROP_YDISTANCE, 150);

      ObjectCreate("S2", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("S2",DoubleToStr(S2,5),10, "Verdana", Red);
      ObjectSet("S2", OBJPROP_CORNER, 1);
      ObjectSet("S2", OBJPROP_XDISTANCE, 10);
      ObjectSet("S2", OBJPROP_YDISTANCE, 170);

      ObjectCreate("S3", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("S3",DoubleToStr(S3,5),10, "Verdana", Red);
      ObjectSet("S3", OBJPROP_CORNER, 1);
      ObjectSet("S3", OBJPROP_XDISTANCE, 10);
      ObjectSet("S3", OBJPROP_YDISTANCE, 190);

      ObjectCreate("S4", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("S4",DoubleToStr(S4,5),10, "Verdana", Red);
      ObjectSet("S4", OBJPROP_CORNER, 1);
      ObjectSet("S4", OBJPROP_XDISTANCE, 10);
      ObjectSet("S4", OBJPROP_YDISTANCE, 210);

      ObjectCreate("S5", OBJ_LABEL, 0, 0, 0);
      ObjectSetText("S5",DoubleToStr(S5,5),10, "Verdana", Red);
      ObjectSet("S5", OBJPROP_CORNER, 1);
      ObjectSet("S5", OBJPROP_XDISTANCE, 10);
      ObjectSet("S5", OBJPROP_YDISTANCE, 230);

      if(OrdersTotal()==0||B1==0) ObjectSetText("B1","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||B2==0) ObjectSetText("B2","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||B3==0) ObjectSetText("B3","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||B4==0) ObjectSetText("B4","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||B5==0) ObjectSetText("B5","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||S1==9999) ObjectSetText("S1","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||S2==9999) ObjectSetText("S2","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||S3==9999) ObjectSetText("S3","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||S4==9999) ObjectSetText("S4","0.00000",10, "Verdana", Yellow);
      if(OrdersTotal()==0||S5==9999) ObjectSetText("S5","0.00000",10, "Verdana", Yellow);
//+------------------------------------------------------------------+
//| Check for last trade                                             |
//+------------------------------------------------------------------+
   LastTrade();
//+------------------------------------------------------------------+
//| Open position                                                    |
//+------------------------------------------------------------------+
   if((TimeFrameM1==true && Period()==1) || (TimeFrameM5==true  && Period()==5) || (TimeFrameM15==true  && Period()==15) || (TimeFrameM30==true  && Period()==30) || (TimeFrameM60==true && Period()==60)  || (TimeFrameM240==true && Period()==240) )
      {   
      if (OrdersTotal() < 1)
         {
         if (Buyvalue ==2)
            {
            Openbuy();
            Buyposition = 1;
            }
         if (Sellvalue ==2)
            {
            Opensell();
            Sellposition = 1;
            }
         }            
      if (OrdersTotal() == 1 && Maxiposition > 1)
         {
         if((Ask - B1) < -Nextopenposition*Point && Buyposition == 1)
            {
            Openbuy();
            Buyposition = 2;
            }
         if((Bid - S1) > Nextopenposition*Point && Sellposition == 1)
            {
            Opensell();
            Sellposition = 2;
            }
         }            
      if (OrdersTotal() == 2 && Maxiposition > 2)
         {
         if((Ask - B2) < -Nextopenposition*1.5*Point && Buyposition == 2)
            {
            Openbuy();
            Buyposition = 3;
            }
         if((Bid - S2) > Nextopenposition*1.5*Point && Sellposition == 2)
            {
            Opensell();
            Sellposition = 3;
           }
         } 
      if (OrdersTotal() == 3 && Maxiposition > 3 && (AccountEquity()*100/(AccountMargin()+0.00000000000001)) >500)
         {
         if((Ask - B3) < -Nextopenposition*2*Point && Buyposition == 3)
            {
            Openbuy();
            Buyposition = 4;
            }
         if((Bid - S3) > Nextopenposition*2*Point && Sellposition == 3)
            {
            Opensell();
            Sellposition = 4;
            }
         } 
      if (OrdersTotal() == 4 && Maxiposition > 4 && (AccountEquity()*100/(AccountMargin()+0.00000000000001)) >400 )
         {
         if((Ask - B4) < -Nextopenposition*3*Point && Buyposition == 4)
            {
            Openbuy();
            Buyposition = 5;
            }
         if((Bid - S4) > Nextopenposition*3*Point && Sellposition == 4)
            {
            Opensell();
            Sellposition = 5;
            }
         } 
         
//Exit lock position         
      }
//+--Exit open position----------------------------------------------+
//+------------------------------------------------------------------+
//| Print detail                                                     |
//+------------------------------------------------------------------+
   Printdetail();
//+------------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Close position by indicator                                      |
//+------------------------------------------------------------------+
   if((TimeFrameM1==true && Period()==1) || (TimeFrameM5==true  && Period()==5) || (TimeFrameM15==true  && Period()==15) || (TimeFrameM30==true  && Period()==30) || (TimeFrameM60==true && Period()==60)  || (TimeFrameM240==true && Period()==240) )
      {  
      if (Buyvalue == 2)
         {
         Closesell();
         } 
      if (Sellvalue == 2)
         {
         Closebuy();
         }
      } 
//+------------------------------------------------------------------+
//| Close by profit optimized                                        |
//+------------------------------------------------------------------+
    if(AccountMargin()>0)
      {
      if((AccountEquity()*100/(AccountMargin()+0.00000000000001))<Stopoutlevel )
         {
         CloseAll();
         }
      }
   if (Closebyprofit == true)   
      {
      for(int i=0;i<OrdersTotal();i++)
        {
        OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
        if(OrderSymbol()==Symbol()) 
            {
            if((OrderType()==OP_BUY && OrderProfit()>Needprofit)||(OrderType()==OP_BUY && OrderProfit()<-Losscan))
               {
               Closebuy();
               }
            if((OrderType()==OP_SELL && OrderProfit()>Needprofit)||(OrderType()==OP_SELL && OrderProfit()<-Losscan))
               {
               Closesell();
               }   
            }
         }
         if (AccountProfit() > Needprofit+1 || AccountProfit() < -Losscan-1)
            {
            CloseAll();
            } 
      }
//+--Exit close position --------------------------------------------------+  
   return(0);
  }
//+--Exit start------------------------------------------------------------+
//+--Start Function Define-------------------------------------------------+
//+------------------------------------------------------------------+
//| Close Open Position  function                                    |
//+------------------------------------------------------------------+
int Closebuy()
  { 
   for(int i=0; i<OrdersTotal(); i++)
     {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if (OrderType()==OP_BUY && OrderSymbol()==Symbol())

        {
         OrderClose(OrderTicket(),OrderLots(),Bid,3,Yellow);
         Sellvalue=0;
         B1 = 0;
         B2 = 0;
         B2 = 0;
         B4 = 0;
         B5 = 0;
         Buyposition = 0;
         }
     }
  }
int Closesell()
  { 
    for(int i=0; i<OrdersTotal(); i++)
      {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderType()==OP_SELL && OrderSymbol()==Symbol())
         {
         OrderClose(OrderTicket(),OrderLots(),Ask,3,Yellow);
         Buyvalue =0;
         S1=9999;
         S2=9999;
         S3=9999;
         S4=9999;
         S5=9999;
         Sellposition = 0;
         }
       }
  }
int CloseAll()
   {
   while(OrdersTotal()>0)
      {
      if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES)) 
         {
         switch(OrderType())
            {
            case OP_BUY       :OrderClose(OrderTicket(),OrderLots(),Bid,NULL,Red); break;
            case OP_SELL      :OrderClose(OrderTicket(),OrderLots(),Ask,NULL,Red); break;
            case OP_BUYLIMIT  :OrderDelete(OrderTicket()); break;
            case OP_SELLLIMIT :OrderDelete(OrderTicket()); break;
            case OP_BUYSTOP   :OrderDelete(OrderTicket()); break;
            case OP_SELLSTOP  :OrderDelete(OrderTicket()); break;
            default           :break;
            }  
            Buyvalue =0;
            S1=9999;
            S2=9999;
            S3=9999;
            S4=9999;
            S5=9999;
            Sellposition = 0;
            Sellvalue=0;
            B1 = 0;
            B1 = 0;
            B3 = 0;
            B4 = 0;
            B5 = 0;
            Buyposition = 0;
         }
      }
   }   
//+----------------------End Close Open Position---------------------+
//+------------------------------------------------------------------+
//| Calculate optimal lot size function                              |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   if (!MoneyManagement)return (Lots);
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
   if(lot<MinLot || UseMinilot==true) lot=MinLot;
   if(OrdersTotal()<1)
      {
      lot=lot;
      }
   if(OrdersTotal()==1)
      {
      lot=2*lot;
      }
   if(OrdersTotal()==2)
      {
      lot=3*lot;
      }
   if(OrdersTotal()==3 )
      {
      lot=4*lot;
      }
   if(OrdersTotal()==4)
      {
      lot=5*lot;
      }

   return(lot);
  }
//+-------------------End Calculate optimal lot size-----------------+
//+------------------------------------------------------------------+
//| Open Position function                                           |
//+------------------------------------------------------------------+
int Openbuy()
   {
   if ( MyStopLoss == 0) StopLoss = 0; 
   if ( MyTakeProfit == 0) TakeProfit = 0; 
   if ( MyStopLoss >0) StopLoss = Ask - MyStopLoss * Point;
   if ( MyTakeProfit >0) 
      {
      if(OrdersTotal()<1) TakeProfit = Ask + MyTakeProfit * Point;
      if(OrdersTotal()==1) TakeProfit = Ask + MyTakeProfit * Point + Nextopenposition * Point;
      if(OrdersTotal()==2) TakeProfit = Ask + MyTakeProfit * Point + Nextopenposition * 1.5 * Point;
      if(OrdersTotal()==3) TakeProfit = Ask + MyTakeProfit * Point + Nextopenposition * 2 * Point;
      if(OrdersTotal()==4) TakeProfit = Ask + MyTakeProfit * Point + Nextopenposition * 3 * Point;
      }           
   OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,Slippage,StopLoss,TakeProfit,NULL,MagicNumber,0,Green);
   Buyvalue=0;
   Signalbuyvalue=0;
   }
int Opensell()
   {  
   if ( MyStopLoss == 0) StopLoss = 0; 
   if ( MyStopLoss >0) StopLoss = Bid + MyStopLoss * Point;
   if ( MyTakeProfit == 0) TakeProfit = 0; 
   if ( MyTakeProfit >0) 
      {
      if(OrdersTotal()<1) TakeProfit = Bid - MyTakeProfit * Point;
      if(OrdersTotal()==1) TakeProfit = Bid - MyTakeProfit * Point - Nextopenposition * Point;
      if(OrdersTotal()==2) TakeProfit = Bid - MyTakeProfit * Point - Nextopenposition * 1.5 * Point;
      if(OrdersTotal()==3) TakeProfit = Bid - MyTakeProfit * Point - Nextopenposition * 2 * Point;
      if(OrdersTotal()==4) TakeProfit = Bid - MyTakeProfit * Point - Nextopenposition * 3* Point;
      }           
   OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,Slippage,StopLoss,TakeProfit,NULL,MagicNumber,0,Red);
   Sellvalue=0;
   Signalsellvalue=9999;
   }
//+------------------------------------------------------------------+
//| Print Trade Detail function                                      |
//+------------------------------------------------------------------+
void Printdetail()
   {
      comments = 
      "Account Balance:-->" + DoubleToStr(AccountBalance(),1)+
      "  |Account Equity:-->"  + DoubleToStr(AccountEquity(),1) + 
      "  |Account Margin:-->"  + DoubleToStr(AccountMargin(),1) +
      "  |Account Level:-->"  + DoubleToStr(AccountEquity()*100/(AccountMargin()+0.00000000000001),1) +
      "  |Free Margin:-->" + DoubleToStr(AccountFreeMargin(),1)  +
      "  |Floating Profit:-->" + DoubleToStr(AccountProfit(),1) +"\n"+
      "Spread:-->" + DoubleToStr((Ask-Bid)/Point,0) + " Point"+
      "  |Total Order:-->" + DoubleToStr(OrdersTotal(),0) +
      "  |Period is "+ Period() + 
      "  |Bas in chart: " + DoubleToStr(Bars,0)

      ;
      Comment(comments);
    
 }
//+------------------------------------------------------------------+
//| Trading history function                                         |
//+------------------------------------------------------------------+
   double LastTrade()
   {   
      for(int j=0;j<OrdersTotal()+1;j++)
        {
        if(OrderSelect(0,SELECT_BY_POS,MODE_TRADES)==false)
         {
         B1=0;
         S1=9999;
         }
        if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES)==true)
            { 
            if(OrderSymbol()==Symbol()) 
                {
                if(OrderType()==OP_BUY)
                   {
                      Sellcount=0;
                      Buycount=OrdersTotal();
                      S1=9999;
                      S2=9999;
                      S3=9999;
                      S4=9999;
                      S5=9999;
                      if ( OrderSelect(0,SELECT_BY_POS,MODE_TRADES)==true)  B1=OrderOpenPrice(); else B1=0;
                      if ( OrderSelect(1,SELECT_BY_POS,MODE_TRADES)==true)  B2=OrderOpenPrice(); else B2=0;
                      if ( OrderSelect(2,SELECT_BY_POS,MODE_TRADES)==true)  B3=OrderOpenPrice(); else B3=0;
                      if ( OrderSelect(3,SELECT_BY_POS,MODE_TRADES)==true)  B4=OrderOpenPrice(); else B4=0;
                      if ( OrderSelect(4,SELECT_BY_POS,MODE_TRADES)==true)  B5=OrderOpenPrice(); else B5=0;
                   }
                if(OrderType()==OP_SELL)
                   {
                      Buycount=0;
                      Sellcount=OrdersTotal();
                      B1=0;
                      B2=0;
                      B3=0;
                      B4=0;
                      B5=0;
                      if ( OrderSelect(0,SELECT_BY_POS,MODE_TRADES)==true)  S1=OrderOpenPrice(); else S1=9999;
                      if ( OrderSelect(1,SELECT_BY_POS,MODE_TRADES)==true)  S2=OrderOpenPrice(); else S2=9999;
                      if ( OrderSelect(2,SELECT_BY_POS,MODE_TRADES)==true)  S3=OrderOpenPrice(); else S3=9999;
                      if ( OrderSelect(3,SELECT_BY_POS,MODE_TRADES)==true)  S4=OrderOpenPrice(); else S4=9999;
                      if ( OrderSelect(4,SELECT_BY_POS,MODE_TRADES)==true)  S5=OrderOpenPrice(); else S5=9999;
                
                   }
                }
            }    
         }
      }      
//+-------------------------Exit trading history----------------------------------+
//+--------------------------------Exit Function----------------------------------+

