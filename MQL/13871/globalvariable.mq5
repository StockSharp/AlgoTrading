//+------------------------------------------------------------------+
//|                                               GlobalVariable.mq5 |
//|                              Copyright © 2015, Vladimir Karputov |
//|                                           http://wmua.ru/slesar/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Vladimir Karputov"
#property link      "http://wmua.ru/slesar/"
#property version    "1.00"
#property description "Example of work with global variables"
//---
string   strGlobalId="main_id";
int      magic=1010458;
double   variable_rezult;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- 
   Print(__FUNCTION__);
//--- Initialize the generator of random numbers
   MathSrand(GetTickCount());
//---
   string FullName=GetFullName(strGlobalId,magic); // get name: main_id_1010458
   if(!GlobalVariableCheck(FullName)) // if the global variable doesn't exist
     {
      CalculateValueGlobalVariable(variable_rezult);  // it is necessary to calculate value
      ResetLastError();
      if(GlobalVariableSet(FullName,variable_rezult)==0) // creates a new global variable
        {
         Print("Failed to creates a new global variable ",GetLastError());
         return(INIT_FAILED);
        }
      Print("value of global variable = ",variable_rezult);
     }
   else
     {
      ResetLastError();
      if(!GlobalVariableGet(FullName,variable_rezult))
        {
         Print("Failed to get a value of global variable ",GetLastError());
         return(INIT_FAILED);
        }
      Print("value of global variable = ",variable_rezult);
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Print(__FUNCTION__);
//---
   string FullName=GetFullName(strGlobalId,magic); // get name: main_id_1010458
   if(!GlobalVariableCheck(FullName)) // if the global variable doesn't exist
     {
      CalculateValueGlobalVariable(variable_rezult);  // it is necessary to calculate value
      ResetLastError();
      if(GlobalVariableSet(FullName,variable_rezult)==0) // creates a new global variable
        {
         Print("Failed to creates a new global variable ",GetLastError());
        }
      Print("value of global variable = ",variable_rezult);
     }
   else
     {
      CalculateValueGlobalVariable(variable_rezult);  // it is necessary to calculate value
      ResetLastError();
      if(GlobalVariableSet(FullName,variable_rezult)==0) // set a new valie of global variable
        {
         Print("Failed to sets a new global variable ",GetLastError());
        }
      Print("new value of global variable = ",variable_rezult);
     }
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Print(__FUNCTION__);
  }
//+------------------------------------------------------------------+
//| return a full name of a variable                                 |
//+------------------------------------------------------------------+
string GetFullName(const string name,const int number)
  {
   string text=name+IntegerToString(number);
   return(text);
  }
//+------------------------------------------------------------------+
//| calculation value of the global variable                         |
//+------------------------------------------------------------------+
bool CalculateValueGlobalVariable(double &rezult)
  {
   rezult=MathRand()/100.0;
//---
   return(true);
  }
//+------------------------------------------------------------------+
