//+------------------------------------------------------------------+
//|                                            Quantum_v2_Expert.mq4 |
//|                                        Copyright 2016, Scriptong |
//|                                          http://advancetools.net |
//+------------------------------------------------------------------+
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property version   "2.0"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_YESNO
  {
   NO,                                                                                             // ���
   YES                                                                                             // ��
  };

#include <Quantum\Quantum_CalculateTradeType_v2.mqh>

input string               i_string1               = "Orders / ������";                            // =================================================
input double               i_staticLots            = 0.1;                                          // Static volume / ������������� ����� 
input double               i_dynamicLots           = 10.0;                                         // Dynamic volume, % / ������������ �����, % 
input uint                 i_tpSize                = 0;                                            // Size of TP, pts / ������ TP, ��.
input uint                 i_slShift               = 40;                                           // Offset from ext., pts. / ������ �� ����������, ��.

input string               i_string2               = "��������� Stochastic � Quantum";             // ======================
input uint                 i_periodK               = 100;                                          // %K period / ������ %K
input uint                 i_periodD               = 100;                                          // %D period / ������ %D
input uint                 i_slowing               = 3;                                            // Slowing / ������������
input double               i_highLevel             = 80.0;                                         // Bottom of overbought zone / ��� ���� ���������������
input double               i_lowLevel              = 20.0;                                         // Top of overselling zone / ���� ���� ���������������
input double               i_highCloseLevel        = 90.0;                                         // Level for Buy close / ������� ��� �������� Buy
input double               i_lowCloseLevel         = 10.0;                                         // Level for Sell close / ������� ��� �������� Sell
input uint                 i_extremumRank          = 300;                                          // Rank of extremum / ���� ����������

input string               i_string3               = "������ ���������";                           // ======================
input ENUM_YESNO           i_is5Digits           = YES;                                            // Use 5-digits / ������������ 5-�������� ���������
input ENUM_YESNO           i_isECN               = NO;                                             // Use ECN / ������������ ECN
input int                  i_slippage            = 3;                                              // Slippage / ���������� �� ����������� ���
input string               i_openOrderSound      = "ok.wav";                                       // Sound for open order / ���� ��� �������� ������
input int                  i_magicNumber         = 2353;                                           // ID of expert orders / ID ������� ��������

TradeParam           g_tradeParam;                                                                 // ��������� ��� �������� �������� ��������� �������
GetSymbolInfo       *g_symbolInfo;                                                                 // ����� ����� ������� �������� ����������
CTrade              *g_trade;                                                                      // ����� ���������� �������� ��������
CalculateTradeType  *g_calcTradeType;                                                              // ����� ������� ���������� ��������� �������
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� ������������� ��������                                                                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
int OnInit()
  {
// ������������� �������� ����� ���������
   if(!IsTunningParametersCorrect())
      return INIT_FAILED;

   if(!InitializeClasses()) // ������������� ������� GetSymbolInfo, Trade � CalculateTradeType
      return INIT_SUCCEEDED;

   return INIT_SUCCEEDED;
  }
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� ������������ �������� ����������� ����������                                                                                                                                             |
//+---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool IsTunningParametersCorrect()
  {
   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   if(i_dynamicLots<0.01 || i_dynamicLots>99.99)
     {
      Alert(name,(isRussianLang)? ": �������� ������������� ������ �������������� � ���������. ���������� �������� �� 0.01 �� 99.99. ������� ��������." :
            ": value of dynamic volume must be greater than 0.00 and less than 100.00. Expert is turned off.");
      return false;
     }

   if(i_periodK<1)
     {
      Alert(name,(isRussianLang)? ": ������ %� ���������� ������ ���� ����� 0. ������� ��������." :
            ": %K period of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_periodD<1)
     {
      Alert(name,(isRussianLang)? ": ������ %D ���������� ������ ���� ����� 0. ������� ��������." :
            ": %D period of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_slowing<1)
     {
      Alert(name,(isRussianLang)? ": ������������ ���������� ������ ���� ����� 0. ������� ��������." :
            ": slowing of stochastic must be greater than zero. Expert is turned off.");
      return false;
     }

   if(i_extremumRank<1)
     {
      Alert(name,(isRussianLang)? ": ���� ���������� ������ ���� ����� 0. ������� ��������." :
            ": rank of extremum must be greater than zero. Expert is turned off.");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� ��������������� ��������                                                                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(g_symbolInfo)
     {
      delete g_symbolInfo;
      g_symbolInfo=NULL;
     }

   if(g_trade)
     {
      delete g_trade;
      g_trade=NULL;
     }

   if(g_calcTradeType)
     {
      delete g_calcTradeType;
      g_calcTradeType=NULL;
     }
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������������� ���� ����������� �������                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool InitializeClasses()
  {
   uint pointMul=1;
   if(i_is5Digits==YES)
      pointMul=10;

   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   g_symbolInfo=new GetSymbolInfo(Symbol(),i_slippage*pointMul,i_isECN==YES);
   if(g_symbolInfo==NULL)
     {
      Alert(name,isRussianLang? ": ���������� ���������������� ����� GetSymbolInfo. ������� ��������!" :
            ": could not to initialize the class GetSymbolInfo. Expert is turned off!");
      return false;
     }

   if(g_symbolInfo.GetPoint()==0)
     {
      Alert(name,isRussianLang? ": ��������� ������ - ������ ������ ����� ����. ������� ��������!" :
            ": fatal error - the size of one point is equal to zero. Expert is turned off!");
      return false;
     }

   if(g_symbolInfo.GetTickValue()==0)
     {
      Alert(name,isRussianLang? ": ��������� ������ - ��������� ���� ����� ����. ������� ��������!" :
            ": fatal error - the cost of one tick is equal to zero. Expert is turned off!");
      return false;
     }

   g_trade=new CTrade(i_openOrderSound);
   if(g_trade==NULL)
     {
      Alert(name,isRussianLang? ": ���������� ���������������� ����� CTrade. ������� ��������!" :
            ": could not to initialize the class CTrade. Expert is turned off!");
      return false;
     }

   g_calcTradeType=new CalculateTradeType(i_staticLots,i_dynamicLots,i_tpSize*pointMul,i_slShift*pointMul,i_periodK,i_periodD,i_slowing,i_highLevel,i_lowLevel,
                                          i_highCloseLevel,i_lowCloseLevel,i_extremumRank,i_magicNumber);
   if(g_calcTradeType==NULL)
     {
      Alert(name,isRussianLang? ": ���������� ���������������� ����� CalculateTradeType. ������� ��������!" :
            ": could not to initialize the class CalculateTradeType. Expert is turned off!");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� start ��������                                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnTick()
  {
   if(Bars<int(i_extremumRank))
      return;

// ���������� �������� ��������
   while(!IsStopped())
     {
      // �������� �� ���������� ��������� ���������
      g_symbolInfo.RefreshInfo();

      if(g_symbolInfo.GetTickValue()==0 || g_symbolInfo.GetPoint()==0 || g_symbolInfo.GetTickSize()==0)
        {
         bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");
         Alert(WindowExpertName(),isRussianLang? ": ��������� ������ ��������� - �������� ������ ��� ���� ����� ����. ������� ��������." :
               ": fatal error - the size of one point or of one tick is equal to zero. Expert is turned off!");
         ExpertRemove();
         return;
        }

      ENUM_TRADE_TYPE tradeType=g_calcTradeType.GetTradeType(g_tradeParam,g_symbolInfo.GetAllSymbolInfo(),g_trade.GetTradeErrorState());

      bool isContinue=true;
      switch(tradeType)
        {
         case TRADE_OPEN:     isContinue=g_trade.OpenOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_MODIFY:   isContinue=g_trade.ModifyOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_DESTROY:  isContinue=g_trade.DestroyOrder(g_symbolInfo,g_tradeParam);
         break;

         case TRADE_CLOSEBY:  isContinue=g_trade.CloseCounter(g_tradeParam);
         break;

         case TRADE_FATAL_ERROR:
            ExpertRemove();
            return;
        }

      if(tradeType==TRADE_NONE || !isContinue)
         break;
     }
  }
//+------------------------------------------------------------------+
