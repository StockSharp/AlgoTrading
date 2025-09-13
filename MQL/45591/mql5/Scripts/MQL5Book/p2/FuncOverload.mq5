//+------------------------------------------------------------------+
//|                                                 FuncOverload.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

//+------------------------------------------------------------------+
//| Transpose 2x2 matrix                                             |
//+------------------------------------------------------------------+
void Transpose(double &m[][2])
{
   double temp = m[1][0];
   m[1][0] = m[0][1];
   m[0][1] = temp;
}

/*
// BUG1
// if uncommented, this function raises compiler error
// 'Transpose' - ambiguous call to overloaded function with the same parameters

void Transpose(double &m[][2], bool fast = false)
{
   // ...
}
*/

/*
// BUG2
// if uncommented, this function rasies compiler error
// 'Swap' - function already defined and has body

void Swap(double &m[][3], int i, int j)
{
   // ...
}
*/

void SwapByReference(double &m[][3], int &i, int &j)
{
   Print(__FUNCSIG__);
   // ...
}

void SwapByReference(double &m[][3], const int &i, const int &j)
{
   Print(__FUNCSIG__);
   // ...
}

//+------------------------------------------------------------------+
//| aux function for matrix transpose                                |
//+------------------------------------------------------------------+
void Swap(double &m[][3], const int i, const int j)
{
   static double temp;
   
   temp = m[i][j];
   m[i][j] = m[j][i];
   m[j][i] = temp;
}

//+------------------------------------------------------------------+
//| Transpose 3x3 matrix                                             |
//+------------------------------------------------------------------+
void Transpose(double &m[][3])
{
   Swap(m, 0, 1);
   Swap(m, 0, 2);
   Swap(m, 1, 2);
}

//+------------------------------------------------------------------+
//| Summation overloads                                              |
//+------------------------------------------------------------------+
double sum(double v1, double v2)
{
   return v1 + v2;
}

int sum(int v1, int v2)
{
   return v1 + v2;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   double a[2][2] = {{1, 2}, {3, 4}};
   Print("Before Transpose 2x2");
   ArrayPrint(a);
   Transpose(a);
   Print("After Transpose 2x2");
   ArrayPrint(a);

   double b[3][3] = {{1, 2, 3}, {4, 5, 6}, {7, 8, 9}};
   Print("Before Transpose 3x3");
   ArrayPrint(b);
   Transpose(b);
   Print("After Transpose 3x3");
   ArrayPrint(b);

   {
      int i = 0, j = 1;
      SwapByReference(b, i, j);
   }
   
   {
      const int i = 0, j = 1;
      SwapByReference(b, i, j);
   }
   
   // error
   // sum(1, 3.14); // ambiguous call to overloaded function
}
