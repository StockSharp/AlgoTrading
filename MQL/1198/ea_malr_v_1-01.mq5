//====================================================================//
//|                                                EA_MALR_v_1-01.mq5 ||
//|                                 Copyright © 2012.10.20, Alexander ||
//|                         https://login.mql5.com/en/users/Im_hungry ||
//| ICQ: 509928564 | email: I-m-hungree@yandex.ru | skype:i_m_hungree ||
//====================================================================//
#property copyright "Copyright © 2012"
#property link      "EA_MALR_v_1-01 by Im_hungry"

//====================================================================//
input  string              section_1             =                   "=====  Trade options";
input  double              Lot                   = 0.1;               // Lot
input  int                 sl                    = 2550;              // sl
input  int                 tp                    = 2578;              // tp
input  int                 Slippage              = 65;                // Slippage
input  int                 magic                 = 20121017;          // magic
input  int                 N_modify_sltp         = 7;                 // N_modify_sltp
input  bool                use_Averaging         = false;             // use_Averaging
input  int                 loss_forAveraging     = 500;               // loss_forAveraging
input  bool                Position_overturn     = true;              // Position_overturn
input  double              koff_multiplication   = 2.0;               // koff_multiplication
//====================================================================//
input  string              section_2             =                   "=====  lot increase";
input  bool                use_increase          = true;              // use_increase
input  int                 Max_drawdown          = 5000;              // Max_drawdown
//====================================================================//
input  string              section_3             =                   "=====  Trail";
input  bool                Trail_StopLoss        = false;             // Trail_StopLoss
input  int                 trail                 = 200;               // trail
input  bool                Activate_by_profit    = true;              // Activate_by_profit
input  int                 profit                = 50;                // use_pointTrail
//====================================================================//
input  string              section_4_1           =                   "=====  MALR";
input  int                 MAPeriod              = 120;               // MAPeriod
input  int                 MAShift               = 0;                 // MAShift
input  ENUM_APPLIED_PRICE  AppliedPrice          = PRICE_CLOSE;       // AppliedPrice
input  double              ChannelReversal       = 1.1;               // ChannelReversal
input  double              ChannelBreakout       = 1.1;               // ChannelBreakout
//=================================================================//
// global parameters                                               ||
//=================================================================//
datetime  _time;
double    high_extr[2],low_extr[2],point,min_lot,max_lot,
          prev_open,prev_avlot;
int       handle_MALR,digits;
//=================================================================//
// Expert initialization function                                  ||
//=================================================================//
int OnInit()
  {
   point = SymbolInfoDouble(_Symbol,SYMBOL_POINT);
   digits = (int)SymbolInfoInteger(_Symbol,SYMBOL_DIGITS);
   min_lot = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   max_lot = SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
   prev_open = 0;
   handle_MALR=iCustom(_Symbol,PERIOD_CURRENT,"MALR",MAPeriod,MAShift,AppliedPrice,ChannelReversal,ChannelBreakout);
   if(handle_MALR==INVALID_HANDLE)
     {
      Print("Error in loading MALR : "+(string)GetLastError()+" / EA removed...");
      ExpertRemove();
     }
   if(PositionSelect(Symbol()) && (use_Averaging || Position_overturn))
     {
      HistorySelect(0,TimeCurrent());
      int tot = HistoryDealsTotal();
      ulong tick = 0;
      for(int l=tot; l>=0; l--)
        {
         tick = HistoryDealGetTicket(l);
         if(HistoryDealGetString(tick,DEAL_SYMBOL)==Symbol() &&
            HistoryDealGetInteger(tick,DEAL_MAGIC)==magic)
           {
            if(HistoryDealGetInteger(tick,DEAL_ENTRY)==DEAL_ENTRY_IN)
              {
               prev_open = HistoryDealGetDouble(tick,DEAL_PRICE);
               if(!use_Averaging) break;
               if(HistoryDealGetString(tick,DEAL_COMMENT)!="Overturn")
                 {
                  prev_avlot = HistoryDealGetDouble(tick,DEAL_VOLUME);
                  break;
                 }
              }
           }
        }
     }
//---
   return(0);
  }
//=================================================================//
// Expert deinitialization function                                ||
//=================================================================//
void OnDeinit(const int reason)
  {
   IndicatorRelease(handle_MALR);
  }
//=================================================================//
// Expert tick function                                            ||
//=================================================================//
void OnTick()
  {
   bool pos=PositionSelect(Symbol());
   if(!pos || (pos && (use_Averaging || Position_overturn)))
     {
      datetime Now_time[1];
      int coptTime=CopyTime(Symbol(),NULL,0,1,Now_time);
      if(Now_time[0]>_time && coptTime!=-1)
        {
         int type=-1;
         double propn=0,prev_lot=0;
         if((use_Averaging || Position_overturn) && pos)
           {
            prev_lot=PositionGetDouble(POSITION_VOLUME);
            type=(int)PositionGetInteger(POSITION_TYPE);
            if(type==0 && prev_open!=0) propn = prev_open-SymbolInfoDouble(Symbol(),SYMBOL_ASK);
            if(type==1 && prev_open!=0) propn = SymbolInfoDouble(Symbol(),SYMBOL_BID)-prev_open;
           }
         ArrayInitialize(high_extr,0);
         ArrayInitialize(low_extr,0);
         int cop1 = CopyBuffer(handle_MALR,3,1,2,high_extr);
         int cop2 = CopyBuffer(handle_MALR,4,1,2,low_extr);
         if(cop1==2 || cop2==2 || high_extr[0]>0 || high_extr[1]>0 || low_extr[0]>0 || low_extr[1]>0)
           {
            double close[2];
            int copy_close = CopyClose(Symbol(),NULL,1,2,close);
            if(copy_close==2)
              {
               if(high_extr[0]<=close[0] && high_extr[1]>=close[1])
                 {
                  if(Position_overturn)
                    {
                     if(type==0)
                       {
                        if(close())
                          {
                           if(!open(1,prev_lot,3)) return;
                          }
                        _time=Now_time[0];
                        return;
                       }
                     if(!use_Averaging && pos)
                       {
                        _time=Now_time[0];
                        return;
                       }
                    }
                  if(use_Averaging)
                    {
                     if(type!=-1)
                       {
                        if(type==1 && propn>=loss_forAveraging*point && propn!=0)
                           if(!open(1,prev_avlot,2)) return;
                       }
                     else if(!open(1,0,1)) return;
                     _time=Now_time[0];
                     return;
                    }
                  else
                    {
                     if(!open(1,0,1)) return;
                    }
                 }
               if(low_extr[0]>=close[0] && low_extr[1]<=close[1])
                 {
                  if(Position_overturn)
                    {
                     if(type==1)
                       {
                        if(close())
                          {
                           if(!open(0,prev_lot,3)) return;
                          }
                        _time=Now_time[0];
                        return;
                       }
                     if(!use_Averaging && pos)
                       {
                        _time=Now_time[0];
                        return;
                       }
                    }
                  if(use_Averaging)
                    {
                     if(type!=-1)
                       {
                        if(type==0 && propn>=loss_forAveraging*point && propn!=0)
                           if(!open(0,prev_avlot,2)) return;
                       }
                     else if(!open(0,0,1)) return;
                     _time=Now_time[0];
                     return;
                    }
                  else
                    {
                     if(!open(0,0,1)) return;
                    }
                 }
               _time=Now_time[0];
              }
           }
        }
      else return;
     }
 //---
   if(Trail_StopLoss && PositionSelect(Symbol()))
     {
      int type=(int)PositionGetInteger(POSITION_TYPE);
      double Price=0,activate=0,
      PRopen=PositionGetDouble(POSITION_PRICE_OPEN),
      StLs=PositionGetDouble(POSITION_SL);
      if(type==0)
        {
         double bid=SymbolInfoDouble(Symbol(),SYMBOL_BID);
         Price=bid-trail*point;
         if(Activate_by_profit) activate=PRopen+profit*point;
         if(!Activate_by_profit?true:bid>=activate)
            if((StLs<Price && StLs>0) || StLs==0) Modifi_Position(Price);
        }
    //---
      if(type==1)
        {
         double ask=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
         Price=ask+trail*point;
         if(Activate_by_profit) activate=PRopen-profit*point;
         if(!Activate_by_profit?true:ask<=activate)
            if((StLs>Price && StLs>0) || StLs==0) Modifi_Position(Price);
        }
     }
  }
//=================================================================//
// Trail  SL                                                       ||
//=================================================================//
void Modifi_Position(double _trail)
  {
   if(!PositionSelect(Symbol())) return;
   double prof_prv=PositionGetDouble(POSITION_TP);
   MqlTradeRequest prequest; MqlTradeResult presult; ZeroMemory(prequest);
   prequest.action = TRADE_ACTION_SLTP;
   prequest.symbol = Symbol();
   prequest.sl     = NormalizeDouble(_trail,digits);
   prequest.tp     = prof_prv;
//---
   if(!OrderSend(prequest,presult))
      Alert("error modify SL = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode);
  }
//=================================================================//
// Open trade function                                             ||
//=================================================================//
bool open(int type,double vol,int mode)
  {
   double _sl=0.0,_tp=0.0,_nLt=vol>0?vol:Lot;
   if(use_increase && vol==0 && !PositionSelect(Symbol()))
     {
      _nLt = NormalizeDouble((Lot*AccountInfoDouble(ACCOUNT_EQUITY)/Max_drawdown),2);
      if(loss_forAveraging) prev_avlot = _nLt;
     }
   if(mode==3 && koff_multiplication>0) _nLt = vol*koff_multiplication;
   if(max_lot<_nLt)
     {
      _nLt=max_lot;
      Alert("Exceed the maximum Lot Volume. Will be installed the maximum limit: ",_nLt);
     }
   if(min_lot>_nLt)
     {
      _nLt=min_lot;
      Alert("Exceed the maximum Lot Volume. Will be installed the minimum limit: ",_nLt);
     }
//---
   MqlTradeRequest mrequest;MqlTradeResult mresult;
   ZeroMemory(mrequest);
   mrequest.action    = TRADE_ACTION_DEAL;
   mrequest.symbol    = Symbol();
   mrequest.volume    = _nLt;
   mrequest.magic     = magic;
   if(mode==1) mrequest.comment = "Main";
   if(mode==2) mrequest.comment = "Averaging";
   if(mode==3) mrequest.comment = "Overturn";
   mrequest.deviation = Slippage;
   if(type==0)
     {
      mrequest.type    = ORDER_TYPE_BUY;
      mrequest.price   = SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      if(sl>0) _sl=mrequest.price-(sl*point);
      if(tp>0) _tp=mrequest.price+(tp*point);
     }
   if(type==1)
     {
      mrequest.type    = ORDER_TYPE_SELL;
      mrequest.price   = SymbolInfoDouble(Symbol(),SYMBOL_BID);
      if(sl>0) _sl=mrequest.price+(sl*point);
      if(tp>0) _tp=mrequest.price-(tp*point);
     }
//---
   if(!OrderSend(mrequest,mresult))
     {
      Print("error Opened order  ",__FUNCTION__,": ",mresult.comment," answer code ",mresult.retcode);
      return(false);
     }
   else
     {
      prev_open=mrequest.price;
      if(PositionSelect(Symbol()) && (sl>0 || tp>0))
        {
         for(int h=0; h<N_modify_sltp; h++)
           {
            MqlTradeRequest         prequest;
            MqlTradeResult          presult;
            ZeroMemory(prequest);
            prequest.action   = TRADE_ACTION_SLTP;
            prequest.symbol   = Symbol();
            prequest.sl       = NormalizeDouble(_sl,digits);
            prequest.tp       = NormalizeDouble(_tp,digits);
            if(!OrderSend(prequest,presult))
              {
               Print("error modif order = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode);
               Sleep(1000);
              }
            else
              {
               Print("successful modify order = ",__FUNCTION__,": ",presult.comment," answer code ",presult.retcode);
               break;
              }
           }
        }
      return(true);
     }
   return(false);
  }
//=================================================================//
// Close trade function                                            ||
//=================================================================//
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
      if(!OrderSend(mrequest,mresult))
        {
         Print("error close position = ",__FUNCTION__,": ",mresult.comment," answer code ",mresult.retcode);
         return(false);
        }
      else return(true);
     }
   return(false);
  }
//+------------------------------------------------------------------+
