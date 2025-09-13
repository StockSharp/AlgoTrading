//+------------------------------------------------------------------+
//|                                                        TK EA.mq4 |
//|                        Copyright 2014, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "P.A.Cofflin"
#property link  "p.a.cofflin@gmail.com"

extern string email = "Email me if you want me to check your inputs or just to share :)";
extern string emailaddress = "p.a.cofflin@gmail.com";
extern string tutorial = "https://www.youtube.com/watch?v=ZKRzSfHMjpQ";
extern string README = "ALL DEFAULT VALUES ARE JUST PLACEHOLDERS!";
extern string OrderSetup = "Order Setup";
extern double MaxLotSize = 500;
extern double MinLotSize = 0.01;
extern double MaxOrders = 1;
extern double OrderExpirationTimer = 720;
extern double AcountBalanceUsedPercent = 10;
extern double OrderOpenLevel = 10;
extern double Slippage = 3;
extern double TakeProfit = 150;
extern double StopLoss = 300;
extern string space1 = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";
extern string HedgingOption = "Option to place sell order when buying";
extern bool Hedging = false;
extern string HedgingMultiplier = "The option to change lot size of hedge order";
extern double HedgingLotSizeMultiplier= 1;
extern string space2 = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";
extern string MinimumMultiplier = "Minimum for an order to be closed";
extern double MinimumStopLossMultiplier = 10;
extern double MinimumTakeProfitMultiplier = 10;
extern string space3 = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";
extern string StopLossTakeProfit = "Following section for different order close options";
extern string TrailingStopandStep = "Sets a trailing stop and step ammount that follows the stop";
extern bool TrailingStopLoss = false;
extern double TrailingStop = 25;
extern double TrailingStep = 25;
extern string PercentageClose = "Sets a percentage to close an order based on Account Equity";
extern bool PercentageStopLoss = false;
extern double AccountPercentageStopLoss = 10;
extern bool PercentageTakeProfit = false;
extern double AccountPercentageTakeProfit = 10;
extern string CloseTimer = "Closes a trade after a specified amount of seconds";
extern bool StopLossTimer = false;
extern double TimedStopLoss = 3600;
extern bool TakeProfitTimer = false;
extern double TimedTakeProfit = 3600;
extern string StochasticClose = "Closes order after Kperiod crosses Dperiod";
extern bool StochasticTakeProfit = false;
extern double KperiodTakeProfit = 20;
extern double DperiodTakeProfit = 5;
extern double SlowingTakeProfit = 3;
extern bool StochasticStopLoss = false;
extern double KperiodStopLoss = 20;
extern double DperiodStopLoss = 5;
extern double SlowingStopLoss = 3;
extern string space4 = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";
extern string OrderModifiers = "The following modify an order to minimize loss potential";
extern string PercentChange = "Checks if a certain percentage change has occured"; 
extern bool StochasticPercentChange = false;
extern double StochasticPercentChangeLevel = 1;
extern string MovingAverageLimter = "Prevents Buy signal from occuring below SlowMA";
extern bool MATradeLimiter = false;
extern string MovingAverageLimter2 = "Prevents Buy signal when FastMA is 2 bars above SlowMA";
extern bool MATradeLimiter2 = false;
extern double SlowMALimiterperiod = 20;
extern double FastMALimiterperiod = 10;
extern string StocTradeLimiter = "Prevents orders two bars above stochastic crossing";
extern bool StochasticTradeLimiter = false;
extern double KperiodLimiter = 20;
extern double DperiodLimiter = 5;
extern double SlowingLimiter = 3;
extern string space5 = "~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~";
extern string Indicators = "Choose which indicators and parameters to use";
extern bool StochasticBuy = true;
extern double StocOverSoldLevel = 20;
extern double Kperiod = 20;
extern double Dperiod = 5;
extern double Slowing = 3;
extern string MACross = "Moving average crossing buy signal";
extern bool MACrossBuy = false;
extern double SlowMAperiod = 20;
extern double FastMAperiod = 10;
extern string MACDBuySignal = "Buy signal when signal line goes below MACD level";
extern bool MACDBuy = false;
extern double FastEMA = 5;
extern double SlowEMA = 15;
extern double SMA = 2;
extern string VolumeSignal = "Buy signal of two increasing volumes and minimum volume level required for buy signal"; 
extern bool VolumeBuy = false;
extern double VolumeMinimumLevel = 200;
extern string RSISignal = "Buy when fast RSI crosses above slow RSI";
extern bool RSIBuy = false;
extern double FastRSI = 13;
extern double SlowRSI = 26;
extern string BolingerBands = "BB for short, gives buy signal when BB seperate by a certain amount of pips";
extern bool BBBuy = false;
extern double BBSeperation = 10;
extern double BBUpperperiod = 20;
extern double BBUpperdeviation = 2;
extern double BBLowerperiod = 20;
extern double BBLowerdeviation = 2;
extern string CandlestickBuy = "Sets a minimum candlestick size before a buy signal is sent";
extern bool CandlestickBuySignal = false;
extern double MinCandleSize = 10;
extern string CandleHLC = "Candlestick high, low, close are high than previos bar then a buy signal is sent";
extern bool CandleHLCOne = false;
extern bool CandleHLCTwo = false;
bool Buy = false;
bool Sell = false;
extern int MagicNumber = 1;
double pips;  
extern string version = "1.5614";



void MoveTrailingStop()
{int cnt,total=OrdersTotal();
   for(cnt=0;cnt<total;cnt++)
   {OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderType()<=OP_SELL&&OrderSymbol()==Symbol())
      {if(OrderType()==OP_BUY)
         {if(TrailingStop>0)  
            {if((NormalizeDouble(OrderStopLoss(),Digits)<NormalizeDouble(Bid-Point*(TrailingStop+TrailingStep),Digits))||(OrderStopLoss()==0))
               {OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Bid-Point*TrailingStop,Digits),OrderTakeProfit(),0,Green);
                  return(0);}}}
         else 
         {if(TrailingStop>0)  
            {if((NormalizeDouble(OrderStopLoss(),Digits)>(NormalizeDouble(Ask+Point*(TrailingStop+TrailingStep),Digits)))||(OrderStopLoss()==0))
               {OrderModify(OrderTicket(),OrderOpenPrice(),NormalizeDouble(Ask+Point*TrailingStop,Digits),OrderTakeProfit(),0,Red);
                  return(0);}}}}}}




//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   double ticksize = MarketInfo(Symbol(),MODE_TICKSIZE);
   if (ticksize == 0.00001 || ticksize == 0.001)
   pips = ticksize*10;
   else pips = ticksize;
   
   // Thanks to Jimdandy1958, his youtube tutorials taught me how to code mql4. Check out his tutorials at http://www.youtube.com/watch?v=n8fZINmSv0g.
   // Also check out his websites http://www.jimdandyforex.com and http://jimdandymql4courses.com.
   
 
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
int i=0; 
double Marketprice = ((Ask+Bid)/2);

//
double CurrentStochasticKperiod = iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,0,0);
double FirstStochasticKperiod =   iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,0,1);
double SecondStochasticKperiod =  iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,0,2);

double CurrentStochasticDperiod = iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,1,0);
double FirstStochasticDperiod =   iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,1,1);
double SecondStochasticDperiod =  iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,1,2);
//

//
double FirstStochasticLimiterKperiod = iStochastic(NULL,0,KperiodLimiter,DperiodLimiter,SlowingLimiter,3,1,0,1);
double SecondStochasticLimiterKperiod = iStochastic(NULL,0,KperiodLimiter,DperiodLimiter,SlowingLimiter,3,1,0,2);

double FirstStochasticLimiterDperiod =   iStochastic(NULL,0,KperiodLimiter,DperiodLimiter,SlowingLimiter,3,1,1,1);
double SecondStochasticLimiterDperiod =  iStochastic(NULL,0,KperiodLimiter,DperiodLimiter,SlowingLimiter,3,1,1,2);
//

//
double FirstStochasticSLKperiod = iStochastic(NULL,0,KperiodStopLoss,DperiodStopLoss,SlowingStopLoss,3,1,0,1);
double SecondStochasticSLKperiod = iStochastic(NULL,0,KperiodStopLoss,DperiodStopLoss,SlowingStopLoss,3,1,0,2);

double FirstStochasticSLDperiod =   iStochastic(NULL,0,KperiodStopLoss,DperiodStopLoss,SlowingStopLoss,3,1,1,1);
double SecondStochasticSLDperiod =  iStochastic(NULL,0,KperiodStopLoss,DperiodStopLoss,SlowingStopLoss,3,1,1,2);
//

//
double FirstStochasticTPKperiod = iStochastic(NULL,0,KperiodTakeProfit,DperiodTakeProfit,SlowingTakeProfit,3,1,0,1);
double SecondStochasticTPKperiod = iStochastic(NULL,0,KperiodTakeProfit,DperiodTakeProfit,SlowingTakeProfit,3,1,0,2);

double FirstStochasticTPDperiod =   iStochastic(NULL,0,KperiodTakeProfit,DperiodTakeProfit,SlowingTakeProfit,3,1,1,1);
double SecondStochasticTPDperiod =  iStochastic(NULL,0,KperiodTakeProfit,DperiodTakeProfit,SlowingTakeProfit,3,1,1,2);
//


double SBS = (iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,0,1) - iStochastic(NULL,0,Kperiod,Dperiod,Slowing,3,1,0,2));

double FirstMACDBuy = iMACD(NULL,0,FastEMA,SlowEMA,SMA,6,0,1);
double SecondMACDBuy = iMACD(NULL,0,FastEMA,SlowEMA,SMA,6,0,2);

double FirstMACDBuySignalLine = iMACD(NULL,0,FastEMA,SlowEMA,SMA,6,1,1);
double SecondMACDBuySignalLine = iMACD(NULL,0,FastEMA,SlowEMA,SMA,6,1,2);

double FirstVolumeBuy = iVolume(NULL,0,1);
double SecondVolumeBuy = iVolume(NULL,0,2);

double FastRSIOne = iRSI(NULL,0,FastRSI,6,1);
double SlowRSIOne = iRSI(NULL,0,SlowRSI,6,1);

double BBUpperOne = iBands(NULL,NULL,BBUpperperiod,BBUpperdeviation,NULL,6,1,1);
double BBLowerOne = iBands(NULL,NULL,BBLowerperiod,BBLowerdeviation,NULL,6,2,1);

double LotSize = (AccountEquity()*(AcountBalanceUsedPercent/100))/1000; 
if (LotSize>MaxLotSize) LotSize = MaxLotSize;
if (LotSize<MinLotSize) LotSize = MinLotSize;


double BuyLowOne = iLow(NULL,0,1);
double BuyLowTwo = iLow(NULL,0,2);
double BuyLowThree = iLow(NULL,0,3);

double BuyHighOne = iHigh(NULL,0,1);
double BuyHighTwo = iHigh(NULL,0,2);
double BuyHighThree = iHigh(NULL,0,3);

double BuyCloseOne = iClose(NULL,0,1);
double BuyCloseTwo = iClose(NULL,0,2);
double BuyCloseThree = iClose(NULL,0,3);



double CurrentSlowMA = iMA(NULL,0,SlowMAperiod,0,3,6,1);
double FirstSlowMA = iMA(NULL,0,SlowMAperiod,0,3,6,2);


double CurrentFastMA = iMA(NULL,0,FastMAperiod,0,3,6,1);
double FirstFastMA = iMA(NULL,0,FastMAperiod,0,3,6,2);

double FirstSlowMALimiter = iMA(NULL,0,SlowMALimiterperiod,0,3,6,2);
double FirstFastMALimiter = iMA(NULL,0,FastMALimiterperiod,0,3,6,2);


double ExpirationTime = TimeCurrent()+OrderExpirationTimer;

double CurrentClose = iClose(NULL,0,0);
double CloseOne =     iClose(NULL,0,1);

double CurrentOpen = iOpen(NULL,0,0);

double CurrentCandleBuy = (iClose(NULL,0,0)-iOpen(NULL,0,0));
double CandleBuy = (iClose(NULL,0,1)-iOpen(NULL,0,1));
double CandleBuyTwo = (iClose(NULL,0,2)-iOpen(NULL,0,2));
double CandleBuyThree = (iClose(NULL,0,3)-iOpen(NULL,0,3));

double CurrentCOMinus = MathAbs(iClose(NULL,0,0)-iOpen(NULL,0,0));
double COMinus = MathAbs(iClose(NULL,0,1)-iOpen(NULL,0,1));


double MinSell = MathAbs((OrderOpenPrice()-Ask));
double Minsell = MathAbs((Bid-OrderOpenPrice()));
double MinSellLevel = (pips*MinimumStopLossMultiplier);

double MinBuy = MathAbs((OrderOpenPrice()-Ask));
double Minbuy = MathAbs((Bid-OrderOpenPrice()));
double MinBuyLevel = (pips*MinimumTakeProfitMultiplier);

bool MATradeLimiterSignal = Ask>CurrentSlowMA;

bool CandleHighBuyOne = BuyHighOne>BuyHighTwo;

bool CandleLowBuyOne = BuyLowOne>BuyLowTwo;

bool CandleCloseBuyOne = BuyCloseOne>BuyCloseTwo;

bool CandleHighBuyTwo = BuyHighTwo>BuyHighThree;

bool CandleLowBuyTwo = BuyLowTwo>BuyLowThree;

bool CandleCloseBuyTwo = BuyCloseTwo>BuyCloseThree;

bool CandleBuySignal = CandleBuy>(MinCandleSize*pips);

bool BBBuySignal = (BBUpperOne-BBLowerOne)>(BBSeperation*pips);

bool RSIBuySignal = FastRSIOne>SlowRSIOne;

bool VolumeMinimum = FirstVolumeBuy>VolumeMinimumLevel;

bool VolumeBuySignal = FirstVolumeBuy>SecondVolumeBuy;

bool MACDBuySignalOne = FirstMACDBuy>FirstMACDBuySignalLine;

bool MACDBuySignalTwo = SecondMACDBuy<FirstMACDBuy;

bool StochasticBuyOne= FirstStochasticLimiterKperiod>FirstStochasticLimiterDperiod;

bool StochasticBuyTwo = SecondStochasticLimiterKperiod>SecondStochasticLimiterDperiod;

bool StochasticBuySignal = FirstStochasticDperiod<FirstStochasticKperiod;

bool StochasticOverSold = FirstStochasticKperiod<StocOverSoldLevel;

bool MACrossBuySignal = CurrentFastMA>CurrentSlowMA;

bool SBSone = SBS>StochasticPercentChangeLevel;

if(OrdersTotal()>=0 && CandlestickBuySignal==true) 
  {if(CandleBuySignal==true) Buy=true;
  {if(CandleBuySignal==false) Buy=false;
  }}

if(OrdersTotal()>=0 && CandleHLCOne==true) 
  {if(CandleHighBuyOne==true && CandleLowBuyOne==true && CandleCloseBuyOne==true) Buy=true;
  {if(CandleHighBuyOne==false) Buy=false;
  {if(CandleLowBuyOne==false) Buy=false;
  {if(CandleCloseBuyOne==false) Buy=false;
  }}}}
  
if(OrdersTotal()>=0 && CandleHLCTwo==true) 
  {if(CandleHighBuyTwo==true && CandleLowBuyTwo==true && CandleCloseBuyTwo==true) Buy=true;
  {if(CandleHighBuyTwo==false) Buy=false;
  {if(CandleLowBuyTwo==false) Buy=false;
  {if(CandleCloseBuyTwo==false) Buy=false;
  }}}}

if(OrdersTotal()>=0 && BBBuy==true) 
  {if(BBBuySignal==true) Buy=true;
  {if(BBBuySignal==false) Buy=false;
  }}

if(OrdersTotal()>=0 && RSIBuy==true) 
  {if(RSIBuySignal==true) Buy=true;
  {if(RSIBuySignal==false) Buy=false;
  }}


if(OrdersTotal()>=0 && VolumeBuy==true) 
  {if(VolumeBuySignal==true && VolumeMinimum==true) Buy=true;
  {if(VolumeMinimum==false) Buy=false;
  {if(VolumeBuySignal==false) Buy=false;
  }}}



if(OrdersTotal()>=0 && MACDBuy==true) 
  {if(MACDBuySignalOne==true && MACDBuySignalTwo==true) Buy=true;
  {if(MACDBuySignalOne==false) Buy=false;
  {if(MACDBuySignalTwo==false) Buy=false;
  }}}


if(OrdersTotal()>=0 && StochasticPercentChange==true) 
  {if(SBSone==true) Buy=true;
  {if(SBSone==false) Buy=false;
  }}


if(OrdersTotal()>=0 && MACrossBuy==true) 
  {if(MACrossBuySignal==true) Buy=true;
  {if(MACrossBuySignal==false) Buy=false;
  }}


if(OrdersTotal()>=0 && StochasticBuy==true) 
  {if(StochasticBuySignal==true && StochasticOverSold==true) Buy=true;
  {if(StochasticBuySignal==false) Buy=false;
  {if(StochasticOverSold==false) Buy=false;
  }}}

   
if(OrdersTotal()>=0 && StochasticTradeLimiter==true && SecondStochasticLimiterKperiod>SecondStochasticLimiterDperiod) Buy=false;
if(OrdersTotal()>=0 && StochasticTradeLimiter==true && FirstStochasticLimiterKperiod<FirstStochasticLimiterDperiod) Buy=false;

if(OrdersTotal()>=0 && Ask<CurrentSlowMA && MATradeLimiter==true) Buy=false;
if(OrdersTotal()>=0 && FirstSlowMALimiter<FirstFastMALimiter && MATradeLimiter2==true) Buy=false;


   
      if(OrdersTotal()<MaxOrders && Buy==true)
      OrderSend(Symbol(),OP_BUYSTOP,LotSize,Ask+(pips*OrderOpenLevel),3,Ask-(StopLoss*pips),Ask+(pips*TakeProfit),NULL,MagicNumber,ExpirationTime,Green);
      
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
          if(AccountBalance()>(AccountEquity()*(1+((AccountPercentageStopLoss*LotSize)/100))) && PercentageStopLoss==true && MinSell>MinSellLevel && OrderMagicNumber()==MagicNumber)
            {if( OrderType() == OP_BUY && Bid < OrderOpenPrice()) 
             OrderClose( OrderTicket(), OrderLots(), Bid, Slippage ); 
            else if( OrderType() == OP_SELL && Ask > OrderOpenPrice())
             OrderClose( OrderTicket(), OrderLots(), Ask, Slippage );
             Print("Account Percentage Stop Loss Close");}}}
             
            
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
          if(AccountEquity()>(AccountBalance()*(1+((AccountPercentageTakeProfit*LotSize)/100))) && PercentageTakeProfit==true && MinBuy>MinBuyLevel && OrderMagicNumber()==MagicNumber)
            {if( OrderType() == OP_BUY && Bid > OrderOpenPrice()) 
             OrderClose( OrderTicket(), OrderLots(), Bid, Slippage); 
            else if( OrderType() == OP_SELL && Ask < OrderOpenPrice())
             OrderClose( OrderTicket(), OrderLots(), Ask, Slippage );
             Print("Account Percentage Take Profit Close");}}}
             
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
            if( (OrderOpenTime() + TimedStopLoss) < TimeCurrent() && StopLossTimer==true && OrderProfit()<=MinBuyLevel && MinSell>MinSellLevel && OrderMagicNumber()==MagicNumber)
               {if( OrderType() == OP_BUY && Bid < OrderOpenPrice()) 
                OrderClose( OrderTicket(), OrderLots(), Bid, Slippage); 
               else if( OrderType() == OP_SELL && Ask > OrderOpenPrice())
                OrderClose( OrderTicket(), OrderLots(), Ask, Slippage);
                Print("Timed Stop Loss Close");}}}
                
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
            if( (OrderOpenTime() + TimedTakeProfit) < TimeCurrent() && TakeProfitTimer==true && OrderProfit()>=MinBuyLevel && MinBuy>MinBuyLevel && OrderMagicNumber()==MagicNumber)
               {if( OrderType() == OP_BUY && Bid > OrderOpenPrice()) 
                OrderClose( OrderTicket(), OrderLots(), Bid, Slippage); 
               else if( OrderType() == OP_SELL && Ask < OrderOpenPrice())
                OrderClose( OrderTicket(), OrderLots(), Ask, Slippage);
                Print("Timed Take Profit Close");}}}
                
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
            if((FirstStochasticSLKperiod<FirstStochasticSLDperiod) && StochasticStopLoss==true && OrderProfit()<=MinBuyLevel && MinSell>MinSellLevel && OrderMagicNumber()==MagicNumber)
               {if( OrderType() == OP_BUY && Bid < OrderOpenPrice()) 
                OrderClose( OrderTicket(), OrderLots(), Bid, Slippage); 
               else if( OrderType() == OP_SELL && Ask > OrderOpenPrice())
                OrderClose( OrderTicket(), OrderLots(), Ask, Slippage);
                Print("Stochastic Stop Loss Close");}}}

        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
            if((FirstStochasticTPKperiod<FirstStochasticTPDperiod) && OrderProfit()>0 && StochasticTakeProfit==true && MinBuy>MinBuyLevel && OrderMagicNumber()==MagicNumber)
               {if( OrderType() == OP_BUY && Bid > OrderOpenPrice()) 
                OrderClose( OrderTicket(), OrderLots(), Bid, Slippage); 
               else if( OrderType() == OP_SELL && Ask < OrderOpenPrice())
                OrderClose( OrderTicket(), OrderLots(), Ask, Slippage);
                Print("Stochastic Take Profit Close");}}}

                


   {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
             {if(OrdersTotal()<=(MaxOrders) && OrdersTotal()>0 && OrderType()==OP_BUY && Hedging==true)
              OrderSend(Symbol(),OP_SELLSTOP,(LotSize*HedgingLotSizeMultiplier),Bid-(pips*OrderOpenLevel),3,Bid+(StopLoss*pips),Bid-(pips*TakeProfit),NULL,MagicNumber,ExpirationTime,Green);
             }}}
   
        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
          if(OrderProfit()>0 && TrailingStop>0 && TrailingStopLoss==true && MinBuy>MinBuyLevel && OrderMagicNumber()==MagicNumber)
            {MoveTrailingStop();
            Print("Trailing Stop");}}}

        {for(i =OrdersTotal() - 1 ; i >=0 ; i--)
         {if( OrderSelect( i, SELECT_BY_POS, MODE_TRADES ) == false ) break; 
          if(OrderProfit()<0 && TrailingStop>0 && TrailingStopLoss==true && MinSell>MinSellLevel && OrderMagicNumber()==MagicNumber)
            {MoveTrailingStop();
            Print("Trailing Stop");}}}

//----

   return(0);
  }
//+------------------------------------------------------------------+

