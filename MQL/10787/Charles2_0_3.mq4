//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#define VER "2.0.3"

#define DECIMAL_CONVERSION 10

extern string Comment_1           = " -- Comma separated list of Symbol Pairs: EURUSD,USDCHF";
extern string Comment_1a          = " --- must be equal to the string of your account provider.";
//extern string Symbols             = "EURUSDm,USDCHFm,GBPUSDm,EURGBPm,EURJPYm,USDJPYm,AUDNZDm,AUDUSDm,NZDUSDm";
extern string Symbols;
extern int    MagicNumber         = 3939;
extern double xFactor             = 1.5;
extern string TimeSet             = "07:32";

extern string Comment_2           = " -- Balance percentage order balance and Lot value";
extern double RiskPercent         = 10;

extern string Comment_3           = " -- Percentage of the risk for each Lot";
extern string Comment_3a          = " --- auto-adapted to number of pairs.";
extern double RiskPerTrade        = 10;

extern string Comment_4           = " -- Whether or not use more strict supports and resistances";
extern bool   Aggressive          = false;

extern string Comment_5           = " -- Whether or not leave open profitable orders";
extern bool   TrendingMode        = false;
extern int    FastPeriod          = 18;
extern int    SlowPeriod          = 60;
extern int    Selectivity         = 14;

extern string Comment_6           = " -- Fixed value if RiskPerTrade == 0";
extern double Lots                = 0.01;
extern int    Slippage            = 3;
extern string Comment_7           = " -- Set to true if broker is ECN/STP needing stops adding after order";
extern bool   ECN                 = false;

extern string Comment_8           = " -- On all orders";
extern int    StopLoss            = 800;
extern int    TrailStart          = 6;
extern int    TrailingAmount      = 3;
extern int    TakeProfit          = 20;

extern string Comment_9           = " -- Whether or not manage ALL opened orders (manual too)";
extern bool   AllOrders           = true;

extern string Comment_10           = " -- Whether or not compute the Profit on ALL pairs";
extern bool   AllSymbols          = false;

extern string Comment_11          = " -- Log to console or to a file";
extern bool   LogToFile           = false;

extern string Comment_12          = " -- On init delete all pending orders";
extern bool   InitResetOrders     = true;
 
int Anchor, PendingBuy, PendingSell, Buys, Sells, i, StopLevel, Spread;

double BuyLots, SellLots, PendingBuyLots, PendingSellLots;
double Amount, Profit, LastProfit, Up, Dw, SL, TP;
double LotsValue, LotsHedgeValue;
double MaxPrice,MinPrice,MaxOpenPrice,MinOpenPrice;

bool isInGain=false, blockConditionHit=false, unlockOrdersPlaced=false; 
int LastOrderTicket, UnlockingOrderTicket, Result, Error;
int Delta;        //Order price shift (in points) from High/Low price
int LastDay;

double stopLoss, delta;
double trailStart, trailAmount, takeProfit;

double lastBarTime;

string SymbolsArray[0], CurrentSymbol;
bool canSell[0], canBuy[0], bullish, bearish;

bool Inited[0];

//+------------------------------------------------------------------+
//| Init function                                                    |
//+------------------------------------------------------------------+
void init()
{
   RefreshRates();
      
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
   
   string delim = ",";
   int size;
   if(StringLen(Symbols)==0)
   {
      size = 1;
   }
   else
   {
      size = 1+StringFindCount(Symbols,delim);
   }
   ArrayResize(SymbolsArray,size);
      if(StringLen(Symbols)>0){StrToStringArray(Symbols,SymbolsArray,delim);}
   ArrayResize(canSell,size);
   ArrayResize(canBuy,size);
   ArrayResize(Inited,size);

   for(i=0; i<size; i++)
   {
      canSell[i] = true;
      canBuy[i] = true;
      Inited[i] = false;
   }

   lastBarTime = TimeCurrent();
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
         if(StringLen(SymbolsArray[s])>0)
         {
            CurrentSymbol = StringTrim(SymbolsArray[s]);
         }
         else
         {
            CurrentSymbol = Symbol();
         }
         
         if(Inited[s] == false)
         {
            if(InitResetOrders == true)
            {
               int    Pos;
               int    Total=OrdersTotal();
   
               if(Total>0)
               {
                  for(i=Total-1; i>=0; i--) 
                  {
                     if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE)
                     {
                        if(OrderSymbol()!=CurrentSymbol)
                        {
                           continue;
                        }//if

                        if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
                        {
                           continue;
                        }//if

                        Pos=OrderType();
                        if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
            //-----------------------
                        if(Result<0){Error=GetLastError();Log("LastError = "+Error);}
                        else Error=0;
            //-----------------------
                     }//if
                  }//for
               }//if
            }//if
            Inited[s]=true;
         }//if

         Spread    = MarketInfo(CurrentSymbol, MODE_SPREAD);
         StopLevel = MarketInfo(CurrentSymbol, MODE_STOPLEVEL) + Spread;
   
         if (MarketInfo(CurrentSymbol, MODE_DIGITS) == 3 || MarketInfo(CurrentSymbol, MODE_DIGITS) == 5)   {
            stopLoss             = NormalizeDouble(StopLoss* DECIMAL_CONVERSION * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            delta                = NormalizeDouble(Delta * DECIMAL_CONVERSION * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            trailStart           = NormalizeDouble(TrailStart * DECIMAL_CONVERSION * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            trailAmount          = NormalizeDouble(TrailingAmount * DECIMAL_CONVERSION * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            takeProfit           = NormalizeDouble(TakeProfit * DECIMAL_CONVERSION * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
         }
         else
         {
            stopLoss             = NormalizeDouble(StopLoss * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            delta                = NormalizeDouble(Delta * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            trailStart           = NormalizeDouble(TrailStart * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            trailAmount          = NormalizeDouble(TrailingAmount * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
            takeProfit           = NormalizeDouble(TakeProfit * MarketInfo(CurrentSymbol, MODE_POINT),MarketInfo(CurrentSymbol, MODE_DIGITS));
         }

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
      
         MaxPrice=iHigh(CurrentSymbol,PERIOD_D1,LastDay)+delta;
         MinPrice=iLow(CurrentSymbol,PERIOD_D1,LastDay)-delta;

         MaxOpenPrice=MaxPrice;
         MinOpenPrice=MinPrice;

         if (MarketInfo(CurrentSymbol,MODE_ASK)+StopLevel*MarketInfo(CurrentSymbol, MODE_POINT)>MaxPrice) MaxOpenPrice = MarketInfo(CurrentSymbol,MODE_ASK)+StopLevel*MarketInfo(CurrentSymbol, MODE_POINT);
         if (MarketInfo(CurrentSymbol,MODE_BID)-StopLevel*MarketInfo(CurrentSymbol, MODE_POINT)<MinPrice) MinOpenPrice = MarketInfo(CurrentSymbol,MODE_BID)-StopLevel*MarketInfo(CurrentSymbol, MODE_POINT);

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
         
         int limitPeriod1, limitPeriod4;
         
         if(Aggressive)
         {
            limitPeriod1=PERIOD_H1;
            limitPeriod4=PERIOD_H4;
         }
         else
         {
            limitPeriod1=PERIOD_H4;
            limitPeriod4=PERIOD_D1;
         }
         
         double Res1=iHigh(CurrentSymbol,limitPeriod1,0)+iATR(CurrentSymbol,limitPeriod1,14,0);
         double Sup1=iLow(CurrentSymbol,limitPeriod1,0)-iATR(CurrentSymbol,limitPeriod1,14,0);
      
         double Res4=iHigh(CurrentSymbol,limitPeriod4,0)+iATR(CurrentSymbol,limitPeriod4,14,0);
         double Sup4=iLow(CurrentSymbol,limitPeriod4,0)-iATR(CurrentSymbol,limitPeriod4,14,0);
      
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

         double EMAF1 = iMA(CurrentSymbol,Period(),FastPeriod,0,MODE_EMA,PRICE_WEIGHTED,0); //fast
         double EMAS1 = iMA(CurrentSymbol,Period(),SlowPeriod,0,MODE_EMA,PRICE_WEIGHTED,0); //slow

         double RSI = iRSI(CurrentSymbol,PERIOD_M5,Selectivity,PRICE_WEIGHTED,0);
         double RSI2 = iRSI(CurrentSymbol,PERIOD_M5,Selectivity,PRICE_WEIGHTED,7);
         double RSIM1 = iRSI(CurrentSymbol,PERIOD_M15,Selectivity,PRICE_WEIGHTED,0);
         double RSIM2 = iRSI(CurrentSymbol,PERIOD_M15 ,Selectivity,PRICE_WEIGHTED,7);

         double RSIL1 = iRSI(CurrentSymbol,PERIOD_M30 ,Selectivity,PRICE_WEIGHTED,0);
         double RSIL2 = iRSI(CurrentSymbol,PERIOD_M30 ,Selectivity,PRICE_WEIGHTED,7);
         //RSITrend =  iRSI(NULL,PERIOD_H4,Selectivity,PRICE_WEIGHTED,0);

         bullish=((RSI>55&&RSIM1>55&&RSIL1>55)&&(RSIL2<50&&RSIM2<50&&RSI2<50)&&EMAS1>EMAF1);
         bearish=((RSI<55&&RSIM1<55&&RSIL1<55)&&(RSIL2>50&&RSIM2>50&&RSI2>50)&&EMAS1<EMAF1);

         Result=0;Error=0;

         //LIMITI
         double Dw1=NormalizeDouble(Sup1-delta-(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));
         double Up1=NormalizeDouble(Res1+delta+(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));

         double Dw4=NormalizeDouble(Sup4-delta-(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));
         double Up4=NormalizeDouble(Res4+delta+(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));

         double DwA=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-(Anchor*MarketInfo(CurrentSymbol, MODE_POINT))-delta-(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));
         double UpA=NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+(Anchor*MarketInfo(CurrentSymbol, MODE_POINT))+delta+(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS));

         if (Dw1 < NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-delta-(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS)))
         {
            Dw = Dw1;
         }
         else if (Dw4 < NormalizeDouble(MarketInfo(CurrentSymbol,MODE_ASK)-delta-(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS)))
         {
            Dw = Dw4;
         }
         else
         {
            Dw = DwA;
         }
      
         if (Up1 > NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+delta+(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS)))
         {
            Up = Up1;
         }
         else if (Up4 > NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID)+delta+(Spread*MarketInfo(CurrentSymbol, MODE_POINT)),MarketInfo(CurrentSymbol, MODE_DIGITS)))
         {
            Up = Up4;
         }
         else
         {
            Up = UpA;
         }

         if((PendingSell==0 && PendingBuy==0) || (TimeStr(CurTime())==TimeSet))
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
               if(canSell[s])
               {
                  if(ECN == false && stopLoss > 0){SL=Dw+stopLoss;}
                  
                  if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                  {
                     Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);
                     //-----------------------
                       if(Result<0){Error=GetLastError();Log("LastError [CORE;OP_SELLSTOP]= "+Error);}
                       else Error=0;
                     //-----------------------
                     }               
               }
             }
       
             if(PendingBuy == 0 && Error==0)
             {
               if(canBuy[s])
               {
                  if(ECN == false && stopLoss > 0){SL=Up-stopLoss;}

                  if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                  {
                     Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Blue);
                     //-----------------------
                       if(Result<0){Error=GetLastError();Log("LastError [CORE;OP_BUYSTOP] = "+Error);}
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
                     if(TrendingMode==true && (bearish==false || bullish==true) )
                     {
                        canSell[s]=false;
                     }

                     if(canSell[s])
                     {
                        if(ECN == false && stopLoss > 0){SL=Dw+stopLoss;}
                     
                        if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                        {
                           Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);          
                           //-----------------------
                             if(Result<0){Error=GetLastError();Log("LastError [CORE - HEDGE;OP_SELLSTOP]= "+Error);}
                             else if(Result!=0)
                             {
                              Error=0;
                              /*DeleteAllPendingOrders(OP_BUY);
                              if(stopLoss > 0){SL=Up-stopLoss;}
                              OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Blue);*/
                             }
                           //-----------------------
                        }
                     }
                  }
         
                  if(PendingSell > 0 && PendingBuy == 0 && Error==0)
                  {
                     if(TrendingMode==true && (bearish==true || bullish==false) )
                     {
                        canBuy[s]=false;
                     }

                     if(canBuy[s])
                     {
                        if(ECN == false && stopLoss > 0){SL=Up-stopLoss;}

                        if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountBalance()*0.1) && GetLastError()!=134)
                        {
                           Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Blue);
                           //-----------------------
                             if(Result<0){Error=GetLastError();Log("LastError [CORE - HEDGE;OP_BUYSTOP] = "+Error);}
                             else if(Result!=0)
                             {
                              Error=0;
                              /*DeleteAllPendingOrders(OP_SELL);
                              if(stopLoss > 0){SL=Dw+stopLoss;}
                              OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);*/
                             }
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

   for(i=OrdersTotal(); i>=0; i--)
   {          
       OrderSelect(i,SELECT_BY_POS,MODE_TRADES);

         if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if

         if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
         {
            continue;
         }//if

         CheckTrailingStop(OrderTicket());
   }//for
   
   Count();

   int sx;   
   if(Profit>=0 && Profit>=Amount/* && Buys>0 && Sells>0*/)
   {
      if(TrendingMode==false)
      {
         //Log("[CONDIZIONE DI CHIUSURA DI SUCCESSO]; Profit: "+Profit+"; Amount: "+Amount+"; LastProfit: "+LastProfit);
         if(isInGain==false)
         {
            isInGain = true;
            LastProfit = Profit;
         }

         if(Profit<=(LastProfit - Amount/10))
         {
            isInGain = false;
            blockConditionHit=false;
            unlockOrdersPlaced=false;
            LastOrderTicket = -1;
            UnlockingOrderTicket = -1;

            for(sx=0; sx<ArraySize(canSell); sx++)
            {
               canSell[sx] = true;
               canBuy[sx] = true;
            }
            CloseAll();
         }
         else
         {
            LastProfit = Profit;
         }
      }
      else
      {
         if(isInGain==false)
         {
            isInGain = true;
            CloseAll();
         }
      }
   }
   
   RefreshRates();
   Count();
   
   if(TrendingMode==true && isInGain==true && Profit==0 && Buys==0 && Sells==0)
   {
      isInGain = false;
      blockConditionHit=false;
      unlockOrdersPlaced=false;
      LastOrderTicket = -1;
      UnlockingOrderTicket = -1;

      for(sx=0; sx<ArraySize(canSell); sx++)
      {
         canSell[sx] = true;
         canBuy[sx] = true;
      }
      CloseAll();
   }

   RefreshRates();
   Count();

   //CONDIZIONE DI BLOCCAGGIO E SBLOCCAGGIO DI EMERGENZA
   if(Profit<-AccountBalance()*RiskPercent/100)
   {
      if(blockConditionHit==false)
      {
         //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         blockConditionHit=true;
         DeleteAllPendingOrders();      
         if(SellLots>0 && BuyLots>=0 && SellLots>BuyLots)
         {
            LastOrderTicket=OpenOrder(SellLots-BuyLots, OP_BUY);
            //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
         }
         else if(BuyLots>0 && SellLots>=0 && BuyLots>SellLots)
         {
            LastOrderTicket=OpenOrder(BuyLots-SellLots, OP_SELL);
            //Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: BuyLots>SellLots; Opened SELL Order Ticket: " + LastOrderTicket);
         }
         else
         {
            for(i=OrdersTotal(); i>=0; i--)
            {          
               OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
               if(OrderSymbol()==CurrentSymbol && OrderTicket()>LastOrderTicket)
               {
                  if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
                  {
                     continue;
                  }//if
                  
                  LastOrderTicket=OrderTicket();
                  break;
               }//if
            }
         }
      }
   }

   if(blockConditionHit==true)
   {
      for(i=OrdersTotal(); i>=0; i--)
      {          
          OrderSelect(i,SELECT_BY_POS,MODE_TRADES);

            if(OrderSymbol()!=CurrentSymbol)
            {
               continue;
            }//if

            if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
            {
               continue;
            }//if

            if(OrderTicket()<=LastOrderTicket)
            {
               if((OrderProfit() + OrderCommission() + OrderSwap())<0)
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
            
            /*if(OrderTicket()>LastOrderTicket)
            {
               if((OrderProfit() + OrderCommission() + OrderSwap())<0)
               {
                  switch(OrderType())
                  {
                     case OP_BUY:
                        if(canSell[s]){OpenOrder(LotsHedgeValue, OP_SELL);canSell[s]=false;}
                        continue;
                     case OP_SELL:
                        if(canBuy[s]){OpenOrder(LotsHedgeValue, OP_BUY);canBuy[s]=false;}
                        continue;
                  }
               }
            }*/
      }//for
   }//if
   
   CharlesStatus();                  
}  

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CharlesStatus()
{

   Count();
   
   if(Buys == 0 && Sells == 0)
   {
      for(int sx=0; sx<ArraySize(canSell); sx++)
      {
         canSell[sx] = true;
         canBuy[sx] = true;
      }
   }
   
   double LookingFor = Amount;
   
   if(Profit>0 && Profit>=Amount/10.0) {LookingFor=(LastProfit - Amount/10.0);}

   Comment("Charles ",VER," - Gain= ",Profit," LookingFor= ",LookingFor," RiskPercent= ",(-AccountBalance()*RiskPercent/100),
          ";\nBalance=",AccountBalance(),"; FreeMargin=", AccountFreeMargin(),"; Equity=", AccountEquity(),
          ";\nBuy=",Buys,"; Sell=", Sells,"; BuyLots=",BuyLots,"; SellLots=",SellLots,
          ";\nPendingBuys=",PendingBuy,"; PendingSells=",PendingSell,"; PendingBuyLots=",PendingBuyLots,"; PendingSellLots=",PendingSellLots);

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
double LOT()
{
   RefreshRates();

   double MINLOT = MarketInfo(CurrentSymbol,MODE_MINLOT);
   double LOT;
   int RiskFactor = RiskPercent*100;
   if(RiskPercent==0 || RiskPerTrade==0)
   {
      LOT = AccountFreeMargin()*AccountLeverage()/1000/MarketInfo(CurrentSymbol,MODE_MARGINREQUIRED)/15;
   }
   else
   {
      LOT = (( ( AccountEquity()/100 )*( RiskPerTrade/ArraySize(SymbolsArray) ))/RiskFactor )/NormalizeDouble(MarketInfo(CurrentSymbol,MODE_TICKVALUE),2);
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
      if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
      {
         continue;
      }//if

      if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
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
   double BuysProfit=0,SellsProfit=0;
   
   if(Total>0)
   {

     for(i=Total-1; i>=0; i--) 
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {
         if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if 

         if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
         {
            continue;
         }//if
         
            Pos=OrderType();
            switch(Pos)
            {
               case OP_BUY:
                  if(TrendingMode==true)
                  {
                     //if(bearish==false && bullish==true && OrderStopLoss()>OrderOpenPrice())continue;
                     if(OrderStopLoss()>OrderOpenPrice())continue;
                  }//if
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(CurrentSymbol,MODE_BID), Slippage, Blue);
                  BuysProfit += OrderProfit()+OrderCommission()+OrderSwap();
                  continue;
                  
               case OP_SELL:
                  if(TrendingMode==true)
                  {
                     //if(bearish==true && bullish==false && OrderStopLoss()>OrderOpenPrice())continue;
                     if(OrderStopLoss()>OrderOpenPrice())continue;
                  }//if
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(CurrentSymbol,MODE_ASK), Slippage, Red);
                  SellsProfit += OrderProfit()+OrderCommission()+OrderSwap();
                  continue;
            }
//-----------------------
            if(Result<0){Error=GetLastError();Log("LastError = "+Error);}
            else Error=0;
//-----------------------

            //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
            while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();
            
       }//if
     }//for

   }//if

   Total=OrdersTotal();
   
   if(Total>0)
   {

     for(i=Total-1; i>=0; i--) 
     {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == TRUE) 
       {
         if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if 

         if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
         {
            continue;
         }//if
         
            Pos=OrderType();
            switch(Pos)
            {
               case OP_BUYSTOP:
               case OP_BUYLIMIT:
               case OP_SELLSTOP:
               case OP_SELLLIMIT:
                  Result=OrderDelete(OrderTicket(), CLR_NONE);
                  continue;
            }
//-----------------------
            if(Result<0){Error=GetLastError();Log("LastError = "+Error);}
            else Error=0;
//-----------------------

            //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
            while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();

       }//if
     }//for

   }//if
   
   return(0);
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void DeleteAllPendingOrders(int PosClose=-1)
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
         if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
         {
            continue;
         }//if 

         if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
         {
            continue;
         }//if

            Pos=OrderType();
            switch(PosClose)
            {
             case -1:
               if((Pos==OP_BUYSTOP)||(Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
               break;
             case OP_BUY:              
               if((Pos==OP_BUYSTOP)||(Pos==OP_SELLLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
               break;
             case OP_SELL:
               if((Pos==OP_SELLSTOP)||(Pos==OP_BUYLIMIT)){Result=OrderDelete(OrderTicket(), CLR_NONE);}
               break;
            }
//-----------------------
            if(Result<0){Error=GetLastError();Log("LastError = "+Error);}
            else Error=0;
//-----------------------

            //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
            while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();

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
         
         if(ECN == false && stopLoss > 0){SL=MarketInfo(CurrentSymbol,MODE_ASK)+stopLoss;}

         if(AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotOpenValue)<=0 || GetLastError()==134)
         {
            return(-1);
         }
         
         Result=OrderSend(CurrentSymbol,OP_BUY,LotOpenValue,MarketInfo(CurrentSymbol,MODE_ASK),Slippage,SL,TP,"Charels_"+VER,MagicNumber,0,Blue);
         Log("OpenOrder[OP_BUY] - Ask:" + MarketInfo(CurrentSymbol,MODE_ASK) + "; Result: "+Result);
      }
      if(Type==OP_SELL)
      {
         SL = 0;
         TP = 0;
         
         if(ECN == false && stopLoss > 0){SL=MarketInfo(CurrentSymbol,MODE_BID)-stopLoss;}

         if(AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotOpenValue)<=0 || GetLastError()==134)
         {
            return(-1);
         }

         Result=OrderSend(CurrentSymbol,OP_SELL,LotOpenValue,MarketInfo(CurrentSymbol,MODE_BID),Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);
         Log("OpenOrder[OP_SELL] - Bid:" + MarketInfo(CurrentSymbol,MODE_BID) + "; Result: "+Result);
      }
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

   if(OrderStopLoss()==0 && stopLoss>0)
   {
      switch(OrderType())
      {
         case OP_BUY:
            OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice()-stopLoss, OrderTakeProfit(), 0, Blue);
            break;
         case OP_SELL:
            OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice()+stopLoss, OrderTakeProfit(), 0, Blue);
            break;
      }
   }

   Sleep(100);
   RefreshRates();
   
   double profitPips, increments, sl;
   double chgFromOpen;
   
   //MaloMax: inoltre il tuo ea fai un traling strettissimo ad ogni Point.
   //         potresti inserire un tralingstep=0.0001
   //
   //             if( sl-tralingstep > NormalizeDouble(OrderStopLoss(), MarketInfo(CurrentSymbol,MODE_DIGITS)) ) {
   //
   //          cosi che lo SL viene spostato solo ogni pips
   double tralingstep = MarketInfo(CurrentSymbol,MODE_POINT);

   if( OrderType() == OP_BUY ) {
      chgFromOpen = NormalizeDouble(MarketInfo(CurrentSymbol,MODE_BID) - OrderOpenPrice(), MarketInfo(CurrentSymbol,MODE_DIGITS));
      
      if( (TimeCurrent() - OrderOpenTime()) /*/ 3600*/ > 2 * PERIOD_D1 && OrderProfit() < 0 && OrderTakeProfit() == 0 && takeProfit > 0)
      {
         return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), MarketInfo(CurrentSymbol,MODE_BID)+takeProfit, 0, Blue) );
      }

      // move to break even
      if( /*TrendingMode==false &&*/ chgFromOpen >= trailStart && chgFromOpen > (StopLevel * MarketInfo(CurrentSymbol,MODE_POINT)) &&
          NormalizeDouble(OrderStopLoss(),MarketInfo(CurrentSymbol,MODE_DIGITS)) < NormalizeDouble(OrderOpenPrice(), MarketInfo(CurrentSymbol,MODE_DIGITS)) && trailStart != 0 ) 
      {
         Print("Moving stop to breakeven on order " + OrderTicket() + ". Bid is " + DoubleToStr( MarketInfo(CurrentSymbol,MODE_BID), MarketInfo(CurrentSymbol,MODE_DIGITS) ) + ", Trail start is " + DoubleToStr( trailStart, MarketInfo(CurrentSymbol,MODE_DIGITS) ) );
         return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Blue) );
      }
   
      profitPips = MarketInfo(CurrentSymbol,MODE_BID) - ( trailStart + OrderOpenPrice() ) ;
      if( trailAmount != 0) 
         increments = MathFloor( profitPips / trailAmount ); 
      else 
         increments = 0;
   
   
      if ( increments >= 1 && MarketInfo(CurrentSymbol,MODE_BID) >= OrderOpenPrice() + trailStart + ( increments * trailAmount ) ) {
         sl = NormalizeDouble( OrderOpenPrice() + ( (increments-1)  * trailAmount ), MarketInfo(CurrentSymbol,MODE_DIGITS));
            
         if( sl-tralingstep > NormalizeDouble(OrderStopLoss(), MarketInfo(CurrentSymbol,MODE_DIGITS)) ) {
      
            if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Blue ) ){
               Print("Trailng stop updated. Total increments: " + DoubleToStr(increments, MarketInfo(CurrentSymbol,MODE_DIGITS)) );
               return(true);
            }
         }
      }
   }

   if( OrderType() == OP_SELL ) {
      chgFromOpen = NormalizeDouble(OrderOpenPrice() - MarketInfo(CurrentSymbol,MODE_ASK), MarketInfo(CurrentSymbol,MODE_DIGITS));

      if( (TimeCurrent() - OrderOpenTime()) /*/ 3600*/ > 2 * PERIOD_D1 && OrderProfit() < 0 && OrderTakeProfit() == 0 && takeProfit > 0)
      {
         return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), MarketInfo(CurrentSymbol,MODE_ASK)-takeProfit, 0, Red) );
      }
      
      // move to break even
      if( /*TrendingMode==false &&*/ chgFromOpen >= trailStart && chgFromOpen > (StopLevel * MarketInfo(CurrentSymbol,MODE_POINT)) &&
         NormalizeDouble(OrderStopLoss(),MarketInfo(CurrentSymbol,MODE_DIGITS)) > NormalizeDouble(OrderOpenPrice(), MarketInfo(CurrentSymbol,MODE_DIGITS)) && trailStart != 0 ) {
         Print("Moving stop to breakeven on order " + OrderTicket() + ". Ask is " + DoubleToStr( MarketInfo(CurrentSymbol,MODE_ASK), MarketInfo(CurrentSymbol,MODE_DIGITS) ) + ", Trail start is " + DoubleToStr( trailStart, MarketInfo(CurrentSymbol,MODE_DIGITS) ) );
         return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Red) );
      }
   
      profitPips = ( OrderOpenPrice()- trailStart ) - MarketInfo(CurrentSymbol,MODE_ASK) ;
      if( trailAmount != 0) { increments = MathFloor( profitPips / trailAmount ); }
      else { increments = 0; }
   
      if ( increments >= 1 && MarketInfo(CurrentSymbol,MODE_ASK) <= OrderOpenPrice() - trailStart - ( increments * trailAmount ) ) {
         sl = NormalizeDouble(OrderOpenPrice() - ( (increments-1) * trailAmount ), MarketInfo(CurrentSymbol,MODE_DIGITS));
      
         if( sl-tralingstep < NormalizeDouble(OrderStopLoss(), MarketInfo(CurrentSymbol,MODE_DIGITS)) ) {
            if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Red) ) {
               Print("Trailng stop updated. Total increments: " + DoubleToStr(increments, MarketInfo(CurrentSymbol,MODE_DIGITS)) );
               return(true);
            }
         }
      }
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
//+------------------------------------------------------------------+
int StringFindCount(string str, string str2)
//+------------------------------------------------------------------+
// Returns the number of occurrences of STR2 in STR
// Usage:   int x = StringFindCount("ABCDEFGHIJKABACABB","AB")   returns x = 3
{
  int c = 0;
  for (int i=0; i<StringLen(str); i++)
    if (StringSubstr(str,i,StringLen(str2)) == str2)  c++;
  return(c);
}
//+------------------------------------------------------------------+
int StrToStringArray(string str, string &a[], string delim=",", string init="")  {
//+------------------------------------------------------------------+
// Breaks down a single string into string array 'a' (elements delimited by 'delim')
  for (int i=0; i<ArraySize(a); i++)
    a[i] = init;
  if (str == "")  return(0);  
  int z1=-1, z2=0;
  if (StringRight(str,1) != delim)  str = str + delim;
  for (i=0; i<ArraySize(a); i++)  {
    z2 = StringFind(str,delim,z1+1);
    if (z2>z1+1)  a[i] = StringSubstr(str,z1+1,z2-z1-1);
    if (z2 >= StringLen(str)-1)   break;
    z1 = z2;
  }
  return(StringFindCount(str,delim));
}
//+------------------------------------------------------------------+
string StringTrim(string str)
//+------------------------------------------------------------------+
// Removes all spaces (leading, traing embedded) from a string
// Usage:    string x=StringUpper("The Quick Brown Fox")  returns x = "TheQuickBrownFox"
{
  string outstr = "";
  for(int i=0; i<StringLen(str); i++)  {
    if (StringSubstr(str,i,1) != " ")
      outstr = outstr + StringSubstr(str,i,1);
  }
  return(outstr);
}
//+------------------------------------------------------------------+
string StringRight(string str, int n=1)
//+------------------------------------------------------------------+
// Returns the rightmost N characters of STR, if N is positive
// Usage:    string x=StringRight("ABCDEFG",2)  returns x = "FG"
//
// Returns all but the leftmost N characters of STR, if N is negative
// Usage:    string x=StringRight("ABCDEFG",-2)  returns x = "CDEFG"
{
  if (n > 0)  return(StringSubstr(str,StringLen(str)-n,n));
  if (n < 0)  return(StringSubstr(str,-n,StringLen(str)-n));
  return("");
}