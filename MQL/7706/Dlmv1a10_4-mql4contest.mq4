//+------------------------------------------------------------------+
//|                                          DLMv1.4-MQL4Contest.mq4 |
//|                              Copyright © 2006, Alejandro Galindo |
//|                                              http://elCactus.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, Alejandro Galindo"
#property link      "http://elCactus.com"
//----
int    TakeProfit=0;                         // Profit Goal for the latest order opened
double Lots=0.1;                             // We start with this lots number
int    StopLoss=0;                           // StopLoss
int    TrailingStop=0;                       // Pips to trail the StopLoss
int MaxTrades=5;                             // Maximum number of orders to open
int Pips=15;                                 // Distance in Pips from one order to another
bool SecureProfitProtection=False;
int SecureProfit=20;                         // If profit made is bigger than SecureProfit we close the orders
int OrderstoProtect=3;                       // Number of orders to enable the account protection
bool AllSymbolsProtect=False;                // if one will check profit from all symbols, if cero only this symbol
bool EquityProtection=True;                  // if true, then the expert will protect the account equity to the percent specified
int AccountEquityPercentProtection=90;       // percent of the account to protect on a set of trades
bool AccountMoneyProtection=False;
double AccountMoneyProtectionValue=3000.00;
bool TradeOnFriday=True;
int OrdersTimeAlive=0;                       // in seconds
bool ReverseCondition=False;                 // if one the desition to go long/short will be reversed
bool SetLimitOrders=True;                    // if true, instead open market orders it will open limit orders 
                                             // (from order 2, not from first order
int mm=0;                                    // if one the lots size will increase based on account size
int risk=12;                                 // risk to calculate the lots size (only if mm is enabled)
int AccountType=1;                           // 0 if Normal Lots, 1 for mini lots, 2 for micro lots
int MagicNumber=222777;                      // Magic number for the orders placed
int Manual=0;                                // If set to one then it will not open trades automatically
int OpenOrdersBasedOn=16;                    // Method to decide if we start long or short
bool ExitWithOpenOrdersBasedON=True;
color ArrowsColor=Black;                     // color for the orders arrows
int  OpenOrders=0, cnt=0;
int MarketOpenOrders=0, LimitOpenOrders=0;
int  slippage=5;
double sl=0, tp=0;
double BuyPrice=0, SellPrice=0;
double lotsi=0, mylotsi=0;
int mode=0, myOrderType=0, myOrderTypetmp=0;
bool ContinueOpening=True;
double LastPrice=0;
int  PreviousOpenOrders=0;
double Profit=0;
int LastTicket=0, LastType=0;
double LastClosePrice=0, LastLots=0;
double Pivot=0;
double PipValue=0;
bool Reversed=False;
double tmp=0;
double iTmpH=0;
double iTmpL=0;
datetime NonTradingTime[][2];
bool FileReaded=false;
string dateLimit="2030.01.12 23:00";
int CurTimeOpeningFlag=0;
datetime LastOrderOpenTime=0;
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
   DeleteAllObjects();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   int cnt=0;
   bool result;
   string text="";
   string version="This version has expired. Alejandro Galindo. http://elCactus.com";
//----
   if (AccountType==0)
     {
      if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000); }
      else { lotsi=Lots; }
     }
   if (AccountType==1)
     {  // then is mini
      if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000)/10; }
      else { lotsi=Lots; }
     }
   if (AccountType==2)
     {
      if (mm!=0) { lotsi=MathCeil(AccountBalance()*risk/10000)/100; }
      else { lotsi=Lots; }
     }
   if (lotsi<0.01) lotsi=0.01;
   if (lotsi>100) lotsi=100;
//----
   OpenOrders=0;
   MarketOpenOrders=0;
   LimitOpenOrders=0;
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
        {
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
           {
            if (OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               MarketOpenOrders++;
               LastOrderOpenTime=OrderOpenTime();
              }
            if (OrderType()==OP_SELLLIMIT || OrderType()==OP_BUYLIMIT) LimitOpenOrders++;
            OpenOrders++;
           }
        }
     }
   if (OpenOrders<1)
     {
      if (!TradeOnFriday && DayOfWeek()==5)
        {
         Comment("TradeOnfriday is False");
         return(0);
        }
     }
   PipValue=MarketInfo(Symbol(),MODE_TICKVALUE);
//----
   if (PipValue==0) { PipValue=5; }
   if (AccountMoneyProtection && AccountBalance()-AccountEquity()>=AccountMoneyProtectionValue)
     {
      text=text + "\nClosing orders because account money protection was triggered.";
      Print("Closing orders because account money protection was triggered. Balance: ",AccountBalance()," Equity: ",AccountEquity());
      PreviousOpenOrders=OpenOrders+1;
      ContinueOpening=False;
      return(0);
     }
   // Account equity protection 
   if (EquityProtection && AccountEquity()<=AccountBalance()*AccountEquityPercentProtection/100)
     {
      text=text + "\nClosing orders because account equity protection was triggered.";
      Print("Closing orders because account equity protection was triggered. Balance: ",AccountBalance()," Equity: ", AccountEquity());
      //Comment("Closing orders because account equity protection was triggered. Balance: ",AccountBalance()," Equity: ", AccountEquity());
      //OrderClose(LastTicket,LastLots,LastClosePrice,slippage,Orange);		 
      PreviousOpenOrders=OpenOrders+1;
      ContinueOpening=False;
      return(0);
     }
   // if dont trade at fridays then we close all
   if (!TradeOnFriday && DayOfWeek()==5)
     {
      PreviousOpenOrders=OpenOrders+1;
      ContinueOpening=False;
      text=text +"\nClosing all orders and stop trading because TradeOnFriday protection.";
      Print("Closing all orders and stop trading because TradeOnFriday protection.");
     }
   // Orders Time alive protection
   if (OrdersTimeAlive>0 && CurTime() - LastOrderOpenTime>OrdersTimeAlive)
     {
      PreviousOpenOrders=OpenOrders+1;
      ContinueOpening=False;
      text=text + "\nClosing all orders because OrdersTimeAlive protection.";
      Print("Closing all orders because OrdersTimeAlive protection.");
     }
   if (PreviousOpenOrders>OpenOrders)
     {
      for(cnt=OrdersTotal()-1;cnt>=0;cnt--)
        {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
           {
            mode=OrderType();
            if ((OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) || AllSymbolsProtect)
              {
               if (mode==OP_BUY || mode==OP_SELL)
                 {
                  OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage,ArrowsColor);
                  return(0);
                 }
              }
           }
        }
      for(cnt=0;cnt<OrdersTotal();cnt++)
        {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
           {
            mode=OrderType();
            if ((OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) || AllSymbolsProtect)
              {
               if (mode==OP_SELLLIMIT || mode==OP_BUYLIMIT || mode==OP_BUYSTOP || mode==OP_SELLSTOP)
                 {
                  OrderDelete(OrderTicket());
                  return(0);
                 }
              }
           }
        }
     }
   PreviousOpenOrders=OpenOrders;
//----
   if (OpenOrders>=MaxTrades)
     {
      ContinueOpening=False;
      }
       else 
      {
      ContinueOpening=True;
     }
   if (LastPrice==0)
     {
      for(cnt=0;cnt<OrdersTotal();cnt++)
        {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
           {
            mode=OrderType();
            if (OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber)
              {
               LastPrice=OrderOpenPrice();
               if (mode==OP_BUY) { myOrderType=2; }
               if (mode==OP_SELL) { myOrderType=1;   }
              }
           }
        }
     }
   // SecureProfit protection
   //if (SecureProfitProtection && MarketOpenOrders>=(MaxTrades-OrderstoProtect)
   // Modified to make easy to understand 
   if (SecureProfitProtection && MarketOpenOrders>=OrderstoProtect)
     {
      Profit=0;
      for(cnt=0;cnt<OrdersTotal();cnt++)
        {
         if (OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES))
           {
            if ((OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) || AllSymbolsProtect)
               Profit=Profit+OrderProfit();
           }
        }
      if (Profit>=SecureProfit)
        {
         text=text + "\nClosing orders because account protection with SecureProfit was triggered.";
         Print("Closing orders because account protection with SeureProfit was triggered. Balance: ",AccountBalance()," Equity: ", AccountEquity()," Profit: ",Profit);
         PreviousOpenOrders=OpenOrders+1;
         ContinueOpening=False;
         return(0);
        }
     }
   myOrderTypetmp=3;
   switch(OpenOrdersBasedOn)
     {
      case 16:
         myOrderTypetmp=OpenOrdersBasedOnFXFish2MA();
         break;
      default:
         myOrderTypetmp=OpenOrdersBasedOnFXFish2MA();
         break;
     }
   if (OpenOrders<1 && Manual==0)
     {
      myOrderType=myOrderTypetmp;
      if (ReverseCondition)
        {
         if (myOrderType==1) { myOrderType=2; }
         else 
         { if (myOrderType==2) { myOrderType=1; } }
        }
     }
   if (ReverseCondition)
     {
      if (myOrderTypetmp==1) { myOrderTypetmp=2; }
      else 
      { if (myOrderTypetmp==2) { myOrderTypetmp=1; } }
     }
   // if we have opened positions we take care of them
   cnt=OrdersTotal()-1;
   while(cnt>=0)
     {
      if(OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if (OrderSymbol()==Symbol() && OrderMagicNumber()==MagicNumber) // && Reversed==False) 
        {
         //Print("Ticket ",OrderTicket()," modified.");	     
         if (OrderType()==OP_SELL)
           {
            if (ExitWithOpenOrdersBasedON && myOrderTypetmp==2)
              {
               PreviousOpenOrders=OpenOrders+1;
               ContinueOpening=False;
               text=text +"\nClosing all orders because Indicator triggered another signal.";
               Print("Closing all orders because Indicator triggered another signal.");
               //return(0);
              }
            if (TrailingStop>0)
              {
               if ((OrderOpenPrice()-OrderClosePrice())>=(TrailingStop*Point+Pips*Point))
                 {
                  if (OrderStopLoss()>(OrderClosePrice()+TrailingStop*Point))
                    {
                     result=OrderModify(OrderTicket(),OrderOpenPrice(),OrderClosePrice()+TrailingStop*Point,OrderClosePrice()-TakeProfit*Point-TrailingStop*Point,0,Purple);
                     if(result!=TRUE) Print("LastError = ", GetLastError());
                     else OrderPrint();
                     return(0);
                    }
                 }
              }
           }
         if (OrderType()==OP_BUY)
           {
            if (ExitWithOpenOrdersBasedON && myOrderTypetmp==1)
              {
               PreviousOpenOrders=OpenOrders+1;
               ContinueOpening=False;
               text=text +"\nClosing all orders because Indicator triggered another signal.";
               Print("Closing all orders because Indicator triggered another signal.");
               //return(0);
              }
            if (TrailingStop>0)
              {
               if ((OrderClosePrice()-OrderOpenPrice())>=(TrailingStop*Point+Pips*Point))
                 {
                  if (OrderStopLoss()<(OrderClosePrice()-TrailingStop*Point))
                    {
                     result=OrderModify(OrderTicket(),OrderOpenPrice(),OrderClosePrice()-TrailingStop*Point,OrderClosePrice()+TakeProfit*Point+TrailingStop*Point,0,ArrowsColor);
                     if(result!=TRUE) Print("LastError = ", GetLastError());
                     else OrderPrint();
                     return(0);
                    }
                 }
              }
           }
        }
      cnt--;
     }
   if (!IsTesting())
     {
      if (myOrderType==3 && OpenOrders<1) { text=text + "\nNo conditions to open trades"; }
      //else { text= text + "\n                         "; }
      Comment("LastPrice=",LastPrice," Previous open orders=",PreviousOpenOrders,"\nContinue opening=",ContinueOpening," OrderType=",myOrderType,"\nLots=",lotsi,text);
     }
   if (OpenOrders<1)
      OpenMarketOrders();
   else
      if (SetLimitOrders) OpenLimitOrders();
      else OpenMarketOrders();
//----
   return(0);
  }
//+------------------------------------------------------------------+
void OpenMarketOrders()
  {
   int cnt=0;
   if (myOrderType==1 && ContinueOpening)
     {
      if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1)
        {
         SellPrice=Bid;
         LastPrice=0;
         if (TakeProfit==0) { tp=0; }
         else { tp=SellPrice-TakeProfit*Point; }
         if (StopLoss==0) { sl=0; }
         else { sl=SellPrice+StopLoss*Point;  }
         if (OpenOrders!=0)
           {
            mylotsi=lotsi;
            for(cnt=0;cnt<OpenOrders;cnt++)
              {
               if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
               else { mylotsi=NormalizeDouble(mylotsi*2,2); }
              }
           } 
           else { mylotsi=lotsi; }
         if (mylotsi>100) { mylotsi=100; }
         OrderSend(Symbol(),OP_SELL,mylotsi,SellPrice,slippage,sl,tp,"DLM"+MagicNumber,MagicNumber,0,ArrowsColor);
         return(0);
        }
     }
   if (myOrderType==2 && ContinueOpening)
     {
      if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1)
        {
         BuyPrice=Ask;
         LastPrice=0;
         if (TakeProfit==0) { tp=0; }
         else { tp=BuyPrice+TakeProfit*Point; }
         if (StopLoss==0)  { sl=0; }
         else { sl=BuyPrice-StopLoss*Point; }
           if (OpenOrders!=0) 
           {
            mylotsi=lotsi;
            for(cnt=0;cnt<OpenOrders;cnt++)
              {
               if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
               else { mylotsi=NormalizeDouble(mylotsi*2,2); }
              }
           }
            else { mylotsi=lotsi; }
         if (mylotsi>100) { mylotsi=100; }
         OrderSend(Symbol(),OP_BUY,mylotsi,BuyPrice,slippage,sl,tp,"DLM"+MagicNumber,MagicNumber,0,ArrowsColor);
         return(0);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OpenLimitOrders()
  {
   int cnt=0;
   if (myOrderType==1 && ContinueOpening)
     {
      //if ((Bid-LastPrice)>=Pips*Point || OpenOrders<1) 
      //{		
      //SellPrice=Bid;				
      SellPrice=LastPrice+Pips*Point;
      LastPrice=0;
      if (TakeProfit==0) { tp=0; }
      else { tp=SellPrice-TakeProfit*Point; }
      if (StopLoss==0) { sl=0; }
      else { sl=SellPrice+StopLoss*Point;  }
      if (OpenOrders!=0)
        {
         mylotsi=lotsi;
         for(cnt=0;cnt<OpenOrders;cnt++)
           {
            if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
            else { mylotsi=NormalizeDouble(mylotsi*2,2); }
           }
        }
         else { mylotsi=lotsi; }
      if (mylotsi>100) { mylotsi=100; }
      OrderSend(Symbol(),OP_SELLLIMIT,mylotsi,SellPrice,slippage,sl,tp,"DLM"+MagicNumber,MagicNumber,0,ArrowsColor);
      return(0);
      //}
     }
   if (myOrderType==2 && ContinueOpening)
     {
      //if ((LastPrice-Ask)>=Pips*Point || OpenOrders<1) 
      //{		
      //BuyPrice=Ask;
      BuyPrice=LastPrice-Pips*Point;
      LastPrice=0;
      if (TakeProfit==0) { tp=0; }
      else { tp=BuyPrice+TakeProfit*Point; }
      if (StopLoss==0)  { sl=0; }
      else { sl=BuyPrice-StopLoss*Point; }
        if (OpenOrders!=0) 
        {
         mylotsi=lotsi;
         for(cnt=0;cnt<OpenOrders;cnt++)
           {
            if (MaxTrades>12) { mylotsi=NormalizeDouble(mylotsi*1.5,2); }
            else { mylotsi=NormalizeDouble(mylotsi*2,2); }
           }
        }
         else { mylotsi=lotsi; }
      if (mylotsi>100) { mylotsi=100; }
      OrderSend(Symbol(),OP_BUYLIMIT,mylotsi,BuyPrice,slippage,sl,tp,"DLM"+MagicNumber,MagicNumber,0,ArrowsColor);
      return(0);
      //}
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void DeleteAllObjects()
  {
   int    obj_total=ObjectsTotal();
   string name;
   for(int i=0;i<obj_total;i++)
     {
      name=ObjectName(i);
      if (name!="")
         ObjectDelete(name);
     }
   ObjectDelete("FLP_txt");
   ObjectDelete("P_txt");
  }
// 16
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OpenOrdersBasedOnFXFish2MA()
  {
   int myOrderType=3;
   double tm0, tm1;
   double line;
//----
   tm0=iCustom(Symbol(),0,"FX_FISH_2MA",10,0,False,False,9,45,"-","-",0,3,3,0);
   tm1=iCustom(Symbol(),0,"FX_FISH_2MA",10,0,False,False,9,45,"-","-",0,3,4,0);
   line=iCustom(Symbol(),0,"FX_FISH_2MA",10,0,False,False,9,45,"-","-",0,3,2,0);
   // sell
   if(line<tm0)
     {
      myOrderType=1;
     }
   // buy 
   if(line>tm0)
     {
      myOrderType=2;
     }
//----
   return(myOrderType);
  }
//+------------------------------------------------------------------+