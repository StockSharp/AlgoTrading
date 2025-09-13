//+------------------------------------------------------------------+
//|                                             HighImpactEvents.mq4 |
//|                               Copyright 2016, Claude G. Beaudoin |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, Claude G. Beaudoin"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

// Define event structure
struct EVENTS
{
   string   time;
   string   title;
   string   currency;
   bool     displayed;
};

#define     MaxDailyEvents       20    // If you think you'll have more than 20 High Impact events, increase this number.
EVENTS      DailyEvents[MaxDailyEvents];


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   // Get today's events
   GetHighImpactEvents();
   
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   ObjectsDeleteAll();
}

//+------------------------------------------------------------------+
//| Expert start function                                             |
//+------------------------------------------------------------------+
void start()
{
   string   event = NULL;
   
   // Is there a high impact event in the next 5 minutes?
   for(int i = 0; i < MaxDailyEvents; i++)
   {
      if(StringLen(DailyEvents[i].time) == 0) break;
      if(TimeCurrent() >= StrToTime(DailyEvents[i].time) - 300 && TimeCurrent() < StrToTime(DailyEvents[i].time) && !DailyEvents[i].displayed)
      {
         // Event in 5 minutes...
         event += DailyEvents[i].title + " (" + DailyEvents[i].currency + "), ";
         DailyEvents[i].displayed = true;
         
         // Delete the vertical line associated to the event
         if(ObjectFind("VLine" + DoubleToStr(i, 0)) >= 0) ObjectDelete("VLine" + DoubleToStr(i, 0));
      }  
   }
   
   // Anything to display?
   if(StringLen(event) != 0)
   {
      event += "in 5 minutes.";
      Alert(event);
   }
}

//+------------------------------------------------------------------+
//| Extract an HTML element
//+------------------------------------------------------------------+
string   GetHTMLElement(string HTML, string ElementStart, string ElementEnd)
{
   string   data = NULL;
   
   // Find start and end position for element
   int s = StringFind(HTML, ElementStart) + StringLen(ElementStart);
   int e = StringFind(StringSubstr(HTML, s), ElementEnd);
   
   // Return element content
   if(e != 0) data = StringSubstr(HTML, s, e);
   return(data);
}

//+------------------------------------------------------------------+
//| Get today's high impact events from ForexFactory.com
//+------------------------------------------------------------------+
bool  GetHighImpactEvents()
{
   string   cookie=NULL, headers, HTML;
   string   url="http://www.forexfactory.com/calendar.php?day=";
   string   time = NULL, lasttime = NULL, currency, impact, title;
   char     post[], result[];
   int      res, cntr = 0, timeout = 5000;

   // If offline, just exit as if it was properly read
   if(!IsConnected() || IsTesting()) return(true);

   // Clear daily event structure
   for(res = 0; res < MaxDailyEvents; res++)
      { DailyEvents[res].time = DailyEvents[res].title = DailyEvents[res].currency = NULL; DailyEvents[res].displayed = false; ++res; }
 
   // Send web request
   url += MthName(Month()) + DoubleToStr(Day(), 0) + "." + DoubleToStr(Year(), 0);
   ResetLastError();
   res = WebRequest("GET", url, cookie, NULL, timeout, post, 0, result, headers);

   // Check for errors
   if(res == -1)
   {
      Print("Error in WebRequest. Error code = ", GetLastError());
      MessageBox("Add the address 'http://forexfactory.com/' in the\nlist of allowed URLs on tab 'Expert Advisors'", "Error", MB_ICONINFORMATION);
   }
   else
   {
      // Convert character array to a string
      HTML = CharArrayToString(result);

      // Calendar loaded, make sure it's for today's date
      int i = StringFind(HTML, "<span class=\"date\">");
      if(i == -1) return(false);
      HTML = StringSubstr(HTML, i);
      string date = GetHTMLElement(HTML, "<span>", "</span>");
      if(date != MthName(Month()) + " " + DoubleToStr(Day(), 0)) return(false);

      // Now get table rows for each event
      lasttime = NULL;
      date = DoubleToStr(Year(), 0) + "." + DoubleToStr(Month(), 0) + "." + DoubleToStr(Day(), 0) + " ";
      do
      {
         // Get event information
         time = GetHTMLElement(HTML, "<td class=\"calendar__cell calendar__time time\">", "</td>");
         if(StringFind(time, "<a name=\"upnext\"") == 0) time = GetHTMLElement(time, "class=\"upnext\">", "</span>");
         if(StringLen(time) != 0) lasttime = time;
         if(StringLen(time) == 0) time = lasttime; 

         // If the time has 'pm' in it, add 12 hours.  StrToTime only understands a 24 hour clock.
         if(StringFind(time, "pm") != -1) time = TimeToStr(StrToTime(time) + (12*60*60));
         time = date + time;

         // Get the other elements we need
         currency = GetHTMLElement(HTML, "<td class=\"calendar__cell calendar__currency currency\">", "</td>");
         impact = GetHTMLElement(HTML, "<span title=\"", "\" class=\"");
         i = StringFind(impact, " Impact");
         if(i != -1) impact = StringSubstr(impact, 0, i);
         title = GetHTMLElement(HTML, "\"calendar__event-title\">", "</span>");
         
         // Is this a high impact event for my currency pair?
         if(StringFind(Symbol(), currency) != -1 && impact == "High")
         {
            // Add to daily event structure
            DailyEvents[cntr].displayed = false;
            DailyEvents[cntr].time = time;
            DailyEvents[cntr].title = title;
            DailyEvents[cntr++].currency = currency;
         }
                  
         // Cut HTML string to the next table row
         i = StringFind(HTML, "</tbody> </table> </td> </tr> ");
         if(i != -1) HTML = StringSubstr(HTML, i+30);
         if(StringFind(HTML, "</table> <div class=\"foot\">") == 0) i = -1;
      } while(i != -1 || cntr == MaxDailyEvents);
   }

   // Display the high impact events, if any
   lasttime = NULL;
   for(cntr = 0; cntr < MaxDailyEvents; cntr++)
   {
      if(StringLen(DailyEvents[cntr].time) == 0) break;
      
      // Create event marker on chart if last market wasn't the same time
      if(lasttime != DailyEvents[cntr].time)
      {
         res = cntr;
         if(ObjectCreate(0, "Event" + DoubleToStr(cntr, 0), OBJ_EVENT, 0, StrToTime(DailyEvents[cntr].time), 0))
         {
            ObjectSetString(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_TEXT, DailyEvents[cntr].title + " (" + DailyEvents[cntr].currency + ")");
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_COLOR, Red);
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_WIDTH, 2);
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_BACK, true);
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_SELECTABLE, false);
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_SELECTED, false);
            ObjectSetInteger(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_HIDDEN, true);
            ObjectSetString(0, "Event" + DoubleToStr(cntr, 0), OBJPROP_TOOLTIP, DailyEvents[cntr].title + " (" + DailyEvents[cntr].currency + ")");
         }
      
         // Create vertical line if event is in the future
         if(TimeCurrent() < StrToTime(DailyEvents[cntr].time))
         {
            if(ObjectCreate(0, "VLine" + DoubleToStr(cntr, 0), OBJ_VLINE, 0, StrToTime(DailyEvents[cntr].time), 0))
            {
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_COLOR, Red);
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_WIDTH, 1);
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_BACK, true);
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_SELECTABLE, false);
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_SELECTED, false);
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_HIDDEN, true);
               ObjectSetString(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_TOOLTIP, DailyEvents[cntr].title + " (" + DailyEvents[cntr].currency + ")");
               ObjectSetInteger(0, "VLine" + DoubleToStr(cntr, 0), OBJPROP_TIMEFRAMES, OBJ_PERIOD_M1 | OBJ_PERIOD_M5 | OBJ_PERIOD_M15 | OBJ_PERIOD_M30 | OBJ_PERIOD_H1);
            }
         }
         else
            DailyEvents[cntr].displayed = true;
      }
      else
      {
         title = ObjectGetString(0, "Event" + DoubleToStr(res, 0), OBJPROP_TOOLTIP);            
         title += "\n" + DailyEvents[cntr].title + " (" + DailyEvents[cntr].currency + ")";
         ObjectSetString(0, "Event" + DoubleToStr(res, 0), OBJPROP_TOOLTIP, title);
         if(TimeCurrent() < StrToTime(DailyEvents[cntr].time)) ObjectSetString(0, "Vline" + DoubleToStr(res, 0), OBJPROP_TOOLTIP, title);
      }
      lasttime = DailyEvents[cntr].time;
   }   
   
   // Exit
   return(true);
}

//+------------------------------------------------------------------+
//| Return the long or short month name
//+------------------------------------------------------------------+
string   MthName(int Mth, bool ShortName=true)
{
   switch(Mth)
   {
      case 1:
         return((ShortName ? "Jan" : "January"));
         break;
      case 2:
         return((ShortName ? "Feb" : "February"));
         break;
      case 3:
         return((ShortName ? "Mar" : "March"));
         break;
      case 4:
         return((ShortName ? "Apr" : "April"));
         break;
      case 5:
         return((ShortName ? "May" : "May"));
         break;
      case 6:
         return((ShortName ? "Jun" : "June"));
         break;
      case 7:
         return((ShortName ? "Jul" : "July"));
         break;
      case 8:
         return((ShortName ? "Aug" : "August"));
         break;
      case 9:
         return((ShortName ? "Sep" : "September"));
         break;
      case 10:
         return((ShortName ? "Oct" : "October"));
         break;
      case 11:
         return((ShortName ? "Nov" : "November"));
         break;
      case 12:
         return((ShortName ? "Dec" : "December"));
         break;
   }
   
   // Unknown month
   return("?");
}

//+------------------------------------------------------------------+
//| END OF CODE
//+------------------------------------------------------------------+
