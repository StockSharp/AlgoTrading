//+------------------------------------------------------------------+
//|                                     IndicatorParameters_Demo.mq5 |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| indicator_info                                                   |
//+------------------------------------------------------------------+
struct indicator_info
  {
   int               win;
   int               subwin;
   int               index;
   string            name;
   int               handle;
  };
//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   indicator_info ind_info_new[];
   static indicator_info ind_info_old[];
   static int ind_total=0;
   static int ind_total_old=0;

//--- chart event change
   if(id==CHARTEVENT_CHART_CHANGE)
     {
      //--- save current indicator list to ind_info_new[] array
      GetIndicatorsInfo(ind_info_new,ind_total);
      //--- checking changes
      if(ind_total!=ind_total_old)
        {
         CompareStates(ind_info_new,ind_info_old);
        }
      //--- save current indicator list to ind_info_old[] array
      GetIndicatorsInfo(ind_info_old,ind_total_old);
     }
//---
  }
//+------------------------------------------------------------------+
//| Compare information about indicators                             |
//+------------------------------------------------------------------+
void CompareStates(indicator_info &new_info[],indicator_info &old_info[])
  {
//--- checking: indicator has been deleted from chart
   if(ArraySize(old_info)>0)
     {
      for(int i=0; i<ArraySize(old_info); i++)
        {
         if(FindIndicator(new_info,old_info[i].name,old_info[i].handle)==false)
           {
            Print("- deleted: win=",old_info[i].win," subwin=",old_info[i].subwin,
                  " name=",old_info[i].name," handle=",old_info[i].handle);
           }
        }
     }
//--- checking: indicator has been added on chart
   if(ArraySize(new_info)>0)
     {
      for(int i=0; i<ArraySize(new_info); i++)
        {
         //--- find it in the old_info[] array
         if(FindIndicator(old_info,new_info[i].name,new_info[i].handle)==false)
           {
            Print("+ added: win=",new_info[i].win," subwin=",new_info[i].subwin,
                  " name=",new_info[i].name," handle=",new_info[i].handle);
            Print(GetParametersInfo(new_info[i].win,new_info[i].index));
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Finds the information about indicator in ind[] array             |
//+------------------------------------------------------------------+
bool FindIndicator(indicator_info &ind[],string name,int handle)
  {
   for(int i=0; i<ArraySize(ind); i++)
     {
      if(ind[i].name==name && ind[i].handle==handle) { return(true); }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| Adds the information about indicator to ind[] array              |
//+------------------------------------------------------------------+
bool AddIndicatorInfo(indicator_info &ind[],int win,int subwin,string name,int handle,int index)
  {
   if(FindIndicator(ind,name,handle)) return(false);
   int cnt=ArraySize(ind);
   ArrayResize(ind,cnt+1);
   ind[cnt].win=win;
   ind[cnt].subwin=subwin;
   ind[cnt].name=name;
   ind[cnt].handle=handle;
   ind[cnt].index=index;
   return(true);
  }
//+------------------------------------------------------------------+
//| Gets the information about all indicators on the chart           |
//+------------------------------------------------------------------+
void GetIndicatorsInfo(indicator_info &ind[],int &indicators_total)
  {
//--- total indicator windows
   int subwindows=(int)ChartGetInteger(0,CHART_WINDOWS_TOTAL);
   string s="CHART_WINDOWS_TOTAL="+IntegerToString(subwindows)+"\n";
   ArrayResize(ind,0);

   indicators_total=0;
   for(int i=0; i<subwindows; i++)
     {
      int indicators=ChartIndicatorsTotal(0,i);
      if(indicators>0)
        {
         indicators_total+=indicators;
         for(int j=0; j<indicators; j++)
           {
            //--- get indicator name
            string name=ChartIndicatorName(0,i,j);
            //--- get indicator handle
            int handle=ChartIndicatorGet(0,i,name);
            //--- add the information to ind[] array
            AddIndicatorInfo(ind,i,j,name,handle,j);
           }
        }
     }
   return;
  }
//+------------------------------------------------------------------+
//| Get the information about indicator parameters                   |
//+------------------------------------------------------------------+
string GetParametersInfo(int sub_window,int ind_index)
  {
//---
   string info="";
//--- get indicator short name by index
   string name=ChartIndicatorName(0,sub_window,ind_index);
//--- get indicator handle
   int handle=ChartIndicatorGet(0,sub_window,name);
//---
   MqlParam parameters[];
   ENUM_INDICATOR indicator_type;
   int params=IndicatorParameters(handle,indicator_type,parameters);
//--- prepare header
   info=name+" => "+EnumToString(ENUM_INDICATOR(indicator_type))+"\r\n";
   for(int i=0;i<params;i++)
     {
      info+=StringFormat("%d: type=%s, long_value=%d, double_value=%G,string_value=%s\r\n",
                         i,
                         EnumToString((ENUM_DATATYPE)parameters[i].type),
                         parameters[i].integer_value,
                         parameters[i].double_value,
                         parameters[i].string_value
                         );
     }
//--- return the information about indicator parameters
   return(info);
  }
//+------------------------------------------------------------------+
