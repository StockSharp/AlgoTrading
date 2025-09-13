//+------------------------------------------------------------------+
//|                                                           RRS EA |
//|                                     Copyright 2025, RRS Value EA |
//|                                             rajeeevrrs@gmail.com |
//+------------------------------------------------------------------+
#include <stdlib.mqh>
#property  strict

input int MAX_TICKS = 100; // User-defined number of ticks
input int CHECK_SECONDS = 1; // User-defined time interval in seconds

double tickPrices[]; // Dynamically allocated array

double spreadValues[]; // Array to store spreads
int tickIndex = 0;
bool filled = false;
datetime lastCheckTime = 0;
double avgPipMovementTick;
double avgSpreadTick;

//+------------------------------------------------------------------+
//| Initialization function                                         |
//+------------------------------------------------------------------+
int OnInit()
  {
   ArrayResize(tickPrices, MAX_TICKS); // Resize array based on user input
   ArrayResize(spreadValues, MAX_TICKS); // Resize spread array
   lastCheckTime = TimeCurrent();
   return INIT_SUCCEEDED;
  }

//+------------------------------------------------------------------+
//| OnTick function                                                 |
//+------------------------------------------------------------------+
void OnTick()
  {
// Store the latest tick price
   tickPrices[tickIndex] = Bid;
   spreadValues[tickIndex] = MarketInfo(Symbol(), MODE_SPREAD); // Store spread in pips
   tickIndex++;

// Check if buffer is fully filled
   if(tickIndex >= MAX_TICKS)
     {
      tickIndex = 0;  // Reset index for circular buffer
      filled = true;
     }

// Compute average pip movement per tick and average spread per tick
   if(filled)
     {
      double totalMovement = 0.0;
      double totalSpread = 0.0;

      // Calculate absolute price movements
      for(int i = 1; i < MAX_TICKS; i++)
        {
         totalMovement += MathAbs((tickPrices[i] - tickPrices[i - 1]) / Point);
        }

      // Compute average pip movement per tick
      avgPipMovementTick = totalMovement / (MAX_TICKS - 1);

      // Calculate average spread
      for(int j = 0; j < MAX_TICKS; j++)
        {
         totalSpread += spreadValues[j];
        }
      avgSpreadTick = totalSpread / MAX_TICKS;

      // Print tick-based results
      Print("Average pip movement per tick: ", avgPipMovementTick);
      Print("Average spread per tick: ", avgSpreadTick);
     }

// Check if the specified time interval has passed
   if(TimeCurrent() - lastCheckTime >= CHECK_SECONDS)
     {
      lastCheckTime = TimeCurrent();

      if(filled)
        {
         double totalMovementSec = 0.0;
         double totalSpreadSec = 0.0;

         // Calculate absolute price movements
         for(int k = 1; k < MAX_TICKS; k++)
           {
            totalMovementSec += MathAbs((tickPrices[k] - tickPrices[k - 1]) / Point);
           }

         // Compute average pip movement per second
         double avgPipMovementSec = totalMovementSec / (MAX_TICKS - 1);

         // Calculate average spread
         for(int l = 0; l < MAX_TICKS; l++)
           {
            totalSpreadSec += spreadValues[l];
           }
         double avgSpreadSec = totalSpreadSec / MAX_TICKS;

         // Print second-based results
         Print("Average pip movement per ", CHECK_SECONDS, " seconds: ", avgPipMovementSec);
         Print("Average spread over last ", CHECK_SECONDS, " seconds: ", avgSpreadSec);

         // Display all results in the chart window
         Comment("Average pip movement per tick: ", avgPipMovementTick, "\n",
                 "Average spread per tick: ", avgSpreadTick, "\n",
                 "Average pip movement per ", CHECK_SECONDS, " seconds: ", avgPipMovementSec, "\n",
                 "Average spread over last ", CHECK_SECONDS, " seconds: ", avgSpreadSec +
                 "\n------------------------------------------------" +
                 "\n:: Email                           : rajeeevrrs@gmail.com " +
                 "\n:: Telegram                     : @rajeevrrs " +
                 "\n:: Skype                         : rajeev-rrs " +
                 "\n------------------------------------------------");
        }
     }
  }
//+------------------------------------------------------------------+
