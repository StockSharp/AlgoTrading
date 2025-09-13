#property show_inputs

#define PAUSE 100

#define STR_SHABLON "<!--STR-->"

#define MAX_CURRENCY 20  // Максимальное количество валют (не пар)

extern string Currencies = "AUD, EUR, USD, CHF, JPY, NZD, GBP, CAD, SGD, NOK, SEK, DKK, ZAR, MXN, HKD, HUF, CZK, PLN, RUR, TRY";

string Shablon = "<!--STR-->, <!--STR-->";  // Шаблон для выдирания валют из входной строки Currencies

int AmountCurrency;  // Общее количество учитываемых валют
string Currency[MAX_CURRENCY]; // Учитываемые валюты
double Volumes[MAX_CURRENCY];

string StrDelSpaces( string Str )
{
  int Pos, Length;

  Str = StringTrimLeft(Str);
  Str = StringTrimRight(Str);

  Length = StringLen(Str) - 1;
  Pos = 1;

  while (Pos < Length)
    if (StringGetChar(Str, Pos) == ' ')
    {  
      Str = StringSubstr(Str, 0, Pos) + StringSubstr(Str, Pos + 1, 0);
      Length--;
    }
    else 
      Pos++;

  return(Str);
}

int StrToStringS( string Str, string Razdelitel, string &Output[] )
{
  int Pos, LengthSh;
  int Count = 0;

  Str = StrDelSpaces(Str);
  Razdelitel = StrDelSpaces(Razdelitel);

  LengthSh = StringLen(Razdelitel);

  while (TRUE)
  {
    Pos = StringFind(Str, Razdelitel);
    Output[Count] = StringSubstr(Str, 0, Pos);
    Count++;
 
    if (Pos == -1)
      break;
 
    Pos += LengthSh;
    Str = StringSubstr(Str, Pos);
  }

  return(Count);
}

int CurrencyPos( string Str )
{
  int i = 0;
  
  while (Currency[i] != Str)  
    i++;
  
  return(i);
}

void CheckArbitrage()
{
  int i;
  string Str;
  
  for (i = 0; i < AmountCurrency; i++)
    Volumes[i] = 0;
  
  for (i = OrdersTotal() - 1; i >= 0; i--)
  {
    OrderSelect(i, SELECT_BY_POS);
    
    if (OrderType() == OP_BUY)
    {
      Str = StringSubstr(OrderSymbol(), 0, 3);
      Volumes[CurrencyPos(Str)] += OrderLots();
      
      Str = StringSubstr(OrderSymbol(), 3, 3);
      Volumes[CurrencyPos(Str)] -= OrderLots() * OrderOpenPrice();
    }
    else if (OrderType() == OP_SELL)
    {
      Str = StringSubstr(OrderSymbol(), 0, 3);
      Volumes[CurrencyPos(Str)] -= OrderLots();
      
      Str = StringSubstr(OrderSymbol(), 3, 3);
      Volumes[CurrencyPos(Str)] += OrderLots() * OrderOpenPrice();
    }
  }
  
  return;
}

string CheckString()
{
  int i;
  string Str = WindowExpertName() + ":";
  
  for (i = 0; i < AmountCurrency; i++)
    if (Volumes[i] != 0)
      Str = Str + "\n" + Currency[i] + " = " + DoubleToStr(Volumes[i], 5) + " lots";
      
  return(Str);
}

void deinit()
{
  Comment("");
  
  return;
}

void start()
{
  AmountCurrency = StrToStringS(Currencies, ",", Currency);
    
  while(!IsStopped())
  {
    RefreshRates();
    
    CheckArbitrage();
    Comment(CheckString());
    
    Sleep(PAUSE);
  }
  
  return;
}