//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#define MAGICMA  3937
#define VER "1.3.3"

extern int Anchor            = 250;
extern double xFactor        = 0.7;
extern string TimeSet="07:32";
extern double Amount         = 1.0;
extern double RiskPercent    = 10;
extern double LotPercent     = 10;
extern double Lots           = 0.01;
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
double Focus, Profit, LastProfit, Up, Dw, LookingForProfit;
double stoploss,takeprofit;
double LotsValue, Denominator;
double MaxPrice,MinPrice,MaxOpenPrice,MinOpenPrice;
bool isInGain=false, blockConditionHit=false, waitForFirstSignal=false; 
int LastOrderTicket = -1;

double lastBarTime;

//+------------------------------------------------------------------+
//| Init function                                                    |
//+------------------------------------------------------------------+
void init()
{
   LookingForProfit = Amount/10.0;
   
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
   int SystemPeriod = Period();

   if ( (TimeCurrent() - lastBarTime) >= 2.0 )
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
      STOPLEVEL = MarketInfo(Symbol(),MODE_STOPLEVEL);

      MaxPrice=iHigh(Symbol(),PERIOD_D1,LastDay)+NormalizeDouble(Delta*Point,Digits);
      MinPrice=iLow(Symbol(),PERIOD_D1,LastDay)-NormalizeDouble(Delta*Point,Digits);

      MaxOpenPrice=MaxPrice;
      MinOpenPrice=MinPrice;

      if (Ask+STOPLEVEL*Point>MaxPrice) MaxOpenPrice = NormalizeDouble(Ask+STOPLEVEL*Point,Digits);
      if (Bid-STOPLEVEL*Point<MinPrice) MinOpenPrice = NormalizeDouble(Bid-STOPLEVEL*Point,Digits);

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
  if( (PendingBuyLots==0 && PendingSellLots==0) || (TimeStr(CurTime())==TimeSet) ) 
  {
      int expiration=CurTime()+(23-TimeHour(CurTime()))*3600+(60-TimeMinute(CurTime()))*60;  //set order expiration time
      if(PendingBuyLots==0 /*&& !waitForFirstSignal*/)
      {
         stoploss   = /*Bid-SL*Point*/ 0;
         takeprofit = /*Ask+TP*Point*/ 0;
         OrderSend(Symbol(),OP_BUYSTOP,LotsValue,MaxOpenPrice,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,expiration,Blue);
      }
      Sleep(10);
      if(PendingSellLots==0 /*&& !waitForFirstSignal*/)
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
   if(Profit>=Amount/10)
   {
      log("[CONDIZIONE DI CHIUSURA DI SUCCESSO]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
      
      if(!isInGain)
      {
         isInGain = true;
         LastProfit = Profit;
         LookingForProfit = Profit;
      }
   
      if(Profit<=(LastProfit - LastProfit/10))
      {
         log("[CONDIZIONE DI CHIUSURA DI SUCCESSO - CloseAll]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
         isInGain = false;
         blockConditionHit=false;
         waitForFirstSignal=false;
         LastOrderTicket = -1;
         LookingForProfit = Amount/10;
         CloseAll();
      }
      else
      {
         LastProfit = Profit;
         LookingForProfit = Profit + Profit/10;
      }
   }
  
   //LIMITI
   Dw=Bid-Anchor*Point;
   Up=Ask+Anchor*Point;

   double LotsHedgeValue, LotsBlockHedgeValue;
       LotsHedgeValue = (LotsValue*xFactor);
       LotsHedgeValue = NormalizeDouble(LotsHedgeValue, 2);
       if(LotsHedgeValue < MarketInfo(Symbol(), MODE_MINLOT))
       {
          LotsHedgeValue = NormalizeDouble(MarketInfo(Symbol(), MODE_MINLOT)*xFactor, 2);
       }

   //CONDIZIONE DI BLOCCAGGIO DI EMERGENZA
   if(Profit<-AccountBalance()*RiskPercent/100)
   {
      if(!blockConditionHit)
      {
         log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         blockConditionHit=true;
      
         if (SellLots>BuyLots)
         {
            DeleteAllPendingOrders();
            LastOrderTicket=OpenOrder(SellLots-BuyLots, OP_BUY);
         }
         else if (BuyLots>SellLots)
         {
            DeleteAllPendingOrders();
            LastOrderTicket=OpenOrder(BuyLots-SellLots, OP_SELL);
         }
      }
   }

   if(blockConditionHit)
   {
      int Current = 0;

      double Buy1_1 = iMA(Symbol(), PERIOD_H4, Fastema, 0, MODE_EMA, PRICE_CLOSE, Current + 0);
      double Buy1_2 = iMA(Symbol(), PERIOD_H4, Slowema, 0, MODE_EMA, PRICE_OPEN, Current + 0);
      double Buy2_1 = iMACD(Symbol(), PERIOD_H4, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 1);
      double Buy2_2 = iMACD(Symbol(), PERIOD_H4, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 0);

      double Sell1_1 = iMA(Symbol(), PERIOD_H4, Fastema, 0, MODE_EMA, PRICE_CLOSE, Current + 0);
      double Sell1_2 = iMA(Symbol(), PERIOD_H4, Slowema, 0, MODE_EMA, PRICE_OPEN, Current + 0);
      double Sell2_1 = iMACD(Symbol(), PERIOD_H4, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 0);
      double Sell2_2 = iMACD(Symbol(), PERIOD_H4, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 1);
      
      if( !waitForFirstSignal && ((Buy1_1 > Buy1_2 && Buy2_1 < Buy2_2) || (Sell1_1 < Sell1_2 && Sell2_1 < Sell2_2)) )
      {
         waitForFirstSignal=true;
      }

      double LocalOrderProfit=0.0;
      int tickets=0;
      int orderTickets[1000];
            
      for(i=OrdersTotal(); i>=0; i--)
      {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OrderSymbol()==Symbol())
         {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }
            
            if(OrderTicket()>LastOrderTicket)
            {
               orderTickets[tickets]=OrderTicket();
               tickets++;

               /*LocalOrderProfit = LocalOrderProfit + OrderProfit() + OrderSwap();
               if(LocalOrderProfit>=Amount)
               {
                  break;
               }*/
            }
         }//if
      }//for

      if(tickets>0 && LocalOrderProfit)
      {
         for(i=0; i<tickets; i++)
         {
            if(OrderSelect(orderTickets[i],SELECT_BY_TICKET,MODE_TRADES))
            {
               LocalOrderProfit=OrderProfit()+OrderSwap();
               if(OrderType()==OP_SELL && LocalOrderProfit>0)
               {
                  //OrderClose(orderTickets[i],OrderLots(),Ask,Slippage, Red);
                  OpenOrder(OrderLots()*xFactor,OP_SELL);
                  break;
                  //OrderSend(Symbol(),OP_SELLSTOP,LotsValue,Bid-10*(i+1)*Point,Slippage,0,0,"Charles_"+VER,MAGICMA,0,Red);
               }
               else if(OrderType()==OP_BUY && LocalOrderProfit>0)
               {
                  //OrderClose(orderTickets[i],OrderLots(),Bid,Slippage, Blue);
                  OpenOrder(OrderLots()*xFactor,OP_BUY);
                  break;
                  //OrderSend(Symbol(),OP_BUYSTOP,LotsValue,Ask+10*(i+1)*Point,Slippage,0,0,"Charles_"+VER,MAGICMA,0,Blue);
               }
            }
         }
      }
                  
   }

   //SYSTEM CORE
   if(PendingSellLots == 0 && PendingBuyLots == 0)
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
   else if((PendingSellLots == 0 && PendingBuyLots != 0) || (PendingSellLots != 0 && PendingBuyLots == 0))
   {
       int Result=-1,cnt=0;
       
       if(PendingBuyLots > 0)
       {
          log("[PendingBuyLots > 0] OP_SELLSTOP; Dw: "+Dw+";LotsHedgeValue: "+LotsHedgeValue+"; PendingBuyLots: "+PendingBuyLots+"; PendingSellLots: "+PendingSellLots);
          stoploss   = /*Ask+SL*Point*/ 0;
          takeprofit = /*Bid-TP*Point*/ 0;
          Result=OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Red);          
          while(Result<0 || cnt==100){
            Result=OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Red);
            cnt++;
          }
       }
       else if(PendingSellLots > 0)
       { 
          log("[PendingSellLots > 0] OP_BUYSTOP; Up: "+Up+";LotsHedgeValue: "+LotsHedgeValue+"; PendingBuyLots: "+PendingBuyLots+"; PendingSellLots: "+PendingSellLots);
          stoploss   = /*Bid-SL*Point*/ 0;
          takeprofit = /*Ask+TP*Point*/ 0;
          Result=OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Blue);
          while(Result<0 || cnt==100){
            Result=OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,stoploss,takeprofit,"Charles_"+VER,MAGICMA,0,Blue);
            cnt++;
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
   Comment("Charles ",VER," - Gain= ",/*Profit*/AccountProfit()," LookingFor= ",LookingForProfit,
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
         if(OrderType()==OP_SELLSTOP || OrderType()==OP_SELLLIMIT){PendingSellLots=PendingSellLots+OrderLots();}
         if(OrderType()==OP_BUYSTOP || OrderType()==OP_BUYLIMIT){PendingBuyLots=PendingBuyLots+OrderLots();}
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