//+------------------------------------------------------------------+
//|                              Coensio Swing Trader V06 CSTV06.mq4 |
//|                                         © Copyright 2014 Coesnio |
//|                                                  www.coensio.com |
//| Changelog@26092014:                                              |
//| + Added BreakEven feature                                        |
//| * Fixed EntryLine weekend crossing issue                         |
//| - Removed weekend close option                                   | 
//+------------------------------------------------------------------+
#property copyright "Coensio"
#property link      "www.coensio.com"

#include <stdlib.mqh>
#include <WinUser32.mqh>

//External variables
extern int        MagicNr              = 3141592;
extern int        StopLoss             = 50;    
extern int        TakeProfit           = 80;       
extern double     Lots                 = 0.05;  //Fixed lot size, set to 0 to use proportional risk management set by RiskMax
extern double     RiskMax              = 0;     //Max risk per trade in %
extern int        Slipage              = 3;     //Slippage in pips
extern int        EntryThreshold       = 15;    //Entry threshold in pips
extern bool       EnableTrailing       = false; 
extern int        BreakEvenPips        = 25;    //BreakEven level in pips
extern int        TrailingStep         = 5;     //Trailing stoploss step
extern bool       FalseBreakClose      = false; //Trade will close when entry candle closes in the wrong direction

//Internal variables
int               Build                = 4;          
int               Slip                 = 0;               
int               Ticket               = 0;  
double            TP                   = 0;
double            SL                   = 0;
double            tp                   = 0;
double            sl                   = 0;
double            Pip                  = 0;
double            GS                   = -1000;
double            GL                   = 1000;
bool              BreakEvenOk          = false;  
bool              BuySig               = false;
bool              SellSig              = false;

//Static variables
static bool       Started              = false;
static double     EntryPrice           = 0; 
static int        Barz                 = 0;
static bool       TradingLong          = false;
static bool       TradingShort         = false;

int OnInit()
{
   if(!Started)
   {
      //Set Pip value
      if(Digits==2 || Digits==4) { Pip = 1*Point; Slip=1*Slipage;}
      if(Digits==3 || Digits==5) { Pip = 10*Point; Slip=10*Slipage;}
      if(Digits==6)              { Pip = 100*Point; Slip=100*Slipage;}
      //Clear old objects
      //ClearObjects();    
      Started=true;
   }
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
      return(0);
   }
   else
   {
      Comment("Let's make some money!");
   }
   //Find TL break signals         
   if(ObjectFind("gl")==0)
   {
      GL = NormalizeDouble(ObjectGetValueByTime(0,"gl",Time[0],OBJ_TREND),Digits);
      if(Ask>GL+EntryThreshold*Pip)
      {
         BuySig=true;
         SellSig=false;
      }
   }
   else
      GL=1000;    
   
   if(ObjectFind("gs")==0)
   {  
      GS = NormalizeDouble(ObjectGetValueByTime(0,"gs",Time[0],OBJ_TREND),Digits);
      if(Bid<GS-EntryThreshold*Pip)
      {
         BuySig=false;
         SellSig=true;
      }
   }
   else
      GS=-1000; 
      
   //Process entry signals 
   if(BuySig)
   if(!OpenOrders) 
   OpenBuy();   
   if(SellSig)
   if(!OpenOrders) 
   OpenSell();
   
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

   //Handle BreakEven
   if(BreakEvenPips>0)
   if(!BreakEvenOk)
   if(OpenOrders)
   BreakEven(); 
      
   return(0);
}

bool OpenBuy()
{
   SL = Ask-(StopLoss*Pip);
   TP = Ask+(TakeProfit*Pip);
   GetLotSize(StopLoss);   
   Ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slip,NormalizeDouble(SL,Digits),NormalizeDouble(TP,Digits),"",MagicNr,0,Blue);
   if (Ticket==-1)
   {
      Alert("Error  " ,GetLastError()," ",Symbol());
      return(false);
   }
   else
   {
      UpArrow(Low[0], 0, Low[0]-5*Pip, Blue);
      EntryPrice = Ask;
      if(ObjectFind("gs")==0) ObjectDelete("gs");
      DrawTrendLine("gl"+Time[0],Ask,Time[5],Ask,Time[0]+(Period()*6*60),Lime,2);
      DrawLine("tp",TP,Blue);
      DrawLine("sl",SL,Red);
      if(ObjectFind("gl")==0) ObjectDelete("gl");
      Barz=Bars;
      return(true);
   }
}

bool OpenSell()
{
   SL = Bid+(StopLoss*Pip);
   TP = Bid-(TakeProfit*Pip);
   GetLotSize(StopLoss);    
   Ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slip,NormalizeDouble(SL,Digits),NormalizeDouble(TP,Digits),"",MagicNr,0,Red);  
   if (Ticket==-1)
   {
      Alert("Error  " ,GetLastError()," ",Symbol());
      return(false);
   } 
   else
   {
      DownArrow(High[0], 0, High[0]+20*Pip, Red);
      EntryPrice = Bid;
      if(ObjectFind("gl")==0) ObjectDelete("gl");
      DrawTrendLine("gs"+Time[0],Bid,Time[5],Bid,Time[0]+(Period()*6*60),Lime,2);
      DrawLine("tp",TP,Blue);
      DrawLine("sl",SL,Red);
      if(ObjectFind("gs")==0) ObjectDelete("gs");
      Barz=Bars;
      return(true);
   }
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
      if(OrderMagicNumber()==MagicNr)
      {
         if(OrderType()==OP_BUY) TradingLong=true;
         if(OrderType()==OP_SELL) TradingShort=true;
         return(true);
      }
   }
   
   TradingLong = false;
   TradingShort = false;
   BreakEvenOk = false;
   GS = -1000;
   GL = 1000;   
   return(false);
}

void CheckOrderLevels()
{
   int Total=OrdersTotal();
   bool OrdersFound = false;
   //Get current levels and change them
   if(ObjectFind("tp")==0)
   {
      tp = NormalizeDouble(ObjectGet( "tp", OBJPROP_PRICE1),Digits);
   }
   if(ObjectFind("sl")==0)
   {
      sl = NormalizeDouble(ObjectGet( "sl", OBJPROP_PRICE1),Digits);
   }
   
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderMagicNumber()==MagicNr)
         {
            OrdersFound = true;
            if(tp!=0)
            if(NormalizeDouble(OrderTakeProfit(),Digits)!=tp)
            {
               Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderStopLoss(),NormalizeDouble(tp,Digits),0,Blue);
               Print("TP changed:", DoubleToStr(tp,3));
            }
            if(sl!=0)
            if(NormalizeDouble(OrderStopLoss(),Digits)!=sl)
            {
               //Change SL only when Trailing dissabled!
               if(!EnableTrailing) 
               Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(sl,Digits),OrderTakeProfit(),0,Red);
               Print("SL changed:", DoubleToStr(sl,3));
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
         if(OrderType()==OP_BUY && OrderMagicNumber()==MagicNr && OrderSymbol()==Symbol())
         {
            if((Bid-OrderStopLoss())/(Pip)>StopLoss+TrailingStep)
            {
               SL = Bid-(StopLoss*Pip);
               Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,Digits),OrderTakeProfit(),0,Blue);
               if(ObjectDelete("sl")) DrawLine("sl",SL,Red);
            }
         }
         if(OrderType()==OP_SELL && OrderMagicNumber()==MagicNr && OrderSymbol()==Symbol())
         {
            if((OrderStopLoss()-Ask)/(Pip)>StopLoss+TrailingStep)
            {
               SL = Ask+(StopLoss*Pip); 
               Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(SL,Digits),OrderTakeProfit(),0,Red);
               if(ObjectDelete("sl")) DrawLine("sl",SL,Red);
            }
         }
      }  
   }
   return;
}
void BreakEven()
{
   for(int j=OrdersTotal()-1; j>=0; j--)
   {
      if(OrderSelect(j, SELECT_BY_POS,MODE_TRADES))
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNr)
      { 
         if(OrderType()==OP_BUY)
         {
            if((Bid-OrderOpenPrice())/Pip > BreakEvenPips)
            { 
               if(sl!=OrderOpenPrice())
               {
                  Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Purple);
                  if(ObjectDelete("sl")) DrawLine("sl",OrderOpenPrice(),Red);
                  BreakEvenOk=true;
               }
            }
         }
         if(OrderType()==OP_SELL)
         {
            if((OrderOpenPrice()-Ask)/Pip > BreakEvenPips)
            {
               if(sl!=OrderOpenPrice())
               {
                  Ticket=OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Purple);
                  if(ObjectDelete("sl")) DrawLine("sl",OrderOpenPrice(),Red);
                  BreakEvenOk=true;
               }
            }
         }            
      }
   }  
}

void CloseBuy()
{
   int Total=OrdersTotal();
   for(int i=Total-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_TRADES))
      {
         if(OrderType()==OP_BUY && OrderMagicNumber()==MagicNr)
         {
            Ticket=OrderClose(OrderTicket(),OrderLots(),Bid,SL,Purple);
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
         if(OrderType()==OP_SELL && OrderMagicNumber()==MagicNr)
         {
            Ticket=OrderClose(OrderTicket(),OrderLots(),Ask,SL,Purple);
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

double CountPairProfit()
{
   double TotalProfit=0;
   for(int i=OrdersHistoryTotal()-1; i>=0; i--)
   {
      if(OrderSelect(i, SELECT_BY_POS,MODE_HISTORY))
      if(OrderMagicNumber()==MagicNr && OrderSymbol()==Symbol()) TotalProfit=TotalProfit+(OrderProfit()+OrderSwap()+OrderCommission());     
   }
   return(TotalProfit);
}

void DisplayLabels()
{
   //Display labels
   DrawLabel("Name",WindowExpertName()+" Build:"+Build+" (Swing Trader)",10,10,Yellow,8);
   DrawLabel("Info","© Copyright 2014 www.Coensio.com",10,30,Yellow,8);
   //Right side info
   DrawLabel("MAGIC","MagicNr: ",10,50,Yellow,8);
   DrawLabel("MagicVal",DoubleToStr(MagicNr,0),70,50,Yellow,8);   
   DrawLabel("Balance","Balance: ",10,60,Yellow,8);
   DrawLabel("BalanceVal",DoubleToStr(AccountBalance(),0),70,60,Yellow,8);
   DrawLabel("Profit","Profit: ",10,70,Yellow,8);
   DrawLabel("ProfitVal",DoubleToStr(CountPairProfit(),1),70,70,Yellow,8); 
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