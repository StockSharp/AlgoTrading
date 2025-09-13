//+------------------------------------------------------------------+
//|                                          Aver4Sto+Postzigzag.mq4 |
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
double   Postzigzagvalue1=0;
double   Postzigzagvalue2=0;
double   Postzigzagvalue3=0;
double   Buyvalue = 0, Sellvalue = 0;
double   Signalbuyvalue = 0, Signalsellvalue = 0;
double   B1=0,B2=0,B3=0,B4=0,B5=0,S1=9999,S2=9999,S3=9999,S4=9999,S5=9999;
int      Buyposition = 0, Sellposition =0;
int      MagicNumber = 162429;
int      Slippage = 3;
double   StopLoss = 0;//inpip
double   TakeProfit = 0;//in pip
double   Maximum.Ft.Profit = 0;
double   Minimum.Ft.Profit = 0;
int      Buycount=0,Sellcount=0;
//---- input parameters
extern bool   MoneyManagement = true;
extern bool   UseMinilot=true;
extern double Lots=0.1;
extern double MaximumRisk=0.1;
extern double MinLot=0.1;
extern double MyStopLoss = 800;
extern double MyTakeProfit = 0;
extern double Stopoutlevel=50;
extern int    Nextopenposition =150;
extern double Delay = 50;//delay inpoint when signal appearent
double NextPosition=0;
extern bool   Closebyprofit = false;
extern int   Maxiposition=5;//Maximum=5
extern double Needprofit =500;//Maximum Profit per Order
extern double Losscan = 500; //Maximum Loss per Order
extern bool   TimeFrameM1=false;
extern bool   TimeFrameM5=true;//Best timeframe to trade
extern bool   TimeFrameM15=true;
extern bool   TimeFrameM30=false;
extern bool   TimeFrameM60=false;
extern bool   TimeFrameM240=false;

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
//| Close Open Position                                              |
//+------------------------------------------------------------------+
int Closebuy()
  { 
   for(int i=0; i<OrdersTotal(); i++)
     {
     OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
     if (OrderType()==OP_BUY && OrderSymbol()==Symbol())

        {
           OrderClose(OrderTicket(),OrderLots(),Bid,3,Yellow);
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
         }
      }
   }   
//+----------------------End Close Open Position---------------------+
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
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
//| Open Position                                                    |
//+------------------------------------------------------------------+
int Openbuy()
   {  
   if ( MyStopLoss == 0) StopLoss = 0; 
   if ( MyStopLoss >0) StopLoss = Ask - MyStopLoss * Point;
   if ( MyTakeProfit == 0) TakeProfit = 0; 
   if ( MyTakeProfit >0) TakeProfit = Ask + MyTakeProfit * Point;
   OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,Slippage,StopLoss,TakeProfit,NULL,MagicNumber,0,Green);
   Buyvalue=0;
   }
int Opensell()
   {  
   if ( MyStopLoss == 0) StopLoss = 0; 
   if ( MyStopLoss >0) StopLoss = Bid + MyStopLoss * Point;
   if ( MyTakeProfit == 0) TakeProfit = 0; 
   if ( MyTakeProfit >0) TakeProfit = Bid - MyTakeProfit * Point;
   OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,Slippage,StopLoss,TakeProfit,NULL,MagicNumber,0,Red);
   Sellvalue=0;
   }
//+----------------------End Open Trade Position---------------------+

//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
//+------------------------------------------------------------------+
//| indicator                                                        |
//+------------------------------------------------------------------+

   iStouchvalue1= iStochastic(NULL,0,26,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue2= iStochastic(NULL,0,72,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue3= iStochastic(NULL,0,144,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue4= iStochastic(NULL,0,288,3,3,MODE_SMMA,1,MODE_MAIN,1);    
   iStouchvalue= (iStouchvalue1+iStouchvalue2+iStouchvalue3+iStouchvalue4)/4;    
   Postzigzagvalue1=iCustom(NULL, 0, "Post-zigzag",14,1,1);
   Postzigzagvalue2=iCustom(NULL, 0, "Post-zigzag",14,2,1);
   Postzigzagvalue3=iCustom(NULL, 0, "Post-zigzag",14,3,1);

      if (iStouchvalue < 5.0 && Postzigzagvalue2!=0)  
         {
         Buyvalue=1;
         Sellvalue=0; 
         Signalbuyvalue=Ask;
         //PlaySound("zerozone.wav");
         //Alert("Signal Buy at " + Symbol());
         Print("Signal Buy at " + DoubleToStr(NormalizeDouble(Signalbuyvalue,Digits),Digits)+" : "+ Symbol());
         ObjectCreate("Trendup", OBJ_LABEL, 0, 0, 0);
         ObjectSetText("Trendup","UP",25, "Verdana", Lime);
         ObjectSet("Trendup", OBJPROP_CORNER, 0);
         ObjectSet("Trendup", OBJPROP_XDISTANCE, 39);
         ObjectSet("Trendup", OBJPROP_YDISTANCE, 46);
         ObjectDelete("Trend");

         }
      if (iStouchvalue > 95 && Postzigzagvalue1 !=0) 
         {
         Sellvalue=1;
         Buyvalue=0;
         Signalsellvalue=Bid;
         //PlaySound("hundered.wav");
         //Alert("Signal Sell at "+Symbol());
         Print("Signal Sell at " + DoubleToStr(NormalizeDouble(Signalsellvalue,Digits),Digits)+" : "+ Symbol());
         ObjectCreate("Trend", OBJ_LABEL, 0, 0, 0);
         ObjectSetText("Trend","DOWN",25, "Verdana", Red);
         ObjectSet("Trend", OBJPROP_CORNER, 0);
         ObjectSet("Trend", OBJPROP_XDISTANCE, 9);
         ObjectSet("Trend", OBJPROP_YDISTANCE, 46);
         ObjectDelete("Trendup");
         }
      if(Buyvalue==1)
         {
         if((Signalbuyvalue-Ask) > Delay*Point && Delay>0) Buyvalue=2;
            else Buyvalue=2;
         }   
      if(Sellvalue==1)
         {
         if((Bid - Signalsellvalue) > Delay*Point && Delay>0) Sellvalue=2;
            else Sellvalue=2;
         }   
         
//----Exit indicator

//+------------------------------------------------------------------+
//| Trading history                                                  |
//+------------------------------------------------------------------+
   for(int j=0;j<OrdersTotal();j++)
     {
     OrderSelect(j,SELECT_BY_POS,MODE_TRADES);
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
            Print("Buy---------------------------------");
            Buyposition = 1;
            NextPosition=Ask-Nextopenposition*Point;
            }
         if (Sellvalue ==2)
            {
            Opensell();
            Sellposition = 1;
            NextPosition=Bid+Nextopenposition*Point;
            }
         }            
      if (OrdersTotal() == 1 && Maxiposition > 1)
         {
         if((Ask - B1) < -Nextopenposition*Point && Buyposition == 1)
            {
            Openbuy();
            Buyposition = 2;
            NextPosition=Ask-Nextopenposition*1.5*Point;
            }
         if((Bid - S1) > Nextopenposition*Point && Sellposition == 1)
            {
            Opensell();
            Sellposition = 2;
            NextPosition=Bid+Nextopenposition*1.5*Point;
            }
         }            
      if (OrdersTotal() == 2 && Maxiposition > 2)
         {
         if((Ask - B2) < -Nextopenposition*1.5*Point && Buyposition == 2)
            {
            Openbuy();
            Buyposition = 3;
            NextPosition=Ask-Nextopenposition*2*Point;
            }
         if((Bid - S2) > Nextopenposition*1.5*Point && Sellposition == 2)
            {
            Opensell();
            Sellposition = 3;
            NextPosition=Bid+Nextopenposition*2*Point;
            }
         } 
      if (OrdersTotal() == 3 && Maxiposition > 3 && (AccountEquity()*100/(AccountMargin()+0.00000000000001)) >500)
         {
         if((Ask - B3) < -Nextopenposition*2*Point && Buyposition == 3)
            {
            Openbuy();
            Buyposition = 4;
            NextPosition=Ask-Nextopenposition*3*Point;
            }
         if((Bid - S3) > Nextopenposition*2*Point && Sellposition == 3)
            {
            Opensell();
            Sellposition = 4;
            NextPosition=Bid+Nextopenposition*3*Point;
            }
         } 
      if (OrdersTotal() == 4 && Maxiposition > 4 && (AccountEquity()*100/(AccountMargin()+0.00000000000001)) >500)
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
      }
//+--Exit open position----------------------------------------------+
//+------------------------------------------------------------------+
//| Close position by indicator                                      |
//+------------------------------------------------------------------+
   if((TimeFrameM1==true && Period()==1) || (TimeFrameM5==true  && Period()==5) || (TimeFrameM15==true  && Period()==15) || (TimeFrameM30==true  && Period()==30) || (TimeFrameM60==true && Period()==60)  || (TimeFrameM240==true && Period()==240) )
      {  
     if (Buyvalue >= 1)
         {
         Closesell();
         Buyvalue =0;
         S1=9999;
         S2=9999;
         S3=9999;
         S4=9999;
         S5=9999;
         Sellposition = 0;
         } 
           
      if (Sellvalue >= 1)
         {
         Closebuy();
         Sellvalue=0;
         B1 = 0;
         B2 = 0;
         B3 = 0;
         B4 = 0;
         B4 = 0;
         Buyposition = 0;
         }
      } 
//+------------------------------------------------------------------+
//| Close by profit optimized                                        |
//+------------------------------------------------------------------+
   if((AccountEquity()*100/(AccountMargin()+0.0000000000000001))<Stopoutlevel )
      {
      CloseAll();
      Sellvalue=0;
      B1 = 0;
      B2 = 0;
      B3 = 0;
      B3 = 0;
      B5 = 0;
      Buyposition = 0;
      Buyvalue =0;
      S1=9999;
      S2=9999;
      S3=9999;
      S4=9999;
      S5=9999;
      Sellposition = 0;
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
               Sellvalue=0;
               B1 = 0;
               B2 = 0;
               B2 = 0;
               B4 = 0;
               B5 = 0;
               Buyposition = 0;
               }
            if((OrderType()==OP_SELL && OrderProfit()>Needprofit)||(OrderType()==OP_SELL && OrderProfit()<-Losscan))
               {
               Closesell();
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

         if (AccountProfit() > Needprofit+1 || AccountProfit() < -Losscan-1)
            {
            CloseAll();
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
//+--Exit close position --------------------------------------------------+  
//+------------------------------------------------------------------+
//| Calculate Maximum Floating Profit                                |
//+------------------------------------------------------------------+
   if (Maximum.Ft.Profit < AccountProfit()) Maximum.Ft.Profit = AccountProfit();
      else Maximum.Ft.Profit = Maximum.Ft.Profit;   
   if (Minimum.Ft.Profit > AccountProfit()) Minimum.Ft.Profit = AccountProfit();
      else Minimum.Ft.Profit = Minimum.Ft.Profit;
//+-------------------Exit---------------------------------------+
//+------------------------------------------------------------------+
//| Print Trade Detail                                               |
//+------------------------------------------------------------------+
string comments="";
   comments = 
   "AccountBalance:-->" + DoubleToStr(AccountBalance(),1)+
   "  |AccountEquity:-->"  + DoubleToStr(AccountEquity(),1) +
   "  |Free Margin:-->" + DoubleToStr(AccountFreeMargin(),1) + 
   "  |Margin Level:-->" + DoubleToStr(AccountEquity()*100/(AccountMargin()+0.0000000000000001),2) + "%" +"\n"+
   "Floating Profit:-->" + DoubleToStr(AccountProfit(),1) +
   "  |Maxi Prof:-->" + DoubleToStr(Maximum.Ft.Profit,1) +
   "  |Maxi Loss:-->" + DoubleToStr(Minimum.Ft.Profit,1) +
   "  |Spread:-->" + DoubleToStr((Ask-Bid)*1000,0) + " Pip"+
   "  |Next Position:-->" + DoubleToStr(NextPosition,3) + "\n" +
   "Total Order:-->" + DoubleToStr(OrdersTotal(),0) +
   "  |Short position:-->" + DoubleToStr(Sellcount,0)+
   "  |Long position:-->" + DoubleToStr(Buycount,0)+
   "  |Maximum position:-->" + DoubleToStr(NormalizeDouble(AccountFreeMargin()/(AccountLeverage()+0.0000000000000001),1),0)+
   "  |Period is "+ Period() +
   "  |Needprofit:-->" + DoubleToStr(Needprofit,1) +"\n" +
   "     B1-------------B2------------B3-------------B4--------------B5-------------S1-------------S2-----------S3-------------S4------------S5" +"\n" +
   DoubleToStr(B1,5) + "|  |" + DoubleToStr(B2,5)+ "|  |" +DoubleToStr(B3,5)+ "|  |" +DoubleToStr(B4,5)+ "|  |" +DoubleToStr(B5,5)+ "|  |" +DoubleToStr(S1,5)+ "|  |" +DoubleToStr(S2,5)+ "|  |" +DoubleToStr(S3,5)+ "|  |" +DoubleToStr(S4,5)+ "|  |" +DoubleToStr(S5,5)
   ;
Comment(comments);   
//+--Exit start------------------------------------------------------------+
   return(0);
  }

