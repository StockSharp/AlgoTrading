//+------------------------------------------------------------------+
//|                                                    OptReader.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/StructPrint.mqh>

#define TYPE_OFFSET 75
#define _STR(A) StringFormat("%s,%s\n", #A, (string)(A))
#define ASTR(A) StringFormat("%s,\"%s\"\n", #A, ShortArrayToString(A))

//+------------------------------------------------------------------+
//| Opt-file header                                                  |
//+------------------------------------------------------------------+
struct TesterOptCacheHeader
{
   uint          version;                // cache version
   ushort        copyright[64];          // copyright
   ushort        name[16];               // signature "TesterOptCache"
   int           head_reserve[66];
   uint          header_size;            // overall header size
   uint          record_size;            // single test record size including optimized parameters
   ushort        expert_name[64];        // expert name
   ushort        expert_path[128];       // expert name and path from MQL5
   ushort        server[64];             // trade server
   ushort        symbol[32];             // instrument
   ushort        period;                 // timeframe
   datetime      date_from;              // test start date
   datetime      date_to;                // test end date
   datetime      date_forward;           // forward end date
   int           opt_mode;               // optimization mode (0-complete, 1-genetics, 2/3-forward)
   int           ticks_mode;             // tick generation mode
   int           last_criterion;         // optimization criterion
   uint          msc_min;                // minimal duration of a single pass (milliseconds)
   uint          msc_max;                // maximal duration of a single pass (milliseconds)
   uint          msc_avg;                // average duration of a single pass (milliseconds)
   int           common_reserve[16];

   ushort        group[80];              // group
   ushort        trade_currency[32];     // account currency
   int           trade_deposit;          // initial deposit
   int           trade_condition;        // trading conditions (0-no delays, -1-arbitrary delays, nnn-millisecond delays)
   int           trade_leverage;         // leverage
   int           trade_hedging;          // 1 - netting, 2 - hedging
   int           trade_currency_digits;  // account currency digits
   int           trade_pips;             // pips mode
   int           trade_reserve[5];

   char          hash_ex5[16];           // hash of expert's binary file
   uint          parameters_size;        // size of the buffer for all parameters (common)
   uint          parameters_total;       // number of parameters in total
   uint          opt_params_size;        // size of the buffer for optimized parameters (per pass)
   uint          opt_params_total;       // number of optimized parameters
   uint          dwords_cnt;             // size of a pass in dwords
   uint          snapshot_size;          // snapshot size
   uint          passes_total;           // number of passes to cover o-space (0 for genetics)
   uint          passes_passed;          // number of completed passes

   string CSVtext()
   {
      return "Field,Value\n"
         + _STR(version) + ASTR(copyright) + ASTR(name) + ASTR(expert_name)
         + ASTR(expert_path) + ASTR(server) + ASTR(symbol) + _STR(period)
         + _STR(date_from) + _STR(date_to) + _STR(date_forward)
         + _STR(opt_mode) + _STR(ticks_mode) + _STR(last_criterion)
         + _STR(msc_min) + _STR(msc_max) + _STR(msc_avg)
         + ASTR(group) + ASTR(trade_currency)
         + _STR(trade_deposit) + _STR(trade_condition) + _STR(trade_leverage)
         + _STR(trade_hedging) + _STR(trade_currency_digits) + _STR(trade_pips)
         + _STR(parameters_size) + _STR(parameters_total) + _STR(opt_params_size) + _STR(opt_params_total)
         + _STR(dwords_cnt) + _STR(snapshot_size) + _STR(passes_total) + _STR(passes_passed);
   }
};

//+------------------------------------------------------------------+
//| Input struct (raw, as is)                                        |
//+------------------------------------------------------------------+
struct TestCacheInput
{
   ushort            name[64];           // parameter name
   int               flag;               // optimized (yes-1, no-0)
   int               type;               // data type TYPE_XXX
   int               digits;             // number of digits
   int               offset;             // offset in a buffer of parameters (used separately for common and per pass buffers)
   int               size;               // size (in bytes) of the value in the buffer
   int               unknown;
   
   union Range                           // 0 - start, 1 - step, 2 - stop
   {
      long integers[3];
      double numbers[3];
   } range;
   
   template<typename T>
   union Number
   {
      T V;
      uchar A[sizeof(T)];
      Number(const uchar &buffer[], const int offset)
      {
         ArrayCopy(A, buffer, 0, offset, sizeof(T)); 
      }
   };
   
   string CSVname() const
   {
      return ShortArrayToString(name);
   }
   
   string CSVvalue(const uchar &buffer[], const int opt = -1)
   {
      if(size == 0) return "";
      int _offset = (opt == -1) ? offset : opt;
      const ENUM_DATATYPE t = (ENUM_DATATYPE)(type - TYPE_OFFSET);
      switch(t)
      {
      case TYPE_BOOL: {Number<bool> r(buffer, _offset); return (string)r.V;}
      case TYPE_CHAR: {Number<char> r(buffer, _offset); return (string)r.V;}
      case TYPE_UCHAR: {Number<uchar> r(buffer, _offset); return CharToString(r.V);}
      case TYPE_SHORT: {Number<short> r(buffer, _offset); return (string)r.V;}
      case TYPE_USHORT: {Number<ushort> r(buffer, _offset); return ShortToString(r.V);}
      case TYPE_COLOR:
      case TYPE_INT:
      case TYPE_UINT:
      default: {Number<uint> r(buffer, _offset); return (string)(r.V);}
      case TYPE_DATETIME:  {Number<datetime> r(buffer, _offset); return TimeToString(r.V, TIME_DATE | TIME_SECONDS);}
      case TYPE_LONG:
      case TYPE_ULONG: {Number<ulong> r(buffer, _offset); return (string)(r.V);}

      case TYPE_FLOAT:
         {
            Number<float> r(buffer, _offset);
            return (r.V == FLT_MAX) ? "~" : (string)(r.V);
         }
      case TYPE_DOUBLE:
         {
            Number<double> r(buffer, _offset);
            return (r.V == DBL_MAX) ? "~" : (string)(r.V);
         }
      case TYPE_STRING:
         {
            ushort a[];
            ArrayResize(a, size / 2);
            for(int i = 0; i < size / 2; ++i)
            {
               a[i] = buffer[offset + i * 2] | (buffer[offset + i * 2 + 1] << 8);
            }
            return ShortArrayToString(a);
         }
      }
      
      return NULL;
   }
};

//+------------------------------------------------------------------+
//| Input struct with stringified fields                             |
//+------------------------------------------------------------------+
struct TestCacheInputExtended: public TestCacheInput
{
   string _name;
   string _type;
   string _range;
   void extend()
   {
      _name = ShortArrayToString(name);
      _type = EnumToString((ENUM_DATATYPE)(type - TYPE_OFFSET));
      const ENUM_DATATYPE t = (ENUM_DATATYPE)(type - TYPE_OFFSET);
      switch(t)
      {
      default:
         _range = "[" + (string)range.integers[0]
            + "/" + (string)range.integers[1] + "/" + (string)range.integers[2] + "]";
         break;

      case TYPE_FLOAT:
      case TYPE_DOUBLE:
         _range = "[" + (string)range.numbers[0]
            + "/" + (string)range.numbers[1] + "/" + (string)range.numbers[2] + "]";
      }
   }
};

//+------------------------------------------------------------------+
//| Base struct for optimization pass                                |
//+------------------------------------------------------------------+
struct TestCacheRecord
{
   ulong pass;
};

//+------------------------------------------------------------------+
//| Trade optimization pass by input parameters                      |
//+------------------------------------------------------------------+
struct ExpTradeSummary: public TestCacheRecord
{
   union UD
   {
      struct Doubles
      {
         double            deposit;             // initial deposit
         double            withdrawal;          // withdrawals
         double            profit;              // net profit
         double            grossprofit;         // gross profit
         double            grossloss;           // gross loss
         double            maxprofit;           // maximal profitable trade
         double            minprofit;           // maximal loss trade
         double            conprofitmax;        // profit of longest sequence of profitable trades
         double            maxconprofit;        // maximal profit in a sequence of profitable trades
         double            conlossmax;          // loss of longest sequence of loss trades
         double            maxconloss;          // maximal loss in a sequence of loss trades
         double            balancemin;          // minimal balance (for balance absolute drawdown)
         double            maxdrawdown;         // maximal absolute balance drawdown
         double            drawdownpercent;     // maximal absolute balance drawdown (in percents)
         double            reldrawdown;         // maximal relative balance drawdown (in money)
         double            reldrawdownpercent;  // maximal relative balance drawdown
         double            equitymin;           // minimal equity (for equity absolute drawdown)
         double            maxdrawdown_e;       // maximal absolute equity drawdown
         double            drawdownpercent_e;   // maximal absolute equity drawdown (in percents)
         double            reldrawdown_e;       // maximal relative equity drawdown (in money)
         double            reldrawdownpercnt_e; // maximal relative equity drawdown
         double            expected_payoff;     // average trade result
         double            profit_factor;       // profit factor
         double            recovery_factor;     // recovery factor
         double            sharpe_ratio;        // Sharpe ratio
         double            margin_level;        // minimal margin level
         double            custom_fitness;      // custom criterion value - OnTester result
      };
      double array[27];
   } doubles;
   union UI
   {
      struct Integers
      {
         int               deals;               // total number of deals
         int               trades;              // number of trades (out/inout deals)
         int               profittrades;        // number of profitable trades
         int               losstrades;          // number of loss trades
         int               shorttrades;         // number of sells
         int               longtrades;          // number of buys
         int               winshorttrades;      // number of profitable sells
         int               winlongtrades;       // number of profitable buys
         int               conprofitmax_trades; // longest sequence of profit trades
         int               maxconprofit_trades; // maximal profit in a sequence of profit trades
         int               conlossmax_trades;   // longest sequence of loss trades
         int               maxconloss_trades;   // maximal loss in a sequence of loss trades
         int               avgconwinners;       // average number of trades in a sequence of profits
         int               avgconloosers;       // average number of trades in a sequence of losses
      };
      int array[14];
   } integers;
   
   static string CSVheader()
   {
      return "Pass,Deposit,Withdrawal,Profit,Grossprofit,Grossloss,Maxprofit,"
      "Minprofit,Conprofitmax,Maxconprofit,Conlossmax,Maxconloss,Balancemin,"
      "Maxdrawdown,Drawdownpercent,Reldrawdown,Reldrawdownpercent,Equitymin,"
      "Maxdrawdown_E,Drawdownpercent_E,Reldrawdown_E,Reldrawdownpercnt_E,"
      "Expected_Payoff,Profit_Factor,Recovery_Factor,Sharpe_Ratio,Margin_Level,Custom_Fitness,"
      "Deals,Trades,Profittrades,Losstrades,Shorttrades,Longtrades,Winshorttrades,Winlongtrades,"
      "Conprofitmax_Trades,Maxconprofit_Trades,Conlossmax_Trades,Maxconloss_Trades,"
      "Avgconwinners,Avgconloosers";
   }
   
   string CSVline() const
   {
      string result = (string)pass;
      for(int i = 0; i < ArraySize(doubles.array); ++i)
      {
         result += "," + (doubles.array[i] == DBL_MAX ? "~" : DoubleToString(doubles.array[i], 2));
      }
      for(int i = 0; i < ArraySize(integers.array); ++i)
      {
         result += "," + (string)integers.array[i];
      }
      return result;
   }
};

//+------------------------------------------------------------------+
//| Trade optimization pass by symbol                                |
//+------------------------------------------------------------------+
struct ExpTradeSummaryBySymbols: public ExpTradeSummary
{
   ushort watch[32];
   
   static string CSVheader()
   {
      return ExpTradeSummary::CSVheader() + ",Symbol";
   }
   
   string CSVline() const
   {
      return ExpTradeSummary::CSVline() + "," + ShortArrayToString(watch);
   }
};

//+------------------------------------------------------------------+
//| Math optimization pass                                           |
//+------------------------------------------------------------------+
struct MathTestCacheRecord: public TestCacheRecord
{
   double fitness;

   static string CSVheader()
   {
      return "Pass,Fitness";
   }
   
   string CSVline() const
   {
      return (string)pass + "," + DoubleToString(fitness);
   }
};

#define SAFE(A) {const uint _ = A; if(!_) return false;}

//+------------------------------------------------------------------+
//| Single optimization pass interface                               |
//+------------------------------------------------------------------+
class RecordBase
{
public:
   virtual int size() const = 0;
   virtual bool read(const int handle, const int opt, const int gen) = 0;
   virtual void print() const = 0;
   virtual void optimized(uchar &buffer[], const int i) const = 0;
   virtual string CSVheader() const = 0;
   virtual string CSVline(const int i) const = 0;
};

//+------------------------------------------------------------------+
//| Single optimization pass with inputs data                        |
//+------------------------------------------------------------------+
template<typename T>
struct ExtendedRecord
{
   T result;
   uchar bufferOfOptimizedInputs[];
   uchar genetics[];
   
   bool read(const int handle, const int opt, const int gen)
   {
      SAFE(FileReadStruct(handle, result));
      SAFE(FileReadArray(handle, bufferOfOptimizedInputs, 0, opt));
      SAFE(FileReadArray(handle, genetics, 0, gen));
      return true;
   }
   
   void print() const
   {
      StructPrint(result);
   }
};

//+------------------------------------------------------------------+
//| Array of optimization passes of T type                           |
//+------------------------------------------------------------------+
template<typename T>
class RecordArray: public RecordBase
{
   T array[];
public:
   RecordArray(const int allocate)
   {
      ArrayResize(array, allocate);
   }
   
   virtual int size() const override
   {
      return ArraySize(array);
   }
   
   virtual bool read(const int handle, const int opt, const int gen) override
   {
      for(int i = 0; i < ArraySize(array); ++i)
      {
         array[i].read(handle, opt, gen);
      }
      return true;
   }
   
   virtual void print() const override
   {
      for(int i = 0; i < ArraySize(array); ++i)
      {
         array[i].print();
      }
   }
   
   virtual string CSVheader() const override
   {
      return array[0].result.CSVheader();
   }
   
   virtual string CSVline(const int i) const override
   {
      return array[i].result.CSVline();
   }
   
   virtual void optimized(uchar &buffer[], const int i = -1) const override
   {
      ArrayCopy(buffer, array[i].bufferOfOptimizedInputs);
   }
};

//+------------------------------------------------------------------+
//| Main class to read opt-file and export it to CSVs                |
//+------------------------------------------------------------------+
class OptReader
{
   TesterOptCacheHeader header;
   TestCacheInputExtended inputs[];
   uchar bufferOfInputs[];
   int shapshot[];
   AutoPtr<RecordBase> records;
   
   bool read(const int handle)
   {
      SAFE(FileReadStruct(handle, header));
      if(header.parameters_total)
      {
         TestCacheInput temp[];
         SAFE(FileReadArray(handle, temp, 0, header.parameters_total));
         const int n = ArrayResize(inputs, header.parameters_total);
         for(int i = 0; i < n; ++i)
         {
            inputs[i] = temp[i];
            inputs[i].extend();
         }
      }
      if(header.parameters_size)
      {
         SAFE(FileReadArray(handle, bufferOfInputs, 0, header.parameters_size));
      }
      if(header.snapshot_size)
      {
         SAFE(FileReadArray(handle, shapshot, 0, header.snapshot_size));
      }
      uint size = header.record_size - header.opt_params_size - header.dwords_cnt * sizeof(int);
      uint appendix = 0;
      switch(size)
      {
      case sizeof(ExpTradeSummary) + sizeof(long):
         appendix = sizeof(long);
      case sizeof(ExpTradeSummary):
         records = new RecordArray<ExtendedRecord<ExpTradeSummary>>(header.passes_passed);
         break;
      case sizeof(ExpTradeSummaryBySymbols) + sizeof(long):
         appendix = sizeof(long);
      case sizeof(ExpTradeSummaryBySymbols):
         records = new RecordArray<ExtendedRecord<ExpTradeSummaryBySymbols>>(header.passes_passed);
         break;
      case sizeof(MathTestCacheRecord) + sizeof(long):
         appendix = sizeof(long);
      case sizeof(MathTestCacheRecord):
         records = new RecordArray<ExtendedRecord<MathTestCacheRecord>>(header.passes_passed);
         break;
      default:
         Print("Unknown TestCacheRecord side: ", size);
         return false;
      }
      records[].read(handle, header.opt_params_size, header.dwords_cnt * sizeof(int) + appendix);
      return true;
   }
public:
   OptReader(const string filename)
   {
      const int handle = FileOpen(filename, FILE_READ | FILE_BIN | FILE_SHARE_READ | FILE_SHARE_WRITE);
      if(handle != INVALID_HANDLE)
      {
         read(handle);
      }
      FileClose(handle);
   }
   
   string CSVheader() const
   {
      string result = records[].CSVheader();
      for(int i = 0; i < ArraySize(inputs); ++i)
      {
         if(inputs[i].flag)
         {
            result += "," + inputs[i].CSVname();
         }
      }
      return result;
   }
   
   string CSVline(const int p) const
   {
      string result = records[].CSVline(p);
      uchar buf[];
      records[].optimized(buf, p);
      for(int i = 0, k = 0; i < ArraySize(inputs); ++i)
      {
         if(inputs[i].flag)
         {
            result += "," + inputs[i].CSVvalue(buf, k++ * sizeof(long));
         }
      }
      return result;
   }
   
   void print() const
   {
      StructPrint(header);
      ArrayPrint(inputs);
      const int size = ArraySize(inputs);
      for(int i = 0; i < size; ++i)
      {
         Print((inputs[i].flag ? "*" : ""),
            inputs[i].CSVname(), "=", inputs[i].CSVvalue(bufferOfInputs));
      }
   }
   
   void inputs2CSV(const string name)
   {
      int output = FileOpen(name, FILE_WRITE | FILE_CSV | FILE_ANSI);
      if(output != INVALID_HANDLE)
      {
         FileWriteString(output, "Parameter,Value,Opt\n");
         const int size = ArraySize(inputs);
         for(int i = 0; i < size; ++i)
         {
            string value = inputs[i].CSVvalue(bufferOfInputs);
            if(StringFind(value, ",") > -1) value = "\"" + value + "\"";
            FileWriteString(output, inputs[i].CSVname() + "," + value + "," + (inputs[i].flag ? "Y" : "N") + "\n");
         }
         PrintFormat("File '%s' saved", name);
      }
      else
      {
         PrintFormat("File '%s' failed", name);
      }
   }
   
   void header2CSV(const string name)
   {
      int output = FileOpen(name, FILE_WRITE | FILE_CSV | FILE_ANSI);
      if(output != INVALID_HANDLE)
      {
         FileWriteString(output, header.CSVtext());
         FileClose(output);
         PrintFormat("File '%s' saved", name);
      }
      else
      {
         PrintFormat("File '%s' failed", name);
      }
   }
   
   void export2CSV(const string name)
   {
      int output = FileOpen(name, FILE_WRITE | FILE_CSV | FILE_ANSI);
      if(output != INVALID_HANDLE)
      {
         FileWriteString(output, CSVheader() + "\n");
         for(int i = 0; i < records[].size(); ++i)
         {
            FileWriteString(output, CSVline(i) + "\n");
         }
         FileClose(output);
         PrintFormat("File '%s' saved", name);
      }
      else
      {
         PrintFormat("File '%s' failed", name);
      }
   }
};
//+------------------------------------------------------------------+
/*

   OptReader reader(OptFilename);
   reader.print();
   reader.header2CSV(OptFilename + "-header.csv");
   reader.inputs2CSV(OptFilename + "-inputs.csv");
   reader.export2CSV(OptFilename + "-data.csv");

*/
//+------------------------------------------------------------------+
