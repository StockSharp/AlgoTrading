//+------------------------------------------------------------------+
//|                                           Robust_EA_Template.mq4 |
//|                                                         Inovance |
//|                                     https://www.inovancetech.com |
//+------------------------------------------------------------------+
#property copyright "Inovance, Inc."
#property link      "https://www.inovancetech.com"
#property description "TRAIDE Exported Strategy"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| EA Inputs                                                        |
//+------------------------------------------------------------------+
extern double  takeProfit         = 50.0;        // Take Profit (pips)
extern double  stopLoss           = 50.0;        // Stop Loss (pips)
input  double  lots               = 0.1;         // Lots
extern int     slippage           = 5;           // Maximum Slippage (pips)
input  int     magicNumber        = 1443726994;  // Magic Number
//+------------------------------------------------------------------+
//| Initialize Variables                                             |
//+------------------------------------------------------------------+
double buyStopLossPips=0.0;
double buyTakeProfitPips= 0.0;
double sellStopLossPips = 0.0;
double sellTakeProfitPips=0.0;
datetime lastBar=D'1970.01.01 00:00';
//+------------------------------------------------------------------+
//| Initialization Loop                                              |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(Bars<30) // Check for enough bars in chart
     {
      Print("Error initializing: Not enough bars in chart");
      return INIT_FAILED;
     }

   if(takeProfit<2) // Check Take Profit
     {
      Print("Error initializing: Take Profit less than 10");
      return INIT_PARAMETERS_INCORRECT;
     }

   if(stopLoss<2) // Check Stop Loss
     {
      Print("Error initializing: Stop Loss less than 10");
      return INIT_PARAMETERS_INCORRECT;
     }

   if(Digits==5 || Digits==3) // Adjust for 3- or 5-digit Brokers
     {
      takeProfit*=10;
      stopLoss *= 10;
      slippage *= 10;
     }

   Print(" Lots=",lots," Take Profit=",takeProfit," Stop Loss=",stopLoss," Slippage=",slippage);
   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------+
//| Main Loop                                                        |
//+------------------------------------------------------------------+
void OnTick()
  {
// Calculate Buy or Sell Signal
   int signal=LongSignal()+ShortSignal();
   processSignal(signal);
  }
//+------------------------------------------------------------------+
//| Set Take Profit and Stop Loss                                    |
//+------------------------------------------------------------------+
void setTakeProfitAndStopLoss()
  {
   buyTakeProfitPips=NormalizeDouble(Ask+takeProfit*Point,Digits);
   buyStopLossPips=NormalizeDouble(Ask-stopLoss*Point,Digits);
   sellTakeProfitPips=NormalizeDouble(Bid-takeProfit*Point,Digits);
   sellStopLossPips=NormalizeDouble(Bid+stopLoss*Point,Digits);
  }
//+------------------------------------------------------------------+
//| Check for open position and entry signal                         |
//+------------------------------------------------------------------+
void processSignal(const int signal)
  {
   setTakeProfitAndStopLoss(); // Set Take Profit and Stop Loss

   if(isPositionOpen()) // Check for open position
     {
      processSignalHasPositionOpen(signal);
     }
   else // If no open position
     {
      processSignalNoPositionOpen(signal);
     }
  }
//+------------------------------------------------------------------+
//| Check for currently open position                                |
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
//| Entry process if position already open                           |
//+------------------------------------------------------------------+
void processSignalHasPositionOpen(const int signal)
  {
   switch(OrderType())
     {
      case OP_BUY:   // A buy order exists
         if(signal<0)
           {
            Print("Short Conditions Met");
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,slippage,Violet))
              {
               Print("Error closing buy order: ",GetLastError());
                 } else if(OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,sellStopLossPips,sellTakeProfitPips,"TRAIDE EA",magicNumber,0,Red)<0) {
               Print("Error opening sell order: ",GetLastError());
              }
              } else if(signal>0) {
            Print("Long Conditions Still Met");
           }
         break;
      case OP_SELL:  // A sell order exists
         if(signal>0)
           {
            Print("Long Conditions Met");
            if(!OrderClose(OrderTicket(),OrderLots(),Ask,slippage,Violet))
              {
               Print("Error closing sell order: ",GetLastError());
                 } else if(OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,buyStopLossPips,buyTakeProfitPips,"TRAIDE EA",magicNumber,0,Green)<0) {
               Print("Error opening buy order: ",GetLastError());
              }
              } else if(signal<0) {
            Print("Short Conditions Still Met");
           }
         break;
      default:
         Print("Error, unexpected OrderType: ",OrderType());
         break;
     }
  }
//+------------------------------------------------------------------+
//| Entry process if no position currently open                      |
//+------------------------------------------------------------------+
void processSignalNoPositionOpen(const int signal)
  {
   if(signal>0)
     {
      Print("Long Conditions Met");
      if(OrderSend(Symbol(),OP_BUY,lots,Ask,slippage,buyStopLossPips,buyTakeProfitPips,"TRAIDE EA",magicNumber,0,Green)<0)
        {
         Print("Error opening buy order: ",GetLastError());
        }
        } else if(signal<0) {
      Print("Short Conditions Met");
      if(OrderSend(Symbol(),OP_SELL,lots,Bid,slippage,sellStopLossPips,sellTakeProfitPips,"TRAIDE EA",magicNumber,0,Red)<0)
        {
         Print("Error opening sell order: ",GetLastError());
        }
     }
  }
//+------------------------------------------------------------------+
//| Erros leading to Deinitializtion                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   switch(reason)
     {
      case REASON_PROGRAM:
         Print("Closing EA: Expert Advisor terminated its operation by calling the ExpertRemove() function (REASON_PROGRAM).");
         closeEA();
         break;
      case REASON_REMOVE:
         Print("Closing EA: Program has been deleted from the chart (REASON_REMOVE).");
         closeEA();
         break;
      case REASON_RECOMPILE:
         Print("Reinitializing EA: Program has been recompiled (REASON_RECOMPILE).");
         reinitEA();
         break;
      case REASON_CHARTCHANGE:
         Print("Reinitializing EA: Symbol or chart period has been changed (REASON_CHARTCHANGE).");
         reinitEA();
         break;
      case REASON_CHARTCLOSE:
         Print("Closing: Chart has been closed (REASON_CHARTCLOSE).");
         closeEA();
         break;
      case REASON_PARAMETERS:
         Print("Reinitializing EA: Input parameters have been changed by user (REASON_PARAMETERS).");
         reinitEA();
         break;
      case REASON_ACCOUNT:
         Print("Reinitializing EA: Another account has been activated or reconnection to the trade server has occurred due to changes in the account settings (REASON_ACCOUNT).");
         reinitEA();
         break;
      case REASON_TEMPLATE:
         Print("Reinitializing EA: A new template has been applied (REASON_TEMPLATE).");
         reinitEA();
         break;
      case REASON_INITFAILED:
         Print("Closing EA: OnInit() handler has returned a nonzero value.");
         closeEA();
         break;
      case REASON_CLOSE:
         Print("Closing EA: Terminal has been closed.");
         closeEA();
         break;
      default:
         Print("Error, unexpected OnDeInit reason: ",reason);
         break;
     }
  }
//+------------------------------------------------------------------+
//| Close position process                                           |
//+------------------------------------------------------------------+
void closeEA()
  {
   if(isPositionOpen())
     {
      switch(OrderType())
        {
         case OP_BUY:   // A buy order exists
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,slippage,Violet))
              {
               Print("Error closing buy order: ",GetLastError());
              }
            break;
         case OP_SELL:  // A sell order exists
            if(!OrderClose(OrderTicket(),OrderLots(),Ask,slippage,Violet))
              {
               Print("Error closing sell order: ",GetLastError());
              }
            break;
         default:
            Print("Error closeEA, unexpected OrderType: ",OrderType());
            break;
        }
     }
  }
//+------------------------------------------------------------------+
//| Reinitialize in case of error                                    |
//+------------------------------------------------------------------+
void reinitEA()
  {
   reinitGlobals();
   if(OnInit()!=INIT_SUCCEEDED)
     {
      Print("Closing EA: Reinitializing failed.");
      closeEA();
     }
  }
//+------------------------------------------------------------------+
//| Reinitialize global variables                                    |
//+------------------------------------------------------------------+
void reinitGlobals()
  {
   buyStopLossPips=0.0;
   buyTakeProfitPips= 0.0;
   sellStopLossPips = 0.0;
   sellTakeProfitPips=0.0;
   lastBar=D'1970.01.01 00:00';
  }
//+------------------------------------------------------------------+
//| Long and Short Entry Conditions                                  |
//+------------------------------------------------------------------+
int indCCI0period = 14; // Indicator 1 period
int indRSI1period = 14; // Indicator 2 period
//+------------------------------------------------------------------+
//| Long Entry(Return "1" for long entry, "0" for no entry)          |
//+------------------------------------------------------------------+
int LongSignal()
  {
   double CCI0 = iCCI(NULL,0,indCCI0period,PRICE_CLOSE,1);
   double RSI1 = iRSI(NULL,0,indRSI1period,PRICE_CLOSE,1);
   int match=0;
   if(CCI0>-200 && CCI0<=-150) match++;
   else if(CCI0>-100 && CCI0<=-50) match++;
   if(RSI1>0 && RSI1<=25) match++;
   if(match == 2) return 1;
   return 0;
  }
//+------------------------------------------------------------------+
//| Short Entry(Return "-1" for long entry, "0" for no entry)        |
//+------------------------------------------------------------------+
int ShortSignal()
  {
   double CCI0 = iCCI(NULL,0,indCCI0period,PRICE_CLOSE,1);
   double RSI1 = iRSI(NULL,0,indRSI1period,PRICE_CLOSE,1);
   int match=0;
   if(CCI0 > 50 && CCI0 <= 150) match++;
   if(RSI1 > 80 && RSI1 <= 100) match++;
   if(match == 2) return -1;
   return 0;
  }
//+------------------------------------------------------------------+
