//====================================================================================================================================================//
#property copyright   "Copyright 2014-2018, Nikolaos Pantzos"
#property link        "https://www.mql5.com/en/users/pannik"
#property version     "2.30"
#property description "This Expert Advisor is a tool to delete or put take profit, stop loss and manage it as basket orders or order by order."
#property description "\n- If select expert to manage 'Order_By_Order_All_Symbols' put it in one chart and expert manage all order of all symbols."
#property description "- If select expert to manage 'Same_Type_Of_Chart_Symbol_As_One' expert can to manage only orders as same chart symbol,"
#property description "- If you want to manage 'Same_Type_Of_Chart_Symbol_As_One' more of one symbols orders, attach expert in same charts/symbols of orders"
//#property icon        "\\Images\\TP-SL-TSL_Logo.ico";
#property strict
//====================================================================================================================================================//
enum MO {Order_By_Order_All_Symbols,Same_Type_Of_Chart_Symbol_As_One};
//====================================================================================================================================================//
extern string SetManageOrders   = "||=============== Manage Orders ===============||";
extern MO     ManageOrders      = Order_By_Order_All_Symbols;
extern string AddSLTP           = "||=============== Add SL/TP ===============||";
extern bool   PutTakeProfit     = true;
extern double TakeProfitPips    = 20.0;
extern bool   PutStopLoss       = true;
extern double StopLossPips      = 20.0;
extern string TrailingSL        = "||=============== Trailing SL ===============||";
extern bool   UseTrailingStop   = true;
extern double PutStopLossAfter  = 0.0;
extern double TrailingStop      = 5.0;
extern double TrailingStep      = 1.0;
extern bool   UseBreakEven      = false;
extern double BreakEvenAfter    = 10.0;
extern double BreakEvenPips     = 5.0;
extern string DeleteSLTP        = "||=============== Delete SL/TP ===============||";
extern bool   DeleteTakeProfit  = false;
extern bool   DeleteStopLoss    = false;
extern string AdvancedSets      = "||=============== Advanced Sets ===============||";
extern string MagicNumberInfo1  = ">0 = modify identifier orders";
extern string MagicNumberInfo2  = "0 = modify all orders";
extern string MagicNumberInfo3  = "-1 = modify only manual orders";
extern string MagicNumberInfo4  = "-2 = modify only chart symbol orders";
extern string MagicNumberInfo5  = "-3 = modify only by comments orders";
extern int    MagicNumber       = -2;
extern string OrdersComment     = "Orders' comment to modify";
extern bool   SoundAlert        = true;
//====================================================================================================================================================//
string SoundModify="tick.wav";
string BackgroundName;
double StopLevel;
double AveragePriceBuy=0;
double AveragePriceSell=0;
double PointsSymbol;
int SumOrders=0;
int BuyOrders=0;
int SellOrders=0;
int MultiplierPoint;
int DigitsPrices;
bool MarketClosedCom;
bool CallMain=false;
long   ChartColor;
string TP;
string SL;
string TSL;
string MN;
string SA;
string BE;
string ManageMode1="";
string ManageMode2="";
//====================================================================================================================================================//
//init function
int OnInit()
  {
//---------------------------------------------------------------------
//Set timer
   EventSetMillisecondTimer(10);
//---------------------------------------------------------------------
//Set background
   ChartColor=ChartGetInteger(0,CHART_COLOR_BACKGROUND,0);
   BackgroundName="Background-"+WindowExpertName();
   if(ObjectFind(BackgroundName)==-1)
      ChartBackground(BackgroundName,(color)ChartColor,0,15,135,179);
//------------------------------------------------------
//Broker 4 or 5 digits
   MultiplierPoint=1;
   if((MarketInfo(Symbol(),MODE_DIGITS)==3)||(MarketInfo(Symbol(),MODE_DIGITS)==5))
      MultiplierPoint=10;
//(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint)
//------------------------------------------------------
//Minimum trailing, take profit, stop loss, break even
   StopLevel=MathMax(MarketInfo(Symbol(),MODE_FREEZELEVEL)/MultiplierPoint,MarketInfo(Symbol(),MODE_STOPLEVEL)/MultiplierPoint);
   if((TrailingStop>0)&&(TrailingStop<StopLevel))
      TrailingStop=StopLevel;
   if(TrailingStep>TrailingStop)
      TrailingStep=TrailingStop;
   if((TakeProfitPips>0)&&(TakeProfitPips<StopLevel))
      TakeProfitPips=StopLevel;
   if((StopLossPips>0)&&(StopLossPips<StopLevel))
      StopLossPips=StopLevel;
   if(BreakEvenAfter<BreakEvenPips)
      BreakEvenAfter=BreakEvenPips;
   if(BreakEvenAfter-BreakEvenPips<StopLevel)
      BreakEvenAfter=BreakEvenPips+StopLevel;
   if((PutStopLossAfter>0)&&(PutStopLossAfter<TrailingStop))
      PutStopLossAfter=TrailingStop;
   if(MagicNumber<-3)
      MagicNumber=-2;
//------------------------------------------------------
//External comment
   if(PutTakeProfit==true)
      TP=DoubleToStr(TakeProfitPips,2);
   else
      TP="FALSE";
   if(PutStopLoss==true)
      SL=DoubleToStr(StopLossPips,2);
   else
      SL="FALSE";
   if(UseTrailingStop==true)
      TSL=DoubleToStr(TrailingStop,2)+"  ("+DoubleToStr(PutStopLossAfter,2)+")";
   else
      TSL="FALSE";
   if(UseBreakEven==true)
      BE=DoubleToStr(BreakEvenPips,2)+"  ("+DoubleToStr(BreakEvenAfter,2)+")";
   else
      BE="FALSE";
   if(MagicNumber>0)
      MN=DoubleToStr(MagicNumber,0);
   if(MagicNumber==0)
      MN="All Orders";
   if(MagicNumber==-1)
      MN="Manual Orders";
   if(MagicNumber==-2)
      MN="Symbol Orders";
   if(MagicNumber==-3)
      MN="By comments Orders";
   if(SoundAlert==true)
      SA="TRUE";
   else
      SA="FALSE";
//----------------------------------
//Set manage mode
   if(ManageOrders==0)
      ManageMode1="Order By Order";
   if(ManageOrders==1)
      ManageMode1="Same Type As One";
   if((ManageOrders==0)&&(MagicNumber!=-2))
      ManageMode2=" All Symbols";
   if((ManageOrders==0)&&(MagicNumber==-2))
      ManageMode2=" Chart Symbols";
   if(ManageOrders==1)
      ManageMode2=" "+Symbol()+" Symbol";
   if((ManageOrders==0)&&(MagicNumber==-2))
      ManageMode2=" Chart Symbols";
//------------------------------------------------------
   if(!IsTesting())
      MainFunction();//For show comment if market is closed
//------------------------------------------------------
   return(INIT_SUCCEEDED);
  }
//====================================================================================================================================================//
//deinit function
void OnDeinit(const int reason)
  {
   EventKillTimer();
   ObjectDelete(BackgroundName);
   Comment("");
  }
//====================================================================================================================================================//
//start function
void OnTick()
  {
//---------------------------------------------------------------------
//Reset values
   CallMain=true;
//---------------------------------------------------------------------
//For testing
   if((IsTesting())||(IsOptimization())||(IsVisualMode()))
     {
      CallMain=false;
      MainFunction();
     }
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
void OnTimer()
  {
//---------------------------------------------------------------------
//Call main function
   if(CallMain==true)
      MainFunction();
//---------------------------------------------------------------------
  }
//====================================================================================================================================================//
//main function
void MainFunction()
  {
   MarketClosedCom=false;
   double LocalTakeProfit=0;
   double LocalStopLoss=0;
   bool WasOrderModified=false;
   double PriceBuyAsk=0;
   double PriceBuyBid=0;
   double PriceSellAsk=0;
   double PriceSellBid=0;
   double Spread=0;
//----------------------------------
//expert not enabled
   if((!IsExpertEnabled())&&(!IsTesting()))
     {
      Comment("==================",
              "\n\n    ",WindowExpertName(),
              "\n\n==================",
              "\n\n    Expert Not Enabled ! ! !",
              "\n\n    Please Turn On Expert",
              "\n\n\n\n==================");
      return;
     }
//------------------------------------------------------
//Comment in screen
   Comment("==================",
           "\n  ",WindowExpertName(),
           "\n  Ready To Modify Orders",
           "\n==================",
           "\n  Manage: ",ManageMode1,
           "\n  Symbol: ",ManageMode2,
           "\n==================",
           "\n  Take Profit  : ",TP,
           "\n  Stop Loss    : ",SL,
           "\n  Trailing SL   : ",TSL,
           "\n  Break Even : ",BE,
           "\n==================",
           "\n  Orders ID   : ",MN,
           "\n  Sound Alert : ",SA,
           "\n==================");
//------------------------------------------------------
//Reset switchs
   if(DeleteTakeProfit==true)
      PutTakeProfit=false;
//---
   if(DeleteStopLoss==true)
     {
      PutStopLoss=false;
      UseTrailingStop=false;
      UseBreakEven=false;
     }
//------------------------------------------------------
//Count orders
   if(ManageOrders==1)//basket
     {
      CountOrders();
      Spread=(Ask-Bid)/(MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint);
      if(AveragePriceBuy!=0)
        {
         PriceBuyAsk=AveragePriceBuy;
         PriceBuyBid=AveragePriceBuy-Spread;
        }
      else
        {
         PriceBuyAsk=Ask;
         PriceBuyBid=Bid;
        }
      //---
      if(AveragePriceSell!=0)
        {
         PriceSellAsk=AveragePriceSell+Spread;
         PriceSellBid=AveragePriceSell;
        }
      else
        {
         PriceSellAsk=Ask;
         PriceSellBid=Bid;
        }
     }
//------------------------------------------------------
//Select order
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
        {
         if((
               ((OrderMagicNumber()==MagicNumber)||(MagicNumber==0))||
               ((OrderMagicNumber()==0)&&(MagicNumber==-1))||
               ((OrderSymbol()==Symbol())&&(MagicNumber==-2))||
               ((OrderComment()==OrdersComment)&&(MagicNumber==-3))
            )&&((OrderSymbol()==Symbol())||(ManageOrders==0)))
           {
            DigitsPrices=(int)MarketInfo(OrderSymbol(),MODE_DIGITS);
            PointsSymbol=MarketInfo(OrderSymbol(),MODE_POINT)*MultiplierPoint;
            //------------------------------------------------------
            //Set prices
            if(ManageOrders==0)
              {
               PriceBuyAsk=MarketInfo(OrderSymbol(),MODE_ASK);
               PriceBuyBid=MarketInfo(OrderSymbol(),MODE_BID);
               PriceSellAsk=MarketInfo(OrderSymbol(),MODE_ASK);
               PriceSellBid=MarketInfo(OrderSymbol(),MODE_BID);
              }
            //------------------------------------------------------
            //Delete stoploss and/or take profit
            if((DeleteTakeProfit==true)||(DeleteStopLoss==true))
              {
               LocalStopLoss=0;
               LocalTakeProfit=0;
               if(DeleteStopLoss==true)
                  LocalStopLoss=-1;
               if(DeleteTakeProfit==true)
                  LocalTakeProfit=-1;
               if((DeleteStopLoss==true)&&(OrderStopLoss()!=0))
                  LocalStopLoss=0;
               if((DeleteStopLoss==false)&&(OrderStopLoss()!=0))
                  LocalStopLoss=OrderStopLoss();
               if((DeleteTakeProfit==true)&&(OrderTakeProfit()!=0))
                  LocalTakeProfit=0;
               if((DeleteTakeProfit==false)&&(OrderTakeProfit()!=0))
                  LocalTakeProfit=OrderTakeProfit();
               //---
               if((LocalStopLoss==0)||(LocalTakeProfit==0))
                  WasOrderModified=OrderModify(OrderTicket(),OrderOpenPrice(),LocalStopLoss,LocalTakeProfit,0,clrNONE);
               if(WasOrderModified>0)
                 {
                  Print("Modify ticket: "+DoubleToStr(OrderTicket(),0));
                  if(SoundAlert==true)
                     PlaySound(SoundModify);
                  continue;
                 }
              }
            //------------------------------------------------------
            //Check stop loss and take profit
            if((UseBreakEven==false)&&(UseTrailingStop==false))
              {
               if((PutStopLoss==true)&&(OrderStopLoss()!=0)&&(PutTakeProfit==true)&&(OrderTakeProfit()!=0))
                  continue;
               //---
               if((PutStopLoss==true)&&(OrderStopLoss()!=0)&&(PutTakeProfit==false))
                  continue;
               //---
               if((PutStopLoss==false)&&(PutTakeProfit==true)&&(OrderTakeProfit()!=0))
                  continue;
              }
            //------------------------------------------------------
            //Modify buy
            if(OrderType()==OP_BUY)
              {
               LocalStopLoss=0;
               LocalTakeProfit=0;
               WasOrderModified=false;
               //------------------------------------------------------
               //Put stoploss and/or take profit
               if(ManageOrders==0)
                 {
                  if((PutStopLoss==true)&&(OrderStopLoss()==0))
                     LocalStopLoss=NormalizeDouble(PriceBuyBid-StopLossPips*PointsSymbol,DigitsPrices);
                  if((PutTakeProfit==true)&&(OrderTakeProfit()==0))
                     LocalTakeProfit=NormalizeDouble(PriceBuyAsk+TakeProfitPips*PointsSymbol,DigitsPrices);
                  else
                     LocalTakeProfit=OrderTakeProfit();
                 }
               //---
               if(ManageOrders==1)
                 {
                  if((PutStopLoss==true)&&((OrderStopLoss()==0)||(OrderStopLoss()!=NormalizeDouble(PriceBuyBid-StopLossPips*PointsSymbol,DigitsPrices))))
                     LocalStopLoss=NormalizeDouble(PriceBuyBid-StopLossPips*PointsSymbol,DigitsPrices);
                  if((PutTakeProfit==true)&&((OrderTakeProfit()==0)||(OrderTakeProfit()!=NormalizeDouble(PriceBuyAsk+TakeProfitPips*PointsSymbol,DigitsPrices))))
                     LocalTakeProfit=NormalizeDouble(PriceBuyAsk+TakeProfitPips*PointsSymbol,DigitsPrices);
                  else
                     LocalTakeProfit=OrderTakeProfit();
                 }
               //------------------------------------------------------
               //Trailing stop
               if(UseBreakEven==false)
                 {
                  if(((UseTrailingStop==true)&&(LocalStopLoss==0)&&(TrailingStop>0))&&
                     ((PutStopLossAfter==0)||(NormalizeDouble(PriceBuyBid-PutStopLossAfter*PointsSymbol,DigitsPrices)>=OrderOpenPrice()))&&
                     ((NormalizeDouble(PriceBuyBid-((TrailingStop+TrailingStep)*PointsSymbol),DigitsPrices)>OrderStopLoss())||(OrderStopLoss()==0)))
                     LocalStopLoss=NormalizeDouble(PriceBuyBid-TrailingStop*PointsSymbol,DigitsPrices);
                 }
               //---
               if((UseBreakEven==true)&&(OrderStopLoss()>=OrderOpenPrice()))
                 {
                  if(((UseTrailingStop==true)&&(LocalStopLoss==0)&&(TrailingStop>0))&&
                     ((PutStopLossAfter==0)||(NormalizeDouble(PriceBuyBid-PutStopLossAfter*PointsSymbol,DigitsPrices)>=OrderOpenPrice()))&&
                     ((NormalizeDouble(PriceBuyBid-((TrailingStop+TrailingStep)*PointsSymbol),DigitsPrices)>OrderStopLoss())||(OrderStopLoss()==0)))
                     LocalStopLoss=NormalizeDouble(PriceBuyBid-TrailingStop*PointsSymbol,DigitsPrices);
                 }
               //------------------------------------------------------
               //Break even
               if((UseBreakEven==true)&&(LocalStopLoss==0)&&(BreakEvenPips>0)&&
                  (NormalizeDouble(PriceBuyBid-BreakEvenAfter*PointsSymbol,DigitsPrices)>=OrderOpenPrice())&&
                  ((NormalizeDouble(OrderOpenPrice()+BreakEvenPips*PointsSymbol,DigitsPrices)>OrderStopLoss())||(OrderStopLoss()==0)))
                  LocalStopLoss=NormalizeDouble(OrderOpenPrice()+BreakEvenPips*PointsSymbol,DigitsPrices);
               //-----------------------
               //Modify
               if(((LocalStopLoss>0)&&(LocalStopLoss!=NormalizeDouble(OrderStopLoss(),DigitsPrices))&&(OrderStopLoss()!=0))||(((LocalStopLoss>0)&&(OrderStopLoss()==0))||((LocalTakeProfit>0)&&(OrderTakeProfit()==0))))
                  WasOrderModified=OrderModify(OrderTicket(),OrderOpenPrice(),LocalStopLoss,LocalTakeProfit,0,clrBlue);
               //---
               if(WasOrderModified>0)
                 {
                  Print("Modify buy ticket: "+DoubleToStr(OrderTicket(),0));
                  if(SoundAlert==true)
                     PlaySound(SoundModify);
                 }
              }//End if(OrderType()
            //------------------------------------------------------
            //Modify sell
            if(OrderType()==OP_SELL)
              {
               LocalStopLoss=0;
               LocalTakeProfit=0;
               WasOrderModified=false;
               //------------------------------------------------------
               //Put stoploss and/or take profit
               if(ManageOrders==0)
                 {
                  if((PutStopLoss==true)&&(OrderStopLoss()==0))
                     LocalStopLoss=NormalizeDouble(PriceSellAsk+StopLossPips*PointsSymbol,DigitsPrices);
                  if((PutTakeProfit==true)&&(OrderTakeProfit()==0))
                     LocalTakeProfit=NormalizeDouble(PriceSellBid-TakeProfitPips*PointsSymbol,DigitsPrices);
                  else
                     LocalTakeProfit=OrderTakeProfit();
                 }
               //---
               if(ManageOrders==1)
                 {
                  if((PutStopLoss==true)&&((OrderStopLoss()==0)||(OrderStopLoss()!=NormalizeDouble(PriceSellAsk+StopLossPips*PointsSymbol,DigitsPrices))))
                     LocalStopLoss=NormalizeDouble(PriceSellAsk+StopLossPips*PointsSymbol,DigitsPrices);
                  if((PutTakeProfit==true)&&((OrderTakeProfit()==0)||(OrderTakeProfit()!=NormalizeDouble(PriceSellBid-TakeProfitPips*PointsSymbol,DigitsPrices))))
                     LocalTakeProfit=NormalizeDouble(PriceSellBid-TakeProfitPips*PointsSymbol,DigitsPrices);
                  else
                     LocalTakeProfit=OrderTakeProfit();
                 }
               //------------------------------------------------------
               //Trailing stop
               if(UseBreakEven==false)
                 {
                  if(((UseTrailingStop==true)&&(LocalStopLoss==0)&&(TrailingStop>0))&&
                     ((PutStopLossAfter==0)||(NormalizeDouble(PriceSellAsk+PutStopLossAfter*PointsSymbol,DigitsPrices)<=OrderOpenPrice()))&&
                     ((NormalizeDouble(PriceSellAsk+((TrailingStop+TrailingStep)*PointsSymbol),DigitsPrices)<OrderStopLoss())||(OrderStopLoss()==0)))
                     LocalStopLoss=NormalizeDouble(PriceSellAsk+TrailingStop*PointsSymbol,DigitsPrices);
                 }
               //---
               if((UseBreakEven==true)&&(OrderStopLoss()<=OrderOpenPrice()))
                 {
                  if(((UseTrailingStop==true)&&(LocalStopLoss==0)&&(TrailingStop>0))&&
                     ((PutStopLossAfter==0)||(NormalizeDouble(PriceSellAsk+PutStopLossAfter*PointsSymbol,DigitsPrices)<=OrderOpenPrice()))&&
                     ((NormalizeDouble(PriceSellAsk+((TrailingStop+TrailingStep)*PointsSymbol),DigitsPrices)<OrderStopLoss())||(OrderStopLoss()==0)))
                     LocalStopLoss=NormalizeDouble(PriceSellAsk+TrailingStop*PointsSymbol,DigitsPrices);
                 }
               //------------------------------------------------------
               //Break even
               if((UseBreakEven==true)&&(LocalStopLoss==0)&&(BreakEvenPips>0)&&
                  (NormalizeDouble(PriceSellAsk+BreakEvenAfter*PointsSymbol,DigitsPrices)<=OrderOpenPrice())&&
                  ((NormalizeDouble(OrderOpenPrice()-BreakEvenPips*PointsSymbol,DigitsPrices)<OrderStopLoss())||(OrderStopLoss()==0)))
                  LocalStopLoss=NormalizeDouble(OrderOpenPrice()-BreakEvenPips*PointsSymbol,DigitsPrices);
               //-----------------------
               //Modify
               if(((LocalStopLoss>0)&&(LocalStopLoss!=NormalizeDouble(OrderStopLoss(),DigitsPrices))&&(OrderStopLoss()!=0))||(((LocalStopLoss>0)&&(OrderStopLoss()==0))||((LocalTakeProfit>0)&&(OrderTakeProfit()==0))))
                  WasOrderModified=OrderModify(OrderTicket(),OrderOpenPrice(),LocalStopLoss,LocalTakeProfit,0,clrRed);
               //---
               if(WasOrderModified>0)
                 {
                  Print("Modify sell ticket: "+DoubleToStr(OrderTicket(),0));
                  if(SoundAlert==true)
                     PlaySound(SoundModify);
                 }
              }//End if(OrderType()
            //------------------------------------------------------
            //Closed Market
            if(GetLastError()==132)
              {
               MarketClosedCom=true;
               break;
              }
            //------------------------------------------------------
           }//End if((OrderMagicNumber()...
        }//End OrderSelect(...
     }//End for(...
//------------------------------------------------------
//Closed market
   if(MarketClosedCom==true)
     {
      MarketClosedCom=true;
      Print(WindowExpertName()+": Could not run, market is closed!!!");
      Comment("==================",
              "\n   ",WindowExpertName(),
              "\n==================",
              "\n\n\n      Market is closed!!! ",
              "\n\n      Not modify orders. ",
              "\n\n\n\n\n==================");
      Sleep(60000);
     }
//------------------------------------------------------
  }
//====================================================================================================================================================//
void CountOrders()
  {
   SumOrders=0;
   BuyOrders=0;
   SellOrders=0;
   AveragePriceBuy=0;
   AveragePriceSell=0;
//---
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
        {
         if(((OrderMagicNumber()==MagicNumber)||(MagicNumber==0))||((OrderMagicNumber()==0)&&(MagicNumber==-1))||((OrderComment()==OrdersComment)&&(MagicNumber==-3)))
           {
            if(OrderSymbol()==Symbol())
              {
               //---Count buy
               if(OrderType()==OP_BUY)
                 {
                  BuyOrders++;
                  AveragePriceBuy+=OrderOpenPrice();
                 }
               //---Count sell
               if(OrderType()==OP_SELL)
                 {
                  SellOrders++;
                  AveragePriceSell+=OrderOpenPrice();
                 }
               //---Count all
               SumOrders++;
              }
           }
        }
     }
//---Set average prices
   if(BuyOrders>0)
      AveragePriceBuy/=BuyOrders;
   if(SellOrders>0)
      AveragePriceSell/=SellOrders;
//---
  }
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
