//+------------------------------------------------------------------+
//|                                                    AppliedTo.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
enum APPLIED_TO
{
   APPLIED_TO_DEFAULT_CLOSE_NA, // Default (Close or Not applicable)
   APPLIED_TO_CLOSE,    // Close
   APPLIED_TO_OPEN,     // Open
   APPLIED_TO_HIGH,     // High
   APPLIED_TO_LOW,      // Low
   APPLIED_TO_MEDIAN,   // Median
   APPLIED_TO_TYPICAL,  // Typical
   APPLIED_TO_WEIGHTED, // Weighted
   APPLIED_TO_PREVIOUS, // Previous
   APPLIED_TO_FIRST,    // First
   APPLIED_TO_HANDLE_10,// Handle 10
   APPLIED_TO_HANDLE_11,// Handle 11
   APPLIED_TO_HANDLE_12,// Handle 12
   APPLIED_TO_HANDLE_13,// Handle 13
   APPLIED_TO_HANDLE_14,// Handle 14
   APPLIED_TO_HANDLE_15,// Handle 15
                        // any number of handles may follow
};

#define APPLIED_TO_STR() EnumToString((APPLIED_TO)_AppliedTo)

//+------------------------------------------------------------------+
