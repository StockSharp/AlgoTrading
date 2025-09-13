//+------------------------------------------------------------------+
//|                                                   ASCTrendND.mq5 |
//|                                                   Alain Verleyen |
//|                                             http://www.alamga.be |
//+------------------------------------------------------------------+
/*
    Trading strategy based on ASCTrend indicator as main signal,
    filtered by NRTR indicator (see    http://www.mql5.com/en/forum/10741)

    * Only for current symbol and timeframe
    * Stoploss based on ASCTrend signal,
    * No takeprofit, exit based on trailing stop
    * Money management NOT YET IMPLEMENTED (only fixed volume)
    * Very basic error management    
    
   1.01 Correction of littles bugs.
   1.02 Add of TrendStrength as filter
   1.03 Cosmetic change for Codebase publication
*/
#property copyright     "Alain Verleyen"
#property link          "http://www.mql5.com/en/forum/10741"
#property version       "1.03"
#property description   "Trading strategy based on ASCTrend indicator as main signal."
#property description   "Filtered by NRTR indicator and/or by TrendStrength indicator."
#property description   "See this topic for more information http://www.mql5.com/en/forum/10741"

#include <Trade\Trade.mqh>

#define EXPERT_NAME         MQL5InfoString(MQL5_PROGRAM_NAME)
#define PIP                 ((_Digits <= 3) ? 0.01 : 0.0001)

input   ulong               Magic               = 1928676;              // Magic Number
input   double              Slippage            = 3.0;                  // Slippage in pips

input   string              trailSettings       = " Trailing stop settings: ";   
input   double              trailValue          = 80.0;                 // Trailing Stop in pips
        double              BreakEven           = 0.0;                  // Breakeven in pips    NOT TESTED YET
        double              ProfitLock          = 0.0;                  // ProfitLock in pips   NOT TESTED YET

input   string              ASC_Settings        = " Asctrend Main Signal settings: ";
input   string              ascIndicatorName    = "Asctrend";           // Asctrend indicator's name
        ENUM_TIMEFRAMES     ascTimeFrame        = PERIOD_CURRENT;       // Time Frame for Asctrend
input   int                 ascRisk             = 3;                    // Asctrend risk 
input   bool                ascUseAsStoploss    = true;                 // Use Asctrend signal value as stoploss

input   string              NRTR_Settings       = " NRTR Filter settings: ";
input   string              nrtrIndicatorName   = "NRTR_color_line";    // NRTR indicator's name
        ENUM_TIMEFRAMES     nrtrTimeFrame       = PERIOD_CURRENT;       // Time Frame
input   int                 nrtrATRPeriod       = 14;                   // ATR period
input   double              nrtrCoefficient     = 4.0;                  // Coefficient
        int                 nrtrSignalMode      = 1;                    // SignalMode: Display signals mode: 0-only Stops,1-Signals & Stops,2-only Signals;
input   bool                nrtrEnabled         = true;                 // Use NRTR to filter main signal

input   string              TS_Settings         = " TrendStrength Filter settings: ";
input   string              tsIndicatorName     = "TrendStrength_v2";   // TrendStrength indicator's name
        ENUM_TIMEFRAMES     tsTimeFrame         = PERIOD_CURRENT;       // Time Frame
input   ENUM_APPLIED_PRICE  tsPrice             = PRICE_CLOSE;          // Applied Price
input   int                 tsRSILength         = 14;                   // RSI Period
input   int                 tsSmooth            = 5;                    // Smoothing Period
input   double              tsK                 = 4.236;                // Multiplier
input   bool                tsEnabled           = true;                 // Use TrendStrength to filter main signal

input   string              lotsSettings        = " MoneyManagement: ";
        int                 lotsMode            = 0;                    // Money Management : 0 - Off, 1 - By Free Margin, 2 - By StopLoss...
input   double              lotsFixed           = 0.1;                  // Lot size (No money management)
        double              lotsRisk            = 0;                    // Risk factor (in %) for lots, calculated depending on the chosen risk 

input   bool                debugMode           = true;                 // Debugging Mode: false - Off, true - On

// Pseudo constants
double  _PIP_;

// Globals variables
CTrade  trade;

int     ascHandle,
        nrtrHandle,
        tsHandle;

double  trailingStop,
        stopLoss,
        stopLevel,
        freezeLevel,
        lotMin,
        lotMax,
        lotStep,
        lots,
        ascSellSignal[],
        ascBuySignal[],
        nrtrUpTrend[],
        nrtrDownTrend[],
        tsUpTrend[],
        tsDownTrend[];

ENUM_SYMBOL_TRADE_EXECUTION execution;

MqlTick tick;

enum OPERATION_TYPE
{   
    OP_BUY,
    OP_SELL
};

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
    _PIP_           = (_Digits <= 3) ? 0.01 : 0.0001;
    stopLevel       = SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL) * _Point;
    freezeLevel     = SymbolInfoInteger(_Symbol, SYMBOL_TRADE_FREEZE_LEVEL) * _Point;
    if (debugMode) Print("Stop level : ", stopLevel, " Freeze level : ", freezeLevel);    
    lotStep         = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
    lotMin          = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
    lotMax          = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
    lots            = NormalizeLots(lotsFixed);
    execution       = (ENUM_SYMBOL_TRADE_EXECUTION) SymbolInfoInteger(_Symbol, SYMBOL_TRADE_EXEMODE);
    trailingStop    = PipToPoint(trailValue) * _Point;

    // Trade context initialization
    trade.SetExpertMagicNumber(Magic);                                  // Set MagicNumber for your orders identification
    trade.SetDeviationInPoints(PipToPoint(Slippage));                   // Set available slippage in points when buying/selling
    trade.SetTypeFilling(ORDER_FILLING_RETURN);                         // Order filling mode, the mode allowed by the server should be used
    trade.SetAsyncMode(true);                                           // Use : true - OrderSendAsync(), false - OrderSend()

    // Asctrend indicator initialization
    ascHandle = iCustom(NULL, ascTimeFrame, ascIndicatorName, ascRisk);   
    if (ascHandle == INVALID_HANDLE) {
        Print("Error in loading of ", ascIndicatorName, " indicator. LastError = ", GetLastError());
        return(-1);
    } 
  
    ChartIndicatorAdd(ChartID(), 0, ascHandle); 
    ArraySetAsSeries(ascSellSignal, true);  
    ArraySetAsSeries(ascBuySignal, true);

    // NRTR indicator initialization
    if (nrtrEnabled) {
        nrtrHandle  = iCustom(NULL, nrtrTimeFrame, nrtrIndicatorName, nrtrATRPeriod, nrtrCoefficient, nrtrSignalMode);
        if (nrtrHandle == INVALID_HANDLE) {
            Print("Error in loading of ", nrtrIndicatorName, " indicator. LastError = ", GetLastError());
            return(-1);
        } 
        ChartIndicatorAdd(ChartID(), 0, nrtrHandle); 
        ArraySetAsSeries(nrtrUpTrend, true);  
        ArraySetAsSeries(nrtrDownTrend, true);
    }    


    // TrendStrength indicator initialization
    if (tsEnabled) {
        tsHandle  = iCustom(NULL, tsTimeFrame, tsIndicatorName, tsPrice, tsRSILength, tsSmooth, tsK);
        if (tsHandle == INVALID_HANDLE) {
            Print("Error in loading of ", tsIndicatorName, " indicator. LastError = ", GetLastError());
            return(-1);
        } 
        ChartIndicatorAdd(ChartID(), 1, tsHandle); 
        ArraySetAsSeries(tsUpTrend, true);  
        ArraySetAsSeries(tsDownTrend, true);
    }    
    
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
    IndicatorRelease(ascHandle);
//    IndicatorRelease(nrtrHandle);
//    IndicatorRelease(tsHandle);

    ArrayFree(ascSellSignal); 
    ArrayFree(ascBuySignal); 
    ArrayFree(nrtrUpTrend); 
    ArrayFree(nrtrDownTrend); 
    ArrayFree(tsUpTrend); 
    ArrayFree(tsDownTrend); 
      
    Comment("");   
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

    if(!SymbolInfoTick(_Symbol, tick)) return;

    bool _positionExist = CheckPositionExist();

    if (_positionExist) CheckTrailingStop();
        
    if (!_positionExist && IsNewBar()) {
        OPERATION_TYPE  _optype;
        stopLoss = 0.0;
        // CheckSignal
        if (CheckTradeSignal(_optype)) {
            SendMarketOrder(_optype, lots);
        }
    }

   
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
//---
   
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
{
}   

//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
//---
   
  }
//+------------------------------------------------------------------+

//+-------------------------------------------------------------------+
//| Function to detect a new bar on current symbol, current timeframe |
//+-------------------------------------------------------------------+
bool IsNewBar(bool __firstIsNew=false)
{
    static datetime _LastBarOpenTime;

    bool _isBarsChanged = false;
    datetime _barOpenTime[];

    if (CopyTime(NULL, PERIOD_CURRENT, 0, 1, _barOpenTime)) {
        // During first call
        if (__firstIsNew == false && _LastBarOpenTime == 0) 
            _LastBarOpenTime    = _barOpenTime[0];

        else if (_barOpenTime[0] != _LastBarOpenTime) {
            _isBarsChanged  = true;
            _LastBarOpenTime    = _barOpenTime[0];
        }
   }   
   
   return(_isBarsChanged);
}

//+------------------------------------------------------------------+
//| Like iTime for MT4                                               |
//+------------------------------------------------------------------+
datetime iTime(string __symbol, ENUM_TIMEFRAMES __timeframe, int __shift)
{
    if(__shift < 0) return(-1);

    datetime _itime[];

    if(CopyTime(__symbol, __timeframe, __shift, 1, _itime) != -1)
        return(_itime[0]);
    else 
        return(-1);
}

//+------------------------------------------------------------------+
//| Function to check a trade signal on last closed bar              |
//|     Return true if a signal exist                                |
//|            and set __type to BUY or SELL                         |
//+------------------------------------------------------------------+
bool CheckAsctrendSignal(OPERATION_TYPE &__type)
{  
    if (CopyBuffer(ascHandle, 0, 1, 1, ascSellSignal) != -1 &&  
        ascSellSignal[0] > 0 && ascSellSignal[0]!= EMPTY_VALUE) {

        if (debugMode) Print("Asctrend sell signal");       
        if (ascUseAsStoploss) stopLoss = ascSellSignal[0];
        __type = OP_SELL; 
        return(true); 
    }
   
    if (CopyBuffer(ascHandle, 1, 1, 1, ascBuySignal) != -1 &&
        ascBuySignal[0] > 0 && ascBuySignal[0]!= EMPTY_VALUE) {

        if (debugMode) Print("Asctrend buy signal");       
        if (ascUseAsStoploss) stopLoss = ascBuySignal[0];
        __type = OP_BUY; 
        return(true); 
    }
    
    return(false);  
}

//+------------------------------------------------------------------+
//| Function to check if main signal is valid (according to nrtr)    |
//|     Return true if trade signal is validated                     |
//+------------------------------------------------------------------+
bool CheckNRTRFilter(OPERATION_TYPE __type)
{   
    bool _signal = false;
    
    switch (__type)
    {
        case OP_BUY:
            if (CopyBuffer(nrtrHandle, 0, 1, 1, nrtrUpTrend) != -1 && 
                nrtrUpTrend[0] > 0 && nrtrUpTrend[0]!= EMPTY_VALUE) {
                
                if (debugMode) Print("Asctrend buy signal confirmation by NRTR");
                _signal = true;
            }
  
            break;
        case OP_SELL:
            if (CopyBuffer(nrtrHandle, 1, 1, 1, nrtrDownTrend) != -1 &&
                nrtrDownTrend[0] > 0 && nrtrDownTrend[0]!= EMPTY_VALUE) {

                if (debugMode) Print("Asctrend sell signal confirmation by NRTR");
                _signal = true;
            } 
            break;
    }
    
   return(_signal);  
}

//+------------------------------------------------------------------+
//| Function to check if main signal is valid (acc.to TrendStrength) |
//|     Return true if trade signal is validated                     |
//+------------------------------------------------------------------+
bool CheckTrendStrengthFilter(OPERATION_TYPE __type)
{   
    bool _signal = false;
    
    switch (__type)
    {
        case OP_BUY:
            if (CopyBuffer(tsHandle, 1, 1, 1, tsUpTrend) != -1 && 
                tsUpTrend[0] > 0 && tsUpTrend[0]!= EMPTY_VALUE) {
                
                if (debugMode) Print("Asctrend buy signal confirmation by TrendStrength");
                _signal = true;
            }
  
            break;
        case OP_SELL:
            if (CopyBuffer(tsHandle, 2, 1, 1, tsDownTrend) != -1 &&
                tsDownTrend[0] > 0 && tsDownTrend[0]!= EMPTY_VALUE) {

                if (debugMode) Print("Asctrend sell signal confirmation by TrendStrength");
                _signal = true;
            } 
            break;
    }
    
   return(_signal);  
}

//+------------------------------------------------------------------+
//| Check existing position on current symbol                        |
//+------------------------------------------------------------------+
bool CheckPositionExist()
{
    bool _exist = false;
    
    if (PositionSelect(_Symbol) && PositionGetInteger(POSITION_MAGIC) == Magic) 
        _exist = true;
        
    return(_exist); 
}

//+------------------------------------------------------------------+
//| Check if a valid trade signal exist                              |
//|     Return true and set __type to BUY or SELL                    |
//+------------------------------------------------------------------+
bool CheckTradeSignal(OPERATION_TYPE &__type)
{   
    bool _signal;
   
    _signal = CheckAsctrendSignal(__type);
   
    if(_signal && nrtrEnabled)
        _signal = CheckNRTRFilter(__type);

    if(_signal && tsEnabled)
        _signal = CheckTrendStrengthFilter(__type);
      
   return(_signal);
}

//+------------------------------------------------------------------+
//| Send market order of __type (BUY or SELL) with __lot volume      |
//| Stoploss is fixed globally (see CheckAscrendSignal               |
//+------------------------------------------------------------------+
void SendMarketOrder(OPERATION_TYPE __type, double __lot)
{
//--- 
    double _volume          = __lot; 
    double _price           = (__type == OP_BUY) ? tick.ask : tick.bid; 
    string _operation       = (__type == OP_BUY) ? " buy " : " sell ";
    ENUM_ORDER_TYPE _opType = (__type == OP_BUY) ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
    string _comment         = EXPERT_NAME + _operation; 
    double _sl,
           _tp = NormalizeDouble(0.0, _Digits);
   
    if (stopLoss != 0.0 ) 
    {
        _sl = NormalizeDouble(stopLoss, _Digits);
        if (debugMode) Print("StopLoss : ", _sl, " Price : ", _price, " Stoplevel : ", stopLevel);
        if (__type == OP_BUY)
            _sl = MathMin(_sl, _price - stopLevel); 
        else
            _sl = MathMax(_sl, _price + stopLevel); 
        if (debugMode) Print(" with stoplevel : ", _price + (__type == OP_BUY ? -1 : 1) * stopLevel);
    }
    else 
        _sl = 0.0;
    
    if (debugMode) Print(" SL : ", _sl);  

    if (!trade.PositionOpen(_Symbol, _opType, _volume, _price, _sl, _tp, _comment)) {
        if (debugMode) Print("Open", _operation, " failed. Return code=", trade.ResultRetcode(), ". Code description: ", trade.ResultRetcodeDescription());
    }
    else {
        if (debugMode) Print("Open", _operation, " executed successfully. Return code=", trade.ResultRetcode(), " (", trade.ResultRetcodeDescription(),")");
        if (execution == SYMBOL_TRADE_EXECUTION_MARKET) {
            if (!trade.PositionModify(_Symbol, _sl, _tp))
                if (debugMode) Print("Modify", _operation, " failed. Return code=", trade.ResultRetcode(), ". Code description: ", trade.ResultRetcodeDescription());
            else
                if (debugMode) Print("Modify", _operation, " executed successfully. Return code=", trade.ResultRetcode(), " (", trade.ResultRetcodeDescription(),")");
        }
    }
}

//+------------------------------------------------------------------+
//| Normalite lot size to fit with lot step, min & max               |
//+------------------------------------------------------------------+
double NormalizeLots(double __lots)
{   
	int _lotsteps	= (int)(__lots / lotStep);
	double _Nlots   = _lotsteps * lotStep;

    if (_Nlots < lotMin) _Nlots = lotMin;
	if (_Nlots > lotMax) _Nlots = lotMax;
	
	return(_Nlots);
}

//+------------------------------------------------------------------+
//| Convert a pip (0.0001 or 0.01) to point for current symbol       |
//+------------------------------------------------------------------+
ulong PipToPoint(double __val)
{
   ulong _ptp = (ulong) MathRound(__val * (int)MathPow(10, _Digits % 2)); 
   return(_ptp);
}

//+------------------------------------------------------------------+
//| Modify stoploss to ...                                           |
//+------------------------------------------------------------------+
void CheckTrailingStop()
{
    if (PositionSelect(_Symbol) && PositionGetInteger(POSITION_MAGIC) == Magic) { 
   
        ENUM_POSITION_TYPE _type    = (ENUM_POSITION_TYPE) PositionGetInteger(POSITION_TYPE);
        int     _sign               = (_type == POSITION_TYPE_BUY) ? 1 : -1;
        double  _openPrice          = PositionGetDouble(POSITION_PRICE_OPEN),
                _oldStopLoss        = PositionGetDouble(POSITION_SL),
                _tp                 = PositionGetDouble(POSITION_TP),
                _newStopLoss        = 0.0;
        
		double  _marketPrice        = (_type == POSITION_TYPE_BUY) ? tick.bid : tick.ask;
        
        if (BreakEven > 0 && NormalizeDouble(_sign * (_oldStopLoss - _openPrice), _Digits) < NormalizeDouble(ProfitLock * _Point, _Digits)) {
			double  _profit         = NormalizeDouble(PositionGetDouble(POSITION_PROFIT), _Digits);
			if (_profit >= BreakEven) 
			        _newStopLoss    = NormalizeDouble(_openPrice + _sign * ProfitLock * _Point, _Digits);
        }
        else if (trailingStop > 0.0) {
            _newStopLoss            = NormalizeDouble(_marketPrice - _sign * trailingStop, _Digits);
        }
		else 
		    _newStopLoss            = 0.0; 			   
			
// ?        if (_newStopLoss <= 0.0) return; 
			   
        if (_sign * (_marketPrice - _newStopLoss) < stopLevel) _newStopLoss = NormalizeDouble(_marketPrice - _sign * stopLevel, _Digits);
              
//        if (debugMode) Print("Openprice = ", _openPrice, " Trailing stop : New stop loss = ", _newStopLoss);
        if (_sign * (NormalizeDouble(_openPrice, _Digits) - _newStopLoss) <= 0.0) {   
            if (debugMode) Print("Trailing stop : New stop loss = ", _newStopLoss);

            if (_sign * (_newStopLoss - NormalizeDouble(_oldStopLoss, _Digits)) > 0.0 || _oldStopLoss == 0.0) {

                if (!trade.PositionModify(_Symbol, _newStopLoss, _tp)) {

                    Print("PositionModify() failed. Return code=", trade.ResultRetcode(),
                          ". Code description: ", trade.ResultRetcodeDescription());
                    Print(": sl=", _oldStopLoss, " tp=", _tp);
                }
                else {
                    if (debugMode) Print("PositionModify() executed successfully. Return code=", trade.ResultRetcode(),
                          " (", trade.ResultRetcodeDescription(),")");
                }
            }
        }
    }            
}
