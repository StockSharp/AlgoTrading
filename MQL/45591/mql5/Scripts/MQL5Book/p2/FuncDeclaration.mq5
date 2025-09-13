//+------------------------------------------------------------------+
//|                                              FuncDeclaration.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

// forward declaration
int Init(const int v);

// before the forward declaration added above,
// here was the error: 'Init' - undeclared identifier
int k = Init(-1);

//+------------------------------------------------------------------+
//| Initialization wrapper with printing                             |
//+------------------------------------------------------------------+
int Init(const int v)
{
   Print("Init: ", v);
   return v;
}

//+------------------------------------------------------------------+
//| FuncBy... Value/Reference demo case                              |
//+------------------------------------------------------------------+
void FuncByValue(int v)
{
   ++v;
   // ... more code
}

// error
// 'FuncByValue' - function already defined and has body
/*
void FuncByValue(const int v)
{
   // ++v;
}
*/

void FuncByReference(int &v)
{
   ++v;
}

void FuncByConstReference(const int &v)
{
   // error
   // ++v; // 'v' - constant cannot be modified
   Print(v); // can access/read v
}

void FuncByValueDummy(int v); // declared, unused, undefined

//+------------------------------------------------------------------+
//| Transpose an array demo case                                     |
//+------------------------------------------------------------------+
void Transpose(double &m[][2])
{
   double temp = m[1][0];
   m[1][0] = m[0][1];
   m[0][1] = temp;
}

void TransposeVector(double &v[])
{
}

//+------------------------------------------------------------------+
//| Find largest value among given ones                              |
//+------------------------------------------------------------------+
double Largest(const double v1, const double v2 = -DBL_MAX,
               const double v3 = -DBL_MAX)
{
   return v1 > v2 ? (v1 > v3 ? v1 : v3) : (v2 > v3 ? v2 : v3);
}

/*
double Largest2(const double v1, const double v2 = -DBL_MAX,
               const double v3) // missing default value for parameter
{
   return v1 > v2 ? (v1 > v3 ? v1 : v3) : (v2 > v3 ? v2 : v3);
}
*/

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int i = 0;
   FuncByValue(i);          // i can't change
   Print(i);                // 0
   FuncByReference(i);      // i can change
   Print(i);                // 1
   FuncByConstReference(i); // i can't change, 1
   
   const int j = 1;
   // error
   // 'j' - constant variable cannot be passed as reference
   // FuncByReference(j);
   
   FuncByValue(10);         // ok
   
   // error: '10' - parameter passed as reference, variable expected
   // FuncByReference(10);
   
   double a[2][2] = {{-1, 2}, {3, 0}};
   Print("Before Transpose");
   ArrayPrint(a);
   Transpose(a);
   Print("After Transpose");
   ArrayPrint(a);
   
   // error
   // TransposeVector(a); // 'a' - parameter conversion not allowed
   
   // Print(Largest()); // error: wrong parameters count
   Print(Largest(1));       // ok: 1
   Print(Largest(0, -2));   // ok: 0
   Print(Largest(1, 2, 3)); // ok: 3
}
//+------------------------------------------------------------------+
