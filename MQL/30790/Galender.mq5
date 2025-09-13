#include <Controls\Dialog.mqh>
#include <Controls\ListView.mqh>
#include <Arrays\ArrayString.mqh>

#define X1 (30)
#define Y1 (30)
#define INDENT_LEFT (10)   // indent from left (with allowance for border width)
#define INDENT_TOP (10)    // indent from top (with allowance for border width)
#define INDENT_RIGHT (50)  // indent from right (with allowance for border width)
#define INDENT_BOTTOM (70) // indent from bottom (with allowance for border width)
#define WIDTH (500)        // size by X coordinate
#define HEIGHT (300)       // size by Y coordinate

enum IMPORTANCE
{
   NONE,
   LOW,
   MODERATE,
   HIGH,
   ALL
};

input datetime date_from = D'2020.07.01 00:00'; //Begin Date
input datetime date_to = D'2020.09.01 00:00'; //End Date
input string cur = "USD"; //Currency Filter
input string filter = "interest"; //Keyword Filter
input IMPORTANCE imp = ALL; //Importance Filter

CArrayString calArray;
ENUM_CALENDAR_EVENT_IMPORTANCE importance;

void SetImportance()
{
   if(imp == ALL) return;
   switch(imp)
   {
      case NONE: importance = CALENDAR_IMPORTANCE_NONE; break;
      case LOW: importance = CALENDAR_IMPORTANCE_LOW; break;
      case MODERATE: importance = CALENDAR_IMPORTANCE_MODERATE; break;
      case HIGH: importance = CALENDAR_IMPORTANCE_HIGH; break;
   }
}

//country -> value -> event
bool GetCalendar()
{
   MqlCalendarCountry countries[];
   bool f = (filter != "")?true:false;    //filter empty
   int c = CalendarCountries(countries);  //country count
   int p = 0; //position   
   double va, vp, vf;
   string row = "";
   string m; //impact
   datetime prev = 0;
   
   if(c == 0)
   {
      Alert("ERROR: unable to get calendar country data.");
      return(false);
   }   
   if(date_from > date_to)
   {
      Print("ERROR: beginning date should be prior to end date.");
      return(false);
   }
     
   for(int i = 0; i < c; i++)
   {
      if(cur != "" && countries[i].currency != cur) continue;     
      MqlCalendarValue values[];
      CalendarValueHistory(values, date_from, date_to, countries[i].code, countries[i].currency);      
      for(int j = 0; j < ArraySize(values); j++)
      {
         MqlCalendarEvent event;
         CalendarEventById(values[j].event_id, event);
         SetImportance();
         if(imp == ALL || event.importance == importance)
         {
            if(f)
            {
               p = StringFind(event.event_code, filter, 0);
            }
            if(p != -1)
            {
               va = values[j].actual_value;
               vp = values[j].prev_value;
               vf = values[j].forecast_value;
               switch(event.multiplier)
               {
                  case CALENDAR_MULTIPLIER_NONE: break;
                  case CALENDAR_MULTIPLIER_THOUSANDS: va /= 1000; vp /= 1000; vf /= 1000;break;
                  case CALENDAR_MULTIPLIER_MILLIONS: va /= 1000000; vp /= 1000000; vf /= 1000000;break;
                  case CALENDAR_MULTIPLIER_BILLIONS: va /= 1000000000; vp /= 1000000000; vf /= 1000000000;break;
                  case CALENDAR_MULTIPLIER_TRILLIONS: va /= 1000000000000; vp /= 1000000000000; vf /= 1000000000000;break;
                  default: va = 0; vp = 0; vf = 0;break;
               }
               switch(values[j].impact_type)
               {
                  case CALENDAR_IMPACT_NA: m = "None"; break;
                  case CALENDAR_IMPACT_NEGATIVE: m = "Negative"; break;
                  case CALENDAR_IMPACT_POSITIVE: m = "Positive"; break;
                  default: m =""; break;
               }
               
               if(TimeCurrent() < values[j].time || values[j].actual_value == -9223372036854775808) va = 0;
               if(values[j].prev_value == -9223372036854775808) vp = 0;
               if(values[j].forecast_value == -9223372036854775808) vf = 0;
               row = TimeToString(values[j].time, TIME_DATE|TIME_MINUTES) + "#" + countries[i].currency + "#" + event.event_code + "#" + m + "#" + DoubleToString(vp, 2) + "#" + DoubleToString(vf, 2) + "#" + DoubleToString(va, 2);
               calArray.Add(row);
            }            
         }            
      }
   }
   calArray.Sort(0);
   return(true);
}

class CControlsDialog : public CAppDialog
{
private:
   CListView m_list_view;

public:
   CControlsDialog(void);
   ~CControlsDialog(void);
   virtual bool Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   virtual bool OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);

protected:
 bool CreateListView(void);
 void OnChangeListView(void);
};

EVENT_MAP_BEGIN(CControlsDialog)
ON_EVENT(ON_CHANGE,m_list_view,OnChangeListView)
EVENT_MAP_END(CAppDialog)

CControlsDialog::CControlsDialog(void){}
CControlsDialog::~CControlsDialog(void){}

bool CControlsDialog::Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2)
{
   if(!CAppDialog::Create(chart, name, subwin, x1, y1, x2, y2))
      return(false);
   if(!CreateListView())
      return(false);
   return(true);
}

bool CControlsDialog::CreateListView(void)
{
   int x1 = INDENT_LEFT;
   int y1 = INDENT_TOP;
   int x2 = WIDTH - INDENT_RIGHT;
   int y2 = HEIGHT - INDENT_BOTTOM;
   if(!m_list_view.Create(m_chart_id, m_name+"ListView", m_subwin, x1, y1, x2, y2))
      return(false);
   if(!Add(m_list_view))
      return(false);
   for(int i = 0; i < calArray.Total(); i++)
   {
      if(!m_list_view.AddItem(calArray.At(i)))
         return(false);
   }
 return(true);
}

void CControlsDialog::OnChangeListView(void)
{
   string a[7];
   string msg = "";
   StringSplit(calArray.At(m_list_view.Current()), '#', a);
   msg += "Time: " + a[0] + "\n";
   msg += "Currency: " + a[1] + "\n";
   msg += "Event: " + a[2] + "\n";
   msg += "Impact: " + a[3] + "\n";
   msg += "Previous: " + a[4] + "\n";
   msg += "Forecast: " + a[5] + "\n";
   msg += "Actual: " + a[6];
   MessageBox(msg, a[2] + "( " + a[0] + ")", MB_OK|MB_ICONINFORMATION);
}

CControlsDialog ExtDialog;

int OnInit()
{
   if(!GetCalendar())
      return(INIT_FAILED);
   if(!ExtDialog.Create(0, "Galender 1.0", 0, X1, Y1, WIDTH, HEIGHT))
      return(INIT_FAILED);
   ExtDialog.Run();
   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
   ExtDialog.Destroy(reason);
}

void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   ExtDialog.ChartEvent(id, lparam, dparam, sparam);
}