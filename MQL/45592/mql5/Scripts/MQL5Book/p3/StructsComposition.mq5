//+------------------------------------------------------------------+
//|                                           StructsComposition.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

struct Inclosure
{
   double X, Y;
};

struct Main
{
   Inclosure data;
   int code;
};

struct Main2
{
   struct Inclosure2
   {
     double X, Y;
   }
   data;
   int code;
};

struct Main3 : Inclosure
{
   int code;
   string xxx;
};

struct Base
{
   const int mode;
   string s;
   Base(const int m) : mode(m) { }
};

struct Derived : Base // 'Base' - wrong parameters count
{
   double data[10];
   Derived() : Base(1) { }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Main m = {{0.1, 0.2}, -1};   // aggregate initialization
   m.data.X = 1.0;              // member-wise access
   m.data.Y = -1.0;

   Main2 m2 = {{0.1, 0.2}, -1}; // aggregate initialization
   m2.data.X = 1.0;             // member-wise access
   m2.data.Y = -1.0;
   
   Main3 m3 = {0.1, 0.2, -1};
   m3.X = 1.0;
   m3.Y = -1.0;
   
   Inclosure in = {10, 100};
   m3 = in;
   
   Print(sizeof(Main));   // 20
   Print(sizeof(Main2));  // 20
   Print(sizeof(Main3));  // 20
}
//+------------------------------------------------------------------+
