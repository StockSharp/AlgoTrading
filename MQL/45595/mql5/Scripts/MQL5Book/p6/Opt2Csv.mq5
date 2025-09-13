//+------------------------------------------------------------------+
//|                                                      Opt2Csv.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Optimization cache file (opt-file) reader. Exports the header, the inputs and the records into 3 CSV files."

#include <MQL5Book/OptReader.mqh>

input string OptFilename = "";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   string opt = OptFilename + "\n";
   StringReplace(opt, ".opt\n", "");
   OptReader reader(OptFilename);
   reader.print();
   reader.header2CSV(opt + "-header.csv");
   reader.inputs2CSV(opt + "-inputs.csv");
   reader.export2CSV(opt + "-data.csv");
}
//+------------------------------------------------------------------+
