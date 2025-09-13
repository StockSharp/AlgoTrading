#define EXPERT_MAGIC 123456   // MagicNumber of the expert
input int InpTakeProfit =900; // Take Profit (in points)
input int InpStopLoss   =900; // Stop loss (in points)
//+------------------------------------------------------------------+
//| Expert new tick handling function                                |
//+------------------------------------------------------------------+
void OnTick(void)
  {
   if(!PositionSelect(Symbol()))
     {
      double volume=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN);
      double price=SymbolInfoDouble(Symbol(),SYMBOL_ASK);
      double sl=price-InpStopLoss*Point();
      double tp=price+InpTakeProfit*Point();

      //--- Opening Buy position                                             |
      uint filling=(uint)SymbolInfoInteger(Symbol(),SYMBOL_FILLING_MODE);
      MqlTradeRequest     request= {};
      MqlTradeCheckResult check= {};
      MqlTradeResult      result= {};
      request.action   =TRADE_ACTION_DEAL;
      request.symbol   =Symbol();
      request.volume   =volume;
      request.type     =ORDER_TYPE_BUY;
      request.price    =price;
      request.sl       =sl;
      request.tp       =tp;
      request.deviation=INT_MAX;
      request.magic    =EXPERT_MAGIC;
      request.type_filling=((filling&SYMBOL_FILLING_FOK)==SYMBOL_FILLING_FOK) ? ORDER_FILLING_FOK : ORDER_FILLING_IOC;

      //--- check the request and margin
      if(OrderCheck(request,check))
        {
         //--- send the request
         if(!OrderSend(request,result))
            PrintFormat("retcode=%u  comment=%s",result.retcode,result.comment);
        }
     }
  }
//+------------------------------------------------------------------+
