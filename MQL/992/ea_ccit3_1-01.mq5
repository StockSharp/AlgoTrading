//+------------------------------------------------------------------+
//|                                                EA_CCIT3_1-01.mq5 |
//|                                Copyright © 2012.08.19, Alexander |
//|                        https://login.mql5.com/en/users/Im_hungry |
//|ICQ: 609928564 | email: I-m-hungree@yandex.ru | skype:i_m_hungree |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012"
#property link      "EA_CCIT3_1-01"
//---
input  string               section_1             =                    "===== Trade options";
input  double               Lots                  = 1.0;                // Lots
input  double               TP                    = 1750;               // TP
input  double               SL                    = 0;                  // SL
input  int                  Slippage              = 65;                 // Slippage
input  int                  magic                 = 20120819;           // magic
input  int                  N_modify_sltp         = 7;                  // N_modify_sltp
input  int                  trail                 = 0;                  // trail
input  int                  Max_drawdown          = 0;                  // Max_drawdown
input  bool                 Trade_overturn        = false;              // Trade_overturn
//---
input  string               section_3             =                    "===== Simple CCIT3";
input  bool                 use_Simple_CCIT3      = true;               // use_Simple_CCIT3_Smpl
input  int                  CCI_Period_Smpl       = 285;                // CCI_Period_Smpl
input  ENUM_APPLIED_PRICE   CCI_Price_Type_Smpl   = PRICE_TYPICAL;      // CCI_Price_Type_Smpl
input  int                  T3_Period_Smpl        = 60;                 // T3_Period_Smpl
input  double               Koeff_B_Smpl          = 0.618;              // Koeff_B_Smpl
//---
input  string               section_4             =                    "===== noReCalc CCIT3";
input  bool                 use_noReCalc_CCIT3    = false;              // use_noReCalc_CCIT3
input  int                  CCI_Period_OtRng      = 250;                // CCI_Period_OtRng
input  ENUM_APPLIED_PRICE   CCI_Price_Type_OtRng  = PRICE_TYPICAL;      // CCI_Price_Type_OtRng
input  int                  T3_Period_OtRng       = 170;                // T3_Period_OtRng
input  double               Koeff_B_OtRng         = 0.618;              // Koeff_B_OtRng
//+------------------------------------------------------------------+
//| global parameters                                                |
//+------------------------------------------------------------------+
double CCIT3[2],Trail_stop; int handle_CCIT3; datetime _time;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if((use_Simple_CCIT3 && use_noReCalc_CCIT3) || (!use_Simple_CCIT3 && !use_noReCalc_CCIT3))
     {
      Alert("Wrong Settings : choose one of use_Simple_CCIT3/use_noReCalc_CCIT3");
      Alert("Expert Removed");
      ExpertRemove();
     }
   if(use_Simple_CCIT3)
     {
      handle_CCIT3=iCustom(Symbol(),NULL,"CCIT3_Simple_v_2-01",CCI_Period_Smpl,CCI_Price_Type_Smpl,T3_Period_Smpl,Koeff_B_Smpl,100000);
      if(handle_CCIT3==INVALID_HANDLE)
        {
         Print("Error in loading CCIT3_Simple_v_2-01. Error : ",GetLastError());
         return(-1);
        }
     }
   if(use_noReCalc_CCIT3)
     {
      handle_CCIT3=iCustom(Symbol(),NULL,"CCIT3_noReCalc_v_3-01",CCI_Period_OtRng,CCI_Price_Type_OtRng,T3_Period_OtRng,Koeff_B_OtRng,100000);
      if(handle_CCIT3==INVALID_HANDLE)
        {
         Print("Error in loading Outrunning CCIT3_noReCalc_v_3-01. Error : ",GetLastError());
         return(-1);
        }
     }
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| deinitialization function                                        |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   IndicatorRelease(handle_CCIT3);
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
      int copy1= CopyBuffer(handle_CCIT3,0,1,2,CCIT3);
      if(copy1<=0) return;
      if(!PositionSelect(Symbol()))
        {
         if(CCIT3[0]<=0 && CCIT3[1]>0) open(0);
         if(CCIT3[0]>=0 && CCIT3[1]<0) open(1);
        }
      else
        {
         if(Trade_overturn)
           {
            int type=(int)PositionGetInteger(POSITION_TYPE);
            if(type==0 && CCIT3[0]>=0 && CCIT3[1]<0)
               if(close()) open(1);
            if(type==1 && CCIT3[0]<=0 && CCIT3[1]>0)
               if(close()) open(0);
           }
        }
     }
   if(trail>0)
     {
      if(PositionSelect(Symbol()) && PositionGetDouble(POSITION_PROFIT)>0)
        {
         int type=(int)PositionGetInteger(POSITION_TYPE);
         double Price=0;
         if(type==0)
           {
            Price=SymbolInfoDouble(Symbol(),SYMBOL_BID)-trail*Point();
            if((Trail_stop<Price && Trail_stop>0) || Trail_stop==0) Modifi_Position(Price);
           }
         if(type==1)
           {
            Price=SymbolInfoDouble(Symbol(),SYMBOL_ASK)+trail*Point();
            if((Trail_stop>Price && Trail_stop>0) || Trail_stop==0) Modifi_Position(Price);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Open trade function                                              |
//+------------------------------------------------------------------+
void open(int type)
  {
   double pr_open=0.0,_sl=0.0,_tp=0.0,_nLt=Lots;
   if(Max_drawdown>0)
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
      Trail_stop=0;
      if(SL>0 || TP>0)
        {
         for(int k=0; k<=N_modify_sltp; k++)
           {
            int minD=(int)SymbolInfoInteger(Symbol(),SYMBOL_TRADE_STOPS_LEVEL);
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
//| Trail  SL                                                        |
//+------------------------------------------------------------------+
void Modifi_Position(double _trail)
  {
   if(!PositionSelect(Symbol())) return;
   double prof_prv=PositionGetDouble(POSITION_TP);
   MqlTradeRequest prequest; MqlTradeResult presult; ZeroMemory(prequest);
   prequest.action = TRADE_ACTION_SLTP;
   prequest.symbol = Symbol();
   prequest.sl     = NormalizeDouble(_trail,(int)SymbolInfoInteger(Symbol(),SYMBOL_DIGITS));
   prequest.tp     = prof_prv;
   if(!OrderSend(prequest,presult))
      Alert("error re-modif SL = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode," trail=",_trail);
   else Trail_stop=_trail;
  }
//+------------------------------------------------------------------+
