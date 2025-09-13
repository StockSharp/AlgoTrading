//+------------------------------------------------------------------+
//|                                                      buysell.mq4 |
//|                                         Copyright 2015, sathudx. |
//|                            https://www.mql5.com/en/users/sathudx |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, sathudx."
#property link      "https://www.mql5.com/en/users/sathudx"
#property version   "1.00"
#property strict

// Initialization variables
double            MinLot,
MaxLot,
LotStep,
StopLevel,
TickValue,
Spread;
int               DigitLot,
DigitFactor;
// Display
string            lbl               = "GridEA";
int               XValue            = 20,
YValue            = 20,
XSize             = 80,
YSize             = 20;
string            Font              = "Tahoma";          // Font
int               FontSize          = 8;                 // Font size
ENUM_ALIGN_MODE   TextAlign         = ALIGN_CENTER;      // Text align
ENUM_BASE_CORNER  ChartCorner       = CORNER_LEFT_UPPER; // Chart corner for anchoring
color             RectangleColor    = clrRed;            // Rectangle color
color             TextColor         = clrGold;           // Text color
color             BackColor         = clrBlack;          // Background color
color             ButtonColor       = clrBlue;           // Background color
color             BorderColor       = clrBlack;          // Border color                  

input int         TakeProfit        = 100;
input double      Lot               = 0.1;
input double      LotExponential    = 2;
input int         MaxTrades         = 20;
input int         Slippage          = 10;
int               MagicNumber       = 1368;

double            BuyPrice,
SellPrice;
int               SumBuy,
SumSell;
double            NextBuy,
NextSell,
NextBuyLot,
NextSellLot;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   MinLot            = MarketInfo(Symbol(),MODE_MINLOT);
   MaxLot            = MarketInfo(Symbol(),MODE_MAXLOT);
   LotStep           = MarketInfo(Symbol(),MODE_LOTSTEP);
   DigitLot          = (int) MathLog10(MinLot)*-1;
   StopLevel         = MarketInfo(Symbol(),MODE_STOPLEVEL);
   TickValue         = MarketInfo(Symbol(),MODE_TICKVALUE);

   if(Digits==3 || Digits==5) DigitFactor=10;
   else DigitFactor=1;
   CreateDisplay();
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   RemoveObjects();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(IsTesting())
      if(OrdersTotal()==0)
        {
         BuyPrice=OpenMarketOrder(Lot,OP_BUY,"0Buy");
         SellPrice=OpenMarketOrder(Lot,OP_SELL,"0Buy");
         NextBuy=BuyPrice;
         NextSell=SellPrice;
         SumBuy=0;
         SumSell=0;
         NextBuyLot=Lot*LotExponential;
         NextSellLot=Lot*LotExponential;
        }

   if(TotalOrder()>0)
      CheckTP();

   if(TotalOrder()>0 && TotalOrder()<MaxTrades)
      AddTrade();
  }
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   if(id==CHARTEVENT_OBJECT_CLICK)
     {
      if(sparam==lbl+"Trade")
        {
         BuyPrice=OpenMarketOrder(Lot,OP_BUY,"0Buy");
         SellPrice=OpenMarketOrder(Lot,OP_SELL,"0Buy");
         NextBuy=BuyPrice;
         NextSell=SellPrice;
         SumBuy=0;
         SumSell=0;
         NextBuyLot=Lot*LotExponential;
         NextSellLot=Lot*LotExponential;
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
        }
      if(sparam==lbl+"CloseTrade")
        {
         CloseAllTrade();
         ObjectSetInteger(0,sparam,OBJPROP_STATE,false);
        }
     }
  }
//+------------------------------------------------------------------+
//| Check Total Order Function                                       |
//+------------------------------------------------------------------+
int TotalOrder()
  {
   int Result=0;
   for(int cnt=OrdersTotal()-1; cnt>=0; cnt--)
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol() && 
            OrderMagicNumber()==MagicNumber)
            Result++;
   return(Result);
  }
//+------------------------------------------------------------------+
//| Check Total Order Function                                       |
//+------------------------------------------------------------------+
int TotalOrderType(int Sum,string Type)
  {
   int Result=0;
   for(int cnt=OrdersTotal()-1; cnt>=0; cnt--)
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol() && 
            OrderMagicNumber()==MagicNumber)
            if(NumberComment(OrderComment(),Type)==Sum)
               Result++;
   return(Result);
  }
//+------------------------------------------------------------------+
//| Check Type Function                                              |
//+------------------------------------------------------------------+
int CheckType()
  {
   int Result=-1;
   for(int cnt=OrdersTotal()-1; cnt>=0; cnt--)
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol() && 
            OrderMagicNumber()==MagicNumber)
            Result=OrderType();
   return(Result);
  }
//+------------------------------------------------------------------+
//| Add Trade Function                                               |
//+------------------------------------------------------------------+
void AddTrade()
  {
   int Sum=0;
   string Type="";
   if(SumBuy==0 && SumSell==0)
     {
      Sum=0;
      Type="Buy";

      if(TotalOrderType(Sum,Type)==1)
        {
         if(CheckType()==1)
           {
            SumBuy++;
            BuyPrice=OpenMarketOrder(NextBuyLot,OP_BUY,IntegerToString(SumBuy)+"Buy");
            SellPrice=OpenMarketOrder(NextBuyLot,OP_SELL,IntegerToString(SumBuy)+"Buy");
            NextBuyLot=NextBuyLot*LotExponential;
           }
         if(CheckType()==0)
           {
            SumSell++;
            BuyPrice=OpenMarketOrder(NextSellLot,OP_BUY,IntegerToString(SumSell)+"Sell");
            SellPrice=OpenMarketOrder(NextSellLot,OP_SELL,IntegerToString(SumSell)+"Sell");
            NextSellLot=NextSellLot*LotExponential;
           }
        }
     }
   if(SumBuy>0 || SumSell>0)
     {
      if(SumBuy>SumSell)
        {
         Sum=SumBuy;
         Type="Buy";
        }
      else
        {
         Sum=SumSell;
         Type="Sell";
        }

      if(TotalOrderType(Sum,Type)==1)
        {
         if(Type=="Buy")
           {
            SumBuy++;
            BuyPrice=OpenMarketOrder(NextBuyLot,OP_BUY,IntegerToString(SumBuy)+"Buy");
            SellPrice=OpenMarketOrder(NextBuyLot,OP_SELL,IntegerToString(SumBuy)+"Buy");
            NextBuyLot=NextBuyLot*LotExponential;
           }
         if(Type=="Sell")
           {
            SumSell++;
            BuyPrice=OpenMarketOrder(NextSellLot,OP_BUY,IntegerToString(SumSell)+"Sell");
            SellPrice=OpenMarketOrder(NextSellLot,OP_SELL,IntegerToString(SumSell)+"Sell");
            NextSellLot=NextSellLot*LotExponential;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Check TP Function                                                |
//+------------------------------------------------------------------+
void CheckTP()
  {
   int OpenBuy=0;
   int OpenSell= 0;
   int TypeBuy = -1;
   int TypeSell = -1;
   for( int cnt = OrdersTotal()-1; cnt >= 0; cnt-- )
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
         if(OrderSymbol()==Symbol() && 
            OrderMagicNumber()==MagicNumber)
           {
            if(OrderType()<=1 && SumBuy>0)
              {
               if(StringFind(OrderComment(),"Buy",0)>=0 && NumberComment(OrderComment(),"Buy")==SumBuy)
                 {
                  TypeBuy=OrderType();
                  OpenBuy++;
                 }
              }
            if(OrderType()<=1 && SumSell>0)
              {
               if(StringFind(OrderComment(),"Sell",0)>=0 && NumberComment(OrderComment(),"Sell")==SumSell)
                 {
                  TypeSell=OrderType();
                  OpenSell++;
                 }
              }
           }
   if(OpenBuy==1 && TypeBuy==OP_BUY)
     {
      CloseAllTrade();
     }

   if(OpenSell==1 && TypeSell==OP_SELL)
     {
      CloseAllTrade();
     }
  }
//+------------------------------------------------------------------+
//| Create Display Function                                          |
//+------------------------------------------------------------------+
void CreateDisplay()
  {
   RectLabelCreate(lbl+"Rect",XValue-3,YValue-3,XSize*1+6,YSize*3+6);
   EditCreate(lbl+"Header",XValue,YValue,XSize,YSize,"Grid EA");
   ButtonCreate(lbl+"Trade",XValue,YValue+YSize,XSize,YSize,"Trade");
   ButtonCreate(lbl+"CloseTrade",XValue,YValue+YSize*2,XSize,YSize,"Close All Trade");
  }
//+------------------------------------------------------------------+
//| Create Edit object                                               |
//+------------------------------------------------------------------+
void EditCreate(const string           name        = "Edit",            // object name
                const int              x           = 0,                 // X coordinate
                const int              y           = 0,                 // Y coordinate
                const int              xsize       = 0,                 // X size
                const int              ysize       = 0,                 // Y size
                const string           text        = "Text")            // text
  {
   ObjectCreate(0,name,OBJ_EDIT,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,xsize);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,ysize);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   ObjectSetString(0,name,OBJPROP_FONT,Font);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,FontSize);
   ObjectSetInteger(0,name,OBJPROP_ALIGN,TextAlign);
   ObjectSetInteger(0,name,OBJPROP_READONLY,true);
   ObjectSetInteger(0,name,OBJPROP_CORNER,ChartCorner);
   ObjectSetInteger(0,name,OBJPROP_COLOR,TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,BackColor);
   ObjectSetInteger(0,name,OBJPROP_BORDER_COLOR,BorderColor);
   ObjectSetInteger(0,name,OBJPROP_BACK,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,0);
  }
//+------------------------------------------------------------------+
//| Create the button                                                |
//+------------------------------------------------------------------+
void ButtonCreate(const string   name        = "Button",    // button name
                  const int      x           = 0,           // X coordinate
                  const int      y           = 0,           // Y coordinate
                  const int      width       = 50,          // button width
                  const int      height      = 18,          // button height
                  const string   text        = "Button")    // text
  {
   ObjectCreate(0,name,OBJ_BUTTON,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,height);
   ObjectSetInteger(0,name,OBJPROP_CORNER,ChartCorner);
   ObjectSetString(0,name,OBJPROP_TEXT,text);
   ObjectSetString(0,name,OBJPROP_FONT,Font);
   ObjectSetInteger(0,name,OBJPROP_FONTSIZE,FontSize);
   ObjectSetInteger(0,name,OBJPROP_COLOR,TextColor);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,ButtonColor);
   ObjectSetInteger(0,name,OBJPROP_BORDER_COLOR,BorderColor);
   ObjectSetInteger(0,name,OBJPROP_BACK,false);
   ObjectSetInteger(0,name,OBJPROP_STATE,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_HIDDEN,false);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,0);
  }
//+------------------------------------------------------------------+
//| Create rectangle label                                           |
//+------------------------------------------------------------------+
bool RectLabelCreate(const string           name="RectLabel",         // label name
                     const int              x=0,                      // X coordinate
                     const int              y=0,                      // Y coordinate
                     const int              width=50,                 // width
                     const int              height=18)                // height
  {
   ObjectCreate(0,name,OBJ_RECTANGLE_LABEL,0,0,0);
   ObjectSetInteger(0,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(0,name,OBJPROP_YDISTANCE,y);
   ObjectSetInteger(0,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(0,name,OBJPROP_YSIZE,height);
   ObjectSetInteger(0,name,OBJPROP_BGCOLOR,BackColor);
   ObjectSetInteger(0,name,OBJPROP_FILL,true);
   ObjectSetInteger(0,name,OBJPROP_BORDER_TYPE,BORDER_FLAT);
   ObjectSetInteger(0,name,OBJPROP_CORNER,ChartCorner);
   ObjectSetInteger(0,name,OBJPROP_COLOR,RectangleColor);
   ObjectSetInteger(0,name,OBJPROP_STYLE,STYLE_SOLID);
   ObjectSetInteger(0,name,OBJPROP_WIDTH,3);
   ObjectSetInteger(0,name,OBJPROP_BACK,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTABLE,false);
   ObjectSetInteger(0,name,OBJPROP_SELECTED,false);
   ObjectSetInteger(0,name,OBJPROP_HIDDEN,true);
   ObjectSetInteger(0,name,OBJPROP_ZORDER,0);
   return(true);
  }
//+------------------------------------------------------------------+
//| Remove object function                                           |
//+------------------------------------------------------------------+  
void RemoveObjects()
  {
   for(int i=ObjectsTotal()-1; i>=0; i--)
      if(StringFind(ObjectName(i),lbl)!=-1) ObjectDelete(ObjectName(i));
  }
//+------------------------------------------------------------------+
//| Open Market Order Function                                       |
//+------------------------------------------------------------------+
double OpenMarketOrder(double LotSize,int TypeOrder,string Comm)
  {
   double   OrderPrice     = 0;
   double   SLPrice        = 0;
   double   TPPrice        = 0;
   int      ticket         = 0;
   while(IsTradeContextBusy()) Sleep(100);
   if(TypeOrder==OP_BUY)
     {
      OrderPrice     = Ask;
      TPPrice        = OrderPrice + TakeProfit*DigitFactor*Point;
     }
   if(TypeOrder==OP_SELL)
     {
      OrderPrice     = Bid;
      TPPrice        = OrderPrice - TakeProfit*DigitFactor*Point;
     }

   LotSize=NormalizeDouble(LotSize,DigitLot);
   OrderPrice=NormalizeDouble(OrderPrice,Digits);
   SLPrice = NormalizeDouble(SLPrice,Digits);
   TPPrice = NormalizeDouble(TPPrice,Digits);

   ticket = OrderSend(Symbol(),      // Symbol
                      TypeOrder,     // Operation type
                      LotSize,       // Lot size
                      OrderPrice,    // Latest price for buy
                      Slippage,      // Slippage
                      SLPrice,       // Stop loss
                      TPPrice,       // Take profit
                      Comm,          // Comment
                      MagicNumber,   // Magic number
                      0,             // Expiration
                      clrNONE);
   if(ticket>0)
      if(OrderSelect(ticket,SELECT_BY_TICKET))
         if(OrderTakeProfit()==0 && TakeProfit>0)
           {
            if(OrderModify(OrderTicket(),
               OrderOpenPrice(),
               SLPrice,          // Stop loss
               TPPrice,          // Take profit
               0,                // Expiration
               clrNONE)) ticket=ticket;
            OrderPrice=OrderTakeProfit();
           }
   if(ticket<0) OrderPrice=0;
   return(OrderPrice);
  }
//+------------------------------------------------------------------+
//| Close All Trade Function                                         |
//+------------------------------------------------------------------+
void CloseAllTrade()
  {
   bool result=false;
   while(IsTradeContextBusy())
      Sleep(100);

   for(int cnt=OrdersTotal()-1; cnt>=0; cnt--)
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES))
        {
         if(OrderMagicNumber()==MagicNumber && 
            OrderSymbol()==Symbol())
           {
            if(OrderType()==OP_BUY ||
               OrderType()==OP_SELL)
               if(OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),10,clrNONE)) result=true;
            if(OrderType()==OP_BUYLIMIT || 
               OrderType() == OP_BUYSTOP ||
               OrderType() == OP_SELLSTOP ||
               OrderType() == OP_SELLLIMIT )
               if(OrderDelete(OrderTicket())) result=true;
           }
        }
  }
//+------------------------------------------------------------------+
//| Number Comment function                                          |
//+------------------------------------------------------------------+   
int NumberComment(string Text,string BuySell)
  {
   int Pos=StringFind(Text,BuySell,0);
   string Match=StringSubstr(Text,0,Pos);
   int Result=(int)StringToInteger(Match);
   return(Result);
  }
//+------------------------------------------------------------------+
