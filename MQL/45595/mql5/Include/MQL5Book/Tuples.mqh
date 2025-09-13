//+------------------------------------------------------------------+
//|                                                       Tuples.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| A set of templated structures to hold various data fields        |
//| while using MonitorInterface (Orders, Deals, Positions) and      |
//| TradeFilter<MonitorInterface<>,ENUM_INT,ENUM_DOUBLE,ENUM_STRING> |
//|   T1,T2,T3+: long, double, string in any combination             |
//|   M is expected to be a MonitorInterface<>                       |
//+------------------------------------------------------------------+

/*
   // EXAMPLE: request profit, symbol, ticket for positions
   //          by specific magic-number, sorted by profit
   
   #include <MQL5Book/Tuples.mqh>
   #include <MQL5Book/PositionFilter.mqh>
   #property script_show_inputs
   
   input ulong Magic;
   
   void OnStart()
   {
      int props[] = {POSITION_PROFIT, POSITION_SYMBOL, POSITION_TICKET};
      Tuple3<double,string,ulong> tuples[];
      PositionFilter filter;
      filter.let(POSITION_MAGIC, Magic).select(props, tuples, true);
      ArrayPrint(tuples);
   }

*/

//+------------------------------------------------------------------+
//| 2-Tuple with fields of arbitrary types                           |
//+------------------------------------------------------------------+
template<typename T1,typename T2>
struct Tuple2
{
   T1 _1;
   T2 _2;
   
   static int size() { return 2; };

   template<typename M>
   void assign(const int &properties[], M &m)
   {
      if(ArraySize(properties) != size()) return;
      _1 = m.get(properties[0], _1);
      _2 = m.get(properties[1], _2);
   }
};

//+------------------------------------------------------------------+
//| 3-Tuple with fields of arbitrary types                           |
//+------------------------------------------------------------------+
template<typename T1,typename T2,typename T3>
struct Tuple3
{
   T1 _1;
   T2 _2;
   T3 _3;
   
   static int size() { return 3; };

   template<typename M>
   void assign(const int &properties[], M &m)
   {
      if(ArraySize(properties) != size()) return;
      _1 = m.get(properties[0], _1);
      _2 = m.get(properties[1], _2);
      _3 = m.get(properties[2], _3);
   }
};

//+------------------------------------------------------------------+
//| 4-Tuple with fields of arbitrary types                           |
//+------------------------------------------------------------------+
template<typename T1,typename T2,typename T3,typename T4>
struct Tuple4
{
   T1 _1;
   T2 _2;
   T3 _3;
   T4 _4;
   
   static int size() { return 4; };

   template<typename M>
   void assign(const int &properties[], M &m)
   {
      if(ArraySize(properties) != size()) return;
      _1 = m.get(properties[0], _1);
      _2 = m.get(properties[1], _2);
      _3 = m.get(properties[2], _3);
      _4 = m.get(properties[3], _4);
   }
};

//+------------------------------------------------------------------+
//| 5-Tuple with fields of arbitrary types                           |
//+------------------------------------------------------------------+
template<typename T1,typename T2,typename T3,typename T4,typename T5>
struct Tuple5
{
   T1 _1;
   T2 _2;
   T3 _3;
   T4 _4;
   T5 _5;
   
   static int size() { return 5; };

   template<typename M>
   void assign(const int &properties[], M &m)
   {
      if(ArraySize(properties) != size()) return;
      _1 = m.get(properties[0], _1);
      _2 = m.get(properties[1], _2);
      _3 = m.get(properties[2], _3);
      _4 = m.get(properties[3], _4);
      _5 = m.get(properties[4], _5);
   }
};

//+------------------------------------------------------------------+
//| N-Tuples: up to 8 fields supported                               |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| 8-Tuple with fields of arbitrary types                           |
//+------------------------------------------------------------------+
template<typename T1,typename T2,typename T3,typename T4,typename T5,typename T6,typename T7,typename T8>
struct Tuple8
{
   T1 _1;
   T2 _2;
   T3 _3;
   T4 _4;
   T5 _5;
   T6 _6;
   T7 _7;
   T8 _8;
   
   static int size() { return 8; };

   template<typename M>
   void assign(const int &properties[], M &m)
   {
      if(ArraySize(properties) != size()) return;
      _1 = m.get(properties[0], _1);
      _2 = m.get(properties[1], _2);
      _3 = m.get(properties[2], _3);
      _4 = m.get(properties[3], _4);
      _5 = m.get(properties[4], _5);
      _6 = m.get(properties[5], _6);
      _7 = m.get(properties[6], _7);
      _8 = m.get(properties[7], _8);
   }
};
//+------------------------------------------------------------------+
