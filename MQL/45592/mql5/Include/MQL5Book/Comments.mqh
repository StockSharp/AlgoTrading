//+------------------------------------------------------------------+
//|                                                     Comments.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/StringUtils.mqh>

// default buffer size is 10 lines of text
#ifndef N_LINES
#define N_LINES 10
#define N_LINES_DEFINED
#endif

//+------------------------------------------------------------------+
//| Multiline comment storage and display tool                       |
//+------------------------------------------------------------------+
class Comments
{
   const int capacity; // maximum number of lines
   const bool reverse; // order of display (true means recents on top)
   string lines[];     // text buffer
   int cursor;         // where to place next string
   int size;           // actual number of lines stored
   
public:
   Comments(const int limit = N_LINES, const bool r = false):
      capacity(limit), reverse(r), cursor(0), size(0)
   {
      ArrayResize(lines, capacity);
   }
   
   void add(const string line);
   void clear();
};

//+------------------------------------------------------------------+
//| Clean up the chart comment and internal buffer                   |
//+------------------------------------------------------------------+
void Comments::clear()
{
   Comment("");
   cursor = 0;
   size = 0;
}

//+------------------------------------------------------------------+
//| Add new line(s) of text to the chart comment                     |
//+------------------------------------------------------------------+
void Comments::add(const string line)
{
   if(line == NULL)
   {
      clear();
      return;
   }
   
   // if input string contains several lines
   // split it by newline character into array
   string inputs[];
   const int n = StringSplit(line, '\n', inputs);
   
   // add new line(s) into the ring buffer
   // at the cursor position (overwriting most outdated records)
   // cursor is always incremented modulo capacity (reset to 0 on overflow)
   for(int i = 0; i < n; ++i)
   {
      lines[cursor] = inputs[reverse ? n - i - 1 : i];
      cursor = (cursor + 1) % capacity;
      if(size < capacity) ++size;
   }

   // combine all text from the buffer in direct or reverse order
   // newline character is used as a glue
   string result = "";
   for(int i = 0, k = size == capacity ? cursor % capacity : 0; i < size; ++i, k = ++k % capacity)
   {
      if(reverse)
      {
         result = lines[k] + "\n" + result;
      }
      else
      {
         result += lines[k] + "\n";
      }
   }
   
   // finally output the result
   Comment(result);
}

//+------------------------------------------------------------------+
//| Continuous comment feed will show most recent posts on top       |
//| in reverse chronological order                                   |
//+------------------------------------------------------------------+
void MultiComment(const string line = NULL)
{
   static Comments com(N_LINES, true);
   com.add(line);
}

//+------------------------------------------------------------------+
//| Bulk posts with multiple lines are better to show in natural     |
//| chronological order (full story reading goes from top to bottom) |
//+------------------------------------------------------------------+
void ChronoComment(const string line = NULL)
{
   static Comments com(N_LINES, false);
   com.add(line);
}

#ifdef N_LINES_DEFINED
#undef N_LINES
#endif
//+------------------------------------------------------------------+
