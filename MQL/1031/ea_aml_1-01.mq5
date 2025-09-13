//+------------------------------------------------------------------+
//|                                                  EA_AML_1-01.mq5 |
//|                                Copyright © 2012.08.30, Alexander |
//|                        https://login.mql5.com/en/users/Im_hungry |
//|ICQ: 609928564 | email: I-m-hungree@yandex.ru | skype:i_m_hungree |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012"
#property link      "EA_AML_1-01 by Im_hungry"
//---
input  string              section_1         =                    "========  Trade options";
input  double              Lots              = 0.1;                // Lots
input  double              TP                = 3500;               // TP
input  double              SL                = 500;                // SL
input  int                 Slippage          = 45;                 // Slippage
input  int                 magic             = 20120830;           // magic
input  int                 N_modify_sltp     = 7;                  // N_modify_sltp
input  bool                use_opposite      = true;               // use_opposite
//---
input  string              section_2         =                     "========  Multpl";
input  bool                use_Multpl        = false;              // use_Multpl
input  int                 Max_drawdown      = 1800;               // Max_drawdown
//---
input  string              section_3         =                    "========  AML";
input  int                 Fractal           = 70;                 // Fractal
input  int                 Lag               = 18;                 // Lag
input  int                 Shift             = 0;                  // Shift
//+------------------------------------------------------------------+
//| global parameters                                                |
//+------------------------------------------------------------------+
int handle_AML;
datetime _time;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   handle_AML=iCustom(Symbol(),NULL,"AML",Fractal,Lag,Shift);
   if(handle_AML==INVALID_HANDLE)
     {
      Print("Error in loading of AML. LastError = ",GetLastError());
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
   IndicatorRelease(handle_AML);
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
      _time=Now_time[0]; double aml1[1],open[1],close[1];
      int cop1=CopyBuffer(handle_AML,0,1,1,aml1),
      cop2 = CopyOpen(Symbol(),0,1,1,open),
      cop3 = CopyClose(Symbol(),0,1,1,close);
      Print("1--  aml1[0]=",aml1[0]," open[0]=",open[0]," close[0]=",close[0]);
      if(cop1<=0 || cop2<=0 || cop3<=0) return;
      if(!PositionSelect(Symbol()))
        {
         if(aml1[0]>=open[0] && aml1[0]<=close[0]) open(0);
         if(aml1[0]<=open[0] && aml1[0]>=close[0]) open(1);
        }
      else if(use_opposite)
        {
         int type=(int)PositionGetInteger(POSITION_TYPE);
         if(type==0 && aml1[0]<=open[0] && aml1[0]>=close[0])
            if(close()) open(1);
         if(type==1 && aml1[0]>=open[0] && aml1[0]<=close[0])
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
      mrequest.price=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      if(SL>0) _sl=mrequest.price-(SL*Point());
      if(TP>0) _tp=mrequest.price+(TP*Point());
     }
   if(type==1)
     {
      mrequest.type=ORDER_TYPE_SELL;
      mrequest.price=SymbolInfoDouble(Symbol(),SYMBOL_BID);
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
