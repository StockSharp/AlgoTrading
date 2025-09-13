//+------------------------------------------------------------------+
//|                                            SignalsDemoDialog.mqh |
//|                   Copyright 2009-2014, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#include <Controls\Dialog.mqh>
#include <Controls\Button.mqh>
#include <Controls\Edit.mqh>
#include <Controls\ListView.mqh>
#include <Controls\Label.mqh>
//+------------------------------------------------------------------+
//| struct SSignalBaseInfo                                           |
//+------------------------------------------------------------------+
struct SSignalBaseInfo
  {
   long              id;             // signal id
   long              date_published; // date published
   long              date_started;   // monitoring srarting date
   long              leverage;       // leverage
   long              pips;           // profit in pips
   long              rating;         // MQ rating
   long              subscribers;    // number of subscribers
   long              trades;         // number of trades
   long              trade_mode;     // trade mode
   //---
   double            balance;        // balance
   double            equity;         // equity
   double            gain;           // gain
   double            max_drawdown;   // maximum drawdown
   double            price;          // signal price
   double            roi;            // return on investment
   //---
   string            author_login;   // author login at mql5.com
   string            name;           // signal name
   string            broker_name;    // broker name
   string            broker_server;  // broker server
  };
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
#define BUTTON_WIDTH                        (100)     // size by X coordinate
#define BUTTON_HEIGHT                       (20)      // size by Y coordinate
#define EDIT_HEIGHT                         (20)      // size by Y coordinate
//+------------------------------------------------------------------+
//| Class CSignalsDemoDialog                                         |
//| Usage: main dialog of the Signals Demo application               |
//+------------------------------------------------------------------+
class CSignalsDemoDialog : public CAppDialog
  {
private:
   //--- signals array from base
   SSignalBaseInfo   m_signals_array[];
   //--- labels
   CLabel            m_label_signal_info_name;
   CLabel            m_label_signal_info_equity_limit;
   CLabel            m_label_signal_info_slippage;
   CLabel            m_label_signal_info_deposit_percent;
   //--- label fields of signal copyn setting
   CLabel            m_label_signal_info_equity_limit2;
   CLabel            m_label_signal_info_slippage2;
   CLabel            m_label_signal_info_deposit_percent2;
   //-- edit fields for signal copy settings
   CEdit             m_edit_signal_info_name;
   CEdit             m_edit_signal_info_equity_limit;
   CEdit             m_edit_signal_info_slippage;
   CEdit             m_edit_signal_info_deposit_percent;
   //--- labels for MQL5 balance
   CLabel            m_label_MQL5balance;
   CLabel            m_label_MQL5balance_info;
   //--- labels for signal properties
   CLabel            m_label_id;
   CLabel            m_label_date_published;
   CLabel            m_label_date_started;
   CLabel            m_label_leverage;
   CLabel            m_label_pips;
   CLabel            m_label_rating;
   CLabel            m_label_subscribers;
   CLabel            m_label_trades;
   CLabel            m_label_trade_mode;
   CLabel            m_label_balance;
   CLabel            m_label_equity;
   CLabel            m_label_gain;
   CLabel            m_label_max_drawdown;
   CLabel            m_label_price;
   CLabel            m_label_roi;
   CLabel            m_label_author_login;
   CLabel            m_label_name;
   CLabel            m_label_broker_name;
   CLabel            m_label_broker_server;
   //--- edits for signal properties
   CEdit             m_edit_id;
   CEdit             m_edit_date_published;
   CEdit             m_edit_date_started;
   CEdit             m_edit_leverage;
   CEdit             m_edit_pips;
   CEdit             m_edit_rating;
   CEdit             m_edit_subscribers;
   CEdit             m_edit_trades;
   CEdit             m_edit_trade_mode;
   CEdit             m_edit_balance;
   CEdit             m_edit_equity;
   CEdit             m_edit_gain;
   CEdit             m_edit_max_drawdown;
   CEdit             m_edit_price;
   CEdit             m_edit_roi;
   CEdit             m_edit_author_login;
   CEdit             m_edit_name;
   CEdit             m_edit_broker_name;
   CEdit             m_edit_broker_server;
   //--- buttons
   CButton           m_button_subscribe;              // the subcribe button object
   CButton           m_button_unsubscribe;            // the unsubcribe button object
   //--- signals listview
   CListView         m_signals_list_view;             // the list object
public:
                     CSignalsDemoDialog(void);
                    ~CSignalsDemoDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);

protected:
   //--- create dependent controls
   bool              PrepareLabel(CLabel &label,string name,int x,int y);
   bool              PrepareEdit(CEdit &edit,string name,int x,int y,int width,bool readonly);
   //--- create dependent controls
   bool              FillSignalsArray(void);
   bool              CreateSignalInfoEdits(void);
   //--- update info
   bool              UpdateSignalsListView(void);
   void              FillBaseSignalInfo(int index);
   void              FillCurrentSignalInfo();
   void              UpdateInfo(void);
   //---
   bool              CreateButtons(void);
   bool              CreateListView(void);
   //--- handlers of the dependent controls events
   void              OnClickButtonSubscribe(void);
   void              OnClickButtonUnsubscribe(void);
   void              OnChangeListView(void);
  };
//+------------------------------------------------------------------+
//| Event Handling                                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CSignalsDemoDialog)
ON_EVENT(ON_CLICK,m_button_subscribe,OnClickButtonSubscribe)
ON_EVENT(ON_CLICK,m_button_unsubscribe,OnClickButtonUnsubscribe)
ON_EVENT(ON_CHANGE,m_signals_list_view,OnChangeListView)
EVENT_MAP_END(CAppDialog)
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSignalsDemoDialog::CSignalsDemoDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CSignalsDemoDialog::~CSignalsDemoDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Create                                                           |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2)
  {
   if(!CAppDialog::Create(chart,name,subwin,x1,y1,x2,y2))
      return(false);
//--- create dependent controls
   if(!CreateButtons())
      return(false);
   if(!CreateListView())
      return(false);
   if(!CreateSignalInfoEdits())
      return(false);
//---    
   UpdateInfo();
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| PrepareLabel                                                     |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::PrepareLabel(CLabel &label,string name,int x,int y)
  {
   if(!label.Create(m_chart_id,m_name+"Label"+name,m_subwin,x,y,x+20,y+EDIT_HEIGHT))
      return(false);
   label.Text(name);
   if(!Add(label))
      return(false);
   return(true);
  }
//+------------------------------------------------------------------+
//| PrepareEdit                                                      |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::PrepareEdit(CEdit &edit,string name,int x,int y,int width,bool readonly)
  {
   if(!edit.Create(m_chart_id,m_name+"Edit"+name,m_subwin,x,y,x+width,y+EDIT_HEIGHT))
      return(false);
   if(!readonly)
     {
      if(!edit.ReadOnly(true))
         return(false);
     }
   if(!Add(edit))
      return(false);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Prepare labels and edits                                         |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::CreateSignalInfoEdits(void)
  {
   PrepareLabel(m_label_signal_info_name,"Signal name",10,10);
   PrepareLabel(m_label_signal_info_deposit_percent,"Use no more than",10,40);
   PrepareLabel(m_label_signal_info_equity_limit,"Stop if equity is less than",10,60);
   PrepareLabel(m_label_signal_info_slippage,"Deviation/Slippage",10,80);
//---
   PrepareLabel(m_label_signal_info_deposit_percent2,"% of deposit",280,40);
   PrepareLabel(m_label_signal_info_equity_limit2,"USD",280,60);
   PrepareLabel(m_label_signal_info_slippage2,"spreads",280,80);
//---
   PrepareLabel(m_label_MQL5balance,"MQL5 balance:",580,40);
   PrepareLabel(m_label_MQL5balance_info,"balance",670,40);
//---
   PrepareEdit(m_edit_signal_info_name,"Signal name",90,10,180,false);
//---
   PrepareEdit(m_edit_signal_info_deposit_percent,"Volume percent",170,40,100,true);
   PrepareEdit(m_edit_signal_info_equity_limit,"Equity limit",170,60,100,true);
   PrepareEdit(m_edit_signal_info_slippage,"Slippage",170,80,100,true);
//---
   PrepareLabel(m_label_id,"id",280,120);
   PrepareLabel(m_label_name,"Name",280,140);
   PrepareLabel(m_label_author_login,"Author login",280,160);
   PrepareLabel(m_label_broker_name,"Broker name",280,180);
   PrepareLabel(m_label_broker_server,"Broker server",280,200);
//---
   PrepareLabel(m_label_balance,"Balance",280,220);
   PrepareLabel(m_label_equity,"Equity",280,240);
   PrepareLabel(m_label_gain,"Gain",280,260);
   PrepareLabel(m_label_max_drawdown,"Max drawdown",280,280);
   PrepareLabel(m_label_price,"Price",280,300);
   PrepareLabel(m_label_roi,"Roi",280,320);
//---
   PrepareLabel(m_label_date_published,"Date published",580,120);
   PrepareLabel(m_label_date_started,"Date started",580,140);
   PrepareLabel(m_label_leverage,"Leverage",580,160);
   PrepareLabel(m_label_pips,"Pips",580,180);
   PrepareLabel(m_label_rating,"Rating",580,200);
   PrepareLabel(m_label_subscribers,"Subscribers",580,220);
   PrepareLabel(m_label_trades,"Trades",580,240);
   PrepareLabel(m_label_trade_mode,"Trade mode",580,260);
//---
   PrepareEdit(m_edit_id,"id",370,120,200,false);
   PrepareEdit(m_edit_name,"Name",370,140,200,false);
   PrepareEdit(m_edit_author_login,"Author login",370,160,200,false);
   PrepareEdit(m_edit_broker_name,"Broker name",370,180,200,false);
   PrepareEdit(m_edit_broker_server,"Broker server",370,200,200,false);
//---
   PrepareEdit(m_edit_balance,"Balance",370,220,200,false);
   PrepareEdit(m_edit_equity,"Equity",370,240,200,false);
   PrepareEdit(m_edit_gain,"Gain",370,260,200,false);
   PrepareEdit(m_edit_max_drawdown,"Max drawdown",370,280,200,false);
   PrepareEdit(m_edit_price,"Price",370,300,200,false);
   PrepareEdit(m_edit_roi,"Roi",370,320,200,false);
//---
   PrepareEdit(m_edit_date_published,"Date published",670,120,120,false);
   PrepareEdit(m_edit_date_started,"Date started",670,140,120,false);
   PrepareEdit(m_edit_leverage,"Leverage",670,160,120,false);
   PrepareEdit(m_edit_pips,"Pips",670,180,120,false);
   PrepareEdit(m_edit_rating,"Rating",670,200,120,false);
   PrepareEdit(m_edit_subscribers,"Subscribers",670,220,120,false);
   PrepareEdit(m_edit_trades,"Trades",670,240,120,false);
   PrepareEdit(m_edit_trade_mode,"Trade mode",670,260,120,false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Subscribe" and "Unsubscribe" buttons                 |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::CreateButtons(void)
  {
//--- coordinates
   int x1=370;
   int y1=40;
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_button_subscribe.Create(m_chart_id,m_name+"ButtonSubscribe",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_button_subscribe.Text("Subscribe"))
      return(false);
   if(!Add(m_button_subscribe))
      return(false);
//--- coordinates
   y1+=30;
   x2=x1+BUTTON_WIDTH;
   y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_button_unsubscribe.Create(m_chart_id,m_name+"ButtonUnsubscribe",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_button_unsubscribe.Text("Unsubscribe"))
      return(false);
   if(!Add(m_button_unsubscribe))
      return(false);
//---
   UpdateInfo();
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| FillSignalsArray                                                 |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::FillSignalsArray(void)
  {
   int total_signals=0;
   ArrayResize(m_signals_array,0);
//--- total signals
   int total=SignalBaseTotal();
//--- proceed all signals
   for(int i=0;i<total;i++)
     {
      //--- select signal from base
      if(SignalBaseSelect(i))
        {
         ArrayResize(m_signals_array,total_signals+1,16);
         //--- string properties
         m_signals_array[total_signals].author_login=SignalBaseGetString(SIGNAL_BASE_AUTHOR_LOGIN);
         m_signals_array[total_signals].name=SignalBaseGetString(SIGNAL_BASE_NAME);
         m_signals_array[total_signals].broker_name=SignalBaseGetString(SIGNAL_BASE_BROKER);
         m_signals_array[total_signals].broker_server=SignalBaseGetString(SIGNAL_BASE_BROKER_SERVER);
         //--- integer properties
         m_signals_array[total_signals].id=SignalBaseGetInteger(SIGNAL_BASE_ID);
         m_signals_array[total_signals].date_published=SignalBaseGetInteger(SIGNAL_BASE_DATE_PUBLISHED);
         m_signals_array[total_signals].date_started=SignalBaseGetInteger(SIGNAL_BASE_DATE_STARTED);
         m_signals_array[total_signals].leverage=SignalBaseGetInteger(SIGNAL_BASE_LEVERAGE);
         m_signals_array[total_signals].pips=SignalBaseGetInteger(SIGNAL_BASE_PIPS);
         m_signals_array[total_signals].rating=SignalBaseGetInteger(SIGNAL_BASE_RATING);
         m_signals_array[total_signals].subscribers=SignalBaseGetInteger(SIGNAL_BASE_SUBSCRIBERS);
         m_signals_array[total_signals].trades=SignalBaseGetInteger(SIGNAL_BASE_TRADES);
         m_signals_array[total_signals].trade_mode=SignalBaseGetInteger(SIGNAL_BASE_TRADE_MODE);
         //--- double poperties
         m_signals_array[total_signals].balance=SignalBaseGetDouble(SIGNAL_BASE_BALANCE);
         m_signals_array[total_signals].equity=SignalBaseGetDouble(SIGNAL_BASE_EQUITY);
         m_signals_array[total_signals].gain=SignalBaseGetDouble(SIGNAL_BASE_GAIN);
         m_signals_array[total_signals].max_drawdown=SignalBaseGetDouble(SIGNAL_BASE_MAX_DRAWDOWN);
         m_signals_array[total_signals].price=SignalBaseGetDouble(SIGNAL_BASE_PRICE);
         m_signals_array[total_signals].roi=SignalBaseGetDouble(SIGNAL_BASE_ROI);
         //---
         total_signals++;
        }
      else
        {
         PrintFormat("Error in SignalBaseSelect. Error code=%d",GetLastError());
         return(false);
        }
     }
   return(true);
  }
//+------------------------------------------------------------------+
//| UpdateSignalsListView                                            |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::UpdateSignalsListView(void)
  {
//--- prpeare items
   m_signals_list_view.ItemsClear();
   for(int i=0,n=ArraySize(m_signals_array); i<n; i++)
     {
      string str=StringFormat("%s",m_signals_array[i].name);
      m_signals_list_view.AddItem(str);
     }
//--- select first by default
   if(ArraySize(m_signals_array)>0)
     {
      m_signals_list_view.Select(0);
      OnChangeListView();
     }
//---  
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "ListView" element                                    |
//+------------------------------------------------------------------+
bool CSignalsDemoDialog::CreateListView(void)
  {
//--- coordinates
   int x1=10;
   int y1=110;
   int x2=x1+260;
   int y2=y1+225;
//--- create
   if(!m_signals_list_view.Create(m_chart_id,m_name+"SignalsListView",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!Add(m_signals_list_view))
      return(false);
//---
//   CreateSignalsListView();
   if(!FillSignalsArray())
      return(false);
   if(!UpdateSignalsListView())
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Event handler Subscribe button                                   |
//+------------------------------------------------------------------+
void CSignalsDemoDialog::OnClickButtonSubscribe(void)
  {
//--- connection to MQL5.community
   if(!TerminalInfoInteger(TERMINAL_COMMUNITY_CONNECTION))
     {
      MessageBox("No connection to MQL5.community.");
      return;
     }   
//--- signals allowed cheking
   if(!MQLInfoInteger(MQL_SIGNALS_ALLOWED))
     {
      MessageBox("Signals modification is not allowed. Enable them in Expert Advisor settings.","Error",MB_ICONERROR);
      ExpertRemove();
      return;
     }
   int index=m_signals_list_view.Current();
   if(index<0) return;
   if(index>=ArraySize(m_signals_array)) return;
   long id=m_signals_array[index].id;
//---
   double price=m_signals_array[index].price;
   double mql5_balance=TerminalInfoDouble(TERMINAL_COMMUNITY_BALANCE);
//---   
   if(price>mql5_balance)
     {
      string str="Not enough money! Signal price="+DoubleToString(price,2)+", MQL5 balance="+DoubleToString(mql5_balance,2);
      MessageBox(str,"Information",MB_ICONINFORMATION);
      return;
     }
//---
   long deposit_percent=StringToInteger(m_edit_signal_info_deposit_percent.Text());
//---
   if((deposit_percent<5) || (deposit_percent>95))
     {
      Print("Deposit percent is not specified. 5% used by default.");
      deposit_percent=5;
     }
//---
   double equity_limit=StringToDouble(m_edit_signal_info_equity_limit.Text());
//---
   if(equity_limit<0)
     {
      Print("Error in equity limit. value="+DoubleToString(equity_limit));
      equity_limit=0;
     }
//---
   long slippage=StringToInteger(m_edit_signal_info_slippage.Text());
//---
   if(slippage<0)
     {
      Print("Error in slippage. Slippage="+DoubleToString(slippage));
      slippage=0;
     }
//---
   SignalInfoSetDouble(SIGNAL_INFO_EQUITY_LIMIT,equity_limit);
   SignalInfoSetInteger(SIGNAL_INFO_DEPOSIT_PERCENT,deposit_percent);
   SignalInfoSetDouble(SIGNAL_INFO_SLIPPAGE,slippage);
//---
   bool result=SignalSubscribe(id);
   if(!result) Print("Error in Subscribe. Error code="+IntegerToString(GetLastError()));
//---
   UpdateInfo();
  }
//+------------------------------------------------------------------+
//| Event handler Unsubscribe button                                 |
//+------------------------------------------------------------------+
void CSignalsDemoDialog::OnClickButtonUnsubscribe(void)
  {
//---     
   bool result=SignalUnsubscribe();
   if(!result) Print("Error in Unsubscribe. Error code="+IntegerToString(GetLastError()));
   UpdateInfo();
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSignalsDemoDialog::OnChangeListView(void)
  {
   FillBaseSignalInfo(m_signals_list_view.Current());
  }
//+------------------------------------------------------------------+
//| FillCurrentSignalInfo                                            |
//+------------------------------------------------------------------+
void CSignalsDemoDialog::FillCurrentSignalInfo(void)
  {
   string current_signal_name=SignalInfoGetString(SIGNAL_INFO_NAME);
   double current_signal_equity_limit=SignalInfoGetDouble(SIGNAL_INFO_EQUITY_LIMIT);
   double current_signal_slippage=SignalInfoGetDouble(SIGNAL_INFO_SLIPPAGE);
   long current_signal_deposit_percent=SignalInfoGetInteger(SIGNAL_INFO_DEPOSIT_PERCENT);
//---
   if((current_signal_name=="") || (current_signal_name==NULL))
     {
      m_edit_signal_info_name.Text("not subscribed");
      m_edit_signal_info_equity_limit.Text(DoubleToString(0,2));
      m_edit_signal_info_slippage.Text(DoubleToString(2,2));
      m_edit_signal_info_deposit_percent.Text(IntegerToString(5));
      m_button_subscribe.Visible(true);
      m_button_unsubscribe.Visible(false);
     }
   else
     {
      m_edit_signal_info_name.Text(current_signal_name);
      m_edit_signal_info_equity_limit.Text(DoubleToString(current_signal_equity_limit,2));
      m_edit_signal_info_slippage.Text(DoubleToString(current_signal_slippage,2));
      m_edit_signal_info_deposit_percent.Text(DoubleToString(current_signal_deposit_percent,2));
      m_button_subscribe.Visible(false);
      m_button_unsubscribe.Visible(true);
     }
  }
//+------------------------------------------------------------------+
//| FillBaseSignalInfo                                               |
//+------------------------------------------------------------------+
void CSignalsDemoDialog::FillBaseSignalInfo(int index)
  {
   if(index>=ArraySize(m_signals_array)) return;
///--
   m_edit_author_login.Text(m_signals_array[index].author_login);
   m_edit_name.Text(m_signals_array[index].name);
   m_edit_broker_name.Text(m_signals_array[index].broker_name);
   m_edit_broker_server.Text(m_signals_array[index].broker_server);
///--
   m_edit_id.Text(IntegerToString(m_signals_array[index].id));
///--
   m_edit_date_published.Text(TimeToString(m_signals_array[index].date_published));
   m_edit_date_started.Text(TimeToString(m_signals_array[index].date_started));
   m_edit_leverage.Text(IntegerToString(m_signals_array[index].leverage));
   m_edit_pips.Text(IntegerToString(m_signals_array[index].pips));
   m_edit_rating.Text(IntegerToString(m_signals_array[index].rating));
   m_edit_subscribers.Text(IntegerToString(m_signals_array[index].subscribers));
   m_edit_trades.Text(IntegerToString(m_signals_array[index].trades));
///--
   string trade_mode="unknown";
   switch(int(m_signals_array[index].trade_mode))
     {
      case 0: trade_mode="real";    break;
      case 1: trade_mode="demo";    break;
      case 2: trade_mode="contest"; break;
     }
///--
   m_edit_trade_mode.Text(trade_mode);
///--
   m_edit_balance.Text(DoubleToString(m_signals_array[index].balance,2));
   m_edit_equity.Text(DoubleToString(m_signals_array[index].equity,2));
   m_edit_gain.Text(DoubleToString(m_signals_array[index].gain,2));
   m_edit_max_drawdown.Text(DoubleToString(m_signals_array[index].max_drawdown,2));
   m_edit_price.Text(DoubleToString(m_signals_array[index].price,2));
   m_edit_roi.Text(DoubleToString(m_signals_array[index].roi,2));
  }
//+------------------------------------------------------------------+
void CSignalsDemoDialog::UpdateInfo(void)
  {
   m_label_MQL5balance_info.Text(DoubleToString(TerminalInfoDouble(TERMINAL_COMMUNITY_BALANCE),2));
//---
   FillCurrentSignalInfo();
  }
//+------------------------------------------------------------------+
