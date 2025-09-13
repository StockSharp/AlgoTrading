//+------------------------------------------------------------------+
//|                                             LarryConnersRSI2.mq5 
//|                                         FOREX - 1 Hour TimeFrame
//|                                Copyright 2017, algotrading.co.za
//|                                         http://algotrading.co.za
//+------------------------------------------------------------------+
#property copyright "Copyright 2017, algotrading.co.za"
#property link      "http://algotrading.co.za"
#property version   "1.00"

#include <Trade\Trade.mqh> // Get code from other places

//--- Input Variables 

input double   lot = 1;                  //Lots
input int      shortSmaPeriods = 5;      //Fast MA period
input int      longSmaPeriods = 200;     //Slow MA period
input int      RSIPeriods = 2;           //RSI Period 
input int      RSILongEntry = 6;         //RSI Long Entry
input int      RSIShortEntry = 95;       //RSI Short Entry

                                         //standard trade management inputs

input int      slippage=3;
input bool     useStopLoss=true;       //Use Stop Loss
input double   stopLossPips=30;        //Stop Loss (pips)
input bool     useTakeProfit=true;     //Use Take Profit
input double   takeProfitPips=60;      //Take Profit (pips)

//--- Service Variables 

CTrade myTradingControlPanel;
MqlRates PriceDataTable[];

double shortSmaData[],longSmaData[],RSIData[];
int numberOfShortSmaData,numberOfLongSmaData,numberOfPriceDataPoints,numberOfRSIData;
int shortSmaControlPanel,longSmaControlPanel,RSIControlPanel;
int P;
double currentBid,currentAsk;
double stopLossPipsFinal,takeProfitPipsFinal,stopLevelPips;
double stopLossLevel,takeProfitLevel;
double shortSma1,shortSma2,longSma1,longSma2,RSI1;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   ArraySetAsSeries(PriceDataTable,true);
   ArraySetAsSeries(shortSmaData,true);
   ArraySetAsSeries(longSmaData,true);
   ArraySetAsSeries(RSIData,true);

   shortSmaControlPanel= iMA(_Symbol,_Period,shortSmaPeriods,0,MODE_SMA,PRICE_CLOSE);
   longSmaControlPanel = iMA(_Symbol,_Period,longSmaPeriods,0,MODE_SMA,PRICE_CLOSE);
   RSIControlPanel=iRSI(_Symbol,_Period,RSIPeriods,PRICE_CLOSE);

   if(_Digits==5 || _Digits==3 || _Digits==1) P=10;else P=1;

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---Release indicators
   IndicatorRelease(shortSmaControlPanel);
   IndicatorRelease(longSmaControlPanel);
   IndicatorRelease(RSIControlPanel);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
// -------------------- Collect most current data --------------------

   currentBid = SymbolInfoDouble(_Symbol,SYMBOL_BID);
   currentAsk = SymbolInfoDouble(_Symbol,SYMBOL_ASK);

   numberOfPriceDataPoints=CopyRates(_Symbol,0,0,10,PriceDataTable);
   numberOfShortSmaData= CopyBuffer(shortSmaControlPanel,0,0,3,shortSmaData);
   numberOfLongSmaData = CopyBuffer(longSmaControlPanel,0,0,3,longSmaData);
   numberOfRSIData=CopyBuffer(RSIControlPanel,0,0,3,RSIData);

   shortSma1 = shortSmaData[1];
   shortSma2 = shortSmaData[2];
   longSma1 = longSmaData[1];
   longSma2 = longSmaData[2];
   RSI1=RSIData[1];

// -------------------- Technical Requirements --------------------

   stopLevelPips=(double)(SymbolInfoInteger(_Symbol,SYMBOL_TRADE_STOPS_LEVEL)+SymbolInfoInteger(_Symbol,SYMBOL_SPREAD))/P;

   if(stopLossPips<stopLevelPips)
     {
      stopLossPipsFinal=stopLevelPips;
     }
   else
     {
      stopLossPipsFinal=stopLossPips;
     }

   if(takeProfitPips<stopLevelPips)
     {
      takeProfitPipsFinal=stopLevelPips;
     }
   else
     {
      takeProfitPipsFinal=takeProfitPips;
     }

// -------------------- EXITS --------------------

   if(PositionSelect(_Symbol)==true) // We have an open position
     {

      // --- Exit Rules (Long Trades) ---

      // --------------------------------------------------------- //

      if(PriceDataTable[1].close>shortSma1) // Rule to exit long trades

         // --------------------------------------------------------- //

        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY) // If it is Buy position
           {

            myTradingControlPanel.PositionClose(_Symbol); // Closes position related to this symbol

            if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
              {
               Print("Exit rules: A close order has been successfully placed with Ticket#: ",myTradingControlPanel.ResultOrder());
              }
            else
              {
               Print("Exit rules: The close order request could not be completed.Error: ",GetLastError());
               ResetLastError();
               return;
              }

           }
        }

      // --------------------------------------------------------- //

      if(PriceDataTable[1].close<shortSma1) // Rule to exit short trades

         // --------------------------------------------------------- //  

        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL) // If it is Sell position
           {

            myTradingControlPanel.PositionClose(_Symbol); // Closes position related to this symbol

            if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
              {
               Print("Exit rules: A close order has been successfully placed with Ticket#: ",myTradingControlPanel.ResultOrder());
              }
            else
              {
               Print("Exit rules: The close order request could not be completed. Error: ",GetLastError());
               ResetLastError();
               return;
              }
           }
        }

     }

// -------------------- ENTRIES --------------------  

   if(PositionSelect(_Symbol)==false) // We have no open position
     {

      // --- Entry Rules (Long Trades) ---

      // --------------------------------------------------------- //

      if(RSI1<RSILongEntry && PriceDataTable[1].close>longSma1) // Rule to enter long trades

         // --------------------------------------------------------- //

        {

         if(useStopLoss) stopLossLevel=currentAsk-stopLossPipsFinal*_Point*P; else stopLossLevel=0.0;
         if(useTakeProfit) takeProfitLevel=currentAsk+takeProfitPipsFinal*_Point*P; else takeProfitLevel=0.0;

         myTradingControlPanel.PositionOpen(_Symbol,ORDER_TYPE_BUY,lot,currentAsk,stopLossLevel,takeProfitLevel,"Buy Trade. Magic Number #"+(string) myTradingControlPanel.RequestMagic()); // Open a Buy position

         if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
           {
            Print("Entry rules: A Buy order has been successfully placed with Ticket#: ",myTradingControlPanel.ResultOrder());
           }
         else
           {
            Print("Entry rules: The Buy order request could not be completed. Error: ",GetLastError());
            ResetLastError();
            return;
           }

        }

      // --- Entry Rules (Short Trades) ---

      // --------------------------------------------------------- //

      if(RSI1>RSIShortEntry && PriceDataTable[1].close<longSma1) // Rule to enter short trades

         // --------------------------------------------------------- //

        {

         if(useStopLoss) stopLossLevel=currentBid+stopLossPipsFinal*_Point*P; else stopLossLevel=0.0;
         if(useTakeProfit) takeProfitLevel=currentBid-takeProfitPipsFinal*_Point*P; else takeProfitLevel=0.0;

         myTradingControlPanel.PositionOpen(_Symbol,ORDER_TYPE_SELL,lot,currentAsk,stopLossLevel,takeProfitLevel,"Sell Trade. Magic Number #"+(string) myTradingControlPanel.RequestMagic()); // Open a Sell position

         if(myTradingControlPanel.ResultRetcode()==10008 || myTradingControlPanel.ResultRetcode()==10009) //Request is completed or order placed
           {
            Print("Entry rules: A Sell order has been successfully placed with Ticket#: ",myTradingControlPanel.ResultOrder());
           }
         else
           {
            Print("Entry rules: The Sell order request could not be completed.Error: ",GetLastError());
            ResetLastError();
            return;
           }

        }

     }

  }

//+------------------------------------------------------------------+
