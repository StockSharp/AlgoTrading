//+------------------------------------------------------------------+
//|                                              eKeyboardTrader.mq5 |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright     "Integer"
#property link          "https://login.mql5.com/ru/users/Integer"
#property description   " "
#property description   "      http://dmffx.com"
#property description   " "
#property description   "      mailto:for-good-letters@yandex.ru"
#property description   " "
//+------------------------------------------------------------------+
//| cIntTrade class                                                  |
//+------------------------------------------------------------------+
class cIntTrade
  {
private:
   MqlTradeRequest   request;
   MqlTradeResult    result;
   double            miAsk,miBid,miPoint;
   int               miMSL,miDigits,miSpread;
   string BuyInfo(string aSymbol,double aVolume,double aStopLoss,double aTakeProfit,int aSlippage,ulong aMagic,string aComment)
     {
      return(aSymbol+", "+
             DoubleToString(aVolume,2)+", "+
             "op: "+DoubleToString(miAsk,miDigits)+", "+
             "sl: "+DoubleToString(aStopLoss,miDigits)+", "+
             "tp: "+DoubleToString(aTakeProfit,miDigits)+", "+
             "slip: "+IntegerToString(aSlippage,0)+", "+
             "mn: "+IntegerToString(aMagic)+", "+
             "com: "+aComment+"; "+
             "spr: "+IntegerToString(miSpread,0)+", "+
             "msl: "+IntegerToString(miMSL,0));
     }
   string SellInfo(string aSymbol,double aVolume,double aStopLoss,double aTakeProfit,int aSlippage,ulong aMagic,string aComment)
     {
      return(aSymbol+", "+
             DoubleToString(aVolume,2)+", "+
             "op: "+DoubleToString(miBid,miDigits)+", "+
             "sl: "+DoubleToString(aStopLoss,miDigits)+", "+
             "tp: "+DoubleToString(aTakeProfit,miDigits)+", "+
             "slip: "+IntegerToString(aSlippage,0)+", "+
             "mn: "+IntegerToString(aMagic)+", "+
             "com: "+aComment+"; "+
             "spr: "+IntegerToString(miSpread,0)+", "+
             "msl: "+IntegerToString(miMSL,0));
     }
   string RetCodeTxt(int aRetCode)
     {
      string tErrText="Unknown ("+IntegerToString(aRetCode)+")";
      switch(aRetCode)
        {
         case TRADE_RETCODE_REQUOTE:            return("Requote (REQUOTE)");
         case TRADE_RETCODE_REJECT:             return("Request rejected (REJECT)");
         case TRADE_RETCODE_CANCEL:             return("Request canceled by trader (CANCEL)");
         case TRADE_RETCODE_PLACED:             return("Order placed (PLACED)");
         case TRADE_RETCODE_DONE:               return("Request completed (DONE)");
         case TRADE_RETCODE_DONE_PARTIAL:       return("Only part of the request was completed (DONE_PARTIAL)");
         case TRADE_RETCODE_ERROR:              return("Request processing error (ERROR)");
         case TRADE_RETCODE_TIMEOUT:            return("Request canceled by timeout (TIMEOUT)");
         case TRADE_RETCODE_INVALID:            return("Invalid request (INVALID)");
         case TRADE_RETCODE_INVALID_VOLUME:     return("Invalid volume in the request (INVALID_VOLUME)");
         case TRADE_RETCODE_INVALID_PRICE:      return("Invalid price in the request (INVALID_PRICE)");
         case TRADE_RETCODE_INVALID_STOPS:      return("Invalid stops in the request (INVALID_STOPS)");
         case TRADE_RETCODE_TRADE_DISABLED:     return("Trade is disabled (TRADE_DISABLED)");
         case TRADE_RETCODE_MARKET_CLOSED:      return("Market is closed (MARKET_CLOSED)");
         case TRADE_RETCODE_NO_MONEY:           return("There is not enough money to complete the request (NO_MONEY)");
         case TRADE_RETCODE_PRICE_CHANGED:      return("Prices changed (PRICE_CHANGED)");
         case TRADE_RETCODE_PRICE_OFF:          return("There are no quotes to process the request (PRICE_OFF)");
         case TRADE_RETCODE_INVALID_EXPIRATION: return("Invalid order expiration date in the request (INVALID_EXPIRATION)");
         case TRADE_RETCODE_ORDER_CHANGED:      return("Order state changed (ORDER_CHANGED)");
         case TRADE_RETCODE_TOO_MANY_REQUESTS:  return("Too frequent requests (TOO_MANY_REQUESTS)");
         case TRADE_RETCODE_NO_CHANGES:         return("No changes in request (NO_CHANGES)");
         case TRADE_RETCODE_SERVER_DISABLES_AT: return("Autotrading disabled by server (SERVER_DISABLES_AT)");
         case TRADE_RETCODE_CLIENT_DISABLES_AT: return("Autotrading disabled by client terminal (CLIENT_DISABLES_AT)");
         case TRADE_RETCODE_LOCKED:             return("Request locked for processing (LOCKED)");
         case TRADE_RETCODE_FROZEN:             return("Order or position frozen (FROZEN)");
         case TRADE_RETCODE_INVALID_FILL:       return("Invalid order filling type (INVALID_FILL)");
         case TRADE_RETCODE_CONNECTION:         return("No connection with the trade server (CONNECTION)");
         case TRADE_RETCODE_ONLY_REAL:          return("Operation is allowed only for live accounts (ONLY_REAL)");
         case TRADE_RETCODE_LIMIT_ORDERS:       return("The number of pending orders has reached the limit (LIMIT_ORDERS)");
         case TRADE_RETCODE_LIMIT_VOLUME:       return("The volume of orders and positions for the symbol has reached the limit (LIMIT_VOLUME)");

        }
      return("?");
     }
   void MarketInfo(string aSymbol)
     {
      miAsk=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
      miBid=SymbolInfoDouble(aSymbol,SYMBOL_BID);
      miMSL=(int)SymbolInfoInteger(aSymbol,SYMBOL_TRADE_STOPS_LEVEL);
      miPoint=SymbolInfoDouble(aSymbol,SYMBOL_POINT);
      miDigits=(int)SymbolInfoInteger(aSymbol,SYMBOL_DIGITS);
      miSpread=(int)SymbolInfoInteger(aSymbol,SYMBOL_SPREAD);
     }
public:
   void SolveBuySLTP(string aSymbol,int aStopLoss,int aTakeProfit,double  &aSL,double  &aTP,bool aSLTPCorrection=false)
     {
      aSL=0;
      aTP=0;
      if(aStopLoss<=0 && aTakeProfit<=0)
        {
         return;
        }
      double msl;
      double pAsk=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
      double pBid=SymbolInfoDouble(aSymbol,SYMBOL_BID);
      double pPoint=SymbolInfoDouble(aSymbol,SYMBOL_POINT);
      int pStopLevel=(int)SymbolInfoInteger(aSymbol,SYMBOL_TRADE_STOPS_LEVEL);
      int pDigits=(int)SymbolInfoInteger(aSymbol,SYMBOL_DIGITS);
      if(aStopLoss>0)
        {
         aSL=pAsk-pPoint*aStopLoss;
         aSL=NormalizeDouble(aSL,pDigits);
         if(aSLTPCorrection)
           {
            msl=pBid-pPoint*(pStopLevel+1);
            msl=NormalizeDouble(msl,pDigits);
            aSL=MathMin(aSL,msl);
           }
        }
      if(aTakeProfit>0)
        {
         aTP=pAsk+pPoint*aTakeProfit;
         aTP=NormalizeDouble(aTP,pDigits);
         if(aSLTPCorrection)
           {
            msl=pAsk+pPoint*(pStopLevel+1);
            msl=NormalizeDouble(msl,pDigits);
            aTP=MathMax(aTP,msl);
           }
        }
     }

   void SolveSellSLTP(string aSymbol,int aStopLoss,int aTakeProfit,double  &aSL,double  &aTP,bool aSLTPCorrection=false)
     {
      aSL=0;
      aTP=0;
      if(aStopLoss<=0 && aTakeProfit<=0)
        {
         return;
        }
      double msl;
      double pAsk=SymbolInfoDouble(aSymbol,SYMBOL_ASK);
      double pBid=SymbolInfoDouble(aSymbol,SYMBOL_BID);
      double pPoint=SymbolInfoDouble(aSymbol,SYMBOL_POINT);
      int pStopLevel=(int)SymbolInfoInteger(aSymbol,SYMBOL_TRADE_STOPS_LEVEL);
      int pDigits=(int)SymbolInfoInteger(aSymbol,SYMBOL_DIGITS);
      if(aStopLoss>0)
        {
         aSL=pBid+pPoint*aStopLoss;
         aSL=NormalizeDouble(aSL,pDigits);
         if(aSLTPCorrection)
           {
            msl=pAsk+pPoint*(pStopLevel+1);
            msl=NormalizeDouble(msl,pDigits);
            aSL=MathMax(aSL,msl);
           }
        }
      if(aTakeProfit>0)
        {
         aTP=pBid-pPoint*aTakeProfit;
         aTP=NormalizeDouble(aTP,pDigits);
         if(aSLTPCorrection)
           {
            msl=pBid-pPoint*(pStopLevel+1);
            msl=NormalizeDouble(msl,pDigits);
            aTP=MathMin(aTP,msl);
           }
        }
     }
   bool Close(string aSymbol,
              int aSlippage        =  0,
              ulong aMagic         =  0,
              string aComment      =  "",
              string aMessage      =  "",
              bool aSound          =  false
              )
     {
      if(PositionSelect(aSymbol))
        {
         switch(PositionGetInteger(POSITION_TYPE))
           {
            case POSITION_TYPE_BUY:
               if(aMessage=="")aMessage="Sell to close buy";
               Sell(aSymbol,PositionGetDouble(POSITION_VOLUME),0,0,aSlippage,aMagic,aComment,aMessage,aSound);
               break;
            case POSITION_TYPE_SELL:
               if(aMessage=="")aMessage="Buy to close sell";
               Buy(aSymbol,PositionGetDouble(POSITION_VOLUME),0,0,aSlippage,aMagic,aComment,aMessage,aSound);
               break;
           }
        }
      else
        {
         if(aSound)
           {
            PlaySound("request");
           }
        }
      return(true);
     }
   ulong Buy(string aSymbol,
             double aVolume       =  0.1,
             double aStopLoss     =  0,
             double aTakeProfit   =  0,
             int aSlippage        =  0,
             ulong aMagic         =  0,
             string aComment      =  "",
             string aMessage      =  "",
             bool aSound          =  false
             )
     {
      if(!HistorySelect(0,TimeCurrent()))return(false);
      MarketInfo(aSymbol);
      request.symbol=aSymbol;
      request.action=TRADE_ACTION_DEAL;
      request.type=ORDER_TYPE_BUY;
      request.volume=aVolume;
      request.price=miAsk;
      request.sl=aStopLoss;
      request.tp=aTakeProfit;
      request.deviation=aSlippage;
      request.type_filling=ORDER_FILLING_AON;
      request.comment=aComment;
      request.magic=aMagic;
      string InfoStr=BuyInfo(aSymbol,aVolume,aStopLoss,aTakeProfit,aSlippage,aMagic,aComment);
      if(aMessage==""){Print("-> Buy ("+InfoStr+")...");}else{Print("-> "+aMessage+" ("+InfoStr+")...");}
      if(aSound)PlaySound("timeout");
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)
        {
         Print("         ...ok (#"+IntegerToString(result.order)+")");
         if(aSound)PlaySound("ok");
         return(result.deal);
        }
      else
        {
         Print("         ...error "+IntegerToString(result.retcode)+" - "+RetCodeTxt(result.retcode));
         if(aSound)PlaySound("timeout");
         return(0);
        }
     }
   ulong Sell(string aSymbol,
              double aVolume       =  0.1,
              double aStopLoss     =  0,
              double aTakeProfit   =  0,
              int aSlippage        =  0,
              ulong aMagic         =  0,
              string aComment      =  "",
              string aMessage      =  "",
              bool aSound          =  false
              )
     {
      if(!HistorySelect(0,TimeCurrent()))return(false);
      MarketInfo(aSymbol);
      request.symbol=aSymbol;
      request.action=TRADE_ACTION_DEAL;
      request.type=ORDER_TYPE_SELL;
      request.volume=aVolume;
      request.price=miBid;
      bool ie=(SymbolInfoInteger(aSymbol,SYMBOL_TRADE_EXEMODE)==SYMBOL_TRADE_EXECUTION_INSTANT);
      request.sl=aStopLoss;
      request.tp=aTakeProfit;
      request.deviation=aSlippage;
      request.type_filling=ORDER_FILLING_AON;
      request.comment=aComment;
      request.magic=aMagic;
      string InfoStr=SellInfo(aSymbol,aVolume,aStopLoss,aTakeProfit,aSlippage,aMagic,aComment);
      if(aMessage==""){Print("-> Sell ("+InfoStr+")...");}else{Print("-> "+aMessage+" ("+InfoStr+")...");}
      if(aSound)PlaySound("expert");
      OrderSend(request,result);
      if(result.retcode==TRADE_RETCODE_DONE)
        {
         Print("         ...ok (#"+IntegerToString(result.order)+")");
         if(aSound)PlaySound("ok");
         return(result.deal);
        }
      else
        {
         Print("         ...error "+IntegerToString(result.retcode)+" - "+RetCodeTxt(result.retcode));
         if(aSound)PlaySound("timeout");
         return(0);
        }
     }
   void About()
     {
      Comment("Integer's Trade class. https://login.mql5.com/ru/users/Integer");
     }
  };

cIntTrade Trade;

double Lots,slts,tpts;
int StopLoss;
int TakeProfit;
int Slippage;

int InputMode=0;
string Inputed="";
string vshift="\n"; // vertical shift
string hshift="   "; // horizontal shift

datetime LastTradeTime;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Lots=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   StopLoss=0;
   TakeProfit=0;
   Slippage=(int)SymbolInfoInteger(_Symbol,SYMBOL_SPREAD)*2;
   ActionVariants();
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
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

   if(id==CHARTEVENT_KEYDOWN)
     {
      if(lparam==27)
        { // esc
         ActionVariants();
         InputMode=0;
        }
      if(lparam==13)
        { // enter

        }
      if(lparam==73)
        {
         if(InputMode==0)
           {
            InputVariants();
            InputMode=1;
           }
         else
           {
            if(Inputed!="")
              {
               switch(InputMode)
                 {
                  case 3: // лот
                     Lots=StringToDouble(Inputed);
                     break;
                  case 4:
                     StopLoss=(int)StringToInteger(Inputed);
                     break;
                  case 5:
                     TakeProfit=(int)StringToInteger(Inputed);
                     break;
                  case 6:
                     Slippage=(int)StringToInteger(Inputed);
                     break;
                 }
              }
            InputMode=0;
            ActionVariants();
           }
        }

      switch(InputMode)
        {
         case 0:
            switch(lparam)
              {
               case 66:
                  if(TimeCurrent()>LastTradeTime+1)
                    {
                     Trade.SolveBuySLTP(_Symbol,StopLoss,TakeProfit,slts,tpts,false);
                     if(Trade.Buy(_Symbol,Lots,slts,tpts,Slippage,0,"","",true)>0)
                       {
                        LastTradeTime=TimeCurrent();
                       }
                    }
                  else
                    {
                     PlaySound("request");
                    }
                  break;
               case 83:
                  if(TimeCurrent()>LastTradeTime+1)
                    {
                     Trade.SolveSellSLTP(_Symbol,StopLoss,TakeProfit,slts,tpts,false);
                     if(Trade.Sell(_Symbol,Lots,slts,tpts,Slippage,0,"","",true)>0)
                       {
                        LastTradeTime=TimeCurrent();
                       }
                    }
                  else
                    {
                     PlaySound("request");
                    }
                  break;
               case 67:
                  Trade.Close(_Symbol,Slippage,0,"","",true);
                  break;
              }
            break;
         case 1:
            switch(lparam)
              {
               case 76: // l
                  InputMode=3;
                  Inputed="";
                  ShowInputed("Lot");
                  break;
               case 75: // k
                  InputMode=4;
                  Inputed="";
                  ShowInputed("StopLoss");
                  break;
               case 74: // j
                  InputMode=5;
                  Inputed="";
                  ShowInputed("TakeProfit");
                  break;
               case 72: // j
                  InputMode=6;
                  Inputed="";
                  ShowInputed("Slippage");
                  break;
              }
            break;
         case 3:
            Inputed=Inputed+KeyChar(lparam);
            ShowInputed("Lot");
            break;
         case 4:
            Inputed=Inputed+KeyChar(lparam);
            ShowInputed("StopLoss");
            break;
         case 5:
            Inputed=Inputed+KeyChar(lparam);
            ShowInputed("TakeProfit");
            break;
         case 6:
            Inputed=Inputed+KeyChar(lparam);
            ShowInputed("Slippage");
            break;
        }

     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ShowInputed(string aCaption)
  {
   string str="";
   str=str+vshift;
   str=str+hshift+aCaption+": "+Inputed+"_";
   str=str+"\n";
   str=str+"\n";
   str=str+hshift+"i - apply"+"\n";
   str=str+hshift+"esc - cancel"+"\n";
   str=str+hshift+"backspace - undo"+"\n";
   Comment(str);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string KeyChar(long aKeyCode)
  {
   if(aKeyCode==8)
     {
      if(StringLen(Inputed)>0)
        {
         Inputed=StringSubstr(Inputed,0,StringLen(Inputed)-1);
        }
      return("");
     }
   if(aKeyCode==110 || aKeyCode==188 || aKeyCode==190 || aKeyCode==191)
     {
      return(".");
     }
   if(aKeyCode>=96 && aKeyCode<=105)
     {
      return(IntegerToString(aKeyCode-96));
     }
   if(aKeyCode>=48 && aKeyCode<=57)
     {
      return(IntegerToString(aKeyCode-48));
     }
   return("");
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ActionVariants()
  {
   string str="";
   str=str+vshift;
   str=str+hshift+"Parameters:\n";
   str=str+"\n";
   str=str+hshift+"      Lots: "+DoubleToString(Lots,2)+"\n";
   str=str+hshift+"      StopLoss: "+DoubleToString(StopLoss,0)+"\n";
   str=str+hshift+"      TeakProfit: "+DoubleToString(TakeProfit,0)+"\n";
   str=str+hshift+"      Slippage: "+DoubleToString(Slippage,0)+"\n";
   str=str+hshift+"\n";
   str=str+hshift+"Commands:\n";
   str=str+"\n";
   str=str+hshift+"      b - buy"+"\n";
   str=str+hshift+"      s - sell"+"\n";
   str=str+hshift+"      c - close"+"\n";
   str=str+hshift+"      i - input"+"\n";
   Comment(str);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void InputVariants()
  {
   string str="";
   str=str+vshift;
   str=str+hshift+"Select:\n";
   str=str+"\n";
   str=str+hshift+"      l - lots"+"\n";
   str=str+hshift+"      k - stoploss"+"\n";
   str=str+hshift+"      j - takeprofit"+"\n";
   str=str+hshift+"      h - slippage"+"\n";
   str=str+hshift+"      i, esc - cancel"+"\n";
   Comment(str);

  }
//+------------------------------------------------------------------+
