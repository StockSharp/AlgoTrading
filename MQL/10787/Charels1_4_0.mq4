//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#include <hanover --- function header.mqh>
#include <hanover --- extensible functions.mqh>

#define MAGICMA  3939
#define VER "1.4.0"

#define DECIMAL_CONVERSION 10

extern string Comment_1           = " -- Comma separated list of Symbol Pairs: EURUSD,USDCHF";
extern string Comment_1a          = " --- must be equal to the string of your account provider.";
extern string Symbols             = "EURUSD";
extern double xFactor             = 1.5;
extern string TimeSet             = "07:32";

extern string Comment_2           = " -- Balance percentage order balance and Lot value";
extern double RiskPercent         = 10;

extern string Comment_3           = " -- Percentage of the risk for each Lot";
extern double RiskPerTrade        = 2;

extern string Comment_4           = " -- Fixed value if RiskPerTrade == 0";
extern double Lots                = 0.01;
extern double StopLoss            = 0;
extern double MinPipsProfit       = 30;
extern double TrailingUnlockStop  = 35;
extern int Slippage               = 3;


extern string Comment_5           = " -- Wheather or not manage ALL opened orders (manual too)";
extern bool AllOrders             = false;

extern string Comment_6           = " -- Wheather or not compute the Profit on ALL pairs";
extern bool AllSymbols            = true;

extern string Comment_7           = " -- Log to console or to a file";
extern bool LogToFile             = false;

extern string Comment_8           = " -- On init delete all pending orders";
extern bool InitResetOrders       = true;
 
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

double lastBarTime;

string SymbolsArray[0], CurrentSymbol;
bool canSell[0], canBuy[0];

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
   
   trailingStop         = TrailingUnlockStop * Point;
   stopLoss             = StopLoss * Point;
   
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
   
   //CurrentSymbol        = Symbol();
   
   string delim = ",";
   int size = 1+StringFindCount(Symbols,delim);
   ArrayResize(SymbolsArray,size);
   StrToStringArray(Symbols,SymbolsArray,delim);
   ArrayResize(canSell,size);
   ArrayResize(canBuy,size);

   for(i=0; i<size; i++)
   {
      canSell[i] = true;
      canBuy[i] = true;
   }

   lastBarTime          = TimeCurrent();
   
   if(InitResetOrders)
   {
      int    Pos;
      int    Total=OrdersTotal();
   
      if(Total>0)
      {for(i=Total-1; i>=0; i--) 
        {
         if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
          {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }//if

               Pos=OrderType();
               if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
   //-----------------------
               if(Result<=0){Error=GetLastError();Log("LastError = "+Error);}
               else Error=0;
   //-----------------------

          }//if
        }//for
      }//if
   
   }
}

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
{
   int SystemPeriod=1;

   if ( (TimeCurrent() - lastBarTime) >= SystemPeriod )
   {
      int s;
      for(s=0; s<ArraySize(SymbolsArray); s++)
      {
         CurrentSymbol = StringTrim(SymbolsArray[s]);
         
         //Alert(CurrentSymbol);
         
         Spread    = MarketInfo(CurrentSymbol, MODE_SPREAD);
         StopLevel = MarketInfo(CurrentSymbol, MODE_STOPLEVEL) + Spread;
   
         //if (trailingStop > 0 && trailingStop < StopLevel) trailingStop = StopLevel;

         // COMPUTING LOT-SIZE
         Count();

         if (RiskPerTrade <= 0)
         {
           LotsValue = Lots;
         }
         else
         {
           LotsValue = LOT();
         }

         Amount = normPrice(MarketInfo(CurrentSymbol,MODE_MINLOT));
      
         MaxPrice=iHigh(CurrentSymbol,PERIOD_D1,LastDay)+Delta*Point;
         MinPrice=iLow(CurrentSymbol,PERIOD_D1,LastDay)-Delta*Point;

         MaxOpenPrice=MaxPrice;
         MinOpenPrice=MinPrice;

         if (MarketInfo(CurrentSymbol,MODE_ASK)+StopLevel*Point>MaxPrice) MaxOpenPrice = MarketInfo(CurrentSymbol,MODE_ASK)+StopLevel*Point;
         if (MarketInfo(CurrentSymbol,MODE_BID)-StopLevel*Point<MinPrice) MinOpenPrice = MarketInfo(CurrentSymbol,MODE_BID)-StopLevel*Point;

         LotsHedgeValue = LotsValue*MathAbs(Buys-Sells)*xFactor;

         if(LotsHedgeValue == 0)
         {
            LotsHedgeValue = LotsValue;
         }
         LotsHedgeValue = normPrice(LotsHedgeValue);

         if(Buys!=0 || Sells!=0)
         {
            CheckForClose(s);
         }

         //SYSTEM CORE
         Count();

         double PivPnt=(iHigh(CurrentSymbol,PERIOD_H1,1)+iLow(CurrentSymbol,PERIOD_H1,1)+iClose(CurrentSymbol,PERIOD_H1,1)+iOpen(CurrentSymbol,PERIOD_H1,1))/4;

         double Res1=iHigh(CurrentSymbol,PERIOD_H4,0)+iATR(CurrentSymbol,PERIOD_H4,14,0);
         double Sup1=iLow(CurrentSymbol,PERIOD_H4,0)-iATR(CurrentSymbol,PERIOD_H4,14,0);
      
         double Res4=iHigh(CurrentSymbol,PERIOD_D1,0)+iATR(CurrentSymbol,PERIOD_D1,14,0);
         double Sup4=iLow(CurrentSymbol,PERIOD_D1,0)-iATR(CurrentSymbol,PERIOD_D1,14,0);
      
         double C1=iClose(CurrentSymbol,PERIOD_H1,1);
         double O1=iOpen(CurrentSymbol,PERIOD_H1,1);
         double L1=iLow(CurrentSymbol,PERIOD_H1,1);
         double H1=iHigh(CurrentSymbol,PERIOD_H1,1);
         double BandUP=iBands(CurrentSymbol,PERIOD_H1,20,2,0,PRICE_CLOSE,1,0);
         double BandDown=iBands(CurrentSymbol,PERIOD_H1,20,-2,0,PRICE_CLOSE,1,0);
         double L2=iLow(CurrentSymbol,PERIOD_H1,2);
         double H2=iHigh(CurrentSymbol,PERIOD_H1,2);
         double C2=iClose(CurrentSymbol,PERIOD_H1,2);
         double ADX=iADX(CurrentSymbol,PERIOD_H1,14,PRICE_CLOSE,MODE_MAIN,1);

         Result=0;Error=0;

         //LIMITI
         double Dw1=NormalizeDouble(Sup1-(Delta*Point)/*-(Spread*Point)*/,Digits);
         double Up1=NormalizeDouble(Res1+(Delta*Point)/*+(Spread*Point)*/,Digits);

         double Dw4=NormalizeDouble(Sup4-(Delta*Point)/*-(Spread*Point)*/,Digits);
         double Up4=NormalizeDouble(Res4+(Delta*Point)/*+(Spread*Point)*/,Digits);

         double DwA=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-(Anchor*Point)-(Delta*Point)/*-(Spread*Point)*/,Digits);
         double UpA=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+(Anchor*Point)+(Delta*Point)/*+(Spread*Point)*/,Digits);

         if (Dw1 < NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-(Delta*Point)-(Spread*Point),Digits))
         {
            Dw = Dw1;
         }
         else if (Dw4 < NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-(Delta*Point)-(Spread*Point),Digits))
         {
            Dw = Dw4;
         }
         else
         {
            Dw = DwA;
         }
      
         if (Up1 > NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+(Delta*Point)+(Spread*Point),Digits))
         {
            Up = Up1;
         }
         else if (Up4 > NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+(Delta*Point)+(Spread*Point),Digits))
         {
            Up = Up4;
         }
         else
         {
            Up = UpA;
         }

         //Dw=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-(Anchor*Point)-(Delta*Point)/*-(Spread*Point)*/,Digits);
         //Up=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+(Anchor*Point)+(Delta*Point)/*+(Spread*Point)*/,Digits);

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
            
               if(canSell[s])
               {
                  //if(stopLoss > 0){SL=Dw+stopLoss;}
                  
                  if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                  {
                     Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);
                     //-----------------------
                       if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();Log("LastError [CORE;OP_SELLSTOP]= "+Error);*/}
                       else Error=0;
                     //-----------------------
                     }               
               }
             }
       
             if(PendingBuy == 0 && Error==0)
             {

               //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys<Sells) {canBuy=false;}
            
               if(canBuy[s])
               {
                  //if(stopLoss > 0){SL=Up-stopLoss;}

                  if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                  {
                     Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Blue);
                     //-----------------------
                       if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();Log("LastError [CORE;OP_BUYSTOP] = "+Error);*/}
                       else Error=0;
                     //-----------------------
                  }
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

                     if(canSell[s])
                     {
                        //if(stopLoss > 0){SL=Dw+stopLoss;}
                     
                        if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                        {
                           Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);          
                           //-----------------------
                             if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();Log("LastError [CORE - HEDGE;OP_SELLSTOP]= "+Error);*/}
                             else Error=0;
                           //-----------------------
                        }
                     }
                  }
         
                  if(PendingSell > 0 && PendingBuy == 0 && Error==0)
                  {

                     //if(blockConditionHit /*&& Profit>-AccountBalance()*RiskPercent/100*/ && Buys<Sells) {canBuy=false;}

                     if(canBuy[s])
                     {
                        //if(stopLoss > 0){SL=Up-stopLoss;}

                        if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                        {
                           Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Blue);
                           //-----------------------
                             if(Result<=0){Error=GetLastError();/*DeleteAllPendingOrders();Log("LastError [CORE - HEDGE;OP_BUYSTOP] = "+Error);*/}
                             else Error=0;
                           //-----------------------
                        }
                     }
                  }      
             }
         }

         lastBarTime = TimeCurrent();

         CharlesStatus();

      }
   }

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose(int s)
{
   RefreshRates();
   
   Count();

   //if(AccountBalance()>=1000){Amount=10.0+(AccountBalance()/10000);}
   //else{Amount=1.0+(AccountBalance()/1000);}

   //CONDIZIONE DI CHIUSURA DI SUCCESSO
   if((Profit>=0 && Profit>=Amount) || (UnBlockingProfit>0 && UnBlockingProfit>-LooserProfit))
   {
      //Log("[CONDIZIONE DI CHIUSURA DI SUCCESSO]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
      
      if(!isInGain)
      {
         isInGain = true;
         LastProfit = Profit;
      }
   
      if(Profit<=(LastProfit - Amount/10) || UnBlockingProfit>-LooserProfit)
      {
         //Log("[CONDIZIONE DI CHIUSURA DI SUCCESSO - CloseAll]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
         isInGain = false;
         blockConditionHit=false;
         unlockOrdersPlaced=false;
         LastOrderTicket = -1;
         UnlockingOrderTicket = -1;
         CloseAll();
         UnBlockingProfit=0;
         LooserProfit=0;

         for(int sx=0; sx<ArraySize(canSell); sx++)
         {
            canSell[sx] = true;
            canBuy[sx] = true;
         }

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
         //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         blockConditionHit=true;
         DeleteAllPendingOrders();      
         if (SellLots>BuyLots)
         {
            LastOrderTicket=OpenOrder(SellLots-BuyLots, OP_BUY);
            //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
         }
         else if (BuyLots>SellLots)
         {
            LastOrderTicket=OpenOrder(BuyLots-SellLots, OP_SELL);
            //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: BuyLots>SellLots; Opened SELL Order Ticket: " + LastOrderTicket);
         }
         else
         {
            for(i=OrdersTotal(); i>=0; i--)
            {          
                OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
                if(OrderSymbol()==CurrentSymbol)
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
          if(OrderSymbol()==CurrentSymbol)
          {
            if(!AllOrders && OrderMagicNumber()!=MAGICMA)
            {
               continue;
            }//if

            if(OrderTicket()<=LastOrderTicket)
            {
               if(OrderProfit()<0)
               {
                  if(OrderType() == OP_BUY && canBuy[s])
                  {
                     canBuy[s]=false;
                  }
                  if(OrderType() == OP_SELL && canSell[s])
                  {
                     canSell[s]=false;
                  }
               }
               else
               {
                  if(OrderType() == OP_BUY && !canBuy[s])
                  {
                     canBuy[s]=true;
                  }
                  if(OrderType() == OP_SELL && !canSell[s])
                  {
                     canSell[s]=true;
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
         }//if
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

   double MINLOT = MarketInfo(CurrentSymbol,MODE_MINLOT);
   double LOT;
   int RiskFactor = /*StopLoss*/ RiskPercent*100;
   if(RiskPercent==0)
   {
      LOT = AccountFreeMargin()*AccountLeverage()/1000/MarketInfo(CurrentSymbol,MODE_MARGINREQUIRED)/15;
   }
   else
   {
      LOT = (((AccountEquity()/100)*RiskPerTrade)/RiskFactor)/NormalizeDouble(MarketInfo(CurrentSymbol,MODE_TICKVALUE),2);
   }
   if (LOT>MarketInfo(CurrentSymbol,MODE_MAXLOT)) LOT = MarketInfo(CurrentSymbol,MODE_MAXLOT);
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
      if(!AllSymbols && OrderSymbol()!=CurrentSymbol)
      {
         continue;
      }//if

      if(!AllOrders && OrderMagicNumber()!=MAGICMA)
      {
         continue;
      }//if
      
      Profit += (OrderProfit() + OrderCommission() + OrderSwap());
      if(OrderType()==OP_SELL){SellLots=SellLots+OrderLots();Sells++;}
      if(OrderType()==OP_BUY){BuyLots=BuyLots+OrderLots();Buys++;}
      
      if(OrderSymbol()==CurrentSymbol)
      {
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
         if(!AllSymbols && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if 

         if(!AllOrders && OrderMagicNumber()!=MAGICMA)
         {
            continue;
         }//if
         
            Pos=OrderType();
            if(Pos==OP_BUY){Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(CurrentSymbol,MODE_BID), Slippage, Blue);}
            if(Pos==OP_SELL){Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(CurrentSymbol,MODE_ASK), Slippage, Red);}
            if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
            if(Result<=0){Error=GetLastError();Log("LastError = "+Error);}
            else Error=0;
//-----------------------
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
         if(!AllSymbols && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if 

         if(!AllOrders && OrderMagicNumber()!=MAGICMA)
         {
            continue;
         }//if

            Pos=OrderType();
            if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
//-----------------------
            if(Result<=0){Error=GetLastError();Log("LastError = "+Error);}
            else Error=0;
//-----------------------

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

   //Log("OpenOrder - LotHedgeValue:" + LotOpenValue + "; AccountFreeMargin: "+AccountFreeMargin());

   if(AccountFreeMargin()>0)
   {
      /*Log("MarketInfo(CurrentSymbol,MODE_MARGINREQUIRED) == " + MarketInfo(CurrentSymbol,MODE_MARGINREQUIRED));
      Log("Ask("+MarketInfo(CurrentSymbol,MODE_ASK)+")-MaxOpenPrice("+MaxOpenPrice+") == " + (MarketInfo(CurrentSymbol,MODE_ASK)-MaxOpenPrice));
      Log("MinOpenPrice("+MinOpenPrice+")-Bid("+MarketInfo(CurrentSymbol,MODE_BID)+") == " + (MinOpenPrice-MarketInfo(CurrentSymbol,MODE_BID)));*/
      if(Type==OP_BUY)
      {
         SL = 0;
         TP = 0;
         
         //if(stopLoss > 0){SL=MarketInfo(CurrentSymbol,MODE_ASK)+stopLoss;}

         if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotOpenValue)<=0 || GetLastError()==134)
         {
            return(-1);
         }
         
         Result=OrderSend(CurrentSymbol,OP_BUY,LotOpenValue,MarketInfo(CurrentSymbol,MODE_ASK),Slippage,SL,TP,"Charels_"+VER,MAGICMA,0,Blue);
         Log("OpenOrder[OP_BUY] - Ask:" + MarketInfo(CurrentSymbol,MODE_ASK) + "; Result: "+Result);
      }
      if(Type==OP_SELL)
      {
         SL = 0;
         TP = 0;
         
         //if(stopLoss > 0){SL=MarketInfo(CurrentSymbol,MODE_BID)-stopLoss;}

         if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotOpenValue)<=0 || GetLastError()==134)
         {
            return(-1);
         }

         Result=OrderSend(CurrentSymbol,OP_SELL,LotOpenValue,MarketInfo(CurrentSymbol,MODE_BID),Slippage,SL,TP,"Charles_"+VER,MAGICMA,0,Red);
         Log("OpenOrder[OP_SELL] - Bid:" + MarketInfo(CurrentSymbol,MODE_BID) + "; Result: "+Result);
      }
      //-----------------------
        if(Result<=0){Error=GetLastError();Log("LastError = "+Error);}
        else Error=0;
      //-----------------------
   }

//-----------------------
   if(Result==-1){Error=GetLastError();Log("LastError = "+Error);}
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
      OrderPips = (MarketInfo(CurrentSymbol,MODE_BID) - OrderOpenPrice())/*/Point*/;
  
      // adjust StopLoss
      if(stopLoss > 0 && OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-stopLoss,OrderTakeProfit(),0,Blue);return(0);}

      if (sl < OrderPips - trailingStop)
      {
        sl=OrderPips - trailingStop;
        if (sl > MinPipsProfit*Point){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+sl,OrderTakeProfit(),0,Blue);return(0);}
      }

      /*if(trailingStop>0 && (MarketInfo(CurrentSymbol,MODE_BID)-trailingStop>OrderStopLoss() || OrderStopLoss()==0))
      {
         OrderModify(ticket, OrderOpenPrice(), MarketInfo(CurrentSymbol,MODE_BID)-trailingStop, 0,0,Blue);
         return(0);
      }
      
      //OrderModify(ticket, OrderOpenPrice(), OrderStopLoss(), 0,0,Blue);
      return(0);*/
   }
   else if (OrderType() == OP_SELL)
   {
      OrderPips = (OrderOpenPrice()-MarketInfo(CurrentSymbol,MODE_ASK))/*/Point*/;
  
      // adjust StopLoss
      if(stopLoss > 0 && OrderStopLoss()==0){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+stopLoss,OrderTakeProfit(),0,Blue);return(0);}
      
      if (sl < OrderPips - trailingStop)
      {
        sl=OrderPips - trailingStop;
        if (sl > MinPipsProfit*Point){OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-sl,OrderTakeProfit(),0,Blue);return(0);}
      }

      /*if(trailingStop>0 && (MarketInfo(CurrentSymbol,MODE_ASK)+trailingStop<OrderStopLoss() || OrderStopLoss()==0))
      {
         OrderModify(ticket, OrderOpenPrice(), MarketInfo(CurrentSymbol,MODE_ASK)+trailingStop, 0,0,Red);
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
void Log(string str)
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