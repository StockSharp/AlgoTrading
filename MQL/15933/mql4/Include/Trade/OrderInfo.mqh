//+------------------------------------------------------------------+
//|                                                    OrderInfo.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
#include "SymbolInfo.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_ORDER_STATE
  {
   ORDER_STATE_STARTED,
   ORDER_STATE_PLACED,
   ORDER_STATE_CANCELED,
   ORDER_STATE_PARTIAL,
   ORDER_STATE_FILLED,
   ORDER_STATE_REJECTED,
   ORDER_STATE_EXPIRED,
   ORDER_STATE_REQUEST_ADD,
   ORDER_STATE_REQUEST_MODIFY,
   ORDER_STATE_REQUEST_CANCEL
  };
//+------------------------------------------------------------------+
//| Class COrderInfo.                                                |
//| Appointment: Class for access to order info.                     |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class COrderInfo : public CObject
  {
protected:
   ulong             m_ticket;
   ENUM_ORDER_TYPE   m_type;
   ENUM_ORDER_STATE  m_state;
   datetime          m_expiration;
   double            m_volume_curr;
   double            m_stop_loss;
   double            m_take_profit;

public:
                     COrderInfo(void);
                    ~COrderInfo(void);
   //--- methods of access to protected data
   ulong             Ticket(void) const { return(m_ticket); }
   //--- fast access methods to the integer order propertyes
   datetime          TimeSetup(void) const { return(OrderOpenTime()); }
   ulong             TimeSetupMsc(void) const;
   datetime          TimeDone(void) const { return(OrderCloseTime()); }
   ulong             TimeDoneMsc(void) const;
   ENUM_ORDER_TYPE   OrderType(void) const { return(OrderType()); }
   string            TypeDescription(void) const;
   ENUM_ORDER_STATE  State(void) const;
   string            StateDescription(void) const;
   datetime          TimeExpiration(void) const { return(OrderExpiration()); }
//   ENUM_ORDER_TYPE_FILLING TypeFilling(void) const;
//   string            TypeFillingDescription(void) const;
//   ENUM_ORDER_TYPE_TIME TypeTime(void) const;
//   string            TypeTimeDescription(void) const;
   int               Magic(void) const { return(OrderMagicNumber()); }
   long              PositionId(void) const;
   //--- fast access methods to the double order propertyes
   double            VolumeInitial(void) const { return(OrderLots()); }
   double            VolumeCurrent(void) const;
   double            PriceOpen(void) const { return(OrderOpenPrice()); }
   double            StopLoss(void) const { return(OrderStopLoss()); }
   double            TakeProfit(void) const { return(OrderTakeProfit()); }
   double            PriceCurrent(void) const;
   double            PriceStopLimit(void) const;
   //--- fast access methods to the string order propertyes
   string            Symbol(void) const { return(OrderSymbol()); }
   string            Comment(void) const { return(OrderComment()); }
   //--- access methods to the API MQL5 functions
//   bool              InfoInteger(const ENUM_ORDER_PROPERTY_INTEGER prop_id,long &var) const;
//   bool              InfoDouble(const ENUM_ORDER_PROPERTY_DOUBLE prop_id,double &var) const;
//   bool              InfoString(const ENUM_ORDER_PROPERTY_STRING prop_id,string &var) const;
   //--- info methods
   string            FormatType(string &str,const uint type) const;
   string            FormatStatus(string &str,const uint status) const;
   string            FormatTypeFilling(string &str,const uint type) const;
   string            FormatTypeTime(string &str,const uint type) const;
   string            FormatOrder(string &str) const;
   string            FormatPrice(string &str,const double price_order,const double price_trigger,const uint digits) const;
   //--- method for select order
   bool              Select(void);
   bool              Select(const ulong ticket);
   bool              SelectByIndex(const int index);
   //--- addition methods
   void              StoreState(void);
   bool              CheckState(void);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
COrderInfo::COrderInfo(void) : m_ticket(ULONG_MAX),
//                               m_type(WRONG_VALUE),
                               m_type(0),
//                               m_state(WRONG_VALUE),
                               m_state(0),
                               m_expiration(0),
                               m_volume_curr(0.0),
                               m_stop_loss(0.0),
                               m_take_profit(0.0)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
COrderInfo::~COrderInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TIME_SETUP_MSC"                    |
//+------------------------------------------------------------------+
ulong COrderInfo::TimeSetupMsc(void) const
  {
//   return(OrderOpenTime());  // ???
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TIME_DONE_MSC"                     |
//+------------------------------------------------------------------+
ulong COrderInfo::TimeDoneMsc(void) const
  {
//   return(OrderCloseTime());  // ???
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE" as string                    |
//+------------------------------------------------------------------+
string COrderInfo::TypeDescription(void) const
  {
   string str;
//---
   return(FormatType(str,OrderType()));
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_STATE"                             |
//+------------------------------------------------------------------+
ENUM_ORDER_STATE COrderInfo::State(void) const
  {
   return(ORDER_STATE_PLACED);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_STATE" as string                   |
//+------------------------------------------------------------------+
string COrderInfo::StateDescription(void) const
  {
   string str;
//---
   return(FormatStatus(str,State()));
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_FILLING"                      |
//+------------------------------------------------------------------+
/*ENUM_ORDER_TYPE_FILLING COrderInfo::TypeFilling(void) const
  {
   return((ENUM_ORDER_TYPE_FILLING)OrderGetInteger(ORDER_TYPE_FILLING));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_FILLING" as string            |
//+------------------------------------------------------------------+
/*string COrderInfo::TypeFillingDescription(void) const
  {
   string str;
//---
   return(FormatTypeFilling(str,TypeFilling()));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_TIME"                         |
//+------------------------------------------------------------------+
/*ENUM_ORDER_TYPE_TIME COrderInfo::TypeTime(void) const
  {
   return((ENUM_ORDER_TYPE_TIME)OrderGetInteger(ORDER_TYPE_TIME));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_TIME" as string               |
//+------------------------------------------------------------------+
/*string COrderInfo::TypeTimeDescription(void) const
  {
   string str;
//---
   return(FormatTypeTime(str,TypeFilling()));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_POSITION_ID"                       |
//+------------------------------------------------------------------+
long COrderInfo::PositionId(void) const
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_VOLUME_CURRENT"                    |
//+------------------------------------------------------------------+
double COrderInfo::VolumeCurrent(void) const
  {
   return(OrderLots());
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_PRICE_CURRENT"                     |
//+------------------------------------------------------------------+
double COrderInfo::PriceCurrent(void) const
  {
   ENUM_ORDER_TYPE type=OrderType();
//---
   if(type==ORDER_TYPE_BUY || type==ORDER_TYPE_BUY_LIMIT || type==ORDER_TYPE_BUY_STOP)
      return(MarketInfo(Symbol(),MODE_ASK));
//---
   return(MarketInfo(Symbol(),MODE_BID));
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_PRICE_STOPLIMIT"                   |
//+------------------------------------------------------------------+
double COrderInfo::PriceStopLimit(void) const
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Access functions OrderGetInteger(...)                            |
//+------------------------------------------------------------------+
/*bool COrderInfo::InfoInteger(const ENUM_ORDER_PROPERTY_INTEGER prop_id,long &var) const
  {
   return(OrderGetInteger(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions OrderGetDouble(...)                             |
//+------------------------------------------------------------------+
/*bool COrderInfo::InfoDouble(const ENUM_ORDER_PROPERTY_DOUBLE prop_id,double &var) const
  {
   return(OrderGetDouble(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions OrderGetString(...)                             |
//+------------------------------------------------------------------+
/*bool COrderInfo::InfoString(const ENUM_ORDER_PROPERTY_STRING prop_id,string &var) const
  {
   return(OrderGetString(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Converts the order type to text                                  |
//+------------------------------------------------------------------+
string COrderInfo::FormatType(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_TYPE_BUY            : str="buy";             break;
      case ORDER_TYPE_SELL           : str="sell";            break;
      case ORDER_TYPE_BUY_LIMIT      : str="buy limit";       break;
      case ORDER_TYPE_SELL_LIMIT     : str="sell limit";      break;
      case ORDER_TYPE_BUY_STOP       : str="buy stop";        break;
      case ORDER_TYPE_SELL_STOP      : str="sell stop";       break;

      default:
         str="unknown order type "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order status to text                                |
//+------------------------------------------------------------------+
string COrderInfo::FormatStatus(string &str,const uint status) const
  {
//--- clean
   str="";
//--- see the type
   switch(status)
     {
      case ORDER_STATE_STARTED       : str="started";            break;
      case ORDER_STATE_PLACED        : str="placed";             break;
      case ORDER_STATE_CANCELED      : str="canceled";           break;
      case ORDER_STATE_PARTIAL       : str="partial";            break;
      case ORDER_STATE_FILLED        : str="filled";             break;
      case ORDER_STATE_REJECTED      : str="rejected";           break;
      case ORDER_STATE_EXPIRED       : str="expired";            break;
//      case ORDER_STATE_REQUEST_ADD   : str="request adding";     break;
//      case ORDER_STATE_REQUEST_MODIFY: str="request modifying";  break;
//      case ORDER_STATE_REQUEST_CANCEL: str="request cancelling"; break;

      default:
         str="unknown order status "+(string)status;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order filling type to text                          |
//+------------------------------------------------------------------+
string COrderInfo::FormatTypeFilling(string &str,const uint type) const
  {
//--- return the result
   return(str="fill or kill");
  }
//+------------------------------------------------------------------+
//| Converts the type of order by expiration to text                 |
//+------------------------------------------------------------------+
string COrderInfo::FormatTypeTime(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      default:
         str="unknown type time "+(string)type;
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order parameters to text                            |
//+------------------------------------------------------------------+
string COrderInfo::FormatOrder(string &str) const
  {
   string      type,price;
   CSymbolInfo symbol;
//--- set up
   symbol.Name(Symbol());
   int digits=symbol.Digits();
//--- form the order description
   str=StringFormat("#%I64u %s %s %s",
                    Ticket(),
                    FormatType(type,OrderType()),
                    DoubleToString(VolumeInitial(),2),
                    Symbol());
//--- receive the price of the order
   FormatPrice(price,PriceOpen(),PriceStopLimit(),digits);
//--- if there is price, write it
   if(price!="")
     {
      str+=" at ";
      str+=price;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the order prices to text                                |
//+------------------------------------------------------------------+
string COrderInfo::FormatPrice(string &str,const double price_order,const double price_trigger,const uint digits) const
  {
   string price,trigger;
//--- clean
   str="";
//--- Is there its trigger price?
   if(price_trigger)
     {
      price  =DoubleToString(price_order,digits);
      trigger=DoubleToString(price_trigger,digits);
      str    =StringFormat("%s (%s)",price,trigger);
     }
   else
      str=DoubleToString(price_order,digits);
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Selecting a order to access                                      |
//+------------------------------------------------------------------+
bool COrderInfo::Select(void)
  {
   return(OrderSelect((int)m_ticket,SELECT_BY_TICKET));
  }
//+------------------------------------------------------------------+
//| Selecting a order to access                                      |
//+------------------------------------------------------------------+
bool COrderInfo::Select(const ulong ticket)
  {
   if(OrderSelect((int)ticket,SELECT_BY_TICKET))
     {
      m_ticket=ticket;
      return(true);
     }
   m_ticket=ULONG_MAX;
//---
   return(false);
  }
//+------------------------------------------------------------------+
//| Select a order on the index                                      |
//+------------------------------------------------------------------+
bool COrderInfo::SelectByIndex(const int index)
  {
   ulong ticket=OrderTicket();
   if(ticket==0)
      return(false);
//---
   return(Select(ticket));
  }
//+------------------------------------------------------------------+
//| Stored order's current state                                     |
//+------------------------------------------------------------------+
void COrderInfo::StoreState(void)
  {
   m_type       =OrderType();
   m_state      =State();
   m_expiration =TimeExpiration();
   m_volume_curr=VolumeCurrent();
   m_stop_loss  =StopLoss();
   m_take_profit=TakeProfit();
  }
//+------------------------------------------------------------------+
//| Check order change                                               |
//+------------------------------------------------------------------+
bool COrderInfo::CheckState(void)
  {
   if(m_type==OrderType() && m_state==State() && 
      m_expiration ==TimeExpiration() &&
      m_volume_curr==VolumeCurrent() &&
      m_stop_loss==StopLoss() && 
      m_take_profit==TakeProfit())
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
