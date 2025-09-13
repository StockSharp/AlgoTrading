//+------------------------------------------------------------------+
//|                                        Informative dashboard.mqh |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, MetaQuotes Ltd."
#property link      "https://www.mql5.com"

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

#include <Controls\Dialog.mqh>
#include <Controls\Defines.mqh>
#include <Controls\Label.mqh>

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

class CInformativeDashboard: public CAppDialog
  {
protected:
   
   virtual bool OnEvent(const int id,const long& lparam,const double& dparam,const string& sparam); //Very important function inheritance
   
   CLabel account_name;
   CLabel daily_pl;
   CLabel percent_dd;
   CLabel pos_orders;
   CLabel spread;
   
   CLabel account_name_value;
   CLabel daily_pl_value;
   CLabel percent_dd_value;
   CLabel pos_orders_value;
   CLabel spread_value;
   
   bool CreateLabel(CLabel &label, int x, int y, int width, string label_name, string text);
   
public:
                     CInformativeDashboard(void);
                    ~CInformativeDashboard(void);
                    
                    
                    bool CreateDashboard(string name, int x1, int y1, int x2, int y2);
                    
                    virtual bool Run()
                     {
                       return CAppDialog::Run();
                     }
                     
                     void RefreshValues(void);
                     
  };
  
EVENT_MAP_BEGIN(CInformativeDashboard)


EVENT_MAP_END(CAppDialog)  
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CInformativeDashboard::CInformativeDashboard(void)
 {
   m_chart_id = 0;
   m_subwin = 0;
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CInformativeDashboard::CreateDashboard(string name,int x1,int y1,int x2,int y2)
 {
    if (!Create(m_chart_id, name, m_subwin, x1, y1, x2, y2))
      {
        Print("Failed to create dashboard Err=",GetLastError());
        return false;
      }
   
   int width = 10;
   
//--- Label names

   CreateLabel(account_name, 20, 20, width, "ac_name", "AC Name :");
   account_name.Color(clrDodgerBlue);
   
   CreateLabel(daily_pl, 20, 40, width, "daily_pl", "Daily PL :");
   CreateLabel(percent_dd, 20, 60, width, "percent_dd", "% Drawdown :");
   CreateLabel(pos_orders, 20, 80, width, "pos_order", "Pos & Orders :");
   CreateLabel(spread, 20, 100, width, "spread", "Spread : ");
   
//--- Label values
   
   string ac_name = AccountInfoString(ACCOUNT_NAME);
   
   CreateLabel(account_name_value, 100, 20, width, "ac_name_value", ac_name);
   account_name_value.Color(clrDodgerBlue);
   
   CreateLabel(daily_pl_value, 100, 40, width, "daily_pl_value", "0.0");
   CreateLabel(percent_dd_value, 110, 60, width, "percent_dd_value", "0.0");
   CreateLabel(pos_orders_value, 110, 80, width, "pos_order_value", "0");
   CreateLabel(spread_value, 80, 100, width, "spread_value", "0");



   ChartRedraw(m_chart_id);
   
   return true;   
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CInformativeDashboard::~CInformativeDashboard(void)
 {
 
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CInformativeDashboard::CreateLabel(CLabel &label,int x,int y,int width,string label_name,string text)
 {
   if (!label.Create(m_chart_id, m_name + label_name, m_subwin, x, y, x, y + width))
     return false;
   if (!Add(label))
     return false;
   if (!label.Text(text))
     return false;
   
   return true;
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CInformativeDashboard::RefreshValues(void)
 {
    daily_pl_value.Text(DoubleToString(DailyPL(), 3));
    
    double daily_pl_var = DailyPL();
    if (daily_pl_var<0) //we made losses
      daily_pl_value.Color(clrRed);
    else
      {
        daily_pl_value.Color(clrDodgerBlue);
      }
    
    double dd_percent = (AccountInfoDouble(ACCOUNT_EQUITY) - AccountInfoDouble(ACCOUNT_BALANCE))/AccountInfoDouble(ACCOUNT_BALANCE);
    percent_dd_value.Text(DoubleToString(dd_percent*100, 3));
    pos_orders_value.Text(string(OrdersTotal()+ PositionsTotal()));
    
    int market_spread = (int)SymbolInfoInteger(Symbol(), SYMBOL_SPREAD);
    spread_value.Text(string(market_spread));
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+



//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

double DailyPL()
 {
   double dayprof = 0.0;
   datetime end = TimeCurrent();
   string sdate = TimeToString (TimeCurrent(), TIME_DATE);
   datetime start = StringToTime(sdate);

   HistorySelect(start, end);
   int TotalDeals = HistoryDealsTotal();

   for(int i = 0; i < TotalDeals; i++)
     {
      ulong Ticket = HistoryDealGetTicket(i);

      if(HistoryDealGetInteger(Ticket,DEAL_ENTRY) == DEAL_ENTRY_OUT)
        {
         double LatestProfit = HistoryDealGetDouble(Ticket, DEAL_PROFIT);
         dayprof += LatestProfit;
        }
     }
   return dayprof;
 }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
