//+------------------------------------------------------------------+
//|                                                       SORSIT.mq4 |
//|                                                        Jay Davis |
//|                            https://www.mql5.com/en/users/johnthe |
//+------------------------------------------------------------------+
#property copyright "Jay Davis"
#property link      "https://www.mql5.com/en/users/johnthe"
#property version   "1.02"
#property description "Self Optomizing Index Trader"
#property description "Choose top value to cross below to initiate a sell"
#property description "Choose bottom value to cross above to initiate a buy"
#property strict
#define MAX_PERCENT 10		// Maximum balance % used in money management
//+------------------------------------------------------------------+
//| Enumerators                                                      |
//+------------------------------------------------------------------+
enum indicator
  {
   _RSI_,
   _MFI_,
  };
input int magic=4376; // Unique number for this EA
input int optomizingPeriods=144;// Optimization periods(bars)   
input bool inAggressive=false; // Make expert aggressive? Risky
input bool inTradeReverse=false; // Reverse trading style
input bool inOneOrderAtATime=true; // Only one order open at a time?

sinput string Lot_sizing_dynamic_invalidates_static;
input double Lots=0.01;//Static Lot size of orders
input bool inUseDynamicLotSize=true; //Use Dynamic lot sizing 
input double inPercentageOfRisk=2; // Balance % to risk on each trade(2=2%)

sinput string Index_Indicator_Values;
input indicator index=_RSI_; // Choose which index indicator to use
input int IndicatorTopValue = 100; // Top most value you would trade at
input int IndicatorBottomValue = 0;// Bottom most value you would trade at
input ENUM_TIMEFRAMES IndyTimeframe=PERIOD_CURRENT;// Timeframe for index
input int inIndyPeriods=14; // Averaging period for index and ATR calculations
input ENUM_APPLIED_PRICE IndyAppPrice=PRICE_CLOSE;// Applied price for index if needed 

sinput string SL_TP_Dynamic_invalidates_static_values;
input int iStoploss=1000; // Static Stoploss value in points
input int iTakeprofit=2000; // Static Takeprofit value in points
input bool inDynamic = true;// Use Dynamic sp & tp based on ATR multiple?
input double inStoplossMultiple=2; // Dynamic SL = X * ATR (Averaging Period)
input double inTakeProfitMultiple=7; // Dynamic TP = X * ATR (Averaging Period)

sinput string Break_Even_Settings;// Padding must be lower than Trigger
input bool bUseBreakEven=true;// Use Break Even (BE)
input int inTrigger=200; // If BE=[true] set Points in profit to trigger
input int inPadding=100; // Padding points to add to BE must be lower than trigger


                         // Global variables
string VolumeDescription="";// Not used.
int buy=NULL; // to contain best buy value
int sell=NULL; // to contain best sell value
double sellProfits=0.0; // will contain the monetary amount for the best sell value in backtesting
double buyProfits=0.0; // backtested profits for best buy value
datetime NewTime=0; // Variable to implement trade on new bars only
//+------------------------------------------------------------------+
//| Structure for categorizing indicator results                     |
//+------------------------------------------------------------------+
struct Results
  {
   int               rank;
   double            level;
   int               profit;
   int               loss;
   double            pipValue;
   double            monetaryValue;
  };
//+------------------------------------------------------------------+
//| Structure for collecting indicator values                        |
//+------------------------------------------------------------------+
struct Optimizing_Structure
  {
   double            indexValue;
   double            close;
   double            bar;
  };
Optimizing_Structure optimizer[];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(inPadding>inTrigger)Alert("Padding is higher than trigger in break even settings.");

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Fills the optimizer array with data                              |
//+------------------------------------------------------------------+
void FillArray(indicator indexType)
  {
//Fill optimizer array
   ArrayResize(optimizer,optomizingPeriods+1);
   for(int i=0; i<=optomizingPeriods;i++)
     {
      if(indexType==_RSI_)
         optimizer[i].indexValue=iRSI(NULL,IndyTimeframe,inIndyPeriods,IndyAppPrice,i);
      if(indexType==_MFI_)
         optimizer[i].indexValue=iMFI(NULL,IndyTimeframe,inIndyPeriods,i);
      optimizer[i].close=iClose(NULL,0,i);
      optimizer[i].bar=i;
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   if(bUseBreakEven)BreakEven(inTrigger,inPadding);//Use Breakeven if on.
   double currIndyValue=NULL;
   double prevIndyValue=NULL;

   bool OneOrderAtATime=false;
   if(OrdersTotal()==0 || !inOneOrderAtATime)
      OneOrderAtATime=true;

// Trade only on new bars, and one at a time
   if(NewTime!=Time[0] && OneOrderAtATime) // Run only on new bars
     {
      FillArray(index);// fill optimizer array
      NewTime=Time[0];
      if(index==_MFI_)
        {
         currIndyValue=iMFI(NULL,IndyTimeframe,inIndyPeriods,1);
         prevIndyValue=iMFI(NULL,IndyTimeframe,inIndyPeriods,2);
        }
      if(index==_RSI_)
        {
         currIndyValue=iRSI(NULL,IndyTimeframe,inIndyPeriods,IndyAppPrice,1);
         prevIndyValue=iRSI(NULL,IndyTimeframe,inIndyPeriods,IndyAppPrice,2);
        }

      // Normalize Doubles for easier displaying in journal or experts tab
      currIndyValue = NormalizeDouble(currIndyValue,2);
      prevIndyValue = NormalizeDouble(prevIndyValue,2);

      // Stoploss and Takeprofit setup
      double stoploss=iStoploss;
      double takeprofit=iTakeprofit;
      if(inDynamic==true)
        {
         double atr=iATR(NULL,IndyTimeframe,inIndyPeriods,1);
         stoploss=inStoplossMultiple*atr/Point;
         takeprofit=inTakeProfitMultiple*atr/Point;
        }

      // Lotsize determination
      double lotsize=Lots;
      if(inUseDynamicLotSize)
        {
         // What size lot would give the suggested percentage of loss at this stoploss?
         lotsize=getLotSize(NULL,Lots,inPercentageOfRisk,stoploss);
        }

      bool VolumeGood=false;
      int ticket=0,ob=IndicatorTopValue,os=IndicatorBottomValue;
      ob=overbought(stoploss,takeprofit,lotsize);
      os=oversold(stoploss,takeprofit,lotsize);
      VolumeGood=CheckVolumeValue(lotsize,VolumeDescription);

      // Place comments on chart
      Comment("Account Profit : ",AccountProfit(),"\n Balance : ",AccountBalance());
      Print("Sell Value / Sell Profits ",sell,"/",sellProfits,"  :: Buy Value / Buy Profits ",buy,"/",buyProfits);

      if(inTradeReverse)
        {
         double ReverseSell= buyProfits;
         double ReverseBuy = sellProfits;
         buyProfits=ReverseBuy;
         sellProfits=ReverseSell;
        }

      if(sellProfits>buyProfits)
        {
         if((currIndyValue<ob && prevIndyValue>ob) || inAggressive)//sell
           {
            if(CheckMoneyForTrade(NULL,lotsize,OP_SELL) && VolumeGood)
              {
               ticket=OrderSend(NULL,OP_SELL,lotsize,Bid,10,Ask+(stoploss*Point),Ask-(takeprofit*Point),"Sell Order",magic,0,clrBlue);
               Print("Best ",EnumToString(index)," value for sell at ",sell," current ",EnumToString(index)," value: ",currIndyValue,
                     " Old ",EnumToString(index)," ",prevIndyValue," Backtest Profits :: (SELL ",sellProfits,") : BUY ",buyProfits);
              }
           }
        }
      else if(sellProfits<buyProfits)
        {
         if((currIndyValue>os && prevIndyValue<os) || inAggressive)//buy
           {
            if(CheckMoneyForTrade(NULL,lotsize,OP_BUY) && VolumeGood)
              {
               ticket=OrderSend(NULL,OP_BUY,lotsize,Ask,10,Bid -(stoploss*Point),Bid+(takeprofit*Point),"Buy Order",magic,0,clrGreen);
               Print("Best ",EnumToString(index)," value for buy at ",buy," current ",EnumToString(index)," value: ",currIndyValue,
                     " Old ",EnumToString(index)," ",prevIndyValue," Backtest Profits :: (BUY ",buyProfits,") : SELL ",sellProfits);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| overbought value generation                                      |
//+------------------------------------------------------------------+
int overbought(double stoploss,double takeprofit,double lotsize)
  {
// create structure to hold overbought levels
   Results sOverBought[];
   ArrayResize(sOverBought,IndicatorTopValue+1);
   double orderOpenPrice=NULL;
   for(int r=IndicatorBottomValue; r<=IndicatorTopValue; r++)
     {
      bool orderOn=false;
      int iIndyValueToCheck=r;
      // Set initial values for indicator level
      sOverBought[r].level=r;
      sOverBought[r].rank=0;
      sOverBought[r].profit=0;
      sOverBought[r].loss=0;
      sOverBought[r].pipValue=0;
      sOverBought[r].monetaryValue=0;
      for(int i=optomizingPeriods-1; i>1; i--)
        {
         if(!orderOn && optimizer[i].indexValue<iIndyValueToCheck && optimizer[i+1].indexValue>iIndyValueToCheck)
           {
            orderOn=true;
            orderOpenPrice=optimizer[i].close;
            // Now that we have an open order we need to check if it hits stoploss or takeprofit first.
            // Loop through the remaining values.
            for(int remainingBars=i-1; remainingBars>0; remainingBars--)
              {
               // Check if it hits stoploss for a sell
               if(orderOn && optimizer[remainingBars].close>orderOpenPrice+(stoploss*Point))
                 {
                  orderOn=false; // Turn the order off since stoploss was hit
                  sOverBought[r].loss++; // Add to the loss count for this level
                  i=remainingBars; // skip the bars used while the trade was on
                  sOverBought[r].pipValue=sOverBought[r].pipValue+(orderOpenPrice-optimizer[remainingBars].close)/Point;
                  break;
                 }//( orderOn && optimizer[remainingBars].close > orderOpenPrice + (stoploss*Point))
               if(orderOn && optimizer[remainingBars].close<orderOpenPrice-(takeprofit*Point))
                 {
                  orderOn=false; // skip the bars used while the trade was on
                  sOverBought[r].profit++; // Add to the profit count for this level
                  i=remainingBars;// skip the bars used while the trade was on
                  sOverBought[r].pipValue=sOverBought[r].pipValue+(orderOpenPrice-optimizer[remainingBars].close)/Point;
                  break;
                 }
              }//for(int remainingValues=bar-1; remainingValues>0; remainingValues--)
           }//if(!orderOn && optimizer[i].indexValue<iIndyValueToCheck && optimizer[i+1].indexValue>iIndyValueToCheck)
        }// for(int i=optomizingPeriods-1; i>0; i--)
      double tickValue=MarketInfo(NULL,MODE_TICKVALUE);
      if(tickValue!=0)// skip divide by zero error
         if(lotsize!=0)// skip divide by zero error
            sOverBought[r].monetaryValue=NormalizeDouble(sOverBought[r].pipValue/(tickValue/lotsize),2);
     }//   for(int r=IndicatorBottomValue; r<=IndicatorTopValue; r++)
//Check for best value (Which value made the most money)
   sell=NULL;
   sellProfits=0.0;
   for(int i=IndicatorBottomValue; i<=IndicatorTopValue; i++)
     {
      if(sOverBought[i].monetaryValue>sellProfits)//mostMoney)
        {
         sellProfits=sOverBought[i].monetaryValue;
         sell=i;
        }
     }
// Delete sOverBought structure
   ArrayResize(sOverBought,0,IndicatorTopValue+1);
   return sell;
  }//int overbought()
//+------------------------------------------------------------------+
//| oversold value generation                                        |
//+------------------------------------------------------------------+
int oversold(double stoploss,double takeprofit,double lotsize)
  {
// create structure to hold oversold levels
   Results sOverSold[];
   ArrayResize(sOverSold,IndicatorTopValue+1);
   double orderOpenPrice=NULL;
   for(int r=IndicatorBottomValue; r<=IndicatorTopValue; r++)
     {
      bool orderOn=false;
      int iIndyValueToCheck=r;
      // Set initial values for indicator level
      sOverSold[r].level=r;
      sOverSold[r].rank=0;
      sOverSold[r].profit=0;
      sOverSold[r].loss=0;
      sOverSold[r].pipValue=0;
      sOverSold[r].monetaryValue=0;
      for(int i=optomizingPeriods-1; i>1; i--)
        {
         if(!orderOn && optimizer[i].indexValue>iIndyValueToCheck && optimizer[i+1].indexValue<iIndyValueToCheck)
           {
            orderOn=true;
            orderOpenPrice=optimizer[i].close;
            for(int remainingBars=i-1; remainingBars>0; remainingBars--)
              {
               // Check if it hits stoploss for a buy
               if(orderOn && optimizer[remainingBars].close<orderOpenPrice-(stoploss*Point))
                 {
                  orderOn=false; // Turn the order off since stoploss was hit
                  sOverSold[r].loss++; // add to loss counter
                  i=remainingBars; // skip the bars used while the trade was on
                  sOverSold[r].pipValue=sOverSold[r].pipValue+(optimizer[remainingBars].close-orderOpenPrice)/Point;
                  break;
                 }//( orderOn && optimizer[remainingBars].close > orderOpenPrice + (stoploss*Point))
               if(orderOn && optimizer[remainingBars].close>orderOpenPrice+(takeprofit*Point))
                 {
                  orderOn=false;// Turn the order off since stoploss was hit
                  sOverSold[r].profit++; // add to profit counter
                  i=remainingBars; // skip the bars used while the trade was on
                  sOverSold[r].pipValue=sOverSold[r].pipValue+(optimizer[remainingBars].close-orderOpenPrice)/Point;
                  break;
                 }
              }//for(int remainingValues=bar-1; remainingValues>0; remainingValues--)
           }// if(!orderOn && optimizer[i].indexValue>iIndyValueToCheck && optimizer[i+1].indexValue<iIndyValueToCheck)
        }// for(int i=optomizingPeriods-1; i>0; i--)
      double tickValue=MarketInfo(NULL,MODE_TICKVALUE);
      if(tickValue!=0)// skip divide by zero error
         if(lotsize!=0)// skip divide by zero error
            sOverSold[r].monetaryValue=NormalizeDouble(sOverSold[r].pipValue/(tickValue/lotsize),2);
     }//   for(int r=IndicatorBottomValue; r<=IndicatorTopValue; r++)

//Check for best value
   buyProfits=0.0;
   buy=NULL;
   for(int i=IndicatorTopValue; i>=0; i--)
     {
      if(sOverSold[i].monetaryValue>buyProfits)
        {
         buyProfits=sOverSold[i].monetaryValue;
         buy=i;
        }
     }
// Delete sOverSold structure
   ArrayResize(sOverSold,0,IndicatorTopValue+1);
   return buy;
  }//int oversold()
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume,string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f",min_volume);
      return(false);
     }

//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f",max_volume);
      return(false);
     }

//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_STEP);

   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step,ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb,double lots,int type)
  {
   double free_margin=AccountFreeMarginCheck(symb,type,lots);
//-- if there is not enough money
   if(free_margin<0)
     {
      string oper=(type==OP_BUY)? "Buy":"Sell";
      Print("Not enough money for ",oper," ",lots," ",symb," Error code=",GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }
//+------------------------------------------------------------------+
//| Moves stoploss to breakeven after a certain level                |
//+------------------------------------------------------------------+
void BreakEven(double trigger,double padding)
  {
   if(padding>trigger) padding=0.0;
   int total=OrdersTotal();
   bool orderSelected=false,modify=false;
   for(int i=total; i>0; i--)
     {
      orderSelected=OrderSelect(i-1,SELECT_BY_POS);
      if(orderSelected==true)
        {
         if(OrderMagicNumber()==magic)
           {
            double stoplevel=MarketInfo(NULL,MODE_STOPLEVEL)*Point;
            if(OrderType()==OP_SELL)
              {
               double currentStopLoss=OrderStopLoss();
               double openPrice=OrderOpenPrice();
               if(currentStopLoss>openPrice)
                 {
                  double newStoploss=currentStopLoss;
                  if(Ask+stoplevel<openPrice -(trigger*Point))
                     newStoploss=openPrice-(padding*Point);
                  if(newStoploss<currentStopLoss)
                     modify=OrderModify(OrderTicket(),openPrice,newStoploss,OrderTakeProfit(),0,clrLavender);
                  if(modify)
                     Print("Sell order #",OrderTicket()," moved to breakeven");
                 }
              }
            if(OrderType()==OP_BUY)
              {
               double currentStopLoss=OrderStopLoss();
               double openPrice=OrderOpenPrice();
               if(currentStopLoss<openPrice)
                 {
                  double newStoploss=currentStopLoss;
                  if(Bid-stoplevel>openPrice+(trigger*Point))
                     newStoploss=openPrice+(padding*Point);
                  if(newStoploss>currentStopLoss)
                     modify=OrderModify(OrderTicket(),openPrice,newStoploss,OrderTakeProfit(),0,clrLavender);
                  if(modify)
                     Print("Buy order #",OrderTicket()," moved to breakeven");
                 }
              }
           }
        }
     }
  }
// Risk-based money management using stop loss in points
double getLotSize(string pSymbol,double pFixedVol,double pPercent,double pStopPoints)
  {
   double tradeSize;

   if(pPercent>0 && pStopPoints>0)
     {
      if(pPercent>MAX_PERCENT) pPercent=MAX_PERCENT;

      double margin=AccountInfoDouble(ACCOUNT_BALANCE) *(pPercent/100);
      double tickSize=SymbolInfoDouble(pSymbol,SYMBOL_TRADE_TICK_VALUE);
      if(tickSize == 0)return(pFixedVol);
      tradeSize = (margin / pStopPoints) / tickSize;
      tradeSize = VerifyVolume(pSymbol,tradeSize);

      return(tradeSize);
     }
   else
     {
      tradeSize = pFixedVol;
      tradeSize = VerifyVolume(pSymbol,tradeSize);

      return(tradeSize);
     }
  }
// Verify and adjust trade volume
double VerifyVolume(string pSymbol,double pVolume)
  {
   double minVolume = SymbolInfoDouble(pSymbol,SYMBOL_VOLUME_MIN);
   double maxVolume = SymbolInfoDouble(pSymbol,SYMBOL_VOLUME_MAX);
   double stepVolume= SymbolInfoDouble(pSymbol,SYMBOL_VOLUME_STEP);

   double tradeSize;
   if(pVolume<minVolume) tradeSize=minVolume;
   else if(pVolume>maxVolume) tradeSize=maxVolume;
   else tradeSize=MathRound(pVolume/stepVolume)*stepVolume;

   if(stepVolume >= 0.1) tradeSize = NormalizeDouble(tradeSize,1);
   else tradeSize = NormalizeDouble(tradeSize,2);

   return(tradeSize);
  }
//+------------------------------------------------------------------+
