//=========================================================================================================================================================================//
#property copyright   "Copyright 2019-2020, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "1.0"
#property description "This expert advisor place pending orders in a specific price."
//#property icon        "\\Images\\PricerEA-Logo.ico";
#property strict
//=========================================================================================================================================================================//
enum MM {Manually_Lot, Automatically_Lot};
//=========================================================================================================================================================================//
extern string OpenOrdersSets      = "||========== Open Orders Sets ==========||";
extern double PriceOpen_BuyStop   = 0.0;//Price Place Buy Stop (0=Not place)
extern double PriceOpen_SellStop  = 0.0;//Price Place Sell Stop (0=Not place)
extern double PriceOpen_BuyLimit  = 0.0;//Price Place Buy Limit (0=Not place)
extern double PriceOpen_SellLimit = 0.0;//Price Place Sell Limit (0=Not place)
extern string OrdersParameters    = "||========== Orders' Parameters Sets ==========||";
extern double TakeProfit          = 10;//Orders' Take Profit (0=Not Add Take Profit)
extern double StopLoss            = 10;//Orders' Stop Loss (0=Not Add Stop Loss)
extern bool   TrailingStopLoss    = false;//Modify Stop Loss
extern double StepTrailing        = 1.0;//Step Modify Stop Loss
extern bool   BreakEvenRun        = false;//Use Break Even
extern double BreakEvenAfter      = 10.0;//Profit To Activate Break Even (Plus Stop Loss)
extern int    MinutesExpire       = 60;//Minutes Expiry Pending Orders (0=Without Expiry)
extern string RiskFactorSet       = "||========== Risk Factor Sets ==========||";
extern MM     TypeOfLotSize       = Manually_Lot;//Type Of Lot Size
extern double RiskFactor          = 1.0;//Risk Factro For Auto Lot
extern double ManualLotSize       = 0.01;//Manual Lot Size
extern string HandleOrders        = "||========== Handle Orders Sets ==========||";
extern int    MagicNumber         = 12345;//Orders' Handle Magic Number
//=========================================================================================================================================================================//
string ExpertName;
int MultiplierPoint;
int i;
int CntTicks;
double DigitPoint;
double LotSize;
double StopLevel;
string BackgroundName;
color ChartColor;
bool SendBS=true;
bool SendSS=true;
bool SendBL=true;
bool SendSL=true;
//=========================================================================================================================================================================//
int OnInit()
  {
//---------------------------------------------------------------------
//Background
   BackgroundName="Background-"+WindowExpertName();
   ChartColor=(color)ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   if(ObjectFind(BackgroundName)==-1)
      ChartBackground(BackgroundName,ChartColor,0,15,180,121);
//---------------------------------------------------------------------
//Broker 4 or 5 digits
   DigitPoint=MarketInfo(Symbol(),MODE_POINT);
   MultiplierPoint=1;
   if(MarketInfo(Symbol(),MODE_DIGITS)==3||MarketInfo(Symbol(),MODE_DIGITS)==5)
     {
      MultiplierPoint=10;
      DigitPoint*=MultiplierPoint;
     }
//---------------------------------------------------------------------
//Minimum take profit and stop loss and distance for pendings
   StopLevel=MathMax(MarketInfo(Symbol(),MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(Symbol(),MODE_STOPLEVEL)/MultiplierPoint);
   if((TakeProfit>0)&&(TakeProfit<StopLevel))
      TakeProfit=StopLevel;
   if((StopLoss>0)&&(StopLoss<StopLevel))
      StopLoss=StopLevel;
//---------------------------------------------------------------------
//Confirm value
   if(PriceOpen_BuyStop<0)
      PriceOpen_BuyStop=0.0;
   if(PriceOpen_SellStop<0)
      PriceOpen_SellStop=0.0;
   if(PriceOpen_BuyLimit<0)
      PriceOpen_BuyLimit=0.0;
   if(PriceOpen_SellLimit<0)
      PriceOpen_SellLimit=0.0;
//---------------------------------------------------------------------
//Reset value
   CntTicks=0;
   SendBS=true;
   SendSS=true;
   SendBL=true;
   SendSL=true;
//---------------------------------------------------------------------
   ExpertName=WindowExpertName();
//---------------------------------------------------------------------
   OnTick();
//---------------------------------------------------------------------
   return(INIT_SUCCEEDED);
//---------------------------------------------------------------------
  }
//=========================================================================================================================================================================//
void OnDeinit(const int reason)
  {
//---------------------------------------------------------------------
   ObjectDelete(BackgroundName);
   Comment("");
//---------------------------------------------------------------------
  }
//=========================================================================================================================================================================//
void OnTick()
  {
//---------------------------------------------------------------------
   datetime Expire=0;
   double FreeMargin=0;
   bool WasOrderModify=false;
   int OpenOrderTicket;
   double SpreadPair=0;
   double DistAsk=0;
   double DistBid=0;
   double TP=0;
   double SL=0;
//---------------------------------------------------------------------
//Set expiry time
   if(MinutesExpire>0)
      Expire=TimeCurrent()+(MinutesExpire*60);
//---------------------------------------------------------------------
//Get spread
   SpreadPair=Ask-Bid;
//---------------------------------------------------------------------
//Set levels
   double OrderTP=NormalizeDouble(TakeProfit*DigitPoint,Digits);
   double OrderSL=NormalizeDouble(StopLoss*DigitPoint,Digits);
   double PipsAfter=NormalizeDouble(BreakEvenAfter*DigitPoint,Digits);
   double TrailingStep=NormalizeDouble(StepTrailing*DigitPoint,Digits);
//------------------------------------------------------
//Set lot size
   if(TypeOfLotSize==0)
      LotSize=ManualLotSize;
   if(TypeOfLotSize==1)
      LotSize=(AccountBalance()/MarketInfo(Symbol(),MODE_LOTSIZE))*RiskFactor;
//---------------------------------------------------------------------
   CommentScreen();
//---------------------------------------------------------------------
   if(CntTicks<3)
      CntTicks++;
   if(CntTicks<3)
      return;
//---------------------------------------------------------------------
//Open orders
//---Open pending stop
   if((PriceOpen_BuyStop>0.0)||(PriceOpen_SellStop>0.0))
     {
      //---Open buy stop
      if((PriceOpen_BuyStop>0.0)&&(isMgNum(MagicNumber,OP_BUYSTOP)==0)&&(SendBS==true))
        {
         FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_BUY,LotSize);
         //---
         while(FreeMargin>=0)
           {
            TP=0;
            SL=0;
            //---
            DistAsk=MathMax(NormalizeDouble(Ask+StopLevel,Digits),NormalizeDouble(PriceOpen_BuyStop,Digits));
            DistBid=MathMax(NormalizeDouble(Bid+StopLevel,Digits),NormalizeDouble(PriceOpen_BuyStop-SpreadPair,Digits));
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
               SendBS=false;
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
      if((PriceOpen_SellStop>0.0)&&(isMgNum(MagicNumber,OP_SELLSTOP)==0)&&(SendSS==true))
        {
         FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_SELL,LotSize);
         //---
         while(FreeMargin>=0)
           {
            TP=0;
            SL=0;
            //---
            DistAsk=MathMin(NormalizeDouble(Ask-StopLevel,Digits),NormalizeDouble(PriceOpen_SellStop+SpreadPair,Digits));
            DistBid=MathMin(NormalizeDouble(Bid-StopLevel,Digits),NormalizeDouble(PriceOpen_SellStop,Digits));
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
               SendSS=false;
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
//---Open pending limit
   if((PriceOpen_BuyLimit>0.0)||(PriceOpen_SellLimit>0.0))
     {
      //---Open buy limit
      if((PriceOpen_BuyLimit>0.0)&&(isMgNum(MagicNumber,OP_BUYLIMIT)==0)&&(SendBL==true))
        {
         FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_BUY,LotSize);
         //---
         while(FreeMargin>=0)
           {
            TP=0;
            SL=0;
            //---
            DistAsk=MathMin(NormalizeDouble(Ask-StopLevel,Digits),NormalizeDouble(PriceOpen_BuyLimit+SpreadPair,Digits));
            DistBid=MathMin(NormalizeDouble(Bid-StopLevel,Digits),NormalizeDouble(PriceOpen_BuyLimit,Digits));
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
               SendBL=false;
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
      if((PriceOpen_SellLimit>0.0)&&(isMgNum(MagicNumber,OP_SELLLIMIT)==0)&&(SendSL==true))
        {
         FreeMargin=AccountFreeMargin()+AccountFreeMarginCheck(Symbol(),OP_SELL,LotSize);
         //---
         while(FreeMargin>=0)
           {
            TP=0;
            SL=0;
            //---
            DistAsk=MathMax(NormalizeDouble(Ask+StopLevel,Digits),NormalizeDouble(PriceOpen_SellLimit,Digits));
            DistBid=MathMax(NormalizeDouble(Bid+StopLevel,Digits),NormalizeDouble(PriceOpen_SellLimit-SpreadPair,Digits));
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
               SendSL=false;
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
//---------------------------------------------------------------------
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
//---------------------------------------------------------------------
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
//---------------------------------------------------------------------
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
//---------------------------------------------------------------------
  }
//=========================================================================================================================================================================//
void CommentScreen()
  {
//---------------------------------------------------------------------
   string MMstring="";
   string StrBS;
   string StrSS;
   string StrBL;
   string StrSL;
   string StrLot;
//---------------------------------------------------------------------
   if(TypeOfLotSize==0)
      StrLot="Manually";
   if(TypeOfLotSize==1)
      StrLot="Automatically";
//---------------------------------------------------------------------
   if(PriceOpen_BuyStop==0)
      StrBS="Not place Buy Stop";
   else
      StrBS=DoubleToStr(PriceOpen_BuyStop,Digits);
   if(PriceOpen_SellStop==0)
      StrSS="Not place Sell Stop";
   else
      StrSS=DoubleToStr(PriceOpen_SellStop,Digits);
   if(PriceOpen_BuyLimit==0)
      StrBL="Not place Buy Limit";
   else
      StrBL=DoubleToStr(PriceOpen_BuyLimit,Digits);
   if(PriceOpen_SellLimit==0)
      StrSL="Not place Sell Limit";
   else
      StrSL=DoubleToStr(PriceOpen_SellLimit,Digits);
//---------------------------------------------------------------------
//Comment in chart
   Comment("=========================","\n",
           WindowExpertName(),"\n",
           "=========================","\n",
           "Price For Buy Stop: ",StrBS,"\n",
           "Price For Sell Stop: ",StrSS,"\n",
           "Price For Buy Limit: ",StrBL,"\n",
           "Price For Sell Limit: ",StrSL,"\n",
           "=========================","\n",
           "Orders' Lot Size: ",StrLot," ",DoubleToStr(LotSize,2),"\n",
           "=========================");
//---------------------------------------------------------------------
  }
//=========================================================================================================================================================================//
