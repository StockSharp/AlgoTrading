//+------------------------------------------------------------------+
//|                                          DBQuotesIntradayLag.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Display quotes imported into a database, with LAGs.\nUse DBquotesImport.mq5 to generate and populate the database beforehand."
#property script_show_inputs

#include <MQL5Book/DBSQLite.mqh>
#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input string Database = "MQL5Book/DB/Quotes";
input datetime SubsetStart = D'2022.01.01';
input datetime SubsetStop = D'2023.01.01';
input int Limit = 10;

const string Table = "MqlRatesDB";

#resource "DBQuotesIntradayLag.sql" as string sql1

/*
   Copy & paste example for SQL query in MetaEditor DB viewer
   
   SELECT
      DATETIME(time, 'unixepoch') as datetime,
      time,
      TIME(time, 'unixepoch') AS intraday,
      STRFTIME('%w', time, 'unixepoch') AS day,
      (LAG(open,-1) OVER (ORDER BY time) - open) AS delta,
      SIGN(open - LAG(open) OVER (ORDER BY time)) AS direction,
      (LAG(open,-1) OVER (ORDER BY time) - open) * (open - LAG(open) OVER (ORDER BY time)) AS product,
      (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
   FROM MqlRatesDB
   WHERE (time >= STRFTIME('%s', '2015-01-01') AND time < STRFTIME('%s', '2021-01-01'))
*/

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("");
   DBSQLite db(Database + _Symbol + PeriodToString());
   if(!PRTF(db.isOpen())) return;
   if(!PRTF(db.hasTable(Table))) return;

   // custom "preparation" of SQL-query for formatting
   string sqlrep = sql1;
   // single percent sign would be consumed by StringFormat,
   // we need to preserve it 'as is' for proper SQL execution
   StringReplace(sqlrep, "%", "%%");
   StringReplace(sqlrep, "?1", "%ld");
   StringReplace(sqlrep, "?2", "%ld");
   StringReplace(sqlrep, "?3", "%d");
   
   // actual parameter substitution
   const string sqlfmt = StringFormat(sqlrep, SubsetStart, SubsetStop, Limit);
   Print(sqlfmt);
   
   // SQL-query execution and print out
   DatabasePrint(db.getHandle(), sqlfmt, 0);
}
//+------------------------------------------------------------------+
/*

   db.isOpen()=true / ok
   db.hasTable(Table)=true / ok
         SELECT
            DATETIME(time, 'unixepoch') as datetime,
            time,
            TIME(time, 'unixepoch') AS intraday,
            STRFTIME('%w', time, 'unixepoch') AS day,
            (LAG(open,-1) OVER (ORDER BY time) - open) AS delta,
            SIGN(open - LAG(open) OVER (ORDER BY time)) AS direction,
            (LAG(open,-1) OVER (ORDER BY time) - open) * (open - LAG(open) OVER (ORDER BY time)) AS product,
            (LAG(open,-1) OVER (ORDER BY time) - open) * SIGN(open - LAG(open) OVER (ORDER BY time)) AS estimate
         FROM MqlRatesDB
         WHERE (time >= 1640995200 AND time < 1672531200)
         ORDER BY time LIMIT 10;
    #| datetime               open       time intraday day                 delta direction               product              estimate
   --+--------------------------------------------------------------------------------------------------------------------------------
    1| 2022-01-03 00:00:00 1.13693 1641168000 00:00:00 1    0.000320000000000098                                                       
    2| 2022-01-03 01:00:00 1.13725 1641171600 01:00:00 1    2.99999999999745e-05         1  9.59999999999478e-09  2.99999999999745e-05 
    3| 2022-01-03 02:00:00 1.13728 1641175200 02:00:00 1    -0.00106000000000006         1 -3.17999999999748e-08  -0.00106000000000006 
    4| 2022-01-03 03:00:00 1.13622 1641178800 03:00:00 1   -0.000340000000000007        -1  3.60400000000028e-07  0.000340000000000007 
    5| 2022-01-03 04:00:00 1.13588 1641182400 04:00:00 1    -0.00157999999999991        -1  5.37199999999982e-07   0.00157999999999991 
    6| 2022-01-03 05:00:00  1.1343 1641186000 05:00:00 1    0.000529999999999919        -1 -8.37399999999827e-07 -0.000529999999999919 
    7| 2022-01-03 06:00:00 1.13483 1641189600 06:00:00 1   -0.000769999999999937         1 -4.08099999999905e-07 -0.000769999999999937 
    8| 2022-01-03 07:00:00 1.13406 1641193200 07:00:00 1   -0.000260000000000149        -1  2.00200000000098e-07  0.000260000000000149 
    9| 2022-01-03 08:00:00  1.1338 1641196800 08:00:00 1     0.00051000000000001        -1 -1.32600000000079e-07  -0.00051000000000001 
   10| 2022-01-03 09:00:00 1.13431 1641200400 09:00:00 1    0.000480000000000036         1  2.44800000000023e-07  0.000480000000000036 

*/
//+------------------------------------------------------------------+
