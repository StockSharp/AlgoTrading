//+------------------------------------------------------------------+
//|                                                wstradecopier.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/ws/wsclient.mqh>
#include <MQL5Book/TradeUtils.mqh>
#include <MQL5Book/DealMonitor.mqh>
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/Tuples.mqh>
#include <MQL5Book/toyjson.mqh>

enum TRADE_ROLE
{
   TRADE_PUBLISHER,  // Trade Publisher
   TRADE_SUBSCRIBER  // Trade Subscriber
};

//+------------------------------------------------------------------+
//| I N P U T S                                                      |
//+------------------------------------------------------------------+
input string Server = "ws://localhost:9000/";
input TRADE_ROLE Role = TRADE_PUBLISHER;
input bool VerboseJson = true; // VerboseJson (user-friendly/machine-efficient)
input group "Publisher";
input string PublisherID = "PUB_ID_001";
input string PublisherPrivateKey = "PUB_KEY_FFF";
input string SymbolFilter = ""; // SymbolFilter (empty - current, '*' - any)
input ulong MagicFilter = 0;    // MagicFilter (0 - any)
input group "Subscriber";
input string SubscriberID = "SUB_ID_100";
input string SubscribeToPublisherID = "PUB_ID_001";
input string SubscriberAccessKey = "fd3f7a105eae8c2d9afce0a7a4e11bf267a40f04b7c216dd01cf78c7165a2a5a";
input string SymbolSubstitute = "EURUSD=GBPUSD"; // SymbolSubstitute (<from>=<to>,...)
input ulong SubscriberMagic = 0;

string Substitutes[][2];

//+------------------------------------------------------------------+
//| Initialization of symbol substitution list, which is also used   |
//| to accept actions on selected symbols only,                      |
//| for example, EURUSD=EURUSD to allow trading EURUSD on receiving  |
//+------------------------------------------------------------------+
void FillSubstitutes()
{
   string list[];
   const int n = StringSplit(SymbolSubstitute, ',', list);
   ArrayResize(Substitutes, n);
   for(int i = 0; i < n; ++i)
   {
      string pair[];
      if(StringSplit(list[i], '=', pair) == 2)
      {
         Substitutes[i][0] = pair[0];
         Substitutes[i][1] = pair[1];
      }
      else
      {
         Print("Wrong substitute: ", list[i]);
      }
   }
}

//+------------------------------------------------------------------+
//| Symbol substitution on subscriber                                |
//+------------------------------------------------------------------+
string FindSubstitute(const string s)
{
   for(int i = 0; i < ArrayRange(Substitutes, 0); ++i)
   {
      if(Substitutes[i][0] == s) return Substitutes[i][1];
   }
   return NULL;
}

//+------------------------------------------------------------------+
//| Class for JSON representation of a trade request result          |
//+------------------------------------------------------------------+
struct MqlTradeResultWeb: public MqlTradeResult
{
   MqlTradeResultWeb(const MqlTradeResult &r)
   {
      ZeroMemory(this);
      retcode = r.retcode;
      deal = r.deal;
      order = r.order;
      volume = r.volume;
      price = r.price;
      bid = r.bid;
      ask = r.ask;
      // not used in json
      comment = r.comment;
      request_id = r.request_id;
      retcode_external = r.retcode_external;
   }
   
   JsValue *asJsValue() const
   {
      JsValue *result = new JsValue();
      result.put("code", retcode);
      if(deal) result.put("d", deal);
      if(order) result.put("o", order);
      if(volume) result.put("v", TU::StringOf(volume));
      if(price) result.put("p", TU::StringOf(price));
      if(bid) result.put("b", TU::StringOf(bid));
      if(ask) result.put("a", TU::StringOf(ask));
      return result;
   }
};

//+------------------------------------------------------------------+
//| Class for JSON representation of a trade request                 |
//+------------------------------------------------------------------+
struct MqlTradeRequestWeb: public MqlTradeRequest
{
   MqlTradeRequestWeb(const MqlTradeRequest &r)
   {
      ZeroMemory(this);
      action = r.action;
      magic = r.magic;
      order = r.order;
      symbol = r.symbol;
      volume = r.volume;
      price = r.price;
      stoplimit = r.stoplimit;
      sl = r.sl;
      tp = r.tp;
      type = r.type;
      type_filling = r.type_filling;
      type_time = r.type_time;
      expiration = r.expiration;
      comment = r.comment;
      position = r.position;
      position_by = r.position_by;
   }

   JsValue *asJsValue() const
   {
      JsValue *req = new JsValue();
      // main block: action, symbol, type
      req.put("a", VerboseJson ? EnumToString(action) : (string)action); // number or enum_xyz
      if(StringLen(symbol) != 0) req.put("s", symbol);
      req.put("t", VerboseJson ? EnumToString(type) : (string)type);   // number or enum_xyz
      
      // volume block
      if(volume != 0) req.put("v", TU::StringOf(volume));
      req.put("f", VerboseJson ? EnumToString(type_filling) : (string)type_filling); // number or enum_xyz
      
      // all prices block
      if(price != 0) req.put("p", TU::StringOf(price));
      if(stoplimit != 0) req.put("x", TU::StringOf(stoplimit));
      if(sl != 0) req.put("sl", TU::StringOf(sl));
      if(tp != 0) req.put("tp", TU::StringOf(tp));

      // pending block
      if(TU::IsPendingType(type))
      {
         req.put("t", VerboseJson ? EnumToString(type_time) : (string)type_time); // number or enum_xyz
         if(expiration != 0) req.put("d", TimeToString(expiration));
      }

      // modification block
      if(order != 0) req.put("o", order);
      if(position != 0) req.put("q", position);
      if(position_by != 0) req.put("b", position_by);
      
      // auxiliary block
      if(magic != 0) req.put("m", magic);
      if(StringLen(comment)) req.put("c", comment);

      return req;
   }
   
};

//+------------------------------------------------------------------+
//| Class for JSON representation of a deal                          |
//+------------------------------------------------------------------+
class DealMonitorWeb: public DealMonitor
{
public:
   DealMonitorWeb(const ulong t): DealMonitor(t) { }
   
   JsValue *asJsValue() const
   {
      JsValue *deal = new JsValue();
      deal.put("d", get(DEAL_TICKET));
      deal.put("o", get(DEAL_ORDER));
      deal.put("t", TimeToString((datetime)get(DEAL_TIME), TIME_DATE | TIME_SECONDS));
      deal.put("tmsc", get(DEAL_TIME_MSC));
      deal.put("type", VerboseJson ? stringify(DEAL_TYPE) : (string)get(DEAL_TYPE));
      deal.put("entry", VerboseJson ? stringify(DEAL_ENTRY) : (string)get(DEAL_ENTRY));
      deal.put("pid", get(DEAL_POSITION_ID));
      deal.put("r", VerboseJson ? stringify(DEAL_REASON) : (string)get(DEAL_REASON));

      deal.put("v", TU::StringOf(get(DEAL_VOLUME)));
      deal.put("p", TU::StringOf(get(DEAL_PRICE)));
      if(get(DEAL_COMMISSION)) deal.put("com", TU::StringOf(get(DEAL_COMMISSION)));
      if(get(DEAL_SWAP)) deal.put("swap", TU::StringOf(get(DEAL_SWAP)));
      if(get(DEAL_PROFIT)) deal.put("m", TU::StringOf(get(DEAL_PROFIT)));
      if(get(DEAL_SL)) deal.put("sl", TU::StringOf(get(DEAL_SL)));
      if(get(DEAL_TP)) deal.put("tp", TU::StringOf(get(DEAL_TP)));
      
      deal.put("s", get(DEAL_SYMBOL));
      if(StringLen(get(DEAL_COMMENT))) deal.put("c", get(DEAL_COMMENT));
      if(StringLen(get(DEAL_EXTERNAL_ID))) deal.put("ext", get(DEAL_EXTERNAL_ID));

      return deal;
   }
};

//+------------------------------------------------------------------+
//| Custom client class to handle WebSocket events                   |
//+------------------------------------------------------------------+
class MyWebSocket: public WebSocketClient<Hybi>
{
public:
   MyWebSocket(const string address, const bool compress = false): WebSocketClient(address, compress) { }
   
   /* void onConnected() override { } */

   void onDisconnect() override
   {
      // ...
      // can do something more and call (or not) inherited code
      WebSocketClient<Hybi>::onDisconnect();
   }

   void onMessage(IWebSocketMessage *msg) override
   {
      Alert(msg.getString());
      JsValue *obj = JsParser::jsonify(msg.getString());
      if(obj && obj["msg"])
      {
         obj["msg"].print();
         if(!RemoteTrade(obj["msg"])) { /* handle errors */ }
         delete obj;
      }
      delete msg;
   }
};

MyWebSocket wss(Server);

bool RemoteTrade(JsValue *obj)
{
   bool success = false;
   
   if(obj["req"]["a"] == TRADE_ACTION_DEAL
      || obj["req"]["a"] == "TRADE_ACTION_DEAL") // enum values/strings are supported all the way
   {
      const string symbol = FindSubstitute(obj["req"]["s"].s);
      if(symbol == NULL)
      {
         Print("Suitable symbol not found for ", obj["req"]["s"].s);
         return false; // not allowed/not found
      }
      
      // NB: price, stop levels, lot limitations, etc are not analized here,
      // copy-trade is performed at current local price
      
      JsValue *pType = obj["req"]["t"];
      if(pType == ORDER_TYPE_BUY || pType == ORDER_TYPE_SELL
         || pType == "ORDER_TYPE_BUY" || pType == "ORDER_TYPE_SELL")
      {
         ENUM_ORDER_TYPE type;
         if(pType.detect() >= JS_STRING)
         {
            if(pType == "ORDER_TYPE_BUY") type = ORDER_TYPE_BUY;
            else type = ORDER_TYPE_SELL;
         }
         else
         {
            type = obj["req"]["t"].get<ENUM_ORDER_TYPE>();
         }
         
         MqlTradeRequestSync request;
         request.deviation = 10;
         request.magic = SubscriberMagic;
         request.type = type;
         
         const double lot = obj["req"]["v"].get<double>();
         JsValue *pDir = obj["deal"]["entry"];
         if(pDir == DEAL_ENTRY_IN || pDir == "DEAL_ENTRY_IN")
         {
            success = request._market(symbol, lot) && request.completed();
            Alert(StringFormat("Trade by subscription: market entry %s %s %s - %s",
               EnumToString(type), TU::StringOf(lot), symbol, success ? "Successful" : "Failed"));
         }
         else if(pDir == DEAL_ENTRY_OUT || pDir == "DEAL_ENTRY_OUT")
         {
            // this action assumes an existing position to close
            PositionFilter filter;
            int props[] = {POSITION_TICKET, POSITION_TYPE, POSITION_VOLUME};
            Tuple3<long,long,double> values[];
            filter.let(POSITION_SYMBOL, symbol).let(POSITION_MAGIC, SubscriberMagic).select(props, values);
            for(int i = 0; i < ArraySize(values) && !success; ++i)
            {
               if(!TU::IsSameType((ENUM_ORDER_TYPE)values[i]._2, type))  // opposite direction found
               {
                  if(TU::Equal(values[i]._3, lot)) // sufficient volume found
                  {
                     success = request.close(values[i]._1, lot) && request.completed();
                     Alert(StringFormat("Trade by subscription: market exit %s %s %s - %s",
                        EnumToString(type), TU::StringOf(lot), symbol, success ? "Successful" : "Failed"));
                  }
                  else
                  {
                     Print("Not enough volume in existing position ", values[i]._1);
                  }
               }
               else
               {
                  Print("Not required direction in existing position ", values[i]._1);
               }
            }
            
            if(!success)
            {
               Print("No suitable position to close");
            }
         }
         else
         {
            Print("Unsupported trade direction ", pDir.stringify());
         }
      }
      else
      {
         Print("Unsupported trade type ", pType.stringify());
      }
   }
   else
   {
      Print("Not a trade command");
   }
   return success;
}

PositionFilter Positions;

//+------------------------------------------------------------------+
//| Initialization handler                                           |
//+------------------------------------------------------------------+
int OnInit()
{
   Print("\n");

   if(MagicFilter) Positions.let(POSITION_MAGIC, MagicFilter);
   if(SymbolFilter == "") Positions.let(POSITION_SYMBOL, _Symbol);
   else if(SymbolFilter != "*") Positions.let(POSITION_SYMBOL, SymbolFilter);
  
   FillSubstitutes();
   EventSetTimer(1);
   wss.setTimeOut(1000);
   Print("Opening...");
   string custom;
   if(Role == TRADE_PUBLISHER)
   {
      custom = "Sec-Websocket-Protocol: X-MQL5-publisher-"
         + PublisherID + "-" + PublisherPrivateKey + "\r\n";
   }
   else
   {
      custom = "Sec-Websocket-Protocol: X-MQL5-subscriber-"
         + SubscriberID + "-" + SubscribeToPublisherID + "-" + SubscriberAccessKey + "\r\n";
   }
   return wss.open(custom) ? INIT_SUCCEEDED : INIT_FAILED;
}

//+------------------------------------------------------------------+
//| Trade filter for publisher                                       |
//+------------------------------------------------------------------+
bool FilterMatched(const string s, const ulong m)
{
   if(MagicFilter != 0 && MagicFilter != m)
   {
      return false;
   }

   if(StringLen(SymbolFilter) == 0)
   {
      if(s != _Symbol)
      {
         return false;
      }
   }
   else if(SymbolFilter != s && SymbolFilter != "*")
   {
      return false;
   }
   
   return true;
}

//+------------------------------------------------------------------+
//| Trade transactions handler                                       |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction &transaction,
   const MqlTradeRequest &request,
   const MqlTradeResult &result)
{
   // debug
   // static ulong count = 0;
   // Print("(" + (string)count++ + ")");
   // Print(TU::StringOf(transaction));

   if(transaction.type == TRADE_TRANSACTION_REQUEST)
   {
      Print(TU::StringOf(request));
      Print(TU::StringOf(result));

      if(result.retcode == TRADE_RETCODE_PLACED           // successful action
         || result.retcode == TRADE_RETCODE_DONE
         || result.retcode == TRADE_RETCODE_DONE_PARTIAL)
      {
         if(FilterMatched(request.symbol, request.magic))
         {
            // container object for message,
            // can contain more sub-objects (status, advice, etc.),
            // it will be delivered to subscribers
            // as "msg" property of json in websocket-messages:
            // {"origin" : "this_publisher_id", "msg" : { your data goes here }}
            JsValue msg;

            MqlTradeRequestWeb req(request);
            msg.put("req", req.asJsValue());
            
            MqlTradeResultWeb res(result);
            msg.put("res", res.asJsValue());
            
            if(result.deal != 0)
            {
               DealMonitorWeb deal(result.deal);
               msg.put("deal", deal.asJsValue());
            }
            
            ulong tickets[];
            Positions.select(tickets);
            JsValue pos;
            pos.put("n", ArraySize(tickets));
            msg.put("pos", &pos);

            // TODO: collect account status:
            // - positions and their props,
            // - pending orders and their props,
            // - margin level, drawdown, etc.
            
            string buffer;
            msg.stringify(buffer);
            
            Print(buffer);
            
            wss.send(buffer);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Timer events handler                                             |
//+------------------------------------------------------------------+
void OnTimer()
{
   // if new messages arrive, call MyWebSocket::OnMessage() for each 
   wss.checkMessages(false); // in timer use non-blocking check
   /*
   // Alternative way of processing messages
   // (if implemented - we may not define MyWebSocket and use
   // WebSocketClient<Hybi> wss(Server);)

   IWebSocketMessage *m;
   while((m = wss.readMessage(false)) != NULL)
   {
      Alert(m.getString());
      // ...
      // other stuff which is currently in MyWebSocket::OnMessage()
      delete m;
   }
   */
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   if(wss.isConnected())
   {
      Print("Closing...");
      wss.close();
   }
}

//+------------------------------------------------------------------+
