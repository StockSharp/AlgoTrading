//+------------------------------------------------------------------+
//|                                                     EnvError.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "Matrix.mqh"

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // make sure matrix inversion works without errors
   Print("Test matrix inversion (should pass)");
   double a[9] =
   {
      1,  2,  3,
      4,  5,  6,
      7,  8,  0,
   };
   
   ResetLastError();
   MatrixSquare mA(a);   // assign data to source matrix
   Print("Input");
   mA.print();
   MatrixSquare mAinv(3);
   mAinv = !mA;          // invert it and store in new matrix
   Print("Result");
   mAinv.print();
   
   Print("Check inverted by multiplication");
   MatrixSquare test(3); // check inversion via multiplication
   test = mA * mAinv;
   test.print();         // get identity matrix

   Print(EnumToString(Matrix::lastError())); // ok

   // try invert another matrix
   Print("Test matrix inversion (should fail)");
   double b[9] =
   {
     -22, -7, 17,
     -21, 15,  9,
     -34,-31, 33
   };
   
   MatrixSquare mB(b);
   Print("Input");
   mB.print();
   ResetLastError();
   Print("Result");
   (!mB).print();
   Print(EnumToString(Matrix::lastError())); // singular

   Print("Empty matrix creation");
   MatrixSquare m0(0);
   Print(EnumToString(Matrix::lastError()));

   Print("'Rectangular' square matrix creation");
   MatrixSquare r12(1, 2);
   Print(EnumToString(Matrix::lastError()));
}
//+------------------------------------------------------------------+
/*
   example output
   
   Test matrix inversion (should pass)
   Input
   1.00000 2.00000 3.00000
   4.00000 5.00000 6.00000
   7.00000 8.00000 0.00000
   Result
   -1.77778  0.88889 -0.11111
    1.55556 -0.77778  0.22222
   -0.11111  0.22222 -0.11111
   Check inverted by multiplication
    1.00000 +0.00000  0.00000
    -0.00000   1.00000  +0.00000
   0.00000 0.00000 1.00000
   ERR_USER_MATRIX_OK
   Test matrix inversion (should fail)
   Input
   -22.00000  -7.00000  17.00000
   -21.00000  15.00000   9.00000
   -34.00000 -31.00000  33.00000
   Result
   0.0 0.0 0.0
   0.0 0.0 0.0
   0.0 0.0 0.0
   ERR_USER_MATRIX_SINGULAR
   Empty matrix creation
   ERR_USER_MATRIX_EMPTY
   'Rectangular' square matrix creation
   ERR_USER_MATRIX_NOT_SQUARE
*/
//+------------------------------------------------------------------+
