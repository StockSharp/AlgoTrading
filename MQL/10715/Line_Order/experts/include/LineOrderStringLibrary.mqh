//+------------------------------------------------------------------+
//|                                       LineOrderStringLibrary.mq4 |
//|                                                            Chris |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Chris"
#property link      ""

/*
In this library you will find all the different functions to convert the string from the description to the final value which
will be used by the LineOrder. Each input needs to be converted from a string into the relevent type hence why each function
will have it's own function to sort out the string. To add a new function to check, create a new else if at the end of processString
then if you wish add in minimum variables check then "return(the new function name(explodeVar))". explodeVar is the variable array 
which has all the function parameters in.

*/
double processString(string function,string explodeVar[])
{
// Start of the custom description functions
if(function=="SMA")
{
int minimum_var = 5;
if(ArraySize(explodeVar)!=minimum_var){
if(ArraySize(explodeVar)>minimum_var)Print("Sorry there is too many variables. "+ArraySize(explodeVar));else Print("Sorry there is too few variables");
return(0);
}
if(iVolume(Symbol(),return_timeframe(explodeVar[0]),0)<100){Sleep(1000)/*Allow the indicators to update;Print("less than 100 volume")*/; SMA(explodeVar);}
return (SMA(explodeVar));
}else if(function=="ATR")
{
return (ATR(explodeVar));
}else if(function=="Fibb")
{
return (Fibb(explodeVar));
}else if(function=="ADX")
{
return (ADX(explodeVar));
}else if(function=="pip"||function=="Pip"||function=="PIP")
{
return (Pip(explodeVar));
}

}

double SMA(string var[])
{
// SMA(Timeframe,MA period,MA shift, applied price,shift)
double value = NormalizeDouble(iMA(Symbol(),return_timeframe(var[0]),StrToInteger(var[1]),StrToInteger(var[2]),MODE_SMA,return_price_constant(var[3]),StrToInteger(var[4])),Digits);
return (value);
}
double ATR(string var[])
{
// ATR(Timeframe, period, shift)
double value = iATR(Symbol(),return_timeframe(var[0]),StrToInteger(var[1]),StrToInteger(var[2]));
return (NormalizeDouble(value,Digits));
}
double Fibb(string var[])
{
double value=0;
int timeframe = return_timeframe(var[0]);
int startShift = StrToInteger(var[1]);
int endShift = StrToInteger(var[2]);
string dir = var[3];
double level = StrToDouble(var[4]);
//double levelStore[] = {};
return (value);
}
double ADX(string var[])
{

}
double Pip(string var[]){
return (StrToDouble(var[0])*Point.pip);
}