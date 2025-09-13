//+------------------------------------------------------------------+
//|                                    Exp_breakeven_trailing_SL.mq5 |
//|                                              Copyright 2015, Oxy |
//|                                                  m-viva@inbox.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, Oxy"
#property link      "m-viva@inbox.ru"
#property version   "1.00"

#include <Trade\Trade.mqh> 
CTrade trade;
//+------------------------------------------------------------------+
//| current symbol or all                                            |
//+------------------------------------------------------------------+
enum checkSymb
  {
   CurrentSymbol, // Current Symbol
   AllSymbols,    // All Symbols
  };
//------- input parameters ------------------------------------------+
input checkSymb  WorkSymb               = AllSymbols;                   // work symbol
input long       MagicNumber            = 0;                            // magic: all positions= -1, users= 0, EA >0
input string     name1                  = "________breakeven_______";   // breakeven
input int        PlusPoints_breakeven   = 500;                          // plus points, 0 - without
input int        StepSL_plus_breakeven  = 200;                          // step breakeven, 0 - without
input string     name2                  = "_________trailing_________"; // trailing
input int        PlusPoints_trail       = 300;                          // plus points, 0 - without
input int        StepSL_plus_trail      = 100;                          // trailing step, 0 - without
//------- global variables ------------------------------------------+
string TXT,BTtxt;
bool   Breakeven;
bool   Trailing;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   TXT=""; Breakeven=true; Trailing=true;
//--- check input parameters
   if(PlusPoints_breakeven<=0 || StepSL_plus_breakeven<=0) Breakeven=false;
   if(PlusPoints_breakeven>0 && PlusPoints_breakeven<=StepSL_plus_breakeven) { Alert("Plus points for breakeven must be more step breakeven!"); return(INIT_FAILED);}
   if(PlusPoints_trail<=0 || StepSL_plus_trail<=0) Trailing=false;
   if(PlusPoints_trail>0 && PlusPoints_trail<=StepSL_plus_trail) { Alert("Plus points for trailing must be more trailing step!");   return(INIT_FAILED);}
//--- for text comments
   BTtxt="\n EA not work!";
   if(Breakeven  && !Trailing) BTtxt = "\n Only breakeven!";
   if(!Breakeven && Trailing)  BTtxt = "\n Only trailing!";
   if(Breakeven  && Trailing)  BTtxt = "\n Breakeven and trailing!";
//---
   Comment("\n Waiting a new tick!");
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) { Comment(""); }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   ShowComment();              // comment
   if(Breakeven) fBreakeven(); // breakeven
   if(Trailing)  fTrailing();  // trailing
  }
//+------------------------------------------------------------------+
//| function trailing stop loss                                      |
//+------------------------------------------------------------------+
void fTrailing()
  {
   int _tp=PositionsTotal();
   for(int i=_tp-1; i>=0; i--)
     {
      string _p_symbol=PositionGetSymbol(i);
      if(WorkSymb==CurrentSymbol && _p_symbol!=_Symbol) continue;
      if(MagicNumber>=0 && MagicNumber!=PositionGetInteger(POSITION_MAGIC)) continue;
      double _s_point = SymbolInfoDouble (_p_symbol, SYMBOL_POINT);
      long   _s_levSt = SymbolInfoInteger(_p_symbol, SYMBOL_TRADE_STOPS_LEVEL);
      int    _s_dig   = (int)SymbolInfoInteger(_p_symbol,SYMBOL_DIGITS);
      double _p_sl    = PositionGetDouble(POSITION_SL);
      double _p_tp    = PositionGetDouble(POSITION_TP);
      double _p_op    = PositionGetDouble(POSITION_PRICE_OPEN);
      if(_p_sl==0) _p_sl=_p_op;
      //---
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         if(Breakeven && _p_sl<_p_op+StepSL_plus_breakeven*_s_point) continue;
         if(!Breakeven && _p_sl<_p_op) _p_sl=_p_op;
         double Bid=SymbolInfoDouble(_p_symbol,SYMBOL_BID);
         if(_p_sl+PlusPoints_trail*_s_point<=Bid)
           {
            double _new_sl=Bid-PlusPoints_trail*_s_point+StepSL_plus_trail*_s_point;
            if(Bid-_new_sl<_s_levSt*_s_point) _new_sl=Bid-_s_levSt*_s_point;
            _new_sl=NormalizeDouble(_new_sl,_s_dig);
            if(_new_sl<=_p_sl)continue;
            trade.PositionModify(_p_symbol,_new_sl,_p_tp);
           }
        }
      else
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         if(Breakeven && _p_sl>_p_op-StepSL_plus_breakeven*_s_point) continue;
         if(!Breakeven && _p_sl>_p_op) _p_sl=_p_op;
         double Ask=SymbolInfoDouble(_p_symbol,SYMBOL_ASK);
         if(_p_sl-PlusPoints_trail*_s_point>=Ask)
           {
            double _new_sl=Ask+PlusPoints_trail*_s_point-StepSL_plus_trail*_s_point;
            if(_new_sl-Ask<_s_levSt*_s_point) _new_sl=Ask+_s_levSt*_s_point;
            _new_sl=NormalizeDouble(_new_sl,_s_dig);
            if(_new_sl>=_p_sl)continue;
            trade.PositionModify(_p_symbol,_new_sl,_p_tp);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| function breakeven stop loss                                     |
//+------------------------------------------------------------------+
void fBreakeven()
  {
   int _tp=PositionsTotal();
   for(int i=_tp-1; i>=0; i--)
     {
      string _p_symbol=PositionGetSymbol(i);
      if(WorkSymb==CurrentSymbol && _p_symbol!=_Symbol) continue;
      if(MagicNumber>=0 && MagicNumber!=PositionGetInteger(POSITION_MAGIC)) continue;
      double _s_point = SymbolInfoDouble(_p_symbol, SYMBOL_POINT);
      long   _s_levSt = SymbolInfoInteger(_p_symbol, SYMBOL_TRADE_STOPS_LEVEL);
      int    _s_dig   = (int)SymbolInfoInteger(_p_symbol,SYMBOL_DIGITS);
      double _p_sl    = PositionGetDouble(POSITION_SL);
      double _p_tp    = PositionGetDouble(POSITION_TP);
      double _p_op    = PositionGetDouble(POSITION_PRICE_OPEN);
      if(_p_sl==0) _p_sl=_p_op;
      //---
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         if(_p_sl>=_p_op+StepSL_plus_breakeven*_s_point) continue;
         double Bid=SymbolInfoDouble(_p_symbol,SYMBOL_BID);
         if(_p_op+PlusPoints_breakeven*_s_point<=Bid)
           {
            double _new_sl=_p_op+StepSL_plus_breakeven*_s_point;
            if(Bid-_new_sl<_s_levSt*_s_point) _new_sl=Bid-_s_levSt*_s_point;
            _new_sl=NormalizeDouble(_new_sl,_s_dig);
            if(_new_sl<=_p_sl)continue;
            trade.PositionModify(_p_symbol,_new_sl,_p_tp);
           }
        }
      else
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         if(_p_sl<=_p_op-StepSL_plus_breakeven*_s_point) continue;
         double Ask=SymbolInfoDouble(_p_symbol,SYMBOL_ASK);
         if(_p_op-PlusPoints_breakeven*_s_point>=Ask)
           {
            double _new_sl=_p_op-StepSL_plus_breakeven*_s_point;
            if(_new_sl-Ask<_s_levSt*_s_point) _new_sl=Ask+_s_levSt*_s_point;
            _new_sl=NormalizeDouble(_new_sl,_s_dig);
            if(_new_sl>=_p_sl)continue;
            trade.PositionModify(_p_symbol,_new_sl,_p_tp);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| function text comments                                           |
//+------------------------------------------------------------------+
void ShowComment()
  {
   int    _tp=PositionsTotal(),_num=0;
   string _symb="";
   for(int i=0; i<_tp; i++)
     {
      string _p_symbol=PositionGetSymbol(i);
      if(WorkSymb==CurrentSymbol && _p_symbol!=_Symbol) continue;
      if(MagicNumber>=0 && MagicNumber!=PositionGetInteger(POSITION_MAGIC)) continue;
      _num++;
      _symb+="\n Symbol "+(string)_num+": "+_p_symbol;
     }
   string _txt=BTtxt+"\n\n Positions: "+(string)_num+_symb;
//---
   if(TXT!=_txt)
     {
      TXT=_txt;
      Comment(TXT);
     }
  }
//+------------------------------------------------------------------+
