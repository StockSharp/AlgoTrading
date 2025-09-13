//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#define MAGICMA  3937
#define VER "1.3.7"

#define DECIMAL_CONVERSION 10

extern int Anchor            = 25;
extern double xFactor        = 1.5;
extern string TimeSet        = "07:32";
extern double Amount         = 1.0;
extern double RiskPercent    = 5;
extern double LotPercent     = 5;
extern double Lots           = 0.01;
extern double StopLoss       = 0;
extern int TrailingProfit    = 150;
extern int TrailingStop      = 80;
extern int Slippage          = 2;
extern string  Macd          = "Quick, Slow, Signal";
extern int     Qema          = 10;
extern int     Sema          = 32;
extern int     Signalmacd    = 4;
extern string  Ema           = "fast close, slow open.";
extern int     Fastema       = 8;
extern int     Slowema       = 14;


extern bool AllOrders = false;
extern bool LogToFile = false;
 
int PendingBuy, PendingSell, Buys, Sells, i, Spread, STOPLEVEL;
double BuyLots, SellLots, PendingBuyLots, PendingSellLots;
double Focus, Profit, LastProfit, Up, Dw;
double stoploss,takeprofit;
double LotsValue, Denominator;
double MaxPrice,MinPrice,MaxOpenPrice,MinOpenPrice;
bool isInGain=false, blockConditionHit=false, unlockOrdersPlaced=false; 
int LastOrderTicket = -1, UnlockingOrderTicket = -1;

double trailingStop, trailingProfit, stopLoss;

double lastBarTime;

//+------------------------------------------------------------------+
//| Init function                                                    |
//+------------------------------------------------------------------+
void init()
{
   if (Digits == 3 || Digits == 5)   {
      TrailingStop   *=    DECIMAL_CONVERSION;
      TrailingProfit *=    DECIMAL_CONVERSION;
      StopLoss       *=    DECIMAL_CONVERSION;
      Anchor         *=    DECIMAL_CONVERSION;
   }
   
   trailingStop     = TrailingStop   *  Point;
   trailingProfit   = TrailingProfit *  Point;
   stopLoss         = StopLoss *  Point;

   STOPLEVEL = MarketInfo(Symbol(),MODE_STOPLEVEL);
      
   if(LogToFile){startFile();}
   Spread   = MarketInfo(Symbol(),MODE_SPREAD);
   if(AccountBalance()<1000){Denominator=400;}
   else{Denominator=200;}
      
   lastBarTime = TimeCurrent();
}

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
{
   // 2 seconds
   int CheckingPeriod=2.0;

   if ( (TimeCurrent() - lastBarTime) >= CheckingPeriod )
   {
      Count();

      if (LotPercent < 1)
      {
        LotsValue = Lots;
      }
      else
      {
        LotsValue = LOT();
      }

      int Delta=10;        //Order price shift (in points) from High/Low price
      int LastDay=0;

      MaxPrice=iHigh(Symbol(),PERIOD_D1,LastDay)+NormalizeDouble(Delta*Point,Digits);
      MinPrice=iLow(Symbol(),PERIOD_D1,LastDay)-NormalizeDouble(Delta*Point,Digits);

      MaxOpenPrice=MaxPrice;
      MinOpenPrice=MinPrice;

      if (Ask+STOPLEVEL*Point>MaxPrice) MaxOpenPrice = NormalizeDouble(Ask+STOPLEVEL*Point,Digits);
      if (Bid-STOPLEVEL*Point<MinPrice) MinOpenPrice = NormalizeDouble(Bid-STOPLEVEL*Point,Digits);

      //LIMITI
      Dw=Bid-Anchor*Point;
      Up=Ask+Anchor*Point;

      double LotsHedgeValue;
          LotsHedgeValue = (LotsValue*xFactor);
          LotsHedgeValue = NormalizeDouble(LotsHedgeValue, 2);
          if(LotsHedgeValue < MarketInfo(Symbol(), MODE_MINLOT))
          {
             LotsHedgeValue = NormalizeDouble(MarketInfo(Symbol(), MODE_MINLOT)*xFactor, 2);
          }

      //SYSTEM CORE
      if(PendingSell == 0 && PendingBuy == 0)
      {
          log("[PendingSellLots == 0 && PendingBuyLots == 0] OP_SELLSTOP+OP_BUYSTOP; LotsHedgeValue: " + LotsHedgeValue);

          stoploss   = /*Ask+SL*Point*/ 0;
          takeprofit = /*Bid-TP*Point*/ 0;
          OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Red);
          Sleep(10);

          stoploss   = /*Bid-SL*Point*/ 0;
          takeprofit = /*Ask+TP*Point*/ 0;
          OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Blue);
      }
      else
      {
          if((PendingSell == 0 && PendingBuy > 0) || (PendingSell > 0 && PendingBuy == 0))
          {
               int Result=-1;
       
               if(PendingBuy > 0)
               {
                  log("[PendingBuyLots > 0] OP_SELLSTOP; Dw: "+Dw+";LotsHedgeValue: "+LotsHedgeValue+"; PendingBuyLots: "+PendingBuyLots+"; PendingSellLots: "+PendingSellLots);
                  stoploss   = /*Ask+SL*Point*/ 0;
                  takeprofit = /*Bid-TP*Point*/ 0;
                  Result=OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Red);          
               }
               else if(PendingSell > 0)
               { 
                  log("[PendingSellLots > 0] OP_BUYSTOP; Up: "+Up+";LotsHedgeValue: "+LotsHedgeValue+"; PendingBuyLots: "+PendingBuyLots+"; PendingSellLots: "+PendingSellLots);
                  stoploss   = /*Bid-SL*Point*/ 0;
                  takeprofit = /*Ask+TP*Point*/ 0;
                  Result=OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Blue);
               }      
          }
      }

      Count();

      if(Buys==0 && Sells==0){CheckForOpen();}
      else{CheckForClose();}
      
      lastBarTime = TimeCurrent();
   }
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForOpen()
{
  int expiration=CurTime()+(23-TimeHour(CurTime()))*3600+(60-TimeMinute(CurTime()))*60;  //set order expiration time
  
  if( (PendingSell==0 && PendingBuy==0) || (TimeStr(CurTime())==TimeSet) ) 
  {
      if(PendingBuyLots==0)
      {
         stoploss   = /*Bid-SL*Point*/ 0;
         takeprofit = /*Ask+TP*Point*/ 0;
         OrderSend(Symbol(),OP_BUYSTOP,LotsValue,MaxOpenPrice,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,expiration,Blue);
      }
      if(PendingSellLots==0)
      {
         stoploss   = /*Ask+SL*Point*/ 0;
         takeprofit = /*Bid-TP*Point*/ 0;
         OrderSend(Symbol(),OP_SELLSTOP,LotsValue,MinOpenPrice,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,expiration,Red);
      }
  }

  CharlesStatus();
} 

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose()
{
   RefreshRates();

   if(AccountBalance()>=1000){Amount=10.0+(AccountBalance()/10000);}
   else{Amount=1.0+(AccountBalance()/1000);}

   //CONDIZIONE DI CHIUSURA DI SUCCESSO
   if(Profit>=0 && Profit>=Amount/10)
   {
      log("[CONDIZIONE DI CHIUSURA DI SUCCESSO]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
      
      if(!isInGain)
      {
         isInGain = true;
         LastProfit = Profit;
      }
   
      if(Profit<=(LastProfit - Amount/10))
      {
         log("[CONDIZIONE DI CHIUSURA DI SUCCESSO - CloseAll]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
         isInGain = false;
         blockConditionHit=false;
         unlockOrdersPlaced=false;
         LastOrderTicket = -1;
         UnlockingOrderTicket = -1;
         CloseAll();
      }
      else
      {
         LastProfit = Profit;
      }
   }
  
   //CONDIZIONE DI BLOCCAGGIO E SBLOCCAGGIO DI EMERGENZA
   if(Profit<-AccountBalance()*RiskPercent/100)
   {
      if(!blockConditionHit)
      {
         log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         blockConditionHit=true;
         DeleteAllPendingOrders();      
         if (SellLots>BuyLots)
         {
            LastOrderTicket=OpenOrder(SellLots-BuyLots, OP_BUY);
            log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
         }
         else if (BuyLots>SellLots)
         {
            LastOrderTicket=OpenOrder(BuyLots-SellLots, OP_SELL);
            log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: BuyLots>SellLots; Opened SELL Order Ticket: " + LastOrderTicket);
         }
      }
   }

   if(blockConditionHit)
   {
      for(i=OrdersTotal(); i>=0; i--)
      {          
          OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
          if(OrderSymbol()==Symbol())
          {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }

            if(OrderTicket()>=LastOrderTicket)
            {
               CheckTrailingStop(OrderTicket());
            }
         }
      }
   }
   
   CharlesStatus();                  
}  

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CharlesStatus()
{

   Count();

   Comment("Charles ",VER," - Gain= ",Profit," LookingFor= ",(LastProfit - Amount/10),
          ";\nBalance=",AccountBalance(),"; FreeMargin=", AccountFreeMargin(),"; Equity=", AccountEquity(),
          ";\nBuy=",Buys,"; Sell=", Sells,"; BuyLots=",BuyLots,"; SellLots=",SellLots,
          ";\nPendingSellLots=",PendingSellLots,"; PendingBuyLots=", PendingBuyLots);

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
double LOT()
{
   RefreshRates();

   double MINLOT = MarketInfo(Symbol(),MODE_MINLOT);
   double LOT = AccountFreeMargin()*AccountLeverage()*RiskPercent/1000/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/15;
   if (LOT>MarketInfo(Symbol(),MODE_MAXLOT)) LOT = MarketInfo(Symbol(),MODE_MAXLOT);
   if (LOT<MINLOT) LOT = MINLOT;
   if (MINLOT<0.1) LOT = NormalizeDouble(LOT,2); else LOT = NormalizeDouble(LOT,1);
   return(LOT);
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------
void Count()
{ 
  RefreshRates();
  Buys=0; Sells=0; PendingBuy=0; PendingSell=0; BuyLots=0; SellLots=0; PendingBuyLots=0; PendingSellLots=0; Profit=0;
  for(i=OrdersTotal(); i>=0; i--)
  {
      OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol())
      {
         if(!AllOrders && OrderMagicNumber()!=MAGICMA)
         {
            continue;
         }
         Profit = Profit + OrderProfit() + OrderSwap();
         if(OrderType()==OP_SELL){SellLots=SellLots+OrderLots();Sells++;}
         if(OrderType()==OP_BUY){BuyLots=BuyLots+OrderLots();Buys++;}
         if(OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT){PendingSellLots=PendingSellLots+OrderLots();PendingSell++;}
         if(OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT){PendingBuyLots=PendingBuyLots+OrderLots();PendingBuy++;}
      }//if
   }//for
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CloseAll()
{
   RefreshRates();

   bool   Result;
   int    i,Pos,Error;
   int    Total=OrdersTotal();
   
   if(Total>0)
   {for(i=Total-1; i>=0; i--) 
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {
         if(OrderSymbol()==Symbol())
         {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }
            Pos=OrderType();
            if(Pos==OP_BUY){Result=OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, Blue);}
            if(Pos==OP_SELL){Result=OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, Red);}
            if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
            if(Result!=true){Error=GetLastError();log("LastError = "+Error);}
            else Error=0;
//-----------------------
         }//if
       }//if
     }//for
   }//if
   
   Sleep(20);
   return(0);
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void DeleteAllPendingOrders()
{
   RefreshRates();

   bool   Result;
   int    i,Pos,Error;
   int    Total=OrdersTotal();
   
   if(Total>0)
   {for(i=Total-1; i>=0; i--) 
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {
         if(OrderSymbol()==Symbol())
         {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }
            Pos=OrderType();
            if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
            if(Result!=true){Error=GetLastError();log("LastError = "+Error);}
            else Error=0;
//-----------------------
         }//if
       }//if
     }//for
   }//if
   return(0);
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int OpenOrder(double LotOpenValue, int Type)
{
   RefreshRates();
   int    Error, Result;

   log("OpenOrder - LotHedgeValue:" + LotOpenValue + "; AccountFreeMargin: "+AccountFreeMargin());

   if(AccountFreeMargin()>0)
   {
      log("MarketInfo(Symbol(),MODE_MARGINREQUIRED) == " + MarketInfo(Symbol(),MODE_MARGINREQUIRED));
      log("Ask("+Ask+")-MaxOpenPrice("+MaxOpenPrice+") == " + (Ask-MaxOpenPrice));
      log("MinOpenPrice("+MinOpenPrice+")-Bid("+Bid+") == " + (MinOpenPrice-Bid));
      if(Type==OP_BUY)
      {
         stoploss   = /*Bid-SL*Point*/ 0;
         takeprofit = /*Ask+TP*Point*/ 0;
         Result=OrderSend(Symbol(),OP_BUY,LotOpenValue,Ask,Slippage,stoploss,takeprofit,"Charels_"+VER,MAGICMA,0,Blue);
         log("OpenOrder[OP_BUY] - Ask:" + Ask + "; Result: "+Result);
      }
      if(Type==OP_SELL)
      {
         stoploss   = /*Ask+SL*Point*/ 0;
         takeprofit = /*Bid-TP*Point*/ 0;
         Result=OrderSend(Symbol(),OP_SELL,LotOpenValue,Bid,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Red);
         log("OpenOrder[OP_SELL] - Bid:" + Bid + "; Result: "+Result);
      }
   }

//-----------------------
   if(Result==-1){Error=GetLastError();log("LastError = "+Error);}
   else {Error=0;}
//-----------------------

   return(Result);
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------
void CheckTrailingStop(int ticket)
{
   RefreshRates();
   
   OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES);

   if(OrderType()==OP_BUY)
   {
      if(Bid-OrderOpenPrice()<0)
      {
         OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(OrderOpenPrice()-trailingStop), normPrice(Bid+trailingProfit),0,Blue);
         return(0);
      }
      else if(trailingStop>0)
      {
         if(Bid-trailingStop>OrderOpenPrice())
         {
            if(OrderStopLoss()<Bid-trailingStop)
            {
               OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(Bid-trailingStop), normPrice(Bid+trailingProfit),0,Blue);
            }
            return(0);
         }
      }
      else if((OrderStopLoss()!=Bid-stopLoss) && (stopLoss != 0))
      {
         OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(Bid-stopLoss), normPrice(Bid+trailingProfit),Blue);
         return(0);
      }
   }
   else if (OrderType() == OP_SELL)
   {
      if(OrderOpenPrice()-Ask<0)
      {
         OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(OrderOpenPrice()+trailingStop), normPrice(Ask-trailingProfit),0,Red);
         return(0);
      }
      else if(trailingStop>0)
      {
         if(Ask+trailingStop<OrderOpenPrice())
         {
            if(OrderStopLoss()>Ask+trailingStop)
            {
               OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(Ask+trailingStop), normPrice(Ask-trailingProfit),Red);
            }
            return(0);
         }         
      }
      else if((OrderStopLoss()!=Ask+stopLoss) && (stopLoss != 0))
      {
         OrderModify(OrderTicket(), OrderOpenPrice(), normPrice(Ask+stopLoss), normPrice(Ask-trailingProfit),Red);
         return(0);
      }
   }
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------
double normPrice (double price)
{
   return(NormalizeDouble (price, Digits)); 
}
//---------------------------------------------------------------------------
void startFile()
{
  int handle;
  handle=FileOpen("Charles-"+VER+".log", FILE_BIN|FILE_READ|FILE_WRITE);
    if(handle<1)
    {
     Print("can't open file error-",GetLastError());
     return(0);
    }
  FileSeek(handle, 0, SEEK_END);
  //---- add data to the end of file
  string str = 
      "----------------------------------------------------------------------------------------------------------------------------------------\n" + 
      "-- Charles v."+VER+" - Log Starting...                                                                                                  --\n" +
      "----------------------------------------------------------------------------------------------------------------------------------------\n";
  FileWriteString(handle, str, StringLen(str));
  FileFlush(handle);
  FileClose(handle);
}
//---------------------------------------------------------------------------
void log(string str)
{
  str = "["+Day()+"-"+Month()+"-"+Year()+" "+Hour()+":"+Minute()+":"+Seconds()+"] "+str+"\n";
  
  if(LogToFile)
  {
      int handle;
      handle=FileOpen("Charles-"+VER+".log", FILE_BIN|FILE_READ|FILE_WRITE);
        if(handle<1)
        {
         Print("can't open file error-",GetLastError());
         return(0);
        }
      FileSeek(handle, 0, SEEK_END);
      //---- add data to the end of file
      FileWriteString(handle, str, StringLen(str));
      FileFlush(handle);
      FileClose(handle);
  }
  else
  {
      Print(str);
  }
}
//---------------------------------------------------------------------------
string TimeStr(int taim)
{
   string sTaim;
   int HH=TimeHour(taim);     // Hour                  
   int MM=TimeMinute(taim);   // Minute   
   if (HH<10) sTaim = StringConcatenate(sTaim,"0",DoubleToStr(HH,0));
   else       sTaim = StringConcatenate(sTaim,DoubleToStr(HH,0));
   if (MM<10) sTaim = StringConcatenate(sTaim,":0",DoubleToStr(MM,0));
   else       sTaim = StringConcatenate(sTaim,":",DoubleToStr(MM,0));
   return(sTaim);
}