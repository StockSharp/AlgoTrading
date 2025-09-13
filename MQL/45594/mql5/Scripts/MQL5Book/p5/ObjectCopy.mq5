//+------------------------------------------------------------------+
//|                                                   ObjectCopy.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates a copy for every selected object.             |
//+------------------------------------------------------------------+
#include <MQL5Book/ObjectMonitor.mqh>

#define PUSH(A,V) (A[ArrayResize(A, ArraySize(A) + 1) - 1] = V)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int flags[2048];
   // fill in the array with values of possible properties
   // some invalid values will be dropped on reading from object
   for(int i = 0; i < ArraySize(flags); ++i)
   {
      flags[i] = i;
   }

   // collect names of selected objects in the following array
   string selected[];   
   const int n = ObjectsTotal(0);
   for(int i = 0; i < n; ++i)
   {
      const string name = ObjectName(0, i);
      if(ObjectGetInteger(0, name, OBJPROP_SELECTED))
      {
         PUSH(selected, name);
      }
   }
   
   // loop through objects to copy
   for(int i = 0; i < ArraySize(selected); ++i)
   {
      const string name = selected[i];
      
      // backup all properties of current object
      ObjectMonitor object(name, flags);
      object.print();
      object.backup();
      const string copy = GetFreeName(name);
      
      if(StringLen(copy) > 0)
      {
         Print("Copy name: ", copy);
         ObjectCreate(0, copy,
            (ENUM_OBJECT)ObjectGetInteger(0, name, OBJPROP_TYPE),
            ObjectFind(0, name), 0, 0);
         object.name(copy);
         object.restore();
      }
      else
      {
         Print("Can't create copy name for: ", name);
      }
   }
}

//+------------------------------------------------------------------+
//| Generate a name of copy of given object, like "Original/Copy №x" |
//+------------------------------------------------------------------+
string GetFreeName(const string name)
{
   const string suffix = "/Copy №";
   const int pos = StringFind(name, suffix);
   string prefix;
   int n;
   
   if(pos <= 0)
   {
      const string candidate = name + suffix + "1";
      // check if a name with "/Copy №1" suffix is vacant, and return it if so
      if(ObjectFind(0, candidate) < 0)
      {
         return candidate;
      }
      prefix = name;
      n = 0;
   }
   else
   {
      prefix = StringSubstr(name, 0, pos);
      n = (int)StringToInteger(StringSubstr(name, pos + StringLen(suffix)));
   }
   
   Print("Found: ", prefix, " ", n);
   for(int i = n + 1; i < 1000; ++i) // try to create no more than 1000 copies
   {
      const string candidate = prefix + suffix + (string)i;
      // check if an object with the name ending with "Copy №i" exists already
      if(ObjectFind(0, candidate) < 0)
      {
         return candidate;
      }
   }
   return NULL;
}
//+------------------------------------------------------------------+
