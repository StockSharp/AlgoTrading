//+------------------------------------------------------------------+
//|                                        starter_v6mod.mq4         |
//|                                        Copyright © 2005          |
//|    Thanks to Starter, Maloma, Amir, Fukinagashi, Forex_trader,   |
//|    kmrunner, and all other strategybuilderFx members that        |
//|    contributed to the success of this expert.                    |
//|    From MrPip                                                    |
//|    My contibution is clean up of code                            |
//|    10/13/05  added EMAAngleZero                                  |
//|              added Maloma logic for No buy when Sell open, etc   |
//|              removed Stop logic code uses TakeProfit instead     |
//|    10/15    Corrected EMAAngleZero for handling USDJPY           |
//|    10/18/05 Added Slippage as parameter to expert                |
//|    10/18/05 Added code for multiple tries to open trade          |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Strategybuilderfx members"
#property link      "http:/strategybuilderfx.com/"
#include <stdlib.mqh>
//----
extern int Debug=0;               // Change to 1 to allow print
//+---------------------------------------------------+
//|Account functions                                  |
//+---------------------------------------------------+
extern int AccountIsMini=0;       // Change to 1 if trading mini account
extern int LiveTrading=0;         // Change to 1 if trading live.
//+---------------------------------------------------+
//|Money Management                                   |
//+---------------------------------------------------+
extern int mm=1;                  // Change to 0 if you want to shutdown money management controls. Lots = 1 will be in effect and only 1 lot will be open regardless of equity.
extern double Riskpercent=5;      // Change to whatever percent of equity you wish to risk.
extern double DecreaseFactor=3;   // Used to decrease lot size after a loss is experienced to protect equity.  Recommended not to change.
extern double StopLoss=35;        // Maximum pips willing to lose per position.
extern double TrailingStop=0;     // Change to whatever number of pips you wish to trail your position with.
extern int MaximumLosses=3;       // Maximum number of losses per day willing to experience.
extern double Margincutoff=800;   // Expert will stop trading if equity level decreases to that level.
extern double Maxtrades=10;       // Total number of positions on all currency pairs. You can change this number as you wish.
//+---------------------------------------------------+
//|Profit controls                                    |
//+---------------------------------------------------+
extern int TakeProfit=10;         // Maximum profit level achieved.    
extern int Slippage=10;           // Possible fix for not getting filled or closed
//extern double Stop = 5;         // Minimum profit level achieved and usually achieved target.
//+---------------------------------------------------+
//|Indicator Variables                                |
//| Change these to try your own system               |
//| or add more if you like                           |
//+---------------------------------------------------+
extern int EMAPeriod=34;          //EMAAnglePeriod
extern double AngleTreshold=0.2;
extern int StartEMAShift=6;       // Check last six bars
extern int EndEMAShift=0;
extern double MAPeriod=120;       // Moving average period.
extern double MAPeriod2=40;       // Moving average period 2.
extern double Lots=1;             // standard lot size. 
extern int Turnon=1;              // Turns expert on, change to 0 and no trades will take place even if expert is enabled.
//+---------------------------------------------------+
//|General controls                                   |
//+---------------------------------------------------+
string OrderText="";
double lotMM;
int TradesInThisSymbol;
int cnt=0, total;
datetime LastTime;
double Sl;
double Tr;
int ticket;
int trstop=0;
bool est_b, est_s;                // determines if buy or sell trades are allowed
//+---------------------------------------------------+
//|  Indicator values for entry or exit conditions    |
//|  Add or Change to test your system                |
//+---------------------------------------------------+
double Laguerre;
double Laguerreprevious;
double Alpha;
double MA, MAprevious;
double MA2,MAprevious2;
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
//| The functions from this point to the start function are where    |
//| changes are made to test other systems or strategies.            |
//|+-----------------------------------------------------------------+
//******************************************************************** 
//+------------------------------------------------------------------+
//|                                                     EMAAngleZero |
//|                                                           jpkfox |
//|                                                                  |
//| You can use this indicator to measure when the EMA angle is      |
//| "near zero". AngleTreshold determines when the angle for the     |
//| EMA is "about zero": This is when the value is between           |
//| [-AngleTreshold, AngleTreshold] (or when the histogram is red).  |
//| EMAPeriod: EMA period                                            |
//| AngleTreshold: The angle value is "about zero" when it is        |
//| between the values [-AngleTreshold, AngleTreshold].              |      
//| StartEMAShift: The starting point to calculate the               |   
//| angle. This is a shift value to the left from the                |
//| observation point. Should be StartEMAShift > EndEMAShift.        | 
//| StartEMAShift: The ending point to calculate the                 |
//| angle. This is a shift value to the left from the                | 
//| observation point. Should be StartEMAShift > EndEMAShift.        |
//|                                                                  |
//| Modified by MrPip                                                |
//| Red for down                                                     |
//| Yellow for near zero                                             |
//| Green for up                                                     |   
//|                                                                  |
//| New mods for expert                                              |
//| returns -1 for downtrend                                         |
//| returns 1 for uptrend                                            |
//| returns 0 for do not trade                                       |
//| Modified to match code in indicator                              |
//+------------------------------------------------------------------+
int EMAAngleZeroCheck()
  {
   double fEndMA, fStartMA;
   double fAngle;
   double mFactor;
   int ShiftDif;
//----
   if(EndEMAShift>=StartEMAShift)
     {
      if (Debug==1) Print("Error: EndEMAShift >= StartEMAShift");
      StartEMAShift=6;
      EndEMAShift=0;
     }
   ShiftDif=StartEMAShift-EndEMAShift;
   mFactor=10000.0;
   if (Symbol()=="USDJPY") mFactor=100.0;
   mFactor/=ShiftDif;
   //
   fEndMA=iMA(NULL,0,EMAPeriod,0,MODE_EMA,PRICE_MEDIAN,EndEMAShift);
   fStartMA=iMA(NULL,0,EMAPeriod,0,MODE_EMA,PRICE_MEDIAN,StartEMAShift);
   // 10000.0 : Multiply by 10000 so that the fAngle is not too small
   // for the indicator Window.
   fAngle=mFactor * (fEndMA - fStartMA);
   // fAngle = MathArctan(fAngle)/0.01745;
   if(fAngle > AngleTreshold) return(1);
   if (fAngle < -AngleTreshold) return(-1);
   return(0);
  }
//+------------------------------------------------------------------+
//| LaGuerre function calculation                                    |
//+------------------------------------------------------------------+
double LaGuerre(double gamma, int shift)
  {
   double RSI;
   double L0[100];
   double L1[100];
   double L2[100];
   double L3[100];
   double CU, CD;
//----
   for(int i=shift+99; i>=shift; i--)
     {
      L0[i]=(1.0 - gamma)*Close[i] + gamma*L0[i+1];
      L1[i]=-gamma*L0[i] + L0[i+1] + gamma*L1[i+1];
      L2[i]=-gamma*L1[i] + L1[i+1] + gamma*L2[i+1];
      L3[i]=-gamma*L2[i] + L2[i+1] + gamma*L3[i+1];
      //
      CU=0;
      CD=0;
      if (L0[i]>=L1[i])  CU=L0[i] - L1[i];
      else              CD=L1[i] - L0[i];
//----
      if (L1[i]>=L2[i])  CU=CU + L1[i] - L2[i];
      else              CD=CD + L2[i] - L1[i];
      if (L2[i]>=L3[i])  CU=CU + L2[i] - L3[i];
      else              CD=CD + L3[i] - L2[i];
//----
      if (CU + CD!=0)      RSI=CU/(CU + CD);
     }
   return(RSI);
  }
//+------------------------------------------+
//| CheckExitCondition                       |
//|                                          |
//| Check if Leguerre has proper value       |
//| return 0 for exit condition not met      |
//| return 1 for exit condition met          |
//+------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CheckExitCondition(string type)
  {
   if (type=="BUY")
     {
      if (Laguerre > 0.9)
        {
         if (Debug==1) Print ("Exit Condition met for Buy");
         return(1);
        }
      return(0);
     }
   if (type=="SELL")
     {
      if (Laguerre < 0.1)
        {
         if (Debug==1) Print ("Exit Condition met for Sell");
//----
         return(1);
        }
     }
   return(0);
  }
//+------------------------------------------+
//| CheckBuyCondition                        |
//|                                          |
//| return 0 for exit condition not met      |
//| return 1 for exit condition met          |
//+------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CheckBuyCondition()
  {
   if((Laguerreprevious<=0) && (Laguerre<=0) && (MA>MAprevious) && (MA2>MAprevious2)&& (Alpha<-5))
     {
      if (Debug==1) Print ("Buy Condition met");
//----
      return(1);
     }
   return(0);
  }
//+------------------------------------------+
//| CheckSellCondition                       |
//|                                          |
//| return 0 for exit condiotion not met     |
//| return 1 for exit condition met          |
//+------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CheckSellCondition()
  {
   if((Laguerreprevious>=1) && (Laguerre>=1) && (MA<MAprevious) && (MA2<MAprevious2) && (Alpha>5))
     {
      if (Debug==1) Print ("Sell Condition met");
      return(1);
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   int donttrade, allexit;
   int ExitCondition, EMAAngle;
   //
   trstop=0;
   //
   int MagicNumber=3000 + func_Symbol2Val(Symbol())*100 + func_TimeFrame_Const2Val(Period());   
   string setup="sv6mod" + Symbol() + "_" + func_TimeFrame_Val2String(func_TimeFrame_Const2Val(Period()));
   //+------------------------------------------------------------------+
   //| Condition statements                                             |
   //| Change or add for your strategy                                  |
   //+------------------------------------------------------------------+
   Laguerre=LaGuerre(0.7, 0);
   Laguerreprevious=LaGuerre(0.7, 1);
   Alpha=iCCI(NULL, 0, 14, PRICE_CLOSE, 0);
   MA=iMA(NULL,0,MAPeriod,0,MODE_EMA,PRICE_MEDIAN,0);
   MAprevious=iMA(NULL,0,MAPeriod,0,MODE_EMA,PRICE_MEDIAN,1);
   MA2=iMA(NULL,0,MAPeriod2,0,MODE_EMA,PRICE_MEDIAN,0);
   MAprevious2=iMA(NULL,0,MAPeriod2,0,MODE_EMA,PRICE_MEDIAN,1);
   //
   donttrade=0;
   allexit=0;
   //+------------------------------------------------------------------+
   //| Friday Exits                                                     |
   //+------------------------------------------------------------------+
   if(DayOfWeek()==5 && Hour()>=18) donttrade=1;
   if(DayOfWeek()==5 && Hour()>=20) allexit=1;
   //+------------------------------------------------------------------+
   //| Open Position Controls                                           |
   //+------------------------------------------------------------------+
   CheckOpenPositions(MagicNumber, allexit);
   //+------------------------------------------------------------------+
   //| New Position Controls                                            |
   //+------------------------------------------------------------------+
     if(AccountFreeMargin() < Margincutoff) 
     {
     return(0);
     }
     if(TradesInThisSymbol > Maxtrades) 
     {
     return(0);
     }
     if(CurTime() < LastTime) 
     {
     return(0);
     }
   // Moved after open positions are closed for more available margin
   if(mm==1)
     {
      lotMM=LotsOptimized(MagicNumber);
     }
     else 
     {
      lotMM=Lots; // Change mm to 0 if you want the Lots parameter to be in effect
     }
   OrderText=""; //Must be blank before going into the main section
   // Check for open positions
   CheckBuySellPositions();  // returns est_b and est_s
   // Check for good angle on EMA making sure no open positions
   // in opposite direction
   EMAAngle=EMAAngleZeroCheck();
   if (EMAAngle==1)             // uptrend
     {
      // Make sure No Sells open
      if (est_b)
        {
         est_b=true;
         est_s=false;
        }
     }
   if (EMAAngle==-1)            // downtrend
     {
      // Make sure no Buys open
      if (est_s)
        {
         est_b=false;
         est_s=true;
        }
     }
   if (EMAAngle==0)             // market is flat
     {
      est_b=false;
      est_s=false;
     }
   if((CheckBuyCondition()==1) && (Turnon==1) && (donttrade==0) && est_b)
     {
      OrderText="BUY";
        if (StopLoss>0) 
        {
         Sl=Ask-StopLoss*Point;
         }
          else 
         {
         Sl=0;
        }
      if (TakeProfit==0)
         Tr=0;
      else
         Tr=Ask+TakeProfit*Point;
     }
   if((CheckSellCondition()==1) && (Turnon==1) && (donttrade==0) && est_s)
     {
      OrderText="SELL";
        if (StopLoss>0) 
        {
         Sl=Bid+StopLoss*Point;
         }
          else 
         {
         Sl=0;
        }
      if (TakeProfit==0)
         Tr=0;
      else
         Tr=Bid-TakeProfit*Point;
     }
   if(OrderText!="" && trstop==0 && TradesInThisSymbol==0)
     {
      LastTime=CurTime();
      DoTrades(setup,MagicNumber);
      return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Check Open Buy or Sell Position                                  |
//| Code from Maloma                                                 |
//| modifies globals est_s and est_b                                 |
//+------------------------------------------------------------------+
int CheckBuySellPositions()
  {
   int total,cnt;
   //
   total=OrdersTotal();
   est_s=true;
   est_b=true;
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect (cnt, SELECT_BY_POS, MODE_TRADES );
      if((OrderType()==OP_SELL) && (OrderSymbol()==Symbol())) est_b=false;
      if((OrderType()==OP_BUY) && (OrderSymbol()==Symbol())) est_s=false;
     }
   return(0);
  }
//+--------------------------------------------------------+
//|  HandleBuys uses exit condition to determine close     |
//+--------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int HandleBuys(int ExitConditions)
  {
   if (ExitConditions==1)
     {
      OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Violet); // close position
      return(0); // exit
     }
   // check for stop
   //   if(Stop>0)  
   //   {                 
   //     if(Bid-OrderOpenPrice()>=Point*Stop)
   //     {
   //        OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Violet); // close position
   //        return(0);
   //      }
   //    }
   if(TrailingStop>0)
     {
      if(Bid-OrderOpenPrice()>Point*TrailingStop)
        {
         if(OrderStopLoss()<Bid-Point*TrailingStop)
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Aqua);
            return(0);
           }
        }
     }
  }
//+--------------------------------------------------------+
//|  HandleSells uses exit condition to determine close    |
//+--------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int HandleSells(int ExitConditions)
  {
   if (ExitConditions==1)
     {
      OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Violet); // close position
      return(0); // exit
     }
   // check for stop
   //   if(Stop>0)  
   //   {                 
   //     if(OrderOpenPrice()-Ask>=Point*Stop)
   //     {
   //        OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Violet); // close position
   //        return(0);
   //     }
   //   }
   if(TrailingStop>0)
     {
      if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
        {
         if(OrderStopLoss()==0.0 || OrderStopLoss()>(Ask+Point*TrailingStop))
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Aqua);
            return(0);
           }
        }
     }
  }
//+-------------------------------------------+
//| DoTrades module cut from start            |
//|  No real changes                          |
//+-------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int DoTrades(string SetupStr,int MagicNum)
  {
   double Min_OrderPrice;
   int err;
   if(OrderText=="BUY")
     {
      Min_OrderPrice=MinOrderPrice(OP_BUY, MagicNum);
        if (Min_OrderPrice>0 && Min_OrderPrice<=Ask*1.05) 
        {
         Print("Buy too expensive => MinOrderPrice= " + Min_OrderPrice + "  Ask=" + Ask);
         }
          else 
         {
         ticket=OrderSend(Symbol(),OP_BUY,lotMM,Ask,Slippage,Sl,Tr,SetupStr,MagicNum,0,Green);
         if (Debug==1) Print ("Buy at ",TimeToStr(CurTime())," for ",Ask);
         LastTime+=12;
           if(ticket<=0) 
           {
            err=GetLastError();
            Alert("Error opening BUY order [" + SetupStr + "]: (" + err + ") " + ErrorDescription(err));
           }
         return(0);
        }
     }
   else if(OrderText=="SELL")
        {
         Min_OrderPrice=MinOrderPrice(OP_SELL, MagicNum);
           if (Min_OrderPrice>0 && Min_OrderPrice<=Bid) 
           {
            Print("Buy too expensive MinOrderPrice= " + Min_OrderPrice + "  Bid=" + Bid);
            }
             else 
            {
            ticket=OrderSend(Symbol(),OP_SELL,lotMM,Bid,Slippage,Sl,Tr,SetupStr,MagicNum,0,Red);
            if (Debug==1) Print ("Sell at ",TimeToStr(CurTime())," for ",Bid);
            LastTime+=12;
              if(ticket<=0) 
              {
               err=GetLastError();
               Alert("Error opening Sell order [" + SetupStr + "]: (" + err + ") " + ErrorDescription(err));
              }
            return(0);
           }
        }
   return(0);
  }
//+------------------------------------------------------------------+
//| Check Open Position Controls                                     |
//+------------------------------------------------------------------+
int CheckOpenPositions(int MagicNumbers, int allexits)
  {
   int cnt, ExitCondition, total;
   //
   total=OrdersTotal();
   TradesInThisSymbol=0;
   for(cnt=0;cnt<total;cnt++)
     {
      if(OrderSelect (cnt, SELECT_BY_POS)==false) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=MagicNumbers)  continue;
      if((OrderType()==OP_BUY || OrderType()==OP_BUYSTOP) && (OrderSymbol()==Symbol()))
        {
         TradesInThisSymbol++;
         //   First check if indicators cause exit
         ExitCondition=CheckExitCondition("BUY");
         // Then check if Friday
         if(allexits==1) ExitCondition=1;
         HandleBuys(ExitCondition);
        }
      if((OrderType()==OP_SELL || OrderType()==OP_SELLSTOP) && (OrderSymbol()==Symbol()))
        {
         TradesInThisSymbol++;
         //   First check if indicators cause exit
         ExitCondition=CheckExitCondition("SELL");
         // Then check if Friday
         if(allexits==1) ExitCondition=1;
         HandleSells(ExitCondition);
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized(int Mnr)
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
   int    tolosses=0;
//---- select lot size
   lot=NormalizeDouble(MathFloor(AccountBalance()*Riskpercent/10000)/10,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL || OrderMagicNumber()!=Mnr) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      for(i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(TimeDay(OrderCloseTime())!=TimeDay(CurTime())) continue;
         //----
         if(OrderProfit()<0) tolosses++;
        }
      if (tolosses>=MaximumLosses) trstop=1;
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
   if(lot > 1)
      lot=MathFloor(lot);
   if(AccountIsMini==1)
      lot=lot * 10;
//---- return lot size
   if(lot<0.1) lot=0.1;
   if(LiveTrading==1)
     {
      if (AccountIsMini==0 && lot < 1.0)
         lot=1.0;
     }
   if(lot > 100)
      lot=100;
//----
   return(lot);
  }
//+------------------------------------------------------------------+
//| Time frame interval appropriation  function                               |
//+------------------------------------------------------------------+
  int func_TimeFrame_Const2Val(int Constant)
  {
     switch(Constant) 
     {
         case 1:  // M1
            return(1);
         case 5:  // M5
            return(2);
         case 15:
            return(3);
         case 30:
            return(4);
         case 60:
            return(5);
         case 240:
            return(6);
         case 1440:
            return(7);
         case 10080:
            return(8);
         case 43200:
            return(9);
        }
     }
         //+------------------------------------------------------------------+
         //| Time frame string appropriation  function                        |
         //+------------------------------------------------------------------+
           string func_TimeFrame_Val2String(int Value)
           {
              switch(Value) 
              {
                  case 1:  // M1
                     return("PERIOD_M1");
                  case 2:  // M1
                     return("PERIOD_M5");
                  case 3:
                     return("PERIOD_M15");
                  case 4:
                     return("PERIOD_M30");
                  case 5:
                     return("PERIOD_H1");
                  case 6:
                     return("PERIOD_H4");
                  case 7:
                     return("PERIOD_D1");
                  case 8:
                     return("PERIOD_W1");
                  case 9:
                     return("PERIOD_MN1");
                  default:
                     return("undefined " + Value);
                 }
              }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
                    int func_Symbol2Val(string symbol) 
                    {
                       if(symbol=="USDCHF") 
                       {
                        return(1);
                        }
 else if(symbol=="GBPUSD") 
                        {
                           return(2);
                           }
                            else if(symbol=="EURUSD") 
                           {
                           return(3);
                           }
                            else if(symbol=="USDJPY") 
                           {
                           return(4);
                           }
                            else if(symbol=="EURGBP") 
                           {
                           return(5);
                           }
                            else if(symbol=="EURJPY") 
                           {
                           return(6);
                           }
                            else 
                           {
                        Comment("unexpected Symbol");
                       }
                    }
                  //+------------------------------------------------------------------+
                  //| Average price efficiency  function                               |
                  //+------------------------------------------------------------------+
                    double MinOrderPrice(int OType, int OMagicNumber) 
                    {
                     double MinPrice;
                       if (OrderType()==OP_BUY) 
                       {
                        MinPrice=1000000;
                        }
                         else 
                        {
                        MinPrice=0;
                       }
                     for(int cnt=0;cnt<OrdersTotal();cnt++)
                       {
                        OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
                          if(OrderType()==OType && OrderSymbol()==Symbol() && OrderMagicNumber()==OMagicNumber) 
                          {
                             if (OrderType()==OP_BUY) 
                             {
                                if (OrderOpenPrice()<MinPrice) 
                                {
                                 MinPrice=OrderOpenPrice();
                                }
                              }
                               else 
                              {
                                if (OrderOpenPrice()>MinPrice) 
                                {
                                 MinPrice=OrderOpenPrice();
                                }
                             }
                          }
                       }
                     if (MinPrice==1000000) MinPrice=0;
                     return(MinPrice);
                    }
//+------------------------------------------------------------------+