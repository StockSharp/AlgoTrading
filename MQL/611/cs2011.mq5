//+------------------------------------------------------------------+
//|                                                       cs2011.mq5 |
//|                                         Copyright © 2011, Xupypr |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Xupypr"
#property version   "1.1"
//+-----------------------------------+
input double Risk=3; // Initial lot
input int TP=2200;
input int SL=0;
input int Fast=30;
input int Slow=500;
input int Sign=36;
//+-----------------------------------+
double Money,Deposit=5000;
int    MACD;
//+-----------------------------------+
void OnInit()
  {
   MACD=iMACD(NULL,0,Fast,Slow,Sign,PRICE_CLOSE);
  }
//+-----------------------------------+
void OnTick()
  {
   static bool UpSignal,DnSignal;
   if(TradeSignalCounter(UpSignal,DnSignal)) TradePerformer(UpSignal,DnSignal);
  }
//+-----------------------------------+
bool TradeSignalCounter(bool &UpSignal,bool &DnSignal)
  {
   if(Bars(_Symbol,0)<100) return(false);
   static int Recount;
   if(IsNewBar() || Recount)
     {
      double Ind[2],Sig[3];
      DnSignal=false;
      UpSignal=false;
      Recount=false;
      if(CopyBuffer(MACD,0,1,2,Ind)<0) Recount=true;
      if(CopyBuffer(MACD,1,1,3,Sig)<0) Recount=true;
      if(Recount==true) return(false);
      if(Ind[0]>0 && Ind[1]<0) DnSignal=true;
      if(Ind[0]<0 && Ind[1]>0) UpSignal=true;
      if(Ind[1]<0 && Sig[0]<Sig[1] && Sig[1]>Sig[2]) DnSignal=true;
      if(Ind[1]>0 && Sig[0]>Sig[1] && Sig[1]<Sig[2]) UpSignal=true;
      return(true);
     }
   return(false);
  }
//+-----------------------------------+
bool TradePerformer(bool &UpSignal,bool &DnSignal)
  {
   if(UpSignal)
     {
      if(PositionOpen(POSITION_TYPE_BUY)) UpSignal=false;
     }
   if(DnSignal)
     {
      if(PositionOpen(POSITION_TYPE_SELL)) DnSignal=false;
     }
   return(true);
  }
//+-----------------------------------+
double Money_M()
  {
   HistorySelect(0,TimeCurrent());
   int total=HistoryDealsTotal();
   double profit=0;
   for(int i=0;i<total;i++)
     {
      ulong deal_ticket=HistoryDealGetTicket(i);
      if(HistoryDealGetString(deal_ticket,DEAL_SYMBOL)!=_Symbol) continue;
      if(HistoryDealGetInteger(deal_ticket,DEAL_TYPE)>1) continue;
      profit+=HistoryDealGetDouble(deal_ticket,DEAL_PROFIT);
      profit+=HistoryDealGetDouble(deal_ticket,DEAL_SWAP);
     }
   Money=Deposit+profit;
   if(PositionSelect(_Symbol))
     {
      Money+=PositionGetDouble(POSITION_PROFIT);
      Money+=PositionGetDouble(POSITION_SWAP);
     }
   double Min=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   double Limit=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_LIMIT);
   double Lots=Risk*Money/Deposit;
   Lots=MathMin(Limit,MathMax(Min,Lots));
   return(Lots);
  }
//+-----------------------------------+
bool PositionOpen(ENUM_POSITION_TYPE Type)
  {
   MqlTradeRequest request;
   MqlTradeResult result;
   MqlTradeCheckResult check;
   ZeroMemory(request);
   double volume=Money_M();
   double takeprofit=0,stoploss=0;
   if(PositionSelect(_Symbol))
     {
      double volumepos=PositionGetDouble(POSITION_VOLUME);
      if(PositionGetInteger(POSITION_TYPE)==Type) volume-=volumepos;
      else volume+=volumepos;
     }
   for(int i=0;i<10;i++)
     {
      volume=NormalizeDouble(volume,1);
      if(volume<=0) break;
      if(Type==POSITION_TYPE_SELL)
        {
         request.type=ORDER_TYPE_SELL;
         request.price=SymbolInfoDouble(_Symbol,SYMBOL_BID);
         if(TP!=0) takeprofit = request.price-TP*_Point;
         if(SL!=0) stoploss = request.price+SL*_Point;
        }
      if(Type==POSITION_TYPE_BUY)
        {
         request.type=ORDER_TYPE_BUY;
         request.price=SymbolInfoDouble(_Symbol,SYMBOL_ASK);
         if(TP!=0) takeprofit = request.price+TP*_Point;
         if(SL!=0) stoploss = request.price-SL*_Point;
        }
      request.action = TRADE_ACTION_DEAL;
      request.symbol = _Symbol;
      request.volume = MathMin(volume,SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX));
      request.sl = stoploss;
      request.tp = takeprofit;
      request.deviation=SymbolInfoInteger(_Symbol,SYMBOL_SPREAD);
      request.type_filling=ORDER_FILLING_FOK;
      request.comment=DoubleToString(Money,2)+"$";
      if(!OrderCheck(request,check))
        {
         if(check.margin_level<100) volume-=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
         Print("OrderCheck Code: ",check.retcode);
         continue;
        }
      if(!OrderSend(request,result) || result.deal==0)
        {
         Print("OrderSend Code: ",result.retcode);
         if(result.retcode==TRADE_RETCODE_TRADE_DISABLED) break;
         if(result.retcode==TRADE_RETCODE_MARKET_CLOSED) break;
         if(result.retcode==TRADE_RETCODE_NO_MONEY) break;
         if(result.retcode==TRADE_RETCODE_TOO_MANY_REQUESTS) Sleep(5000);
         if(result.retcode==TRADE_RETCODE_FROZEN) break;
         if(result.retcode==TRADE_RETCODE_CONNECTION) Sleep(15000);
         if(result.retcode==TRADE_RETCODE_LIMIT_VOLUME) break;
        }
      else volume-=result.volume;
      Sleep(1000);
     }
   if(!PositionSelect(_Symbol)) return(false);
   return(true);
  }
//+-----------------------------------+
bool IsNewBar()
  {
   static datetime Told;
   datetime Tnew[1];
   CopyTime(_Symbol,PERIOD_CURRENT,0,1,Tnew);
   if(Tnew[0]!=Told)
     {
      Told=Tnew[0];
      return(true);
     }
   return(false);
  }
//+-----------------------------------+
