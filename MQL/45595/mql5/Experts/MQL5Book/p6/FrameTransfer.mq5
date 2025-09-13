//+------------------------------------------------------------------+
//|                                                FrameTransfer.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property link "https://www.mql5.com"
#property description "Example of sending and receiving data frames during EA optimization."

#property tester_set "FrameTransfer.set"
#property tester_no_cache

#include <MQL5Book/MqlError.mqh>

input bool Parameter0;
input long Parameter1;
input double Parameter2;
input string Parameter3;

#define MY_FILE_ID 100
#define MY_TIME_ID 101

ulong startup; // single pass timing (just a demo data)
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   startup = GetMicrosecondCount();
   MathSrand((uint)startup);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Test completion event handler                                    |
//+------------------------------------------------------------------+
double OnTester()
{
   // send a file in one frame
   const static string filename = "binfile";
   int h = FileOpen(filename, FILE_WRITE | FILE_BIN | FILE_ANSI);
   FileWriteString(h, StringFormat("Random: %d", MathRand()));
   FileClose(h);
   FrameAdd(filename, MY_FILE_ID, MathRand(), filename);
   
   // send an array in another frame
   ulong dummy[1];
   dummy[0] = GetMicrosecondCount() - startup;
   FrameAdd("timing", MY_TIME_ID, 0, dummy);
   
   return (Parameter2 + 1) * (Parameter1 + 2);
}

//+------------------------------------------------------------------+
//| Adjust optimization range to the limit (to simplify the demo)    |
//+------------------------------------------------------------------+
template<typename T>
void LimitParameterCount(const string name, const int limit)
{
   bool enabled;
   T value, start, step, stop;
   if(ParameterGetRange(name, enabled, value, start, step, stop))
   {
      if(enabled && step > 0)
      {
         if((stop - start) / step > limit)
         {
            ParameterSetRange(name, enabled, value, start, step, start + step * limit);
         }
      }
   }
}

int handle; // a file to collect all custom results

//+------------------------------------------------------------------+
//| Optimization start                                               |
//+------------------------------------------------------------------+
void OnTesterInit()
{
   handle = FileOpen("output.csv", FILE_WRITE | FILE_CSV | FILE_ANSI, ",");
   LimitParameterCount<long>("Parameter1", 10);
   LimitParameterCount<double>("Parameter2", 10);
}

//+------------------------------------------------------------------+
//| Receive data with MY_FILE_ID                                     |
//+------------------------------------------------------------------+
void ProcessFileFrames()
{
   static ulong framecount = 0; // our own counter of frame count
   
   // frame fields
   ulong   pass;
   string  name;
   long    id;
   double  value;
   // when the array is used to read a file from a frame,
   // the datatype of the array must have a size that
   // the file size is divisible by it without a remainder,
   // so most universal types are:
   // - uchar for binary files and ANSI text files
   // - ushort for text files in Unicode
   // otherwise you'll get INVALID_ARRAY(4006) error
   uchar   data[];
   
   // input parameters for the pass where the frame belongs
   string  params[];
   uint    count;
   
   ResetLastError();
   
   while(FrameNext(pass, name, id, value, data))
   {
      PrintFormat("Pass: %lld Frame: %s Value:%f", pass, name, value);
      if(id != MY_FILE_ID) continue;
      if(FrameInputs(pass, params, count))
      {
         string header, record;
         if(framecount == 0) // prepare CSV header
         {
            header = "Counter,Pass ID,";
         }
         record = (string)framecount + "," + (string)pass + ",";
         // let collect values of optimized parameters
         for(uint i = 0; i < count; i++)
         {
            string name2value[];
            int n = StringSplit(params[i], '=', name2value);
            if(n == 2)
            {
               long pvalue, pstart, pstep, pstop;
               bool enabled = false;
               if(ParameterGetRange(name2value[0], enabled, pvalue, pstart, pstep, pstop))
               {
                  if(enabled)
                  {
                     if(framecount == 0) // prepare CSV header
                     {
                        header += name2value[0] + ",";
                     }
                     record += name2value[1] + ","; // data field
                  }
               }
            }
         }
         if(framecount == 0) // prepare CSV header
         {
            FileWriteString(handle, header + "Value,File Content\n");
         }
         // write data records into CSV
         FileWriteString(handle, record + DoubleToString(value) + ","
            + CharArrayToString(data) + "\n");
      }
      framecount++;
   }
   
   if(_LastError != 4000 && _LastError != 0)
   {
      Print("Error: ", E2S(_LastError));
   }
}

//+------------------------------------------------------------------+
//| Optimization pass (frame)                                        |
//+------------------------------------------------------------------+
void OnTesterPass()
{
   ProcessFileFrames(); // standard processing of frames
}

//+------------------------------------------------------------------+
//| End of optimization                                              |
//+------------------------------------------------------------------+
void OnTesterDeinit()
{
   ProcessFileFrames(); // final clean-up: some frames may be late
   FileClose(handle);   // close CSV-file

   ulong   pass;
   string  name;
   long    id;
   double  value;
   ulong   data[]; // use the same datatype as it was at sending of the array

   FrameFilter("timing", MY_TIME_ID); // rewind to first frames
   
   ulong count = 0;
   ulong total = 0;
   // loop through 'timing' frames only
   while(FrameNext(pass, name, id, value, data))
   {
      if(ArraySize(data) == 1)
      {
         total += data[0];
      }
      else
      {
         total += (ulong)value;
      }
      ++count;
   }
   if(count > 0)
   {
      PrintFormat("Average timing: %lld", total / count);
   }
}
//+------------------------------------------------------------------+
