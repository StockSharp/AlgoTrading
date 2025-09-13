//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#define MAGICMA  3939
#define VER "1.3.9"

#define DECIMAL_CONVERSION 10

extern double xFactor             = 1.5;
extern string TimeSet             = "07:32";
extern double RiskPercent         = 2;
extern double RiskPerTrade        = 1.5;
extern double Lots                = 0.01;
extern double StopLoss            = 55;
extern double MinPipsProfit       = 30;
extern double TrailingUnlockStop  = 35;
extern int Slippage               = 3;

extern bool AllOrders = false;
extern bool LogToFile = false;
 
int Anchor, PendingBuy, PendingSell, Buys, Sells, i, StopLevel, Spread;

double BuyLots, SellLots, PendingBuyLots, PendingSellLots;
double Amount, Profit, LastProfit, LooserProfit, UnBlockingProfit, Up, Dw, SL, TP;
double LotsValue, LotsHedgeValue;
double MaxPrice,MinPrice,MaxOpenPrice,MinOpenPrice;

bool isInGain=false, blockConditionHit=false, unlockOrdersPlaced=false; 
int LastOrderTicket, UnlockingOrderTicket, Result, Error;
int Delta;        //Order price shift (in points) from High/Low price
int LastDay;

double trailingStop, takeProfit, stopLoss;
bool canSell=true, canBuy=true;

double lastBarTime;

//+------------------------------------------------------------------+
//| Init function                                                    |
//+------------------------------------------------------------------+
void init()
{
   RefreshRates();
   
   if (Digits == 3 || Digits == 5)   {
      MinPipsProfit      *=    DECIMAL_CONVERSION;
      TrailingUnlockStop *=    DECIMAL_CONVERSION;
      StopLoss           *=    DECIMAL_CONVERSION;
   }
   
   trailingStop     = TrailingUnlockStop * Point;
   stopLoss         = StopLoss *  Point;

   Spread    = MarketInfo(Symbol(), MODE_SPREAD);
   StopLevel = MarketInfo(Symbol(), MODE_STOPLEVEL) + Spread;
   
   //if (trailingStop > 0 && trailingStop < StopLevel) trailingStop = StopLevel;
   
   if(LogToFile){startFile();}

   Amount               = 1.0;
   Anchor               = 250;
   Delta                = 5;
   LastDay              = 1;
   PendingBuy           = 0;
   PendingSell          = 0;
   Buys                 = 0;
   Sells                = 0;
   
   BuyLots              = 0;
   SellLots             = 0;
   PendingBuyLots       = 0;
   PendingSellLots      = 0;

   LastOrderTicket      = -1;
   UnlockingOrderTicket = -1;
   
   lastBarTime = TimeCurrent();
}

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
{
   int SystemPeriod=2;

   if ( (TimeCurrent() - lastBarTime) >= SystemPeriod )
   {
      
      Count();

      if (RiskPerTrade <= 0)
      {
        LotsValue = Lots;
      }
      else
      {
        LotsValue = LOT();
      }

      Amount = normPrice(MarketInfo(Symbol(),MODE_MINLOT));
      
      MaxPrice=iHigh(Symbol(),PERIOD_D1,LastDay)+Delta*Point;
      MinPrice=iLow(Symbol(),PERIOD_D1,LastDay)-Delta*Point;

      MaxOpenPrice=MaxPrice;
      MinOpenPrice=MinPrice;

      if (Ask+StopLevel*Point>MaxPrice) MaxOpenPrice = Ask+StopLevel*Point;
      if (Bid-StopLevel*Point<MinPrice) MinOpenPrice = Bid-StopLevel*Point;

      LotsHedgeValue = LotsValue*MathAbs(Buys-Sells)*xFactor;

      if(LotsHedgeValue == 0)
      {
         LotsHedgeValue = LotsValue;
      }
      LotsHedgeValue = normPrice(LotsHedgeValue);

      if(Buys!=0 || Sells!=0)
      {
         CheckForClose();
      }

      //SYSTEM CORE
      Count();

      double PivPnt=(iHigh(Symbol(),PERIOD_H1,1)+iLow(Symbol(),PERIOD_H1,1)+iClose(Symbol(),PERIOD_H1,1)+iOpen(Symbol(),PERIOD_H1,1))/4;

      double Res1=iHigh(Symbol(),PERIOD_H4,0)+iATR(Symbol(),PERIOD_H4,14,0);
      double Sup1=iLow(Symbol(),PERIOD_H4,0)-iATR(Symbol(),PERIOD_H4,14,0);
      
      double Res4=iHigh(Symbol(),PERIOD_D1,0)+iATR(Symbol(),PERIOD_D1,14,0);
      double Sup4=iLow(Symbol(),PERIOD_D1,0)-iATR(Symbol(),PERIOD_D1,14,0);
      
      double C1=iClose(Symbol(),PERIOD_H1,1);
      double O1=iOpen(Symbol(),PERIOD_H1,1);
      double L1=iLow(Symbol(),PERIOD_H1,1);
      double H1=iHigh(Symbol(),PERIOD_H1,1);
      double BandUP=iBands(Symbol(),PERIOD_H1,20,2,0,PRICE_CLOSE,1,0);
      double BandDown=iBands(Symbol(),PERIOD_H1,20,-2,0,PRICE_CLOSE,1,0);
      double L2=iLow(Symbol(),PERIOD_H1,2);
      double H2=iHigh(Symbol(),PERIOD_H1,2);
      double C2=iClose(Symbol(),PERIOD_H1,2);
      double ADX=iADX(Symbol(),PERIOD_H1,14,PRICE_CLOSE,MODE_MAIN,1);

      Result=0;Error=0;

      //LIMITI
      double Dw1=NormalizeDouble(Sup1-(Delta*Point)/*-(Spread*Point)*/,Digits);
      double Up1=NormalizeDouble(Res1+(Delta*Point)/*+(Spread*Point)*/,Digits);

      double Dw4=NormalizeDouble(Sup4-(Delta*Point)/*-(Spread*Point)*/,Digits);
      double Up4=NormalizeDouble(Res4+(Delta*Point)/*+(Spread*Point)*/,Digits);

      double DwA=NormalizeDouble(Ask-(Anchor*Point)-(Delta*Point)/*-(Spread*Point)*/,Digits);
      double UpA=NormalizeDouble(Bid+(Anchor*Point)+(Delta*Point)/*+(Spread*Point)*/,Digits);

      if (Dw1 < NormalizeDouble(Ask-(Delta*Point)-(Spread*Point),Digits))
      {
         Dw = Dw1;
      }
      else if (Dw4 < NormalizeDouble(Ask-(Delta*Point)-(Spread*Point),Digits))
      {
         Dw = Dw4;
      }
      else
      {
         Dw = DwA;
      }
      
      if (Up1 > NormalizeDouble(Bid+(Delta*Point)+(Spread*Point),Digits))
      {
         Up = Up1;
      }
      else if (Up4 > NormalizeDouble(Bid+(Delta*Point)+(Spread*Point),Digits))
      {
         Up = Up4;
      }
      else
      {
         Up = UpA;
      }

      //Dw=NormalizeDouble(Ask-(Anchor*Point)-(Delta*Point)/*-(Spread*Point)*/,Digits);
      //Up=NormalizeDouble(Bid+(Anchor*Point)+(Delta*Point)/*+(Spread*Point)*/,Digits);

      /*if(TimeStr(CurTime())==TimeSet && Buys==0 && Sells==0)
      {
         DeleteAllPendingOrders();
         Count();
      }*/

      if((PendingSell==0 && PendingBuy==0) /*|| (TimeStr(CurTime())==TimeSet)*/)
      {
          RefreshRates();
          
          if(TimeStr(CurTime())==TimeSet)
          {
            Dw=MinOpenPrice;
            Up=MaxOpenPrice;
          }
    
          SL=0;
          TP=0;

          if(PendingSell == 0)
          {
          
            //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys>Sells) {canSell=false;}
            
            if(canSell)
            {
               //if(stopLoss > 0){SL=Dw+stopLoss;}
               
               Result=OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);
               //-----------------------
                 if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();log("LastError [CORE;OP_SELLSTOP]= "+Error);*/}
                 else Error=0;
               //-----------------------
            }
          }
       
          if(PendingBuy == 0 && Error==0)
          {

            //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys<Sells) {canBuy=false;}
            
            if(canBuy)
            {
               //if(stopLoss > 0){SL=Up-stopLoss;}

               Result=OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Blue);
               //-----------------------
                 if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();log("LastError [CORE;OP_BUYSTOP] = "+Error);*/}
                 else Error=0;
               //-----------------------
            }
          }
      }
      else
      {
          if((PendingSell == 0 && PendingBuy > 0) || (PendingSell > 0 && PendingBuy == 0))
          {
               RefreshRates();

               SL=0;
               TP=0;

               if(PendingSell == 0 && PendingBuy > 0)
               {

                  //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys>Sells) {canSell=false;}

                  if(canSell)
                  {
                     //if(stopLoss > 0){SL=Dw+stopLoss;}
                     
                     Result=OrderSend(Symbol(),OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);          
                     //-----------------------
                       if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();log("LastError [CORE - HEDGE;OP_SELLSTOP]= "+Error);*/}
                       else Error=0;
                     //-----------------------
                  }
               }
         
               if(PendingSell > 0 && PendingBuy == 0 && Error==0)
               {

                  //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys<Sells) {canBuy=false;}

                  if(canBuy)
                  {
                     //if(stopLoss > 0){SL=Up-stopLoss;}

                     Result=OrderSend(Symbol(),OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Blue);
                     //-----------------------
                       if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();log("LastError [CORE - HEDGE;OP_BUYSTOP] = "+Error);*/}
                       else Error=0;
                     //-----------------------
                  }
               }      
          }
      }

      lastBarTime = TimeCurrent();

      CharlesStatus();
   }

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose()
{
   RefreshRates();
   
   Count();

   //if(AccountBalance()>=1000){Amount=10.0+(AccountBalance()/10000);}
   //else{Amount=1.0+(AccountBalance()/1000);}

   //CONDIZIONE DI CHIUSURA DI SUCCESSO
   if((Profit>=0 && Profit>=Amount) || (UnBlockingProfit>0 && UnBlockingProfit>-LooserProfit))
   {
      //log("[CONDIZIONE DI CHIUSURA DI SUCCESSO]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
      
      if(!isInGain)
      {
         isInGain = true;
         LastProfit = Profit;
      }
   
      if(Profit<=(LastProfit - Amount/10) || UnBlockingProfit>-LooserProfit)
      {
         //log("[CONDIZIONE DI CHIUSURA DI SUCCESSO - CloseAll]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
         isInGain = false;
         blockConditionHit=false;
         unlockOrdersPlaced=false;
         LastOrderTicket = -1;
         UnlockingOrderTicket = -1;
         CloseAll();
         canSell=true;
         canBuy=true;
         UnBlockingProfit=0;
         LooserProfit=0;
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
         //log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         blockConditionHit=true;
         DeleteAllPendingOrders();      
         if (SellLots>BuyLots)
         {
            LastOrderTicket=OpenOrder(SellLots-BuyLots, OP_BUY);
            //log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
         }
         else if (BuyLots>SellLots)
         {
            LastOrderTicket=OpenOrder(BuyLots-SellLots, OP_SELL);
            //log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: BuyLots>SellLots; Opened SELL Order Ticket: " + LastOrderTicket);
         }
         else
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
                  
                  LastOrderTicket=OrderTicket();
                  break;
                }
            }
         }
      }
   }

   if(blockConditionHit)
   {
      UnBlockingProfit=0;
      LooserProfit=0;
      for(i=OrdersTotal(); i>=0; i--)
      {          
          OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
          if(OrderSymbol()==Symbol())
          {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }

            if(OrderTicket()<=LastOrderTicket)
            {
               if(OrderProfit()<0)
               {
                  if(OrderType() == OP_BUY && canBuy)
                  {
                     canBuy=false;
                  }
                  if(OrderType() == OP_SELL && canSell)
                  {
                     canSell=false;
                  }
               }
               else
               {
                  if(OrderType() == OP_BUY && !canBuy)
                  {
                     canBuy=true;
                  }
                  if(OrderType() == OP_SELL && !canSell)
                  {
                     canSell=true;
                  }
               }
            }

            if(OrderTicket()>LastOrderTicket)
            {
               if(OrderType()==OP_BUY || OrderType()==OP_SELL)
               {
                  CheckTrailingStop(OrderTicket());
                  UnBlockingProfit += (OrderProfit() + OrderCommission() + OrderSwap());
               }
            }
            
            if(OrderProfit()<0)
            {
               LooserProfit += (OrderProfit() + OrderCommission() + OrderSwap());
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
   
   double LookingFor = Amount;
   
   if(Profit>0 && Profit>=Amount/10.0) {LookingFor=(LastProfit - Amount/10.0);}

   Comment("Charles ",VER," - Gain= ",Profit," LookingFor= ",LookingFor," RiskPercent= ",(-AccountBalance()*RiskPercent/100),
          ";\nBalance=",AccountBalance(),"; FreeMargin=", AccountFreeMargin(),"; Equity=", AccountEquity(),
          ";\nBuy=",Buys,"; Sell=", Sells,"; BuyLots=",BuyLots,"; SellLots=",SellLots,
          ";\nPendingBuys=",PendingBuy,"; PendingSells=",PendingSell,"; PendingBuyLots=",PendingBuyLots,"; PendingSellLots=",PendingSellLots,
          ";\nUnBlockingProfit=",UnBlockingProfit);

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
double LOT()
{
   RefreshRates();

   double MINLOT = MarketInfo(Symbol(),MODE_MINLOT);
   double LOT;
   int RiskFactor = /*StopLoss*/ RiskPercent*100;
   if(RiskPercent==0)
   {
      LOT = AccountFreeMargin()*AccountLeverage()/1000/MarketInfo(Symbol(),MODE_MARGINREQUIRED)/15;
   }
   else
   {
      LOT = (((AccountEquity()/100)*RiskPerTrade)/RiskFactor)/NormalizeDouble(MarketInfo(Symbol(),MODE_TICKVALUE),2);
   }
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
         Profit += (OrderProfit() + OrderCommission() + OrderSwap());
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

   int    Pos;
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
            if(Result<=0){Error=GetLastError();log("LastError = "+Error);}
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
            if(Result<=0){Error=GetLastError();log("LastError = "+Error);}
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

   //log("OpenOrder - LotHedgeValue:" + LotOpenValue + "; AccountFreeMargin: "+AccountFreeMargin());

   if(AccountFreeMargin()>0)
   {
      /*log("MarketInfo(Symbol(),MODE_MARGINREQUIRED) == " + MarketInfo(Symbol(),MODE_MARGINREQUIRED));
      log("Ask("+Ask+")-MaxOpenPrice("+MaxOpenPrice+") == " + (Ask-MaxOpenPrice));
      log("MinOpenPrice("+MinOpenPrice+")-Bid("+Bid+") == " + (MinOpenPrice-Bid));*/
      if(Type==OP_BUY)
      {
         SL = 0;
         TP = 0;
         
         //if(stopLoss > 0){SL=Ask+stopLoss;}

         Result=OrderSend(Symbol(),OP_BUY,LotOpenValue,Ask,Slippage,SL,TP,"Charels_"+VER,MAGICMA,0,Blue);
         log("OpenOrder[OP_BUY] - Ask:" + Ask + "; Result: "+Result);
      }
      if(Type==OP_SELL)
      {
         SL = 0;
         TP = 0;
         
         //if(stopLoss > 0){SL=Bid-stopLoss;}

         Result=OrderSend(Symbol(),OP_SELL,LotOpenValue,Bid,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);
         log("OpenOrder[OP_SELL] - Bid:" + Bid + "; Result: "+Result);
      }
      //-----------------------
        if(Result<=0){Error=GetLastError();log("LastError = "+Error);}
        else Error=0;
      //-----------------------
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

   double OrderPips=0;
   double sl=0;
   
   if(OrderType()==OP_BUY)
   {
      OrderPips = (Bid - OrderOpenPrice())/*/Point*/;
  
      // adjust StopLoss
      if(stopLoss > 0 && OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stopLoss,OrderTakeProfit(),0,Blue);return(0);}

      if (sl < OrderPips - trailingStop)
      {
        sl=OrderPips - trailingStop;
        if (sl > MinPipsProfit*Point){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+sl,OrderTakeProfit(),0,Blue);return(0);}
      }

      /*if(trailingStop>0 && (Bid-trailingStop>OrderStopLoss() || OrderStopLoss()==0))
      {
         OrderModify(ticket, OrderOpenPrice(), Bid-trailingStop, 0,0,Blue);
         return(0);
      }
      
      //OrderModify(ticket, OrderOpenPrice(), OrderStopLoss(), 0,0,Blue);
      return(0);*/
   }
   else if (OrderType() == OP_SELL)
   {
      OrderPips = (OrderOpenPrice()-Ask)/*/Point*/;
  
      // adjust StopLoss
      if(stopLoss > 0 && OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stopLoss,OrderTakeProfit(),0,Blue);return(0);}
      
      if (sl < OrderPips - trailingStop)
      {
        sl=OrderPips - trailingStop;
        if (sl > MinPipsProfit*Point){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-sl,OrderTakeProfit(),0,Blue);return(0);}
      }

      /*if(trailingStop>0 && (Ask+trailingStop<OrderStopLoss() || OrderStopLoss()==0))
      {
         OrderModify(ticket, OrderOpenPrice(), Ask+trailingStop, 0,0,Red);
         return(0);
      }         

      //OrderModify(ticket, OrderOpenPrice(), OrderStopLoss(), 0,0,Red);
      return(0);*/
   }
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------
double normPrice (double price)
{
   return(NormalizeDouble(price,2)); 
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