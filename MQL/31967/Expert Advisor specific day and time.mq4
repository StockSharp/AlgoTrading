//+------------------------------------------------------------------+
//|                         Expert Advisor specific day and time.mq4 |
//|                                                    Dwi Sudarsono |
//|                                        https://t.me/DwiSudarsono |
//+------------------------------------------------------------------+
#property copyright "Dwi Sudarsono"
#property link      "https://t.me/DwiSudarsono"
#property version   "1.00"
#property description "This Expert Advisor open and close orders at a specific day and time."
#property strict
//=========================================================================================================================================================================//
enum Select {Day_Of_Week, Day_Of_Year};
enum Type {Market_Orders, Pending_Stop, Pending_Limit};
enum MM {Manually_Lot, Automatically_Lot};
//=========================================================================================================================================================================//
extern string OpenOrdersSets    = "||========== Open Orders Sets ==========||";
extern datetime DayTimeOpen     = D'29.11.2021 00:00';//Day And Time Open Orders (YYYY.MM.DD HH:MM)
extern string OrdersParameters  = "||========== Orders' Parameters Sets ==========||";
extern Type   TypeOfOrders      = Market_Orders;//Type Of Orders Open
extern bool   OpenBuyType       = false;//Open Buy Orders
extern bool   OpenSellType      = false;//Open Sell Orders
extern double TakeProfit        = 10;//Orders' Take Profit (0=Not Add Take Profit)
extern double StopLoss          = 10;//Orders' Stop Loss (0=Not Add Stop Loss)
extern bool   TrailingStopLoss  = false;//Modify Stop Loss
extern double StepTrailing      = 1.0;//Step Modify Stop Loss
extern bool   BreakEvenRun      = false;//Use Break Even
extern double BreakEvenAfter    = 10.0;//Profit To Activate Break Even (Plus Stop Loss)
extern double OrderDistance     = 10.0;//Distance For Pending Orders
extern int    MinutesExpire     = 60;//Minutes Expiry Pending Orders (0=Without Expiry)
extern string RiskFactorSet     = "||========== Risk Factor Sets ==========||";
extern MM     TypeOfLotSize     = Manually_Lot;//Type Of Lot Size
extern double RiskFactor        = 1.0;//Risk Factro For Auto Lot
extern double ManualLotSize     = 0.01;//Manual Lot Size
extern string CloseOrdersSets   = "||========== Close Orders Sets ==========||";
extern datetime DayTimeClose    = D'29.11.2021 12:00';//Day And Time Close Orders (YYYY.MM.DD HH:MM)
extern bool   CloseOwnOrders    = false;//Close Only Own Orders In Time
extern bool   CloseAllOrders    = false;//Close All Orders In Time
extern bool   DeletePending     = true;//Delete Pending Orders
extern string HandleOrders      = "||========== Handle Orders Sets ==========||";
extern int    MagicNumber       = 12345;//Orders' Handle Magic Number
//=========================================================================================================================================================================//
string ExpertName;
int MultiplierPoint;
int i;
double DigitPoint;
double LotSize;
string BackgroundName;
color ChartColor;
int LastBarOpenBuy=-1;
int LastBarOpenSell=-1;
//=========================================================================================================================================================================//
int OnInit()
  {
//------------------------------------------------------
//Background
   BackgroundName="Background-"+WindowExpertName();
   ChartColor=(color)ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   if(ObjectFind(BackgroundName)==-1)
      ChartBackground(BackgroundName,ChartColor,0,15,180,145);
//------------------------------------------------------
//Broker 4 or 5 digits
   DigitPoint=MarketInfo(Symbol(),MODE_POINT);
   MultiplierPoint=1;
   if(MarketInfo(Symbol(),MODE_DIGITS)==3||MarketInfo(Symbol(),MODE_DIGITS)==5)
     {
      MultiplierPoint=10;
      DigitPoint*=MultiplierPoint;
     }
//------------------------------------------------------
//Minimum take profit and stop loss and distance for pendings
   double StopLevel=MathMax(MarketInfo(Symbol(),MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(Symbol(),MODE_STOPLEVEL)/MultiplierPoint);
   if((TakeProfit>0)&&(TakeProfit<StopLevel))
      TakeProfit=StopLevel;
   if((StopLoss>0)&&(StopLoss<StopLevel))
      StopLoss=StopLevel;
   if(OrderDistance<StopLevel)
      OrderDistance=StopLevel;
//------------------------------------------------------
   ExpertName=WindowExpertName();
//------------------------------------------------------
   OnTick();
//------------------------------------------------------
   return(INIT_SUCCEEDED);
//------------------------------------------------------
  }
//=========================================================================================================================================================================//
void OnDeinit(const int reason)
  {
//------------------------------------------------------
   ObjectDelete(BackgroundName);
   Comment("");
//------------------------------------------------------
  }
//=========================================================================================================================================================================//
void OnTick()
  {
//------------------------------------------------------
   datetime Expire=0;
   double FreeMargin=0;
   bool WasOrderClosed=false;
   bool WasOrderDeleted=false;
   bool WasOrderModify=false;
   int OpenOrderTicket;
   double DistAsk=0;
   double DistBid=0;
   double TP=0;
   double SL=0;
   bool CloseOrders=false;
//---------------------------------------------------------------------
//Set expiry time
   if(MinutesExpire>0)
      Expire=TimeCurrent()+(MinutesExpire*60);
//------------------------------------------------------
//Set levels
   double OrderTP=NormalizeDouble(TakeProfit*DigitPoint,Digits);
   double OrderSL=NormalizeDouble(StopLoss*DigitPoint,Digits);
   double OrderDist=NormalizeDouble(OrderDistance*DigitPoint,Digits);
   double PipsAfter=NormalizeDouble(BreakEvenAfter*DigitPoint,Digits);
   double TrailingStep=NormalizeDouble(StepTrailing*DigitPoint,Digits);
//------------------------------------------------------
//Set lot size
   if(TypeOfLotSize==0)
      LotSize=ManualLotSize;
   if(TypeOfLotSize==1)
      LotSize=(AccountBalance()/MarketInfo(Symbol(),MODE_LOTSIZE))*RiskFactor;
//------------------------------------------------------
//Close orders
   if(TimeToString(TimeCurrent(),TIME_DATE|TIME_MINUTES)==TimeToString(DayTimeClose,TIME_DATE|TIME_MINUTES))
     {
      for(i=OrdersTotal()-1; i>=0; i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            //---
            if((OrderSymbol()==Symbol())&&(OrderMagicNumber()==MagicNumber)&&(CloseOwnOrders==true))
               CloseOrders=true;
            if((OrderSymbol()==Symbol())&&(CloseAllOrders==true))
               CloseOrders=true;
            //---
            if(CloseOrders==true)
              {
               //---Close market orders
               if(TypeOfOrders==0)
                 {
                  //---Close buy
                  if(OrderType()==OP_BUY)
                    {
                     while(true)
                       {
                        WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),3,Yellow);
                        if(WasOrderClosed>0)
                          {
                           Print(ExpertName+": close buy order, ticket: "+IntegerToString(OrderTicket()));
                           break;
                          }
                        else
                          {
                           Print("Error: ",IntegerToString(GetLastError())+" || "+ExpertName+": receives new data and try again close order");
                           RefreshRates();
                          }
                       }
                    }
                  //---Close sell
                  if(OrderType()==OP_SELL)
                    {
                     while(true)
                       {
                        WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),3,Yellow);
                        if(WasOrderClosed>0)
                          {
                           Print(ExpertName+": close sell order, ticket: "+IntegerToString(OrderTicket()));
                           break;
                          }
                        else
                          {
                           Print("Error: ",IntegerToString(GetLastError())+" || "+ExpertName+": receives new data and try again close order");
                           RefreshRates();
                          }
                       }
                    }
                 }
               //---Delete pending orders
               if(TypeOfOrders>0)
                 {
                  if(OrderType()>=2)
                    {
                     while(true)
                       {
                        WasOrderDeleted=OrderDelete(OrderTicket(),clrNONE);
                        if(WasOrderDeleted>0)
                          {
                           Print(ExpertName+": delete pending order, ticket: "+IntegerToString(OrderTicket()));
                           break;
                          }
                        else
                          {
                           Print("Error: ",IntegerToString(GetLastError())+" || "+ExpertName+": receives new data and try again delete order");
                           RefreshRates();
                          }
                       }
                    }
                 }
               //---
              }
           }
        }
     }
//------------------------------------------------------
//Open orders
   if(TimeToString(TimeCurrent(),TIME_DATE|TIME_MINUTES)==TimeToString(DayTimeOpen,TIME_DATE|TIME_MINUTES))
     {
      //---Open market orders
      if(TypeOfOrders==0)
        {
         if((OpenBuyType==true)||(OpenSellType==true))
           {
            //---Open buy
            if((OpenBuyType==true)&&(isMgNum(MagicNumber,OP_BUY)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenBuy))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_BUY,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(Ask+OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(Bid-OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_BUY,NormalizeLot(LotSize),Ask,3,SL,TP,ExpertName,MagicNumber,0,clrBlue);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenBuy=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open buy order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            //---Open sell
            if((OpenSellType==true)&&(isMgNum(MagicNumber,OP_SELL)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenSell))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_SELL,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(Bid-OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(Ask+OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_SELL,NormalizeLot(LotSize),Bid,3,SL,TP,ExpertName,MagicNumber,0,clrRed);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenSell=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open sell order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            Sleep(1000);
           }
        }
      //---Open pending stop
      if(TypeOfOrders==1)
        {
         if((OpenBuyType==true)||(OpenSellType==true))
           {
            //---Open buy stop
            if((OpenBuyType==true)&&(isMgNum(MagicNumber,OP_BUYSTOP)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenBuy))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_BUY,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  DistAsk=NormalizeDouble(Ask+OrderDist,Digits);
                  DistBid=NormalizeDouble(Bid+OrderDist,Digits);
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(DistAsk+OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(DistBid-OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_BUYSTOP,NormalizeLot(LotSize),DistAsk,3,SL,TP,ExpertName,MagicNumber,Expire,clrBlue);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenBuy=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open buystop order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            //---Open sell stop
            if((OpenSellType==true)&&(isMgNum(MagicNumber,OP_SELLSTOP)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenSell))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_SELL,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  DistAsk=NormalizeDouble(Ask-OrderDist,Digits);
                  DistBid=NormalizeDouble(Bid-OrderDist,Digits);
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(DistBid-OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(DistAsk+OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_SELLSTOP,NormalizeLot(LotSize),DistBid,3,SL,TP,ExpertName,MagicNumber,Expire,clrRed);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenSell=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open sellstop order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            Sleep(1000);
           }
        }
      //---Open pending limit
      if(TypeOfOrders==2)
        {
         if((OpenBuyType==true)||(OpenSellType==true))
           {
            //---Open buy limit
            if((OpenBuyType==true)&&(isMgNum(MagicNumber,OP_BUYLIMIT)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenBuy))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_BUY,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  DistAsk=NormalizeDouble(Ask-OrderDist,Digits);
                  DistBid=NormalizeDouble(Bid-OrderDist,Digits);
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(DistAsk+OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(DistBid-OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_BUYLIMIT,NormalizeLot(LotSize),DistBid,3,SL,TP,ExpertName,MagicNumber,Expire,clrBlue);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenBuy=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open buylimit order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            //---Open sell limit
            if((OpenSellType==true)&&(isMgNum(MagicNumber,OP_SELLLIMIT)==0)&&(iBars(Symbol(),PERIOD_M1)!=LastBarOpenSell))
              {
               FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_SELL,LotSize);
               //---
               while(FreeMargin>=0)
                 {
                  TP=0;
                  SL=0;
                  //---
                  DistAsk=NormalizeDouble(Ask+OrderDist,Digits);
                  DistBid=NormalizeDouble(Bid+OrderDist,Digits);
                  //---
                  if(TakeProfit>0)
                     TP=NormalizeDouble(DistBid-OrderTP,Digits);
                  if(StopLoss>0)
                     SL=NormalizeDouble(DistAsk+OrderSL,Digits);
                  //---
                  OpenOrderTicket=OrderSend(Symbol(),OP_SELLLIMIT,NormalizeLot(LotSize),DistAsk,3,SL,TP,ExpertName,MagicNumber,Expire,clrRed);
                  //---
                  if(OpenOrderTicket>0)
                    {
                     LastBarOpenSell=iBars(Symbol(),PERIOD_M1);
                     Print(ExpertName+" M"+IntegerToString(Period())+" "+"open selllimit order");
                     break;
                    }
                  else
                    {
                     Print(ExpertName+": receives new data and try again open order");
                     Sleep(100);
                     RefreshRates();
                    }
                 }
              }
            Sleep(1000);
           }
        }
      //---
     }
//------------------------------------------------------
//Modify orders
   if((TrailingStopLoss==true)||(BreakEvenRun==true))
     {
      for(i=0; i<OrdersTotal(); i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
           {
            //---Modify sl with tsl with be
            if((TrailingStopLoss==true)&&(BreakEvenRun==true)&&(StopLoss>0))
              {
               //--Modify buy
               if((OrderType()==OP_BUY)&&(Bid-OrderStopLoss()>OrderSL+TrailingStep)&&(Bid-OrderOpenPrice()>=PipsAfter+OrderSL))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify buy 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
               //--Modify sell
               if((OrderType()==OP_SELL)&&(OrderStopLoss()-Ask>OrderSL+TrailingStep)&&(OrderOpenPrice()-Ask>=PipsAfter+OrderSL))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify sell 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
              }
            //---Modify sl with tsl without be
            if((TrailingStopLoss==true)&&(BreakEvenRun==false)&&(StopLoss>0))
              {
               //--Modify buy
               if((OrderType()==OP_BUY)&&(Bid-OrderStopLoss()>OrderSL+TrailingStep))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify buy 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
               //--Modify sell
               if((OrderType()==OP_SELL)&&(OrderStopLoss()-Ask>OrderSL+TrailingStep))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify sell 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
              }
            //---Modify sl without tsl with be
            if((TrailingStopLoss==false)&&(BreakEvenRun==true)&&(StopLoss>0))
              {
               //--Modify buy
               if((OrderType()==OP_BUY)&&(Bid-OrderOpenPrice()>=PipsAfter+OrderSL)&&(OrderStopLoss()<OrderOpenPrice()))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify buy 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
               //--Modify sell
               if((OrderType()==OP_SELL)&&(OrderOpenPrice()-Ask>=PipsAfter+OrderSL)&&(OrderStopLoss()>OrderOpenPrice()))
                 {
                  while(true)
                    {
                     WasOrderModify=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+OrderSL,Digits),OrderTakeProfit(),0,clrBlue);
                     //---
                     if(WasOrderModify>0)
                       {
                        Print(ExpertName+" M"+IntegerToString(Period())+" "+"modify sell 0rder");
                        break;
                       }
                     else
                       {
                        Print(ExpertName+": receives new data and try again modify order");
                        Sleep(100);
                        RefreshRates();
                       }
                    }
                 }
              }
           }
        }
     }
//------------------------------------------------------
   CommentScreen();
//------------------------------------------------------
  }
//=========================================================================================================================================================================//
double NormalizeLot(double LotsSize)
  {
//---------------------------------------------------------------------
   if(IsConnected())
     {
      return(MathMin(MathMax((MathRound(LotsSize/MarketInfo(Symbol(),MODE_LOTSTEP))*MarketInfo(Symbol(),MODE_LOTSTEP)),MarketInfo(Symbol(),MODE_MINLOT)),MarketInfo(Symbol(),MODE_MAXLOT)));
     }
   else
     {
      return(LotsSize);
     }
//---------------------------------------------------------------------
  }
//=========================================================================================================================================================================//
//Get magic number orders
int isMgNum(int Magic, int OpType)
  {
   for(int j=0; j<OrdersTotal(); j++)
     {
      if(OrderSelect(j,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==Magic)&&(OrderSymbol()==Symbol())&&(OrderType()==OpType))
            return(1);
        }
     }
   return(0);
  }
//=========================================================================================================================================================================//
void ChartBackground(string StringName,color ImageColor,int Xposition,int Yposition,int Xsize,int Ysize)
  {
//------------------------------------------------------
   if(ObjectFind(0,StringName)==-1)
     {
      ObjectCreate(0,StringName,OBJ_RECTANGLE_LABEL,0,0,0,0,0);
      ObjectSetInteger(0,StringName,OBJPROP_XDISTANCE,Xposition);
      ObjectSetInteger(0,StringName,OBJPROP_YDISTANCE,Yposition);
      ObjectSetInteger(0,StringName,OBJPROP_XSIZE,Xsize);
      ObjectSetInteger(0,StringName,OBJPROP_YSIZE,Ysize);
      ObjectSetInteger(0,StringName,OBJPROP_BGCOLOR,ImageColor);
      ObjectSetInteger(0,StringName,OBJPROP_BORDER_TYPE,BORDER_FLAT);
      ObjectSetInteger(0,StringName,OBJPROP_BORDER_COLOR,clrBlack);
      ObjectSetInteger(0,StringName,OBJPROP_BACK,false);
      ObjectSetInteger(0,StringName,OBJPROP_SELECTABLE,false);
      ObjectSetInteger(0,StringName,OBJPROP_SELECTED,false);
      ObjectSetInteger(0,StringName,OBJPROP_HIDDEN,true);
      ObjectSetInteger(0,StringName,OBJPROP_ZORDER,0);
     }
//------------------------------------------------------
  }
//=========================================================================================================================================================================//
void CommentScreen()
  {
//------------------------------------------------------
   string MMstring="";
   string StrOpenTime;
   string StrCloseTime;
   string StrActivateClose;
   string StrPositionsMode;
   string TypeOrdersStr;
//------------------------------------------------------
   StrOpenTime=TimeToString(DayTimeOpen,TIME_DATE|TIME_MINUTES);
   StrCloseTime=TimeToString(DayTimeClose,TIME_DATE|TIME_MINUTES);
//------------------------------------------------------
   if((CloseOwnOrders==false)&&(CloseAllOrders==false))
      StrActivateClose="Not Activate Close Orders";
   if(CloseOwnOrders==true)
      StrActivateClose="Close Own Orders";
   if(CloseAllOrders==true)
      StrActivateClose="Close All Orders";
   if((CloseOwnOrders==true)&&(CloseAllOrders==true))
      StrActivateClose="Please Check Settings";
//------------------------------------------------------
   if(TypeOfOrders==0)
      TypeOrdersStr="(Market)";
   if(TypeOfOrders==1)
      TypeOrdersStr="(Stop)";
   if(TypeOfOrders==2)
      TypeOrdersStr="(Limit)";
//------------------------------------------------------
   if((OpenBuyType==true)&&(OpenSellType==true))
      StrPositionsMode="Open Buy And Sell "+TypeOrdersStr;
   if((OpenBuyType==true)&&(OpenSellType==false))
      StrPositionsMode="Open Only Buy "+TypeOrdersStr;
   if((OpenBuyType==false)&&(OpenSellType==true))
      StrPositionsMode="Open Only Sell "+TypeOrdersStr;
   if((OpenBuyType==false)&&(OpenSellType==false))
      StrPositionsMode="Please Check Settings";
//------------------------------------------------------
//Comment in chart
   Comment("=========================","\n",
           WindowExpertName(),"\n",
           "=========================","\n",
           "Time Open Orders: ",StrOpenTime,"\n",
           "Time Close Orders: ",StrCloseTime,"\n",
           "=========================","\n",
           "Type Open: ",StrPositionsMode,"\n",
           "=========================","\n",
           "Type Close: ",StrActivateClose,"\n",
           "=========================","\n",
           "Orders' Lot Size: ",DoubleToStr(LotSize,2),"\n",
           "=========================");
//------------------------------------------------------
  }
//=========================================================================================================================================================================//

//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
