//+------------------------------------------------------------------+
//|                                                EA_MARSI_1-02.mq5 |
//|                                Copyright © 2012.08.22, Alexander |
//|                        https://login.mql5.com/en/users/Im_hungry |
//|ICQ: 609928564 | email: I-m-hungree@yandex.ru | skype:i_m_hungree |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012"
#property link      "EA_MARSI_1-02"
//---
input  string              section_1         =                    "========  Trade options";
input  double              Lots              = 0.1;                // Lots
input  double              TP                = 0;                  // TP
input  double              SL                = 0;                  // SL
input  int                 Slippage          = 65;                 // Slippage
input  int                 magic             = 303300;             // magic
input  int                 N_modify_sltp     = 7;                  // N_modify_sltp
//---
input  string              section_2         =                     "========  Multpl";
input  bool                use_Multpl        = false;              // use_Multpl
input  int                 Max_drawdown      = 10000;              // Max_drawdown
//---
input  string              section_3         =                    "========  slow EMA_RSI_VA";
input  int                 slow_RSIPeriod    = 310;                // slow_RSIPeriod
input  int                 slow_EMAPeriods   = 40;                 // slow_EMAPeriods
input  ENUM_APPLIED_PRICE  slow_Price        = PRICE_CLOSE;        // slow_Price
//---
input  string              section_4         =                    "========  fast EMA_RSI_VA";
input  int                 fast_RSIPeriod    = 200;                // fast_RSIPeriod
input  int                 fast_EMAPeriods   = 50;                 // fast_EMAPeriods
input  ENUM_APPLIED_PRICE  fast_Price        = PRICE_CLOSE;        // fast_Price
//+------------------------------------------------------------------+
//| global parameters                                                |
//+------------------------------------------------------------------+
double slow_EMA_RSI_VA[2],fast_EMA_RSI_VA[2];
int handle_slow_EMA_RSI,handle_fast_EMA_RSI; datetime _time;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   handle_slow_EMA_RSI = iCustom(Symbol(),NULL,"EMA_RSI_VA",slow_RSIPeriod,slow_EMAPeriods,slow_Price);
   handle_fast_EMA_RSI = iCustom(Symbol(),NULL,"EMA_RSI_VA",fast_RSIPeriod,fast_EMAPeriods,fast_Price);
   if(handle_slow_EMA_RSI==INVALID_HANDLE || handle_fast_EMA_RSI==INVALID_HANDLE)
     {
      Print("Error in loading of EMA_RSI_VA. GetLastError() = ",GetLastError());
      return(-1);
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   IndicatorRelease(handle_slow_EMA_RSI);
   IndicatorRelease(handle_fast_EMA_RSI);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   datetime Now_time[1];
   int coptTime=CopyTime(Symbol(),NULL,0,1,Now_time);
   if(Now_time[0]>_time && coptTime!=-1)
     {
      _time=Now_time[0];
      ArraySetAsSeries(slow_EMA_RSI_VA,true);
      ArraySetAsSeries(fast_EMA_RSI_VA,true);
      int cop1 = CopyBuffer(handle_slow_EMA_RSI,0,1,2,slow_EMA_RSI_VA);
      int cop2 = CopyBuffer(handle_fast_EMA_RSI,0,1,2,fast_EMA_RSI_VA);
      if(cop1<=0 || cop2<=0) return;
      if(!PositionSelect(Symbol()))
        {
         if(slow_EMA_RSI_VA[1]<fast_EMA_RSI_VA[1] && slow_EMA_RSI_VA[0]>=fast_EMA_RSI_VA[0]) open(1);
         if(slow_EMA_RSI_VA[1]>fast_EMA_RSI_VA[1] && slow_EMA_RSI_VA[0]<=fast_EMA_RSI_VA[0]) open(0);
        }
      else
        {
         int type=(int)PositionGetInteger(POSITION_TYPE);
         if(type==0 && slow_EMA_RSI_VA[1]<fast_EMA_RSI_VA[1] && slow_EMA_RSI_VA[0]>=fast_EMA_RSI_VA[0])
            if(close()) open(1);
         if(type==1 && slow_EMA_RSI_VA[1]>fast_EMA_RSI_VA[1] && slow_EMA_RSI_VA[0]<=fast_EMA_RSI_VA[0])
            if(close()) open(0);
        }
     }
  }
//+------------------------------------------------------------------+
//| Open trade function                                              |
//+------------------------------------------------------------------+
void open(int type)
  {
   double pr_open=0.0,_sl=0.0,_tp=0.0,_nLt=Lots;
   if(use_Multpl)
     {
      _nLt=NormalizeDouble((Lots*AccountInfoDouble(ACCOUNT_BALANCE)/Max_drawdown),2);
      if(SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX)<_nLt)
        {
         _nLt=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
         Alert("Exceed the maximum Lot Volume. Will be installed the limit: ",_nLt);
        }
     }
   MqlTradeRequest mrequest;MqlTradeResult mresult;
   ZeroMemory(mrequest);
   mrequest.action    = TRADE_ACTION_DEAL;
   mrequest.symbol    = Symbol();
   mrequest.volume    = _nLt;
   mrequest.magic     = magic;
   mrequest.deviation = Slippage;
   if(type==0)
     {
      mrequest.type=ORDER_TYPE_BUY;
      mrequest.price     = SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      if(SL>0) _sl=mrequest.price-(SL*Point());
      if(TP>0) _tp=mrequest.price+(TP*Point());
     }
   if(type==1)
     {
      mrequest.type=ORDER_TYPE_SELL;
      mrequest.price     = SymbolInfoDouble(Symbol(),SYMBOL_BID);
      if(SL>0) _sl=mrequest.price+(SL*Point());
      if(TP>0) _tp=mrequest.price-(TP*Point());
     }
//---
   if(!OrderSend(mrequest,mresult))
      Print("error Opened order    __FUNCTION__",__FUNCTION__,": ",mresult.comment," answer code ",mresult.retcode);
   else
     {
      if(SL>0 || TP>0)
        {
         for(int k=0; k<=N_modify_sltp; k++)
           {
            MqlTradeRequest         prequest;
            MqlTradeResult          presult;
            ZeroMemory(prequest);
            prequest.action       = TRADE_ACTION_SLTP;
            prequest.symbol       = Symbol();
            prequest.sl           = NormalizeDouble(_sl,Digits());
            prequest.tp           = NormalizeDouble(_tp,Digits());
            if(!OrderSend(prequest,presult))
              {
               Print("error modif order = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode);
               Sleep(1000);
              }
            else
              {
               Print("successful modify order = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode);
               return;
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Close trade function                                             |
//+------------------------------------------------------------------+
bool close()
  {
   MqlTradeRequest mrequest;
   MqlTradeResult mresult;
   ZeroMemory(mrequest);
   if(PositionSelect(Symbol()))
     {
      mrequest.action=TRADE_ACTION_DEAL;
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
         mrequest.price=SymbolInfoDouble(Symbol(),SYMBOL_BID);
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
         mrequest.price=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      mrequest.symbol  = Symbol();
      mrequest.volume  = PositionGetDouble(POSITION_VOLUME);
      mrequest.magic   = PositionGetInteger(POSITION_MAGIC);
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
         mrequest.type=ORDER_TYPE_SELL;
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
         mrequest.type=ORDER_TYPE_BUY;
      mrequest.deviation=Slippage;
      if(!OrderSend(mrequest,mresult)) {GetLastError(); return(false);}
      else return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
