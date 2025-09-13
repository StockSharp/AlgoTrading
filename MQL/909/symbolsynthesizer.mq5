//+------------------------------------------------------------------+
//|                                            SymbolSynthesizer.mq5 |
//|                                          Copyright 2012, alohafx |
//|                                   http://alohafx.blog36.fc2.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, alohafx"
#property link      "http://alohafx.blog36.fc2.com/"
#property version   "1.00"

//+---description----------------------------------------------------+
//| EA to show order panel to make hedge position.                   |
//+------------------------------------------------------------------+
string Sym[13][4];
//---
#include <SymbolSynthesizerDialog.mqh>
//+------------------------------------------------------------------+
//| Global Variables                                                 |
//+------------------------------------------------------------------+
CSymbolSynthesizerDialog ExtDialog;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   Sym[0][0] ="EURUSD"; Sym[0][1] ="EURGBP"; Sym[0][2] ="GBPUSD"; Sym[0][3] ="L";
   Sym[1][0] ="GBPUSD"; Sym[1][1] ="EURGBP"; Sym[1][2] ="EURUSD"; Sym[1][3] ="S";
   Sym[2][0] ="USDCHF"; Sym[2][1] ="EURUSD"; Sym[2][2] ="EURCHF"; Sym[2][3] ="S";
   Sym[3][0] ="USDJPY"; Sym[3][1] ="EURUSD"; Sym[3][2] ="EURJPY"; Sym[3][3] ="S";
   Sym[4][0] ="USDCAD"; Sym[4][1] ="EURUSD"; Sym[4][2] ="EURCAD"; Sym[4][3] ="S";
   Sym[5][0] ="AUDUSD"; Sym[5][1] ="EURAUD"; Sym[5][2] ="EURUSD"; Sym[5][3] ="S";
   Sym[6][0] ="EURGBP"; Sym[6][1] ="GBPUSD"; Sym[6][2] ="EURUSD"; Sym[6][3] ="S";
   Sym[7][0] ="EURAUD"; Sym[7][1] ="AUDUSD"; Sym[7][2] ="EURUSD"; Sym[7][3] ="S";
   Sym[8][0] ="EURCHF"; Sym[8][1] ="EURUSD"; Sym[8][2] ="USDCHF"; Sym[8][3] ="L";
   Sym[9][0] ="EURJPY"; Sym[9][1] ="EURUSD"; Sym[9][2] ="USDJPY"; Sym[9][3] ="L";
   Sym[10][0]="GBPJPY"; Sym[10][1]="GBPUSD"; Sym[10][2]="USDJPY"; Sym[10][3]="L";
   Sym[11][0]="AUDJPY"; Sym[11][1]="AUDUSD"; Sym[11][2]="USDJPY"; Sym[11][3]="L";
   Sym[12][0]="GBPCHF"; Sym[12][1]="GBPUSD"; Sym[12][2]="USDCHF"; Sym[12][3]="L";
//---
   if(!ExtDialog.Create(0,"SymbolSynthesizer",0,20,20,315,255))
     {
      Print("Create dialog Failure!");
      return(-1);
     }
//--- run application
   ExtDialog.Run();
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy dialog
   ExtDialog.Destroy();
  }
//+------------------------------------------------------------------+
//| Expert chart event function                                      |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event ID  
                  const long& lparam,   // event parameter of the long type
                  const double& dparam, // event parameter of the double type
                  const string& sparam) // event parameter of the string type
  {
   ExtDialog.ChartEvent(id,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+
//| "Tick" event handler function                                    |
//+------------------------------------------------------------------+
void OnTick(void)
  {
   ExtDialog.TickChange();
  }
//+------------------------------------------------------------------+
