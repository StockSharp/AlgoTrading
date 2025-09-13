//+------------------------------------------------------------------+
//|                                                      Logical.mq4 |
//|                                       Copyright © 2009, Tinytjan |
//|                                                 tinytjan@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Tinytjan"
#property link      "tinytjan@mail.ru"
#property library

/// \brief Equal to ternar operator 
/// needed = Condition ? IfTrue : IfFalse;
/// \param IfTrue
/// \param IfFalse
/// \return matching value from parameters
double DoubleIf(bool Condition, double IfTrue, double IfFalse)
{
   if (Condition) return (IfTrue);
   else           return (IfFalse);
}

/// \brief Equal to ternar operator 
/// needed = Condition ? IfTrue : IfFalse;
/// \param IfTrue
/// \param IfFalse
/// \return matching value from parameters
int IntIf(bool Condition, int IfTrue, int IfFalse)
{
   if (Condition) return (IfTrue);
   else           return (IfFalse);
}


/// \brief Equal to ternar operator 
/// needed = Condition ? IfTrue : IfFalse;
/// \param IfTrue
/// \param IfFalse
/// \return matching value from parameters
string StringIf(bool Condition, string IfTrue, string IfFalse)
{
   if (Condition) return (IfTrue);
   else           return (IfFalse);
}

/// \brief Use this function to stuck your code
void Stuck()
{
   while(!IsStopped())
   {
      Sleep(100);
   }
}