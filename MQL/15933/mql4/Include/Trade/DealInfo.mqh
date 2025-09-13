//+------------------------------------------------------------------+
//|                                                     DealInfo.mqh |
//|                   Copyright 2009-2013, MetaQuotes Software Corp. |
//|                                              http://www.mql4.com |
//+------------------------------------------------------------------+
#include <Object.mqh>
#include "SymbolInfo.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_DEAL_TYPE
  {
   DEAL_TYPE_BUY =OP_BUY,
   DEAL_TYPE_SELL=OP_SELL
  };
//+------------------------------------------------------------------+
//| Class CDealInfo.                                                 |
//| Appointment: Class for access to history deal info.              |
//|              Derives from class CObject.                         |
//+------------------------------------------------------------------+
class CDealInfo : public CObject
  {
protected:
//   ulong             m_ticket;             // ticket of history order

public:
                     CDealInfo(void);
                    ~CDealInfo(void);
   //--- methods of access to protected data
//   void              Ticket(const ulong ticket)   { m_ticket=ticket;  }
   ulong             Ticket(void) const { return(OrderTicket()); }
   //--- fast access methods to the integer position propertyes
   long              Order(void) const { return(OrderTicket()); }
   datetime          Time(void) const { return(OrderOpenTime()); }
   ulong             TimeMsc(void) const;
   ENUM_DEAL_TYPE    DealType(void) const { return((ENUM_DEAL_TYPE)OrderType()); }
   string            TypeDescription(void) const;
//   ENUM_DEAL_ENTRY   Entry(void) const;
//   string            EntryDescription(void) const;
   long              Magic(void) const { return(OrderMagicNumber()); }
   long              PositionId(void) const { return(OrderTicket()); }
   //--- fast access methods to the double position propertyes
   double            Volume(void) const { return(OrderLots()); }
   double            Price(void) const { return(OrderOpenPrice()); }
   double            Commission(void) const { return(OrderCommission()); }
   double            Swap(void) const { return(OrderSwap()); }
   double            Profit(void) const { return(OrderProfit()); }
   //--- fast access methods to the string position propertyes
   string            Symbol(void) const { return(OrderSymbol()); }
   string            Comment(void) const { return(OrderComment()); }
   //--- access methods to the API MQL5 functions
//   bool              InfoInteger(ENUM_DEAL_PROPERTY_INTEGER prop_id,long &var) const;
//   bool              InfoDouble(ENUM_DEAL_PROPERTY_DOUBLE prop_id,double &var) const;
//   bool              InfoString(ENUM_DEAL_PROPERTY_STRING prop_id,string &var) const;
   //--- info methods
   string            FormatAction(string &str,const uint action) const;
   string            FormatEntry(string &str,const uint entry) const;
   string            FormatDeal(string &str) const;
   //--- method for select deal
   bool              SelectByIndex(const int index);
  };
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CDealInfo::CDealInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CDealInfo::~CDealInfo(void)
  {
  }
//+------------------------------------------------------------------+
//| Get the property value "DEAL_TIME_MSC"                           |
//+------------------------------------------------------------------+
ulong CDealInfo::TimeMsc(void) const
  {
//   return(HistoryDealGetInteger(m_ticket,DEAL_TIME_MSC));
   return(0);
  }
//+------------------------------------------------------------------+
//| Get the property value "DEAL_TYPE" as string                     |
//+------------------------------------------------------------------+
string CDealInfo::TypeDescription(void) const
  {
   string str;
//---
   switch(DealType())
     {
      case DEAL_TYPE_BUY       : str="Buy type";        break;
      case DEAL_TYPE_SELL      : str="Sell type";       break;
//      case DEAL_TYPE_BALANCE   : str="Balance type";    break;
//      case DEAL_TYPE_CREDIT    : str="Credit type";     break;
//      case DEAL_TYPE_CHARGE    : str="Charge type";     break;
//      case DEAL_TYPE_CORRECTION: str="Correction type"; break;
      default                  : str="Unknown type";
     }
//---
   return(str);
  }
//+------------------------------------------------------------------+
//| Get the property value "DEAL_ENTRY"                              |
//+------------------------------------------------------------------+
/*ENUM_DEAL_ENTRY CDealInfo::Entry(void) const
  {
   return((ENUM_DEAL_ENTRY)HistoryDealGetInteger(m_ticket,DEAL_ENTRY));
  }*/
//+------------------------------------------------------------------+
//| Get the property value "DEAL_ENTRY" as string                    |
//+------------------------------------------------------------------+
/*string CDealInfo::EntryDescription(void) const
  {
   string str;
//---
   switch(Entry())
     {
      case DEAL_ENTRY_IN   : str="In entry";      break;
      case DEAL_ENTRY_OUT  : str="Out entry";     break;
      case DEAL_ENTRY_INOUT: str="InOut entry";   break;
      case DEAL_ENTRY_STATE: str="Status record"; break;
      default              : str="Unknown entry";
     }
//---
   return(str);
  }*/
//+------------------------------------------------------------------+
//| Access functions HistoryDealGetInteger(...)                      |
//+------------------------------------------------------------------+
/*bool CDealInfo::InfoInteger(ENUM_DEAL_PROPERTY_INTEGER prop_id,long &var) const
  {
   return(HistoryDealGetInteger(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions HistoryDealGetDouble(...)                       |
//+------------------------------------------------------------------+
/*bool CDealInfo::InfoDouble(ENUM_DEAL_PROPERTY_DOUBLE prop_id,double &var) const
  {
   return(HistoryDealGetDouble(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Access functions HistoryDealGetString(...)                       |
//+------------------------------------------------------------------+
/*bool CDealInfo::InfoString(ENUM_DEAL_PROPERTY_STRING prop_id,string &var) const
  {
   return(HistoryDealGetString(m_ticket,prop_id,var));
  }*/
//+------------------------------------------------------------------+
//| Converths the type of a  deal to text                            |
//+------------------------------------------------------------------+
string CDealInfo::FormatAction(string &str,const uint action) const
  {
//--- clean
   str="";
//--- see the type  
   switch(action)
     {
      case DEAL_TYPE_BUY       : str="buy";        break;
      case DEAL_TYPE_SELL      : str="sell";       break;
//      case DEAL_TYPE_BALANCE   : str="balance";    break;
//      case DEAL_TYPE_CREDIT    : str="credit";     break;
//      case DEAL_TYPE_CHARGE    : str="charge";     break;
//      case DEAL_TYPE_CORRECTION: str="correction"; break;
      default                  : str="unknown deal type "+(string)action;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the deal direction to text                              |
//+------------------------------------------------------------------+
string CDealInfo::FormatEntry(string &str,const uint entry) const
  {
//--- clean
   str="";
//--- see the type
   switch(entry)
     {
//      case DEAL_ENTRY_IN   : str="in";     break;
//      case DEAL_ENTRY_OUT  : str="out";    break;
//      case DEAL_ENTRY_INOUT: str="in/out"; break;
      default              : str="unknown deal entry "+(string)entry;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Converts the deal parameters to text                             |
//+------------------------------------------------------------------+
string CDealInfo::FormatDeal(string &str) const
  {
   string      type;
   CSymbolInfo symbol;
//--- set up
   symbol.Name(Symbol());
   int digits=symbol.Digits();
//--- form the description of the deal
   switch(DealType())
     {
      //--- Buy-Sell
      case DEAL_TYPE_BUY       :
      case DEAL_TYPE_SELL      :
         str=StringFormat("#%I64u %s %s %s at %s",
                          Ticket(),
                          FormatAction(type,DealType()),
                          DoubleToString(Volume(),2),
                          Symbol(),
                          DoubleToString(Price(),digits));
      break;

      //--- balance operations
/*      case DEAL_TYPE_BALANCE   :
      case DEAL_TYPE_CREDIT    :
      case DEAL_TYPE_CHARGE    :
      case DEAL_TYPE_CORRECTION:
         str=StringFormat("#%I64u %s %s [%s]",
                          Ticket(),
                          FormatAction(type,DealType()),
                          DoubleToString(Profit(),2),
                          Comment());
      break;*/

      default:
         str="unknown deal type "+(string)DealType();
         break;
     }
//--- return the result
   return(str);
  }
//+------------------------------------------------------------------+
//| Select a deal on the index                                       |
//+------------------------------------------------------------------+
bool CDealInfo::SelectByIndex(const int index)
  {
   return(OrderSelect(index,SELECT_BY_POS));
  }
//+------------------------------------------------------------------+
