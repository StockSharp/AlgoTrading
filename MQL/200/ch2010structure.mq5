//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2010, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
bool IsNewBar(ENUM_TIMEFRAMES period);
bool DebugOrderSend(int line,MqlTradeRequest &tr,MqlTradeResult &res);
bool DebugOrderCheck(int line,MqlTradeRequest &tr,MqlTradeCheckResult &tres);
//-------------------------------------------------------------------
class CExp
  {
private:
   //...
   double            getTP();
   double            getVol();
   ENUM_ORDER_TYPE   getOrderType();
   double            getDB();
public:
                     CExp(){};
                    ~CExp() {/*...*/};
   bool              Init(string symbol,/*...*/);
   void              DayTrade();
   void              IntraDayTrade();
  };
//-------------------------------------------------------------------
//....
bool IsNewBar(ENUM_TIMEFRAMES period)

  {
   static datetime prevTime[2];
   datetime currentTime[1];
   CopyTime(_Symbol,period,0,1,currentTime);
   int _=period==PERIOD_M30;
   if(currentTime[0]==prevTime[_])return(false);
   else
     {
      prevTime[_]=currentTime[0];
      return(true);
     }
  }
//------------------------------------------------------------------
bool DebugOrderSend(int line,MqlTradeRequest &tr,MqlTradeResult &res)
  {
   bool yes;
   double vol;
   int _;
   if(((OrdersTotal()==12) || 
      (SymbolInfoDouble(tr.symbol,SYMBOL_VOLUME_MIN)>tr.volume) || 
      (5<tr.volume)) && (tr.action==TRADE_ACTION_PENDING))
      return(false);
   if(tr.action==TRADE_ACTION_PENDING)
     {
      vol=0;
      if(PositionSelect(tr.symbol))
        {
         if(((PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY) && 
            (tr.type==ORDER_TYPE_BUY_LIMIT)) || 
            ((PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL) && 
            (tr.type==ORDER_TYPE_SELL_LIMIT)))
            vol=PositionGetDouble(POSITION_VOLUME);
        }

      for(_=OrdersTotal()-1; _>=0;_--)
         if(OrderSelect(OrderGetTicket(_)) && 
            (OrderGetString(ORDER_SYMBOL)==tr.symbol))
            if(OrderGetInteger(ORDER_TYPE)==tr.type)
               vol+=OrderGetDouble(ORDER_VOLUME_INITIAL);
      if(vol+tr.volume>15) tr.volume=15-vol;
      if(tr.volume>5) return(false);
      if(tr.volume<0.1) return(false);
     }
   yes=OrderSend(tr,res);
   if(!yes)
     {
      Print("Ordersend Err:",res.retcode);
      Print("Line: ",line);
      Print("tr.symbol:", tr.symbol);
      Print("tr.volume:", tr.volume);
      Print("tr.type:",tr.type);
      Print("tr.price:",tr.price);
      Print("tr.action:",tr.action);
      Print("tr.tp:",tr.tp);
     }
   return(yes);
  }
//------------------------------------------------------------------
bool DebugOrderCheck(int line,MqlTradeRequest &tr,MqlTradeCheckResult &tres)
  {
   if(((OrdersTotal()==12) || 
      (SymbolInfoDouble(tr.symbol,SYMBOL_VOLUME_MIN)>tr.volume) || 
      (SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX)<tr.volume)) && 
      (tr.action==TRADE_ACTION_PENDING)) return(false);
   bool yes=OrderCheck(tr,tres);
   if(!yes)
     {
      if(tres.retcode==10025) return(false);
      Print("OrderCheck Err:",tres.retcode);
      Print("tr.Comment:",tres.comment);
      Print("Line: ",line);
      Print("Bid:", SymbolInfoDouble(tr.symbol, SYMBOL_BID));
      Print("Ask:", SymbolInfoDouble(tr.symbol, SYMBOL_ASK));
      Print("tr.Order: ", tr.order);
      Print("tr.symbol:", tr.symbol);
      Print("tr.volume:", tr.volume);
      Print("tr.type:",tr.type);
      Print("tr.price:",tr.price);
      Print("tr.action:",tr.action);
      Print("tr.tp:",tr.tp);
     }
   return(yes);
  }
//--------------------------------------------------------------------
CExp chf,gbp,aud,jpy,eurgbp;
//--------------------------------------------------------------------
int OnInit()
  {
   chf.Init("USDCHF", /*...*/);
   gbp.Init("GBPUSD", /*...*/);
   aud.Init("AUDUSD", /*...*/);
   jpy.Init("USDJPY", /*...*/);
   eurgbp.Init("EURGBP",/*...*/);
   return(INIT_SUCCEEDED);
  }
//--------------------------------------------------------------------
void OnTick()
  {
   if(IsNewBar(PERIOD_D1))
     {
      chf.DayTrade();
      gbp.DayTrade();
      aud.DayTrade();
      jpy.DayTrade();
      eurgbp.DayTrade();
      return;
     }
   if(IsNewBar(PERIOD_M30))
     {
      chf.IntraDayTrade();
      gbp.IntraDayTrade();
      aud.IntraDayTrade();
      jpy.IntraDayTrade();
      eurgbp.IntraDayTrade();
     }
  }
//+------------------------------------------------------------------+
