//+------------------------------------------------------------------+
//|                                            Divergence_Expert.mq4 |
//|                                        Copyright 2015, Scriptong |
//|                                          http://advancetools.net |
//+------------------------------------------------------------------+
#property copyright "Scriptong"
#property link      "http://advancetools.net"
#property strict

#include <stderror.mqh>
#include <Divergence\Divergence_CalculateTradeType.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_YESNO
  {
   NO,                                                                                             // No / ���
   YES                                                                                             // Yes / ��
  };
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum ENUM_ONOFF
  {
   OFF,                                                                                            // Off / ����.
   ON                                                                                              // On / ���.
  };

//---- input parameters
input string                   i_string1             = "Parameters of orders / ��������� �������"; // ======================
input double                   i_staticLots          = 0.1;                                        // Constant volume / ���������� �����
input double                   i_dynamicLots         = 10.0;                                       // Dynamic volume, % of balance/ ������������ ����� � % �� �������
input double                   i_tpToSLRatio         = 2.0;                                        // Ratio of the TP size to SL / ��������� ������� TP � SL
input uint                     i_slOffset            = 1;                                          // Offset for Stop Loss, pts. / ������ ��� Stop Loss, ��.

input string                   i_string2="Divergence parameters / ��������� �����������"; // ============================
input ENUM_INDICATOR_TYPE      i_indicatorType       = WILLIAM_BLAU;                               // Base indicator / ������� ���������
input int                      i_divergenceDepth     = 20;                                         // Depth of 2nd ref. point search / ������� ������ 2�� ��. �����
input int                      i_barsPeriod1         = 8000;                                       // First calculate period / ������ ������ �������
input int                      i_barsPeriod2         = 2;                                          // Second calculate period / ������ ������ �������
input int                      i_barsPeriod3         = 1;                                          // Third calculate period / ������ ������ �������
input ENUM_APPLIED_PRICE       i_indAppliedPrice     = PRICE_CLOSE;                                // Applied price of indicator / ���� ������� ����������
input ENUM_MA_METHOD           i_indMAMethod         = MODE_EMA;                                   // MA calculate method / ����� ������� ��������
input int                      i_findExtInterval     = 10;                                         // Price ext. to indicator ext. / �� ����. ���� �� ����. ���.
input ENUM_MARKET_APPLIED_PRICE i_marketAppliedPrice = MARKET_APPLIED_PRICE_CLOSE;                 // Applied price of market / ������������ �������� ����
input string                   i_customName          = "Sentiment_Line";                           // The name of indicator / ��� ����������
input int                      i_customBuffer        = 0;                                          // Index of data buffer / ������ ������ ��� ����� ������
input ENUM_CUSTOM_PARAM_CNT    i_customParamCnt      = PARAM_CNT_3;                                // Amount of ind. parameters / ���-�� ���������� ����������
input double                   i_customParam1        = 13.0;                                       // Value of the 1st parameter / �������� 1-��� ���������
input double                   i_customParam2        = 1.0;                                        // Value of the 2nd parameter / �������� 2-��� ���������
input double                   i_customParam3        = 0.0;                                        // Value of the 3rd parameter / �������� 3-��� ���������
input double                   i_customParam4        = 0.0;                                        // Value of the 4th parameter / �������� 4-��� ���������
input double                   i_customParam5        = 0.0;                                        // Value of the 5th parameter / �������� 5-��� ���������
input double                   i_customParam6        = 0.0;                                        // Value of the 6th parameter / �������� 6-��� ���������
input double                   i_customParam7        = 0.0;                                        // Value of the 7th parameter / �������� 7-��� ���������
input double                   i_customParam8        = 0.0;                                        // Value of the 8th parameter / �������� 8-��� ���������
input double                   i_customParam9        = 0.0;                                        // Value of the 9th parameter / �������� 9-��� ���������
input double                   i_customParam10       = 0.0;                                        // Value of the 10th parameter / �������� 10-��� ���������
input double                   i_customParam11       = 0.0;                                        // Value of the 11th parameter / �������� 11-��� ���������
input double                   i_customParam12       = 0.0;                                        // Value of the 12th parameter / �������� 12-��� ���������
input double                   i_customParam13       = 0.0;                                        // Value of the 13th parameter / �������� 13-��� ���������
input double                   i_customParam14       = 0.0;                                        // Value of the 14th parameter / �������� 14-��� ���������
input double                   i_customParam15       = 0.0;                                        // Value of the 15th parameter / �������� 15-��� ���������
input double                   i_customParam16       = 0.0;                                        // Value of the 16th parameter / �������� 16-��� ���������
input double                   i_customParam17       = 0.0;                                        // Value of the 17th parameter / �������� 17-��� ���������
input double                   i_customParam18       = 0.0;                                        // Value of the 18th parameter / �������� 18-��� ���������
input double                   i_customParam19       = 0.0;                                        // Value of the 19th parameter / �������� 19-��� ���������
input double                   i_customParam20       = 0.0;                                        // Value of the 20th parameter / �������� 20-��� ���������
input ENUM_ONOFF               i_useCoincidenceCharts = OFF;                                       // The coincidence of charts / ���������� ��������
input ENUM_ONOFF               i_excludeOverlaps     = OFF;                                        // Exclude overlaps of lines / ��������� ��������� �����
input ENUM_YESNO               i_useClassA           = YES;                                        // Use class A divergence / ������������ ����������� ������ �
input ENUM_YESNO               i_useClassB           = YES;                                        // Use class B divergence / ������������ ����������� ������ B
input ENUM_YESNO               i_useClassC           = YES;                                        // Use class C divergence / ������������ ����������� ������ C
input ENUM_YESNO               i_useHidden           = YES;                                        // Use hidden divergence / ������������ ������� �����������

input string                   i_string3             = "Other parameters / ������ ���������";      // ======================
input int                      i_startBarsCnt        = 100;                                        // Start bars count / ���������� ����� �� ������
input ENUM_YESNO               i_is5Digits           = YES;                                        // Use 5-digits / ������������ 5-�������� ���������
input ENUM_YESNO               i_isECN               = NO;                                         // Use ECN / ������������ ECN
input int                      i_slippage            = 3;                                          // Slippage / ���������� �� ����������� ���
input string                   i_openOrderSound      = "ok.wav";                                   // Sound for open order / ���� ��� �������� ������
input int                      i_magicNumber         = 3;                                          // ID of expert orders / ID ������� ��������

//--- ���������� ���������� ��������
TradeParam           g_tradeParam;                                                                 // ��������� ��� �������� �������� ��������� �������
GetSymbolInfo       *g_symbolInfo;                                                                 // ����� ����� ������� �������� ����������
CTrade              *g_trade;                                                                      // ����� ���������� �������� ��������
CCalculateTradeType *g_calcTradeType;                                                              // ����� ������� ���������� ��������� �������
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� ������������� ��������                                                                                                                                                           |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
int OnInit()
  {
   if(!TuningParameters()) // ������������� �������� ����������� ����������
      return INIT_FAILED;

   if(!InitializeClasses()) // ������������� ������� GetSymbolInfo, Trade � CalculateTradeType
      return INIT_FAILED;

   return INIT_SUCCEEDED;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������� ��������������� ��������                                                                                                                                                         |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   delete g_symbolInfo;
   delete g_trade;
   delete g_calcTradeType;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| �������� �������� ����������� ����������                                                                                                                                                 |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool TuningParameters()
  {
   string name=WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");

   if(i_dynamicLots<0.01 || i_dynamicLots>99.99)
     {
      Alert(name,": �������� ������������� ������ �������������� � ���������. ���������� �������� �� 0.01 �� 99.99. ������� ��������.");
      Alert(name,(isRussianLang)? ": �������� ������������� ������ �������������� � ���������. ���������� �������� �� 0.01 �� 99.99. ������� ��������." :
            ": value of dynamic volume must be greater than 0.00 and less than 100.00. Expert is turned off.");
      return false;
     }

   if(i_barsPeriod1<1)
     {
      Alert(name,(isRussianLang)? ": ������ ���������� ����� ��� ������� ��������� ���������� ����� 1. ��������� ��������." :
            ": the first amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_barsPeriod2<1)
     {
      Alert(name,(isRussianLang)? ": ������ ���������� ����� ��� ������� ��������� ���������� ����� 1. ��������� ��������." :
            ": the second amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_barsPeriod3<1)
     {
      Alert(name,(isRussianLang)? ": ������ ���������� ����� ��� ������� ��������� ���������� ����� 1. ��������� ��������." :
            ": the third amount of bars for calculate the indicator values is less then 1. The indicator is turned off.");
      return false;
     }

   if(i_findExtInterval<1)
     {
      Alert(name,(isRussianLang)? ": �������� ������ ���������� ���� ������ ���� ����� ���� �����. ��������� ��������." :
            ": the interval of search of price extremum must be greater than zero bars. The indicator is turned off.");
      return false;
     }

   return true;
  }
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
//| ������������� ���� ����������� �������                                                                                                                                                   |
//+------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------+
bool InitializeClasses()
  {
   uint pointMul=1;
   if(i_is5Digits==YES)
      pointMul=10;

   g_symbolInfo= new GetSymbolInfo(Symbol(),i_slippage * pointMul,i_isECN == YES);
   string name = WindowExpertName();
   bool isRussianLang=(TerminalInfoString(TERMINAL_LANGUAGE)=="Russian");
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
      Alert(name,isRussianLang? ": ���������� ���������������� ����� Trade. ������� ��������!" :
            ": could not to initialize the class Trade. Expert is turned off!");
      return false;
     }

   g_calcTradeType=new CCalculateTradeType(i_staticLots,i_dynamicLots,i_tpToSLRatio,i_slOffset*pointMul*g_symbolInfo.GetPoint(),i_magicNumber,i_indicatorType,i_divergenceDepth,
                                           i_barsPeriod1,i_barsPeriod2,i_barsPeriod3,i_indAppliedPrice,i_indMAMethod,i_findExtInterval,i_marketAppliedPrice,i_customName,
                                           i_customBuffer,i_customParamCnt,i_customParam1,i_customParam2,i_customParam3,i_customParam4,i_customParam5,i_customParam6,
                                           i_customParam7,i_customParam8,i_customParam9,i_customParam10,i_customParam11,i_customParam12,i_customParam13,i_customParam14,
                                           i_customParam15,i_customParam16,i_customParam17,i_customParam18,i_customParam19,i_customParam20,i_useCoincidenceCharts==ON,
                                           i_excludeOverlaps==ON,i_useClassA==YES,i_useClassB==YES,i_useClassC==YES,i_useHidden==YES);
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
   if(Bars<i_startBarsCnt)
      return;

   static bool isStart=false;
   if(!isStart)
     {
      Alert("������!");
      isStart=true;
     }

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

      switch(tradeType)
        {
         case TRADE_OPEN:     if(!g_trade.OpenOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_MODIFY:   if(!g_trade.ModifyOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_DESTROY:  if(!g_trade.DestroyOrder(g_symbolInfo,g_tradeParam))
            return;
            break;

         case TRADE_CLOSEBY:  if(!g_trade.CloseCounter(g_tradeParam))
            return;
            break;

         case TRADE_FATAL_ERROR:
            ExpertRemove();
            return;

         case TRADE_NONE:     return;
        }
     }
  }
//+------------------------------------------------------------------+
