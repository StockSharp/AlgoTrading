//+------------------------------------------------------------------+
//|                                            FXTticksCollector.mq4 |
//|                      Copyright © 2006, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2006, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

#include <FXTHeader.mqh>

int      ExtHandle=-1;
string   ExtFileName;
int      ExtBars;
int      ExtTicks;
datetime ExtLastBarTime;

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   ExtLastBarTime=0;
   ExtTicks=0;
   ExtBars=0;
   ExtFileName=Symbol()+Period()+"_0.fxt";
   ExtHandle=FileOpen(ExtFileName,FILE_BIN|FILE_READ);
//----
   if(ExtHandle<0)
     {
      //---- file does not exist
      WriteHeaderAndFirstBars();
      if(ExtHandle<0) return;
     }
   else
     {
      if(!ReadAndCheckHeader(ExtHandle,Period(),ExtBars))
        {
         //---- file is wrong
         FileClose(ExtHandle);
         WriteHeaderAndFirstBars();
        }
      else
        {
         CheckWrittenBars();
         //---- reopen file for write
         FileClose(ExtHandle);
         ExtHandle=FileOpen(ExtFileName,FILE_BIN|FILE_READ|FILE_WRITE);
         FileSeek(ExtHandle,0,SEEK_END);
         //---- check for absentee bars after last run
         if(ExtLastBarTime<Time[0])
           {
            int shift=iBarShift(NULL,0,ExtLastBarTime,true);
            if(shift>0)
              {
               int period_seconds=Period()*60;
               int added=0;
               //---- add missing bars
               while(shift>0)
                 {
                  datetime open_time=Time[shift];
                  FileWriteInteger(ExtHandle, open_time, LONG_VALUE);
                  FileWriteDouble(ExtHandle, Open[shift], DOUBLE_VALUE);
                  FileWriteDouble(ExtHandle, Low[shift], DOUBLE_VALUE);
                  FileWriteDouble(ExtHandle, High[shift], DOUBLE_VALUE);
                  FileWriteDouble(ExtHandle, Close[shift], DOUBLE_VALUE);
                  FileWriteDouble(ExtHandle, Volume[shift], DOUBLE_VALUE);
                  FileWriteInteger(ExtHandle, open_time+period_seconds-1, LONG_VALUE);
                  FileWriteInteger(ExtHandle, 1, LONG_VALUE);
                  shift--;
                  added++;
                  if(ExtLastBarTime<open_time) ExtBars++;
                 }
               Print("There were ",added," bars from ",TimeToStr(ExtLastBarTime)," added.");
              }
           }
        }
     }
//----
   WriteLastTick();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//---- finalize header
   if(ExtHandle>0)
     {
      //---- store processed bars amount
      FileFlush(ExtHandle);
      FileSeek(ExtHandle,88,SEEK_SET);
      FileWriteInteger(ExtHandle,ExtBars,LONG_VALUE);
      //---- zeroize fields "for internal use"
      FileFlush(ExtHandle);
      FileSeek(ExtHandle,344,SEEK_SET);
      for(int i=0; i<64; i++) FileWriteInteger(ExtHandle,0,LONG_VALUE);
      //----
      FileClose(ExtHandle);
      ExtHandle=0;
      Print(ExtTicks," ticks added. ",ExtBars," bars finalized in the header");
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if(ExtHandle>0) WriteLastTick();
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteHeaderAndFirstBars()
  {
   ExtHandle=FileOpen(ExtFileName,FILE_BIN|FILE_WRITE);
   if(ExtHandle<0) return;
//----
   int start_bar=Bars;
   if(Bars>100) start_bar=100;
   WriteHeader(ExtHandle,Symbol(),Period(),start_bar);
//---- first 100 bars as is
   int period_seconds=i_period*60;
   for(int i=start_bar; i>0; i--,ExtBars++)
     {
      datetime cur_open=Time[i];
      FileWriteInteger(ExtHandle, cur_open, LONG_VALUE);
      FileWriteDouble(ExtHandle, Open[i], DOUBLE_VALUE);
      FileWriteDouble(ExtHandle, Low[i], DOUBLE_VALUE);
      FileWriteDouble(ExtHandle, High[i], DOUBLE_VALUE);
      FileWriteDouble(ExtHandle, Close[i], DOUBLE_VALUE);
      FileWriteDouble(ExtHandle, Volume[i], DOUBLE_VALUE);
      //---- generated current time for bar state
      FileWriteInteger(ExtHandle, cur_open+period_seconds-1, LONG_VALUE);
      //---- flag 0 - testing is not evaluated
      FileWriteInteger(ExtHandle, 0, LONG_VALUE);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckWrittenBars()
  {
   int bars_count=0;
//----
   ExtLastBarTime=0;
   FileSeek(ExtHandle,600,SEEK_SET);
//----
   while(!IsStopped())
     {
      datetime open_time=FileReadInteger(ExtHandle, LONG_VALUE);
      if(FileIsEnding(ExtHandle)) break;
      //---- check for bar changing
      if(ExtLastBarTime!=open_time)
        {
         ExtLastBarTime=open_time;
         bars_count++;
        }
      //---- set position to next bar
      FileSeek(ExtHandle,48,SEEK_CUR);
     }
//----
   if(ExtBars!=bars_count)
     {
      Print("Wrong bars count ",ExtBars," in the FXT-header. Should be ",bars_count);
      ExtBars=bars_count;
     }
//----
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void WriteLastTick()
  {
//---- check for bar changing
   if(ExtLastBarTime!=Time[0])
     {
      ExtLastBarTime=Time[0];
      ExtBars++;
     }
//----
   FileWriteInteger(ExtHandle, ExtLastBarTime, LONG_VALUE);
   FileWriteDouble(ExtHandle, Open[0], DOUBLE_VALUE);
   FileWriteDouble(ExtHandle, Low[0], DOUBLE_VALUE);
   FileWriteDouble(ExtHandle, High[0], DOUBLE_VALUE);
   FileWriteDouble(ExtHandle, Close[0], DOUBLE_VALUE);
   FileWriteDouble(ExtHandle, Volume[0], DOUBLE_VALUE);
   FileWriteInteger(ExtHandle, CurTime(), LONG_VALUE);
   FileWriteInteger(ExtHandle, 4, LONG_VALUE);
//----
   ExtTicks++;
  }
//+------------------------------------------------------------------+