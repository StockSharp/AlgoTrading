#property show_inputs

#include <stdlib.mqh> // string ErrorDescription(int error_code);

#define PAUSE 100   // ����� � ������������� ����� ���������
#define ALPHA 0.001 // ��� ��������� �������

#define STR_SHABLON "<!--STR-->"

#define MAX_CURRENCY 20  // ������������ ���������� ����� (�� ���)
#define MAX_REALSYMBOLS 380  // ������������ ��������� ����������� �������� ������� �� Market Watch (== (MAX_CURRENCY * (MAX_CURRENCY - 1)))
#define MAX_ALLSYMBOLS 380  // ������������ ���������� ��������� �������� (== (MAX_CURRENCY * (MAX_CURRENCY - 1)))
#define MAX_VARIANTSYMBOLS 74 // ������������ ���������� ��������� ��������� ������� (== (4 * MAX_CURRENCY - 6))
#define MAX_VARIANTPAIRS 5402 // ������������ ���������� ��������� ��� �������� (== (MAX_VARIANTSYMBOLS * (MAX_VARIANTSYMBOLS - 1))

extern string Currencies = "AUD, EUR, USD, CHF, JPY, NZD, GBP, CAD, SGD, NOK, SEK, DKK, ZAR, MXN, HKD, HUF, CZK, PLN, RUR, TRY";
extern double MinPips = 0.5; // ����������� ����������� ������� ��������� � "������" �������
extern int SlipPage = 0; // ���������� SlipPage ��� Market-�������� (�� ������ ��������)
extern bool Lock = FALSE; // ��������� ���� ��� ���
extern double Lots = 1; // ����� ������� �� ���������������� �������
extern double MaxLot = 20; //
extern double MinLot = 0.1; // ���������� �������� ��-�� ������������� ������������� �������� MarketInfo
extern bool Monitoring = TRUE; // ����������� ������ ���� ����������� ���������� � ���� ��� ��� (������ �������� �����, ������� �������� ��� ���������)
extern int TimeToWrite = 5; // ����� ����� ����� (� �������) ����� ������������ � ���� �������������� ������ �� ���������

int DigitsLot; // ���������� ���� ����� ������� � ���������� ������ �����
string SymbolPrefix = "";  // �������, ������� �������� � Market Watch ����� ������������ ��������� ����

string Shablon = "<!--STR-->, <!--STR-->";  // ������ ��� ��������� ���� ������ �� ������. ����� ��� Currencies

int AmountCurrency;  // ����� ���������� ����������� �����
string Currency[MAX_CURRENCY]; // ����������� ������

int AmountRealSymbols;  // ����� ���������� ����������� �������� �������� �� Market Watch
string RealSymbols[MAX_REALSYMBOLS];  // ������ ����������� (��� ���������) �������� �������� �������� �� Market Watch
string REALSymbols[MAX_REALSYMBOLS];  // ������ �������� (� ������ ���������) �������� �������� �������� �� Market Watch

int AmountAllSymbols; // ����� ���������� ��������������� ��� (�������� + �������������)
int AllSymbols[MAX_ALLSYMBOLS][MAX_VARIANTSYMBOLS]; // [k][m] - ������ ������ �������� �������� ��� �������� k-�� �������
int Math[MAX_ALLSYMBOLS][MAX_VARIANTSYMBOLS]; // [k][m] - ������ ���. �������� �������� k-�� ������� �� m-� �������� ��������
int Count[MAX_ALLSYMBOLS];  // [k] - ���������� ��������� ��������� k-�� �������.

double PointD[MAX_ALLSYMBOLS]; // [k] - ������ ������ (�������) ������  k-�� �������
double MinPipsD[MAX_ALLSYMBOLS]; // [k] - ������ MinPips ������ �������  k-�� �������

double Bids[MAX_ALLSYMBOLS][MAX_VARIANTSYMBOLS];  // [k][m] - ������ Bid �-�� ������� ����������� �� m-� �������� ��������
double Asks[MAX_ALLSYMBOLS][MAX_VARIANTSYMBOLS];  // [k][m] - ������ Ask �-�� ������� ����������� �� m-� �������� ��������

double BidsReal[MAX_REALSYMBOLS]; // Bid-���� �������� ��������
double AsksReal[MAX_REALSYMBOLS]; // Ask-���� �������� ��������

double Position[MAX_REALSYMBOLS];  // ������ ����������� � ����� �������� �������� ��� ��������
double XPosition[MAX_ALLSYMBOLS][MAX_VARIANTPAIRS];  // [k][] - ������ ����������� ����������� (+/-) � ����� ����� ���� ��������� ��������� k-�� ������� 
bool XTrade[MAX_ALLSYMBOLS][MAX_VARIANTPAIRS];  // [k][] - ������ ����������/������ �� ������������� ��������� ����� ����� ���������� ��������� k-�� �������

// ��� ����� ����������
int CountArbitrage[MAX_ALLSYMBOLS][MAX_VARIANTPAIRS];  // [k][] - ������ ���������� ����������� ����� ���� ��������� ��������� k-�� ������� 
int MaxCountArbitrage;

int PrevTime, CurrentTime; // ���������� (��� ������ ����������) � ������� ����� �������

string StrOut = ""; // ����� � ���  

//------------------------------------------------------------------------------------------------------------------------------

/************************************************* BEGIN BLOCK_1 *************************************************/
// ���� �������, �������������� � �������� ��� �������������:
// string StrDelSpaces( string Str );
// int StrToStringS( string Str, string Razdelitel, string &Output[] );
// bool RealSymbol( string Str );
// void GetRealSymbols();
// int GetNumRealSymbol( string Str );
// void GetAllSymbols();
// void GetRealBidAsk();
// string SymbolToStr( int i, int j );
// void GetDataLot( string Symb );
// void GetSymbolPrefix( string Symb );
// void GetXTrade( string FileName );
// void GetPipsD();
// void InitArbitrage();
// void PrintXTrade();
// void PrintBeginInfo();
/************************************************* BEGIN BLOCK_1 *************************************************/

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

// �������� �� ���������� (���� �� � Market Watch) �������
// ����������: TRUE - ��������, FALSE - �������������
bool RealSymbol( string Str )
{
  return(MarketInfo(Str + SymbolPrefix, MODE_BID) != 0);
}

// �������� � RealSymbols[] ��� ����������� �������� ������� �� Market Watch
void GetRealSymbols()
{
  int i, j;
  string Str;

  AmountRealSymbols = 0;

  for (i = 0; i < AmountCurrency; i++)
    for (j = 0; j < AmountCurrency; j++)
      if (i != j)  // ���� ������ ���� �� ������ �����
      {
        Str = Currency[i] + Currency[j];  // �������� ��������� ���������� ���
     
        if (RealSymbol(Str))
        {
          RealSymbols[AmountRealSymbols] = Str;
          REALSymbols[AmountRealSymbols] = Str + SymbolPrefix;  // � ���������� ����������� ��� ��������� ��������
          AmountRealSymbols++;
        }
      }

  return;
}

// ���������� ����� �������, ��� ��������� ������ Str � RealSymbols[]. ��� ������� ���������� -1.
int GetNumRealSymbol( string Str )
{
  int i;

  for (i = 0; i < AmountRealSymbols; i++)
    if (RealSymbols[i] == Str)
      return(i);

  return(-1);
}

// �������� � AllSymbols[][] �������� ��������������� ���
void GetAllSymbols()
{
  int i, j, k, m;
  string Str, Str1[4], Str2[4];

  AmountAllSymbols = 0; // ���������������� ���������� ����������� ��������������� ���
  
  for (i = 0; i < MAX_ALLSYMBOLS; i++)
    Count[i] = 0;  // ���������������� ���������� ��������� ��������� ����������� ��������������� ���

  for (i = 0; i < AmountCurrency; i++) // ������ ���� ������ ij (�� ��������!!!)
    for (j = 0; j < AmountCurrency; j++)
      if (i != j)  // ���� ������ �������� �� ������ �����
      {
        Str = Currency[i] + Currency[j];  // ������� ���� ij
     
        if (RealSymbol(Str)) // ���� ���� ��������, �� (������) ������� �� ��������� - ��� ����.
        {
          AllSymbols[AmountAllSymbols][Count[AmountAllSymbols]] = GetNumRealSymbol(Str);
          Math[AmountAllSymbols][Count[AmountAllSymbols]] = -1; // -2 - "1 / S"; -1 - "S"; 0 - "S1 / S2"; 1 - "S1 * S2"; 2 - "1 / (S1 * S2)"; 3 - "S2 / S1"
          Count[AmountAllSymbols]++;
        }

        Str = Currency[j] + Currency[i];  // ������� �������� ���� - ji

        if (RealSymbol(Str)) // ���� �������� ���� ��������, �� ������� ��������� ������ ���� - 1 / ��������.
        {
            AllSymbols[AmountAllSymbols][Count[AmountAllSymbols]] = GetNumRealSymbol(Str);
            Math[AmountAllSymbols][Count[AmountAllSymbols]] = -2; // -2 - "1 / S"; -1 - "S"; 0 - "S1 / S2"; 1 - "S1 * S2"; 2 - "1 / (S1 * S2)"; 3 - "S2 / S1"
            Count[AmountAllSymbols]++;
        }

        for (k = 0; k < AmountCurrency; k++)
          if ((k != i) && (k != j))
          {
            // �������� ��������� 4-� �������� ��������� ij-����
            
            Str1[0] = Currency[i] + Currency[k];
            Str1[1] = Currency[i] + Currency[k];
            Str1[2] = Currency[k] + Currency[i];
            Str1[3] = Currency[k] + Currency[i];
         
            Str2[0] = Currency[j] + Currency[k];
            Str2[1] = Currency[k] + Currency[j];
            Str2[2] = Currency[j] + Currency[k];
            Str2[3] = Currency[k] + Currency[j];
         
            for (m = 0; m < 4; m++)
              if (RealSymbol(Str1[m]) && RealSymbol(Str2[m]))
              {
                // ����� ������ �������� ����� ���� ��� (Str1 � Str2) ����� ������
                AllSymbols[AmountAllSymbols][Count[AmountAllSymbols]] = GetNumRealSymbol(Str1[m]) * AmountRealSymbols + GetNumRealSymbol(Str2[m]);
                Math[AmountAllSymbols][Count[AmountAllSymbols]] = m; // 0 - "S1 / S2"; 1 - "S1 * S2"; 2 - "1 / (S1 * S2)"; 3 - "S2 / S1"
                Count[AmountAllSymbols]++;
              }
          }
          
        if (Count[AmountAllSymbols] >= 2) // ���� ��������� ��������� ij-���� �� ������ ���� 
                                          // (����������� ��� �������� ������������ �������������, ����� - �������� �� >= 1),
                                          // ��������� ��� ���� ������
          AmountAllSymbols++; // ��������� ���������� ����������� ��������������� ���
        else  // ����� - �� ���������
          Count[AmountAllSymbols] = 0;
      }

  return;
}

void GetRealBidAsk()
{
  for (int i = 0; i < AmountRealSymbols; i++)
  {
    BidsReal[i] = MarketInfo(REALSymbols[i], MODE_BID);
    AsksReal[i] = MarketInfo(REALSymbols[i], MODE_ASK);
  }
  
  return;
}

// ���������� �������� j-�� �������� ��������� i-�� ���������������� �������
string SymbolToStr( int i, int j )
{
  string Str = "", S1, S2;

  if (Math[i][j] == -1) // 
    Str = RealSymbols[AllSymbols[i][j]];
  else if (Math[i][j] == -2)
    Str = "1 / " + RealSymbols[AllSymbols[i][j]];
  else
  {
    S1 = RealSymbols[AllSymbols[i][j] / AmountRealSymbols];  // �������� �������� ������ ����
    S2 = RealSymbols[AllSymbols[i][j] % AmountRealSymbols];  // �������� �������� ������ ����
     
    switch (Math[i][j])
    {
      case 0: // 0 - "S1 / S2"
        Str = S1 + " / " + S2;
        break;
      case 1: // 1 - "S1 * S2"
        Str = S1 + " * " + S2;
        break;
      case 2: // 2 - "1 / (S1 * S2)";
        Str = "1 / (" + S1 + " * " + S2 + ")";
        break;
      case 3: // 3 - "S2 / S1"
        Str = S2 + " / " + S1;
        break;
     }
   }
 
  return(Str);
}  

void GetDataLot( string Symb )
{
  int Tmp = 1 / MarketInfo(Symb, MODE_LOTSTEP) + 0.1;
  
  Tmp /= 10;
  DigitsLot = 0;
  
  while (Tmp > 0)
  {
    DigitsLot++;
    Tmp /= 10;
  }
  
  if (MaxLot > MarketInfo(Symb, MODE_MAXLOT))
    MaxLot = MarketInfo(Symb, MODE_MAXLOT);

  if (MinLot < MarketInfo(Symb, MODE_MINLOT))
    MinLot = MarketInfo(Symb, MODE_MINLOT);
  
  if (MinLot > MarketInfo(Symb, MODE_LOTSTEP) + ALPHA)
    Alert(WindowExpertName(), " - WARNING: MinLot (", MinLot, ") > LotStep (", MarketInfo(Symb, MODE_MINLOT), ")");
    
  return;
}

void GetSymbolPrefix( string Symb )
{
  SymbolPrefix = StringSubstr(Symb, 6);
  
  return;
}

// ����������� ����������� �������� ��� ��������
void GetXTrade( string FileName )
{
  int i, j, k, m, Pos;
  int handle;
  int AmountStrings = 0;
  string Str1, Str2, Str3, Str[MAX_VARIANTPAIRS];
  
  handle = FileOpen(FileName, FILE_READ);
  
  while (!FileIsEnding(handle))
  {
    Str[AmountStrings] = FileReadString(handle);
    AmountStrings++;
  }
  
  FileClose(handle);
  
  for (i = 0; i < AmountAllSymbols; i++)
  {
    Pos = 0;
    
    for (j = 0; j < Count[i] - 1; j++)
    {
      Str1 = SymbolToStr(i, j);
      Pos += j + 1;
  
      for (k = j + 1; k < Count[i]; k++)
      {
        Str2 = Str1 + " && " + SymbolToStr(i, k);
        Str3 = SymbolToStr(i, k) + " && " + Str1;
        
        for (m = 0; m < AmountStrings; m++)
          if ((Str[m] == Str2) || (Str[m] == Str3))
            break;
        
        if (m == AmountStrings)
          XTrade[i][Pos] = FALSE;   
        else
          XTrade[i][Pos] = TRUE;
          
        XPosition[i][Pos] = 0;
        CountArbitrage[i][Pos] = 0;
        
        Pos++;
      }
    }
  }
  
  return;
}

void GetPipsD()
{
  GetRealBidAsk();
  
  for (int i = 0; i < AmountAllSymbols; i++)
  {
    GetBidAsk(i, 0);           
     
    // ���������� ������ (�������) ������
    if (Bids[i][0] > 10)
    {
      PointD[i] = 0.01;
      MinPipsD[i] = MinPips * 0.01;
    }
    else
    {
      PointD[i] = 0.0001;
      MinPipsD[i] = MinPips * 0.0001;
    }
  }
  
  return;
}

void InitArbitrage()
{
  GetDataLot(Symbol());
  GetSymbolPrefix(Symbol());
  
  AmountCurrency = StrToStringS(Currencies, ",", Currency);

  GetRealSymbols();
  GetAllSymbols();
  GetPipsD();
  
  MaxCountArbitrage = 0;

  for (int i = 0; i < AmountRealSymbols; i++)
    Position[i] = 0;
    
  GetXTrade("Trade-Arbitrage.txt");

  return;
}

void PrintXTrade()
{
  int i, j, k, Pos;
  
  for (i = 0; i < AmountAllSymbols; i++)
  {
    Pos = 0;
    
    for (j = 0; j < Count[i] - 1; j++)
    {
      Pos += j + 1;
  
      for (k = j + 1; k < Count[i]; k++)
      {
        if  (XTrade[i][Pos])
          Print(SymbolToStr(i, j) + " && " + SymbolToStr(i, k));

        Pos++;
      }
    }
  }
  
  return;
}

void PrintBeginInfo()
{
  int i, Max;
  
  Print("MAX_CURRENCY = " + AmountCurrency);
  Print("MAX_REALSYMBOLS = " + AmountRealSymbols);
  Print("MAX_ALLSYMBOLS = " + AmountAllSymbols);
   
  Max = 0;
  
  for (i = 0; i < AmountAllSymbols; i++)
    if (Count[i] > Max)
      Max = Count[i];

  Print("MAX_VARIANTSYMBOLS = " + Max);

  Max = 0;
  
  for (i = 0; i < AmountAllSymbols; i++)
    if (Count[i] * (Count[i] - 1) > Max)
      Max = Count[i] * (Count[i] - 1);
      
  Print("MAX_VARIANTPAIRS = " + Max);
  
  Max = 0;
  
  for (i = 0; i < AmountAllSymbols; i++)
    Max += Count[i];
      
  Print("SumAllCounts = " + Max);

  Max = 0;
  
  for (i = 0; i < AmountAllSymbols; i++)
    Max += Count[i] * (Count[i] - 1);

  Print("SumAllVariants = " + Max);

  PrintXTrade();
  
  return;
}

/************************************************* END BLOCK_1 *************************************************/
// ���� �������, �������������� � �������� ��� �������������:
// string StrDelSpaces( string Str );
// int StrToStringS( string Str, string Razdelitel, string &Output[] );
// bool RealSymbol( string Str );
// void GetRealSymbols();
// int GetNumRealSymbol( string Str );
// void GetAllSymbols();
// void GetRealBidAsk();
// string SymbolToStr( int i, int j );
// void GetDataLot( string Symb );
// void GetSymbolPrefix( string Symb );
// void GetXTrade( string FileName );
// void GetPipsD();
// void InitArbitrage();
// void PrintXTrade();
// void PrintBeginInfo();
/************************************************* END BLOCK_1 *************************************************/

//------------------------------------------------------------------------------------------------------------------------------

/************************************************* BEGIN BLOCK_2 *************************************************/
// ���� �������������� �������:
// string ArbitragePositions();
// string SymbolToFile( int i, int j );
// void StringToFile( string FileName, string Str );
// void MonitoringArbitrage( int NumSymbol, int Variant1, int Variant2 );
// void WriteStatistic( string FileName );
/************************************************* BEGIN BLOCK_2 *************************************************/
       
string ArbitragePositions()
{
  string Str = WindowExpertName() + ": MinPips = " + DoubleToStr(MinPips, 1);
  int i, j, k, Pos;  

  for (i = 0; i < AmountAllSymbols; i++)
  {
    Pos = 0;
    
    for (j = 0; j < Count[i] - 1; j++)
    {
      Pos += j + 1;
  
      for (k = j + 1; k < Count[i]; k++)
      {
        if  (XTrade[i][Pos])
        {
          if (XPosition[i][Pos] < -ALPHA)
            Str = Str + "\n" + SymbolToStr(i, j) + " (SELL) && " + SymbolToStr(i, k) + " (BUY)";
          else if (XPosition[i][Pos] > ALPHA)
            Str = Str + "\n" + SymbolToStr(i, j) + " (BUY) && " + SymbolToStr(i, k) + " (SELL)";
        }
        
        Pos++;
      }
    }
  }
  
  return(Str);
}

// ������� ������ ��� ������ � ���� j-�� �������� ��������� i-�� ���������������� �������
string SymbolToFile( int i, int j )
{
  string Str = "";
  int S1, S2;

  if (Math[i][j] < 0)
  {
    S1 = AllSymbols[i][j];

    Str = RealSymbols[S1] + ": " + DoubleToStr(BidsReal[S1], MarketInfo(REALSymbols[S1], MODE_DIGITS)) + " " +
                                   DoubleToStr(AsksReal[S1], MarketInfo(REALSymbols[S1], MODE_DIGITS));
  } 
  else
  {
    S1 = AllSymbols[i][j] / AmountRealSymbols;
    S2 = AllSymbols[i][j] % AmountRealSymbols;
    Str = RealSymbols[S1] + ": " + DoubleToStr(BidsReal[S1], MarketInfo(REALSymbols[S1], MODE_DIGITS)) + " " +
                                   DoubleToStr(AsksReal[S1], MarketInfo(REALSymbols[S1], MODE_DIGITS)) + "\n" +
          RealSymbols[S2] + ": " + DoubleToStr(BidsReal[S2], MarketInfo(REALSymbols[S2], MODE_DIGITS)) + " " +
                                   DoubleToStr(AsksReal[S2], MarketInfo(REALSymbols[S2], MODE_DIGITS));
  }

  return(Str);
}

void StringToFile( string FileName, string Str )
{
  int handle;
  
  handle = FileOpen(FileName, FILE_READ|FILE_WRITE, "\t");
  FileSeek(handle, 0, SEEK_END);

  FileWrite(handle, Str);

  FileClose(handle);

  return;
}

void MonitoringArbitrage( int NumSymbol, int Variant1, int Variant2 )
{
  int V;
  string Str;

  if (Variant1 < Variant2)
    V = Variant1 * Count[NumSymbol] + Variant2;
  else
    V = Variant2 * Count[NumSymbol] + Variant1;

  CountArbitrage[NumSymbol][V]++;
  
  if (CountArbitrage[NumSymbol][V] > MaxCountArbitrage)
    MaxCountArbitrage = CountArbitrage[NumSymbol][V];
  
  if (Monitoring)
  {
    Str = "Time = " + TimeToStr(CurrentTime, TIME_DATE|TIME_SECONDS) + "\n";
    Str = Str + "Bid \"" + SymbolToStr(NumSymbol, Variant1) + "\" (" + DoubleToStr(Bids[NumSymbol][Variant1], 5) +
          ") > (" + DoubleToStr(Asks[NumSymbol][Variant2], 5) + ") Ask \"" + SymbolToStr(NumSymbol, Variant2) +
          "\", Difference = " + DoubleToStr((Bids[NumSymbol][Variant1] - Asks[NumSymbol][Variant2]) / PointD[NumSymbol], 1) + " pips";
    Str = Str + "\n" + SymbolToFile(NumSymbol, Variant1) + "\n" + SymbolToFile(NumSymbol, Variant2) + "\n";
    Str = Str + "Count = " + CountArbitrage[NumSymbol][V];

    StringToFile("Arbitrage.txt", Str); 
  }

  return;
}

void WriteStatistic( string FileName )
{
  string Str = WindowExpertName() + ": MinPips = " + DoubleToStr(MinPips, 1);
  int i, j, k, Pos;
  int V1, V2;
  int handle, Cnt;
  int BenchTime = GetTickCount();
  
  Cnt = MaxCountArbitrage;
  
  while (Cnt >= 2) // ��� �������� � �������� �����, ��� �������, 2 ����
  {
    for (i = 0; i < AmountAllSymbols; i++)
    {
      Pos = 0;
    
      for (j = 0; j < Count[i] - 1; j++)
      {
        Pos += j + 1;
  
        for (k = j + 1; k < Count[i]; k++)
        {
          if  (CountArbitrage[i][Pos] == Cnt)
            Str = Str + "\n" + CountArbitrage[i][Pos] + ": " + SymbolToStr(i, j) + " && " + SymbolToStr(i, k);
            
          Pos++;
        }
      }
    }
    
    Cnt--;
  }
    
  handle = FileOpen(FileName, FILE_WRITE, "\t");
  FileWrite(handle, Str);
  FileClose(handle);
  
  Print("MaxCountArbitrage = " + MaxCountArbitrage + ", Write Time Statistic = " +
        DoubleToStr((GetTickCount() - BenchTime) / 1000, 0) + " s.");
  
  return;
}

/************************************************* END BLOCK_2 *************************************************/
// ���� �������������� �������:
// string ArbitragePositions();
// string SymbolToFile( int i, int j );
// void StringToFile( string FileName, string Str );
// void MonitoringArbitrage( int NumSymbol, int Variant1, int Variant2 );
// void WriteStatistic( string FileName );
/************************************************* END BLOCK_2 *************************************************/

//------------------------------------------------------------------------------------------------------------------------------

/************************************************* BEGIN BLOCK_3 *************************************************/
// ���� �������� �������� �������:
// int OrderSlipPage( double OriginalPrice );
// double GetTradeVolume( int PrevTicket );
// int _OrderSend( string _symbol, int _cmd, double _volume, double _price, int _slippage, double _stoploss, double _takeprofit);
// double _OrderClose( int _ticket, double _lots, double _price, int _slippage);
/************************************************* BEGIN BLOCK_3 *************************************************/

int OrderSlipPage( double OriginalPrice )
{
  double Tmp;
  int Res;
  
  if (OrderCloseTime() == 0)
  {
    if (OrderType() == OP_BUY)
      Tmp = OriginalPrice - OrderOpenPrice();
    else if (OrderType() == OP_SELL)
      Tmp = OrderOpenPrice() - OriginalPrice;
  }
  else
  {
    if (OrderType() == OP_BUY)
      Tmp = OrderClosePrice() - OriginalPrice;
    else if (OrderType() == OP_SELL)
      Tmp = OriginalPrice - OrderClosePrice();
  }
  
  if (Tmp > 0)
    Res = Tmp / MarketInfo(OrderSymbol(), MODE_POINT) + 0.1;
  else
    Res = Tmp / MarketInfo(OrderSymbol(), MODE_POINT) - 0.1;
  
  return(Res);
}

double GetTradeVolume( int PrevTicket )
{
  if (OrderTicket() == PrevTicket) // ��� ����� �������
    return(0);
    
  return(OrderLots());
}

// ������������ �������� Partial Fills. ��� ����������� ��������� �������� �������� �������� ����� �������� �����������
int _OrderSend( string _symbol, int _cmd, double _volume, double _price, int _slippage, double _stoploss, double _takeprofit)
{
  static string OrderTypeToString[7] = {"OP_BUY", "OP_SELL", "OP_BUYLIMIT", "OP_SELLLIMIT", "OP_BUYSTOP", "OP_SELLSTOP", "Balance"};
  int PrevTicket;
  int Ticket = -1;
  int _GetLastError;
  int SP = 0;
  double Vol = 0;
      
  OrderSelect(OrdersTotal() - 1, SELECT_BY_POS, MODE_TRADES);
  PrevTicket = OrderTicket();
  
  OrderSend(_symbol, _cmd, _volume, _price, _slippage, _stoploss, _takeprofit); 
  _GetLastError = GetLastError();
  OrderSelect(OrdersTotal() - 1, SELECT_BY_POS, MODE_TRADES);
                     
  if (GetTradeVolume(PrevTicket) > ALPHA)
    if (OrderSymbol() == _symbol) // ���������� ��-�� ��������� � ��������� ����� �������
    {
      SP = OrderSlipPage(_price);
      Ticket = OrderTicket();
      Vol = OrderLots();
    }
    
  Print(Ticket, " = OrderSend(", _symbol, ", ", OrderTypeToString[_cmd], ", ", _volume, ", ", DoubleToStr(_price, MarketInfo(_symbol, MODE_DIGITS)),
        ", ", _slippage, ", ", _stoploss, ", ", _takeprofit, ") - ", ErrorDescription(_GetLastError), ", SlipPage = ", SP, ", Lots = ", Vol);
        
  return(Ticket);
}

// ������������ �������� Partial Fills. ��� ����������� ��������� �������� �������� �������� ����� �������� �����������
double _OrderClose( int _ticket, double _lots, double _price, int _slippage)
{
  int Ticket = -1;
  int PrevTicket;
  int _GetLastError;
  int SP = 0;
  double Vol = 0;
  string _symbol;
  double PrevLots;
  
  OrderSelect(_ticket, SELECT_BY_TICKET);
  _symbol = OrderSymbol();
  PrevLots = OrderLots();

  OrderSelect(OrdersHistoryTotal() - 1, SELECT_BY_POS, MODE_HISTORY);
  PrevTicket = OrderTicket();
  OrderClose(_ticket, _lots, _price, _slippage);
  _GetLastError = GetLastError();
//  Sleep(1000) // ��� MBTrading ������ ����� ���������� � �� ������ �� 100% �������� ����������� ��������� ��������
  OrderSelect(OrdersHistoryTotal() - 1, SELECT_BY_POS, MODE_HISTORY);
  
  if (GetTradeVolume(PrevTicket) > ALPHA)
    if (OrderSymbol() == _symbol) // ���������� ��-�� ��������� � ��������� ����� �������
    {
      SP = OrderSlipPage(_price);
      Ticket = OrderTicket();
      Vol = OrderLots();
    }
  
  Print(Ticket, "(", _symbol, ") = OrderClose(", _ticket, ", ", _lots, "(", PrevLots, "), ", DoubleToStr(_price, MarketInfo(_symbol, MODE_DIGITS)),
        ", ", _slippage, ") - ", ErrorDescription(_GetLastError), ", SlipPage = ", SP, ", Lots = ", Vol);
  
  return(Vol);
}

/************************************************* END BLOCK_3 *************************************************/
// ���� �������� �������� �������:
// int OrderSlipPage( double OriginalPrice );
// double GetTradeVolume( int PrevTicket );
// int _OrderSend( string _symbol, int _cmd, double _volume, double _price, int _slippage, double _stoploss, double _takeprofit);
// double _OrderClose( int _ticket, double _lots, double _price, int _slippage);
/************************************************* END BLOCK_3 *************************************************/

//------------------------------------------------------------------------------------------------------------------------------

/************************************************* BEGIN BLOCK_4 *************************************************/
// ���� ��������������� �������� �������:
// int GetOrderTicket( string Symb, int Type, bool FlagMax );
// bool MyOrderSend( string Symb, int Type, double& Vol, int SlipPage, double MaxLot, bool Lock );
// void CloseLock();
// void RefreshPositions() 
/************************************************* BEGIN BLOCK_4 *************************************************/

int GetOrderTicket( string Symb, int Type, bool FlagMax )
{
  int Ticket, TicketMax = -1, TicketMin = -1;
  double Max = 0;
  double Min = 9999;
  int Pos = OrdersTotal() - 1;
  
  while (Pos >= 0)
  {
    OrderSelect(Pos, SELECT_BY_POS);

    if (OrderSymbol() == Symb)
       if (OrderType() == Type)
       {
         if (OrderLots() > Max) // ������� ���������� ����� ��� ����������� ���������� �������� ��������
         {
           Max = OrderLots();
           TicketMax = OrderTicket();
         }

         if (OrderLots() < Min)  // ������� ���������� ����� ��� �������� CloseBy (�������� MinLot)
         {
           Min = OrderLots();
           TicketMin = OrderTicket();
         }
       }

    Pos--;
  }
  
  if (FlagMax)
    Ticket = TicketMax;
  else
    Ticket = TicketMin;
  
  if (Ticket > 0)
    OrderSelect(Ticket, SELECT_BY_TICKET);
  
  return(Ticket);
}

// ��������� ����� � Partial Fills
bool MyOrderSend( string Symb, int Type, double& Vol, int SlipPage, double MaxLot, bool Lock )
{
  int TypeReverse;
  double Price;
  double VolTmp;
  
  if (Type == OP_BUY)
  {
    Price = MarketInfo(Symb, MODE_ASK);
//    Price = AsksReal[GetNumRealSymbol(Symb)]; // ����� ���� �������� � ����� ��-�� ����������� MT4
    TypeReverse = OP_SELL;
  }
  else // (Type == OP_SELL)
  {
    Price = MarketInfo(Symb, MODE_BID);
//    Price = BidsReal[GetNumRealSymbol(Symb)]; // ����� ���� �������� � ����� ��-�� ����������� MT4
    TypeReverse = OP_BUY;
  }
    
  if (!Lock) // �������� ��� ���� (�������� ��� ��������� ��-�� �������� ���������� "������" �������� ��������)
    while ((Vol > MinLot - ALPHA) && (GetOrderTicket(Symb, TypeReverse, TRUE) > 0)) // Vol >= MinLot
    {
      if (Vol > OrderLots() + MinLot - ALPHA) // Vol >= OrderLots() + MinLot
        VolTmp = OrderLots();
      else if (Vol < OrderLots() - MinLot + ALPHA) // Vol <= OrderLots() - MinLot
        VolTmp = Vol;
      else if (Vol > OrderLots() + ALPHA)// Vol > OrderLots()
        VolTmp = OrderLots() - MinLot;
      else if (Vol < OrderLots() - ALPHA) // Vol < OrderLots()
        VolTmp = MathMin(OrderLots() - MinLot, Vol - MinLot);
      else // Vol == OrderLots()
        VolTmp = OrderLots();

      if (VolTmp < MinLot - ALPHA) // VolTmp < MinLot
      {
        Alert("Cannot close Order ", OrderSymbol(), " ", OrderTicket(), " ", OrderLots(), " by ", Vol, " lots! Lock is needed!");
        
        break;
      }
      
      // � ����� ������������ ����������� ����� ���������� � ����������� ��������� ������� �������
      VolTmp = _OrderClose(OrderTicket(), NormalizeDouble(VolTmp, DigitsLot), Price, SlipPage);
      
      if (VolTmp < ALPHA)
        return(FALSE);
        
      Vol -= VolTmp;
    }
  
  while (Vol - MaxLot > ALPHA) // Vol > MaxLot
  {
    if (_OrderSend(Symb, Type, NormalizeDouble(MathMin(MaxLot, Vol - MinLot), DigitsLot), Price, SlipPage, 0, 0) < 0)
      return(FALSE);
          
    Vol -= OrderLots();
  }
 
  while (Vol > MinLot - ALPHA) // Vol >= MinLot
  {
    if (_OrderSend(Symb, Type, NormalizeDouble(Vol, DigitsLot), Price, SlipPage, 0, 0) < 0)
      return(FALSE);
    
    Vol -= OrderLots();
  }
  
  return(TRUE);
}

// �������� ������������ �������
void CloseLock()
{
  int BuyTicket, SellTicket;
  double SellLots, Tmp;
  string Symb;
  bool FlagMax = TRUE;
  bool FlagRepeat = FALSE;

  for (int i = 0; i < AmountRealSymbols; i++)
  {
    Symb = REALSymbols[i];
    
    BuyTicket = GetOrderTicket(Symb, OP_BUY, FlagMax);
    SellTicket = GetOrderTicket(Symb, OP_SELL, !FlagMax);
  
    while ((BuyTicket != -1) && (SellTicket != -1))
    {
      SellLots = OrderLots();
      
      OrderSelect(BuyTicket, SELECT_BY_TICKET);
      
      Tmp = MathAbs(OrderLots() - SellLots);
      
      if ((ALPHA < Tmp) && (Tmp < MinLot - ALPHA))
      {
        if (FlagRepeat)
          break;
          
        FlagRepeat = TRUE;
        FlagMax = !FlagMax;
      }
      else
      {
        if (!OrderCloseBy(BuyTicket, SellTicket))
          return;
        
        FlagRepeat = FALSE;
      }

      BuyTicket = GetOrderTicket(Symb, OP_BUY, FlagMax);
      SellTicket = GetOrderTicket(Symb, OP_SELL, !FlagMax);
    }
  }
  
  return;
}

// ��������� ����������� ������� - Position[]
void RefreshPositions() 
{
  bool Flag = FALSE;  // ���� �� ������� ���������
  double Vol;
  
  for (int i = 0; i < AmountRealSymbols; i++)
  {
    Position[i] = NormalizeDouble(Position[i], DigitsLot); // �� ����� ������ (�� �������� ����������) �������
    
    if (Position[i] > ALPHA) // �������. �������� �������� ��-�� ������������������� Position[i]
    {
      Vol = Position[i];
       
      if (!MyOrderSend(REALSymbols[i], OP_BUY, Vol, SlipPage, MaxLot, Lock))
        Print("Vol = ", Vol);
      
      Position[i] = Vol; // �� ��������� ����� ��-�� �������� ������
      Flag = TRUE;
    }
    else if (Position[i] < -ALPHA) // �������
    {
      Vol = -Position[i];

      if (!MyOrderSend(REALSymbols[i], OP_SELL, Vol, SlipPage, MaxLot, Lock))
        Print("Vol = ", Vol);
       
      Position[i] = -Vol; // �� ��������� ����� ��-�� �������� ������
      Flag = TRUE;
    }
  }
    
  if (Flag) // �������� ���������
    Comment(ArbitragePositions());
    
  CloseLock(); // ��� Lock = FALSE ��-�� ������� MinLot ����� ���� ������������ �������
  
  return;
}

/************************************************* END BLOCK_4 *************************************************/
// ���� ��������������� �������� �������:
// int GetOrderTicket( string Symb, int Type, bool FlagMax );
// bool MyOrderSend( string Symb, int Type, double& Vol, int SlipPage, double MaxLot, bool Lock );
// void CloseLock();
// void RefreshPositions() 
/************************************************* END BLOCK_4 *************************************************/

//------------------------------------------------------------------------------------------------------------------------------

/************************************************* BEGIN BLOCK_5 *************************************************/
// ���� ������� ���������:
// void SymbolDone( double Vol, int Symb );
// void OpenSymbolPosition( int NumSymbol, int Variant, int Type, double Vol );
// void OpenArbitragePosition( int NumSymbol, int Variant1, int Variant2, double Vol );
// void GetBidAsk( int i, int j );
// void TradeArbitrage();
/************************************************* BEGIN BLOCK_6 *************************************************/

void SymbolDone( double Vol, int Symb )
{
  if (Vol == 0)
    return;

  Position[Symb] += Vol;
  
  if (Vol > 0)
    StrOut = StrOut + "; BUY " + RealSymbols[Symb] + "(" + DoubleToStr(Vol, DigitsLot) + ") = " +
                      DoubleToStr(AsksReal[Symb], MarketInfo(REALSymbols[Symb], MODE_DIGITS)) + " Ask";
  else // Vol < 0
    StrOut = StrOut + "; SELL " + RealSymbols[Symb] + "(" + DoubleToStr(Vol, DigitsLot) + ") = " +
                      DoubleToStr(BidsReal[Symb], MarketInfo(REALSymbols[Symb], MODE_DIGITS)) + " Bid";
          
  return;
}

// �������� Type-���� ������� �� ���������������� ������� AllSymbols[NumSymbol][Variant]
void OpenSymbolPosition( int NumSymbol, int Variant, int Type, double Vol )
{
  int S1, S2;
  double Tmp = 0, Tmp1 = 0, Tmp2 = 0;
  int Symb = AllSymbols[NumSymbol][Variant];
  int Mth = Math[NumSymbol][Variant];
 
  if (Type == OP_SELL)
  {
    if (Mth == -2) // -2 - "1 / S"
      Tmp = Vol / AsksReal[Symb];
    else if (Mth == -1)  //  -1 - "S"
      Tmp = -Vol;
    else
    {
      S1 = Symb / AmountRealSymbols;
      S2 = Symb % AmountRealSymbols;

      switch (Mth)
      {
        case 0: // 0 - "S1 / S2"
          Tmp1 = -Vol;
          Tmp2 = Vol * Bids[NumSymbol][Variant];
          break;
        case 1: // 1 - "S1 * S2"
          Tmp1 = -Vol;
          Tmp2 = -Vol * BidsReal[S1];
          break;
        case 2: // 2 - "1 / (S1 * S2)";
          Tmp1 = Vol / AsksReal[S1];
          Tmp2 = Vol * Bids[NumSymbol][Variant];
          break;
        case 3: // 3 - "S2 / S1"
          Tmp1 = Vol / AsksReal[S1];
          Tmp2 = -Tmp1;
          break;
      }
    }   
  }
  else // (Type == OP_BUY)
  {
    if (Mth == -2) // -2 - "1 / S"
      Tmp = -Vol / BidsReal[Symb];
    else if (Mth == -1)  //  -1 - "S"
      Tmp = Vol;
    else
    {
      S1 = Symb / AmountRealSymbols;
      S2 = Symb % AmountRealSymbols;

      switch (Mth)
      {
        case 0: // 0 - "S1 / S2"
          Tmp1 = Vol;
          Tmp2 = -Vol * Asks[NumSymbol][Variant];
          break;
        case 1: // 1 - "S1 * S2"
          Tmp1 = Vol;
          Tmp2 = Vol * AsksReal[S1];
          break;
        case 2: // 2 - "1 / (S1 * S2)";
          Tmp1 = -Vol / BidsReal[S1];
          Tmp2 = -Vol * Asks[NumSymbol][Variant];
          break;
        case 3: // 3 - "S2 / S1"
          Tmp1 = -Vol / BidsReal[S1];
          Tmp2 = -Tmp1;
          break;
      }
    }   
  }
  
  SymbolDone(Tmp, Symb);
  SymbolDone(Tmp1, S1);
  SymbolDone(Tmp2, S2);

  return;    
}

// Bids[NumSymbol][Variant1] > Asks[NumSymbol][Variant2]
void OpenArbitragePosition( int NumSymbol, int Variant1, int Variant2, double Vol )
{
  int V;
  double XPos;
  int j, k;
  
  // ������ �� ������, ���� �� �������� ��������� ��� ������� �������
  if (Variant1 < Variant2)
  {
    V = Variant1 * Count[NumSymbol] + Variant2;  // ����� �������� ������ �����
    XPos = XPosition[NumSymbol][V];
    
    if (XPos < -ALPHA)
      return;

    XPos += Vol; // ���� ������ ���������� ������
    XPosition[NumSymbol][V] = -Vol;
  }    
  else
  {
    V = Variant2 * Count[NumSymbol] + Variant1;
    XPos = XPosition[NumSymbol][V];
    
    if (XPos > ALPHA)
      return;
      
    XPos = Vol - XPos;  // ���� ������ ���������� ������
    XPosition[NumSymbol][V] = Vol;
  }
  
  MonitoringArbitrage(NumSymbol, Variant1, Variant2);

  if (XTrade[NumSymbol][V]) // ������� ������ �������� ����������
  {
    OpenSymbolPosition(NumSymbol, Variant1, OP_SELL, XPos);  // ������� ������ �������
    OpenSymbolPosition(NumSymbol, Variant2, OP_BUY, XPos);   // ������ ������ �������

    Print("Variant1 = " + SymbolToStr(NumSymbol, Variant1) + " (Bid = " + DoubleToStr(Bids[NumSymbol][Variant1], 6) + 
          "), Variant2 = " + SymbolToStr(NumSymbol, Variant2) + " (Ask = " + DoubleToStr(Asks[NumSymbol][Variant2], 6) +
          "), Difference = " + DoubleToStr((Bids[NumSymbol][Variant1] - Asks[NumSymbol][Variant2]) / PointD[NumSymbol], 1) + " pips");
    Print(StrOut);
 
    StrOut = "";
  }

  return;
}

// ��������� � Bids[i][j] � Asks[i][j] ���� j-�� �������� ��������� i-�� ���������������� �������
void GetBidAsk( int i, int j )
{
  double Bid1, Bid2;
  double Ask1, Ask2;
  int Mth, Symb;
  int S1, S2;

  Symb = AllSymbols[i][j];
  Mth = Math[i][j];
  
  if (Mth == -2) // -2 - "1 / S"
  {
    Bids[i][j] = 1 / AsksReal[Symb];
    Asks[i][j] = 1 / BidsReal[Symb];
  }
  else if (Mth == -1)  //  -1 - "S"
  {
    Bids[i][j] = BidsReal[Symb];
    Asks[i][j] = AsksReal[Symb];
  }
  else
  {
    S1 = Symb / AmountRealSymbols;
    S2 = Symb % AmountRealSymbols;

    Bid1 = BidsReal[S1];
    Bid2 = BidsReal[S2];
    Ask1 = AsksReal[S1];
    Ask2 = AsksReal[S2];
     
    switch (Mth)
    {
      case 0: // 0 - "S1 / S2"
        Bids[i][j] = Bid1 / Ask2;
        Asks[i][j] = Ask1 / Bid2;
        break;
      case 1: // 1 - "S1 * S2"
        Bids[i][j] = Bid1 * Bid2;
        Asks[i][j] = Ask1 * Ask2;
        break;
      case 2: // 2 - "1 / (S1 * S2)";
        Bids[i][j] = 1 / (Ask1 * Ask2);
        Asks[i][j] = 1 / (Bid1 * Bid2);
        break;
      case 3: // 3 - "S2 / S1"
        Bids[i][j] = Bid2 / Ask1;
        Asks[i][j] = Ask2 / Bid1;
    }
  }

  return;
}

void TradeArbitrage()
{
  int i, j, k;
  double Bid1, Bid2, Ask1, Ask2;

  GetRealBidAsk();
 
  for (i = 0; i < AmountAllSymbols; i++)
  {
    for (j = 0; j < Count[i]; j++)
      GetBidAsk(i, j);           
 
    for (j = 0; j < Count[i] - 1; j++)
    {
      Bid1 = Bids[i][j] - MinPipsD[i];
      Ask1 = Asks[i][j] + MinPipsD[i];
  
      for (k = j + 1; k < Count[i]; k++)
      {
        Bid2 = Bids[i][k];
        Ask2 = Asks[i][k];
                       
        if (Bid1 > Ask2)
          OpenArbitragePosition(i, j, k, Lots);
        else if (Ask1 < Bid2)
          OpenArbitragePosition(i, k, j, Lots);
      }
    }
  }

  RefreshPositions();

  return;
}

/************************************************* END BLOCK_5 *************************************************/
// ���� ������� ���������:
// void SymbolDone( double Vol, int Symb );
// void OpenSymbolPosition( int NumSymbol, int Variant, int Type, double Vol );
// void OpenArbitragePosition( int NumSymbol, int Variant1, int Variant2, double Vol );
// void GetBidAsk( int i, int j );
// void TradeArbitrage();
/************************************************* END BLOCK_5 *************************************************/

//------------------------------------------------------------------------------------------------------------------------------

void init()
{
  Comment(WindowExpertName() + ": MinPips = " + DoubleToStr(MinPips, 1)) ;
  
  InitArbitrage();
  PrintBeginInfo();

  TimeToWrite *= 60;
  PrevTime = TimeCurrent();

  return;
}

void deinit()
{
  Comment("");
   
  return;
}
 
void start()
{
  while(!IsStopped())
  {
    RefreshRates();
    CurrentTime = TimeCurrent();
    
    TradeArbitrage();

    if (CurrentTime - PrevTime > TimeToWrite)
    {
      PrevTime = CurrentTime;
      
      WriteStatistic("ArbitrageStatistic.txt");
    }
    
    Sleep(PAUSE);
  }

  return;
}