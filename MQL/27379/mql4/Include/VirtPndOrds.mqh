//+------------------------------------------------------------------+
//|                                                      PndOrds.mqh |
//|                                              Copyright 2019, RAP |
//|                                 http://www.mql5.com/en/users/dng |
//+------------------------------------------------------------------+
#property copyright "Copyright 2019, R Poster"
#property link      "http://www.mql5.com/en/users/dng"
#property version   "1.00"
#property strict

//+------------------------------------------------------------------+
//|     Simulates Pending Order Execution                                                             |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CPO          
  {
   enum ENUM_ORD_TYPE 
    {
     ORD_TYPE_BUY      = 0,    // Buy Mkt Order
     ORD_TYPE_SELL     = 1,    // Sell Mkt Order
     ORD_TYPE_BUYLIMIT = 2,    // Buy Limit
     ORD_TYPE_SELLLIMIT= 3,    // Sell Limit
     ORD_TYPE_BUYSTOP  = 4,    // Buy Stop
     ORD_TYPE_SELLSTOP = 5     // Sell Stop
    };
private:
   int                  i_Ticket;            // Ticket of order
   string               s_Symbol;            // Symbol
   datetime             dt_OpenTime;         // Time of open position
   double               d_ThrshldPrice;       // Price of opened position
   double               d_SL;                // Stop Loss (pips)
   double               d_TP;                // Take Profit (pips)
   double               d_stp_loss;          // stop loss price
   double                d_tk_profit;         // take profit price
   ENUM_ORD_TYPE        e_OrdTyp;            // Direct of opened position (0-Buy,1=Sell)
   double               d_Lots;               // Lots
   int                  i_MagicNum;            // magic number
   string               s_Cmt;                 // commnet
   bool                 b_open;              // open virtual PO
   double               d_lifetime;          // Pend Order Expiration time
//---
   double               d_Point;             // point
   double               d_Mult;              // Multiplier of point
   int                  i_Digits;            // brokder ditits
   int                  i_slpg;               // slippage
   double               d_bid;               // current ask price
   double               d_ask;               // current bid price
 //  double               d_spread;
   
public:
                     CPO(string symbol, ENUM_ORD_TYPE stype,int slpg,double slots, datetime opntime,int magicNm, 
                         string comment, double thrshld_price,double stop_loss, double tk_profit,double lifetime);
                    ~CPO();
   //--- 
     bool               IsOpen(void);
     ENUM_ORD_TYPE      OrdType(void)       {return e_OrdTyp;     }
     datetime           GetOpnTime(void)    {return dt_OpenTime;  }
     uint               GetTicket(void)     {return i_Ticket;     }
     int                Tick(double price, double spread);
     void               CloseVirtPO();
     void               TimeClosePO(datetime crtime); 
  };
  
//+------------------------------------------------------------------+
//|      Constructor                                                 |
//+------------------------------------------------------------------+
CPO::CPO(string symbol,  ENUM_ORD_TYPE stype,int sslpg, double slots, datetime opntime,int magicNm, 
         string comment,double thrshld_price,double stop_loss,double tk_profit, double lifetime) 
  {
   i_Ticket       =  0;
   s_Symbol       =  symbol;
   d_Point        =  SymbolInfoDouble(s_Symbol,SYMBOL_POINT);
   dt_OpenTime    =  opntime;
   d_ThrshldPrice =  thrshld_price;
   e_OrdTyp       =  stype;
   d_SL           =  stop_loss;  // pips
   d_TP           =  tk_profit; // pips
   i_slpg         =  sslpg;
   d_Lots         =  slots;
   i_MagicNum     =  magicNm;
   s_Cmt          =  comment;
   b_open         =  true;
   d_lifetime     =  lifetime; // minutes
   //
   i_Digits = int(MarketInfo(s_Symbol,MODE_DIGITS));
    d_Mult=1.0;
   if(i_Digits==5 || i_Digits==3)
     d_Mult=10.0;
  }
//+------------------------------------------------------------------+
//|       Destructor                                                 |
//+------------------------------------------------------------------+
CPO::~CPO()
  {
  }
//+------------------------------------------------------------------+
//|   check virt pend order to execute market order                  |
//+------------------------------------------------------------------+
 int CPO::Tick(double bidprice, double spread)
  {
  // spread in pips
  // compare threshold( bid) to bid price
  // return ticket # >0 if market order generated
//---
   i_Ticket = 0;
   d_bid = bidprice;                         // current bid
   d_ask = bidprice + spread*d_Mult*d_Point; // current ask
   switch(e_OrdTyp)
     {
      case ORD_TYPE_BUYLIMIT:
        if(d_bid<d_ThrshldPrice)
         {
          // make market Buy Order
          d_stp_loss =  NormalizeDouble(d_bid - d_SL*d_Mult*d_Point, i_Digits);
          d_tk_profit = NormalizeDouble(d_bid + d_TP*d_Mult*d_Point, i_Digits);
          i_Ticket=OrderSend(s_Symbol,OP_BUY,d_Lots,d_ask,i_slpg,d_stp_loss,d_tk_profit,s_Cmt,i_MagicNum,0,Green);
          b_open = false;     // close Pend order when have market order
          d_ThrshldPrice = 0.; // reset threshold
         }
        break;
      case ORD_TYPE_SELLLIMIT:
        if(d_bid>d_ThrshldPrice) 
         {
          // make market Sell Order
          d_stp_loss =  NormalizeDouble(d_ask + d_SL*d_Mult*d_Point, i_Digits);
          d_tk_profit = NormalizeDouble(d_ask - d_TP*d_Mult*d_Point, i_Digits);
          i_Ticket=OrderSend(s_Symbol,OP_SELL,d_Lots,d_bid,i_slpg,d_stp_loss,d_tk_profit,s_Cmt,i_MagicNum,0,Green);
          b_open = false;           // close Pend order when have market order
          d_ThrshldPrice = 9999.;  // reset threshold
         }
        
        break;
      case ORD_TYPE_BUYSTOP:
        if(d_bid>d_ThrshldPrice) 
        {
       // make market Buy Order
         d_stp_loss =  NormalizeDouble(d_bid - d_SL*d_Mult*d_Point, i_Digits);
         d_tk_profit = NormalizeDouble(d_bid + d_TP*d_Mult*d_Point, i_Digits);
         i_Ticket=OrderSend(s_Symbol,OP_BUY,d_Lots,d_ask,i_slpg,d_stp_loss,d_tk_profit,s_Cmt,i_MagicNum,0,Green);
         b_open = false;          // close Pend order when have market order
         d_ThrshldPrice = 9999.;
        }
        break;
      case ORD_TYPE_SELLSTOP:
        if(d_bid<d_ThrshldPrice)
         {
         // make market Sell Order
          d_stp_loss =  NormalizeDouble(d_ask + d_SL*d_Mult*d_Point, i_Digits);
          d_tk_profit = NormalizeDouble(d_ask - d_TP*d_Mult*d_Point, i_Digits);
          i_Ticket=OrderSend(s_Symbol,OP_SELL,d_Lots,d_bid,i_slpg,d_stp_loss,d_tk_profit,s_Cmt,i_MagicNum,0,Green);
          b_open = false;         // close Pend order when have market order
          d_ThrshldPrice = 0.;
         }
        break;
     }
   return (i_Ticket);
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|   Set close flag for virt pending order                          |
//+------------------------------------------------------------------+
void CPO::CloseVirtPO(void)
  {
  // disable virtual pending order by resetting threshold outside of possible price range
    if(e_OrdTyp== ORD_TYPE_BUYSTOP || e_OrdTyp==ORD_TYPE_SELLLIMIT) d_ThrshldPrice = 9999.;
    if(e_OrdTyp== ORD_TYPE_SELLSTOP || e_OrdTyp==ORD_TYPE_BUYLIMIT) d_ThrshldPrice =  0.;
    b_open = false; 
    return;
  }
 //------------------------------------------------------------------
//+------------------------------------------------------------------+
//|   Set close flag for virt pending order based on expiration time |
//+------------------------------------------------------------------+
 
  void CPO::TimeClosePO(datetime crtime)
   {
    if(crtime > (dt_OpenTime+d_lifetime*60.) )  // convert to seconds
     {
      if(e_OrdTyp== ORD_TYPE_BUYSTOP || e_OrdTyp==ORD_TYPE_SELLLIMIT) d_ThrshldPrice = 9999.;
      if(e_OrdTyp== ORD_TYPE_SELLSTOP || e_OrdTyp==ORD_TYPE_BUYLIMIT) d_ThrshldPrice =  0.;
      b_open = false; 
     }  
     return; 
   }
  //------------------------------------------------------------------
//+------------------------------------------------------------------+
//|   Return close flag for virt pending                             |
//+------------------------------------------------------------------+  
  bool CPO::IsOpen(void)
  {
   return (b_open);
  }
