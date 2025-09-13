//+------------------------------------------------------------------+
//|                                                    neverwolf.mq5 |
//|                                             Copyright neverwolf. |
//|                          https://www.mql5.com/en/users/neverwolf |
//+------------------------------------------------------------------+
#property copyright "Copyright Neverwolf."
#property link      "https://www.mql5.com/en/users/neverwolf"
#property version   "1.00"


input ENUM_TIMEFRAMES Period_TF           = PERIOD_M5; // Indicator Timeframe


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(checkTimeframePeriods() == false)
     {
      return(INIT_FAILED);
      //return(INIT_PARAMETERS_INCORRECT);
      ExpertRemove();
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+

//---
bool checkTimeframePeriods()
  {
   int allowedPeriods[] = {PERIOD_M5, PERIOD_M10, PERIOD_M15, PERIOD_M20, PERIOD_M30,
                           PERIOD_H1, PERIOD_H2, PERIOD_H3, PERIOD_H4, PERIOD_H6,
                           PERIOD_H8, PERIOD_H12, PERIOD_D1, PERIOD_W1, PERIOD_MN1
                          };

   int periodsToCheck[] = {Period_TF //Include all variables that you have to verify.
                          };

   int allowedPeriodsSize = ArraySize(allowedPeriods);
   int periodsToCheckSize = ArraySize(periodsToCheck);

   for(int i = 0; i < periodsToCheckSize; i++)
     {
      bool isInAllowedPeriods = false;

      for(int j = 0; j < allowedPeriodsSize; j++)
        {
         if(periodsToCheck[i] == allowedPeriods[j])
           {
            isInAllowedPeriods = true;
            break;
           }
        }

      if(!isInAllowedPeriods)
        {
         return false;
        }
     }

   return true;
  }
//---
//---