//+------------------------------------------------------------------+
//|                                                      Charles.mq4 |
//|                                       Copyright 2012, AlFa Corp. |
//|                                      alessio.fabiani @ gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AlFa"
#property link      "alessio.fabiani @ gmail.com"

#define VER "2.1.5"

#define DECIMAL_CONVERSION 10

extern string Comment_1           = " -- Comma separated list of Symbol Pairs: EURUSD,USDCHF";
extern string Comment_1a          = " --- must be equal to the string of your account provider.";
extern string Symbols             = "EURUSDm,EURGBPm,EURJPYm,CHFJPYm,GBPUSDm,GBPJPYm,GBPCHFm,GBPAUDm,USDCADm,USDCHFm,USDJPYm,AUDNZDm,AUDUSDm,NZDUSDm";
//extern string Symbols;
extern int    MagicNumber         = 3939;
extern double xFactor             = 1.8;
extern string TimeSet             = "07:32";

extern string Comment_2           = " -- Balance percentage order balance and Lot value";
extern double RiskPercent         = 5;

extern string Comment_3           = " -- Percentage of the risk for each Lot";
extern string Comment_3a          = " --- auto-adapted to number of pairs.";
extern double RiskPerTrade        = 10;
extern int    MaxAllowedOrders    = 15;
extern int    MaxOpenHours        = 0;
extern int    ProfitCheckPeriod   = 3;

extern string Comment_4           = " -- Whether or not use more strict supports and resistances";
extern bool   Aggressive          = false;
extern bool   UsePivots           = true;

extern string Comment_5           = " -- Whether or not leave open profitable orders";
extern int    FastPeriod          = 18;
extern int    SlowPeriod          = 60;
extern int    Selectivity         = 14;

extern string Comment_6           = " -- Fixed value if RiskPerTrade == 0";
extern double Lots                = 0.01;
extern int    Slippage            = 3;
extern string Comment_7           = " -- Set to true if broker is ECN/STP needing stops adding after order";
extern bool   ECN                 = true;
extern double MarginPercent       = 20;

extern string Comment_8           = " -- On all orders";
extern int    StopLoss            = 0;
extern int    TrailStart          = 30;
extern int    TrailingAmount      = 10;
extern int    TakeProfit          = 20;

extern string Comment_9           = " -- Whether or not manage ALL opened orders (manual too)";
extern bool   AllOrders           = true;

extern string Comment_10           = " -- Whether or not compute the Profit on ALL pairs";
extern bool   AllSymbols          = true;

extern string Comment_11          = " -- Log to console or to a file";
extern bool   LogToFile           = false;

extern string Comment_12          = " -- On init delete all pending orders";
extern bool   InitResetOrders     = true;

extern string Comment_13          = " -- Trading Time Management";
extern int    StartHour           = 0;
extern int    EndHour             = 24;
extern bool   CloseAllNow         = false;
extern int    FridayCloseTime     = 0;

 
int Anchor, PendingBuy, PendingSell, Buys, Sells, i, StopLevel, Spread;

int MaxOpenOrders;

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

double lastBarTime,lastpM5BarTime,lastpM15BarTime,lastpH1BarTime;

string SymbolsArray[0], CurrentSymbol;
bool canSell[0], canBuy[0], bullish[0], bearish[0];

bool Inited[0], DayStart[0], inited=false;

bool tradingAllowed=false;

double    Pivot[0];
double  Resist1[0];
double  Resist2[0];
double  Resist3[0];
double Support1[0];
double Support2[0];
double Support3[0];
double   pPoint[0];
bool     isCals[0];
double r1[0] , s1[0] ,s2[0], r2[0], r3[0], s3[0];

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

   MaxOpenOrders        = MaxAllowedOrders/size;
      
   ArrayResize(SymbolsArray,size);
      if(StringLen(Symbols)>0){StrToStringArray(Symbols,SymbolsArray,delim);}
   ArrayResize(canSell,size);
   ArrayResize(canBuy,size);
   ArrayResize(Inited,size);
   ArrayResize(DayStart,size);

   ArrayResize(Pivot,size);
   ArrayResize(Resist1,size);
   ArrayResize(Resist2,size);
   ArrayResize(Support1,size);
   ArrayResize(Support2,size);
   ArrayResize(Support3,size);
   ArrayResize(Resist3,size);
   ArrayResize(pPoint,size);
   ArrayResize(isCals,size);
   ArrayResize(r1,size);
   ArrayResize(s1,size);
   ArrayResize(s2,size);
   ArrayResize(r2,size);
   ArrayResize(r3,size);
   ArrayResize(s3,size);
   ArrayResize(bullish,size);
   ArrayResize(bearish,size);

   for(i=0; i<size; i++)
   {
      canSell[i] = true;
      canBuy[i] = true;
      Inited[i] = false;
      DayStart[i] = false;
      isCals[i] = false;
      bullish[i] = false;
      bearish[i] = false;
   }

   lastBarTime = TimeCurrent();
   lastpM5BarTime = TimeCurrent();
   lastpM15BarTime = TimeCurrent();
   lastpH1BarTime = TimeCurrent();
}

//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
{
   bool pM5=false,pM15=false,pH1=false;
   if((TimeCurrent()-lastpM5BarTime)/60>=PERIOD_M5)
   {
      pM5=true;
      lastpM5BarTime=TimeCurrent();
   }
   if((TimeCurrent()-lastpM15BarTime)/60>=PERIOD_M15)
   {
      pM15=true;
      lastpM15BarTime=TimeCurrent();
   }
   if((TimeCurrent()-lastpH1BarTime)/60>=PERIOD_H1)
   {
      pH1=true;
      lastpH1BarTime=TimeCurrent();
   }
   
   int SystemPeriod=1;
   
   tradingAllowed=false;
   if((StartHour < EndHour && TimeHour(TimeCurrent()) >= StartHour && TimeHour(TimeCurrent()) < EndHour) || (StartHour > EndHour && TimeHour(TimeCurrent()) >= StartHour ||
         TimeHour(TimeCurrent()) < EndHour)) 
   {
         if(DayOfWeek() != 5 || FridayCloseTime==0 || Hour()<FridayCloseTime )
         {
            tradingAllowed=true;
         }
   }

   if( tradingAllowed==false )
   {
      DeleteAllPendingOrders();
   }
   
   if( CloseAllNow==true || (DayOfWeek() == 5 && FridayCloseTime>0 && Hour()>=FridayCloseTime) )
   {
      ForceCloseAllOpenOrders();
      DeleteAllPendingOrders();
   }

   if( inited==false || (TimeCurrent() - lastBarTime)/60 >= SystemPeriod )
   {
      if(inited==false) inited=true;
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
         
         if( stopLoss>0 && stopLoss<MarketInfo(CurrentSymbol,MODE_STOPLEVEL)* MarketInfo(CurrentSymbol, MODE_POINT) )
         {
            stopLoss = MarketInfo(CurrentSymbol,MODE_STOPLEVEL)* MarketInfo(CurrentSymbol, MODE_POINT);
         }
         
         // COMPUTING LOT-SIZE
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

         //SYSTEM CORE
         RefreshRates();
         Count();

         /*if(Buys!=0 || Sells!=0)
         {*/
            CheckForClose(s);
         /*}*/

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

         double EMAF1 = iMA(CurrentSymbol,PERIOD_M15,FastPeriod,0,MODE_EMA,PRICE_WEIGHTED,0); //fast
         double EMAS1 = iMA(CurrentSymbol,PERIOD_M15,SlowPeriod,0,MODE_EMA,PRICE_WEIGHTED,0); //slow

         double RSI = iRSI(CurrentSymbol,PERIOD_M5,Selectivity,PRICE_WEIGHTED,0);
         double RSI2 = iRSI(CurrentSymbol,PERIOD_M5,Selectivity,PRICE_WEIGHTED,7);
         double RSIM1 = iRSI(CurrentSymbol,PERIOD_M15,Selectivity,PRICE_WEIGHTED,0);
         double RSIM2 = iRSI(CurrentSymbol,PERIOD_M15 ,Selectivity,PRICE_WEIGHTED,7);

         double RSIL1 = iRSI(CurrentSymbol,PERIOD_M30 ,Selectivity,PRICE_WEIGHTED,0);
         double RSIL2 = iRSI(CurrentSymbol,PERIOD_M30 ,Selectivity,PRICE_WEIGHTED,7);
         //RSITrend =  iRSI(NULL,PERIOD_H4,Selectivity,PRICE_WEIGHTED,0);

         Result=0;Error=0;

         RefreshRates();         
         Count();

         if(pM5==true)
         {
            // VOLATILITY;
            double SignalBufferSellStop   = 0;
            double SignalBufferBuyStop    = 0;
            double SignalBufferSellLimit   = 0;
            double SignalBufferBuyLimit    = 0;
         
            int mul = MathPow(10,MarketInfo(CurrentSymbol,MODE_DIGITS)-1);
            double Volatility1=(iMA(CurrentSymbol,PERIOD_M15,1,0,MODE_SMA,PRICE_HIGH,1)-iMA(CurrentSymbol,PERIOD_M15,1,0,MODE_SMA,PRICE_LOW,1))*mul;
            double Volatility2=(iMA(CurrentSymbol,PERIOD_M15,1,0,MODE_SMA,PRICE_HIGH,2)-iMA(CurrentSymbol,PERIOD_M15,1,0,MODE_SMA,PRICE_LOW,2))*mul;

            if(Volatility1-Volatility2>=20)
            {
               //Alert(TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS) + " -- " + Volatility1 + " / " + Volatility2);
               if( iClose(CurrentSymbol,PERIOD_M15,1)>iOpen(CurrentSymbol,PERIOD_M15,1) )
               {
                  //SignalBufferSellLimit=iHigh(CurrentSymbol,PERIOD_M5,iHighest(CurrentSymbol,PERIOD_M5,MODE_HIGH,3,0));
                  SignalBufferSellStop=NormalizeDouble( iLow(CurrentSymbol,PERIOD_M15,1)-delta+((iHigh(CurrentSymbol,PERIOD_M15,1)-iLow(CurrentSymbol,PERIOD_M15,1))/2), MarketInfo(CurrentSymbol,MODE_DIGITS));
               }
               else
               {
                  //SignalBufferBuyLimit=iLow(CurrentSymbol,PERIOD_M5,iLowest(CurrentSymbol,PERIOD_M5,MODE_LOW,3,0));
                  SignalBufferBuyStop=NormalizeDouble( iLow(CurrentSymbol,PERIOD_M15,1)+delta-((iLow(CurrentSymbol,PERIOD_M15,1)-iHigh(CurrentSymbol,PERIOD_M15,1))/2), MarketInfo(CurrentSymbol,MODE_DIGITS));
               }
            }
            
            if(SignalBufferSellStop>0 && (Sells+Buys)<=MaxOpenOrders)
            {
               OpenOrder(CurrentSymbol,LotsValue,OP_SELLSTOP,SignalBufferSellLimit);
            }
            if(SignalBufferBuyStop>0 && (Sells+Buys)<=MaxOpenOrders)
            {
               OpenOrder(CurrentSymbol,LotsValue,OP_BUYSTOP,SignalBufferBuyLimit);
            }
            if(SignalBufferSellLimit>0 && (Sells+Buys)<=MaxOpenOrders)
            {
               OpenOrder(CurrentSymbol,LotsValue,OP_SELLLIMIT,SignalBufferSellLimit);
            }
            if(SignalBufferBuyLimit>0 && (Sells+Buys)<=MaxOpenOrders)
            {
               OpenOrder(CurrentSymbol,LotsValue,OP_BUYLIMIT,SignalBufferBuyLimit);
            }
         }

         if(pM15==true)
         {
            // CHECK TREND CONDITION
            bullish[s]=((RSI>55&&RSIM1>55&&RSIL1>55)&&(RSIL2<50&&RSIM2<50&&RSI2<50)&&EMAS1>EMAF1);
            bearish[s]=((RSI<55&&RSIM1<55&&RSIL1<55)&&(RSIL2>50&&RSIM2>50&&RSI2>50)&&EMAS1<EMAF1);

            RefreshRates();
            Count();
   
            if(bearish[s]==true && bullish[s]==false)
            {
               if(MathAbs(Sells-Buys)<=MaxOpenOrders)OpenOrder(CurrentSymbol,LotsHedgeValue,OP_SELL);
               //canSell[s]=true;
               canBuy[s]=false;
            }
            if(bullish[s]==true && bearish[s]==false)
            {
               if(MathAbs(Sells-Buys)<=MaxOpenOrders)OpenOrder(CurrentSymbol,LotsHedgeValue,OP_BUY);
               canSell[s]=false;
               //canBuy[s]=true;
            }
         }
         
         if(pH1==true)
         {
            // COMPUTING PIVOTS AND LEVELS
            isCalsPivot(CurrentSymbol, s);
         }

         RefreshRates();         
         Count();

         //LIMITS
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
          
             if(TimeStr(CurTime())==TimeSet && DayStart[s]==false)
             {
               Log("[DAY RESET: "+TimeToStr(TimeCurrent())+"]");
               DeleteAllPendingOrders();
               Dw=MinOpenPrice;
               Up=MaxOpenPrice;
               DayStart[s]=true;
             }
             else if(TimeStr(CurTime())!=TimeSet && DayStart[s]==true)
             {
               DayStart[s]=false;
             }
    
             SL=0;
             TP=0;

             if(PendingSell == 0)
             {
               if(canSell[s])
               {
                  if(ECN == false && stopLoss > 0){SL=Dw+stopLoss;}
                  
                  if(tradingAllowed==true && AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountEquity()*MarginPercent/100) && GetLastError()!=134)
                  {
                     //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                     while (!IsTradeAllowed()) Sleep(100);
                     RefreshRates();
            
                     Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);
                     //-----------------------
                       if(Result<0){Error=GetLastError();Log("LastError [CORE;OP_SELLSTOP]= "+Error+" - Dw# "+Dw+" - Ask#"+MarketInfo(CurrentSymbol,MODE_ASK));}
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

                  if(tradingAllowed==true && AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountEquity()*MarginPercent/100) && GetLastError()!=134)
                  {
                     //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                     while (!IsTradeAllowed()) Sleep(100);
                     RefreshRates();
            
                     Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Blue);
                     //-----------------------
                       if(Result<0){Error=GetLastError();Log("LastError [CORE;OP_BUYSTOP] = "+Error+" - Dw# "+Up+" - Bid#"+MarketInfo(CurrentSymbol,MODE_BID));}
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
                     if(canSell[s])
                     {
                        if(ECN == false && stopLoss > 0){SL=Dw+stopLoss;}
                     
                        if(tradingAllowed==true && AccountFreeMarginCheck(CurrentSymbol,OP_SELL,LotsHedgeValue)>=(AccountEquity()*MarginPercent/100) && GetLastError()!=134)
                        {
                           //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                           while (!IsTradeAllowed()) Sleep(100);
                           RefreshRates();
            
                           Result=OrderSend(CurrentSymbol,OP_SELLSTOP,LotsHedgeValue,Dw,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);          
                           //-----------------------
                             if(Result<0){Error=GetLastError();Log("LastError [CORE - HEDGE;OP_SELLSTOP]= "+Error);}
                             else if(Result!=0)
                             {
                              Error=0;
                             }
                           //-----------------------
                        }
                     }
                  }
         
                  if(PendingSell > 0 && PendingBuy == 0 && Error==0)
                  {
                     if(canBuy[s])
                     {
                        if(ECN == false && stopLoss > 0){SL=Up-stopLoss;}

                        if(tradingAllowed==true && AccountFreeMarginCheck(CurrentSymbol,OP_BUY,LotsHedgeValue)>=(AccountEquity()*MarginPercent/100) && GetLastError()!=134)
                        {
                           //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                           while (!IsTradeAllowed()) Sleep(100);
                           RefreshRates();
            
                           Result=OrderSend(CurrentSymbol,OP_BUYSTOP,LotsHedgeValue,Up,Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Blue);
                           //-----------------------
                             if(Result<0){Error=GetLastError();Log("LastError [CORE - HEDGE;OP_BUYSTOP] = "+Error);}
                             else if(Result!=0)
                             {
                              Error=0;
                             }
                           //-----------------------
                        }
                     }
                  }      
             }
         }
      }

	  pM5=false;
	  pM15=false;
	  pH1=false;
	  lastBarTime = TimeCurrent();

	  CharlesStatus();

	}
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
int CheckForClose(int s)
{
   RefreshRates();
   Count();

   int ordersTotal=OrdersTotal();
   bool skipBasket=false;
   
   if(ordersTotal>0)
   {
      if(Profit>=0)
      {
         skipBasket=true;
      }
      
      if(skipBasket==false)
      {
         double ordersWinningBasket[],ordersWinningBasketTkts[],ordersLosingBasket[],ordersLosingBasketTkts[],ordersBasketHedgeTkts[];
         ArrayResize(ordersWinningBasket,ordersTotal);
         ArrayResize(ordersWinningBasketTkts,ordersTotal);
         ArrayResize(ordersLosingBasket,ordersTotal);
         ArrayResize(ordersLosingBasketTkts,ordersTotal);
         ArrayResize(ordersBasketHedgeTkts,ordersTotal);

         int winners=0,loosers=0;
               
         for(i=0; i<ordersTotal; i++)
         {
            ordersWinningBasket[i]=0;
            ordersWinningBasketTkts[i]=0;
            ordersLosingBasket[i]=0;
            ordersLosingBasketTkts[i]=0;
            ordersBasketHedgeTkts[i]=0;
         
             if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
             {

               if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
               {
                  continue;
               }//if

               if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
               {
                  continue;
               }//if

               if(OrderType() == OP_BUY || OrderType() == OP_SELL)
               {
                  if(OrderProfit()+OrderSwap()+OrderCommission()>0)
                  {
                     ordersWinningBasket[winners]     =  OrderProfit()+OrderSwap()+OrderCommission();
                     ordersWinningBasketTkts[winners] =  OrderTicket();
                     winners++;
                  } 
                  else if(OrderProfit()+OrderSwap()+OrderCommission()<0)
                  {
                     ordersLosingBasket[loosers]     =  OrderProfit()+OrderSwap()+OrderCommission();
                     ordersLosingBasketTkts[loosers] =  OrderTicket();
                     loosers++;
                  }
               }
            }
         }//for
         
         Log( "[Basket] - Winners#"+winners+"; Loosers#"+loosers );
         
         for(int w=0;w<winners;w++)
         {
            if( OrderSelect(ordersWinningBasketTkts[w],SELECT_BY_TICKET,MODE_TRADES) )
            {
               Log( "[Basket] - Checking Order#"+OrderTicket()+"; ["+OrderSymbol()+"]#"+(OrderProfit()+OrderCommission()+OrderSwap()) );
               for(int l=0;l<loosers;l++)
               {
                  if( OrderSelect(ordersLosingBasketTkts[l],SELECT_BY_TICKET,MODE_TRADES) && MathAbs(ordersLosingBasket[l])<ordersWinningBasket[w] )
                  {
                     //Log( "[Basket - STEP 1.0] - Loosing Order#"+OrderTicket()+"; ["+OrderSymbol()+"]#"+(OrderProfit()+OrderCommission()+OrderSwap()) );
                     int sum=loosers;
                     while(sum>0)
                     {
                        for(int ls=0;ls<sum;ls++)
                        {
                           ordersBasketHedgeTkts[0]=ordersLosingBasketTkts[l];
                           double hedgeProfit=ordersLosingBasket[l];
                           int hedgePos=1;
                           for(int lsp=0;lsp<sum;lsp++)
                           {
                              if( ordersLosingBasketTkts[lsp] != ordersLosingBasketTkts[l] && MathAbs(ordersLosingBasket[lsp])<ordersWinningBasket[w] )
                              {
                                 ordersBasketHedgeTkts[hedgePos]=ordersLosingBasketTkts[lsp];
                                 hedgeProfit+=ordersLosingBasket[lsp];
                                 hedgePos++;
                              }//if
                           }//for
                        }//for
                        
                        if(hedgePos>1)
                           Log( "[Basket - STEP 1."+ls+"] - hedgePos#"+hedgePos+"; hedgeProfit#"+MathAbs(hedgeProfit*xFactor)+"; ordersWinningBasket#"+ordersWinningBasket[w] );
                        
                        if(hedgePos>1 && (MathAbs(hedgeProfit*xFactor)<ordersWinningBasket[w]) )
                        {
                           Log("[BASKET] hedgePos#"+hedgePos+"; hedgeProfit#"+MathAbs(ordersLosingBasket[l])+"; ordersWinningBasket#"+ordersWinningBasket[w]);
                           for(int oh=0; oh<hedgePos; oh++)
                           {
                              if(OrderSelect(ordersBasketHedgeTkts[oh],SELECT_BY_TICKET,MODE_TRADES))
                              {
                                 //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                                 while (!IsTradeAllowed()) Sleep(100);
                                 RefreshRates();

                                 Log("[BASKET] - Closing Order#"+OrderTicket()+"; ["+OrderSymbol()+"]");

                                 if(OrderType()==OP_BUY)
                                 {
                                    OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), Slippage, Blue);
                                 }
                                 else if(OrderType()==OP_SELL)
                                 {
                                    OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), Slippage, Red);
                                 }
                              }
                           }

                           if(OrderSelect(ordersWinningBasketTkts[w],SELECT_BY_TICKET,MODE_TRADES))
                           {
                              //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
                              while (!IsTradeAllowed()) Sleep(100);
                              RefreshRates();

                              Log("[BASKET] - Closing Order#"+OrderTicket()+"; ["+OrderSymbol()+"]");
               
                              if(OrderType()==OP_BUY)
                              {
                                 OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), Slippage, Blue);
                              }
                              else if(OrderType()==OP_SELL)
                              {
                                 OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), Slippage, Red);
                              }
                           }
                           sum=0;
                           loosers=0;
                           winners=0;
                           break;
                        }
                        
                        sum--;
                     }//while
                  }               
               }
            }
         }
         
      }
   }

   RefreshRates();      
   Count();

   int sx;   
   if(Profit>=0 && Profit>=Amount)
   {
      ordersTotal=OrdersTotal();
      
      double gainingProfit=0,loosingProfit=0;
      if(ordersTotal>0)
      {
         for(i=ordersTotal-1; i>=0; i--)
         {
            if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
            {
               if( (OrderProfit()+OrderSwap()+OrderCommission())>0 )
               {
                  gainingProfit+=(OrderProfit()+OrderSwap()+OrderCommission());
               }
               else
               {
                  loosingProfit+=(OrderProfit()+OrderSwap()+OrderCommission());
               }
            }
         }
      }

      if(loosingProfit==0 || gainingProfit>(MathAbs(loosingProfit)) )
      {
         isInGain=true;
      }
      else
      {
         isInGain=false;
      }

      RefreshRates();      
      Count();
      
      if( isInGain==false && Profit<=(LastProfit - Amount/10) )
      {
         isInGain = false;
         blockConditionHit=false;
         unlockOrdersPlaced=false;
         LastOrderTicket = -1;
         UnlockingOrderTicket = -1;
         LastProfit=0;
         
         for(sx=0; sx<ArraySize(canSell); sx++)
         {
            canSell[sx] = true;
            canBuy[sx] = true;
            CloseAll(sx, isInGain);
         }
      }
      
      if(isInGain==true )
      {
         ordersTotal=OrdersTotal();
   
         if(ordersTotal>0)
         {
            for(i=ordersTotal-1; i>=0; i--)
            {
               double highest=0;
               double lowest=0;
               double chgFromOpen=0,chgFromPeak=0,chgFromLown=0;
               double ceil=0,floor=0;
               if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==true)
               {

                  if (MarketInfo(OrderSymbol(), MODE_DIGITS) == 3 || MarketInfo(OrderSymbol(), MODE_DIGITS) == 5)   {
                     floor    = NormalizeDouble(Delta * DECIMAL_CONVERSION * MarketInfo(OrderSymbol(), MODE_POINT),MarketInfo(OrderSymbol(), MODE_DIGITS));
                     ceil     = NormalizeDouble(TakeProfit * DECIMAL_CONVERSION * MarketInfo(OrderSymbol(), MODE_POINT),MarketInfo(OrderSymbol(), MODE_DIGITS));
                  }
                  else
                  {
                     floor    = NormalizeDouble(Delta * MarketInfo(OrderSymbol(), MODE_POINT),MarketInfo(OrderSymbol(), MODE_DIGITS));
                     ceil     = NormalizeDouble(TakeProfit * MarketInfo(OrderSymbol(), MODE_POINT),MarketInfo(OrderSymbol(), MODE_DIGITS));
                  }

                  highest = iHigh(OrderSymbol(),PERIOD_M15,iHighest(CurrentSymbol,PERIOD_M15,MODE_HIGH,ProfitCheckPeriod,1));
                  lowest  = iLow(OrderSymbol(),PERIOD_M15,iLowest(CurrentSymbol,PERIOD_M15,MODE_LOW,ProfitCheckPeriod,1));

                  if( OrderType() == OP_BUY  )
                  {
                     chgFromOpen = NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - OrderOpenPrice(), MarketInfo(OrderSymbol(),MODE_DIGITS));
                     chgFromPeak = NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - highest, MarketInfo(OrderSymbol(),MODE_DIGITS));
                     chgFromLown = NormalizeDouble(lowest - MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS));
                     
                     if( (TimeCurrent()-OrderOpenTime())/60>=ProfitCheckPeriod*PERIOD_M15 && 
                           OrderProfit()+OrderCommission()+OrderSwap()>0 &&
                            chgFromOpen>0 && 
                              MathAbs(chgFromOpen-chgFromPeak)>=floor && MathAbs(chgFromOpen-chgFromPeak)<=ceil &&
                               chgFromLown<0 && MathAbs(chgFromLown)>=floor)
                     {
                        for(sx=0; sx<ArraySize(canSell); sx++)
                        {
                           canSell[sx] = true;
                           canBuy[sx] = true;
                           CloseAll(sx);
                        }
                        break;
                     }
                  }
                  else if( OrderType() == OP_SELL  )
                  {
                     chgFromOpen = NormalizeDouble(OrderOpenPrice() - MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS));
                     chgFromPeak = NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - highest, MarketInfo(OrderSymbol(),MODE_DIGITS));
                     chgFromLown = NormalizeDouble(lowest - MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS));
                     
                     if( (TimeCurrent()-OrderOpenTime())/60>=ProfitCheckPeriod*PERIOD_M15 && 
                           OrderProfit()+OrderCommission()+OrderSwap()>0 &&
                           chgFromOpen>0 && 
                              MathAbs(chgFromOpen-chgFromLown)>=floor && MathAbs(chgFromOpen-chgFromLown)<=ceil && 
                               chgFromPeak<0 && MathAbs(chgFromPeak)>=floor)
                     {
                        for(sx=0; sx<ArraySize(canSell); sx++)
                        {
                           canSell[sx] = true;
                           canBuy[sx] = true;
                           CloseAll(sx);
                        }
                        break;
                     }
                  }
               }
            }
         }
      }

      LastProfit = Profit;
   }
   else
   {
      isInGain=false;
   }
   
   RefreshRates();
   ordersTotal=OrdersTotal();

   if(ordersTotal>0)
   {
      for(i=ordersTotal-1; i>=0; i--)
      {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         {

            if(AllSymbols==false && OrderSymbol()!=CurrentSymbol)
            {
               continue;
            }//if 

            if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
            {
               continue;
            }//if

            CheckTrailingStop(OrderTicket(),s,isInGain);
         }
      }
   }
   
   RefreshRates();
   Count();
   
   if(Profit==0 && Buys==0 && Sells==0 && PendingBuy==0 && PendingSell==0)
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
         CloseAll(sx, isInGain);
      }
   }

   RefreshRates();
   Count();

   //CONDIZIONE DI BLOCCAGGIO E SBLOCCAGGIO DI EMERGENZA
   LastOrderTicket=-1;
   if(Profit<-AccountBalance()*RiskPercent/100)
   {
      if(blockConditionHit==false)
      {
         Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - First Hit]: DeleteAllPendingOrders; AccountFreeMargin: " + AccountFreeMargin());
         DeleteAllPendingOrders();
      }
      blockConditionHit=true;

      bool blockedSellLots=0,blockedBuyLots=0;
      string blockedOrderSymbol;
      for(i=OrdersTotal()-1; i>=0; i--)
      {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         {
            if(OrderSymbol()!=CurrentSymbol)
            {
               continue;
            }//if

            if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
            {
               continue;
            }//if

            if(OrderProfit()+OrderCommission()+OrderSwap()<0)
            {
               blockedOrderSymbol = OrderSymbol();
               if(OrderType()==OP_BUY)
               {
                  blockedBuyLots+=OrderLots();
               }
               else if(OrderType()==OP_SELL)
               {
                  blockedSellLots+=OrderLots();
               }
            }

            int ticket=-1;
            if( (blockedSellLots>0 || blockedBuyLots>0) && (blockedSellLots != blockedBuyLots) )
            {
               if(blockedSellLots > blockedBuyLots)
               {
                  ticket=OpenOrder(blockedOrderSymbol,blockedSellLots-blockedBuyLots, OP_BUY);
                  if(ticket>0)
                  {
                     LastOrderTicket=ticket;
                     Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
                  }
               }//if
               else if(blockedBuyLots > blockedSellLots)
               {
                  ticket=OpenOrder(blockedOrderSymbol,blockedBuyLots-blockedSellLots, OP_SELL);
                  if(ticket>0)
                  {
                     LastOrderTicket=ticket;
                     Log("[CONDIZIONE DI BLOCCAGGIO DI EMERGENZA - Blocking Loss]: SellLots>BuyLots; Opened BUY Order Ticket: " + LastOrderTicket);
                  }
               }//if
            }//if

         }//if
      }//for
   }
   else
   {
      blockConditionHit=false;
   }

   if(blockConditionHit==true)
   {
      for(i=OrdersTotal()-1; i>=0; i--)
      {
         double OpenedLoosingSellProfit=0,OpenedLoosingBuyProfit=0;
         double OpenedLoosingSellLots=0,OpenedLoosingBuyLots=0;

         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
         {

            if(OrderSymbol()!=CurrentSymbol)
            {
               continue;
            }//if

            if(AllOrders==false && OrderMagicNumber()!=MagicNumber)
            {
               continue;
            }//if

            if((OrderProfit() + OrderCommission() + OrderSwap())<0)
            {
               if(OrderType() == OP_BUY)
               {
                  OpenedLoosingBuyLots+=OrderLots();
                  OpenedLoosingBuyProfit+=(OrderProfit() + OrderCommission() + OrderSwap());
                  canBuy[s]=false;
               }
               if(OrderType() == OP_SELL)
               {
                  OpenedLoosingSellLots+=OrderLots();
                  OpenedLoosingSellProfit+=(OrderProfit() + OrderCommission() + OrderSwap());
                  canSell[s]=false;
               }
            }
            else
            {
               if(OrderType() == OP_BUY)
               {
                  canBuy[s]=true;
               }
               if(OrderType() == OP_SELL)
               {
                  canSell[s]=true;
               }
            }
         }//if

         //Alert(OpenedLoosingBuyLots+" - "+OpenedLoosingSellLots);
      
         if( OpenedLoosingBuyLots>0 && OpenedLoosingSellLots>0 && OpenedLoosingBuyLots == OpenedLoosingSellLots)
         {
            double balanceLots=normPrice(OpenedLoosingBuyLots);
            if(balanceLots<normPrice(MarketInfo(CurrentSymbol,MODE_MINLOT)))
            {
               balanceLots=normPrice(MarketInfo(CurrentSymbol,MODE_MINLOT));
            }

            if(OpenedLoosingBuyProfit>OpenedLoosingSellProfit)
            {
               OpenOrder(CurrentSymbol,balanceLots, OP_BUY);
            }
            if(OpenedLoosingSellProfit>OpenedLoosingBuyProfit)
            {
               OpenOrder(CurrentSymbol,balanceLots, OP_SELL);
            }
         }

      }//for
      
   }//if
   
   CharlesStatus();                  
}  

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CharlesStatus()
{

   if(Buys == 0 && Sells == 0 && PendingBuy==0 && PendingSell==0)
   {
      for(int sx=0; sx<ArraySize(canSell); sx++)
      {
         canSell[sx] = true;
         canBuy[sx] = true;
      }
   }

   RefreshRates();
   Count();
   
   double LookingFor = Amount;
   
   if(Profit>0 && Profit>=Amount/10.0) {LookingFor=(LastProfit - Amount/10.0);}
   
   string blocked;
   if(blockConditionHit)
   {
      blocked = "\nBlock Condition HIT";
   }
   
   Comment("Charles ",VER," - Gain= ",Profit," LookingFor= ",LookingFor," RiskPercent= ",(-AccountBalance()*RiskPercent/100),
          ";\nBalance=",AccountBalance(),"; FreeMargin=", AccountFreeMargin(),"; Equity=", AccountEquity(),
          ";\nBuy=",Buys,"; Sell=", Sells,"; BuyLots=",BuyLots,"; SellLots=",SellLots,
          ";\nPendingBuys=",PendingBuy,"; PendingSells=",PendingSell,"; PendingBuyLots=",PendingBuyLots,"; PendingSellLots=",PendingSellLots,blocked);

}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
double LOT()
{
   RefreshRates();

   double money=AccountFreeMargin();
   int leverage=AccountLeverage();
   double tickvalue=MarketInfo(CurrentSymbol,MODE_TICKVALUE);
   double MINLOT = MarketInfo(CurrentSymbol,MODE_MINLOT);
   double LOTDIGITS = MarketInfo(CurrentSymbol,MODE_DIGITS);
   double LOT;
   int riskPip;
     
   if (StopLoss>0)
   {
      riskPip = StopLoss;
   }
   else
   {
      riskPip = RiskPercent*10;
   }

   if(RiskPercent==0 || RiskPerTrade==0)
   {
      LOT = AccountFreeMargin()*AccountLeverage()/1000/MarketInfo(CurrentSymbol,MODE_MARGINREQUIRED)/15;
   }
   else
   {
      double ordRisk = RiskPerTrade*0.01;
      double ordLeverage = leverage*0.1;
      LOT = NormalizeDouble((money*ordRisk/riskPip)/(leverage/tickvalue),LOTDIGITS);
      //LOT = (( ( AccountEquity()/100 )*( RiskPerTrade/ArraySize(SymbolsArray) ))/RiskFactor )/NormalizeDouble(MarketInfo(CurrentSymbol,MODE_TICKVALUE),2);
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
  for(i=OrdersTotal()-1; i>=0; i--)
  {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      {
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
      }//if
   }//for
}

//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
//---------------------------------------------------------------------------         
void CloseAll(int sx, bool isGaining=false)
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
                  if(isGaining==true)
                  {
                     if( /*(bullish[sx]==true && OrderSymbol()==SymbolsArray[sx]) ||*/ (OrderStopLoss()>0 && OrderStopLoss()>OrderOpenPrice()) )
                        continue;
                  }
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), Slippage, Blue);
                  BuysProfit += OrderProfit()+OrderCommission()+OrderSwap();
                  continue;
                  
               case OP_SELL:
                  if(isGaining==true)
                  {
                     if( /*(bearish[sx]==true && OrderSymbol()==SymbolsArray[sx]) ||*/ (OrderStopLoss()>0 && OrderStopLoss()<OrderOpenPrice()) )
                        continue;
                  }
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), Slippage, Red);
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
   
   if(Total>0 && isInGain==true)
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
void ForceCloseAllOpenOrders()
{
   RefreshRates();

   int    Pos;
   int    Total=OrdersTotal();
   
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
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), Slippage, Blue);
                  continue;
                  
               case OP_SELL:
                  Result=OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), Slippage, Red);
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
}
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
int OpenOrder(string symbol, double LotOpenValue, int Type, double Price=0)
{
   RefreshRates();

   //Log("OpenOrder - LotHedgeValue:" + LotOpenValue + "; AccountFreeMargin: "+AccountFreeMargin());

   if(tradingAllowed==true && AccountFreeMargin()>0)
   {
      if(Type==OP_BUY)
      {
         SL = 0;
         TP = 0;
         
         if(ECN == false && stopLoss > 0){SL=MarketInfo(symbol,MODE_ASK)+stopLoss;}

         if(AccountFreeMarginCheck(symbol,OP_BUY,LotOpenValue)<=(AccountEquity()*MarginPercent/100) || GetLastError()==134)
         {
            return(-1);
         }
         
         //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
         while (!IsTradeAllowed()) Sleep(100);
         RefreshRates();
            
         Result=OrderSend(symbol,OP_BUY,LotOpenValue,MarketInfo(symbol,MODE_ASK),Slippage,SL,TP,"Charels_"+VER,MagicNumber,0,Blue);
         Log("OpenOrder[OP_BUY] - Ask:" + MarketInfo(symbol,MODE_ASK) + "; Result: "+Result);
      }
      if(Type==OP_SELL)
      {
         SL = 0;
         TP = 0;
         
         if(ECN == false && stopLoss > 0){SL=MarketInfo(symbol,MODE_BID)-stopLoss;}

         if(AccountFreeMarginCheck(symbol,OP_SELL,LotOpenValue)<=(AccountEquity()*MarginPercent/100) || GetLastError()==134)
         {
            return(-1);
         }

         //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
         while (!IsTradeAllowed()) Sleep(100);
         RefreshRates();
            
         Result=OrderSend(symbol,OP_SELL,LotOpenValue,MarketInfo(symbol,MODE_BID),Slippage,SL,TP,"Charles_"+VER,MagicNumber,0,Red);
         Log("OpenOrder[OP_SELL] - Bid:" + MarketInfo(symbol,MODE_BID) + "; Result: "+Result);
      }
      
      if( (Type==OP_SELLLIMIT||Type==OP_BUYSTOP) && Price!=0 && Price>MarketInfo(symbol,MODE_ASK))
      {
         SL = 0;
         TP = 0;
         
         if(ECN == false && stopLoss > 0){SL=Price-stopLoss;}

         if(AccountFreeMarginCheck(symbol,OP_SELL,LotOpenValue)<=(AccountEquity()*MarginPercent/100) || GetLastError()==134)
         {
            return(-1);
         }
         
         while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();
            
         Result=OrderSend(symbol,Type,LotOpenValue,Price+MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT),Slippage,SL,TP,"Charels_"+VER,MagicNumber,0,Blue);
         Log("OpenOrder["+Type+"] - Ask: " + (Price+MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT)) + "; Result: "+Result);
      }
      if( (Type==OP_BUYLIMIT||Type==OP_SELLSTOP) && Price!=0 && Price<MarketInfo(symbol,MODE_BID))
      {
         SL = 0;
         TP = 0;
         
         if(ECN == false && stopLoss > 0){SL=Price+stopLoss;}

         if(AccountFreeMarginCheck(symbol,OP_BUY,LotOpenValue)<=(AccountEquity()*MarginPercent/100) || GetLastError()==134)
         {
            return(-1);
         }
         
         while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();
                     
         Result=OrderSend(symbol,Type,LotOpenValue,Price-MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT),Slippage,SL,TP,"Charels_"+VER,MagicNumber,0,Blue);
         Log("OpenOrder["+Type+"] - Bid: " + (Price-MarketInfo(symbol,MODE_SPREAD)*MarketInfo(symbol,MODE_POINT)) + "; Result: "+Result);
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
void CheckTrailingStop(int ticket,int index,bool isGaining=false)
{
   RefreshRates();
   
   if(OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES))
   {

      if(OrderStopLoss()==0 && stopLoss>0)
      {
         switch(OrderType())
         {
            case OP_BUY:
               //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
               while (!IsTradeAllowed()) Sleep(100);
               RefreshRates();
            
               if(OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice()-stopLoss, OrderTakeProfit(), 0, Blue)==false)
               {
                  Error=GetLastError();
                  Log("LastError [CheckTrailingStop;OP_BUY::UpdateStopLoss]= "+Error+" - OrderOpenPrice# "+OrderOpenPrice()+" - StopLoss# "+stopLoss+" - New Stop# "+(OrderOpenPrice()-stopLoss));
               }
               else
               {
                  Log("[ECN Order BUY#"+OrderTicket()+"] StopLoss# "+stopLoss+" updated to # "+(OrderOpenPrice()-stopLoss) );
                  break;
               }
            case OP_SELL:
               //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
               while (!IsTradeAllowed()) Sleep(100);
               RefreshRates();
            
               if(OrderModify(OrderTicket(), OrderOpenPrice(), OrderOpenPrice()+stopLoss, OrderTakeProfit(), 0, Red)==false)
               {
                  Error=GetLastError();
                  Log("LastError [CheckTrailingStop;OP_SELL::UpdateStopLoss]= "+Error+" - OrderOpenPrice# "+OrderOpenPrice()+" - StopLoss# "+stopLoss+" - New Stop# "+(OrderOpenPrice()+stopLoss));
               }
               else
               {
                  Log("[ECN Order SELL#"+OrderTicket()+"] StopLoss# "+stopLoss+" updated to # "+(OrderOpenPrice()+stopLoss) );
                  break;
               }
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
      double tralingstep = 0;//0*MarketInfo(OrderSymbol(),MODE_POINT);

      if( OrderType() == OP_BUY ) {
         if( MaxOpenHours>0 && (TimeCurrent()-OrderOpenTime())/60 >= MaxOpenHours*PERIOD_H1 
            && OrderProfit()+OrderSwap()+OrderCommission()<0)
         {
            //if(MathAbs(Sells-Buys)<=MaxOpenOrders)OpenOrder(OrderSymbol(),normPrice(OrderLots()*xFactor),OP_SELL);
            return( OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_BID), Slippage, Blue) );
         }
         
         if(((TimeCurrent()-OrderOpenTime())/60 >= PERIOD_H1) && OrderProfit()+OrderSwap()+OrderCommission()<0 && takeProfit>0)
         {
            //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
            while (!IsTradeAllowed()) Sleep(100);
            RefreshRates();
            
            //return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), MarketInfo(OrderSymbol(),MODE_BID)+takeProfit, 0, Blue) );
            if(UsePivots==true && isCals[index]==true)
            {
               double chgFromSupport1=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - Support1[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               double chgFromSupport2=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - Support2[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               double chgFromSupport3=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - Support3[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               
               //Log("[Order# "+ticket+"] BID# "+MarketInfo(OrderSymbol(),MODE_BID)+" - takeProfit# "+takeProfit+" - chgFromSupport1# "+chgFromSupport1+" - chgFromSupport2# "+chgFromSupport2+" - chgFromSupport3# "+chgFromSupport3);
               if(chgFromSupport3<0 && MathAbs(chgFromSupport3)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()>(Resist1[index]+takeProfit)))
               {
                  /*if(MathAbs(chgFromSupport3)>=1.5*takeProfit && (OrderStopLoss()==0 || OrderStopLoss()<Support3[index]))
                  {
                     Log("[Order# "+ticket+"] WARNING! Too low. Hedging and modifying stopLoss to "+normPrice(MarketInfo(OrderSymbol(), MODE_BID)-takeProfit));
                     //OpenOrder(OrderSymbol(),NormalizeDouble(OrderLots()*xFactor,MarketInfo(OrderSymbol(),MODE_DIGITS)),OP_SELL);
                     return( OrderModify( OrderTicket(), OrderOpenPrice(), normPrice(MarketInfo(OrderSymbol(), MODE_BID)-takeProfit), OrderTakeProfit(), 0, Blue) );
                  }
                  else
                  {*/
                     Log("[Order# "+ticket+"] Modifying takeprofit to "+(Resist1[index]+takeProfit));
                     return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Resist1[index]+takeProfit, 0, Blue) );
                  /*}*/
               }
               if(chgFromSupport2<0 && MathAbs(chgFromSupport2)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()>(Resist2[index]+takeProfit)))
               {
                  Log("[Order# "+ticket+"] Modifying takeprofit to "+(Resist2[index]+takeProfit));
                  return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Resist2[index]+takeProfit, 0, Blue) );
               }
               if(chgFromSupport1<0 && MathAbs(chgFromSupport1)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()>(Resist3[index]+takeProfit)))
               {
                  Log("[Order# "+ticket+"] Modifying takeprofit to "+(Resist3[index]+takeProfit));
                  return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Resist3[index]+takeProfit, 0, Blue) );
               }
            }
         }

         //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
         while (!IsTradeAllowed()) Sleep(100);
         RefreshRates();

         chgFromOpen = NormalizeDouble(MarketInfo(OrderSymbol(),MODE_BID) - OrderOpenPrice(), MarketInfo(OrderSymbol(),MODE_DIGITS));
         //Log("["+OrderSymbol()+"] isGaining#"+isGaining+"; chgFromOpen#"+chgFromOpen+"; trailStart#"+trailStart+"; StopLevel#"+(StopLevel * MarketInfo(OrderSymbol(),MODE_POINT)) );
      
         // move to break even
         if(isGaining==true && chgFromOpen >= trailStart && chgFromOpen > (StopLevel * MarketInfo(OrderSymbol(),MODE_POINT)) && (OrderStopLoss()==0 || NormalizeDouble(OrderStopLoss(),MarketInfo(OrderSymbol(),MODE_DIGITS)) < NormalizeDouble(OrderOpenPrice(), MarketInfo(OrderSymbol(),MODE_DIGITS))) && trailStart != 0 ) 
         {
            Log("["+OrderSymbol()+"] Moving stop to breakeven on order " + OrderTicket() + ". chgFromOpen is " + chgFromOpen + ", OrderOpenPrice is " + OrderOpenPrice() + " and Bid is " + DoubleToStr( MarketInfo(OrderSymbol(),MODE_BID), MarketInfo(OrderSymbol(),MODE_DIGITS) ) + ", Trail start is " + DoubleToStr( trailStart, MarketInfo(OrderSymbol(),MODE_DIGITS) ) );
            return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Blue) );
         }

         profitPips = MarketInfo(OrderSymbol(),MODE_BID) - ( trailStart + OrderOpenPrice() ) ;
         if( trailAmount != 0) 
            increments = MathFloor( profitPips / trailAmount ); 
         else 
            increments = 0;


         if (isGaining==true && increments >= 1 && MarketInfo(OrderSymbol(),MODE_BID) >= OrderOpenPrice() + trailStart + ( increments * trailAmount ) ) {
            sl = NormalizeDouble( OrderOpenPrice() + ( (increments-1)  * trailAmount ), MarketInfo(OrderSymbol(),MODE_DIGITS));
         
            if( OrderStopLoss()==0 || sl-tralingstep > NormalizeDouble(OrderStopLoss(), MarketInfo(OrderSymbol(),MODE_DIGITS)) ) {
               //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
               while (!IsTradeAllowed()) Sleep(100);
               RefreshRates();
   
               if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Blue ) ){
                  Log("Trailng stop updated. Total increments: " + DoubleToStr(increments, MarketInfo(OrderSymbol(),MODE_DIGITS)) );
                  return(true);
               }
            }
         }
      }

      if( OrderType() == OP_SELL ) {
         if( MaxOpenHours>0 && (TimeCurrent()-OrderOpenTime())/60 >= MaxOpenHours*PERIOD_H1 
            && OrderProfit()+OrderSwap()+OrderCommission()<0)
         {
            //if(MathAbs(Sells-Buys)<=MaxOpenOrders)OpenOrder(OrderSymbol(),normPrice(OrderLots()*xFactor),OP_BUY);
            return( OrderClose(OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(),MODE_ASK), Slippage, Red) );
         }

         if(((TimeCurrent()-OrderOpenTime())/60 >= PERIOD_H1) && OrderProfit()+OrderSwap()+OrderCommission()<0 && takeProfit>0)
         {
            //return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), MarketInfo(OrderSymbol(),MODE_ASK)-takeProfit, 0, Red) );
            if(UsePivots==true && isCals[index]==true)
            {
               double chgFromResist1=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK) - Resist1[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               double chgFromResist2=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK) - Resist2[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               double chgFromResist3=NormalizeDouble(MarketInfo(OrderSymbol(),MODE_ASK) - Resist3[index], MarketInfo(OrderSymbol(),MODE_DIGITS));
               
               //Log("[Order# "+ticket+"] BID# "+MarketInfo(OrderSymbol(),MODE_BID)+" - takeProfit# "+takeProfit+" - chgFromResist1# "+chgFromResist1+" - chgFromResist2# "+chgFromResist2+" - chgFromResist3# "+chgFromResist3);
               if(chgFromResist3>0 && MathAbs(chgFromResist3)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()<(Support1[index]-takeProfit)))
               {
                  /*if(MathAbs(chgFromResist3)>=1.5*takeProfit && (OrderStopLoss() == 0 || OrderStopLoss()>Resist3[index]))
                  {
                     Log("[Order# "+ticket+"] WARNING! Too low. Hedging and modifying takeprofit to "+normPrice(MarketInfo(OrderSymbol(),MODE_ASK)+takeProfit));
                     //OpenOrder(OrderSymbol(),NormalizeDouble(OrderLots()*xFactor,MarketInfo(OrderSymbol(),MODE_DIGITS)),OP_BUY);
                     return( OrderModify( OrderTicket(), OrderOpenPrice(), normPrice(MarketInfo(OrderSymbol(),MODE_ASK)+takeProfit), OrderTakeProfit(), 0, Red) );
                  }
                  else
                  {*/
                     Log("[Order# "+ticket+"] Modifying takeprofit to "+(Support1[index]-takeProfit));
                     return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Support1[index]-takeProfit, 0, Red) );
                  /*}*/
               }
               if(chgFromResist2>0 && MathAbs(chgFromResist2)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()<(Support2[index]-takeProfit)))
               {
                  Log("[Order# "+ticket+"] Modifying takeprofit to "+(Support2[index]-takeProfit));
                  return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Support2[index]-takeProfit, 0, Red) );
               }
               if(chgFromResist1>0 && MathAbs(chgFromResist1)>=takeProfit && (OrderTakeProfit()==0 || OrderTakeProfit()<(Support3[index]-takeProfit)))
               {
                  Log("[Order# "+ticket+"] Modifying takeprofit to "+(Support3[index]-takeProfit));
                  return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderStopLoss(), Support3[index]-takeProfit, 0, Red) );
               }
            }
         }
      
         //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
         while (!IsTradeAllowed()) Sleep(100);
         RefreshRates();

         chgFromOpen = NormalizeDouble(OrderOpenPrice() - MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS));
         //Log("["+OrderSymbol()+"] isGaining#"+isGaining+"; chgFromOpen#"+chgFromOpen+"; trailStart#"+trailStart+"; StopLevel#"+(StopLevel * MarketInfo(OrderSymbol(),MODE_POINT)) );

         // move to break even
         if(isGaining==true && chgFromOpen >= trailStart && chgFromOpen > (StopLevel * MarketInfo(OrderSymbol(),MODE_POINT)) && (OrderStopLoss()==0 || NormalizeDouble(OrderStopLoss(),MarketInfo(OrderSymbol(),MODE_DIGITS)) > NormalizeDouble(OrderOpenPrice(), MarketInfo(OrderSymbol(),MODE_DIGITS))) && trailStart != 0 ) {
            Log("["+OrderSymbol()+"] Moving stop to breakeven on order " + OrderTicket() + ". chgFromOpen is " + chgFromOpen + ", OrderOpenPrice is " + OrderOpenPrice() + " and Ask is " + DoubleToStr( MarketInfo(OrderSymbol(),MODE_ASK), MarketInfo(OrderSymbol(),MODE_DIGITS) ) + ", Trail start is " + DoubleToStr( trailStart, MarketInfo(OrderSymbol(),MODE_DIGITS) ) );
            return( OrderModify( OrderTicket(), OrderOpenPrice(), OrderOpenPrice(), OrderTakeProfit(), 0, Red) );
         }
   
         profitPips = ( OrderOpenPrice()- trailStart ) - MarketInfo(OrderSymbol(),MODE_ASK) ;
         if( trailAmount != 0) { increments = MathFloor( profitPips / trailAmount ); }
         else { increments = 0; }
   
         if (isGaining==true && increments >= 1 && MarketInfo(OrderSymbol(),MODE_ASK) <= OrderOpenPrice() - trailStart - ( increments * trailAmount ) ) {
            sl = NormalizeDouble(OrderOpenPrice() - ( (increments-1) * trailAmount ), MarketInfo(OrderSymbol(),MODE_DIGITS));
   
            if( OrderStopLoss()==0 || sl-tralingstep < NormalizeDouble(OrderStopLoss(), MarketInfo(OrderSymbol(),MODE_DIGITS)) ) {
               //MaloMax: la chiusura di molti ordini puo dare problemi per la lentezza di esecuzione. Tra una chiusura e l'altra potresti inserire questo codice
               while (!IsTradeAllowed()) Sleep(100);
               RefreshRates();

               if( OrderModify( OrderTicket(), OrderOpenPrice(), sl, OrderTakeProfit(), 0, Red) ) {
                  Log("Trailng stop updated. Total increments: " + DoubleToStr(increments, MarketInfo(OrderSymbol(),MODE_DIGITS)) );
                  return(true);
               }
            }
         }
      }

   }   
}

//+------------------------------------------------------------------+
//| initialization of Calspivot function                             |
//+------------------------------------------------------------------+
bool isCalsPivot(string theSymbol, int index)
{
   int    tHour;
   double hLine;
   double lLine;
   int i,k;

   /*if ( TimeHour(iTime(theSymbol,PERIOD_H1,1))== 0 )
     {*/
         tHour = TimeHour(iTime(theSymbol,PERIOD_H1,2));
         k= tHour+1;

         //Log(theSymbol + " - tHour: " + tHour);
         //Log(theSymbol + " - K: " + k);

         hLine = iHigh(theSymbol,PERIOD_H1,tHour);
         lLine = iLow(theSymbol,PERIOD_H1,tHour);
         for (i = tHour+1; i > 0; i--)
           {
         if (iLow(theSymbol,PERIOD_H1,i+1) < lLine) 
            lLine = iLow(theSymbol,PERIOD_H1,i+1);
         if (iHigh(theSymbol,PERIOD_H1,i+1) > hLine) 
            hLine = iHigh(theSymbol,PERIOD_H1,i+1);

            //Log(theSymbol + " - i# "+i+" lLine# "+ lLine+ " Low# "+ iLow(theSymbol,PERIOD_H1,i+1)+ " hLine #"+ hLine+ " High #"+ iHigh(theSymbol,PERIOD_H1,i+1)+ " time #" + TimeHour(iTime(theSymbol,PERIOD_H1,i+1))+ " day #"+ TimeDay(iTime(theSymbol,PERIOD_H1,i+1)));
        }
      pPoint[index] = (lLine + hLine + iClose(theSymbol,PERIOD_H1,2)) / 3;
      r1[index] = 2 * pPoint[index] - lLine;
      s1[index] = 2 * pPoint[index] - hLine;
      r2[index] = pPoint[index] + (r1[index] - s1[index]);
      s2[index] = pPoint[index] - (r1[index] - s1[index]);
      r3[index] = hLine + 2 * (pPoint[index] - lLine);
      s3[index] = lLine - 2 * (hLine - pPoint[index]);
      CalsPivot(theSymbol, index);
      isCals[index] = true;
      return (true);
    /* }
   return (false);*/
}
//+------------------------------------------------------------------+
//| Cals Pivot    function                                           |
//+------------------------------------------------------------------+
void CalsPivot(string theSymbol, int index)
{
   Pivot[index]    =NormalizeDouble(pPoint[index],MarketInfo(theSymbol,MODE_DIGITS));
   Resist1[index]  =NormalizeDouble(r1[index],MarketInfo(theSymbol,MODE_DIGITS));
   Resist2[index]  =NormalizeDouble(r2[index],MarketInfo(theSymbol,MODE_DIGITS));
   Resist3[index]  =NormalizeDouble(r3[index],MarketInfo(theSymbol,MODE_DIGITS));
   Support1[index] =NormalizeDouble(s1[index],MarketInfo(theSymbol,MODE_DIGITS));
   Support2[index] =NormalizeDouble(s2[index],MarketInfo(theSymbol,MODE_DIGITS));
   Support3[index] =NormalizeDouble(s3[index],MarketInfo(theSymbol,MODE_DIGITS));

      //Log(theSymbol + " - Pivot# "+Pivot[index]+" Resist1# "+Resist1[index]+" Support1# "+Support1[index]);
      //Log(theSymbol + " - Pivot# "+Pivot[index]+" Resist2# "+Resist2[index]+" Support2# "+Support2[index]);
      //Log(theSymbol + " - Pivot# "+Pivot[index]+" Resist3# "+Resist3[index]+" Support3# "+Support3[index]);
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