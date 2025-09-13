//+------------------------------------------------------------------+
//|                              Coensio Swing Trader V05 CSTV05.mq4 |
//|                                         © Copyright 2014 Coesnio |
//|                                                  www.coensio.com |
//| Changelog@07202014:                                              |
//| + Added automatic magic number generation                        |
//| * Fixed DrawTrendLine bug at buy entry                           |
//| * Fixed trailing stop bug                                        |
//| * Fixed false breakout detection bug                             |   
//+------------------------------------------------------------------+
#property copyright "Coensio"
#property link      "www.coensio.com"

#include <stdlib.mqh>
#include <WinUser32.mqh>

//External variables
extern int        MAGICMA              = 3141592;
extern int        StopLoss             = 50;    
extern int        TakeProfit           = 100;       
extern double     Lots                 = 0;     //Fixed lot size, set to 0 to use proportional risk management set by RiskMax
extern double     RiskMax              = 2;     //Max risk per trade in %
extern int        Slipage              = 3;     //Slippage in pips
extern int        EntryThreshold       = 15;    //Entry threshold in pips
extern bool       EnableTrailing       = false; 
extern int        TrailingStep         = 5;     //Trailing stoploss step
extern bool       FalseBreakClose      = true;  //Trade will close when entry candle closes in the wrong direction

//Internal variables          
int               Slip                 = 0;               
int               Ticket               = 0;  
double            SL,TP;
double            Pip                  = 0;
double            x1,y1,x2,y2,a_s,a_b,b_b,b_s,time;
bool              BuySig               = false;
bool              SellSig              = false;

//Static variables
static bool       Started              = false;
static double     EntryPrice           = 0; 
static int        Barz                 = 0;
static bool       TradingLong          = false;
static bool       TradingShort         = false;

int init()
{
   if(!Started)
   {
      //Set Pip value
      if(Digits==2 || Digits==4) { Pip = 1*Point; Slip=1*Slipage;}
      if(Digits==3 || Digits==5) { Pip = 10*Point; Slip=10*Slipage;}
      if(Digits==6)              { Pip = 100*Point; Slip=100*Slipage;}
      //Clear old objects
      ClearObjects();
      //Set Magic number
      MathSrand(TimeCurrent());
      MAGICMA=MathAbs(MAGICMA*MathRand()*Period()*StringGetChar(Symbol(),1)*StringGetChar(Symbol(),3));
      MAGICMA=StrToDouble(StringSubstr(DoubleToStr(MAGICMA,0),1,6));       
      Started=true;
   }
   return(0);
}

int deinit()
{
   return(0);
}

int start()
{
   DisplayLabels();
   BuySig=false;
   SellSig=false;
   bool OpenOrders=CheckOpenOrders();
   if(DayOfWeek()==0 || DayOfWeek()==6)
   {
      Comment("It is weekend! Take a break!");
      return;
   }
   else
   {
      Comment("Let's make some money!");
   }
   //Find TL break signals         
   if(ObjectFind("gl")==0)
   {
      //Get 4 coordinates
      x1 = ObjectGet( "gl", OBJPROP_TIME1);
      y1 = ObjectGet( "gl", OBJPROP_PRICE1);
      x2 = ObjectGet( "gl", OBJPROP_TIME2);
      y2 = ObjectGet( "gl", OBJPROP_PRICE2);
      //Check if Trend Line is going over the weekend -> results in a calculation error
      int shift_gl=iBarShift(Symbol(),Period(),x1);   
      for(int z_gl=0; z_gl<=shift_gl; z_gl++)
      {
         if(TimeDayOfWeek(Time[z_gl])==0 || TimeDayOfWeek(Time[z_gl])==6) //If Sunday or Satruday 
         {
            Alert("Move your Trend Line pointer closer to current date!");
            ObjectDelete("gl");
            PlaySound("Error.wav");
            return;
         }
      }
      //Calculate the slope of the line (a)
      a_b = (y2-y1)/(x2-x1);
      //Calcualte the offset (b)
      b_b = y1 -a_b*x1;
      //Get the current x value
      time = TimeCurrent();
      //Calculate the value (y) of the projected trendline at (x): y = ax + b
      double BuyValue = a_b*time + b_b;
      //Comment("Lots: ", Lots ," Distance: ", (BuyValue-Ask)/Pip);
      if(Ask>BuyValue+EntryThreshold*Pip)
      {
         BuySig=true;
         SellSig=false;
      }
   }
   if(ObjectFind("gs")==0)
   {
      //Get the four coordinates
      x1 = ObjectGet( "gs", OBJPROP_TIME1);
      y1 = ObjectGet( "gs", OBJPROP_PRICE1);
      x2 = ObjectGet( "gs", OBJPROP_TIME2);
      y2 = ObjectGet( "gs", OBJPROP_PRICE2);
      //Check if Trend Line is going over the weekend -> results in calculation error
      int shift_gs=iBarShift(Symbol(),Period(),x1);   
      for(int z_gs=0; z_gs<=shift_gs; z_gs++)
      {
         if(TimeDayOfWeek(Time[z_gs])==6 || TimeDayOfWeek(Time[z_gs])==0) //If Sunday or Satruday 
         {
            Alert("Move your Trend Line pointer closer to current date!");
            ObjectDelete("gs");
            PlaySound("Error.wav");
            return;
         }
      }
      //Calculate the slope of the line (a)
      a_s = (y2-y1)/(x2-x1);
      //Calcualte the offset (b)
      b_s = y1 -a_s*x1;
      //Get the current x value
      time = TimeCurrent();
      //Calculate the value (y) of the projected trendline at (x): y = ax + b
      double SellValue = a_s*time + b_s;
      //Comment("Lots: ", Lots ," Distance: ", (Bid-SellValue)/Pip);
      if(Bid<SellValue-EntryThreshold*Pip)
      {
         BuySig=false;
         SellSig=true;
      }
   }
   //Process entry signals 
   if(BuySig)
   if(!OpenOrders) 
   {
      SL = Ask-(StopLoss*Pip);
      TP = Ask+(TakeProfit*Pip);
      GetLotSize(StopLoss);   
      Ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slip,NormalizeDouble(SL,Digits),NormalizeDouble(TP,Digits),"",MAGICMA,0,Blue);
      if (Ticket==-1)
      {
         Alert("Error  " ,GetLastError()," ",Symbol());
         return; 
      }
      UpArrow(Low[0], 0, Low[0]-5*Pip, Blue);
      EntryPrice = Bid;
      if(ObjectFind("gs")==0) ObjectDelete("gs");
      DrawTrendLine("gl"+MathRand(),ObjectGet("gl",OBJPROP_PRICE1),ObjectGet("gl",OBJPROP_TIME1),(a_b*(Time[0]+(Period()*3*60)) + b_b),(Time[0]+(Period()*3*60)),Blue,2);
      DrawLine("tp",TP,Blue);
      DrawLine("sl",SL,Red);
      ObjectDelete("gl");
      Barz=Bars;
   }
   
   if(SellSig)
   if(!OpenOrders) 
   {
      SL = Bid+(StopLoss*Pip);
      TP = Bid-(TakeProfit*Pip);
      GetLotSize(StopLoss);    
      Ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slip,NormalizeDouble(SL,Digits),NormalizeDouble(TP,Digits),"",MAGICMA,0,Red);  
      if (Ticket==-1)
      {
         Alert("Error  " ,GetLastError()," ",Symbol());
         return; 
      } 
      DownArrow(High[0], 0, High[0]+20*Pip, Red);
      EntryPrice = Ask;
      if(ObjectFind("gl")==0) ObjectDelete("gl");
      DrawTrendLine("gs"+MathRand(),ObjectGet("gs",OBJPROP_PRICE1),ObjectGet("gs",OBJPROP_TIME1),(a_s*(Time[0]+(Period()*3*60)) + b_s),(Time[0]+(Period()*3*60)),Red,2);
      DrawLine("tp",TP,Blue);
      DrawLine("sl",SL,Red);
      ObjectDelete("gs");
      Barz=Bars;
   }
   
   //Handle False breaks
   if(FalseBreakClose)
   if(OpenOrders)
   if(Barz+1==Bars)
   {
      if(TradingLong)
      if(Close[1]<EntryPrice-(EntryThreshold*Pip))
      //if(Bid<EntryPrice-(EntryThreshold*Pip))
      {
         CloseBuy();
         Comment("Trade closed due to false break!");  
      }
      if(TradingShort)
      if(Close[1]>EntryPrice+(EntryThreshold*Pip))
      //if(Ask>EntryPrice+(EntryThreshold*Pip))
      {
         CloseSell();
         Comment("Trade closed due to false break!");  
      }        
   }
   //Handle trailing stop
   if(EnableTrailing) CheckOrdersTrailing();
   //Check TP and SL levels
   CheckOrderLevels();
   //We close our trades on Friday
   if(DayOfWeek()==5 && TimeHour(TimeCurrent())>23)
   {
      CloseBuy();
      CloseSell();
      Comment("All trades closed! Have a nice weekend!");  
   }
   
   return(0);
}

void GetLotSize(int StopLossPips)
{
   //Proportional or fixed risk management
   if(Lots==0 && RiskMax>0) Lots = NormalizeDouble(((RiskMax/100)*AccountEquity()/StopLossPips) / (Pip/Point),2); 
   if(Lots<MarketInfo(Symbol(), MODE_MINLOT)) Lots=MarketInfo(Symbol(), MODE_MINLOT);
   return;   
}

bool CheckOpenOrders()
{
   for(int i=OrdersTotal()-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      if(OrderMagicNumber()==MAGICMA)
      {
         if(OrderType()==OP_BUY) TradingLong=true;
         if(OrderType()==OP_SELL) TradingShort=true;
         return(true);
      }
   }
   
   TradingLong = false;
   TradingShort = false;
   return(false);
}

void CheckOrderLevels()
{
   int Total=OrdersTotal();
   bool OrdersFound = false;
   //Get current levels and change them
   if(ObjectFind("tp")==0)
   {
      double tp = NormalizeDouble(ObjectGet( "tp", OBJPROP_PRICE1),Digits);
   }
   if(ObjectFind("sl")==0)
   {
      double sl = NormalizeDouble(ObjectGet( "sl", OBJPROP_PRICE1),Digits);
   }
   
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderMagicNumber()==MAGICMA)
         {
            OrdersFound = true;
            if(tp!=0)
            if(NormalizeDouble(OrderTakeProfit(),Digits)!=tp)
            {
               OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(tp,Digits),0,Blue);
               //Print("TP changed:", DoubleToStr(tp,3));
            }
            if(sl!=0)
            if(NormalizeDouble(OrderStopLoss(),Digits)!=sl)
            {
               //Change SL only when Trailing dissabled!
               if(!EnableTrailing) 
               OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Red);
               //Print("SL changed:", DoubleToStr(sl,3));
            }
         }    
      }  
   }
   if(!OrdersFound) 
   { 
      ObjectDelete("tp");
      ObjectDelete("sl");
   } 
   return;
}

void CheckOrdersTrailing()
{ 
   int Total=OrdersTotal();
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderType()==OP_BUY && OrderMagicNumber()==MAGICMA)
         {
            if((Bid-OrderStopLoss())/(Pip)>StopLoss+TrailingStep)
            {
               SL = Bid-(StopLoss*Pip);
               OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,Digits),OrderTakeProfit(),0,Blue);
            }
         }
         if(OrderType()==OP_SELL && OrderMagicNumber()==MAGICMA)
         {
            if((OrderStopLoss()-Ask)/(Pip)>StopLoss+TrailingStep)
            {
               SL = Ask+(StopLoss*Pip); 
               OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,Digits),OrderTakeProfit(),0,Red);
            }
         }
      }  
   }
   return;
}

void CloseBuy()
{
   int Total=OrdersTotal();
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderType()==OP_BUY && OrderMagicNumber()==MAGICMA)
         {
            OrderClose(OrderTicket(),OrderLots(),Bid,SL,Purple);
            DrawDot(Bid+MathRand(), 0, Bid, Yellow);
         }
      }
   }
   return;
}

void CloseSell()
{
   int Total=OrdersTotal();
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderType()==OP_SELL && OrderMagicNumber()==MAGICMA)
         {
            OrderClose(OrderTicket(),OrderLots(),Ask,SL,Purple);
            DrawDot(Ask+MathRand(), 0, Ask, Yellow);
         }
      }
   }
   return;
}

void ClearObjects()
{
   ObjectsDeleteAll(0);
   return;
}

void DisplayLabels()
{
   //Display labels
   DrawLabel("Name",WindowExpertName()+": Coensio Swing Trader V04",10,20,Yellow,8);
   DrawLabel("Info","© Copyright 2014 www.Coensio.com",10,30,Yellow,8);
   //Right side info
   DrawLabel("MAGIC","MAGICNR: ",10,50,Yellow,8);
   DrawLabel("MagicVal",DoubleToStr(MAGICMA,0),70,50,Yellow,8);   
   DrawLabel("Balance","Balance: ",10,60,Yellow,8);
   DrawLabel("BalanceVal",DoubleToStr(AccountBalance(),0),70,60,Yellow,8);
   DrawLabel("Profit","Profit: ",10,70,Yellow,8);
   DrawLabel("ProfitVal",DoubleToStr(AccountEquity()-AccountBalance(),1),70,70,Yellow,8); 
   return;      
}

void DrawLabel(string STR="",string TEXT="",int X=0, int Y=0,color COLOR=Blue,int FONT_SIZE=5)
{
   if(ObjectFind(STR) == -1)
   {
      ObjectCreate(STR,OBJ_LABEL,0,0,0);
   }
   ObjectSetText(STR,TEXT,FONT_SIZE,"",COLOR);
   ObjectSet(STR,OBJPROP_CORNER, 2); 
   ObjectSet(STR,OBJPROP_XDISTANCE,X);
   ObjectSet(STR,OBJPROP_YDISTANCE,Y);
}  

void UpArrow(string Name, int Bar, double Level, color Color)
{
      ObjectCreate(Name, OBJ_ARROW, 0, Time[Bar], Level, 0);
      ObjectSet(Name, OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet(Name, OBJPROP_ARROWCODE, SYMBOL_ARROWUP);
      ObjectSet(Name, OBJPROP_WIDTH, 4);
      ObjectSet(Name, OBJPROP_COLOR, Color); 
      return;
}

void DownArrow(string Name, int Bar, double Level, color Color)
{
      ObjectCreate(Name, OBJ_ARROW, 0, Time[Bar], Level, 0);
      ObjectSet(Name, OBJPROP_STYLE, STYLE_SOLID);
      ObjectSet(Name, OBJPROP_ARROWCODE, SYMBOL_ARROWDOWN);
      ObjectSet(Name, OBJPROP_WIDTH, 4);
      ObjectSet(Name, OBJPROP_COLOR, Color);
      return;
}

void DrawDot(string Name, int Bar, double Level, color Color)
{
      ObjectCreate(Name, OBJ_ARROW, 0, Time[Bar], Level, 0);
      ObjectSet(Name, OBJPROP_STYLE, STYLE_DOT);
      ObjectSet(Name, OBJPROP_ARROWCODE, SYMBOL_STOPSIGN);
      ObjectSet(Name, OBJPROP_WIDTH, 2);
      ObjectSet(Name, OBJPROP_COLOR, Color); 
      return;
}

void DrawLine(string Name, double Level, color Color)
{
      ObjectCreate(Name, OBJ_HLINE, 0, 0, Level, 0);
      ObjectSet(Name, OBJPROP_COLOR, Color);
      ObjectSet(Name, OBJPROP_STYLE, 0);
      ObjectSet(Name, OBJPROP_WIDTH, 1);
      return;
}

void DrawTrendLine(string STR="",double PRICE1=0,datetime TIME1=0,double PRICE2=0,datetime TIME2=0,color COLOR=Orange,int WIDTH=1)
{
   if(ObjectFind(STR) == -1)
   {
      ObjectCreate(STR, OBJ_TREND, 0, TIME1, PRICE1, TIME2, PRICE2);       
      ObjectSet(STR, OBJPROP_WIDTH, WIDTH);
      ObjectSet(STR, OBJPROP_COLOR, COLOR);
      ObjectSet(STR, OBJPROP_RAY, false);
      ObjectSet(STR, OBJPROP_BACK, false);
      ObjectSet(STR, OBJPROP_STYLE,STYLE_DOT);
   }
}