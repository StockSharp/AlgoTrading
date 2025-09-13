//====================================================================================================================================================//
// email: nikolaospantzos@gmail.com                                                                                                      FollowLineEA //
//====================================================================================================================================================//
#property link      "https://www.mql5.com/en/users/pannik"
#property copyright "2019 by Pantzos Nikolaos"
#property version   "1.0"
#property strict
//===================================================================================================================================================//
enum Arrows{Hide_Arrows,Simple_Arrows,Open_Cose_Median,High_Low_Open_Close};
//====================================================================================================================================================//
extern string IndicatorsSets    = "==== Set Indicator ====";
extern string IndicatorName     ="FollowLine_v1.5";
extern int    BarsCount         = 10;
extern int    BBperiod          = 21;
extern double BBdeviations      = 1;
extern int    MAperiod          = 21;
extern int    ATRperiod         = 5;
extern bool   UseATRfilter      = false;
extern bool   AlertON           = false;
extern Arrows TypeOfArrows      = Simple_Arrows;
extern int    IndicatorsShift   = 1;
extern string SetCloseOrders    = "==== Set Close Orders ====";
extern bool   CloseInSignal     = false;
extern bool   UseBasketClose    = false;
extern bool   CloseInProfit     = false;
extern double PipsCloseProfit   = 10.0;
extern bool   CloseInLoss       = false;
extern double PipsCloseLoss     = 100.0;
extern string SetOrders         = "==== Set Orders Parametre ====";
extern bool   UseTakeProfit     = true;
extern double TakeProfit        = 10.0;
extern bool   UseStopLoss       = true;
extern double StopLoss          = 50.0;
extern bool   UseTrailingStop   = false;
extern double TrailingStop      = 5;
extern double TrailingStep      = 1;
extern bool   UseBreakEven      = false;
extern double BreakEven         = 4;
extern double BreakEvenAfter    = 2;
extern string Money_Management  = "==== Money Management ====";
extern bool   AutoLotSize       = true;
extern double RiskFactor        = 1.0;
extern double ManualLotSize     = 0.01;
extern string TimeFilter        = "==== Time Filter ====";
extern bool   UseTimeFilter     = false;
extern int    TimeStartTrade    = 0;
extern int    TimeEndTrade      = 0;
extern string SetGeneral        = "==== General Set ====";
extern string MaxSpreadInfo     = "If MaxSpread=0 not check spread";
extern double MaxSpread         = 0.0;
extern string MaxOrdersInfo     = "If MaxOrders=0 there is not limit";
extern int    MaxOrders         = 0;
extern int    Slippage          = 3;
extern bool   RunNDDbroker      = false;
extern bool   SoundAlert        = true;
extern string MagicNumberInfo   = "if MagicNumber = 0, expert generate automatical MagicNumber";
extern int    MagicNumber       = 0;
extern string CommentsOrders    = "FollowLineEA";
//====================================================================================================================================================//
string SoundFileAtClose="alert2.wav";
string SoundFileAtOpen="alert.wav";
string SoundModify="tick.wav";
string ExpertName;
string SymbolName;
string OperationInfo;
string Suffix="";
double DigitPoints;
double StopLevel;
double Spread;
double TotalHistoryProfitLoss;
double PipsBuyOrders;
double PipsSellOrders;
double PipsLastBuyOrders;
double PipsLastSellOrders;
double ProfitBuyOrders;
double ProfitSellOrders;
double SumFloating;
int OrdersID;
int TotalHistoryOrders;
int HistoryBuy;
int HistorySell;
int MultiplierPoint;
int OrdersOpened;
int SumOrders;
int TypeLastOrder;
int BuyOrders;
int SellOrders;
int BarOpenBuy=0;
int BarOpenSell=0;
int i;
bool CheckSpread;
bool TimeToTrade;
bool OpenBuy=false;
bool OpenSell=false;
bool CloseBuy=false;
bool CloseSell=false;
datetime GetStartTime;
datetime LastTimeBar=0;
color ChartColor;
//====================================================================================================================================================//
//OnInit function
//====================================================================================================================================================//
int OnInit()
  {
//------------------------------------------------------
//Started information
   ExpertName=WindowExpertName();
   SymbolName=Symbol();
   GetStartTime=TimeCurrent();
   if(StringLen(SymbolName)>6) Suffix=StringSubstr(SymbolName,6,0);
//------------------------------------------------------
//Background
   ChartColor=(color)ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   if(ObjectFind("Background")==-1) ChartBackground("Background",ChartColor,0,15,220,170);
//------------------------------------------------------
//Set ID
   OrdersID=MagicNumber;
   if(MagicNumber==0)
     {
      OrdersID=0;
      for(i=0; i<StringLen(SymbolName); i++) OrdersID+=(StringGetChar(SymbolName,i)*(i+1));
      for(i=0; i<StringLen(ExpertName); i++) OrdersID+=(StringGetChar(ExpertName,i)*(i+1));
      OrdersID+=2233;
     }
//------------------------------------------------------
//Broker 4 or 5 digits
   DigitPoints=MarketInfo(SymbolName,MODE_POINT);
   MultiplierPoint=1;
   if(MarketInfo(SymbolName,MODE_DIGITS)==3 || MarketInfo(SymbolName,MODE_DIGITS)==5)
     {
      MultiplierPoint=10;
      DigitPoints*=MultiplierPoint;
     }
//------------------------------------------------------
//Minimum trailing, take profit and stop loss
   StopLevel=MathMax(MarketInfo(SymbolName,MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(SymbolName,MODE_STOPLEVEL)/MultiplierPoint);
   if((TrailingStop>0) && (TrailingStop<StopLevel)) TrailingStop=StopLevel;
   if((BreakEven>0) && (BreakEven<StopLevel)) BreakEven=StopLevel;
   if((TakeProfit>0) && (TakeProfit<StopLevel)) TakeProfit=StopLevel;
   if((StopLoss>0) && (StopLoss<StopLevel)) StopLoss=StopLevel;
   if(RiskFactor<1) RiskFactor=1;
   if(RiskFactor>100) RiskFactor=100;
//---------------------------------------------------------------------
//Working check
   OperationInfo=ExpertName+"   Working well....";
//------------------------------------------------------
   if(!IsTesting()) OnTick();//For show comment if market is closed
//------------------------------------------------------
   return(INIT_SUCCEEDED);
  }
//====================================================================================================================================================//
//OnDeinit function
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
   ObjectDelete("Background");
   Comment("");
  }
//====================================================================================================================================================//
//OnTick function
//====================================================================================================================================================//
void OnTick()
  {
//------------------------------------------------------
//Check for history and trading
   if(iBars(SymbolName,0)<10)
     {
      Print("Missing bars..........!!!");
      if(!IsTesting()) Comment("\n  Please Wait to Update Bars ....");
      CommentScreen();
      return;
     }
//---------------------------------------------------------------------
//Closed market
   if((TimeCurrent()==GetStartTime) && (!IsTesting()) && (!IsOptimization()))
     {
      Print("Market is closed..........!!!");
      CommentScreen();
      return;
     }
//---------------------------------------------------------------------
//Call main fucntion
   MainFunction();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//OnTick function
//====================================================================================================================================================//
void MainFunction()
  {
   CheckSpread=true;
   OrdersOpened=OrdersTotal();
   TimeToTrade=true;
   OpenBuy=false;
   OpenSell=false;
   CloseBuy=false;
   CloseSell=false;
//------------------------------------------------------
//Check time to trade
   if(UseTimeFilter==true)
     {
      if((TimeStartTrade<TimeEndTrade) && ((TimeHour(TimeCurrent())<TimeStartTrade) || (TimeHour(TimeCurrent())>=TimeEndTrade))) TimeToTrade=false;
      else
         if((TimeStartTrade>TimeEndTrade) && ((TimeHour(TimeCurrent())<TimeStartTrade) && (TimeHour(TimeCurrent())>=TimeEndTrade))) TimeToTrade=false;
     }
//------------------------------------------------------
//Market spread
   Spread=(Ask-Bid)/DigitPoints;
//------------------------------------------------------
//Check spread
   if((Spread>MaxSpread) && (MaxSpread>0))
     {
      CheckSpread=false;
      Print("Spread is greater than MaxSpread!!! (Spread: "+DoubleToStr(Spread,1)+" || MaxSpread: "+DoubleToStr(MaxSpread,1)+")");
     }
//------------------------------------------------------
//Count orders
   CountOrders(OrdersID);
//------------------------------------------------------
//Close orders
   if(SumOrders>0)
     {
      if(CloseInProfit==true)
        {
         if(UseBasketClose==false)
           {
            if((BuyOrders>0) && (PipsBuyOrders>=(PipsCloseProfit*BuyOrders))) {CloseOrders(OP_BUY); return;}
            if((SellOrders>0) && (PipsSellOrders>=(PipsCloseProfit*SellOrders))) {CloseOrders(OP_SELL); return;}
           }
         if(UseBasketClose==true)
           {
            if((BuyOrders+SellOrders>0) && (PipsBuyOrders+PipsSellOrders>=MathMax((PipsCloseProfit*BuyOrders),(PipsCloseProfit*SellOrders))))
              {
               CloseOrders(OP_BUY);
               CloseOrders(OP_SELL);
               return;
              }
           }
        }
      //---
      if(CloseInLoss==true)
        {
         if(UseBasketClose==false)
           {
            if((BuyOrders>0) && (PipsBuyOrders<=-(PipsCloseLoss*BuyOrders))) {CloseOrders(OP_BUY); return;}
            if((SellOrders>0) && (PipsSellOrders<=-(PipsCloseLoss*SellOrders))) {CloseOrders(OP_SELL); return;}
           }
         if(UseBasketClose==true)
           {
            if((BuyOrders+SellOrders>0) && (PipsBuyOrders+PipsSellOrders<=-MathMax((PipsCloseLoss*BuyOrders),(PipsCloseLoss*SellOrders))))
              {
               CloseOrders(OP_BUY);
               CloseOrders(OP_SELL);
               return;
              }
           }
        }
      //------------------------------------------------------
      //Call modify and close orders functions
      if((UseTrailingStop==true) || (UseBreakEven==true)) ModifyOrders();
      //---
      if(CloseInSignal==true)
        {
         GetSignals();
         //---
         if((BuyOrders>0) && (CloseBuy==true)) CloseOrders(OP_BUY);
         if((SellOrders>0)&&(CloseSell==true)) CloseOrders(OP_SELL);
        }
     }
//------------------------------------------------------
//Open orders
   if((CheckSpread==true) && (TimeToTrade==true) && ((SumOrders<MaxOrders) || (MaxOrders==0)))
     {
      GetSignals();
      //---Check for buy
      if((OpenBuy==true) && (iBars(SymbolName,0)!=BarOpenBuy) && (BuyOrders==0))
        {
         BarOpenBuy=iBars(SymbolName,0);
         OpenPosition(OP_BUY);
        }
      //---Check for sell
      if((OpenSell==true) && (iBars(SymbolName,0)!=BarOpenSell) && (SellOrders==0))
        {
         BarOpenSell=iBars(SymbolName,0);
         OpenPosition(OP_SELL);
        }
     }
//------------------------------------------------------
//Call comment function every tick
   if(!IsTesting())
     {
      HistoryResults();
      CommentScreen();
     }
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Open orders
//====================================================================================================================================================//
void OpenPosition(int PositionType)
  {
   int OpenOrderTicket=0;
   bool WasOrderModified;
   double OpenPrice=0;
   color OpenColor=clrNONE;
   string TypeOfOrder;
   double OrdrLotSize=CalcLots();
   double CheckMargin=0;
//------------------------------------------------------
//Calculate take profit and stop loss in pips
   double TP=0;
   double SL=0;
   double OrderTP=NormalizeDouble(TakeProfit*DigitPoints,Digits);
   double OrderSL=NormalizeDouble(StopLoss*DigitPoints,Digits);
   double TrailingSL=NormalizeDouble(TrailingStop*DigitPoints,Digits);
//------------------------------------------------------
//Calculate free margin base lot from open orders
   if(OrdrLotSize!=0) CheckMargin=AccountFreeMarginCheck(SymbolName,PositionType,OrdrLotSize);
   if(CheckMargin<=0)
     {
      Print("<NOTICE...[ "+ExpertName+": Free margin is low ("+DoubleToStr(CheckMargin,2)+") ]...NOTICE>");
      Comment("\n\nFree margin is low ("+DoubleToStr(CheckMargin,2)+")");
      return;
     }
//------------------------------------------------------
   while(true)
     {
      //------------------------------------------------------
      //Buy stop loss and take profit in price
      if(PositionType==OP_BUY)
        {
         TP=0;
         SL=0;
         OpenPrice=NormalizeDouble(Ask,Digits);
         OpenColor=clrBlue;
         if((TakeProfit>0) && (UseTakeProfit==true)) TP=NormalizeDouble(Ask+OrderTP,Digits);
         if((StopLoss>0) && (UseStopLoss==true)) SL=NormalizeDouble(Bid-OrderSL,Digits);
         //if((StopLoss>0)&&(UseStopLoss==true)) SL=NormalizeDouble(MathMin(iLow(NULL,0,1),Bid)-OrderSL,Digits);
         if((TrailingStop>0) && (UseStopLoss==false) && (UseTrailingStop==true) && (SL==0)) SL=NormalizeDouble(Bid-TrailingSL,Digits);
         TypeOfOrder="Buy";
        }
      //------------------------------------------------------
      //Sell stop loss and take profit in price
      if(PositionType==OP_SELL)
        {
         TP=0;
         SL=0;
         OpenPrice=NormalizeDouble(Bid,Digits);
         OpenColor=clrRed;
         if((TakeProfit>0) && (UseTakeProfit==true)) TP=NormalizeDouble(Bid-OrderTP,Digits);
         if((StopLoss>0) && (UseStopLoss==true)) SL=NormalizeDouble(Ask+OrderSL,Digits);
         //if((StopLoss>0)&&(UseStopLoss==true)) SL=NormalizeDouble(MathMax(iHigh(NULL,0,1),Ask)+OrderSL,Digits);
         if((TrailingStop>0) && (UseStopLoss==false) && (UseTrailingStop==true) && (SL==0)) SL=NormalizeDouble(Ask+TrailingSL,Digits);
         TypeOfOrder="Sell";
        }
      //------------------------------------------------------
      //NDD broker, no sl no tp
      if(RunNDDbroker==true)
        {
         TP=0;
         SL=0;
        }
      //------------------------------------------------------
      //Send orders
      OpenOrderTicket=OrderSend(SymbolName,PositionType,OrdrLotSize,OpenPrice,Slippage,SL,TP,CommentsOrders,OrdersID,0,OpenColor);
      //---
      if(OpenOrderTicket>0)
        {
         if(SoundAlert==true) PlaySound(SoundFileAtOpen);
         Print(ExpertName+" M"+DoubleToStr(Period(),0)+" "+TypeOfOrder);
         break;
        }
      else
        {
         Print(ExpertName+": receives new data and try again open order");
         Sleep(100);
         RefreshRates();
        }
      //---
     }//End while(true)
//------------------------------------------------------
//NDD send stop loss and take profit
   if((RunNDDbroker==true) && (OpenOrderTicket>0) && ((UseTakeProfit==true) || (UseStopLoss==true) || (UseTrailingStop==true)))
     {
      if(OrderSelect(OpenOrderTicket,SELECT_BY_TICKET))
        {
         //------------------------------------------------------
         //Modify stop loss and take profit buy order
         if((OrderType()==OP_BUY) && (OrderStopLoss()==0) && (OrderTakeProfit()==0))
           {
            while(true)
              {
               TP=0;
               SL=0;
               if((TakeProfit>0) && (UseTakeProfit==true)) TP=NormalizeDouble(Ask+OrderTP,Digits);
               if((StopLoss>0) && (UseStopLoss==true)) SL=NormalizeDouble(Bid-OrderSL,Digits);
               if((TrailingStop>0) && (UseStopLoss==false) && (UseTrailingStop==true)) SL=NormalizeDouble(Bid-TrailingStop,Digits);
               //---
               if((TP==0) && (SL==0)) break;
               //---
               WasOrderModified=OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),Digits),SL,TP,0,clrBlue);
               //---
               if(WasOrderModified>0)
                 {
                  if(SoundAlert==true) PlaySound(SoundModify);
                  Print(ExpertName+": modify buy by NDDmode, ticket: "+DoubleToStr(OrderTicket(),0));
                  break;
                 }
               else
                 {
                  Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try again modify order");
                  RefreshRates();
                 }
               //---Errors
               if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
               //---
              }//End while(true)
           }//End if((OrderType()
         //------------------------------------------------------
         //Modify stop loss and take profit sell order
         if((OrderType()==OP_SELL) && (OrderStopLoss()==0) && (OrderTakeProfit()==0))
           {
            while(true)
              {
               TP=0;
               SL=0;
               if((TakeProfit>0) && (UseTakeProfit==true)) TP=NormalizeDouble(Bid-OrderTP,Digits);
               if((StopLoss>0) && (UseStopLoss==true)) SL=NormalizeDouble(Ask+OrderSL,Digits);
               if((TrailingStop>0) && (UseStopLoss==false) && (UseTrailingStop==true)) SL=NormalizeDouble(Ask+TrailingStop,Digits);
               //---
               if((TP==0) && (SL==0)) break;
               //---
               WasOrderModified=OrderModify(OrderTicket(),NormalizeDouble(OrderOpenPrice(),Digits),SL,TP,0,clrRed);
               //---
               if(WasOrderModified>0)
                 {
                  if(SoundAlert==true) PlaySound(SoundModify);
                  Print(ExpertName+": modify sell by NDDmode, ticket: "+DoubleToStr(OrderTicket(),0));
                  break;
                 }
               else
                 {
                  Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try again modify order");
                  RefreshRates();
                 }
               //---Errors
               if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
               //---
              }//End while(true)
           }//End if((OrderType()
         //------------------------------------------------------
        }//End OrderSelect(...
      //------------------------------------------------------
     }//End if(RunNDDbroker==true)
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Modify orders
//====================================================================================================================================================//
void ModifyOrders()
  {
   double PriceComad=0;
   double LocalStopLoss=0;
   bool WasOrderModified;
   string CommentModify;
//------------------------------------------------------
//Select order
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS)==True)
        {
         if((OrderSymbol()==SymbolName) && (OrderMagicNumber()==OrdersID))
           {
            //------------------------------------------------------
            //Modify buy
            if(OrderType()==OP_BUY)
              {
               LocalStopLoss=0.0;
               WasOrderModified=false;
               while(true)
                 {
                  //------------------------------------------------------
                  //Break even
                  if((LocalStopLoss==0) && (BreakEven>0) && (UseBreakEven==true) && (Bid-OrderOpenPrice()>=(BreakEven+BreakEvenAfter)*DigitPoints) && (NormalizeDouble(OrderOpenPrice()+BreakEven*DigitPoints,Digits)<=Bid-(StopLevel*DigitPoints)))//&&(OrderStopLoss()<OrderOpenPrice()))
                    {
                     PriceComad=NormalizeDouble(OrderOpenPrice()+BreakEven*DigitPoints,Digits);
                     LocalStopLoss=BreakEven;
                     CommentModify="break even";
                    }
                  //------------------------------------------------------
                  //Trailing stop
                  if((LocalStopLoss==0) && (TrailingStop>0) && (UseTrailingStop==true) && ((NormalizeDouble(Bid-((TrailingStop+TrailingStep)*DigitPoints),Digits)>OrderStopLoss())))
                    {
                     PriceComad=NormalizeDouble(Bid-TrailingStop*DigitPoints,Digits);
                     LocalStopLoss=TrailingStop;
                     CommentModify="trailing stop";
                    }
                  //------------------------------------------------------
                  //Modify
                  if((LocalStopLoss>0) && (PriceComad!=NormalizeDouble(OrderStopLoss(),Digits)))
                     WasOrderModified=OrderModify(OrderTicket(),0,PriceComad,NormalizeDouble(OrderTakeProfit(),Digits),0,clrBlue);
                  else break;
                  //---
                  if(WasOrderModified>0)
                    {
                     if(SoundAlert==true) PlaySound(SoundModify);
                     Print(ExpertName+": modify buy by "+CommentModify+", ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try again modify order");
                     RefreshRates();
                    }
                  //---Errors
                  if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
                  //---
                 }//End while(true)
              }//End if(OrderType()
            //------------------------------------------------------
            //Modify sell
            if(OrderType()==OP_SELL)
              {
               WasOrderModified=false;
               LocalStopLoss=0.0;
               while(true)
                 {
                  //------------------------------------------------------
                  //Break even
                  if((LocalStopLoss==0) && (BreakEven>0) && (UseBreakEven==true) && (OrderOpenPrice()-Ask>=(BreakEven+BreakEvenAfter)*DigitPoints) && (NormalizeDouble(OrderOpenPrice()-BreakEven*DigitPoints,Digits)>=Ask+(StopLevel*DigitPoints)))//&&(OrderStopLoss()>OrderOpenPrice()))
                    {
                     PriceComad=NormalizeDouble(OrderOpenPrice()-BreakEven*DigitPoints,Digits);
                     LocalStopLoss=BreakEven;
                     CommentModify="break even";
                    }
                  //------------------------------------------------------
                  //Trailing stop
                  if((LocalStopLoss==0) && (TrailingStop>0) && (UseTrailingStop==true) && ((NormalizeDouble(Ask+((TrailingStop+TrailingStep)*DigitPoints),Digits)<OrderStopLoss())))
                    {
                     PriceComad=NormalizeDouble(Ask+TrailingStop*DigitPoints,Digits);
                     LocalStopLoss=TrailingStop;
                     CommentModify="trailing stop";
                    }
                  //------------------------------------------------------
                  //Modify
                  if((LocalStopLoss>0) && (PriceComad!=NormalizeDouble(OrderStopLoss(),Digits)))
                     WasOrderModified=OrderModify(OrderTicket(),0,PriceComad,NormalizeDouble(OrderTakeProfit(),Digits),0,clrRed);
                  else break;
                  //---
                  if(WasOrderModified>0)
                    {
                     if(SoundAlert==true) PlaySound(SoundModify);
                     Print(ExpertName+": modify sell by "+CommentModify+", ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try again modify order");
                     RefreshRates();
                    }
                  //---Errors
                  if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
                  //---
                 }//End while(true)
              }//End if(OrderType()
            //------------------------------------------------------
           }//End if((OrderSymbol()...
        }//End OrderSelect(...
     }//End for(...
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Close orders
//====================================================================================================================================================//
void CloseOrders(int TypeOfOrders)
  {
   bool WasOrderClosed;
   string CommentClose="close function";
//------------------------------------------------------
//Select order
   for(i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS)==True)
        {
         if((OrderSymbol()==SymbolName) && (OrderMagicNumber()==OrdersID))
           {
            //------------------------------------------------------
            //Close buy
            if((OrderType()==OP_BUY) && (TypeOfOrders==OP_BUY))
              {
               WasOrderClosed=false;
               while(true)
                 {
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,clrAquamarine);
                  if(WasOrderClosed>0)
                    {
                     if(SoundAlert==true) PlaySound(SoundFileAtClose);
                     Print(ExpertName+": close buy by "+CommentClose+", ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try close modify order");
                     RefreshRates();
                    }
                  //---Errors
                  if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
                  //---
                 }//End while(true)
              }//End if(OrderType()
            //------------------------------------------------------
            //Close sell
            if((OrderType()==OP_SELL) && (TypeOfOrders==OP_SELL))
              {
               WasOrderClosed=false;
               while(true)
                 {
                  WasOrderClosed=OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,clrTomato);
                  if(WasOrderClosed>0)
                    {
                     if(SoundAlert==true) PlaySound(SoundFileAtClose);
                     Print(ExpertName+": close sell by "+CommentClose+", ticket: "+DoubleToStr(OrderTicket(),0));
                     break;
                    }
                  else
                    {
                     Print("Error: ",DoubleToStr(GetLastError(),0)+" || "+ExpertName+": receives new data and try again close order");
                     RefreshRates();
                    }
                  //---Errors
                  if((GetLastError()==1) || (GetLastError()==132) || (GetLastError()==133) || (GetLastError()==137) || (GetLastError()==4108) || (GetLastError()==4109)) break;
                  //---
                 }//End while(true)
              }//End if(OrderType()
            //------------------------------------------------------
           }//End if((OrderSymbol()...
        }//End OrderSelect(...
     }//End for(...
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Check orders
//====================================================================================================================================================//
void CountOrders(int Magic)
  {
   SumOrders=0;
   BuyOrders=0;
   SellOrders=0;
   TypeLastOrder=-1;
   PipsBuyOrders=0;
   PipsSellOrders=0;
   PipsLastBuyOrders=0;
   PipsLastSellOrders=0;
   ProfitBuyOrders=0;
   ProfitSellOrders=0;
   SumFloating=0;
//---
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==Magic) && (OrderSymbol()==SymbolName))
           {
            TypeLastOrder=OrderType();
            if(OrderType()==OP_BUY)
              {
               PipsLastBuyOrders=(Bid-OrderOpenPrice())/DigitPoints;
               PipsBuyOrders+=(Bid-OrderOpenPrice())/DigitPoints;
               ProfitBuyOrders+=OrderProfit()+OrderCommission()+OrderSwap();
               BuyOrders++;
              }
            if(OrderType()==OP_SELL)
              {
               PipsLastSellOrders=(OrderOpenPrice()-Ask)/DigitPoints;
               PipsSellOrders+=(OrderOpenPrice()-Ask)/DigitPoints;
               ProfitSellOrders+=OrderProfit()+OrderCommission()+OrderSwap();
               SellOrders++;
              }
            SumOrders++;
            SumFloating+=OrderProfit()+OrderCommission()+OrderSwap();
           }
        }
     }
  }
//====================================================================================================================================================//
//History results
//====================================================================================================================================================//
void HistoryResults()
  {
//---------------------------------------------------------------------
   TotalHistoryOrders=0;
   TotalHistoryProfitLoss=0;
   HistoryBuy=0;
   HistorySell=0;
//---------------------------------------------------------------------
   for(i=0; i<OrdersHistoryTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if((OrderMagicNumber()==OrdersID) && (OrderSymbol()==SymbolName))
           {
            TotalHistoryOrders++;
            TotalHistoryProfitLoss+=OrderProfit()+OrderCommission()+OrderSwap();
            if(OrderType()==OP_BUY) HistoryBuy++;
            if(OrderType()==OP_SELL) HistorySell++;
           }
        }
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//Lot size
//====================================================================================================================================================//
double CalcLots()
  {
   double LotSize=0;
   if(AutoLotSize==true) LotSize=MathMin(MathMax((MathRound((AccountFreeMargin()*RiskFactor/100000)/MarketInfo(SymbolName,MODE_LOTSTEP))*MarketInfo(SymbolName,MODE_LOTSTEP)),MarketInfo(SymbolName,MODE_MINLOT)),MarketInfo(SymbolName,MODE_MAXLOT));
   if(AutoLotSize==false) LotSize=MathMin(MathMax((MathRound(ManualLotSize/MarketInfo(SymbolName,MODE_LOTSTEP))*MarketInfo(SymbolName,MODE_LOTSTEP)),MarketInfo(SymbolName,MODE_MINLOT)),MarketInfo(SymbolName,MODE_MAXLOT));
   return(LotSize);
  }
//====================================================================================================================================================//
//Comment's background
//====================================================================================================================================================//
void ChartBackground(string StringName,color ImageColor,int Xposition,int Yposition,int Xsize,int Ysize)
  {
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
  }
//====================================================================================================================================================//
//Comment in chart
//====================================================================================================================================================//
void CommentScreen()
  {
   string MMstring="";
   string StringSpread="";
//------------------------------------------------------
//String money management
   if(AutoLotSize==true) MMstring="Auto";
   if(AutoLotSize==false) MMstring="Manual";
//------------------------------------------------------
//String spread
   if(MaxSpread==0) StringSpread="EA NOT CHECK SPREAD,  Expert running";
   if((Spread<=MaxSpread) && (MaxSpread>0)) StringSpread="Acceptable Spread ,  Expert is running";
   if((Spread>MaxSpread) && (MaxSpread>0)) StringSpread="Unacceptable Spread. EA stop running";
//------------------------------------------------------
//Comment in chart
   Comment("==============================","\n",
           OperationInfo,"\n",
           "==============================","\n",
           StringSpread,"\n",
           "Max Spread: ",DoubleToStr(MaxSpread,2)," || Current Spread: ",DoubleToStr(Spread,2),"\n",
           "==============================","\n",
           "Money Management: ",MMstring," || Lot: ",DoubleToStr(CalcLots(),2),"\n",
           "==============================","\n",
           "Buy Orders: ",DoubleToStr(BuyOrders,0)," | Sell Orders: ",DoubleToStr(SellOrders,0)," | Total: ",DoubleToStr(SumOrders,0),"\n",
           "==============================","\n",
           "Buy PnL: ",DoubleToStr(ProfitBuyOrders,2)," | Sell PnL: ",DoubleToStr(ProfitSellOrders,2)," | Total PnL: ",DoubleToStr(SumFloating,2),"\n",
           "==============================","\n",
           "History Trades / Profit:   ",DoubleToStr(TotalHistoryOrders,0)," / ",DoubleToStr(TotalHistoryProfitLoss,2)," (",DoubleToStr(HistoryBuy,0),"/",DoubleToStr(HistorySell,0),")\n",
           "==============================");
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Indicator signals
//====================================================================================================================================================//
void GetSignals()
  {
//------------------------------------------------------
   double TrendUP=0;
   double TrendDN=0;
//------------------------------------------------------
   if(iTime(NULL,0,0)!=LastTimeBar)
     {
      TrendUP=iCustom(NULL,0,IndicatorName,BarsCount,BBperiod,BBdeviations,MAperiod,ATRperiod,UseATRfilter,AlertON,TypeOfArrows,3,IndicatorsShift);
      TrendDN=iCustom(NULL,0,IndicatorName,BarsCount,BBperiod,BBdeviations,MAperiod,ATRperiod,UseATRfilter,AlertON,TypeOfArrows,2,IndicatorsShift);
      //------------------------------------------------------
      //Signals open orders
      if((TrendDN!=EMPTY_VALUE)) OpenBuy=true;
      if((TrendUP!=EMPTY_VALUE)) OpenSell=true;
      //------------------------------------------------------
      //Signals close orders
      if(CloseInSignal==true)
        {
         if((BuyOrders>0)&&(OpenSell==true)) CloseBuy=true;
         if((SellOrders>0)&&(OpenBuy==true)) CloseSell=true;
        }
      //------------------------------------------------------
      LastTimeBar=iTime(NULL,0,0);
     }
//------------------------------------------------------
  }
//====================================================================================================================================================//
//End code
//====================================================================================================================================================//
