//+------------------------------------------------------------------+
//|                                                 PositionInfo.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
#include "SymbolInfo.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_POSITION_TYPE
  {
   POSITION_TYPE_BUY =OP_BUY,
   POSITION_TYPE_SELL=OP_SELL
  };
//+------------------------------------------------------------------+
//| Class CPositionInfo.                                             |
//| Appointment: Class for access to position info.                  |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class CPositionInfo : public CObject
  {
protected:
   ENUM_POSITION_TYPE m_type;
   double            m_volume;
   double            m_price;
   double            m_stop_loss;
   double            m_take_profit;

public:
                     CPositionInfo(void);
                    ~CPositionInfo(void);
   //--- fast access methods to the integer position propertyes
   datetime          Time(void) const { return(OrderOpenTime()); }
   ulong             TimeMsc(void) const;
   datetime          TimeUpdate(void) const;
   ulong             TimeUpdateMsc(void) const;
   ENUM_POSITION_TYPE PositionType(void) const { return((ENUM_POSITION_TYPE)OrderType()); }
   string            TypeDescription(void) const;
   long              Magic(void) const { return(OrderMagicNumber()); }
   long              Identifier(void) const { return(OrderTicket()); }
   //--- fast access methods to the double position propertyes
   double            Volume(void) const { return(OrderLots()); }
   double            PriceOpen(void) const { return(OrderOpenPrice()); }
   double            StopLoss(void) const { return(OrderStopLoss()); }
   double            TakeProfit(void) const { return(OrderTakeProfit()); }
   double            PriceCurrent(void) const;
   double            Commission(void) const { return(OrderCommission()); }
   double            Swap(void) const { return(OrderSwap()); }
   double            Profit(void) const { return(OrderProfit()); }
   //--- fast access methods to the string position propertyes
   string            Symbol(void) const { return(OrderSymbol()); }
   string            Comment(void) const { return(OrderComment()); }
   //--- access methods to the API MQL5 functions
//   bool              InfoInteger(const ENUM_POSITION_PROPERTY_INTEGER prop_id,long &var) const;
//   bool              InfoDouble(const ENUM_POSITION_PROPERTY_DOUBLE prop_id,double &var) const;
//   bool              InfoString(const ENUM_POSITION_PROPERTY_STRING prop_id,string &var) const;
   //--- info methods
   string            FormatType(string &str,const uint type) const;
   string            FormatPosition(string &str) const;
   //--- methods for select position
   bool              Select(const string symbol);
   bool              SelectByIndex(const int index);
   //---
   void              StoreState(void);
   bool              CheckState(void);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CPositionInfo::CPositionInfo(void) : //m_type(WRONG_VALUE),
                                     m_type(0),
                                     m_volume(0.0),
                                     m_price(0.0),
                                     m_stop_loss(0.0),
                                     m_take_profit(0.0)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CPositionInfo::~CPositionInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the property value "POSITION_TIME_MSC"                       |
//+------------------------------------------------------------------+
ulong CPositionInfo::TimeMsc(void) const
  {
//   return((datetime)PositionGetInteger(POSITION_TIME_MSC));
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "POSITION_TIME_UPDATE"                    |
//+------------------------------------------------------------------+
datetime CPositionInfo::TimeUpdate(void) const
  {
//   return((datetime)PositionGetInteger(POSITION_TIME_UPDATE));
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "POSITION_TIME_UPDATE_MSC"                |
//+------------------------------------------------------------------+
ulong CPositionInfo::TimeUpdateMsc(void) const
  {
//   return((datetime)PositionGetInteger(POSITION_TIME_UPDATE_MSC));
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "POSITION_TYPE" as string                 |
//+------------------------------------------------------------------+
string CPositionInfo::TypeDescription(void) const
  {
   string str;
//---
   return(FormatType(str,PositionType()));
  }
//+------------------------------------------------------------------+
//| Get the property value "POSITION_PRICE_CURRENT"                  |
//+------------------------------------------------------------------+
double CPositionInfo::PriceCurrent(void) const
  {
   ENUM_POSITION_TYPE type=(ENUM_POSITION_TYPE)OrderType();
//---
   if(type==POSITION_TYPE_BUY)
      return(MarketInfo(Symbol(),MODE_ASK));
   if(type==POSITION_TYPE_SELL)
      return(MarketInfo(Symbol(),MODE_BID));
//---
   return(0);
  }
//+------------------------------------------------------------------+
//| Access functions PositionGetInteger(...)                         |
//+------------------------------------------------------------------+
/*bool CPositionInfo::InfoInteger(const ENUM_POSITION_PROPERTY_INTEGER prop_id,long &var) const
  {
   return(PositionGetInteger(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions PositionGetDouble(...)                          |
//+------------------------------------------------------------------+
/*bool CPositionInfo::InfoDouble(const ENUM_POSITION_PROPERTY_DOUBLE prop_id,double &var) const
  {
   return(PositionGetDouble(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions PositionGetString(...)                          |
//+------------------------------------------------------------------+
/*bool CPositionInfo::InfoString(const ENUM_POSITION_PROPERTY_STRING prop_id,string &var) const
  {
   return(PositionGetString(prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Converts the position type to text                               |
//+------------------------------------------------------------------+
string CPositionInfo::FormatType(string &str,const uint type) const
  {
//--- clean
   str="";
//--- see the type
   switch(type)
     {
      case POSITION_TYPE_BUY : str="buy";  break;
      case POSITION_TYPE_SELL: str="sell"; break;
      default                : str="unknown position type "+(string)type;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the position parameters to text                         |
//+------------------------------------------------------------------+
string CPositionInfo::FormatPosition(string &str) const
  {
   string      tmp,type;
   CSymbolInfo symbol;
//--- set up
   symbol.Name(Symbol());
   int digits=symbol.Digits();
//--- form the position description
   str=StringFormat("%s %s %s %s",
                    FormatType(type,PositionType()),
                    DoubleToString(Volume(),2),
                    Symbol(),
                    DoubleToString(PriceOpen(),digits+3));
//--- add stops if there are any
   double sl=StopLoss();
   double tp=TakeProfit();
   if(sl!=0.0)
     {
      tmp=StringFormat(" sl: %s",DoubleToString(sl,digits));
      str+=tmp;
     }
   if(tp!=0.0)
     {
      tmp=StringFormat(" tp: %s",DoubleToString(tp,digits));
      str+=tmp;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Access functions PositionSelect(...)                             |
//+------------------------------------------------------------------+
bool CPositionInfo::Select(const string symbol)
  {
//   return(PositionSelect(symbol));
   return(false);
  }
//+------------------------------------------------------------------+
//| Select a position on the index                                   |
//+------------------------------------------------------------------+
bool CPositionInfo::SelectByIndex(const int index)
  {
   return(OrderSelect(index,SELECT_BY_POS));
  }
//+------------------------------------------------------------------+
//| Stored position's current state                                  |
//+------------------------------------------------------------------+
void CPositionInfo::StoreState(void)
  {
   m_type       =PositionType();
   m_volume     =Volume();
   m_price      =PriceOpen();
   m_stop_loss  =StopLoss();
   m_take_profit=TakeProfit();
  }
//+------------------------------------------------------------------+
//| Check position change                                            |
//+------------------------------------------------------------------+
bool CPositionInfo::CheckState(void)
  {
   if(m_type==PositionType() && 
      m_volume==Volume() && 
      m_price==PriceOpen() && 
      m_stop_loss==StopLoss() && 
      m_take_profit==TakeProfit())
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
