//+------------------------------------------------------------------+
//|                                         CalendarChangeReader.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Test script to show how to read saved calendar changes from CalendarChangeSaver service."
#property script_show_inputs

#include <MQL5Book/Defines.mqh>

#define PACK_DATETIME_COUNTER(D,C) (D | (((ulong)(C)) << 48))
#define DATETIME(A) ((datetime)((A) & 0x7FFFFFFFF))
#define COUNTER(A)  ((ushort)((A) >> 48))

input string Filename = "calendar2.chn";
input datetime Start;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const long day = 60 * 60 * 24;
   datetime now = Start ? Start : (datetime)(TimeCurrent() / day * day);

   int handle = FileOpen(Filename,
      FILE_READ | FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_BIN);
   if(handle == INVALID_HANDLE)
   {
      PrintFormat("Can't open file '%s' for reading", Filename);
      return;
   }
   
   ChangeFileReader reader(handle, now);
   
   // read the file step by step, incrementing time artificially in this demo
   while(!FileIsEnding(handle))
   {
      // in real application testing could call reader.check on every tick,
      // or postpond further checks until reader.getState().dt (if it's not LONG_MAX)
      ulong records[];
      if(reader.check(now, records))
      {
         Print(now);
         ArrayPrint(records);
      }
      now += 60; // increment time by 1 minute at once, can be 1 second
   }
   
   FileClose(handle);
}

//+------------------------------------------------------------------+
//| Single change (datetime + array of affected calendar record IDs) |
//+------------------------------------------------------------------+
struct ChangeState
{
   datetime dt;
   ulong ids[];
   
   ChangeState(): dt(LONG_MAX) {}
   ChangeState(const datetime at, ulong &_ids[])
   {
      dt = at;
      ArraySwap(ids, _ids);
   }
   void operator=(const ChangeState &other)
   {
      dt = other.dt;
      ArrayCopy(ids, other.ids);
   }
};

//+------------------------------------------------------------------+
//| Class to walk through changes saved in a file for advancing time |
//+------------------------------------------------------------------+
class ChangeFileReader
{
   const int handle;
   ChangeState current;
   const ChangeState zero;
   
public:
   ChangeFileReader(const int h, const datetime start = 0): handle(h)
   {
      if(readState())
      {
         if(start)
         {
            ulong dummy[];
            check(start, dummy, true); // jump to first edit after 'start'
         }
      }
   }
   
   bool check(datetime now, ulong &records[], const bool fastforward = false)
   {
      if(current.dt > now) return false;
      
      ArrayFree(records);
      
      if(!fastforward)
      {
         ArrayCopy(records, current.ids);
         current = zero;
      }
      
      while(readState() && current.dt <= now)
      {
         if(!fastforward) ArrayInsert(records, current.ids, ArraySize(records));
      }
      
      return true;
   }
   
   bool readState()
   {
      if(FileIsEnding(handle)) return false;
      ResetLastError();
      const ulong v = FileReadLong(handle);
      current.dt = DATETIME(v);
      ArrayFree(current.ids);
      const int n = COUNTER(v);
      for(int i = 0; i < n; ++i)
      {
         PUSH(current.ids, FileReadLong(handle));
      }
      return _LastError == 0;
   }
   
   ChangeState getState() const
   {
      return current;
   }
};
//+------------------------------------------------------------------+
