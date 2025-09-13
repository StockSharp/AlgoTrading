//+------------------------------------------------------------------+
//|                                      MultiIndicatorOptimizer.mq4 |
//|                                      Copyright � 2008, komposter |
//|                                      mailto:komposterius@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright � 2008, komposter"
#property link      "mailto:komposterius@mail.ru"

/*
�������� ���������� 5 �����������.
���������� (������, �� �����) ����� ������ � ���� �������� �� ����������� �� ���-�� ���������� � ������� ���������.
��� ������� ���������� ����� ��������� (�������� �����������):
 - ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);
 - ��� ������� �� �� (�� �1 �� MN):
   - ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
   - ��������� ����������.

�� ���� ���������� �����������/�� ������� ������� 2 �������.
������ �� ����������/�� ������������� ��� (������1 + ������2) / 2 * "��_�".
��������� (��������) �������� �������� ����� ���� �������� �� �����������/��.

������� ��� �����������:
	MACD:
		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		������2 - ��������� ���� ���������� ����� �������� +1, ��������� ���� ���������� ����� �������� -1;
	Awesome Oscillator:
		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		������2 - ��������� �������� ����� �������� +1, ��������� �������� ����� �������� -1;
	Moving Average of Oscillator:
		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		������2 �� ������������.
	Williams' Percent Range:
		������1 - ��������� ���������� ����� ��������������� �����=+1, ��������� ���������� ����� ��������������� ����=-1;
		������2 �� ������������.
	Stochastic Oscillator:
		������1 - %K ���������� ����� ��������������� �����=+1, %K ���������� ����� ��������������� ����=-1;
		������2 - %K ���� %D = +1, %K ���� %D = -1.

��������:
 - ���� �������� ������ ������ 0 � ���  ���-�������, ��������� ���;
 - ���� �������� ������ ������ 0 � ��� ����-�������, ��������� ����.

��������� ����������� �������:
 - ������ ����:
   - ���� "�" = 0, ������������ ��������� ������ ���� ("�1");
   - ���� "�" = 1, ��� ������������� �� �������: "(������ - �2) / �3 * �1", �� �� ������ "�1";
   - ���� ������ ���������� ������ �2*�4, �2 ������������� � �4 ���.
 - �� � ��:
   - ���� "�" = 0, �� � �� ����� ����� ��������������� ������� ����������;
   - ���� "�" = 1, �� � �� ������������� �� �������: "��������� ��(��) * �������� ������ * �1";

������������� �������:
 - ���� "�" = 1, � ���� �������� �������:
   - ����������� ����� ��/�� ��� ������� �� �������: "��������� ��(��) * �������� ������ * �1" (������ �� ���� �������� �������);
   - ���� ���� ����� �� ����� �������� �� ��� ��, ��������� �������;
   - ���� �� ��� �� ������� �� ����� ����������� ���������, ������������ ��.
*/

//---- ������� ���������� (��������� � ���� ������� ��������) - ����� ���������� �������� �� ���������
extern string 		Expert_Properties 				= "-------Expert-Properties-------";
extern int 			Expert_Id 							= 1135;	// ���������� ������������� ��������. ���� �� 2-� �������� � ���������� �������� � ����������� ������ �������� 2 ��������, ���� ���������� �� ������ Expert_Id

extern int			L										= 0;
extern double		L1										= 0.1;
extern double		L2										= 1000.0;
extern double		L3										= 500.0;
extern double		L4										= 2.0;

extern int			D										= 0;
extern double		D1										= 0.1;
extern int			StopLoss								= 50;		// ���������� �� ��������� � ������� (0 - ��������� ��)
extern int			TakeProfit							= 50;		// ���������� �� ����������� � ������� (0 - ��������� ��)

extern string 		Indicator_Properties 			= "------Indicator-Properties-----";

//+------------------------------------------------------------------+
//| Ind1
//+------------------------------------------------------------------+
extern int			Ind1_GLOBAL_Use					= 1;		// ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);

extern int			Ind1_M1_MACD_Weight				= 0;		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
extern int			Ind1_M1_MACD_FastPeriod			= 12;		// ������ ���������� ��� ���������� ������� ���������� �������
extern int			Ind1_M1_MACD_SlowPeriod			= 26;		// ������ ���������� ��� ���������� ��������� ���������� �������.
extern int			Ind1_M1_MACD_SignalPeriod		= 9;		// ������ ���������� ��� ���������� ���������� �����
extern int			Ind1_M1_MACD_Price				= 0;		// ������������ ����

extern int			Ind1_M5_MACD_Weight				= 0;
extern int			Ind1_M5_MACD_FastPeriod			= 12;
extern int			Ind1_M5_MACD_SlowPeriod			= 26;
extern int			Ind1_M5_MACD_SignalPeriod		= 9;
extern int			Ind1_M5_MACD_Price				= 0;

extern int			Ind1_M15_MACD_Weight				= 0;
extern int			Ind1_M15_MACD_FastPeriod		= 12;
extern int			Ind1_M15_MACD_SlowPeriod		= 26;
extern int			Ind1_M15_MACD_SignalPeriod		= 9;
extern int			Ind1_M15_MACD_Price				= 0;

extern int			Ind1_M30_MACD_Weight				= 0;
extern int			Ind1_M30_MACD_FastPeriod		= 12;
extern int			Ind1_M30_MACD_SlowPeriod		= 26;
extern int			Ind1_M30_MACD_SignalPeriod		= 9;
extern int			Ind1_M30_MACD_Price				= 0;

extern int			Ind1_H1_MACD_Weight				= 1;
extern int			Ind1_H1_MACD_FastPeriod			= 12;
extern int			Ind1_H1_MACD_SlowPeriod			= 26;
extern int			Ind1_H1_MACD_SignalPeriod		= 9;
extern int			Ind1_H1_MACD_Price				= 0;

extern int			Ind1_H4_MACD_Weight				= 0;
extern int			Ind1_H4_MACD_FastPeriod			= 12;
extern int			Ind1_H4_MACD_SlowPeriod			= 26;
extern int			Ind1_H4_MACD_SignalPeriod		= 9;
extern int			Ind1_H4_MACD_Price				= 0;

extern int			Ind1_D1_MACD_Weight				= 0;
extern int			Ind1_D1_MACD_FastPeriod			= 12;
extern int			Ind1_D1_MACD_SlowPeriod			= 26;
extern int			Ind1_D1_MACD_SignalPeriod		= 9;
extern int			Ind1_D1_MACD_Price				= 0;

extern int			Ind1_W1_MACD_Weight				= 0;
extern int			Ind1_W1_MACD_FastPeriod			= 12;
extern int			Ind1_W1_MACD_SlowPeriod			= 26;
extern int			Ind1_W1_MACD_SignalPeriod		= 9;
extern int			Ind1_W1_MACD_Price				= 0;

extern int			Ind1_MN_MACD_Weight				= 0;
extern int			Ind1_MN_MACD_FastPeriod			= 12;
extern int			Ind1_MN_MACD_SlowPeriod			= 26;
extern int			Ind1_MN_MACD_SignalPeriod		= 9;
extern int			Ind1_MN_MACD_Price				= 0;


//+------------------------------------------------------------------+
//| Ind2
//+------------------------------------------------------------------+
extern int			Ind2_GLOBAL_Use					= 1;		// ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);

extern int			Ind2_M1_AO_Weight					= 0;		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
extern int			Ind2_M5_AO_Weight					= 0;
extern int			Ind2_M15_AO_Weight				= 0;
extern int			Ind2_M30_AO_Weight				= 0;
extern int			Ind2_H1_AO_Weight					= 1;
extern int			Ind2_H4_AO_Weight					= 0;
extern int			Ind2_D1_AO_Weight					= 0;
extern int			Ind2_W1_AO_Weight					= 0;
extern int			Ind2_MN_AO_Weight					= 0;


//+------------------------------------------------------------------+
//| Ind3
//+------------------------------------------------------------------+
extern int			Ind3_GLOBAL_Use					= 1;		// ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);

extern int			Ind3_M1_OsMA_Weight				= 0;		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
extern int			Ind3_M1_OsMA_FastPeriod			= 12;		// ������ ���������� ��� ���������� ������� ���������� �������
extern int			Ind3_M1_OsMA_SlowPeriod			= 26;		// ������ ���������� ��� ���������� ��������� ���������� �������.
extern int			Ind3_M1_OsMA_SignalPeriod		= 9;		// ������ ���������� ��� ���������� ���������� �����
extern int			Ind3_M1_OsMA_Price				= 0;		// ������������ ����

extern int			Ind3_M5_OsMA_Weight				= 0;
extern int			Ind3_M5_OsMA_FastPeriod			= 12;
extern int			Ind3_M5_OsMA_SlowPeriod			= 26;
extern int			Ind3_M5_OsMA_SignalPeriod		= 9;
extern int			Ind3_M5_OsMA_Price				= 0;

extern int			Ind3_M15_OsMA_Weight				= 0;
extern int			Ind3_M15_OsMA_FastPeriod		= 12;
extern int			Ind3_M15_OsMA_SlowPeriod		= 26;
extern int			Ind3_M15_OsMA_SignalPeriod		= 9;
extern int			Ind3_M15_OsMA_Price				= 0;

extern int			Ind3_M30_OsMA_Weight				= 0;
extern int			Ind3_M30_OsMA_FastPeriod		= 12;
extern int			Ind3_M30_OsMA_SlowPeriod		= 26;
extern int			Ind3_M30_OsMA_SignalPeriod		= 9;
extern int			Ind3_M30_OsMA_Price				= 0;

extern int			Ind3_H1_OsMA_Weight				= 1;
extern int			Ind3_H1_OsMA_FastPeriod			= 12;
extern int			Ind3_H1_OsMA_SlowPeriod			= 26;
extern int			Ind3_H1_OsMA_SignalPeriod		= 9;
extern int			Ind3_H1_OsMA_Price				= 0;

extern int			Ind3_H4_OsMA_Weight				= 0;
extern int			Ind3_H4_OsMA_FastPeriod			= 12;
extern int			Ind3_H4_OsMA_SlowPeriod			= 26;
extern int			Ind3_H4_OsMA_SignalPeriod		= 9;
extern int			Ind3_H4_OsMA_Price				= 0;

extern int			Ind3_D1_OsMA_Weight				= 0;
extern int			Ind3_D1_OsMA_FastPeriod			= 12;
extern int			Ind3_D1_OsMA_SlowPeriod			= 26;
extern int			Ind3_D1_OsMA_SignalPeriod		= 9;
extern int			Ind3_D1_OsMA_Price				= 0;

extern int			Ind3_W1_OsMA_Weight				= 0;
extern int			Ind3_W1_OsMA_FastPeriod			= 12;
extern int			Ind3_W1_OsMA_SlowPeriod			= 26;
extern int			Ind3_W1_OsMA_SignalPeriod		= 9;
extern int			Ind3_W1_OsMA_Price				= 0;

extern int			Ind3_MN_OsMA_Weight				= 0;
extern int			Ind3_MN_OsMA_FastPeriod			= 12;
extern int			Ind3_MN_OsMA_SlowPeriod			= 26;
extern int			Ind3_MN_OsMA_SignalPeriod		= 9;
extern int			Ind3_MN_OsMA_Price				= 0;


//+------------------------------------------------------------------+
//| Ind4
//+------------------------------------------------------------------+
extern int			Ind4_GLOBAL_Use					= 1;		// ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);

extern int			Ind4_M1_WPR_Weight				= 0;		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
extern int			Ind4_M1_WPR_Period				= 14;		// ������(���������� �����) ��� ���������� ����������
extern double		Ind4_M1_WPR_DN_Level				= -80;	// ������� ���������������
extern double		Ind4_M1_WPR_UP_Level				= -20;		// ������� ���������������

extern int			Ind4_M5_WPR_Weight				= 0;
extern int			Ind4_M5_WPR_Period				= 14;
extern double		Ind4_M5_WPR_DN_Level				= -80;
extern double		Ind4_M5_WPR_UP_Level				= -20;

extern int			Ind4_M15_WPR_Weight				= 0;
extern int			Ind4_M15_WPR_Period				= 14;
extern double		Ind4_M15_WPR_DN_Level			= -80;
extern double		Ind4_M15_WPR_UP_Level			= -20;

extern int			Ind4_M30_WPR_Weight				= 0;
extern int			Ind4_M30_WPR_Period				= 14;
extern double		Ind4_M30_WPR_DN_Level			= -80;
extern double		Ind4_M30_WPR_UP_Level			= -20;

extern int			Ind4_H1_WPR_Weight				= 1;
extern int			Ind4_H1_WPR_Period				= 14;
extern double		Ind4_H1_WPR_DN_Level				= -80;
extern double		Ind4_H1_WPR_UP_Level				= -20;

extern int			Ind4_H4_WPR_Weight				= 0;
extern int			Ind4_H4_WPR_Period				= 14;
extern double		Ind4_H4_WPR_DN_Level				= -80;
extern double		Ind4_H4_WPR_UP_Level				= -20;

extern int			Ind4_D1_WPR_Weight				= 0;
extern int			Ind4_D1_WPR_Period				= 14;
extern double		Ind4_D1_WPR_DN_Level				= -80;
extern double		Ind4_D1_WPR_UP_Level				= -20;

extern int			Ind4_W1_WPR_Weight				= 0;
extern int			Ind4_W1_WPR_Period				= 14;
extern double		Ind4_W1_WPR_DN_Level				= -80;
extern double		Ind4_W1_WPR_UP_Level				= -20;

extern int			Ind4_MN_WPR_Weight				= 0;
extern int			Ind4_MN_WPR_Period				= 14;
extern double		Ind4_MN_WPR_DN_Level				= -80;
extern double		Ind4_MN_WPR_UP_Level				= -20;


//+------------------------------------------------------------------+
//| Ind5
//+------------------------------------------------------------------+
extern int			Ind5_GLOBAL_Use					= 1;		// ���������� ���� ������������� ���������� (1 - ��������� ������������, 0 - ��������� �� ������������);

extern int			Ind5_M1_Stoch_Weight				= 0;		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
extern int			Ind5_M1_Stoch_Kperiod			= 5;		// ������(���������� �����) ��� ���������� ����� %K
extern int			Ind5_M1_Stoch_Dperiod			= 3;		// ������ ���������� ��� ���������� ����� %D
extern int			Ind5_M1_Stoch_Slowing			= 3;		// �������� ����������
extern int			Ind5_M1_Stoch_Method				= 0;		// ����� ����������
extern int			Ind5_M1_Stoch_Price				= 0;		// ������������ ����
extern double		Ind5_M1_Stoch_DN_Level			= 20;		// ������� ���������������
extern double		Ind5_M1_Stoch_UP_Level			= 80;		// ������� ���������������

extern int			Ind5_M5_Stoch_Weight				= 0;
extern int			Ind5_M5_Stoch_Kperiod			= 5;
extern int			Ind5_M5_Stoch_Dperiod			= 3;
extern int			Ind5_M5_Stoch_Slowing			= 3;
extern int			Ind5_M5_Stoch_Method				= 0;
extern int			Ind5_M5_Stoch_Price				= 0;
extern double		Ind5_M5_Stoch_DN_Level			= 20;
extern double		Ind5_M5_Stoch_UP_Level			= 80;

extern int			Ind5_M15_Stoch_Weight			= 0;
extern int			Ind5_M15_Stoch_Kperiod			= 5;
extern int			Ind5_M15_Stoch_Dperiod			= 3;
extern int			Ind5_M15_Stoch_Slowing			= 3;
extern int			Ind5_M15_Stoch_Method			= 0;
extern int			Ind5_M15_Stoch_Price				= 0;
extern double		Ind5_M15_Stoch_DN_Level			= 20;
extern double		Ind5_M15_Stoch_UP_Level			= 80;

extern int			Ind5_M30_Stoch_Weight			= 0;
extern int			Ind5_M30_Stoch_Kperiod			= 5;
extern int			Ind5_M30_Stoch_Dperiod			= 3;
extern int			Ind5_M30_Stoch_Slowing			= 3;
extern int			Ind5_M30_Stoch_Method			= 0;
extern int			Ind5_M30_Stoch_Price				= 0;
extern double		Ind5_M30_Stoch_DN_Level			= 20;
extern double		Ind5_M30_Stoch_UP_Level			= 80;

extern int			Ind5_H1_Stoch_Weight				= 1;
extern int			Ind5_H1_Stoch_Kperiod			= 5;
extern int			Ind5_H1_Stoch_Dperiod			= 3;
extern int			Ind5_H1_Stoch_Slowing			= 3;
extern int			Ind5_H1_Stoch_Method				= 0;
extern int			Ind5_H1_Stoch_Price				= 0;
extern double		Ind5_H1_Stoch_DN_Level			= 20;
extern double		Ind5_H1_Stoch_UP_Level			= 80;

extern int			Ind5_H4_Stoch_Weight				= 0;
extern int			Ind5_H4_Stoch_Kperiod			= 5;
extern int			Ind5_H4_Stoch_Dperiod			= 3;
extern int			Ind5_H4_Stoch_Slowing			= 3;
extern int			Ind5_H4_Stoch_Method				= 0;
extern int			Ind5_H4_Stoch_Price				= 0;
extern double		Ind5_H4_Stoch_DN_Level			= 20;
extern double		Ind5_H4_Stoch_UP_Level			= 80;

extern int			Ind5_D1_Stoch_Weight				= 0;
extern int			Ind5_D1_Stoch_Kperiod			= 5;
extern int			Ind5_D1_Stoch_Dperiod			= 3;
extern int			Ind5_D1_Stoch_Slowing			= 3;
extern int			Ind5_D1_Stoch_Method				= 0;
extern int			Ind5_D1_Stoch_Price				= 0;
extern double		Ind5_D1_Stoch_DN_Level			= 20;
extern double		Ind5_D1_Stoch_UP_Level			= 80;

extern int			Ind5_W1_Stoch_Weight				= 0;
extern int			Ind5_W1_Stoch_Kperiod			= 5;
extern int			Ind5_W1_Stoch_Dperiod			= 3;
extern int			Ind5_W1_Stoch_Slowing			= 3;
extern int			Ind5_W1_Stoch_Method				= 0;
extern int			Ind5_W1_Stoch_Price				= 0;
extern double		Ind5_W1_Stoch_DN_Level			= 20;
extern double		Ind5_W1_Stoch_UP_Level			= 80;

extern int			Ind5_MN_Stoch_Weight				= 0;
extern int			Ind5_MN_Stoch_Kperiod			= 5;
extern int			Ind5_MN_Stoch_Dperiod			= 3;
extern int			Ind5_MN_Stoch_Slowing			= 3;
extern int			Ind5_MN_Stoch_Method				= 0;
extern int			Ind5_MN_Stoch_Price				= 0;
extern double		Ind5_MN_Stoch_DN_Level			= 20;
extern double		Ind5_MN_Stoch_UP_Level			= 80;


#include <trade_lib&info_lib.mqh>

int		SummSignal						= 0;
int		counted_bar						= 0;
int		last_trade						= 0;

int		Ind1_PeriodsCount				= 0;
int		Ind1_Periods					[9];
int		Ind1_MACD_Weight				[9];		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
int		Ind1_MACD_FastPeriod			[9];		// ������ ���������� ��� ���������� ������� ���������� �������
int		Ind1_MACD_SlowPeriod			[9];		// ������ ���������� ��� ���������� ��������� ���������� �������.
int		Ind1_MACD_SignalPeriod		[9];		// ������ ���������� ��� ���������� ���������� �����
int		Ind1_MACD_Price				[9];		// ������������ ����

int		Ind2_PeriodsCount				= 0;
int		Ind2_Periods					[9];
int		Ind2_AO_Weight					[9];		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);

int		Ind3_PeriodsCount				= 0;
int		Ind3_Periods					[9];
int		Ind3_OsMA_Weight				[9];		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
int		Ind3_OsMA_FastPeriod			[9];		// ������ ���������� ��� ���������� ������� ���������� �������
int		Ind3_OsMA_SlowPeriod			[9];		// ������ ���������� ��� ���������� ��������� ���������� �������.
int		Ind3_OsMA_SignalPeriod		[9];		// ������ ���������� ��� ���������� ���������� �����
int		Ind3_OsMA_Price				[9];		// ������������ ����

int		Ind4_PeriodsCount				= 0;
int		Ind4_Periods					[9];
int		Ind4_WPR_Weight				[9];		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
int		Ind4_WPR_Period				[9];		// ������(���������� �����) ��� ���������� ����������
double	Ind4_WPR_DN_Level				[9];		// ������� ���������������
double	Ind4_WPR_UP_Level				[9];		// ������� ���������������

int		Ind5_PeriodsCount				= 0;
int		Ind5_Periods					[9];
int		Ind5_Stoch_Weight				[9];		// ��� ������� ���������� �� ������ �� (0 - ��������� �� ������������, >0 - ������������);
int		Ind5_Stoch_Kperiod			[9];		// ������(���������� �����) ��� ���������� ����� %K
int		Ind5_Stoch_Dperiod			[9];		// ������ ���������� ��� ���������� ����� %D
int		Ind5_Stoch_Slowing			[9];		// �������� ����������
int		Ind5_Stoch_Method				[9];		// ����� ����������
int		Ind5_Stoch_Price				[9];		// ������������ ����
double	Ind5_Stoch_DN_Level			[9];		// ������� ���������������
double	Ind5_Stoch_UP_Level			[9];		// ������� ���������������

int init()
{
	TradeInfoLib_Initialization ( Expert_Id, "MultiIndicatorOptimizer" );

	// ���������� ��� "�������" �� � ������� ��� �������� �������
	Ind1_PeriodsCount = 0;
	if ( Ind1_GLOBAL_Use == 1 )
	{
		if ( Ind1_MN_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_MN1;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_MN_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_MN_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_MN_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_MN_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_MN_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_W1_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_W1;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_W1_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_W1_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_W1_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_W1_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_W1_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_D1_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_D1;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_D1_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_D1_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_D1_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_D1_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_D1_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_H4_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_H4;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_H4_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_H4_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_H4_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_H4_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_H4_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_H1_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_H1;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_H1_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_H1_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_H1_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_H1_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_H1_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_M30_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_M30;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_M30_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_M30_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_M30_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_M30_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_M30_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_M15_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_M15;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_M15_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_M15_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_M15_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_M15_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_M15_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_M5_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_M5;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_M5_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_M5_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_M5_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_M5_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_M5_MACD_Price;

			Ind1_PeriodsCount ++;
		}
		if ( Ind1_M1_MACD_Weight != 0 )
		{
			Ind1_Periods					[Ind1_PeriodsCount] = PERIOD_M1;
			Ind1_MACD_Weight				[Ind1_PeriodsCount] = Ind1_M1_MACD_Weight;
			Ind1_MACD_FastPeriod			[Ind1_PeriodsCount] = Ind1_M1_MACD_FastPeriod;
			Ind1_MACD_SlowPeriod			[Ind1_PeriodsCount] = Ind1_M1_MACD_SlowPeriod;
			Ind1_MACD_SignalPeriod		[Ind1_PeriodsCount] = Ind1_M1_MACD_SignalPeriod;
			Ind1_MACD_Price				[Ind1_PeriodsCount] = Ind1_M1_MACD_Price;

			Ind1_PeriodsCount ++;
		}
	}


	if ( Ind2_GLOBAL_Use == 1)
	{
		Ind2_PeriodsCount = 0;
		if ( Ind2_MN_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_MN1;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_MN_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_W1_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_W1;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_W1_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_D1_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_D1;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_D1_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_H4_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_H4;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_H4_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_H1_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_H1;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_H1_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_M30_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_M30;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_M30_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_M15_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_M15;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_M15_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_M5_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_M5;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_M5_AO_Weight;
			Ind2_PeriodsCount ++;
		}
		if ( Ind2_M1_AO_Weight != 0 )
		{
			Ind2_Periods					[Ind2_PeriodsCount] = PERIOD_M1;
			Ind2_AO_Weight					[Ind2_PeriodsCount] = Ind2_M1_AO_Weight;
			Ind2_PeriodsCount ++;
		}
	}


	if ( Ind3_GLOBAL_Use == 1)
	{
		Ind3_PeriodsCount = 0;
		if ( Ind3_MN_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_MN1;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_MN_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_MN_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_MN_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_MN_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_MN_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_W1_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_W1;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_W1_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_W1_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_W1_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_W1_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_W1_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_D1_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_D1;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_D1_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_D1_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_D1_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_D1_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_D1_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_H4_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_H4;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_H4_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_H4_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_H4_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_H4_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_H4_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_H1_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_H1;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_H1_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_H1_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_H1_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_H1_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_H1_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_M30_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_M30;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_M30_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_M30_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_M30_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_M30_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_M30_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_M15_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_M15;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_M15_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_M15_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_M15_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_M15_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_M15_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_M5_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_M5;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_M5_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_M5_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_M5_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_M5_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_M5_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
		if ( Ind3_M1_OsMA_Weight != 0 )
		{
			Ind3_Periods					[Ind3_PeriodsCount] = PERIOD_M1;
			Ind3_OsMA_Weight				[Ind3_PeriodsCount] = Ind3_M1_OsMA_Weight;
			Ind3_OsMA_FastPeriod			[Ind3_PeriodsCount] = Ind3_M1_OsMA_FastPeriod;
			Ind3_OsMA_SlowPeriod			[Ind3_PeriodsCount] = Ind3_M1_OsMA_SlowPeriod;
			Ind3_OsMA_SignalPeriod		[Ind3_PeriodsCount] = Ind3_M1_OsMA_SignalPeriod;
			Ind3_OsMA_Price				[Ind3_PeriodsCount] = Ind3_M1_OsMA_Price;
			Ind3_PeriodsCount ++;
		}
	}


	if ( Ind4_GLOBAL_Use == 1)
	{
		Ind4_PeriodsCount = 0;
		if ( Ind4_MN_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_MN1;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_MN_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_MN_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_MN_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_MN_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_W1_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_W1;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_W1_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_W1_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_W1_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_W1_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_D1_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_D1;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_D1_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_D1_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_D1_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_D1_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_H4_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_H4;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_H4_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_H4_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_H4_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_H4_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_H1_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_H1;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_H1_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_H1_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_H1_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_H1_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_M30_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_M30;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_M30_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_M30_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_M30_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_M30_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_M15_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_M15;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_M15_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_M15_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_M15_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_M15_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_M5_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_M5;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_M5_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_M5_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_M5_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_M5_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
		if ( Ind4_M1_WPR_Weight != 0 )
		{
			Ind4_Periods					[Ind4_PeriodsCount] = PERIOD_M1;
			Ind4_WPR_Weight				[Ind4_PeriodsCount] = Ind4_M1_WPR_Weight;
			Ind4_WPR_Period				[Ind4_PeriodsCount] = Ind4_M1_WPR_Period;
			Ind4_WPR_DN_Level				[Ind4_PeriodsCount] = Ind4_M1_WPR_DN_Level;
			Ind4_WPR_UP_Level				[Ind4_PeriodsCount] = Ind4_M1_WPR_UP_Level;
			Ind4_PeriodsCount ++;
		}
	}


	if ( Ind5_GLOBAL_Use == 1)
	{
		Ind5_PeriodsCount = 0;
		if ( Ind5_MN_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_MN1;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_MN_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_MN_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_MN_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_MN_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_MN_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_MN_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_MN_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_MN_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_W1_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_W1;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_W1_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_W1_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_W1_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_W1_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_W1_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_W1_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_W1_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_W1_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_D1_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_D1;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_D1_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_D1_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_D1_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_D1_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_D1_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_D1_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_D1_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_D1_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_H4_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_H4;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_H4_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_H4_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_H4_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_H4_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_H4_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_H4_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_H4_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_H4_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_H1_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_H1;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_H1_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_H1_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_H1_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_H1_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_H1_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_H1_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_H1_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_H1_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_M30_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_M30;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_M30_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_M30_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_M30_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_M30_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_M30_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_M30_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_M30_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_M30_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_M15_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_M15;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_M15_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_M15_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_M15_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_M15_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_M15_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_M15_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_M15_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_M15_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_M5_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_M5;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_M5_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_M5_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_M5_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_M5_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_M5_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_M5_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_M5_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_M5_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
		if ( Ind5_M1_Stoch_Weight != 0 )
		{
			Ind5_Periods					[Ind5_PeriodsCount] = PERIOD_M1;
			Ind5_Stoch_Weight				[Ind5_PeriodsCount] = Ind5_M1_Stoch_Weight;
			Ind5_Stoch_Kperiod			[Ind5_PeriodsCount] = Ind5_M1_Stoch_Kperiod;
			Ind5_Stoch_Dperiod			[Ind5_PeriodsCount] = Ind5_M1_Stoch_Dperiod;
			Ind5_Stoch_Slowing			[Ind5_PeriodsCount] = Ind5_M1_Stoch_Slowing;
			Ind5_Stoch_Method				[Ind5_PeriodsCount] = Ind5_M1_Stoch_Method;
			Ind5_Stoch_Price				[Ind5_PeriodsCount] = Ind5_M1_Stoch_Price;
			Ind5_Stoch_DN_Level			[Ind5_PeriodsCount] = Ind5_M1_Stoch_DN_Level;
			Ind5_Stoch_UP_Level			[Ind5_PeriodsCount] = Ind5_M1_Stoch_UP_Level;
			Ind5_PeriodsCount ++;
		}
	}
	return(0);
}

int deinit()
{
	TradeInfoLib_Deinitialization();
	return(0);
}

int start()
{
	if ( !IsOK() ) return;

	//    - ���� ������ ���������� ������ �2*�4, �2 ������������� � �4 ���.
	while ( AccountEquity() >= L2*L4 ) L2 *= L4;

	find_signal();		// ����������� ����������
	trade_control();	// ��������� ���������

	return(0);
}

void find_signal()
{
	// �������� ������ ��� ��������� ������ ���� �� ����� ������� �� ������������� ��
	if ( Time[0] <= counted_bar ) return;
	counted_bar = Time[0];

	SummSignal = 0;

	int p, p_signal_1, p_signal_2;

	// �� ���� ���������� �����������/�� ������� ������� 2 �������.
	for ( p = 0; p < Ind1_PeriodsCount; p ++ )
	{
		p_signal_1 = 0; p_signal_2 = 0;

		// 	MACD:
		double macd_m = iMACD( _Symbol, Ind1_Periods[p], Ind1_MACD_FastPeriod[p], Ind1_MACD_SlowPeriod[p], Ind1_MACD_SignalPeriod[p], Ind1_MACD_Price[p], MODE_MAIN, 1 );
		double macd_s = iMACD( _Symbol, Ind1_Periods[p], Ind1_MACD_FastPeriod[p], Ind1_MACD_SlowPeriod[p], Ind1_MACD_SignalPeriod[p], Ind1_MACD_Price[p], MODE_SIGNAL, 1 );

		// 		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		if ( macd_m > 0.0 ) p_signal_1 =  1;
		if ( macd_m < 0.0 ) p_signal_1 = -1;

		// 		������2 - ��������� ���� ���������� ����� �������� +1, ��������� ���� ���������� ����� �������� -1;
		if ( macd_m > macd_s ) p_signal_2 =  1;
		if ( macd_m < macd_s ) p_signal_2 = -1;

		// ������ �� ����������/�� ������������� ��� (������1 + ������2) / 2 * "��_�".
		// ��������� (��������) �������� �������� ����� ���� �������� �� �����������/��.
		SummSignal += (p_signal_1 + p_signal_2) / 2.0 * Ind1_MACD_Weight[p];
	}

	for ( p = 0; p < Ind2_PeriodsCount; p ++ )
	{
		p_signal_1 = 0; p_signal_2 = 0;

		// 	Awesome Oscillator:
		double ao_1 = iAO( _Symbol, Ind2_Periods[p], 1 );
		double ao_2 = iAO( _Symbol, Ind2_Periods[p], 2 );

		// 		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		if ( ao_1 > 0.0 ) p_signal_1 =  1;
		if ( ao_1 < 0.0 ) p_signal_1 = -1;

		// 		������2 - ��������� �������� ����� �������� +1, ��������� �������� ����� �������� -1;
		if ( ao_1 > ao_2 ) p_signal_2 =  1;
		if ( ao_1 < ao_2 ) p_signal_2 = -1;

		SummSignal += (p_signal_1 + p_signal_2) / 2.0 * Ind2_AO_Weight[p];
	}

	for ( p = 0; p < Ind3_PeriodsCount; p ++ )
	{
		p_signal_1 = 0; p_signal_2 = 0;

		// 	Moving Average of Oscillator:
		double osma_1 = iOsMA( _Symbol, Ind3_Periods[p], Ind3_OsMA_FastPeriod[p], Ind3_OsMA_SlowPeriod[p], Ind3_OsMA_SignalPeriod[p], Ind3_OsMA_Price[p], 1 );

		// 		������1 - ��������� ���� ������� ����� ��������=+1, ��������� ���� ������� ����� ��������=-1;
		if ( osma_1 > 0.0 ) p_signal_1 =  1;
		if ( osma_1 < 0.0 ) p_signal_1 = -1;

		// 		������2 �� ������������.
		p_signal_2 = p_signal_1;

		SummSignal += (p_signal_1 + p_signal_2) / 2.0 * Ind3_OsMA_Weight[p];
	}

	for ( p = 0; p < Ind4_PeriodsCount; p ++ )
	{
		p_signal_1 = 0; p_signal_2 = 0;

		// 	Williams' Percent Range:
		double wpr_1 = iWPR( _Symbol, Ind4_Periods[p], Ind4_WPR_Period[p], 1 );
		double wpr_2 = iWPR( _Symbol, Ind4_Periods[p], Ind4_WPR_Period[p], 2 );

		// 		������1 - ��������� ���������� ����� ��������������� �����=+1, ��������� ���������� ����� ��������������� ����=-1;
		if ( wpr_1 > Ind4_WPR_DN_Level[p] && wpr_2 <= Ind4_WPR_DN_Level[p] ) p_signal_1 =  1;
		if ( wpr_1 < Ind4_WPR_UP_Level[p] && wpr_2 >= Ind4_WPR_UP_Level[p] ) p_signal_1 = -1;

		// 		������2 �� ������������.
		p_signal_2 = p_signal_1;

		SummSignal += (p_signal_1 + p_signal_2) / 2.0 * Ind4_WPR_Weight[p];
	}

	for ( p = 0; p < Ind5_PeriodsCount; p ++ )
	{
		p_signal_1 = 0; p_signal_2 = 0;

		// 	Stochastic Oscillator:
		double sto_m_1 = iStochastic( _Symbol, Ind5_Periods[p], Ind5_Stoch_Kperiod[p], Ind5_Stoch_Dperiod[p], Ind5_Stoch_Slowing[p], Ind5_Stoch_Method[p], Ind5_Stoch_Price[p], MODE_MAIN, 1 );
		double sto_m_2 = iStochastic( _Symbol, Ind5_Periods[p], Ind5_Stoch_Kperiod[p], Ind5_Stoch_Dperiod[p], Ind5_Stoch_Slowing[p], Ind5_Stoch_Method[p], Ind5_Stoch_Price[p], MODE_MAIN, 2 );
		double sto_s_1 = iStochastic( _Symbol, Ind5_Periods[p], Ind5_Stoch_Kperiod[p], Ind5_Stoch_Dperiod[p], Ind5_Stoch_Slowing[p], Ind5_Stoch_Method[p], Ind5_Stoch_Price[p], MODE_SIGNAL, 1 );

		// 		������1 - %K ���������� ����� ��������������� �����=+1, %K ���������� ����� ��������������� ����=-1;
		if ( sto_m_1 > Ind5_Stoch_DN_Level[p] && sto_m_2 <= Ind5_Stoch_DN_Level[p] ) p_signal_1 =  1;
		if ( sto_m_1 < Ind5_Stoch_UP_Level[p] && sto_m_2 >= Ind5_Stoch_UP_Level[p] ) p_signal_1 = -1;

		// 		������2 - %K ���� %D = +1, %K ���� %D = -1.
		if ( sto_m_1 > sto_s_1 ) p_signal_2 =  1;
		if ( sto_m_1 < sto_s_1 ) p_signal_2 = -1;

		SummSignal += (p_signal_1 + p_signal_2) / 2.0 * Ind5_Stoch_Weight[p];
	}
}

//+------------------------------------------------------------------+
//| �-��� �������� �������� �������                                  |
//+------------------------------------------------------------------+
void trade_control()
{
	int _OrdersTotal = OrdersTotal(), _GetLastError, type; bool opened_b = false, opened_s = false, error = false; double SLTP_k, _StopLossLevel, _TakeProfitLevel;

	for ( int z = _OrdersTotal - 1; z >= 0; z -- )
	{
		if ( !OrderSelect( z, SELECT_BY_POS ) )
		{
			_GetLastError = GetLastError();
			Print( "OrderSelect( ", z, ", SELECT_BY_POS ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}
		if ( OrderMagicNumber() != _MagicNumber ) continue;
		if ( OrderSymbol() != _Symbol ) continue;

		type = OrderType();

		// ������������� �������:
		//  - ���� "�" = 1, � ���� �������� �������:
		//    - ����������� ����� ��/�� ��� ������� �� �������: "��������� ��(��) * �������� ������ * �1" (������ �� ���� �������� �������);
		//    - ���� ���� ����� �� ����� �������� �� ��� ��, ��������� �������;
		//    - ���� �� ��� �� ������� �� ����� ����������� ���������, ������������ ��.
		if ( type == OP_BUY )
		{
			opened_b = true;
			if ( D == 1 && SummSignal != 0 )
			{
				SLTP_k = SummSignal * D1;

				if ( StopLoss > 0 )
				{ _StopLossLevel = NormalizeDouble( OrderOpenPrice() - StopLoss*SLTP_k*_Point, _Digits ); }
				else
				{ _StopLossLevel = 0.0; }

				if ( TakeProfit > 0 )
				{ _TakeProfitLevel = NormalizeDouble( OrderOpenPrice() + TakeProfit*SLTP_k*_Point, _Digits ); }
				else
				{ _TakeProfitLevel = 0.0; }

				if ( _StopLossLevel > _Point/2.0 && NormalizeDouble( _StopLossLevel - Bid, _Digits ) >= 0.0 )
				{
					Print( "SL for buy order #", OrderTicket(), " (", DoubleToStr( _StopLossLevel, _Digits ), ")!" );
					if ( _OrderClose( OrderTicket() ) < 0 ) error = true;
					continue;
				}
				if ( _TakeProfitLevel > _Point/2.0 && NormalizeDouble( Bid - _TakeProfitLevel, _Digits ) >= 0.0 )
				{
					Print( "TP for buy order #", OrderTicket(), " (", DoubleToStr( _TakeProfitLevel, _Digits ), ")!" );
					if ( _OrderClose( OrderTicket() ) < 0 ) error = true;
					continue;
				}

				if ( NormalizeDouble( MathAbs( _StopLossLevel - OrderStopLoss() ), _Digits ) > _Point/2.0 || NormalizeDouble( MathAbs( _TakeProfitLevel - OrderTakeProfit() ), _Digits ) > _Point/2.0 )
				{
					Print( "New SL/TP for buy order #", OrderTicket(), " (", DoubleToStr( _StopLossLevel, _Digits ), "/", DoubleToStr( _TakeProfitLevel, _Digits ), ")!" );
					_OrderModify( OrderTicket(), OrderOpenPrice(), _StopLossLevel, _TakeProfitLevel );
					continue;
				}
			}
		}
		if ( type == OP_SELL )
		{
			opened_s = true;
			if ( D == 1 && SummSignal != 0 )
			{
				SLTP_k = -SummSignal * D1;

				if ( StopLoss > 0 )
				{ _StopLossLevel = NormalizeDouble( OrderOpenPrice() + StopLoss*SLTP_k*_Point, _Digits ); }
				else
				{ _StopLossLevel = 0.0; }

				if ( TakeProfit > 0 )
				{ _TakeProfitLevel = NormalizeDouble( OrderOpenPrice() - TakeProfit*SLTP_k*_Point, _Digits ); }
				else
				{ _TakeProfitLevel = 0.0; }

				if ( _StopLossLevel > _Point/2.0 && NormalizeDouble( Ask - _StopLossLevel, _Digits ) >= 0.0 )
				{
					Print( "SL for sell order #", OrderTicket(), " (", DoubleToStr( _StopLossLevel, _Digits ), ")!" );
					if ( _OrderClose( OrderTicket() ) < 0 ) error = true;
					continue;
				}
				if ( _TakeProfitLevel > _Point/2.0 && NormalizeDouble( _TakeProfitLevel - Ask, _Digits ) >= 0.0 )
				{
					Print( "TP for sell order #", OrderTicket(), " (", DoubleToStr( _TakeProfitLevel, _Digits ), ")!" );
					if ( _OrderClose( OrderTicket() ) < 0 ) error = true;
					continue;
				}

				if ( NormalizeDouble( MathAbs( _StopLossLevel - OrderStopLoss() ), _Digits ) > _Point/2.0 || NormalizeDouble( MathAbs( _TakeProfitLevel - OrderTakeProfit() ), _Digits ) > _Point/2.0 )
				{
					Print( "New SL/TP for sell order #", OrderTicket(), " (", DoubleToStr( _StopLossLevel, _Digits ), "/", DoubleToStr( _TakeProfitLevel, _Digits ), ")!" );
					_OrderModify( OrderTicket(), OrderOpenPrice(), _StopLossLevel, _TakeProfitLevel );
					continue;
				}
			}
		}
	}

	// ��������:
	//  - ���� �������� ������ ������ 0 � ���  ���-�������, ��������� ���;
	//  - ���� �������� ������ ������ 0 � ��� ����-�������, ��������� ����.
	if ( !opened_b && SummSignal > 0 ) OpenBuy ();
	if ( !opened_s && SummSignal < 0 ) OpenSell();

	if ( !error ) SummSignal = 0;
}

int OpenBuy()
{
	if ( Time[0] <= last_trade ) return(0);

	double _OpenPriceLevel, _StopLossLevel, _TakeProfitLevel;
	_OpenPriceLevel = NormalizeDouble( Ask, _Digits );

	//  - �� � ��:
	//    - ���� "�" = 0, �� � �� ����� ����� ��������������� ������� ����������;
	//    - ���� "�" = 1, �� � �� ������������� �� �������: "��������� ��(��) * �������� ������ * �1";
	double SLTP_k = 1.0;
	if ( D == 1 ) SLTP_k = SummSignal * D1;

	if ( StopLoss > 0 )
	{ _StopLossLevel = NormalizeDouble( _OpenPriceLevel - StopLoss*SLTP_k*_Point, _Digits ); }
	else
	{ _StopLossLevel = 0.0; }

	if ( TakeProfit > 0 )
	{ _TakeProfitLevel = NormalizeDouble( _OpenPriceLevel + TakeProfit*SLTP_k*_Point, _Digits ); }
	else
	{ _TakeProfitLevel = 0.0; }

	//  - ������ ����:
	//    - ���� "�" = 0, ������������ ��������� ������ ���� ("�1");
	//    - ���� "�" = 1, ��� ������������� �� �������: "(������ - �2) / �3 * �1", �� �� ������ "�1";
	//    - ���� ������ ���������� ������ �2*�4, �2 ������������� � �4 ���.
	double lot = L1;
	if ( L == 1 ) lot = MathMax( (AccountEquity() - L2) / L3 * L1, L1 );

	double MinLot	= MarketInfo( _Symbol, MODE_MINLOT );
	double MaxLot	= MarketInfo( _Symbol, MODE_MAXLOT );
	double LotStep	= MarketInfo( _Symbol, MODE_LOTSTEP );
	lot = NormalizeDouble( lot / LotStep, 0 ) * LotStep;
	if ( lot < MinLot ) lot = MinLot;
	if ( lot > MaxLot ) lot = MaxLot;

	int res = _OrderSend ( _Symbol, OP_BUY, lot, _OpenPriceLevel, Slippage, _StopLossLevel, _TakeProfitLevel, "", _MagicNumber );
	if ( res < 0 )
	{
		Alert( strComment, ": ������ ��� �������� BUY-�������!!!" );
	}
	else
	{
		SummSignal = 0;
		last_trade = Time[0];
	}
	return( res );
}

int OpenSell()
{
	if ( Time[0] <= last_trade ) return(0);

	double _OpenPriceLevel, _StopLossLevel, _TakeProfitLevel;
	_OpenPriceLevel = NormalizeDouble( Bid, _Digits );

	//  - �� � ��:
	//    - ���� "�" = 0, �� � �� ����� ����� ��������������� ������� ����������;
	//    - ���� "�" = 1, �� � �� ������������� �� �������: "��������� ��(��) * �������� ������ * �1";
	double SLTP_k = 1.0;
	if ( D == 1 ) SLTP_k = -SummSignal * D1;

	if ( StopLoss > 0 )
	{ _StopLossLevel = NormalizeDouble( _OpenPriceLevel + StopLoss*SLTP_k*_Point, _Digits ); }
	else
	{ _StopLossLevel = 0.0; }

	if ( TakeProfit > 0 )
	{ _TakeProfitLevel = NormalizeDouble( _OpenPriceLevel - TakeProfit*SLTP_k*_Point, _Digits ); }
	else
	{ _TakeProfitLevel = 0.0; }

	//  - ������ ����:
	//    - ���� "�" = 0, ������������ ��������� ������ ���� ("�1");
	//    - ���� "�" = 1, ��� ������������� �� �������: "(������ - �2) / �3 * �1", �� �� ������ "�1";
	//    - ���� ������ ���������� ������ �2*�4, �2 ������������� � �4 ���.
	double lot = L1;
	if ( L == 1 ) lot = MathMax( (AccountEquity() - L2) / L3 * L1, L1 );

	double MinLot	= MarketInfo( _Symbol, MODE_MINLOT );
	double MaxLot	= MarketInfo( _Symbol, MODE_MAXLOT );
	double LotStep	= MarketInfo( _Symbol, MODE_LOTSTEP );
	lot = NormalizeDouble( lot / LotStep, 0 ) * LotStep;
	if ( lot < MinLot ) lot = MinLot;
	if ( lot > MaxLot ) lot = MaxLot;

	int res = _OrderSend ( _Symbol, OP_SELL, lot, _OpenPriceLevel, Slippage, _StopLossLevel, _TakeProfitLevel, "", _MagicNumber );
	if ( res < 0 )
	{
		Alert( strComment, ": ������ ��� �������� SELL-�������!!!" );
	}
	else
	{
		SummSignal = 0;
		last_trade = Time[0];
	}
	return( res );
}

