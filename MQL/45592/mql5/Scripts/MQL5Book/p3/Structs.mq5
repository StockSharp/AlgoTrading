//+------------------------------------------------------------------+
//|                                                      Structs.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define N_COEF 3

struct Settings
{
   datetime start;
   int barNumber;
   ENUM_APPLIED_PRICE price;
   int components;
};

struct Result
{
// public: is assumed by default for structs
   double probability;
   double coef[N_COEF];
   int direction;

private:
   string status;

public:
   void Result()
   {
      static int count = 0;
      Print(__FUNCSIG__, " ", ++count);
      status = "ok";
   }

   void Result(string s)
   {
      static int count = 0;
      Print(__FUNCSIG__, " ", ++count);
      status = s;
   }

   void ~Result()
   {
      static int count = 0;
      Print(__FUNCSIG__, " ", ++count);
   }

   void print()
   {
      Print(probability, " ", direction, " ", status);
      ArrayPrint(coef);
   }
};

//+------------------------------------------------------------------+
//| Some calculations stub                                           |
//+------------------------------------------------------------------+
Result calculate(Settings &settings)
{
   // adjust inputs: read and write struct members
   if(settings.barNumber > 1000)
   {
      settings.components = (int)(MathSqrt(settings.barNumber) + 1);
   }
   // ...
   // simulate a result
   Result r;// = {}; // 'r' - cannot be initialized with initializer list
   r.probability = 0.5;
   r.direction = +1;

   // error:
   // cannot access to private member 'status' declared in struct 'Result'
   // r.status = "message";

   for(int i = 0; i < N_COEF; i++) r.coef[i] = i + 1;
   return r;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Settings s = {D'2021.01.01', 1000, PRICE_CLOSE, 8};

   // error: '{' - parameter conversion not allowed
   // s = {D'2021.01.01', 1000, PRICE_CLOSE, 8};

   /* ok: member-wise assignment
   s.start = D'2021.01.01';
   s.barNumber = 1000;
   s.price = PRICE_CLOSE;
   s.components = 8;
   */

   Result r = calculate(s);
   // Print(r);       // error: 'r' - objects are passed by reference only
   // Print(&r);      // error: 'r' - class type expected
   r.print();
   // will output:
   // 0.5 1 ok
   // 1.00000 2.00000 3.00000
   
   Result r2("n/a");
   r2 = r;           // ok: full member-wise copy
   r2.print();
   // will output the same data:
   // 0.5 1 ok
   // 1.00000 2.00000 3.00000

   Print(offsetof(Result, status)); // 36
}
