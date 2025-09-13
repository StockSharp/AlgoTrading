//+------------------------------------------------------------------+
//|                                          Basic_Trailing_Stop.mq4 |
//|                                                         Inovance |
//|                                     https://www.inovancetech.com |
//+------------------------------------------------------------------+
#property copyright ""
#property link      "https://www.inovancetech.com"
#property description "Basic Trailing Stop by Inovance"
#property version   "1.00"
#property strict
//--- EA Inputs
extern double   stopLossPips       = 20.0;        // Trailing Stop (pips)
input  double   lots               = 0.1;         // Lots
extern int      slippage           = 5;           // Maximum Slippage (pips)
input  int      magicNumber        = 1443192709;  // Magic Number
//--- Other global variables
double buyTrailingStop=0.0;
double sellTrailingStop = 0.0;
double sellStopLossPips = 0.0;
double buyStopLossPips=0.0;
bool   ordm;
//+------------------------------------------------------------------+
//| Initialization checks                                            |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(Bars<30)
     {
      Print("Error initializing: Not enough bars in chart");
      return INIT_FAILED;
     }
   if(stopLossPips<10)
     {
      Print("Error initializing: Trailing Stop Loss less than 10");
      return INIT_PARAMETERS_INCORRECT;
     }
   if(Digits==5 || Digits==3)
     {
      stopLossPips*=10;
      slippage*=10;
     }
   Print(" Lots=",lots," Stop Loss=",stopLossPips," Slippage=",slippage);
   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------+
//| Main Loop                                                        |
//+------------------------------------------------------------------+
void OnTick()
  {
   setTrailingStop();
   setInitialStop();
//--- Input long and short signals (1 for long, -1 for short)    
   int signal=LongRule()+ShortRule();
   processSignal(signal);

  }
//+------------------------------------------------------------------+
//| Set Trailing Stop                                                |
//+------------------------------------------------------------------+
void setTrailingStop()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(isPositionOpen())
        {
         switch(OrderType())
           {
            case OP_BUY:
               buyTrailingStop=NormalizeDouble(Ask-stopLossPips*Point,Digits);
               if(buyTrailingStop>OrderStopLoss())
                 {
                  ordm=OrderModify(OrderTicket(),OrderOpenPrice(),buyTrailingStop,OrderTakeProfit(),0,CLR_NONE);
                 }
               break;
               //---
            case OP_SELL:
               sellTrailingStop=NormalizeDouble(Bid+stopLossPips*Point,Digits);
               if(sellTrailingStop<OrderStopLoss())
                 {
                  ordm=OrderModify(OrderTicket(),OrderOpenPrice(),sellTrailingStop,OrderTakeProfit(),0,CLR_NONE);
                 }
               break;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Set Initial Stop                                                 |
//+------------------------------------------------------------------+
void setInitialStop()
  {
   buyStopLossPips=NormalizeDouble(Ask-stopLossPips*Point,Digits);
   sellStopLossPips=NormalizeDouble(Bid+stopLossPips*Point,Digits);
  }
//+------------------------------------------------------------------+
//| Check for Open Positions                                         |
//+------------------------------------------------------------------+
bool isPositionOpen()
  {
   for(int i=0; i<OrdersTotal(); i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES) && OrderMagicNumber()==magicNumber && OrderSymbol()==Symbol())
        {
         return true;
        }
     }
   return false;
  }
//+------------------------------------------------------------------+
//| Open trade if conditions met and order not currently open        |
//+------------------------------------------------------------------+
void processSignal(const int signal)
  {
   if(!isPositionOpen())
     {
      if(signal>0)
        {
         Print("Long Conditions Met");
         if(OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,buyStopLossPips,0,"TRAIDE EA",magicNumber,0,Green)<0)
           {
            Print("Error opening buy order: ",GetLastError());
           }
           } else if(signal<0) {
         Print("Short Conditions Met");
         if(OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,sellStopLossPips,0,"TRAIDE EA",magicNumber,0,Red)<0)
           {
            Print("Error opening sell order: ",GetLastError());
           }
        }
     }
  }
//--- Input your trading logic here
int indCCI0period = 14;
int indRSI1period = 14;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int LongRule()
  {
   double CCI0 = iCCI(NULL,0,indCCI0period,PRICE_CLOSE,1);
   double RSI1 = iRSI(NULL,0,indRSI1period,PRICE_CLOSE,1);
   int match=0;
   if(CCI0>-100 && CCI0<=-150) match++;
   if(RSI1>0 && RSI1<=30) match++;
   if(match == 2) return 1;
   return 0;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int ShortRule()
  {
   double CCI0 = iCCI(NULL,0,indCCI0period,PRICE_CLOSE,1);
   double RSI1 = iRSI(NULL,0,indRSI1period,PRICE_CLOSE,1);
   int match=0;
   if(CCI0> 100 && CCI0 <= 250) match++;
   if(RSI1 > 70 && RSI1 <= 100) match++;
   if(match == 2) return -1;
   return 0;
  }
//+------------------------------------------------------------------+
