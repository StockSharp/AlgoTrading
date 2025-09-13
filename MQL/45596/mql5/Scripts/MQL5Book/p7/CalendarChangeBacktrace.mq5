//+------------------------------------------------------------------+
//|                                      CalendarChangeBacktrace.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Request economic calendar changes in backward direction (decreasing change IDs)."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

#define BUF_SIZE 10

input int BacktraceSize = 10000;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ulong change = 0;
   MqlCalendarValue values[BUF_SIZE];
   PRTF(CalendarValueLast(change, values));
   if(!change)
   {
      Print("Can't get start change ID");
      return;
   }
   
   Print("Starting backward from the change ID: ", change);
   
   ulong id = 0;
   const ulong start = change;
   for(int i = 1; i <= BacktraceSize && !IsStopped(); ++i)
   {
      change = start - i;
      const int n = CalendarValueLast(change, values);
      if(n)
      {
         if(values[0].id != id)
         {
            Print("Change ID: ", start - i);
            MqlCalendarValue subset[];
            int j = 1;
            for(; j < n; ++j)
            {
               if(values[j].id == id)
               {
                  break;
               }
            }
            ArrayCopy(subset, values, 0, 0, j);
            ArrayPrint(subset);
            if(j == BUF_SIZE)
            {
               PrintFormat("[more than %d news records in this change, trimmed]", BUF_SIZE);
            }
            id = values[0].id;
         }
      }
   }
}
//+------------------------------------------------------------------+
/*

CalendarValueLast(change,values)=0 / ok
Starting backward from the change ID: 86504192
Change ID: 86504191
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 167675  840200009 2022.07.07 17:30:00 2022.07.01 00:00:00          0 -9223372036854775808     82000000 -9223372036854775808         63000000             0        ...
Change ID: 86503679
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 167189  840200009 2022.06.30 17:30:00 2022.06.24 00:00:00          0       82000000     74000000 -9223372036854775808         75000000             2        ...
Change ID: 86503423
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value]     [forecast_value] [impact_type] [reserved]
[0] 166647   76080001 2022.09.30 14:00:00 2022.10.01 00:00:00          0 -9223372036854775808      7010000 -9223372036854775808 -9223372036854775808             0        ...
Change ID: 86503167
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value]     [forecast_value] [impact_type] [reserved]
[0] 163191   76080001 2022.06.30 14:00:00 2022.07.01 00:00:00          0        7010000      6820000 -9223372036854775808 -9223372036854775808             0        ...
Change ID: 86502911
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163462  840010014 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808      -400000 -9223372036854775808                0             0        ...
Change ID: 86502655
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163402  840010006 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808       500000 -9223372036854775808          2300000             0        ...
Change ID: 86502399
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163390  840010005 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808       200000 -9223372036854775808          -100000             0        ...
Change ID: 86502143
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163378  840010004 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808      6300000 -9223372036854775808          6700000             0        ...
Change ID: 86501887
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163366  840010003 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808       600000 -9223372036854775808           500000             0        ...
Change ID: 86501631
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163354  840010002 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808      4700000 -9223372036854775808          5000000             0        ...
Change ID: 86501375
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163342  840010001 2022.07.29 15:30:00 2022.06.01 00:00:00          0 -9223372036854775808       300000 -9223372036854775808           400000             0        ...
Change ID: 86501119
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 162079  124010035 2022.07.29 15:30:00 2022.05.01 00:00:00          0 -9223372036854775808      5000000 -9223372036854775808          4300000             0        ...
Change ID: 86500863
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 161955  124010021 2022.07.29 15:30:00 2022.05.01 00:00:00          0 -9223372036854775808       300000 -9223372036854775808                0             0        ...
Change ID: 86500607
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 168163  840140002 2022.07.07 15:30:00 2022.06.25 00:00:00          0 -9223372036854775808      1328000 -9223372036854775808          1286000             0        ...
Change ID: 86500351
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 168167  840140003 2022.07.07 15:30:00 2022.07.02 00:00:00          0 -9223372036854775808    231750000 -9223372036854775808        233471000             0        ...
Change ID: 86500095
      [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 168159  840140001 2022.07.07 15:30:00 2022.07.02 00:00:00          0 -9223372036854775808    231000000 -9223372036854775808        209000000             0        ...
Change ID: 86499583
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163461  840010014 2022.06.30 15:30:00 2022.05.01 00:00:00          0        -400000       700000               300000           200000             2        ...
Change ID: 86499071
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163401  840010006 2022.06.30 15:30:00 2022.05.01 00:00:00          0         500000       400000               500000         -3100000             1        ...
Change ID: 86498559
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163389  840010005 2022.06.30 15:30:00 2022.05.01 00:00:00          0         200000       900000               600000          -100000             1        ...
Change ID: 86498047
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163377  840010004 2022.06.30 15:30:00 2022.05.01 00:00:00          0        6300000      6300000 -9223372036854775808          6400000             2        ...
Change ID: 86497535
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163365  840010003 2022.06.30 15:30:00 2022.05.01 00:00:00          0         600000       200000 -9223372036854775808           300000             1        ...
Change ID: 86497023
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163353  840010002 2022.06.30 15:30:00 2022.05.01 00:00:00          0        4700000      4900000 -9223372036854775808          4500000             1        ...
Change ID: 86496511
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 163341  840010001 2022.06.30 15:30:00 2022.05.01 00:00:00          0         300000       300000 -9223372036854775808           400000             2        ...
Change ID: 86495999
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 162078  124010035 2022.06.30 15:30:00 2022.04.01 00:00:00          0        5000000      3500000 -9223372036854775808          4000000             1        ...
Change ID: 86495487
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 161954  124010021 2022.06.30 15:30:00 2022.04.01 00:00:00          0         300000       700000 -9223372036854775808                0             1        ...
Change ID: 86494975
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 167370  840140002 2022.06.30 15:30:00 2022.06.18 00:00:00          0        1328000      1315000              1331000          1358000             1        ...
Change ID: 86494463
      [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved]
[0] 167375  840140003 2022.06.30 15:30:00 2022.06.25 00:00:00          0      231750000    223500000            224500000        225837000             2        ...

*/
//+------------------------------------------------------------------+
