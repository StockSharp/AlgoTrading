//+------------------------------------------------------------------+
//|                                             StopLevelCounter.mq5 | 
//|            Copyright © 2012, Nikolay Kositsin & Aleksey Rodionov | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin & Aleksey Rodionov"
#property link      "farria@mail.redcom.ru"
//---- Expert Advisor version number
#property version   "1.10"
//+-----------------------------------+
// type_font enumeration description  |
// CFontName class description        | 
//+-----------------------------------+
#include <GetFontName.mqh>
//+-----------------------------------+
//|  INDICATOR INPUT PARAMETERS       |
//+-----------------------------------+
input double Lot=1.0; //lot size
input color  LevelColor=clrBlue;//color for buy
input color  BuyColor=clrTeal;//color for buy
input color  SellColor=clrRed;//color for sell
input int    FontSize=11; //font size
input type_font FontType=Font14; //font type
input ENUM_BASE_CORNER  WhatCorner=CORNER_LEFT_UPPER; //location corner
input uint Y_=20; //vertical location
input uint X_=5; //horizontal location
//+-----------------------------------+

string sFontType,IndName;
//---- Declaration of integer variables of data starting point
int min_rates_total;
uint shift_1,shift_2,shift_3;
//+------------------------------------------------------------------+   
//| StopLevelCounter initialization function                         | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total=1;
   EventSetTimer(1);

//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,"StopLevelCounter");

//--- determining the accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,0);
//---- 
   CFontName FONT;
   sFontType=FONT.GetFontName(FontType);
   Deinit();
//----   
   IndName=__FILE__;
   int Len=StringLen(IndName);
   IndName=StringSubstr(IndName,0,Len-4);
//----
   switch(WhatCorner)
     {
      case CORNER_RIGHT_LOWER:
        {
         shift_1=Y_+40;
         shift_2=Y_+20;
         shift_3=Y_+0;
         break;
        }

      case CORNER_LEFT_LOWER:
        {
         shift_1=Y_+40;
         shift_2=Y_+20;
         shift_3=Y_+0;
         break;
        }
      default:
        {
         shift_1=Y_+0;
         shift_2=Y_+20;
         shift_3=Y_+40;
        }
     }
//---- end of initialization
  }
//+------------------------------------------------------------------+
//| Custom indicator deinitialization function                       |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   Deinit();
//----
  }
//+------------------------------------------------------------------+
//| StopLevelCounter function                                        |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---- declaration of variables with a floating point  
   double Bid,Ask,dStopLevel,dBuyProfit,dSellProfit;
//---- Declaration of string variables
   string word,sBuyProfit,sSellProfit,sStopLevel,sLot,sCurr;
   static double OlddStopLevel,OldBid,OldAsk;
   
   Bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
   Ask=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
   
   if(ObjectFind(0,"StopLevelCounter_Level")<0) SetHline(0,"StopLevelCounter_Level",0,Bid,LevelColor,0,3,false);       
   dStopLevel=NormalizeDouble(ObjectGetDouble(0,"StopLevelCounter_Level",OBJPROP_PRICE,0),_Digits);
   
   if(dStopLevel==OlddStopLevel && OldBid==Bid && OldAsk==Ask) return;

   
   if(!OrderCalcProfit(ORDER_TYPE_BUY,Symbol(),Lot,Ask,dStopLevel,dBuyProfit)) return;
   if(!OrderCalcProfit(ORDER_TYPE_SELL,Symbol(),Lot,Bid,dStopLevel,dSellProfit)) return;
   
   sStopLevel=DoubleToString(dStopLevel,_Digits);
   sBuyProfit=DoubleToString(dBuyProfit,2);
   sSellProfit=DoubleToString(dSellProfit,2);
   sLot=DoubleToString(Lot,3);
   sCurr=AccountInfoString(ACCOUNT_CURRENCY);
   
   StringConcatenate(word,"Profit with lot ",sLot," on the level ",sStopLevel);
   SetTLabel(0,"StopLevelCounter_Info",0,WhatCorner,ENUM_ANCHOR_POINT(2*WhatCorner),X_,shift_1,word,LevelColor,sFontType,FontSize);
   
   StringConcatenate(word,"to Buy position will be ",sBuyProfit,sCurr);
   SetTLabel(0,"StopLevelCounter_Buy",0,WhatCorner,ENUM_ANCHOR_POINT(2*WhatCorner),X_,shift_2,word,BuyColor,sFontType,FontSize);
   
   StringConcatenate(word,"to Sell position will be ",sSellProfit,sCurr);
   SetTLabel(0,"StopLevelCounter_Sell",0,WhatCorner,ENUM_ANCHOR_POINT(2*WhatCorner),X_,shift_3,word,SellColor,sFontType,FontSize);
//----     
   ChartRedraw(0);
   OlddStopLevel=dStopLevel;
   OldBid=Bid;
   OldAsk=Ask;
  }
//+------------------------------------------------------------------+
//| StopLevelCounter deinitialization function                       |
//+------------------------------------------------------------------+    
void Deinit()
  {
//----
   ObjectDelete(0,"StopLevelCounter_Info");
   ObjectDelete(0,"StopLevelCounter_Buy");
   ObjectDelete(0,"StopLevelCounter_Sell");
   ObjectDelete(0,"StopLevelCounter_Level");
//----
  }
//+------------------------------------------------------------------+
//|  Creating horizontal level                                       |
//+------------------------------------------------------------------+
void CreateHline
(
 long     chart_id,      // chart ID
 string   name,          // object name
 int      nwin,          // window index
 double   price1,        // price level 1
 color    Color,         // line color
 int      style,         // line style
 int      width,         // line width
 bool     back           // background display
 )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_HLINE,nwin,0,price1);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetInteger(chart_id,name,OBJPROP_STYLE,style);
   ObjectSetInteger(chart_id,name,OBJPROP_WIDTH,width);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,back);
   ObjectSetInteger(chart_id,name,OBJPROP_SELECTABLE,true);
   ObjectSetInteger(chart_id,name,OBJPROP_SELECTED,true);
//----
  }
//+------------------------------------------------------------------+
//|  Reinstallation of the horizontal level                          |
//+------------------------------------------------------------------+
void SetHline
(
 long     chart_id,      // chart ID
 string   name,          // object name
 int      nwin,          // window index
 double   price1,        // price level 1
 color    Color,         // line color
 int      style,         // line style
 int      width,         // line width
 bool     back           // background display
 )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateHline(chart_id,name,nwin,price1,Color,style,width,back);
   else
     {
      ObjectMove(chart_id,name,0,0,price1);
      ObjectSetInteger(chart_id,name,OBJPROP_BACK,back);
     }
//----
  }
//+------------------------------------------------------------------+
//|  Creating a text label                                           |
//+------------------------------------------------------------------+
void CreateTLabel
(
 long   chart_id,         // chart ID
 string name,             // object name
 int    nwin,             // window index
 ENUM_BASE_CORNER corner,// base corner location
 ENUM_ANCHOR_POINT point, // anchor point location
 int    X,                // the distance from the base corner along the X-axis in pixels
 int    Y,                // the distance from the base corner along the Y-axis in pixels
 string text,             // text
 color  Color,            // text color
 string Font,             // text font
 int    Size              // font size
 )
//---- 
  {
//----
   ObjectCreate(chart_id,name,OBJ_LABEL,0,0,0);
   ObjectSetInteger(chart_id,name,OBJPROP_CORNER,corner);
   ObjectSetInteger(chart_id,name,OBJPROP_ANCHOR,point);
   ObjectSetInteger(chart_id,name,OBJPROP_XDISTANCE,X);
   ObjectSetInteger(chart_id,name,OBJPROP_YDISTANCE,Y);
   ObjectSetString(chart_id,name,OBJPROP_TEXT,text);
   ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
   ObjectSetString(chart_id,name,OBJPROP_FONT,Font);
   ObjectSetInteger(chart_id,name,OBJPROP_FONTSIZE,Size);
   ObjectSetInteger(chart_id,name,OBJPROP_BACK,false);
//----
  }
//+------------------------------------------------------------------+
//|  Resetting a text label                                          |
//+------------------------------------------------------------------+
void SetTLabel
(
 long   chart_id,         // chart ID
 string name,             // object name
 int    nwin,             // window index
 ENUM_BASE_CORNER corner,// base corner location
 ENUM_ANCHOR_POINT point, // anchor point location
 int    X,                // the distance from the base corner along the X-axis in pixels
 int    Y,                // the distance from the base corner along the Y-axis in pixels
 string text,             // text
 color  Color,            // text color
 string Font,             // text font
 int    Size              // font size
 )
//---- 
  {
//----
   if(ObjectFind(chart_id,name)==-1) CreateTLabel(chart_id,name,nwin,corner,point,X,Y,text,Color,Font,Size);
   else
     {
      ObjectSetString(chart_id,name,OBJPROP_TEXT,text);
      ObjectSetInteger(chart_id,name,OBJPROP_XDISTANCE,X);
      ObjectSetInteger(chart_id,name,OBJPROP_YDISTANCE,Y);
      ObjectSetInteger(chart_id,name,OBJPROP_COLOR,Color);
     }
//----
  }
//+------------------------------------------------------------------+
