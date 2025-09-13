//+------------------------------------------------------------------+
//|                                             HistoryOrderInfo.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
#include "SymbolInfo.mqh"
//+------------------------------------------------------------------+
//| Class CHistoryOrderInfo.                                         |
//| Appointment: Class for access to history order info.             |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class CHistoryOrderInfo : public CObject
  {
protected:
   ulong             m_ticket;             // ticket of history order
public:
                     CHistoryOrderInfo(void);
                    ~CHistoryOrderInfo(void);
   //--- methods of access to protected data
   void              Ticket(const ulong ticket) { m_ticket=ticket;  }
   ulong             Ticket(void)         const { return(m_ticket); }
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
   long              PositionId(void) const { return(OrderTicket()); }
   //--- fast access methods to the double order propertyes
   double            VolumeInitial(void) const { return(OrderLots()); }
   double            VolumeCurrent(void) const { return(OrderLots()); }
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
   //--- method for select history order
   bool              SelectByIndex(const int index);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CHistoryOrderInfo::CHistoryOrderInfo(void) : m_ticket(0)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CHistoryOrderInfo::~CHistoryOrderInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TIME_SETUP_MSC"                    |
//+------------------------------------------------------------------+
ulong CHistoryOrderInfo::TimeSetupMsc(void) const
  {
//   return(OrderGetInteger(ORDER_TIME_SETUP_MSC));
   return(ULONG_MAX);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TIME_DONE_MSC"                     |
//+------------------------------------------------------------------+
ulong CHistoryOrderInfo::TimeDoneMsc(void) const
  {
//   return(OrderGetInteger(ORDER_TIME_DONE_MSC));
   return(ULONG_MAX);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE" as string                    |
//+------------------------------------------------------------------+
string CHistoryOrderInfo::TypeDescription(void) const
  {
   string str;
//---
   return(FormatType(str,OrderType()));
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_STATE"                             |
//+------------------------------------------------------------------+
ENUM_ORDER_STATE CHistoryOrderInfo::State(void) const
  {
   return(ORDER_STATE_FILLED);
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_STATE" as string                   |
//+------------------------------------------------------------------+
string CHistoryOrderInfo::StateDescription(void) const
  {
   string str;
//---
   return(FormatStatus(str,State()));
  }
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_FILLING"                      |
//+------------------------------------------------------------------+
/*ENUM_ORDER_TYPE_FILLING CHistoryOrderInfo::TypeFilling(void) const
  {
   return((ENUM_ORDER_TYPE_FILLING)HistoryOrderGetInteger(m_ticket,ORDER_TYPE_FILLING));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_FILLING" as string            |
//+------------------------------------------------------------------+
/*string CHistoryOrderInfo::TypeFillingDescription(void) const
  {
   string str;
//---
   return(FormatTypeFilling(str,TypeFilling()));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_TIME"                         |
//+------------------------------------------------------------------+
/*ENUM_ORDER_TYPE_TIME CHistoryOrderInfo::TypeTime(void) const
  {
   return((ENUM_ORDER_TYPE_TIME)HistoryOrderGetInteger(m_ticket,ORDER_TYPE_TIME));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_TYPE_TIME" as string               |
//+------------------------------------------------------------------+
/*string CHistoryOrderInfo::TypeTimeDescription(void) const
  {
   string str;
//---
   return(FormatTypeTime(str,TypeTime()));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "ORDER_PRICE_CURRENT"                     |
//+------------------------------------------------------------------+
double CHistoryOrderInfo::PriceCurrent(void) const
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
double CHistoryOrderInfo::PriceStopLimit(void) const
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Access functions OrderGetInteger(...)                            |
//+------------------------------------------------------------------+
/*bool CHistoryOrderInfo::InfoInteger(const ENUM_ORDER_PROPERTY_INTEGER prop_id,long &var) const
  {
   return(HistoryOrderGetInteger(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions OrderGetDouble(...)                             |
//+------------------------------------------------------------------+
/*bool CHistoryOrderInfo::InfoDouble(const ENUM_ORDER_PROPERTY_DOUBLE prop_id,double &var) const
  {
   return(HistoryOrderGetDouble(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions OrderGetString(...)                             |
//+------------------------------------------------------------------+
/*bool CHistoryOrderInfo::InfoString(const ENUM_ORDER_PROPERTY_STRING prop_id,string &var) const
  {
   return(HistoryOrderGetString(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Converts the order type to text                                  |
//+------------------------------------------------------------------+
string CHistoryOrderInfo::FormatType(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_TYPE_BUY            : str="buy";        break;
      case ORDER_TYPE_SELL           : str="sell";       break;
      case ORDER_TYPE_BUY_LIMIT      : str="buy limit";  break;
      case ORDER_TYPE_SELL_LIMIT     : str="sell limit"; break;
      case ORDER_TYPE_BUY_STOP       : str="buy stop";   break;
      case ORDER_TYPE_SELL_STOP      : str="sell stop";  break;

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
string CHistoryOrderInfo::FormatStatus(string &str,const uint status) const
  {
//--- clean
   str="";
//--- see the type
   switch(status)
     {
      case ORDER_STATE_STARTED : str="started";  break;
      case ORDER_STATE_PLACED  : str="placed";   break;
      case ORDER_STATE_CANCELED: str="canceled"; break;
      case ORDER_STATE_PARTIAL : str="partial";  break;
      case ORDER_STATE_FILLED  : str="filled";   break;
      case ORDER_STATE_REJECTED: str="rejected"; break;
      case ORDER_STATE_EXPIRED : str="expired";  break;

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
string CHistoryOrderInfo::FormatTypeFilling(string &str,const uint type) const
  {
//--- return the result
   return(str="fill or kill");
  }
//+------------------------------------------------------------------+
//| Converts the type of order by expiration to text                 |
//+------------------------------------------------------------------+
string CHistoryOrderInfo::FormatTypeTime(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case ORDER_TIME_GTC          : str="gtc";           break;
      case ORDER_TIME_DAY          : str="day";           break;
      case ORDER_TIME_SPECIFIED    : str="specified";     break;
      case ORDER_TIME_SPECIFIED_DAY: str="specified day"; break;

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
string CHistoryOrderInfo::FormatOrder(string &str) const
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
string CHistoryOrderInfo::FormatPrice(string &str,const double price_order,const double price_trigger,const uint digits) const
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
//| Select a history order on the index                              |
//+------------------------------------------------------------------+
bool CHistoryOrderInfo::SelectByIndex(const int index)
  {
   if(!OrderSelect(index,SELECT_BY_POS))
      return(false);
   ulong ticket=OrderTicket();
   if(ticket==0)
      return(false);
   Ticket(ticket);
//---
   return(true);
  }
//+------------------------------------------------------------------+
