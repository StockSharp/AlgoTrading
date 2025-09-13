//+------------------------------------------------------------------+
//|                                        MartingaleEA-5 Levels.mq5 |
//|                                  Copyright 2023, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\SymbolInfo.mqh>

CTrade trading;
CPositionInfo positioning;
CSymbolInfo symbolling;

//***************************Margingale mode**************************//
double VOL;
                     
input bool MartingaleMode = true;
input string Symbol_Name = "EURUSD";                         //Name Of The Symbol
input double MartingaleVolumeMultiplier = 2;                 //Margingale Volume Multiply:
input int MargingaleNum = 4;                                 //Number of Martingale Trading (MAX 5 Times):
input double MartingaleDis1 = 300;                           // NO.1 Martingale Distance(pips):
input double MartingaleDis2 = 400;                           // NO.2 Martingale Distance(pips):
input double MartingaleDis3 = 500;                           // NO.3 Martingale Distance(pips):
input double MartingaleDis4 = 600;                           // NO.4 Martingale Distance(pips):
input double MartingaleDis5 = 700;                           // NO.5 Martingale Distance(pips):
input double MartingaleTakeProfit = 200;                     // Close All Buy Or Sell Pos When Total Profit is ($):
input double MartingaleStopLoss = -500;                      // Close All Buy Or Sell Pos When Total Loss is (-$):
int BCount = 0;
int SCount = 0;
double BMartingaleVol = VOL;
double SMartingaleVol = VOL;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

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
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   if(MartingaleMode)
     {
      MartingaleTrading();
      MartingaleClosePos();
     }


  }
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//|                      Function Martingale                         |
//+------------------------------------------------------------------+
void MartingaleTrading()
  {

// for martingale buy positions ********************************************

   for(int i = PositionsTotal()-1; i>=0; i--)
     {
      if(positioning.SelectByIndex(i))
        {
         if(positioning.Symbol() == Symbol_Name)
           {

            if(positioning.PositionType() == POSITION_TYPE_BUY && positioning.Profit() < 0 && positioning.PriceOpen() - positioning.PriceCurrent() >= MartingaleDis1 *Point())
              {
               if(BCount < MargingaleNum && BCount <1)
                 {
                  BMartingaleVol = positioning.Volume();
                  BMartingaleVol =NormalizeDouble(BMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Buy(BMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"");
                  BCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_BUY && positioning.Profit() <0 && positioning.PriceOpen() - positioning.PriceCurrent() >= (MartingaleDis1+MartingaleDis2) *Point())
              {
               if(BCount < MargingaleNum && BCount <2)
                 {

                  BMartingaleVol = NormalizeDouble(BMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Buy(BMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"");
                  BCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_BUY && positioning.Profit() <0 && positioning.PriceOpen() - positioning.PriceCurrent() >= (MartingaleDis1+MartingaleDis2 + MartingaleDis3) *Point())
              {
               if(BCount < MargingaleNum && BCount<3)
                 {

                  BMartingaleVol = NormalizeDouble(BMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Buy(BMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"");
                  BCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_BUY && positioning.Profit() <0 && positioning.PriceOpen() - positioning.PriceCurrent() >= (MartingaleDis1+MartingaleDis2 + MartingaleDis3 +MartingaleDis4) *Point())
              {
               if(BCount < MargingaleNum && BCount <4)
                 {

                  BMartingaleVol = NormalizeDouble(BMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Buy(BMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"");
                  BCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_BUY && positioning.Profit() < 0 && positioning.PriceOpen() - positioning.PriceCurrent() >= (MartingaleDis1+MartingaleDis2 + MartingaleDis3 +MartingaleDis4+ MartingaleDis5) *Point())
              {
               if(BCount < MargingaleNum && BCount<5)
                 {

                  BMartingaleVol = NormalizeDouble(BMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Buy(BMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_ASK),0,0,"");
                  BCount++;

                 }
              }

            // for martingale sell positions ********************************************

            if(positioning.PositionType() == POSITION_TYPE_SELL && positioning.Profit() < 0 && positioning.PriceCurrent() - positioning.PriceOpen() >= MartingaleDis1 *Point())
              {
               if(SCount < MargingaleNum && SCount <1)
                 {
                  SMartingaleVol = positioning.Volume();
                  SMartingaleVol =NormalizeDouble(SMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Sell(SMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"");
                  SCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_SELL && positioning.Profit() < 0 && positioning.PriceCurrent() - positioning.PriceOpen() >= (MartingaleDis1 + MartingaleDis2) *Point())
              {
               if(SCount < MargingaleNum && SCount <2)
                 {
                  SMartingaleVol =NormalizeDouble(SMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Sell(SMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"");
                  SCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_SELL && positioning.Profit() < 0 && positioning.PriceCurrent() - positioning.PriceOpen() >= (MartingaleDis1 + MartingaleDis2 + MartingaleDis3) *Point())
              {
               if(SCount < MargingaleNum && SCount <3)
                 {
                  SMartingaleVol =NormalizeDouble(SMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Sell(SMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"");
                  SCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_SELL && positioning.Profit() < 0 && positioning.PriceCurrent() - positioning.PriceOpen() >= (MartingaleDis1 + MartingaleDis2 + MartingaleDis3+ MartingaleDis4) *Point())
              {
               if(SCount < MargingaleNum && SCount <4)
                 {
                  SMartingaleVol =NormalizeDouble(SMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Sell(SMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"");
                  SCount++;

                 }
              }


            if(positioning.PositionType() == POSITION_TYPE_SELL && positioning.Profit() < 0 && positioning.PriceCurrent() - positioning.PriceOpen() >= (MartingaleDis1 + MartingaleDis2 + MartingaleDis3+ MartingaleDis4 + MartingaleDis5) *Point())
              {
               if(SCount < MargingaleNum && SCount <5)
                 {
                  SMartingaleVol =NormalizeDouble(SMartingaleVol*MartingaleVolumeMultiplier,2);
                  trading.Sell(SMartingaleVol,Symbol_Name,SymbolInfoDouble(Symbol(),SYMBOL_BID),0,0,"");
                  SCount++;

                 }
              }

           }
        }
     }

  }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|     Close martingale positions when reach the profit             |
//+------------------------------------------------------------------+

void MartingaleClosePos()
  {
   double BTotalProfit = 0;
   double BProfit = 0;
   double SProfit = 0;
   double STotalProfit = 0;

   for(int i = PositionsTotal()-1 ; i>=0; i--)
     {

      if(positioning.SelectByIndex(i))
        {
         if(positioning.Symbol()== Symbol_Name && positioning.PositionType() == POSITION_TYPE_BUY)
           {
            BProfit += positioning.Profit();

           }
        }
     }
   BTotalProfit = BProfit;

   if(BTotalProfit >= MartingaleTakeProfit || BTotalProfit <= MartingaleStopLoss)
     {
      for(int j = PositionsTotal()-1; j>=0; j--)
        {
         if(positioning.SelectByIndex(j))
           {
            if(positioning.Symbol() == Symbol_Name && positioning.PositionType() == POSITION_TYPE_BUY)
              {
               trading.PositionClose(PositionGetTicket(j),5);
               
               
              }
           }
        }

      BMartingaleVol = VOL;
      BCount = 0;
      BTotalProfit = 0;
      BProfit = 0;
     }



// for closing sell positions**************************


   for(int i = PositionsTotal()-1 ; i>=0; i--)
     {
      if(positioning.SelectByIndex(i))
        {
         if(positioning.Symbol()== Symbol_Name && positioning.PositionType() == POSITION_TYPE_SELL)
           {
            SProfit += positioning.Profit();

           }
        }
     }

   STotalProfit = SProfit;

   if(STotalProfit >= MartingaleTakeProfit || STotalProfit <= MartingaleStopLoss)
     {
      for(int j = PositionsTotal()-1; j>=0; j--)
        {
         if(positioning.SelectByIndex(j))
           {
            if(positioning.Symbol() == Symbol_Name && positioning.PositionType() == POSITION_TYPE_SELL)
              {
               trading.PositionClose(PositionGetTicket(j),5);
               
               
              }
           }
        }

      SMartingaleVol = VOL;
      SCount = 0;
      SProfit = 0;
      STotalProfit = 0;
     }



  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+


