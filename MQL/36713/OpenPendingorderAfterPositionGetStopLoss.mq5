//+------------------------------------------------------------------+
//|                     OpenPendingorderAfterPositionGetStopLoss.mq5 |
//|                                                          Tungman |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Tungman"
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Include standard library                                         |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
#include <Trade\PositionInfo.mqh>
#include <Trade\OrderInfo.mqh>
#include <Trade\HistoryOrderInfo.mqh>
#include <Expert\Trailing\TrailingFixedPips.mqh>
#include <Tools\Datetime.mqh>
#include <Trade\SymbolInfo.mqh>

//+------------------------------------------------------------------+
//| Variable standard library                                        |
//+------------------------------------------------------------------+
CTrade               eatrade;
CPositionInfo        eaposition;
COrderInfo           eapending;
CDateTime            eadate;
CTrailingFixedPips   eatrail;
CSymbolInfo          easymbol;

//+------------------------------------------------------------------+
//| Local variable                                                   |
//+------------------------------------------------------------------+
double               MainBuffer[], SignalBuffer[]; // buffer for indicator
int                  ihandle;
string               comment1                = "";
datetime             lastbar_timeopen;

//+------------------------------------------------------------------+
//|Input parameter                                                   |
//+------------------------------------------------------------------+
input group                "****** Price parameters"
input double               PriceVolume             = 0.01; // Lot
input double               PriceSL                 = 1000; // Stop loss (point) (0 = not use)
input double               PriceTP                 = 900; // Take profit (point) (0 = not use)
input int                  trailingstoppoint       = 200; // Trailing stop point (can view by symbol)
input ENUM_TIMEFRAMES      Expirydate              = PERIOD_D1; // Expiry time for pending order
input int                  Step                    = 500; // Distance between position
input double               TargetProfit            = 6;
input int                  RefreshTime             = 60;
input int                  HistorySL               = 7;
input int                  Slippate                = 10;

input group                "****** Stochastic parameters"
input int                  KPeriod                 = 22; // %K Period for STO
input int                  DPeriod                 = 7; // %D Period for STO
input ENUM_STO_PRICE       STOPricemethod          = STO_CLOSECLOSE;
input int                  Slowing                 = 2; // Slowing
input int                  Maxposition             = 1; // Max Position

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   eatrade.SetTypeFillingBySymbol(_Symbol);
   eatrade.SetDeviationInPoints(Slippate);
   eatrade.SetMarginMode();
   long account = AccountInfoInteger(ACCOUNT_LOGIN);
   eatrade.SetExpertMagicNumber(account);

   ResetLastError();

   ihandle                 = iStochastic(_Symbol, PERIOD_CURRENT, KPeriod, DPeriod, Slowing, MODE_EMA, STOPricemethod);


   if(ihandle == INVALID_HANDLE)
     {
      //--- tell about the failure and output the error code
      PrintFormat("Failed to create handle of the iStochastic indicator for the symbol %s/%s, error code %d",
                  _Symbol,
                  EnumToString(_Period),
                  GetLastError());
      //--- the indicator is stopped early
      return(INIT_FAILED);
     }
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//|Check pending order if not exists                                 |
//+------------------------------------------------------------------+
bool CheckPendingOrder(string ordertype, string suffix)
  {
   bool res = false;
// Check existiing pending
   for(int i = OrdersTotal(); i >= 0; i --)
     {
      if(eapending.SelectByIndex(i))
        {
         string PenSymbol        = eapending.Symbol();
         string comment          = eapending.Comment();
         if(PenSymbol == _Symbol && comment == ordertype + _Symbol + EnumToString(PERIOD_CURRENT) + suffix)
           {
            res = true;
            break;
           }
        }
     }
   return res;
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

   if(!TerminalInfoInteger(TERMINAL_CONNECTED))
      return;
   ResetLastError();
   double   CurrentBid        = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double   CurrentAsk        = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double   BuyPrice, SellPrice, SL, TP;
   double   Volume            = NormalizeDouble(PriceVolume, 2);
   datetime curdatetime       = TimeCurrent(eadate);
   datetime expdate           = curdatetime + PeriodSeconds(PERIOD_H4);
   int spred                  = (int)SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
   int stop                   = (int)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
   double stopprice           = NormalizeDouble((stop + spred) * _Point, _Digits);
   string d                   = "";

   bool CheckSellPending      = CheckPendingOrder("S", "S");
   bool CheckBuyPending       = CheckPendingOrder("B", "S");

   int sumsell                = SumPosition("S", "S");
   int sumbuy                 = SumPosition("B", "S");

   bool IsCheckVolume         = CheckVolumeValue(Volume, d);

   bool IsCheckMoneySell      = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_SELL);
   bool IsCheckMoneyBuy       = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_BUY);
   int Backdate               = 1;
   int Barsize                = 200;
   datetime t                 = iTime(_Symbol,PERIOD_CURRENT,0) + PeriodSeconds(PERIOD_M10);
   double NextBuy             = 0;
   double NextSell            = 0;

   ArraySetAsSeries(MainBuffer, true); // Must set before copy buffer
   ArraySetAsSeries(SignalBuffer, true); // Must set before copy buffer
   int countbar               = iBars(_Symbol, PERIOD_CURRENT); // Total bars in chart
   CopyBuffer(ihandle, MAIN_LINE, 0, countbar, MainBuffer); // 0 = Start count, 1 = Value count
   CopyBuffer(ihandle, SIGNAL_LINE, 0, countbar, SignalBuffer); // 0 = Start count, 1 = Value count

   if(!IsCheckMoneySell)
      return;

   if(!IsCheckMoneyBuy)
      return;

   if(MainBuffer[0] < MainBuffer[2]) // Sell
     {
      if(IsCheckVolume && sumsell < Maxposition && !CheckSellPending)
        {
         SellPrice   = NormalizeDouble(CurrentBid - stopprice, _Digits);
         SL          = NormalizeDouble(SellPrice + (PriceSL * _Point), _Digits);
         TP          = NormalizeDouble(SellPrice - (PriceTP * _Point), _Digits);
         //  CheckPositionForClose("B","S");
         if(!eatrade.SellStop(Volume, SellPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "S" + _Symbol + EnumToString(PERIOD_CURRENT) + "S"))
           {
            if(!IsTestMode())
              {
               Print(__FUNCTION__ + " cannot open pending order sell stop at : " + (string)BuyPrice + " SL : " + (string)SL + " TP : " + (string)TP,GetLastError());
              }
           }
        }
     }

   if(MainBuffer[0] > MainBuffer[2]) // Buy
     {
      if(IsCheckVolume && sumbuy < Maxposition && !CheckBuyPending)
        {
         BuyPrice    = NormalizeDouble(CurrentAsk + stopprice, _Digits);
         SL          = NormalizeDouble(BuyPrice - (PriceSL * _Point), _Digits);
         TP          = NormalizeDouble(BuyPrice + (PriceTP * _Point), _Digits);
         // CheckPositionForClose("S","S");
         if(!eatrade.BuyStop(Volume, BuyPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "B" + _Symbol + EnumToString(PERIOD_CURRENT) + "S"))
           {
            if(!IsTestMode())
              {
               Print(__FUNCTION__ + " cannot open pending order buy stop at : " + (string)BuyPrice + " SL : " + (string)SL + " TP : " + (string)TP,GetLastError());
              }
           }
        }
     }

  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
  {
   ENUM_TRADE_TRANSACTION_TYPE   Transtype         = trans.type;
   string Transymbol                               = trans.symbol;
   ENUM_ORDER_TYPE pendingtype                     = trans.order_type;
   ulong pendingticket                             = trans.order; // pending order ticket
   ulong positionticket                            = trans.position; // position ticket
   ENUM_ORDER_STATE Transstate                     = trans.order_state;

// For pending
   ResetLastError();
   double   CurrentBid        = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   double   CurrentAsk        = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   double   BuyPrice, SellPrice, SL, TP;
   double   Volume            = NormalizeDouble(PriceVolume, 2);
   datetime curdatetime       = TimeCurrent(eadate);
   datetime expdate           = curdatetime + PeriodSeconds(Expirydate);
   int spred                  = (int)SymbolInfoInteger(_Symbol, SYMBOL_SPREAD);
   int stop                   = (int)SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL);
   double stopprice           = NormalizeDouble((stop + spred) * _Point, _Digits);
   string d                   = "";
   bool IsCheckVolume         = CheckVolumeValue(Volume, d);

   bool IsCheckMoneySell      = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_SELL);
   bool IsCheckMoneyBuy       = CheckMoneyForTrade(_Symbol,Volume,ORDER_TYPE_BUY);
   int Backdate               = 1;
   int Barsize                = 200;
   datetime t                 = iTime(_Symbol,PERIOD_CURRENT,0) + PeriodSeconds(PERIOD_M10);


   switch(Transtype)
     {
      case TRADE_TRANSACTION_DEAL_ADD: // Try detect stop loss
        {
         if(Transstate == ORDER_STATE_FILLED)
           {
            ulong Dealticket            = trans.deal; // Deal ticket
            if(HistoryDealSelect(Dealticket))
              {
               // Check SL
               ENUM_DEAL_TYPE  dealtype         = (ENUM_DEAL_TYPE)HistoryDealGetInteger(Dealticket,DEAL_TYPE);
               double DealOpenprice             = HistoryDealGetDouble(Dealticket,DEAL_PRICE);
               double DealProfit                = HistoryDealGetDouble(Dealticket,DEAL_PROFIT);
               double DealVolume                = HistoryDealGetDouble(Dealticket,DEAL_VOLUME);
               string DealSymbol                = HistoryDealGetString(Dealticket,DEAL_SYMBOL);
               long reason                      = HistoryDealGetInteger(Dealticket,DEAL_REASON);

               if(reason == DEAL_REASON_SL)
                 {
                  if(DealSymbol == _Symbol)
                    {
                     switch(dealtype)
                       {
                        case DEAL_TYPE_BUY:
                          {
                           if(DealProfit < 0 && trans.price > trans.price_sl && IsCheckVolume && IsCheckMoneySell)
                             {
                              SellPrice   = NormalizeDouble(trans.price - stopprice, _Digits);
                              SL          = NormalizeDouble(SellPrice + (PriceSL * _Point), _Digits);
                              TP          = NormalizeDouble(SellPrice - (PriceTP * _Point), _Digits);
                              //  CheckPositionForClose("B","S");
                              if(eatrade.SellStop(Volume, SellPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "S" + _Symbol + EnumToString(PERIOD_CURRENT) + "S"))
                                {
                                 // ObjectCreate(0,"ARROW SELL",OBJ_ARROW_SELL,0,TimeGMT(),SellPrice);

                                 if(!IsTestMode())
                                   {
                                    Print(_Symbol + " Sell : " + DoubleToString(SellPrice) + " Lot : " + DoubleToString(Volume));
                                   }
                                }
                              else
                                {
                                 if(!IsTestMode())
                                   {
                                    Print(__FUNCTION__ + " cannot open pending order sell stop at : " + (string)BuyPrice + " SL : " + (string)SL + " TP : " + (string)TP,GetLastError());
                                   }
                                }
                             }
                          }
                        break;
                        case DEAL_TYPE_SELL:
                          {
                           if(DealProfit < 0 && trans.price < trans.price_sl && IsCheckVolume && IsCheckMoneyBuy)
                             {
                              BuyPrice    = NormalizeDouble(trans.price + stopprice, _Digits);
                              SL          = NormalizeDouble(BuyPrice - (PriceSL * _Point), _Digits);
                              TP          = NormalizeDouble(BuyPrice + (PriceTP * _Point), _Digits);
                              // CheckPositionForClose("S","S");
                              if(eatrade.BuyStop(Volume, BuyPrice, _Symbol, SL, TP, ORDER_TIME_SPECIFIED, expdate, "B" + _Symbol + EnumToString(PERIOD_CURRENT) + "S"))
                                {
                                 // ObjectCreate(0,"ARROW BUY",OBJ_ARROW_BUY,0,TimeGMT(),BuyPrice);
                                 if(!IsTestMode())
                                   {
                                    Print(_Symbol + " Buy : " + DoubleToString(BuyPrice) + " Lot : " + DoubleToString(Volume));
                                   }
                                }
                              else
                                {
                                 if(!IsTestMode())
                                   {
                                    Print(__FUNCTION__ + " cannot open pending order buy stop at : " + (string)BuyPrice + " SL : " + (string)SL + " TP : " + (string)TP,GetLastError());
                                   }
                                }
                             }
                          }
                        break;
                       }
                    }
                 }

              }
           }

        }

      break;
      case TRADE_TRANSACTION_ORDER_DELETE:
        {
         if(Transymbol == _Symbol && !IsTestMode())
           {
            if(eapending.Select(pendingticket))
              {
               ENUM_ORDER_TYPE type  = eapending.Type();
               Print("Delete pending order " + pendingticket + " type  " + EnumToString(type));
              }

           }
        }
      break;
     }

  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume, string &description)
  {
//--- minimal allowed volume for trade operations
   double min_volume=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN);
   if(volume<min_volume)
     {
      description=StringFormat("Volume is less than the minimal allowed SYMBOL_VOLUME_MIN=%.2f", min_volume);
      return(false);
     }
//--- maximal allowed volume of trade operations
   double max_volume=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MAX);
   if(volume>max_volume)
     {
      description=StringFormat("Volume is greater than the maximal allowed SYMBOL_VOLUME_MAX=%.2f", max_volume);
      return(false);
     }
//--- get minimal step of volume changing
   double volume_step=SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_STEP);
   int ratio=(int)MathRound(volume/volume_step);
   if(MathAbs(ratio*volume_step-volume)>0.0000001)
     {
      description=StringFormat("Volume is not a multiple of the minimal step SYMBOL_VOLUME_STEP=%.2f, the closest correct volume is %.2f",
                               volume_step, ratio*volume_step);
      return(false);
     }
   description="Correct volume value";
   return(true);
  }

//+------------------------------------------------------------------+
//|Check free margin before open pending                             |
//+------------------------------------------------------------------+
bool CheckMoneyForTrade(string symb, double lots, ENUM_ORDER_TYPE type)
  {
   ResetLastError();
//--- Getting the opening price
   MqlTick mqltick;
   SymbolInfoTick(symb, mqltick);
   double price=mqltick.ask;
   if(type==ORDER_TYPE_SELL)
      price=mqltick.bid;
//--- values of the required and free margin
   double margin, free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
//--- call of the checking function
   if(!OrderCalcMargin(type, symb, lots, price, margin))
     {
      if(!IsTestMode())
         //--- something went wrong, report and return false
         Print("Error in ", __FUNCTION__, " code=", GetLastError());
      return(false);
     }
//--- if there are insufficient funds to perform the operation
   if(margin>free_margin)
     {
      //--- report the error and return false
      if(!IsTestMode())
         Print("Not enough money for ", EnumToString(type), " ", lots, " ", symb, " Error code=", GetLastError());
      return(false);
     }
//--- checking successful
   return(true);
  }

//+------------------------------------------------------------------+
//|Check is test mode or not                                         |
//+------------------------------------------------------------------+
bool IsTestMode()
  {
   bool res = true;
   if(!(MQLInfoInteger(MQL_DEBUG) || MQLInfoInteger(MQL_TESTER) || MQLInfoInteger(MQL_VISUAL_MODE) || MQLInfoInteger(MQL_OPTIMIZATION)))
      res = false;

   return res;
  }
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Sum total position                                               |
//+------------------------------------------------------------------+
int SumPosition(string Positiontype, string suffix)
  {
   int count = 0;
   for(int i = PositionsTotal(); i >= 0; i--)
     {
      if(eaposition.SelectByIndex(i))
        {
         string PosSymbol        = eaposition.Symbol();
         string comment          = eaposition.Comment();
         string des              = eaposition.TypeDescription();
         if(Positiontype == "B" && des == "buy" && PosSymbol == _Symbol)
            count += 1;
         if(Positiontype == "S" && des == "sell" && PosSymbol == _Symbol)
            count += 1;
        }
     }
   return count;
  }
//+------------------------------------------------------------------+
