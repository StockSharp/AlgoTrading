//====================================================================================================================================================//
#property copyright   "Copyright 2017-2019, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.2"
#property description "Expert need 'iDoubleChannel_v1.5' indicator to works."
#property description "Can get the indicator from 'https://www.mql5.com/en/code/26462' link."
#property strict
//====================================================================================================================================================//
enum TypeC{Close_Ticket_Orders,Close_Basket_Orders};
//====================================================================================================================================================//
extern string IndicatorsSets    = "==== Set Indicator ====";
extern int    ChannelPeriod     = 14;
extern int    ChannelWidth      = 2;
extern int    IndicatorShift    = 0;
extern string AdvancedSets      = "==== Set Signals ====";
extern bool   OpenEverySignal   = true;
extern bool   CloseInSignal     = false;
extern string SetCloseOrders    = "==== Set Close Orders ====";
extern TypeC  TypeAutoClose     = Close_Ticket_Orders;
extern bool   CloseInProfit     = true;
extern double PipsCloseProfit   = 25.0;
extern bool   CloseInLoss       = false;
extern double PipsCloseLoss     = 100.0;
extern string SetOrders         = "==== Set Orders Parametre ====";
extern bool   UseTakeProfit     = false;
extern double TakeProfit        = 10.0;
extern bool   UseStopLoss       = false;
extern double StopLoss          = 10.0;
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
extern string CommentsOrders    = "DoubleChannelEA";
//====================================================================================================================================================//
string SoundFileAtClose="alert2.wav";
string SoundFileAtOpen="alert.wav";
string SoundModify="tick.wav";
string ExpertName;
string EASymbol;
string OperInfo;
string SymbolExtension="";
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
double FirstLotBuy;
double FirstLotSell;
double TotalLotsBuy;
double TotalLotsSell;
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
int i;
color ChartColor;
bool CheckSpread;
bool TimeToTrade;
bool OpenBuy=false;
bool OpenSell=false;
bool CloseBuy=false;
bool CloseSell=false;
datetime BarOpenBuy=0;
datetime BarOpenSell=0;
datetime TimeBegin;
datetime TimeEnd;
//====================================================================================================================================================//
//OnInit function
//====================================================================================================================================================//
int OnInit()
  {
//------------------------------------------------------
//Started information
   ExpertName=WindowExpertName();
   EASymbol=Symbol();
   if(StringLen(EASymbol)>6) SymbolExtension=StringSubstr(EASymbol,6,0);
//------------------------------------------------------
//Set ID
   OrdersID=MagicNumber;
   if(MagicNumber==0)
     {
      OrdersID=0;
      for(i=0; i<StringLen(EASymbol); i++) OrdersID+=(StringGetChar(EASymbol,i)*(i+1));
      for(i=0; i<StringLen(ExpertName); i++) OrdersID+=(StringGetChar(ExpertName,i)*(i+1));
      OrdersID+=22033;
     }
//------------------------------------------------------
//Broker 4 or 5 digits
   DigitPoints=MarketInfo(EASymbol,MODE_POINT);
   MultiplierPoint=1;
   if((MarketInfo(EASymbol,MODE_DIGITS)==3) || (MarketInfo(EASymbol,MODE_DIGITS)==5))
     {
      MultiplierPoint=10;
      DigitPoints*=MultiplierPoint;
     }
//------------------------------------------------------
//Minimum trailing, take profit and stop loss
   StopLevel=MathMax(MarketInfo(EASymbol,MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(EASymbol,MODE_STOPLEVEL)/MultiplierPoint);
   if((TrailingStop>0) && (TrailingStop<StopLevel)) TrailingStop=StopLevel;
   if((BreakEven>0) && (BreakEven<StopLevel)) BreakEven=StopLevel;
   if((TakeProfit>0) && (TakeProfit<StopLevel)) TakeProfit=StopLevel;
   if((StopLoss>0) && (StopLoss<StopLevel)) StopLoss=StopLevel;
   if(RiskFactor<0.1) RiskFactor=0.1;
   if(RiskFactor>100) RiskFactor=100;
//------------------------------------------------------
//Background
   ChartColor=(color)ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   if((ObjectFind("Background")==-1) && (!IsTesting()) && (!IsVisualMode())) ChartBackground("Background",ChartColor,0,15,210,182);
//---------------------------------------------------------------------
//Operation ifno
   OperInfo=ExpertName+"   Working well....";
//------------------------------------------------------
   if(!IsTesting()) OnTick();//For show comment if market is closed
//------------------------------------------------------
   return(INIT_SUCCEEDED);
//------------------------------------------------------
  }
//====================================================================================================================================================//
//OnDeinit function
//====================================================================================================================================================//
void OnDeinit(const int reason)
  {
//------------------------------------------------------
   ObjectDelete("Background");
   Comment("");
//------------------------------------------------------
  }
//====================================================================================================================================================//
//OnTick function
//====================================================================================================================================================//
void OnTick()
  {
//------------------------------------------------------
//Check for history and trading
   if(iBars(EASymbol,0)<ChannelPeriod)
     {
      if(!IsTesting()) Comment("\n  Please Wait to Update Bars ....");
      OperInfo="Expert find for bars on pair "+EASymbol;
      Print(ExpertName+" || "+OperInfo);
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
//------------------------------------------------------
   CheckSpread=true;
   OrdersOpened=OrdersTotal();
   TimeToTrade=true;
   OpenBuy=false;
   OpenSell=false;
   CloseBuy=false;
   CloseSell=false;
//---------------------------------------------------------------------
//Check open market
   if((!IsTesting()) || (!IsVisualMode()) || (!IsOptimization()))
     {
      MqlDateTime today;
      TimeToStruct(TimeLocal(),today);
      //---
      if(SymbolInfoSessionTrade(EASymbol,(ENUM_DAY_OF_WEEK)today.day_of_week,0,TimeBegin,TimeEnd)==false)
        {
         OperInfo="Market is closed!!!";
        }
     }
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
      Print(ExpertName+" || "+"Spread is greater than MaxSpread!!! (Spread: "+DoubleToStr(Spread,1)+" || MaxSpread: "+DoubleToStr(MaxSpread,1)+")");
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
         if(TypeAutoClose==0)
           {
            if((BuyOrders>0) && (PipsBuyOrders>=(PipsCloseProfit*BuyOrders))) {CloseOrders(OP_BUY); return;}
            if((SellOrders>0) && (PipsSellOrders>=(PipsCloseProfit*SellOrders))) {CloseOrders(OP_SELL); return;}
           }
         if(TypeAutoClose==1)
           {
            if((BuyOrders+SellOrders>0) && (ProfitBuyOrders+ProfitSellOrders>0))
              {
               if(((TotalLotsBuy>TotalLotsSell) && (PipsBuyOrders+PipsSellOrders>=PipsCloseProfit*BuyOrders)) || ((TotalLotsBuy<TotalLotsSell) && (PipsBuyOrders+PipsSellOrders>=PipsCloseProfit*SellOrders)))
                 {
                  CloseOrders(OP_BUY);
                  CloseOrders(OP_SELL);
                  return;
                 }
              }
           }
        }
      //---
      if(CloseInLoss==true)
        {
         if(TypeAutoClose==0)
           {
            if((BuyOrders>0) && (PipsBuyOrders<=-(PipsCloseLoss*BuyOrders))) {CloseOrders(OP_BUY); return;}
            if((SellOrders>0) && (PipsSellOrders<=-(PipsCloseLoss*SellOrders))) {CloseOrders(OP_SELL); return;}
           }
         if(TypeAutoClose==1)
           {
            if((BuyOrders+SellOrders>0) && (ProfitBuyOrders+ProfitSellOrders<0))
              {
               if(((TotalLotsBuy>TotalLotsSell) && (PipsBuyOrders+PipsSellOrders<=-PipsCloseLoss*BuyOrders)) || ((TotalLotsBuy<TotalLotsSell) && (PipsBuyOrders+PipsSellOrders<=-PipsCloseLoss*SellOrders)))
                 {
                  CloseOrders(OP_BUY);
                  CloseOrders(OP_SELL);
                  return;
                 }
              }
           }
        }
     }
//------------------------------------------------------
//Check signals
   if((CheckSpread==true) && (TimeToTrade==true)) GetSignals();
//------------------------------------------------------
//Call modify and close orders functions
   if(SumOrders>0)
     {
      if((UseTrailingStop==true) || (UseBreakEven==true)) ModifyOrders();
      if(CloseInSignal==true)
        {
         if((BuyOrders>0) && (CloseBuy==true)) CloseOrders(OP_BUY);
         if((SellOrders>0)&&(CloseSell==true)) CloseOrders(OP_SELL);
        }
     }
//------------------------------------------------------
//Open orders
   if((CheckSpread==true) && (TimeToTrade==true) && ((SumOrders<MaxOrders) || (MaxOrders==0)))
     {
      if((OpenBuy==true) && ((TimeCurrent()-BarOpenBuy)/60>Period()) && ((BuyOrders==0) || (OpenEverySignal==true))) OpenPosition(OP_BUY);
      if((OpenSell==true) && ((TimeCurrent()-BarOpenSell)/60>Period()) && ((SellOrders==0) || (OpenEverySignal==true))) OpenPosition(OP_SELL);
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
//------------------------------------------------------
   int OpenOrderTicket=0;
   bool WasOrderModified;
   double OpenPrice=0;
   color OpenColor=clrNONE;
   string TypeOfOrder;
   double CheckMargin=0;
   double OrdrLotSize=0;
//------------------------------------------------------
//Calculate take profit and stop loss in pips
   double TP=0;
   double SL=0;
   double OrderTP=NormalizeDouble(TakeProfit*DigitPoints,Digits);
   double OrderSL=NormalizeDouble(StopLoss*DigitPoints,Digits);
   double TrailingSL=NormalizeDouble(TrailingStop*DigitPoints,Digits);
//------------------------------------------------------
//Buy stop loss and take profit in price
   if(PositionType==OP_BUY)
     {
      TP=0;
      SL=0;
      if(BuyOrders>0) OrdrLotSize=FirstLotBuy; else OrdrLotSize=CalcLots();
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
      if(SellOrders>0) OrdrLotSize=FirstLotSell; else OrdrLotSize=CalcLots();
      OpenPrice=NormalizeDouble(Bid,Digits);
      OpenColor=clrRed;
      if((TakeProfit>0) && (UseTakeProfit==true)) TP=NormalizeDouble(Bid-OrderTP,Digits);
      if((StopLoss>0) && (UseStopLoss==true)) SL=NormalizeDouble(Ask+OrderSL,Digits);
      //if((StopLoss>0)&&(UseStopLoss==true)) SL=NormalizeDouble(MathMax(iHigh(NULL,0,1),Ask)+OrderSL,Digits);
      if((TrailingStop>0) && (UseStopLoss==false) && (UseTrailingStop==true) && (SL==0)) SL=NormalizeDouble(Ask+TrailingSL,Digits);
      TypeOfOrder="Sell";
     }
//------------------------------------------------------
//Check free margin base lot from open orders
   if(OrdrLotSize!=0) CheckMargin=AccountFreeMarginCheck(EASymbol,PositionType,OrdrLotSize);
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
      //NDD broker, no sl no tp
      if(RunNDDbroker==true)
        {
         TP=0;
         SL=0;
        }
      //------------------------------------------------------
      //Send orders
      OpenOrderTicket=OrderSend(EASymbol,PositionType,OrdrLotSize,OpenPrice,Slippage,SL,TP,CommentsOrders,OrdersID,0,OpenColor);
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
//------------------------------------------------------
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
         if((OrderSymbol()==EASymbol) && (OrderMagicNumber()==OrdersID))
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
//------------------------------------------------------
   bool WasOrderClosed;
   string CommentClose="close function";
//------------------------------------------------------
//Select order
   for(i=OrdersTotal()-1; i>=0; i--)
     {
      if(OrderSelect(i,SELECT_BY_POS)==True)
        {
         if((OrderSymbol()==EASymbol) && (OrderMagicNumber()==OrdersID))
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
//------------------------------------------------------
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
   BarOpenBuy=0;
   BarOpenSell=0;
   FirstLotBuy=0;
   FirstLotSell=0;
   TotalLotsBuy=0;
   TotalLotsSell=0;

//---
   for(i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if((OrderMagicNumber()==Magic) && (OrderSymbol()==EASymbol))
           {
            TypeLastOrder=OrderType();
            if(OrderType()==OP_BUY)
              {
               if(FirstLotBuy==0) FirstLotBuy=OrderLots();
               TotalLotsBuy+=OrderLots();
               BarOpenBuy=OrderOpenTime();
               PipsLastBuyOrders=(Bid-OrderOpenPrice())/DigitPoints;
               PipsBuyOrders+=(Bid-OrderOpenPrice())/DigitPoints;
               ProfitBuyOrders+=OrderProfit()+OrderCommission()+OrderSwap();
               BuyOrders++;
              }
            if(OrderType()==OP_SELL)
              {
               if(FirstLotSell==0) FirstLotSell=OrderLots();
               TotalLotsSell+=OrderLots();
               BarOpenSell=OrderOpenTime();
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
//------------------------------------------------------
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
         if((OrderMagicNumber()==OrdersID) && (OrderSymbol()==EASymbol))
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
//------------------------------------------------------
   double LotSize=0;
   if(AutoLotSize==true)
      LotSize=MathMin(MathMax((MathRound(((AccountFreeMargin()*RiskFactor/100000)/MarketInfo(EASymbol,MODE_TICKVALUE))/MarketInfo(EASymbol,MODE_LOTSTEP))*MarketInfo(EASymbol,MODE_LOTSTEP)),MarketInfo(EASymbol,MODE_MINLOT)),MarketInfo(EASymbol,MODE_MAXLOT));
   if(AutoLotSize==false)
      LotSize=MathMin(MathMax((MathRound(ManualLotSize/MarketInfo(EASymbol,MODE_LOTSTEP))*MarketInfo(EASymbol,MODE_LOTSTEP)),MarketInfo(EASymbol,MODE_MINLOT)),MarketInfo(EASymbol,MODE_MAXLOT));
   return(LotSize);
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Comment's background
//====================================================================================================================================================//
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
//====================================================================================================================================================//
//Comment in chart
//====================================================================================================================================================//
void CommentScreen()
  {
//------------------------------------------------------
   string MMstring="";
   string StringSpread="";
   double LotBuy=0;
   double LotSell=0;
//------------------------------------------------------
//String lot size
   if(BuyOrders>0) LotBuy=FirstLotBuy; else LotBuy=CalcLots();
   if(SellOrders>0) LotSell=FirstLotSell; else LotSell=CalcLots();
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
   Comment(" ============================",
           "\n ",OperInfo,
           "\n ============================",
           "\n ",StringSpread,
           "\n Max Spread: ",DoubleToStr(MaxSpread,2)," || Current Spread: ",DoubleToStr(Spread,2),
           "\n ============================",
           "\n MM: ",MMstring," || LotBuy: ",DoubleToStr(LotBuy,2)," || LotSell: ",DoubleToStr(LotSell,2),
           "\n ============================",
           "\n Buy Orders: ",DoubleToStr(BuyOrders,0)," || Buy PnL: ",DoubleToStr(ProfitBuyOrders,2)," (",DoubleToStr(PipsBuyOrders,2),"/",DoubleToStr((PipsCloseProfit*BuyOrders),2),")",
           "\n Sell Orders : ",DoubleToStr(SellOrders,0)," || Sell PnL: ",DoubleToStr(ProfitSellOrders,2)," (",DoubleToStr(PipsSellOrders,2),"/",DoubleToStr((PipsCloseProfit*SellOrders),2),")",
           "\n ============================",
           "\n Total Orders: ",DoubleToStr(SumOrders,0)," || Total PnL: ",DoubleToStr(SumFloating,2)," (",DoubleToStr(PipsBuyOrders+PipsSellOrders,2),"/",DoubleToStr(MathMax((PipsCloseProfit*BuyOrders),(PipsCloseProfit*SellOrders)),2),")",
           "\n ============================",
           "\n History Trades / Profit:   ",DoubleToStr(TotalHistoryOrders,0)," / ",DoubleToStr(TotalHistoryProfitLoss,2)," (",DoubleToStr(HistoryBuy,0),"/",DoubleToStr(HistorySell,0),")",
           "\n ============================");
//------------------------------------------------------
  }
//====================================================================================================================================================//
//Orders signals
//====================================================================================================================================================//
void GetSignals()
  {
//------------------------------------------------------
   double UpArrows=0;
   double DnArrows=0;
//------------------------------------------------------
   UpArrows=iCustom(NULL,0,"iDoubleChannel_v1.5",ChannelPeriod,ChannelWidth,IndicatorShift,3,0);
   DnArrows=iCustom(NULL,0,"iDoubleChannel_v1.5",ChannelPeriod,ChannelWidth,IndicatorShift,4,0);
//------------------------------------------------------
//Signals open orders
   if((UpArrows!=0) && (DnArrows!=0))
     {
      if(UpArrows!=EMPTY_VALUE) OpenBuy=true;
      if(DnArrows!=EMPTY_VALUE) OpenSell=true;
     }
//------------------------------------------------------
//Signals close orders
   if(CloseInSignal==true)
     {
      if(OpenSell==true) CloseBuy=true;
      if(OpenBuy==true) CloseSell=true;
     }
//------------------------------------------------------
  }
//====================================================================================================================================================//
//End code
//====================================================================================================================================================//
