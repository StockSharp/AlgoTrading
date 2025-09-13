//+------------------------------------------------------------------+
//|														 trade_lib&info_lib.mqh |
//|                                      Copyright � 2005, komposter |
//|                                      mailto:komposterius@mail.ru |
//+------------------------------------------------------------------+
//#property copyright "Copyright � 2005, komposter"
//#property link      "mailto:komposterius@mail.ru"
/*
���� ����������:
 - ��������� ��������
 - ����� ����� ���������

//+------------------------------------------------------------------+
//| ����������� �� � ����� (������)
//+------------------------------------------------------------------+
// ����� ��������� init(), deinit() � start():
int AllowedAccounts[] = { 0 };

	// � ������ ������� init() - ���������, � ������ ������� deinit() � start() - ��� ������ Alert
	bool IsAllowedAccount = false;
	for ( int curAcc = ArraySize( AllowedAccounts ) - 1; curAcc >= 0; curAcc -- )
	{
		if ( AllowedAccounts[curAcc] == AccountNumber() || AllowedAccounts[curAcc] == 0 )
		{
			IsAllowedAccount = true;
			break;
		}
	}
	if ( !IsAllowedAccount )
	{
		Alert( "������ �� ���� ����� ���������!" );
		return(-1);
	}
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//| ����������� �� �������
//+------------------------------------------------------------------+
// ����� ��������� init(), deinit() � start():
datetime ExpirationTime = D'2008.01.20';

	// � ���������� - � ������ ������� init()
	if ( ExpirationTime > 0 && ( TimeCurrent() > ExpirationTime || TimeLocal() > ExpirationTime ) )
	{ Alert( "Indicator ERROR!\n\n��� ����-������ ����������!\n���� ������ ���������� " + TimeToStr( ExpirationTime ) + "!\n\n��� ������ ����������� ��������� �� ����:         \nkomposterius@mail.ru" ); return(-1); }

	// � �������� - � ������ ������� init()
	if ( ExpirationTime > 0 && ( TimeCurrent() > ExpirationTime || TimeLocal() > ExpirationTime ) )
	{ Alert( "Expert ERROR!\r\n\r\n��� ����-������ ��������!\r\n���� ������ ���������� " + TimeToStr( ExpirationTime ) + "!\r\n\r\n��� ������ ����������� ��������� �� ����:         \r\nkomposterius@mail.ru" ); return(-1); }

	// � ���������� � �������� - � ������ ������� deinit() � start()
	if ( ExpirationTime > 0 && ( TimeCurrent() > ExpirationTime || TimeLocal() > ExpirationTime ) ) { return(-1); }
//+------------------------------------------------------------------+


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
������ ������� ��������� �� MQL4:
#import  "kernel32.dll" 
int      WinExec(string lpCmdLine, int uCmdShow);

WinExec("F:\PhoneCall.exe", 1);
Slawa - ����������:
WinExec("F:\\PhoneCall.exe", 1);

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
����������� �����:
#import "kernel32.dll"
   int CopyFileA(string FromFileName,string ToFileName,int FailIfExists);
#import
 
int start(){
   int rv=CopyFileA("C:\\1.txt","C:\\2.txt",0);

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
�������� ��������� � �������.
#include <WinUser32.mqh>
#define DESTROY 33050

void Destroy()
{
  PostMessageA(WindowHandle(Symbol(), Period()), WM_COMMAND, DESTROY, 1);
 
  return;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
http://www.metaquotes.ru/forum/6907/page2
��� ���� ������ ���� � ������������. ����������� � �������� ������ ���� ������ �� ����� ������� ���� ������������ ���.
������� ���������� ������������ �� �������� ������� � ���������:

ERR_TRADE_TIMEOUT - �����/������ �� ��������, ����� ���������� ��������� ������ ����� ��������� ����� (��������, ������, � �� ����� 5 ���)
ERR_TOO_FREQUENT_REQUESTS ��� ERR_TOO_MANY_REQUESTS - ������� ������ ������� �� ������, ���� ��������� ������� ��������, ��� ����� ��������� �� ������ � ������ ��������/���������
ERR_INVALID_PRICE - ������������ ���� bid/ask, �������� ��-�� ����, ��� ������� �������� �� ���������� �������� ���������� ����� RefreshRates ����� ��������. ���������� ��������� �� ������������ ������ � ��������. ����� ����� ������ ����������� ���� ������������� �������� � ����������� � ����.
ERR_INVALID_STOPS - ������� ������� ����� ��� ���������� ������������ ���� � ������ (take profit, stop loss ��� open price � ���������� �������), ����������� ������ ��������� �������� �������, ���� ������ ��� 100% ��������, ��� ��� ��������� ��-�� ����������� ����.
ERR_INVALID_TRADE_VOLUME - ������ � ���������� �������, �� � ���� ������ ������ ��������� ������
ERR_MARKET_CLOSED - ����� ������, ����� ����������� ������, �� ������ ����� ���������� ������� ���� (��������� �����)
ERR_TRADE_DISABLED - �������� �� ����������� ��������� ���������, ��������� ������ ������ �� � ���� ������.
ERR_NOT_ENOUGH_MONEY - ����� �� �������, ��������� ������ � ���� �� ����������� ������������� ������. ����� ���������, �������� �����, �� ���� ���� ��������� � ������������� ������� � ���������� ���������� ������.
ERR_PRICE_CHANGED ��� ERR_REQUOTE - ������ - ���� ����������, ����� ����� �������� �������� ��������� � ����������� ������, ����� ���� ��� ��������.
ERR_OFF_QUOTES ��� ERR_BROKER_BUSY - ����� �� �����-�� ������� (��������, � ������ ������ ��� ���, �� �������������� ����, fast market) �� ��� ��� ��� �������. ����� ����� ��������� ������ ����� ��������� ������ ������� (�� 5 ���) �� ����������� �������� ���������
ERR_ORDER_LOCKED - ����� ������������ � ��� ��������������, ������ �� ����� ������ � ������ �������� ��� � ����� ��������� MT4, ����� ������ �� ���������, � �����
ERR_LONG_POSITIONS_ONLY_ALLOWED - ��������� ������ �������, ��������� sell �� � ���� ������ ������
ERR_TRADE_MODIFY_DENIED - ����������� ���������, ��� ��� ����� ������� ������ � ����� � ����������, ����� ����������� ����� ��������� ���������� ������� (������ ����� 10-15, �� �� � ���� ������ �� �����)
ERR_TRADE_CONTEXT_BUSY - �������� ����� �����, ���������� ������������ IsTradeAllowed(), ���� ��������� ���������� ������� � ������ ��������� ������
ERR_TRADE_EXPIRATION_DENIED - ��������� ������������ ���� expiration � ���������� �������, ��������� �������� ����� ������ ���� ������ expiration.

��� ������������ ���������� ����������, ���� ����� ��������� - �����������, ����������.
*/

extern string 	Trade_Properties 		= "--------Trade-Properties-------";
extern int 		Slippage 				= 5;
extern int 		PauseBeforeTrade		= 5;
extern int 		MaxWaitingTime			= 300;
extern color 	OrderBuyColor 			= Lime;
extern color 	OrderSellColor 		= Red;

extern string 	Allow_Flags		 		= "-----------Allow-Flags---------";
extern bool		Allow_Info				= true;
extern bool		Allow_LogFile			= true;
extern bool		Allow_TradeLogFile	= true;
extern bool		Allow_ErrorMail		= true;
extern bool		Allow_ErrorLogFile	= true;

extern string 	Info_Properties 		= "--------Info-Properties--------";
extern bool		EnglishInfo				= false;
extern int		Font_Size_Variant		= 4; //�� 1 �� 10
extern color 	Standart_Color 		= White;
extern color 	Warning_Color 			= Magenta;
extern color 	Price_Up_Color 		= Lime;
extern color 	Price_Down_Color 		= Red;

string	strComment, strPeriod, TradeInfoLib_Font = "Arial";
double	TradeInfoLib_FontSize, TradeInfoLib_HeadFontSize;

string	_Symbol, ExpertName;
int		_Period, _MagicNumber, _Digits;
double	_Point, _StopLevel, _Spread;
datetime StartTime;

/////////////////////////////////////////////////////////////////////////////////
string stringServer						= "������ ";
string stringReal							= "�������� �ר� #";
string stringDemo							= "����-���� #";
string stringTradeAllow					= "�������� ���������!";
string stringTradeNotAllow				= "�������� ���������";
string stringLoaded						= "������� ������� ��������...";

string stringOpenedPosition			= "���� �������, �������� ���������:";
string stringOpenedOrder				= "���� �����, ������������� ���������:";
string stringPointBefore				= "������� �� ������������ - ";

string stringGlobalStop					= "������� ����������!";
string stringGlobalStop1				= "���������� ����������";
string stringGlobalStop2				= "������ ���� >= 0!";
string stringNoBars						= "������� ���� ����� �� �������!";

string stringInvalidParameters		= "Invalid trade parameters";///////////////////////////////
string stringInvalidOrderType			= "Invalid OrderType";//////////////////////////////////////
string stringInvalidVolume				= " - ������������ �����!!!";
string stringIncorrectAbove			= " - ����������� (����";
string stringIncorrectBelow			= " - ����������� (����";
string stringIncorrectTooBeside		= " - ����������� (������� ������ �";
string stringInvalidExpiration		= " - ��� ������-������ ������ ���������� ����� ���������!!!";
string stringIncorrectExpiration		= " - ����� ��������� ������ ���������� � �������!!!";

string stringSendingOrder				= "������������� ";
string stringSendingOrder1				= "-�����...";
string stringSendingPosition			= "��������� ";
string stringSendingPosition1			= " �������...";

string stringCheck						= "��������� ���������.....";
string stringCheckError					= "��������� ���������.....������!";
string stringCheckOK						= "��������� ���������.....��� ���������...";

string stringNoConnection				= "��� ���������� � ��������";
string stringStopped						= "������� ���������� �������������";
string stringTimeOut						= "����� �������� �������";
string stringSuccessfully				= "�������...";
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
/**/ void TradeInfoLib_Initialization ( int TradeInfoLib_ExpertId, string TradeInfoLib_ExpertName )
/////////////////////////////////////////////////////////////////////////////////
// ������������� ����������, �������� �������� ��� ������ ����������,
// ������ � ��� ���������� �� �������������.
// ��������� ����������� � ������� ( ������� ����� ������� � ������� �-��� _OrderSend )
//
// ���������� ������� �-��� �� init() ��������
/////////////////////////////////////////////////////////////////////////////////
{

		/////////////////////////////////////////////////////////////////////////////////
		if ( EnglishInfo )
		{
				stringServer						= "Server ";
				stringReal							= "REAL ACCOUNT #";
				stringDemo							= "Demo account #";
				stringTradeAllow					= "TRADE IS ALLOWED!";
				stringTradeNotAllow				= "Trade is not allowed";
				stringLoaded						= "Expert loaded successfully...";

				stringOpenedPosition				= " - position opened by this expert:";
				stringOpenedOrder					= " - order opened by this expert:";
				stringPointBefore					= "������� �� ������������ - ";///////////////////////////////

				stringGlobalStop					= "Expert was stopped!";
				stringGlobalStop1					= "Global variable";
				stringGlobalStop2					= "must be >= 0!";
				stringNoBars						= "������� ���� ����� �� �������!";///////////////////////////

				stringInvalidParameters			= "Invalid trade parameters";
				stringInvalidOrderType			= "Invalid OrderType";
				stringInvalidVolume				= " - invalid volume!!!";
				stringIncorrectAbove				= " - Incorrect (above";
				stringIncorrectBelow				= " - Incorrect (below";
				stringIncorrectTooBeside		= " - Incorrect (too beside to";
				stringInvalidExpiration			= " - ��� ������-������ ������ ���������� ����� ���������!!!";//////
				stringIncorrectExpiration		= " - ����� ��������� ������ ���������� � �������!!!";/////////////

				stringSendingOrder				= "Sending ";
				stringSendingOrder1				= "-order...";
				stringSendingPosition			= "Sending ";
				stringSendingPosition1			= " position...";

				stringCheck							= "Checking.....";
				stringCheckError					= "Checking.....Error!";
				stringCheckOK						= "Checking.....OK...";

				stringNoConnection				= "No connection with trade server";
				stringStopped						= "Expert was stopped by user";
				stringTimeOut						= "Limit of waiting time";
				stringSuccessfully				= "Successfully...";
		}
		/////////////////////////////////////////////////////////////////////////////////
//		if (false) { _MagicNumber(1,"",1); _OrderSend("",1,1.1,1.1,1,1.1,1.1); _OrderModify(1,1.1,1.1,1.1,0); _OrderClose(1,1.1,1.1,1); _OrderDelete(1); _Reverse(1); _TrailingStop(1,1); intPeriod(1); }
	int _GetLastError;

//---- ��� ������������ ��� "����������" ���������
	if ( IsTesting() )
	{
		Allow_Info				= false;
		Allow_LogFile			= false;
		Allow_TradeLogFile	= false;
		Allow_ErrorMail		= false;
		Allow_ErrorLogFile	= false;
	}

//---- �������������� ���������� ���������� ���������� (����������� ��� �-���)
	StartTime	= 0;
	_Symbol		= Symbol();
	_Period		= Period();
	_Digits		= MarketInfo( _Symbol, MODE_DIGITS );
	_Point		= MarketInfo( _Symbol, MODE_POINT );
	_StopLevel	= NormalizeDouble ( ( MarketInfo( _Symbol, MODE_STOPLEVEL ) + 1 ) * _Point, _Digits );
	_Spread		= NormalizeDouble ( MarketInfo ( _Symbol, MODE_SPREAD ) * _Point, _Digits );
	strPeriod	= strPeriod( _Period );
	_MagicNumber= _MagicNumber( TradeInfoLib_ExpertId, _Period );


	if ( TradeInfoLib_ExpertName == "" )
		ExpertName = WindowExpertName();
	else
		ExpertName = TradeInfoLib_ExpertName;

	strComment	= ExpertName + " (" + _Symbol + ", " + strPeriod + ")";
//---- ������ �������� ������ ���������� ����������
	while ( !IsStopped() )
	{
		if ( GlobalVariableSet( strComment + "-return!", 0.0 ) > 0 ) { break; }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{ Print( "trade_lib&info_lib - TradeInfoLib_Initialization( ", TradeInfoLib_ExpertId, ", ", TradeInfoLib_ExpertName, " ) - GlobalVariableSet ( \"", strComment, "-return!\", 0.0 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" ); }

		Sleep(100);
	}
	
//---- ������������� ���������� ���������� TradeIsBusy � 0
	TradeIsNotBusy();

//---- ������ ������� ��� ������ ����������
	int v_shag = 16;
	int h_shag = 350;
	switch ( Font_Size_Variant )
	{
		case 1:	TradeInfoLib_FontSize = 5;		TradeInfoLib_HeadFontSize = 5;	v_shag = 8;		h_shag = 180; break;
		case 2:	TradeInfoLib_FontSize = 6;		TradeInfoLib_HeadFontSize = 7;	v_shag = 9;		h_shag = 200; break;
		case 3:	TradeInfoLib_FontSize = 7;		TradeInfoLib_HeadFontSize = 8;	v_shag = 11;	h_shag = 230; break;
		case 4:	TradeInfoLib_FontSize = 8;		TradeInfoLib_HeadFontSize = 9;	v_shag = 13;	h_shag = 270; break;
		case 5:	TradeInfoLib_FontSize = 9;		TradeInfoLib_HeadFontSize = 11;	v_shag = 15;	h_shag = 310; break;
		case 6:	TradeInfoLib_FontSize = 10;	TradeInfoLib_HeadFontSize = 12;	v_shag = 16;	h_shag = 350; break;
		case 7:	TradeInfoLib_FontSize = 11;	TradeInfoLib_HeadFontSize = 13;	v_shag = 18;	h_shag = 390; break;
		case 8:	TradeInfoLib_FontSize = 12;	TradeInfoLib_HeadFontSize = 14;	v_shag = 20;	h_shag = 430; break;
		case 9:	TradeInfoLib_FontSize = 13;	TradeInfoLib_HeadFontSize = 15;	v_shag = 22;	h_shag = 480; break;
		case 10:	TradeInfoLib_FontSize = 14;	TradeInfoLib_HeadFontSize = 16;	v_shag = 24;	h_shag = 530; break;
		default:	TradeInfoLib_FontSize = 10;	TradeInfoLib_HeadFontSize = 12;	v_shag = 16;	h_shag = 350; break;
	}

	_LabelCreate ( "ExpertLog_00", 4, 15				);
	_LabelCreate ( "ExpertLog_01", 4, 15 + v_shag	);
	_LabelCreate ( "ExpertLog_02", 4, 15 + v_shag*2 );
	_LabelCreate ( "ExpertLog_03", 4, 15 + v_shag*3 );
	_LabelCreate ( "ExpertLog_04", 4, 15 + v_shag*4 );

	_LabelCreate ( "ExpertLog_10", h_shag, 15					);
	_LabelCreate ( "ExpertLog_11", h_shag, 15 + v_shag		);
	_LabelCreate ( "ExpertLog_12", h_shag, 15 + v_shag*2	);
	_LabelCreate ( "ExpertLog_13", h_shag, 15 + v_shag*3	);
	_LabelCreate ( "ExpertLog_14", h_shag, 15 + v_shag*4	);

//---- ������� ���������� �� ������������� � ���������� � � ����
	string	AccountStatus	= stringReal;
	int		AccountColor	= 1;
	if ( IsDemo() )
	{
		AccountStatus	= stringDemo;
		AccountColor	= 0;
	}
	string	TradeStatus	= stringTradeAllow;
	int		TradeColor	= 1;
	if ( !IsTradeAllowed() )
	{
		TradeStatus	= stringTradeNotAllow;
		TradeColor	= 0;
	}
	clear_info ();
	_FileWrite	( 		"\n"																									);
	_FileWrite	( 		"++---------Initialization---------++"														);
	_info			( 1,	stringServer + ServerAddress()											, 0				);
	_info			( 2,	AccountStatus + AccountNumber() + " ( " + AccountName() + " )"	, AccountColor );
	_info			( 3,	TradeStatus																		, TradeColor	);
	_info			( 4,	stringLoaded																	, 0				);
	_FileWrite	( 		"++--------------------------------++\n"													);

//---- ���� �������, ���� ��� ����� ���������� ����� ���� ��������� =)
	Sleep(1000);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ bool IsOK()
/////////////////////////////////////////////////////////////////////////////////
// �������� ������ ���������� ���������� ��������, ���������� ����� �� ������� � ���������� ���������
// � ������ �������� �������� ���������� true, ����� - false. ��� ������������ ���������� true.
// ������������� � ������ IsOK() == false ���������� ������ ��������.
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(true); }

	_StopLevel	= NormalizeDouble ( ( MarketInfo( _Symbol, MODE_STOPLEVEL ) + 1 ) * _Point, _Digits );
	_Spread		= NormalizeDouble ( MarketInfo ( _Symbol, MODE_SPREAD ) * _Point, _Digits );

	clear_info();

	if ( GlobalVariableGet ( strComment + "-return!" ) < 0 )
	{
		_info( 1, stringGlobalStop, 1 );
		_info( 2, stringGlobalStop1 + "\"" + strComment + "-return!\" " + stringGlobalStop2, 1 );
		return(false);
	}
	if ( Bars < 100 )
	{
		_info( 1, stringNoBars, 1 );
		return(false);
	}
/*	if ( MarketInfo( _Symbol, MODE_TRADEALLOWED ) == false )
	{
		_info( 1, stringTradeNotAllow, 1 );
		return(false);
	}
	if ( TimeLocal() < MarketInfo( _Symbol, MODE_STARTING ) || TimeLocal() > MarketInfo( _Symbol, MODE_EXPIRATION ) )
	{
		_info( 1, "�� ����� ���������!", 1 );
		return(false);
	}
*/
return(true);
}
/////////////////////////////////////////////////////////////////////////////////
/**/ void TradeInfoLib_Deinitialization()
/////////////////////////////////////////////////////////////////////////////////
// �������� ��������, �������� �����������. ��� �������� ���������� ���������� true, ����� - false.
//
// ���������� �������� � deinit() ��������.
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;
	
//---- ������� ������ ���������� ���������� ��������
	if( !GlobalVariableDel( strComment + "-return!" ) )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - TradeInfoLib_Deinitialization() - GlobalVariableDel ( \"", strComment, "-return!\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}

	if ( !Allow_Info ) { return(0); }

//---- � ��� ������� ��� ������ ����������
	if ( !ObjectDelete ( "ExpertLog_00" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "00", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_01" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "01", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_02" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "02", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_03" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "03", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_04" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "04", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_10" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "10", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_11" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "11", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_12" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "12", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_13" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "13", _GetLastError );
	}
	if ( !ObjectDelete ( "ExpertLog_14" ) )
	{
		_GetLastError = GetLastError();
		TradeInfoLib_Deinit_print( "14", _GetLastError );
	}
}
void TradeInfoLib_Deinit_print ( string ExpertLog_Number, int Error )
{
	Print( "trade_lib&info_lib - TradeInfoLib_Deinitialization() - ObjectDelete( \"ExpertLog_", ExpertLog_Number, "\" ) - Error #", Error, " ( ", ErrorDescription( Error ), " )" );
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _info ( int LogNumber, string Text = "", int ColorVariant = 0, bool File_Log = true )
/////////////////////////////////////////////////////////////////////////////////
// ����� ���������� �� �����, ������ � �� � ���-���� �������� � � ���-���� ��.
//
// LogNumber - ����� ������ �� ������, � ������� ����� �������� ����������:
// �� 1 �� 4 (4 ������ �� ����� �������� ������) � �� 10 �� 14 (5 ����� - �� ������) 
//
// Text - ����������, �����, ��������� �� ����� � ������������ � ���-�����
//
// ColorVariant - ���� ������ ���������� �� �����: 0 - Standart_Color, 1 - Warning_Color,
// 2 - Price_Up_Color, 3 - Price_Down_Color. ��� 4 ����� ���������������� ��������� �����������.
//
// File_Log - ����, ����������� (�����������) ������ ���������� � ���-���� ��������:
// 1(true) - ������ ��������� (�� ���������), 0(false) - ������ ���������.
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;

	if ( Allow_Info )
	{
		string _infoLabelName;
		double _infoFontSize = TradeInfoLib_FontSize;
		switch ( LogNumber )
		{
			case 0:  _infoLabelName = "ExpertLog_00";	_infoFontSize = TradeInfoLib_HeadFontSize;	break;
			case 1:  _infoLabelName = "ExpertLog_01";																break;
			case 2:  _infoLabelName = "ExpertLog_02";																break;
			case 3:  _infoLabelName = "ExpertLog_03";																break;
			case 4:  _infoLabelName = "ExpertLog_04";																break;
			case 10: _infoLabelName = "ExpertLog_10";	_infoFontSize = TradeInfoLib_HeadFontSize;	break;
			case 11: _infoLabelName = "ExpertLog_11";																break;
			case 12: _infoLabelName = "ExpertLog_12";																break;
			case 13: _infoLabelName = "ExpertLog_13";																break;
			case 14: _infoLabelName = "ExpertLog_14";																break;
			default: _infoLabelName = "ExpertLog_01";																break;
		}
		color _infoColor;
		switch ( ColorVariant )
		{
			case 1:	_infoColor = Warning_Color;		break;
			case 2:	_infoColor = Price_Up_Color;		break;
			case 3:	_infoColor = Price_Down_Color;	break;
			default:	_infoColor = Standart_Color;		break;
		}

		if ( !ObjectSetText( "ExpertLog_00", "Expert log ( " + TimeToStr( TimeLocal(), TIME_SECONDS) + " )", TradeInfoLib_HeadFontSize, TradeInfoLib_Font, Standart_Color ) )
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _info( ", LogNumber, ", \"", Text, "\", ", ColorVariant, ", ", File_Log, " ) - ObjectSetText( \"ExpertLog_00\", \"Expert log ( ", TimeToStr( TimeLocal(), TIME_SECONDS), " )\", ", TradeInfoLib_HeadFontSize, ", ", TradeInfoLib_Font, ", ", Standart_Color, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		}

		if ( !ObjectSetText( _infoLabelName, Text, _infoFontSize, TradeInfoLib_Font, _infoColor) )
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _info( ", LogNumber, ", \"", Text, "\", ", ColorVariant, ", ", File_Log, " ) - ObjectSetText( \"", _infoLabelName,"\", \"", Text, "\", ", _infoFontSize, ", ", TradeInfoLib_Font, ", ", _infoColor, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		}
	}

	if ( Text != "" && File_Log ) { _FileWrite ( Text ); }
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void clear_info ()
/////////////////////////////////////////////////////////////////////////////////
// ������� ������ �� ����������, ���������� ����������� �� �����.
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;

	if ( !Allow_Info ) { return(0); }

	if ( !ObjectSetText ( "ExpertLog_00", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "00", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_01", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "01", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_02", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "02", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_03", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "03", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_04", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "04", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_10", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "10", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_11", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "11", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_12", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "12", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_13", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "13", _GetLastError ); }
	if ( !ObjectSetText ( "ExpertLog_14", "", 10 ) ) { _GetLastError = GetLastError(); clear_info_print ( "14", _GetLastError ); }
}
void clear_info_print ( string ExpertLog_Number, int Error )
{ Print( "trade_lib&info_lib - clear_info() - ObjectSetText( \"ExpertLog_", ExpertLog_Number, " \", \"\", 10 ) - Error #", Error, " ( ", ErrorDescription( Error ), " )" ); }

/////////////////////////////////////////////////////////////////////////////////
/**/ void _LabelCreate ( string _LabelName, int _LabelXDistance, int _LabelYDistance, int _LabelCorner = 0 )
/////////////////////////////////////////////////////////////////////////////////
// ��������� �-����
// �������� ������� "��������� �����" � ������ _LabelName.
// ����������: � = _LabelXDistance, � = _LabelYDistance, ���� - _LabelCorner.
/////////////////////////////////////////////////////////////////////////////////
{
	if ( !Allow_Info ) { return(false); }

	int _GetLastError;

	if ( !ObjectCreate( _LabelName, OBJ_LABEL, 0, 0, 0 ) )
	{
		_GetLastError = GetLastError();
		if ( _GetLastError != 4200 )
		{
			Print( "trade_lib&info_lib - _LabelCreate( \"", _LabelName, "\", ", _LabelXDistance, ", ", _LabelYDistance, ", ", _LabelCorner ," ) - ObjectCreate( \"", _LabelName, "\", OBJ_LABEL,0,0,0 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			return(-1);
		}
	}
	if ( !ObjectSet( _LabelName, OBJPROP_CORNER, _LabelCorner ) )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _LabelCreate( \"", _LabelName, "\", ", _LabelXDistance, ", ", _LabelYDistance, ", ", _LabelCorner ," ) - ObjectSet( \"", _LabelName, "\", OBJPROP_CORNER, ", _LabelCorner, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
	if ( !ObjectSet( _LabelName, OBJPROP_XDISTANCE, _LabelXDistance ) )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _LabelCreate( \"", _LabelName, "\", ", _LabelXDistance, ", ", _LabelYDistance, ", ", _LabelCorner ," ) - ObjectSet( \"", _LabelName, "\", OBJPROP_XDISTANCE, ", _LabelXDistance, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
	if ( !ObjectSet( _LabelName, OBJPROP_YDISTANCE, _LabelYDistance ) )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _LabelCreate( \"", _LabelName, "\", ", _LabelXDistance, ", ", _LabelYDistance, ", ", _LabelCorner ," ) - ObjectSet( \"", _LabelName, "\", OBJPROP_YDISTANCE, ", _LabelYDistance, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
	if ( !ObjectSetText ( _LabelName, "", 10 ) )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _LabelCreate( \"", _LabelName, "\", ", _LabelXDistance, ", ", _LabelYDistance, ", ", _LabelCorner ," ) - ObjectSetText( \"", _LabelName, "\", \"\", 10 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _FileWrite ( string text )
/////////////////////////////////////////////////////////////////////////////////
// ������ text � ���-����.
// (�\MetaTrader 4\experts\files\_ExpertLogs\��� ��������( ������, ������ )\���.�����.����.txt )
/////////////////////////////////////////////////////////////////////////////////
{
	if ( !Allow_LogFile ) { return(0); }

	int _GetLastError;

	string file_name = "_ExpertLogs\\" + strComment + "\\" + TimeToStr( TimeLocal(), TIME_DATE ) + ".txt";
	int file_handle = FileOpen ( file_name, FILE_READ | FILE_WRITE, " " );
	
	if ( file_handle > 0 )
	{
		if ( FileSeek ( file_handle, 0, SEEK_END ) )
		{
			if ( text != "\n" && text != "\r\n" ) { text = TimeToStr( TimeLocal(), TIME_SECONDS ) + " - - - " + text; }
			if ( FileWrite ( file_handle, text ) < 0 )
			{
				_GetLastError = GetLastError();
				Print( "trade_lib&info_lib - _FileWrite( \"" + text + "\" ) - FileWrite ( ", file_handle, ", ", text, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			}
		}
		else
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _FileWrite( \"" + text + "\" ) - FileSeek ( " + file_handle + ", 0, SEEK_END ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		}
		FileClose ( file_handle );
	}
	else
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _FileWrite( \"" + text + "\" ) - FileOpen( ", file_name, ", FILE_READ | FILE_WRITE, \" \" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _MagicNumber( int Expert_Id, int _Period )
/////////////////////////////////////////////////////////////////////////////////
// �-��� ���������� MagicNumber, ���������� ��� ������ Expert_Id - _Period.
// 
// ����� �������, ���� � ���������� Expert_Id ��������, ���������� �� ������ �� ����� ������������ ������ MagicNumber.
// ��������, ��� Expert_Id = 1230 � _Period = PERIOD_H1,  MagicNumber ����� 12305
// � ���� �� �������, �� �� ������� � �������� PERIOD_W1 ����� ����� MagicNumber = 12308
/////////////////////////////////////////////////////////////////////////////////
{
	return( Expert_Id );

	// ������ ����� �������:
	int Period_Id = 0;
	switch ( _Period )
	{
		case PERIOD_MN1: Period_Id = 9; break;
		case PERIOD_W1:  Period_Id = 8; break;
		case PERIOD_D1:  Period_Id = 7; break;
		case PERIOD_H4:  Period_Id = 6; break;
		case PERIOD_H1:  Period_Id = 5; break;
		case PERIOD_M30: Period_Id = 4; break;
		case PERIOD_M15: Period_Id = 3; break;
		case PERIOD_M5:  Period_Id = 2; break;
		case PERIOD_M1:  Period_Id = 1; break;
		default: Print( "trade_lib&info_lib - _MagicNumber( ", Expert_Id, ", ", _Period, " ) - Invalid Period!" );
	}
	return(Expert_Id * 10 + Period_Id);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ bool _IsExpertOrder ( int _MagicNumber )
/////////////////////////////////////////////////////////////////////////////////
// ����� �������/������ � �������� _MagicNumber � � ������� ��������.
// ���� ����� ����, ������� ���������� � ���������� true, ���� ��� - ���������� false.
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;

	int _OrdersTotal = OrdersTotal();
	for ( int z = _OrdersTotal - 1; z >= 0; z -- )
	{
		if ( !OrderSelect( z, SELECT_BY_POS ) )
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _IsExpertOrder( ", _MagicNumber, " ) - OrderSelect( ", z, ", SELECT_BY_POS ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}
		if ( OrderMagicNumber() == _MagicNumber && OrderSymbol() == _Symbol )
		{
			if ( !Allow_Info ) { return(true); }

			int		_OrderTicket			= OrderTicket();
			int		_OrderType				= OrderType();
			string	_OrderSymbol			= OrderSymbol();
			double	_OrderLots				= NormalizeDouble( OrderLots(), 2 );
			double	_OrderOpenPrice		= NormalizeDouble( OrderOpenPrice(), _Digits );
			double	_OrderStopLoss			= NormalizeDouble( OrderStopLoss(), _Digits );
			double	_OrderProfit			= OrderProfit();

			_info ( 10, "" );
			_info ( 11, "" );
			_info ( 12, "" );
			_info ( 13, "" );
			_info ( 14, "" );

			if ( _OrderType <= OP_SELL )
			{
				double ProfitLoss = _OrderProfit + OrderSwap() + OrderCommission();
				double Point_Cost = LotCost( _OrderSymbol ) * _OrderLots;

				int _profit_color = 0;
				if ( ProfitLoss > 0 ) { _profit_color = 2; }
				if ( ProfitLoss < 0 ) { _profit_color = 3; }

				double ProfitLossPoints = MathAbs( _OrderProfit / Point_Cost );

				double MaxLossPoints;
				if ( _OrderType == OP_BUY )
				{ MaxLossPoints = ( _OrderStopLoss - _OrderOpenPrice ) / _Point; }
				else
				{ MaxLossPoints = ( _OrderOpenPrice - _OrderStopLoss ) / _Point; }
				double MaxLoss = MaxLossPoints * Point_Cost + OrderSwap() + OrderCommission();

				string ifNoStopLoss = DoubleToStr( MaxLoss, 2 ) + " " + AccountCurrency() + " (" + DoubleToStr( MathAbs( MaxLossPoints ), 0 ) + " points)";
				int _maxloss_color = 0;
				if ( MaxLossPoints > 0 ) { _maxloss_color = 2; }
				if ( MaxLossPoints < 0 ) { _maxloss_color = 3; }
				if ( _OrderStopLoss <= 0 ) { _maxloss_color = 1; ifNoStopLoss = "���� ���� �� ����������!"; }
				
				_info ( 1, stringOpenedPosition, 0, 0 );
				_info ( 2, "#" + _OrderTicket + " - " + strOrderType ( _OrderType )	+ ",  " + DoubleToStr( _OrderLots, 2 ) + " lot(s)", 0, 0 );
				_info ( 3, "�������/������:  " + DoubleToStr( ProfitLoss, 2 ) + " " + AccountCurrency() + " (" + DoubleToStr( ProfitLossPoints, 0 ) + " points)"		, _profit_color	, 0 );
				_info ( 4, "�� ���������:  " + " " + " " + " " + " " + ifNoStopLoss, _maxloss_color	, 0 );
			}
			else
			{
				int		_before_color	= 0;
				double	Price				= NormalizeDouble( MarketInfo( _OrderSymbol, MODE_BID ), _Digits );
				string	tmp_str			= ", Bid = " + DoubleToStr( Price, _Digits );
				if ( _OrderType == OP_BUYLIMIT || _OrderType == OP_BUYSTOP )
				{
					Price = NormalizeDouble( MarketInfo( _OrderSymbol, MODE_ASK ), _Digits );
					tmp_str = ", Ask = " + DoubleToStr( Price, _Digits );
				}

				if ( MathAbs( _OrderOpenPrice - Price ) / _Point <= 10 ) { _before_color = 1; }
			
				_info ( 1, _OrderSymbol + stringOpenedOrder																	, 0				, 0 );	
				_info ( 2, "� " + _OrderTicket + " - " + strOrderType ( _OrderType )								, 0				, 0 );
				_info ( 3, "Open Price = " + DoubleToStr( _OrderOpenPrice, _Digits )  + tmp_str					, 0				, 0 );
				_info	( 4, stringPointBefore + DoubleToStr( MathAbs( _OrderOpenPrice - Price ) / _Point, 0 ) , _before_color, 0 );
			}
			return(true);
		}
	}
return(false);
}

int _ExpertOrders = 0; int _MarketOrders = 0; int _PendingOrders = 0;
int _BuyTicket = 0; double _BuyLots = 0.0; double _BuyOpenPrice = 0.0; double _BuyStopLoss = 0.0; double _BuyTakeProfit = 0.0; datetime _BuyOpenTime = -1; double _BuyProfit = 0.0; double _BuySwap = 0.0; double _BuyCommission = 0.0; string _BuyComment = ""; 
int _SellTicket = 0; double _SellLots = 0.0; double _SellOpenPrice = 0.0; double _SellStopLoss = 0.0; double _SellTakeProfit = 0.0; datetime _SellOpenTime = -1; double _SellProfit = 0.0; double _SellSwap = 0.0; double _SellCommission = 0.0; string _SellComment = ""; 
int _BuyStopTicket = 0; double _BuyStopLots = 0.0; double _BuyStopOpenPrice = 0.0; double _BuyStopStopLoss = 0.0; double _BuyStopTakeProfit = 0.0; datetime _BuyStopOpenTime = -1; string _BuyStopComment = ""; datetime _BuyStopExpiration = -1;
int _SellStopTicket = 0; double _SellStopLots = 0.0; double _SellStopOpenPrice = 0.0; double _SellStopStopLoss = 0.0; double _SellStopTakeProfit = 0.0; datetime _SellStopOpenTime = -1; string _SellStopComment = ""; datetime _SellStopExpiration = -1;
int _BuyLimitTicket = 0; double _BuyLimitLots = 0.0; double _BuyLimitOpenPrice = 0.0; double _BuyLimitStopLoss = 0.0; double _BuyLimitTakeProfit = 0.0; datetime _BuyLimitOpenTime = -1; string _BuyLimitComment = ""; datetime _BuyLimitExpiration = -1;
int _SellLimitTicket = 0; double _SellLimitLots = 0.0; double _SellLimitOpenPrice = 0.0; double _SellLimitStopLoss = 0.0; double _SellLimitTakeProfit = 0.0; datetime _SellLimitOpenTime = -1; string _SellLimitComment = ""; datetime _SellLimitExpiration = -1;

/////////////////////////////////////////////////////////////////////////////////
/**/ void _ExpertOrdersInit( int _MagicNumber )
/////////////////////////////////////////////////////////////////////////////////
// ����� ������������, ���� ������� ������������� �������� ���� ����� ������ ���� (OP_BUY, OP_SELL, � �.�.)
// ����������� �������� ����������, ������������� ����. �� ����� ������������ �� �������� ��� ��������� ����.
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;

	_ExpertOrders = 0; _MarketOrders = 0; _PendingOrders = 0;
	_BuyTicket = 0; _BuyLots = 0.0; _BuyOpenPrice = 0.0; _BuyStopLoss = 0.0; _BuyTakeProfit = 0.0; _BuyOpenTime = -1; _BuyProfit = 0.0; _BuySwap = 0.0; _BuyCommission = 0.0; _BuyComment = "";
	_SellTicket = 0; _SellLots = 0.0; _SellOpenPrice = 0.0; _SellStopLoss = 0.0; _SellTakeProfit = 0.0; _SellOpenTime = -1; _SellProfit = 0.0; _SellSwap = 0.0; _SellCommission = 0.0; _SellComment = "";
	_BuyStopTicket = 0; _BuyStopLots = 0.0; _BuyStopOpenPrice = 0.0; _BuyStopStopLoss = 0.0; _BuyStopTakeProfit = 0.0; _BuyStopOpenTime = -1; _BuyStopComment = ""; _BuyStopExpiration = -1;
	_SellStopTicket = 0; _SellStopLots = 0.0; _SellStopOpenPrice = 0.0; _SellStopStopLoss = 0.0; _SellStopTakeProfit = 0.0; _SellStopOpenTime = -1; _SellStopComment = ""; _SellStopExpiration = -1;
	_BuyLimitTicket = 0; _BuyLimitLots = 0.0; _BuyLimitOpenPrice = 0.0; _BuyLimitStopLoss = 0.0; _BuyLimitTakeProfit = 0.0; _BuyLimitOpenTime = -1; _BuyLimitComment = ""; _BuyLimitExpiration = -1;
	_SellLimitTicket = 0; _SellLimitLots = 0.0; _SellLimitOpenPrice = 0.0; _SellLimitStopLoss = 0.0; _SellLimitTakeProfit = 0.0; _SellLimitOpenTime = -1; _SellLimitComment = ""; _SellLimitExpiration = -1;

	int _OrdersTotal = OrdersTotal();
	for ( int z = _OrdersTotal - 1; z >= 0; z -- )
	{
		if ( !OrderSelect( z, SELECT_BY_POS ) )
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _ExpertOrdersInit( ", _MagicNumber, " ) - OrderSelect( ", z, ", SELECT_BY_POS ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}
		if ( OrderMagicNumber() == _MagicNumber && OrderSymbol() == _Symbol )
		{
			int		_OrderType			= OrderType();
			int		_OrderTicket		= OrderTicket();
			double	_OrderLots			= NormalizeDouble( OrderLots(), 2 );
			double	_OrderOpenPrice	= NormalizeDouble( OrderOpenPrice(), _Digits );
			double	_OrderStopLoss		= NormalizeDouble( OrderStopLoss(), _Digits );
			double	_OrderTakeProfit	= NormalizeDouble( OrderTakeProfit(), _Digits );
			datetime	_OrderOpenTime		= OrderOpenTime();
			double	_OrderProfit		= NormalizeDouble( OrderProfit(), 2 );
			double	_OrderSwap			= NormalizeDouble( OrderSwap(), 2 );
			double	_OrderCommission	= NormalizeDouble( OrderCommission(), 2 );
			string	_OrderComment		= OrderComment();
			datetime	_OrderExpiration	= OrderExpiration();

			if ( _OrderType == OP_BUY			) { _MarketOrders ++; _BuyTicket = _OrderTicket; _BuyLots = _OrderLots; _BuyOpenPrice = _OrderOpenPrice; _BuyStopLoss = _OrderStopLoss; _BuyTakeProfit = _OrderTakeProfit; _BuyOpenTime = _OrderOpenTime; _BuyProfit = _OrderProfit; _BuySwap = _OrderSwap; _BuyCommission = _OrderCommission; _BuyComment = _OrderComment; }
			if ( _OrderType == OP_SELL			) { _MarketOrders ++; _SellTicket = _OrderTicket; _SellLots = _OrderLots; _SellOpenPrice = _OrderOpenPrice; _SellStopLoss = _OrderStopLoss; _SellTakeProfit = _OrderTakeProfit; _SellOpenTime = _OrderOpenTime; _SellProfit = _OrderProfit; _SellSwap = _OrderSwap; _SellCommission = _OrderCommission; _SellComment = _OrderComment; }
			if ( _OrderType == OP_BUYSTOP		) { _PendingOrders ++; _BuyStopTicket = _OrderTicket; _BuyStopLots = _OrderLots; _BuyStopOpenPrice = _OrderOpenPrice; _BuyStopStopLoss = _OrderStopLoss; _BuyStopTakeProfit = _OrderTakeProfit; _BuyStopOpenTime = _OrderOpenTime; _BuyStopComment = _OrderComment; _BuyStopExpiration = _OrderExpiration; }
			if ( _OrderType == OP_SELLSTOP	) { _PendingOrders ++; _SellStopTicket = _OrderTicket; _SellStopLots = _OrderLots; _SellStopOpenPrice = _OrderOpenPrice; _SellStopStopLoss = _OrderStopLoss; _SellStopTakeProfit = _OrderTakeProfit; _SellStopOpenTime = _OrderOpenTime; _SellStopComment = _OrderComment; _SellStopExpiration = _OrderExpiration; }
			if ( _OrderType == OP_BUYLIMIT	) { _PendingOrders ++; _BuyLimitTicket = _OrderTicket; _BuyLimitLots = _OrderLots; _BuyLimitOpenPrice = _OrderOpenPrice; _BuyLimitStopLoss = _OrderStopLoss; _BuyLimitTakeProfit = _OrderTakeProfit; _BuyLimitOpenTime = _OrderOpenTime; _BuyLimitComment = _OrderComment; _BuyLimitExpiration = _OrderExpiration; }
			if ( _OrderType == OP_SELLLIMIT	) { _PendingOrders ++; _SellLimitTicket = _OrderTicket; _SellLimitLots = _OrderLots; _SellLimitOpenPrice = _OrderOpenPrice; _SellLimitStopLoss = _OrderStopLoss; _SellLimitTakeProfit = _OrderTakeProfit; _SellLimitOpenTime = _OrderOpenTime; _SellLimitComment = _OrderComment; _SellLimitExpiration = _OrderExpiration; }
		}
	}
	_ExpertOrders = _MarketOrders + _PendingOrders;
}

// �-��� minri, ������������ ����
/////////////////////////////////////////////////////////////////////////////////
/**/ double LotCost ( string _Symbol )
/////////////////////////////////////////////////////////////////////////////////
{
	if ( MarketInfo ( _Symbol, MODE_BID ) <= 0 ) { return(-1.0); }
	double Cost = -1.0;

	string FirstPart  = StringSubstr( _Symbol, 0, 3 );
	string SecondPart = StringSubstr( _Symbol, 3, 3 );

	double Base = MarketInfo ( _Symbol, MODE_LOTSIZE ) * MarketInfo ( _Symbol, MODE_POINT );
	if ( SecondPart == "USD" )
	{ Cost = Base; }
	else
	{
		if ( FirstPart == "USD" )
		{ Cost = Base / MarketInfo ( _Symbol, MODE_BID ); }
		else
		{
			if ( MarketInfo( "USD" + SecondPart, MODE_BID ) > 0 )
			{ Cost = Base / MarketInfo( "USD" + SecondPart, MODE_BID ); }
			else
			{ Cost = Base * MarketInfo( SecondPart + "USD", MODE_BID ); }
		}
	}
	return( NormalizeDouble(Cost, 2) );
}

string	Send_Symbol = "", Send_Comment = "";
int		Send_OrderType = 0, Send_Slippage = 0, Send_MagicNumber = 0, Send_StartTickCount = 0, Send_GetLastError = 0;
double	Send_Volume = 0.0, Send_OpenPrice = 0.0, Send_StopLoss = 0.0, Send_TakeProfit = 0.0;
datetime	Send_Expiration = 0;
color		Send_Color = CLR_NONE;
int		Send_Result = -1;

/////////////////////////////////////////////////////////////////////////////////
/**/ int _OrderSend ( string _Symbol, int _OrderType, double _Volume, double _OpenPrice, int _Slippage = -1, double _StopLoss = 0.0, double _TakeProfit = 0.0, string _Comment = "", int _MagicNumber = 0, datetime _Expiration = 0, color _Color = -2 )
/////////////////////////////////////////////////////////////////////////////////
// ����������� �-��� OrderSend + �������� ��������.
// ��� �������� ���������� ���������� OrderTicket, ��� ������ ��������� ���������� "-1",
// ��� ������ �������� ���������� "-2". ���� �������� ��������� ��������, ���������� "-3".
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Send_Symbol = _Symbol; Send_Comment = _Comment;
	Send_OrderType = _OrderType; Send_Slippage = _Slippage; Send_MagicNumber = _MagicNumber; Send_StartTickCount = GetTickCount();
	Send_Volume = _Volume; Send_OpenPrice = _OpenPrice; Send_StopLoss = _StopLoss; Send_TakeProfit = _TakeProfit;
	Send_Expiration = _Expiration;
	Send_Color = _Color;
	Send_Result = -1;
	StartTime = TimeLocal();

//---- �������� �� ����������, ������� ����� ������ � ����������� ��
	_OrderSend_SetValue();

//---- ������� ����������
	_OrderSend_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	if ( _OrderSend_Check() == false ) { return(-2); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(1);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������,
	if ( GetTickCount() - Send_StartTickCount > 1000 )
	{
//---- ��������� ����������
		_OrderSend_RefreshValue();
//---- � ��������� ��. ���� ���� ������ - �������
		if ( _OrderSend_Check() == false ) { TradeIsNotBusy(); return(-2); }
	}

//---- ����������, �����������
	Send_Result = OrderSend ( Send_Symbol, Send_OrderType, Send_Volume, Send_OpenPrice, Send_Slippage, Send_StopLoss, Send_TakeProfit, Send_Comment, Send_MagicNumber, Send_Expiration, Send_Color );
	Send_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( Send_Result < 0 || Send_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Send_GetLastError, "OrderSend" );
//---- ����������� �������� �����,
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 1, "Error", Send_GetLastError, ErrorDescription( Send_GetLastError ), "OrderSend(...)", "Error #" + Send_GetLastError + " ( " + ErrorDescription( Send_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}
//---- ���� �� ������,
//---- ����������� �������� �����,
	TradeIsNotBusy();

//---- ��������� - ������������� �� ��������� ������� �, ���� ���, ������� ���������� � �������, ��������� -4.
	int _GetLastError;
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( OrderSelect( Send_Result, SELECT_BY_TICKET ) ) { break; }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 ) { _Print_ ( 1, "OrderSelect( " + Send_Result + ", SELECT_BY_TICKET )", "Error #" + _GetLastError + " ( " + ErrorDescription( _GetLastError ) + " )" ); continue; }
		Processing_Error ( 0, "OrderSend" );
		_Return_ ( 1, "Error", 0, "����� �� ��� ����������/������� �� ���� �������", "OrderSend(...)", "����� �� ��� ����������/������� �� ���� �������" );
		return(-4);
	}

//---- ������ �������� � ����������,
	_OrderSend_SetArrow();

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 1, "OK", 0, "", "OrderSend(...)", "OK. Ticket " + Send_Result );
//---- � �������, ��������� � ������.
return(Send_Result);
}

void _OrderSend_SetValue ()
{
//---- ��������� _Slippage
	if ( Send_Slippage < 0 ) { Send_Slippage = Slippage; }
//---- ���� _Comment �� ���������, ������� "���_��������( ������, ��������� )"
	if ( Send_Comment == "" ) { Send_Comment = strComment; }
//---- ���� ���� �� ���������, ������������� OrderSellColor(��� "��������" �������) ��� OrderBuyColor(��� "�������" �������)
	if ( Send_Color < -1 )
	{
		Send_Color = OrderSellColor;
		if ( Send_OrderType == OP_BUY || Send_OrderType == OP_BUYLIMIT || Send_OrderType == OP_BUYSTOP )
		{ Send_Color = OrderBuyColor; }
	}
	else
	{
		if ( Send_Color < 0 ) Send_Color = CLR_NONE;
	}

//---- ���������� ���� �������� � ������� ����� ��������� ��� BUY � SELL �������
	if ( Send_OrderType == OP_BUY ) { Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits ); Send_Expiration = 0; }
	if ( Send_OrderType == OP_SELL ) { Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits ); Send_Expiration = 0; }

//	Send_Volume = NormalizeDouble( Send_Volume, 2 );
	Send_OpenPrice = NormalizeDouble( Send_OpenPrice, _Digits );
	Send_StopLoss = NormalizeDouble( Send_StopLoss, _Digits );
	Send_TakeProfit = NormalizeDouble( Send_TakeProfit, _Digits );
}
void _OrderSend_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }
	string str_tmp = stringSendingOrder + strOrderType ( Send_OrderType ) + stringSendingOrder1;
	if ( Send_OrderType <= OP_SELL )
	{ str_tmp = stringSendingPosition + strOrderType ( Send_OrderType ) + stringSendingPosition1; }
	int _OrderType_ColorVariant = 3;
	if ( Send_OrderType == OP_BUY || Send_OrderType == OP_BUYLIMIT || Send_OrderType == OP_BUYSTOP ) { _OrderType_ColorVariant = 2; }

	_FileWrite ( "- - - - - - - - - - - - - - - - - OrderSend Start- - - - - - - - - - - - - - - - - - -" );
	_info ( 10, str_tmp, _OrderType_ColorVariant );
	_info ( 1, "Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
	_info ( 2, "Stop Loss = " + DoubleToStr( Send_StopLoss, _Digits ) );
	_info ( 3, "Take Profit = " + DoubleToStr( Send_TakeProfit, _Digits ) );
	_info ( 4, "Lot(s) = " + DoubleToStr( Send_Volume, 2 ) );

	_info ( 11, "Comment = " + Send_Comment, 0, 0 );
	_info ( 12, "MagicNumber = " + Send_MagicNumber, 0, 0 );
	_info ( 13, "" );
	_info ( 14, "" );
	
	if ( Send_Expiration > 0 ) { _FileWrite ( "Expiration Time = " + TimeToStr( Send_Expiration, TIME_DATE | TIME_SECONDS ) ); }
}
bool _OrderSend_Check ()
{
	if ( Allow_Info )	{ _info ( 13, stringCheck, 0, 0 ); }

	if ( _OrderCheck( Send_Symbol, Send_OrderType, Send_Volume, Send_OpenPrice, Send_StopLoss, Send_TakeProfit, Send_Expiration ) < 0 )
	{
		if ( Allow_Info )	{ _info ( 13, stringCheckError, 1, 0 ); }
		if ( Allow_LogFile ) { _FileWrite( "������ ��� �������� ����������!" ); }
		_Return_ ( 1, "Error", 0, stringInvalidParameters, "_OrderSend_Check()", stringInvalidParameters );
		return(false);
	}

	if ( Allow_Info ) { _info ( 13, stringCheckOK, 0, 0 ); }

return(true);
}
void _OrderSend_RefreshValue ()
{
	bool refreshed = false;
//---- ��������� ���� �������� ��� BUY � SELL �������
	if ( Send_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Send_OpenPrice, _Digits ) != NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits ) )
		{
			Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - OrderSend Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
			}
		}
	}
	if ( Send_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Send_OpenPrice, _Digits ) != NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits ) )
		{
			Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - OrderSend Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
			}
		}
	}
}
void _OrderSend_SetArrow ()
{
	if ( Send_Color == CLR_NONE ) { return(0); }
	string arrow_description = Send_Comment + "\nId " + Send_MagicNumber;
	string end_name;
	switch ( Send_OrderType )
	{
		case OP_BUY:			end_name = " buy "; break;
		case OP_SELL:			end_name = " sell "; break;
		case OP_BUYLIMIT:		end_name = " buy limit "; break;
		case OP_SELLLIMIT:	end_name = " sell limit "; break;
		case OP_BUYSTOP:		end_name = " buy stop "; break;
		case OP_SELLSTOP:		end_name = " sell stop "; break;
	}
	string open_name = "#" + Send_Result + end_name + DoubleToStr( Send_Volume, 2 ) + " " + Send_Symbol + " at " + DoubleToStr( Send_OpenPrice, _Digits );
	string sl_name = open_name + " stop loss at " + DoubleToStr( Send_StopLoss, _Digits );
	string tp_name = open_name + " take profit at " + DoubleToStr( Send_TakeProfit, _Digits );

	ObjectSetText( open_name, arrow_description, 10 );
	if ( NormalizeDouble( Send_StopLoss		, _Digits ) > 0 ) ObjectSetText( sl_name, arrow_description, 10 );
	if ( NormalizeDouble( Send_TakeProfit	, _Digits ) > 0 ) ObjectSetText( tp_name, arrow_description, 10 );
	GetLastError();
}

string	Modify_Symbol = "", Modify_OrderComment = "";
int		Modify_OrderTicket = 0, Modify_OrderType = 0, Modify_OrderMagicNumber = 0, Modify_StartTickCount = 0, Modify_GetLastError = 0;
double	Modify_OrderLots = 0.0, Modify_OrderOpenPrice = 0.0, Modify_OrderStopLoss = 0.0, Modify_OrderTakeProfit = 0.0;
double	Modify_New_OpenPrice = 0.0, Modify_New_StopLoss = 0.0, Modify_New_TakeProfit = 0.0;
datetime	Modify_OrderExpiration = 0, Modify_New_Expiration = 0;
color		Modify_Color = CLR_NONE;
bool		Modify_Result = false;

/////////////////////////////////////////////////////////////////////////////////
/**/ int _OrderModify ( int _OrderTicket, double New_OpenPrice = 0.0, double New_StopLoss = 0.0, double New_TakeProfit = 0.0, datetime New_Expiration = 0, color _Color = -2 )
/////////////////////////////////////////////////////////////////////////////////
// ����������� �-��� OrderModify + ��������.
// ��� �������� ���������� ���������� "1", ��� ������ ����������� ���������� "-1",
// ��� ������ ������ ������ ���������� "-2", ��� ������ �������� ���������� "-3".
// ���� �������� ��������� ��������, ���������� "-4", ���� ��� ���������� ��� �����������, ���������� "0".
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Modify_Symbol = ""; Modify_OrderComment = "";
	Modify_OrderTicket = _OrderTicket; Modify_OrderType = 0; Modify_OrderMagicNumber = 0; Modify_StartTickCount = GetTickCount();
	Modify_OrderLots = 0.0; Modify_OrderOpenPrice = 0.0; Modify_OrderStopLoss = 0.0; Modify_OrderTakeProfit = 0.0;
	Modify_New_OpenPrice = NormalizeDouble( New_OpenPrice, _Digits ); Modify_New_StopLoss = NormalizeDouble( New_StopLoss, _Digits ); Modify_New_TakeProfit = NormalizeDouble( New_TakeProfit, _Digits );
	Modify_OrderExpiration = 0; Modify_New_Expiration = New_Expiration;
	Modify_Color = _Color;
	Modify_Result = false;
	StartTime = TimeLocal();

//---- �������� �� ����������, ������� ����� ������ � ����������� ��, ���� ������ ��� ������ ������ - �������
	if ( _OrderModify_SetValue() == false ) { return(-2); }

//---- ������� ����������
	_OrderModify_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	int Check = _OrderModify_Check();
	if ( Check < 0 ) { return(Check); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(2);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������, ��������� ���������� � ��������� ��. ���� ���� ������ - �������
	if ( GetTickCount() - Modify_StartTickCount > 1000 )
	{
		if ( _OrderModify_RefreshValue() == false ) { TradeIsNotBusy(); return(-2); }
		Check = _OrderModify_Check();
		if ( Check < 0 ) { TradeIsNotBusy(); return(Check); }
	}

//---- ����������, �����������
	Modify_Result = OrderModify( Modify_OrderTicket, Modify_New_OpenPrice, Modify_New_StopLoss, Modify_New_TakeProfit, Modify_New_Expiration, Modify_Color );
	Modify_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( !Modify_Result || Modify_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Modify_GetLastError, "OrderModify" );
//---- ����������� �������� �����
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 2, "Error", Modify_GetLastError, ErrorDescription( Modify_GetLastError ), "OrderModify(...)", "Error #" + Modify_GetLastError + " ( " + ErrorDescription( Modify_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}
//---- ���� �� ������,
//---- ����������� �������� �����
	TradeIsNotBusy();

//---- ��������� - ������������� �� ���������� ��������� ������� �, ���� ���, ������� ���������� � �������, ��������� -4.
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( !OrderSelect( Modify_OrderTicket, SELECT_BY_TICKET ) )
		{
			Modify_GetLastError = GetLastError();
			_Print_ ( 2, "OrderSelect( " + Modify_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Modify_GetLastError + " ( " + ErrorDescription( Modify_GetLastError ) + " )" );
			continue;
		}
		if ( 	NormalizeDouble( Modify_OrderOpenPrice,  _Digits ) != NormalizeDouble( OrderOpenPrice(),  _Digits ) ||
				NormalizeDouble( Modify_OrderStopLoss,   _Digits ) != NormalizeDouble( OrderStopLoss(),   _Digits ) ||
				NormalizeDouble( Modify_OrderTakeProfit, _Digits ) != NormalizeDouble( OrderTakeProfit(), _Digits ) ||
				Modify_OrderExpiration != OrderExpiration() )
		{ break; }
		else
		{
			Processing_Error ( 0, "OrderModify" );
			_Return_ ( 2, "Error", 0, "����� �� ��� �������������/������� �� ���� ��������������", "OrderModify(...)", "����� �� ��� �������������/������� �� ���� ��������������" );
			return(-4);
		}
	}

//---- ������ �������� � ����������,
	_OrderModify_SetArrow();

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 2, "OK", 0, "", "OrderModify(...)", "OK" );

//---- � �������, ��������� 1.
return(1);
}

bool _OrderModify_SetValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Modify_OrderTicket, SELECT_BY_TICKET ) )
	{
		Modify_GetLastError = GetLastError();
		_Return_ ( 2,  "Error", Modify_GetLastError, ErrorDescription( Modify_GetLastError ), "OrderSelect( " + Modify_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Modify_GetLastError + " ( " + ErrorDescription( Modify_GetLastError ) + " )" );
		return(false);
	}

	Modify_Symbol				= OrderSymbol();
	Modify_OrderType			= OrderType();
	Modify_OrderLots			= NormalizeDouble ( OrderLots(), 2 );
	Modify_OrderOpenPrice	= NormalizeDouble ( OrderOpenPrice(), _Digits );
	Modify_OrderStopLoss		= NormalizeDouble ( OrderStopLoss(), _Digits );
	Modify_OrderTakeProfit	= NormalizeDouble ( OrderTakeProfit(), _Digits );
	Modify_OrderExpiration	= OrderExpiration();
	Modify_OrderMagicNumber = OrderMagicNumber();
	Modify_OrderComment		= OrderComment();

//---- ���� ���� �� ���������, ������������� OrderSellColor(��� "��������" �������) ��� OrderBuyColor(��� "�������" �������)
	if ( Modify_Color < -1 )
	{
		Modify_Color = OrderSellColor;
		if ( Modify_OrderType == OP_BUY || Modify_OrderType == OP_BUYLIMIT || Modify_OrderType == OP_BUYSTOP )
		{ Modify_Color = OrderBuyColor; }
	}
	else
	{
		if ( Modify_Color < 0 ) Modify_Color = CLR_NONE;
	}

//---- ���� ��������� ��� ��������� �� ����������, ��������� �� ����������� ������
	if ( Modify_New_OpenPrice  < 0 )	{ Modify_New_OpenPrice	= NormalizeDouble( Modify_OrderOpenPrice, _Digits ); }
	if ( Modify_New_StopLoss   < 0 )	{ Modify_New_StopLoss	= NormalizeDouble( Modify_OrderStopLoss, 	_Digits ); }
	if ( Modify_New_TakeProfit < 0 )	{ Modify_New_TakeProfit	= NormalizeDouble( Modify_OrderTakeProfit, _Digits ); }
	if ( Modify_New_Expiration < 0 )	{ Modify_New_Expiration	= Modify_OrderExpiration; }

	if ( Modify_OrderType <= OP_SELL )
	{ Modify_New_OpenPrice = NormalizeDouble( Modify_OrderOpenPrice, _Digits ); Modify_New_Expiration = 0; }
	
return(true);
}
void _OrderModify_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }

	string str_tmp = "������������ ����� � ";
	if ( Modify_OrderType <= OP_SELL ) { str_tmp = "������������ ������� � "; }

	int _OpenPrice_ColorVariant = 0;
	bool _OpenPrice_FileWrite = false;
	if ( Modify_New_OpenPrice > Modify_OrderOpenPrice ) { _OpenPrice_ColorVariant = 2; _OpenPrice_FileWrite = true; }
	if ( Modify_New_OpenPrice < Modify_OrderOpenPrice ) { _OpenPrice_ColorVariant = 3; _OpenPrice_FileWrite = true; }
	int _StopLoss_ColorVariant = 0;
	bool _StopLoss_FileWrite = false;
	if ( Modify_New_StopLoss > Modify_OrderStopLoss ) { _StopLoss_ColorVariant = 2; _StopLoss_FileWrite = true; }
	if ( Modify_New_StopLoss < Modify_OrderStopLoss ) { _StopLoss_ColorVariant = 3; _StopLoss_FileWrite = true; }
	int _TakeProfit_ColorVariant = 0;
	bool _TakeProfit_FileWrite = false;
	if ( Modify_New_TakeProfit > Modify_OrderTakeProfit ) { _TakeProfit_ColorVariant = 2; _TakeProfit_FileWrite = true; }
	if ( Modify_New_TakeProfit < Modify_OrderTakeProfit ) { _TakeProfit_ColorVariant = 3; _TakeProfit_FileWrite = true; }

	_FileWrite ( " - - - - - - - - - - - - - - - - OrderModify Start - - - - - - - - - - - - - - - - - -" );
	_info ( 10, str_tmp + Modify_OrderTicket + ", " +  strOrderType( Modify_OrderType ) + "..." );
	_info ( 1, "Old Open Price = " + DoubleToStr( Modify_OrderOpenPrice, _Digits ), _OpenPrice_ColorVariant, _OpenPrice_FileWrite );
	_info ( 2, "Old Stop Loss = " + DoubleToStr( Modify_OrderStopLoss, _Digits ), _StopLoss_ColorVariant, _StopLoss_FileWrite );
	_info ( 3, "Old Take Profit = " + DoubleToStr( Modify_OrderTakeProfit, _Digits ), _TakeProfit_ColorVariant, _TakeProfit_FileWrite );
	_info ( 4, "" );

	_info ( 11, "New Open Price = " + DoubleToStr( Modify_New_OpenPrice, _Digits ), _OpenPrice_ColorVariant, _OpenPrice_FileWrite );
	_info ( 12, "New Stop Loss = " + DoubleToStr( Modify_New_StopLoss, _Digits ), _StopLoss_ColorVariant, _StopLoss_FileWrite );
	_info ( 13, "New Take Profit = " + DoubleToStr( Modify_New_TakeProfit, _Digits ), _TakeProfit_ColorVariant, _TakeProfit_FileWrite );
	_info ( 14, "" );

	if ( Modify_New_Expiration > 0 ) { _FileWrite ( "New Expiration Time = " + TimeToStr( Modify_New_Expiration, TIME_DATE | TIME_SECONDS ) ); }
}
int _OrderModify_Check ()
{
	if ( Allow_Info )	{ _info ( 14, stringCheck, 0, 0 ); }

//---- ���� ������ ������ - �������
	if ( 	NormalizeDouble( Modify_New_OpenPrice, _Digits ) == NormalizeDouble( Modify_OrderOpenPrice, _Digits ) &&
			NormalizeDouble( Modify_New_StopLoss, _Digits ) == NormalizeDouble( Modify_OrderStopLoss, _Digits ) &&
			NormalizeDouble( Modify_New_TakeProfit, _Digits ) == NormalizeDouble( Modify_OrderTakeProfit, _Digits ) &&
			Modify_New_Expiration == Modify_OrderExpiration )
	{
		if ( Allow_Info ) { _info ( 14, "��������� ���������.....��� �� ������ ��������� ��� ���������. ����������� ��������..." ); }
		if ( Allow_LogFile ) { _FileWrite( "��� �� ������ ��������� ��� ���������. ����������� ��������..." ); }
		_Return_ ( 2,  "Error", 0, "��� �� ������ ��������� ��� ���������", "_OrderModify_Check()", "��� �� ������ ��������� ��� ���������" );
		return(-1);
	}

//---- ��������� ��� ��������� OrderModify ( ����� _OrderTicket � _Color ),
//---- � ���� ���� ������ - �������
/*	if ( _OrderCheck( Modify_Symbol, Modify_OrderType, Modify_OrderLots, Modify_New_OpenPrice, Modify_New_StopLoss, Modify_New_TakeProfit, Modify_New_Expiration ) < 0 )
	{
		if ( Allow_Info )	{ _info ( 14, stringCheckError, 1, 0 ); }
		if ( Allow_LogFile ) { _FileWrite( "������ ��� �������� ����������!" ); }
		_Return_ ( 2,  "Error", 0, "������ ��� �������� ����������!", "_OrderModify_Check()", "������ ��� �������� ����������!" );
		return(-3);
	}
*/
	if ( Allow_Info ) { _info ( 14, stringCheckOK, 0, 0 ); }

return(1);
}
bool _OrderModify_RefreshValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Modify_OrderTicket, SELECT_BY_TICKET ) )
	{
		Modify_GetLastError = GetLastError();
		_Return_ ( 2,  "Error", Modify_GetLastError, ErrorDescription( Modify_GetLastError ), "OrderSelect( " + Modify_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Modify_GetLastError + " ( " + ErrorDescription( Modify_GetLastError ) + " )" );
		return(false);
	}

	Modify_OrderLots			= NormalizeDouble ( OrderLots(), 2 );
	Modify_OrderOpenPrice	= NormalizeDouble ( OrderOpenPrice(), _Digits );
	Modify_OrderStopLoss		= NormalizeDouble ( OrderStopLoss(), _Digits );
	Modify_OrderTakeProfit	= NormalizeDouble ( OrderTakeProfit(), _Digits );
	Modify_OrderExpiration	= OrderExpiration();

	if ( Modify_New_OpenPrice  < 0 )	{ Modify_New_OpenPrice	= NormalizeDouble( Modify_OrderOpenPrice, _Digits ); }
	if ( Modify_New_StopLoss   < 0 )	{ Modify_New_StopLoss	= NormalizeDouble( Modify_OrderStopLoss, _Digits ); }
	if ( Modify_New_TakeProfit < 0 )	{ Modify_New_TakeProfit	= NormalizeDouble( Modify_OrderTakeProfit, _Digits ); }
	if ( Modify_New_Expiration < 0 )	{ Modify_New_Expiration	= Modify_OrderExpiration; }

return(true);
}
void _OrderModify_SetArrow ()
{
	if ( Modify_Color == CLR_NONE ) { return(0); }

	string arrow_description = Modify_OrderComment + "\nId " + Modify_OrderMagicNumber;
	string end_name;
	switch ( Modify_OrderType )
	{
		case OP_BUY:			end_name = " buy"; break;
		case OP_SELL:			end_name = " sell"; break;
		case OP_BUYLIMIT:		end_name = " buy limit"; break;
		case OP_SELLLIMIT:	end_name = " sell limit"; break;
		case OP_BUYSTOP:		end_name = " buy stop"; break;
		case OP_SELLSTOP:		end_name = " sell stop"; break;
	}
	end_name = end_name + " modified " + TimeToStr( CurTime() );
	
	string open_name = "#" + Modify_OrderTicket + end_name;
	string sl_name = "#" + Modify_OrderTicket + " sl modified ";
	string tp_name = "#" + Modify_OrderTicket + " tp modified ";

	if ( NormalizeDouble( Modify_New_OpenPrice, _Digits ) != NormalizeDouble( Modify_OrderOpenPrice, _Digits ) )
	{
		sl_name = sl_name + TimeToStr( CurTime() );
		tp_name = tp_name + TimeToStr( CurTime() );
	
		ObjectSetText( open_name, arrow_description, 10 );
	}
	if ( NormalizeDouble( Modify_New_StopLoss, _Digits ) != NormalizeDouble( Modify_OrderStopLoss, _Digits ) )
	{
		ObjectSetText( sl_name, arrow_description, 10 );
	}
	if ( NormalizeDouble( Modify_New_TakeProfit, _Digits ) != NormalizeDouble( Modify_OrderTakeProfit, _Digits ) )
	{
		ObjectSetText( tp_name, arrow_description, 10 );
	}
	GetLastError();
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _OrderCheck ( string _Symbol, int _OrderType, double _Volume, double _OpenPrice, double _StopLoss, double _TakeProfit, datetime _Expiration )
/////////////////////////////////////////////////////////////////////////////////
// ��������� ���������� OP,SL,TP, ����� � ����� ��������� ��� �-��� _OrderSend � _OrderModify.
// ��� �������� �������� ���������� "1", ��� ������ ���������� "-1".
/////////////////////////////////////////////////////////////////////////////////
{
	int		_return				= 1;
	double	_Ask					= NormalizeDouble( MarketInfo( _Symbol, MODE_ASK ), _Digits );
	double	_Bid					= NormalizeDouble( MarketInfo( _Symbol, MODE_BID ), _Digits );
	
	string	_Ask_str				= DoubleToStr( _Ask, _Digits );
	string	_Bid_str				= DoubleToStr( _Bid, _Digits );
	string	_OpenPrice_str		= DoubleToStr( _OpenPrice, _Digits );
	string	_StopLoss_str		= DoubleToStr( _StopLoss, _Digits );
	string	_TakeProfit_str	= DoubleToStr( _TakeProfit, _Digits );
	string	_Expiration_str	= TimeToStr( _Expiration, TIME_DATE|TIME_SECONDS );

// _OrderType ������ ���� �� 0 �� 5
	if ( _OrderType < 0 || _OrderType > 5 )
	{
		_info ( 4, stringInvalidOrderType + " ( " + _OrderType + " )!!!", 1 );
		Send_GetLastError = 3;
		_return = -1;
	}

// ����� ������ ���� >= 0.1
	if ( NormalizeDouble( MarketInfo( _Symbol, MODE_MINLOT ) - _Volume, 4 ) > 0.0 || NormalizeDouble( _Volume - MarketInfo( _Symbol, MODE_MAXLOT ), 4 ) > 0.0 )
	{
		_info ( 4, "Lot(s) = " + DoubleToStr( _Volume, 4 ) + stringInvalidVolume, 1 );
		Send_GetLastError = 131;
		_return = -1;
	}

// - �������� ���� ��������
// - OP_BUYLIMIT �����
	if ( _OrderType == OP_BUYLIMIT )
	{
		if ( NormalizeDouble( _Ask - _OpenPrice, _Digits ) < NormalizeDouble( _StopLevel, _Digits ) )
		{
// - - ���� �������� - ������ ���� ���� Ask
			if ( NormalizeDouble( _Ask - _OpenPrice, _Digits ) < 0 )
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectAbove + " Ask = " + _Ask_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectTooBeside + " Ask = " + _Ask_str + ")!!!", 1 );
			}
			Send_GetLastError = 4107;
			_return = -1;
		}
	}
// - OP_BUYSTOP �����
	if ( _OrderType == OP_BUYSTOP )
	{
// - - ���� �������� - ������ ���� ���� Ask
		if ( NormalizeDouble( _OpenPrice - _Ask, _Digits ) < NormalizeDouble( _StopLevel, _Digits ) )
		{
			if ( NormalizeDouble( _OpenPrice - _Ask, _Digits ) < 0 )
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectBelow + " Ask = " + _Ask_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectTooBeside + " Ask = " + _Ask_str + ")!!!", 1 );
			}
			Send_GetLastError = 4107;
			_return = -1;
		}
	}
// - OP_SELLLIMIT
	if ( _OrderType == OP_SELLLIMIT )
	{
// - - ���� �������� - ������ ���� ���� Bid
		if ( NormalizeDouble( _OpenPrice - _Bid, _Digits ) < NormalizeDouble( _StopLevel, _Digits ) )
		{
			if ( NormalizeDouble( _OpenPrice - _Bid, _Digits ) < 0 )
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectBelow + " Bid = " + _Bid_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectTooBeside + " Bid = " + _Bid_str + ")!!!", 1 );
			}
			Send_GetLastError = 4107;
			_return = -1;
		}
	}
// - OP_SELLSTOP
	if ( _OrderType == OP_SELLSTOP )
	{
// - - ���� �������� - ������ ���� ���� Bid
		if ( NormalizeDouble( _Bid - _OpenPrice, _Digits ) < NormalizeDouble( _StopLevel, _Digits ) )
		{
			if ( NormalizeDouble( _Bid - _OpenPrice, _Digits ) < 0 )
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectAbove + " Bid = " + _Bid_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 1, "Open Price = " + _OpenPrice_str + stringIncorrectTooBeside + " Bid = " + _Bid_str + ")!!!", 1 );
			}
			Send_GetLastError = 4107;
			_return = -1;
		}
	}

// �������� ���� "�������" �������/�������
	if ( _OrderType == OP_BUY || _OrderType == OP_BUYLIMIT || _OrderType == OP_BUYSTOP )
	{
// - _StopLoss (���� ����) ������ ���� ���� _OpenPrice
		if ( NormalizeDouble( _StopLoss, _Digits ) > 0 && NormalizeDouble( (_OpenPrice - _Spread) - _StopLoss, _Digits ) < NormalizeDouble( _StopLevel, _Digits )  )
		{
			if ( NormalizeDouble( _OpenPrice - _StopLoss, _Digits ) < 0 )
			{
				_info ( 2, "Stop Loss = " + _StopLoss_str + stringIncorrectAbove + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 2, "Stop Loss = " + _StopLoss_str + stringIncorrectTooBeside + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
			Send_GetLastError = 130;
			_return = -1;
		}
// - _TakeProfit (���� ����) ������ ���� ���� _OpenPrice
		if ( NormalizeDouble( _TakeProfit, _Digits ) > 0 && NormalizeDouble( _TakeProfit - (_OpenPrice - _Spread), _Digits ) < NormalizeDouble( _StopLevel, _Digits )  )
		{
			if ( NormalizeDouble( _TakeProfit - _OpenPrice, _Digits ) < 0 )
			{
				_info ( 3, "Take Profit = " + _TakeProfit_str + stringIncorrectBelow + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 3, "Take Profit = " + _TakeProfit_str + stringIncorrectTooBeside + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
			Send_GetLastError = 130;
			_return = -1;
		}
	}

// �������� ���� "��������" �������/�������
	if ( _OrderType == OP_SELL || _OrderType == OP_SELLLIMIT || _OrderType == OP_SELLSTOP )
	{
// - _StopLoss (���� ����) ������ ���� ���� _OpenPrice
		if ( NormalizeDouble( _StopLoss, _Digits ) > 0 && NormalizeDouble( _StopLoss - (_OpenPrice + _Spread), _Digits ) < NormalizeDouble( _StopLevel, _Digits )  )
		{
			if ( NormalizeDouble( _StopLoss - _OpenPrice, _Digits ) < 0 )
			{
				_info ( 2, "Stop Loss = " + _StopLoss_str + stringIncorrectBelow + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 2, "Stop Loss = " + _StopLoss_str + stringIncorrectTooBeside + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
			Send_GetLastError = 130;
			_return = -1;
		}
// - _TakeProfit (���� ����) ������ ���� ���� _OpenPrice
		if ( NormalizeDouble( _TakeProfit, _Digits ) > 0 && NormalizeDouble( (_OpenPrice + _Spread) - _TakeProfit, _Digits ) < NormalizeDouble( _StopLevel, _Digits )  )
		{
			if ( NormalizeDouble( _OpenPrice - _TakeProfit, _Digits ) < 0 )
			{
				_info ( 3, "Take Profit = " + _TakeProfit_str + stringIncorrectAbove + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
// - - ����������� ������ - _StopLevel
			else
			{
				_info ( 3, "Take Profit = " + _TakeProfit_str + stringIncorrectTooBeside + " OpenPrice = " + _OpenPrice_str + ")!!!", 1 );
			}
			Send_GetLastError = 130;
			_return = -1;
		}
	}

// - ����� ���������
// - - ������-������ - ����� ��������� ������ ���� = 0
	if ( _OrderType <= OP_SELL )
	{
		if ( _Expiration != 0 )
		{
			_info ( 4, "Expiration Time = " + _Expiration_str + stringInvalidExpiration, 1 );
			Send_GetLastError = 3;
			_return = -1;
		}
	}
// - - ���������� ������ - ����� ��������� (���� ����) ������ ���� > �������� ���������� �������
	else
	{
		if ( _Expiration > 0 && _Expiration <= CurTime() )
		{
			_info ( 4, "Expiration Time = " + _Expiration_str + stringIncorrectExpiration, 1 );
			Send_GetLastError = 3;
			_return = -1;
		}
	}

// ���� ���� ���� ���� ������, ���������� -1
// ���� ��� �������� ������ �������, ���������� 1
return(_return);
}

string	Close_OrderSymbol = "", Close_ProfitLoss = "", Close_OrderComment = "";
int		Close_OrderTicket = 0, Close_OrderType = 0, Close_OrderMagicNumber = 0, Close_Slippage = 0, Close_StartTickCount = 0, Close_GetLastError = 0;
double	Close_Volume = 0.0, Close_OrderLots = 0.0, Close_OrderOpenPrice = 0.0, Close_ClosePrice = 0.0;
color		Close_Color = CLR_NONE;
bool		Close_Result = false;

/////////////////////////////////////////////////////////////////////////////////
/**/ int _OrderClose ( int _OrderTicket, double _Volume = 0.0, double ClosePrice = 0.0, int _Slippage = -1, color _Color = -2 )
/////////////////////////////////////////////////////////////////////////////////
// ����������� �-��� OrderClose + ��������.
//
// ��� �������� ���������� ���������� "1", ��� ������ �������� ���������� "-1",
// ��� ������ ������ ������ ���������� "-2", ��� ������ �������� ���������� "-3".
// ���� �������� ��������� ��������, ���������� "-4".
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Close_OrderSymbol = ""; Close_ProfitLoss = ""; Close_OrderComment = "";
	Close_OrderTicket = _OrderTicket; Close_OrderType = 0; Close_OrderMagicNumber = 0; Close_Slippage = _Slippage; Close_StartTickCount = GetTickCount();
	Close_Volume = _Volume; Close_OrderLots = 0.0; Close_OrderOpenPrice = 0.0; Close_ClosePrice = ClosePrice;
	Close_Color = _Color;
	Close_Result = false;
	StartTime = TimeLocal();

//---- �������� �� ����������, ������� ����� ������ � ����������� ��, ���� ������ ��� ������ ������ - �������
	if ( _OrderClose_SetValue() == false ) { return(-2); }

//---- ������� ����������
	_OrderClose_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	if ( _OrderClose_Check() == false ) { return(-3); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(3);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������, ��������� ���������� � ��������� ��. ���� ���� ������ - �������
	if ( GetTickCount() - Close_StartTickCount > 1000 )
	{
		if ( _OrderClose_RefreshValue() == false ) { TradeIsNotBusy(); return(-2); }
		if ( _OrderClose_Check() == false ) { TradeIsNotBusy(); return(-3); }
	}

//---- ����������, �����������
	Close_Result = OrderClose( Close_OrderTicket, Close_Volume, Close_ClosePrice, Close_Slippage, Close_Color );
	Close_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( !Close_Result || Close_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Close_GetLastError, "OrderClose" );
//---- ����������� �������� �����
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 3, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "OrderClose(...)", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}

//---- ���� �� ������,
//---- ����������� �������� �����
	TradeIsNotBusy();

//---- ��������� - ������������� �� ��������� ������� �, ���� ���, ������� ���������� � �������, ��������� -5
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
		{
			if ( OrderCloseTime() <= 0 )
			{
				Processing_Error ( 0, "OrderClose" );
				_Return_ ( 3, "Error", 0, "������� �� ���� �������", "OrderClose(...)", "������� �� ���� �������" );
				return(-5);
			}
			else
			{ break; }
		}
		Close_GetLastError = GetLastError();
		_Print_ ( 3, "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
	}

//---- ������ �������� � ����������,
	_OrderClose_SetArrow();

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 3, "OK", 0, "", "OrderClose(...)", "OK" );

//---- � �������, ��������� 1.
return(1);
}

bool _OrderClose_SetValue ()
{
//---- ��������� _Slippage
	if ( Close_Slippage < 0 ) { Close_Slippage = Slippage; }

//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
	{
		Close_GetLastError = GetLastError();
		_Return_ ( 3, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
		return(false);
	}

	Close_OrderSymbol			= OrderSymbol();
	Close_OrderType			= OrderType();
	Close_ProfitLoss			= DoubleToStr( OrderProfit() + OrderSwap() + OrderCommission(), 2 );
	Close_OrderComment		= OrderComment();
	Close_OrderMagicNumber	= OrderMagicNumber();

Close_OrderOpenPrice = OrderOpenPrice();
//---- ��������� Close_Volume - ���� ������ OrderLots ��� ������ 0.1, ������ OrderLots.
	Close_OrderLots = NormalizeDouble( OrderLots(), _Digits );
	if ( NormalizeDouble( Close_Volume, 2 ) > NormalizeDouble( Close_OrderLots, 2 ) || NormalizeDouble( Close_Volume, 2 ) < NormalizeDouble( MarketInfo( _Symbol, MODE_MINLOT ), 2 ) )
	{ Close_Volume = NormalizeDouble( Close_OrderLots, 2 ); }

//---- ���������� Close_ClosePrice
	if ( Close_OrderType == OP_SELL ) { Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_ASK ), _Digits ); }
	if ( Close_OrderType == OP_BUY ) { Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_BID ), _Digits ); }

//---- ����������� ����������
	Close_Volume = NormalizeDouble( Close_Volume, 2 );

//---- ���� ���� �� ���������, ������������� OrderSellColor(��� "��������" �������) ��� OrderBuyColor(��� "�������" �������)
	if ( Close_Color < -1 )
	{
		Close_Color = OrderSellColor;
		if ( Close_OrderType == OP_BUY || Close_OrderType == OP_BUYLIMIT || Close_OrderType == OP_BUYSTOP )
		{ Close_Color = OrderBuyColor; }
	}
	else
	{
		if ( Close_Color < 0 ) Close_Color = CLR_NONE;
	}
	return(true);
}
void _OrderClose_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }
	int _OrderType_ColorVariant = 3;
	if ( Close_OrderType == OP_BUY || Close_OrderType == OP_BUYLIMIT || Close_OrderType == OP_BUYSTOP ) { _OrderType_ColorVariant = 2; }

	_FileWrite ( "- - - - - - - - - - - - - - - - - OrderClose Start - - - - - - - - - - - - - - - - - -" );
	_info ( 10, "��������� ������� � " + Close_OrderTicket + ", " + strOrderType ( Close_OrderType ) + "...", _OrderType_ColorVariant );
	_info ( 1, "�lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
	_info ( 2, "Lot(s) = " + DoubleToStr( Close_Volume, 2 ) );
	_info ( 3, "Profit/Loss = " + Close_ProfitLoss );
	_info ( 4, "" );

	_info ( 11, "Comment = " + Close_OrderComment, 0, 0 );
	_info ( 12, "MagicNumber = " + Close_OrderMagicNumber, 0, 0 );
	_info ( 13, "" );
	_info ( 14, "" );
}
bool _OrderClose_Check ()
{
	if ( Allow_Info )	{ _info ( 13, stringCheck, 0, 0 ); }

	if ( Close_OrderType > OP_SELL )
	{
		if ( Allow_Info || Allow_LogFile )
		{
			_info ( 13, stringCheckError, 1, 0 );
			_info ( 4, "��������� OrderType() - " + Close_OrderType + ". ������� ����� ���� ������ OP_BUY ��� OP_SELL �������.", 1 );
		}
		_Return_ ( 3, "Error", 0, "��������� OrderType()", "_OrderClose_Check()", "��������� OrderType()" );
		return(false);
	}

	if ( Allow_Info ) { _info ( 13, stringCheckOK, 0, 0 ); }
return(true);
}
bool _OrderClose_RefreshValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
	{
		Close_GetLastError = GetLastError();
		_Return_ ( 3, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
		return(false);
	}

	bool refreshed = false;
	Close_OrderType = OrderType();
	Close_OrderLots = NormalizeDouble( OrderLots(), _Digits );

//---- ��������� Close_ClosePrice
	if ( Close_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Close_ClosePrice, _Digits ) != NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_ASK ), _Digits ) )
		{
			Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_ASK ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - OrderClose Refresh - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed �lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
			}
		}
	}
	if ( Close_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Close_ClosePrice, _Digits ) != NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_BID ), _Digits ) )
		{
			Close_ClosePrice = NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_BID ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - OrderClose Refresh - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed �lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
			}
		}
	}

//---- ��������� Close_Volume - ���� ������ OrderLots ��� ������ 0.1, ������ OrderLots.
	if ( NormalizeDouble( Close_Volume, 2 ) > NormalizeDouble( Close_OrderLots, 2 ) || NormalizeDouble( Close_Volume, 2 ) < NormalizeDouble( MarketInfo( _Symbol, MODE_MINLOT ), 2 ) )
	{
		Close_Volume = NormalizeDouble( Close_OrderLots, 2 );
		if ( Allow_Info || Allow_LogFile )
		{
			if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - OrderClose Refresh - - - - - - - - - - - - - - - - -" ); refreshed = true; }
			_info ( 2, "Refreshed Lot(s) = " + DoubleToStr( Close_Volume, 2 ) );
		}
	}
return(true);
}
void _OrderClose_SetArrow ()
{
	if ( Close_Color == CLR_NONE ) { return(0); }
	string arrow_description = Close_OrderComment + "\nId " + Close_OrderMagicNumber;
	string end_name;
	switch ( Close_OrderType )
	{
		case OP_BUY:	end_name = " buy "; break;
		case OP_SELL:	end_name = " sell "; break;
	}
	string close_name = "#" + Close_OrderTicket + end_name + DoubleToStr( Close_Volume, 2 ) + " " + Close_OrderSymbol + " at " + DoubleToStr( Close_OrderOpenPrice, _Digits ) + " close at " + DoubleToStr( Close_ClosePrice, _Digits );

	ObjectSetText( close_name, arrow_description, 10 );
	GetLastError();
}

string	Delete_OrderComment = "";
int		Delete_OrderTicket = 0, Delete_OrderType = 0, Delete_OrderMagicNumber = 0, Delete_StartTickCount = 0, Delete_GetLastError = 0;
bool		Delete_Result = false;

/////////////////////////////////////////////////////////////////////////////////
/**/ int _OrderDelete ( int _OrderTicket )
/////////////////////////////////////////////////////////////////////////////////
// ����������� �-��� OrderDelete + ��������.
//
// ��� �������� ���������� ���������� "1", ��� ������ �������� ���������� "-1",
// ��� ������ ������ ������ ���������� "-2", ��� ������ �������� ���������� "-3".
// ���� �������� ��������� ��������, ���������� "-4".
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Delete_OrderComment = "";
	Delete_OrderTicket = _OrderTicket; Delete_OrderType = 0; Delete_OrderMagicNumber = 0; Delete_StartTickCount = GetTickCount();
	Delete_Result = false;
	StartTime = TimeLocal();

//---- �������� �� ����������, ������� ����� ������ � ����������� ��, ���� ������ ��� ������ ������ - �������
	if ( _OrderDelete_SetValue() == false ) { return(-2); }

//---- ������� ����������
	_OrderDelete_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	if ( _OrderDelete_Check() == false ) { return(-3); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(3);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������, ��������� ���������� � ��������� ��. ���� ���� ������ - �������
	if ( GetTickCount() - Delete_StartTickCount > 1000 )
	{
		if ( _OrderDelete_RefreshValue() == false ) { TradeIsNotBusy(); return(-2); }
		if ( _OrderDelete_Check() == false ) { TradeIsNotBusy(); return(-3); }
	}

//---- ����������, �������
	Delete_Result = OrderDelete( Delete_OrderTicket );
	Delete_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( !Delete_Result || Delete_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Delete_GetLastError, "OrderDelete" );
//---- ����������� �������� �����
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 4, "Error", Delete_GetLastError, ErrorDescription( Delete_GetLastError ), "OrderDelete(...)", "Error #" + Delete_GetLastError + " ( " + ErrorDescription( Delete_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}

//---- ���� �� ������,
//---- ����������� �������� �����
	TradeIsNotBusy();

//---- ��������� - ������������� �� ����� ����� �, ���� ���, ������� ���������� � �������, ��������� -5
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		int _OrdersTotal = OrdersTotal();
		for ( int z = _OrdersTotal - 1; z >= 0; z -- )
		{
			if ( OrderSelect( z, SELECT_BY_POS ) )
			{
				if ( OrderTicket() == Delete_OrderTicket )
				{
					Processing_Error ( 0, "OrderDelete" );
					_Return_ ( 4, "Error", 0, "����� �� ��� �����", "OrderDelete(...)", "����� �� ��� �����" );
					return(-5);
				}
				if ( z == 0 ) { x = 5; break; }
			}
			else
			{
				Delete_GetLastError = GetLastError();
				_Print_ ( 4, "OrderSelect( " + z + ", SELECT_BY_POS )", "Error #" + Delete_GetLastError + " ( " + ErrorDescription( Delete_GetLastError ) + " )" );
			}
		}
	}

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 4, "OK", 0, "", "OrderDelete(...)", "OK" );
//---- � �������, ��������� 1.
return(1);
}

bool _OrderDelete_SetValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Delete_OrderTicket, SELECT_BY_TICKET ) )
	{
		Delete_GetLastError = GetLastError();
		_Print_ ( 4, "OrderSelect( " + Delete_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Delete_GetLastError + " ( " + ErrorDescription( Delete_GetLastError ) + " )" );
		return(false);
	}

	Delete_OrderType			= OrderType();
	Delete_OrderComment		= OrderComment();
	Delete_OrderMagicNumber	= OrderMagicNumber();

return(true);
}
void _OrderDelete_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }
	int _OrderType_ColorVariant = 3;
	if ( Delete_OrderType == OP_BUY || Delete_OrderType == OP_BUYLIMIT || Delete_OrderType == OP_BUYSTOP ) { _OrderType_ColorVariant = 2; }

	_FileWrite ( " - - - - - - - - - - - - - - - - OrderDelete Start - - - - - - - - - - - - - - - - - -" );
	_info ( 10, "������� ����� � " + Delete_OrderTicket + ", " + strOrderType ( Delete_OrderType ) + "...", _OrderType_ColorVariant );
	_info ( 1, "" );
	_info ( 2, "" );
	_info ( 3, "" );
	_info ( 4, "" );

	_info ( 11, "Comment = " + Delete_OrderComment, 0, 0 );
	_info ( 12, "MagicNumber = " + Delete_OrderMagicNumber, 0, 0 );
	_info ( 13, "" );
	_info ( 14, "" );
}
bool _OrderDelete_Check ()
{
	if ( Allow_Info )	{ _info ( 13, stringCheck, 0, 0 ); }

//---- ��������� _OrderType
	if ( Delete_OrderType <= OP_SELL )
	{
		if ( Allow_Info || Allow_LogFile )
		{
			_info ( 13, stringCheckError, 1, 0 );
			_info ( 14, "��������� OrderType() - " + Delete_OrderType + ". ����� ����� ���� ������ ���������� �����.", 1 );
		}
		_Return_ ( 4, "Error", 0, "��������� OrderType", "_OrderDelete_Check()", "��������� OrderType" );
		return(false);
	}

	if ( Allow_Info ) { _info ( 13, stringCheckOK, 0, 0 ); }

return(true);
}
bool _OrderDelete_RefreshValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Delete_OrderTicket, SELECT_BY_TICKET ) )
	{
		Delete_GetLastError = GetLastError();
		_Print_ ( 4, "OrderSelect( " + Delete_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Delete_GetLastError + " ( " + ErrorDescription( Delete_GetLastError ) + " )" );
		return(false);
	}

	Delete_OrderType = OrderType();
return(true);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _Reverse ( int _OrderTicket, int _Slippage = -1, color _CloseColor = CLR_NONE, color _SendColor = CLR_NONE )
/////////////////////////////////////////////////////////////////////////////////
// ���������. ������� � _OrderTicket ����������� � ����������� ����� � ����� �� ����� ��������� + ��������.
//
// ��� �������� ���������� ���������� "1", ��� �������� ������ ���������� "-1",
// ��� ������ ������ ������ ���������� "-2", ��� ������ �������� ���������� "-3".
// ���� �������� ��������� ��������, ���������� "-4".
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Close_OrderSymbol = ""; Close_ProfitLoss = ""; Close_OrderComment = "";
	Close_OrderTicket = _OrderTicket; Close_OrderType = 0; Close_OrderMagicNumber = 0; Close_Slippage = _Slippage; Close_StartTickCount = GetTickCount();
	Close_Volume = 0.0; Close_OrderLots = 0.0; Close_ClosePrice = 0.0;
	Close_Color = _CloseColor;
	Close_Result = false;

	Send_Symbol = ""; Send_Comment = "";
	Send_OrderType = 0; Send_Slippage = _Slippage; Send_MagicNumber = 0;
	Send_Volume = 0.0; Send_OpenPrice = 0.0; Send_StopLoss = 0.0; Send_TakeProfit = 0.0;
	Send_Expiration = 0;
	Send_Color = _SendColor;
	Send_Result = -1;

	StartTime = TimeLocal();

//---- �������� �� ����������, ������� ����� ������ � ����������� ��, ���� ������ ��� ������ ������ - �������
	if ( _Reverse_SetValue() == false ) { return(-2); }

//---- ������� ����������
	_Reverse_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	if ( _Reverse_Check() == false ) { return(-3); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(5);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������, ��������� ���������� � ��������� ��. ���� ���� ������ - �������
	if ( GetTickCount() - Close_StartTickCount > 1000 )
	{
		if ( _Reverse_RefreshValue() == false ) { TradeIsNotBusy(); return(-2); }
		if ( _Reverse_Check() == false ) { TradeIsNotBusy(); return(-3); }
	}

//---- ����������, �����������
	Close_Result = OrderClose( Close_OrderTicket, Close_Volume, Close_ClosePrice, Close_Slippage, Close_Color );
	Close_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( !Close_Result || Close_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Close_GetLastError, "Reverse" );
//---- ����������� �������� �����
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 5, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "Reverse(...)", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}
//---- ���� �� ������,
//---- ��������� - ������������� �� ��������� ������� �, ���� ���, ������� ���������� � �������, ��������� -5
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
		{
			if ( OrderCloseTime() <= 0 )
			{
				Processing_Error ( 0, "OrderClose" );
				_Return_ ( 5, "Error", 0, "������� �� ���� �������", "Reverse(...)", "������� �� ���� �������" );
				return(-5);
			}
			else
			{ break; }
		}
		Close_GetLastError = GetLastError();
		_Print_ ( 5, "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
	}

//---- ������ �������� � ����������,
	_OrderClose_SetArrow();

//---- ������� ����������,
	_Return_ ( 5, "OK", 0, "", "OrderClose(...)", "OK" );

//---- � ����������� ���������
	Send_Result = OrderSend ( Send_Symbol, Send_OrderType, Send_Volume, Send_OpenPrice, Send_Slippage, Send_StopLoss, Send_TakeProfit, Send_Comment, Send_MagicNumber, Send_Expiration, Send_Color );
	Send_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( Send_Result < 0 || Send_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Send_GetLastError, "Reverse" );
//---- ����������� �������� �����,
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 5, "Error", Send_GetLastError, ErrorDescription( Send_GetLastError ), "Revers(...)", "Error #" + Send_GetLastError + " ( " + ErrorDescription( Send_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}
//---- ���� �� ������,
//---- ����������� �������� �����,
	TradeIsNotBusy();

//---- ��������� - ������������� �� ��������� ������� �, ���� ���, ������� ���������� � �������, ��������� -4.
	int _GetLastError;
	for ( x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( OrderSelect( Send_Result, SELECT_BY_TICKET ) ) { break; }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 ) { _Print_ ( 6, "OrderSelect( " + Send_Result + ", SELECT_BY_TICKET )", "Error #" + _GetLastError + " ( " + ErrorDescription( _GetLastError ) + " )" ); continue; }
		Processing_Error ( 0, "Reverse" );
		_Return_ ( 6, "Error", 0, "����� �� ��� ����������/������� �� ���� �������", "Reverse(...)", "����� �� ��� ����������/������� �� ���� �������" );
		return(-4);
	}

//---- ������ �������� � ����������,
	_OrderSend_SetArrow();

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 6, "OK", 0, "", "OrderSend(...)", "OK. Ticket " + Send_Result );
//---- � �������, ��������� � ������.
return(Send_Result);
}

bool _Reverse_SetValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
	{
		Close_GetLastError = GetLastError();
		_Return_ ( 5, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
		return(false);
	}
	Close_OrderSymbol			= OrderSymbol();
	Close_OrderType			= OrderType();
	Close_Volume				= NormalizeDouble( OrderLots(), _Digits );
	Close_ProfitLoss			= DoubleToStr( OrderProfit() + OrderSwap() + OrderCommission(), 2 );
	Close_OrderComment		= OrderComment();
	Close_OrderMagicNumber	= OrderMagicNumber();

//---- ���������� Close_ClosePrice
	if ( Close_OrderType == OP_SELL ) { Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_ASK ), _Digits ); }
	if ( Close_OrderType == OP_BUY ) { Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_BID ), _Digits ); }

	Send_Symbol			= Close_OrderSymbol;
	Send_Comment		= Close_OrderComment;
	Send_MagicNumber	= Close_OrderMagicNumber;
	Send_Volume			= Close_Volume;
	Send_StopLoss		= 0.0;
	Send_TakeProfit	= 0.0;
	Send_Expiration	= 0;

	if ( Close_OrderType == OP_BUY ) { Send_OrderType = OP_SELL; }
	if ( Close_OrderType == OP_SELL ) { Send_OrderType = OP_BUY; }

//---- ���������� ���� �������� � ������� ����� ��������� ��� BUY � SELL �������
	if ( Send_OrderType == OP_BUY ) { Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits ); }
	if ( Send_OrderType == OP_SELL ) { Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits ); }


//---- ��������� _Slippage
	if ( Send_Slippage < 0 ) { Send_Slippage = Slippage; }
	if ( Close_Slippage < 0 ) { Close_Slippage = Slippage; }
//---- ���� _Comment �� ���������, ������� "���_��������( ������, ��������� )"
	if ( Send_Comment == "" ) { Send_Comment = strComment; }
//---- ���� ���� �� ���������, ������������� OrderSellColor(��� "��������" �������) ��� OrderBuyColor(��� "�������" �������)
	if ( Send_Color < -1 )
	{
		Send_Color = OrderSellColor;
		if ( Send_OrderType == OP_BUY || Send_OrderType == OP_BUYLIMIT || Send_OrderType == OP_BUYSTOP )
		{ Send_Color = OrderBuyColor; }
	}
	else
	{
		if ( Send_Color < 0 ) Send_Color = CLR_NONE;
	}
	if ( Close_Color < -1 )
	{
		Close_Color = OrderSellColor;
		if ( Close_OrderType == OP_BUY || Close_OrderType == OP_BUYLIMIT || Close_OrderType == OP_BUYSTOP )
		{ Close_Color = OrderBuyColor; }
	}
	else
	{
		if ( Close_Color < 0 ) Close_Color = CLR_NONE;
	}
	return(true);
}
void _Reverse_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }

	_FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Start  - - - - - - - - - - - - - - - - - -" );
	_info ( 10, "��������� ������� � " + Close_OrderTicket + ", " + strOrderType ( Close_OrderType ) + "..." );
	_info ( 1, "�lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
	_info ( 2, "Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
	_info ( 3, "Lot(s) = " + DoubleToStr( Close_Volume, 1 ) );
	_info ( 4, "" );

	_info ( 11, "Comment = " + Close_OrderComment, 0, 0 );
	_info ( 12, "MagicNumber = " + Close_OrderMagicNumber, 0, 0 );
	_info ( 13, "" );
	_info ( 14, "" );
}

bool _Reverse_Check ()
{
	if ( Allow_Info )	{ _info ( 13, stringCheck, 0, 0 ); }

//---- ��������� _OrderType
	if ( Close_OrderType > OP_SELL )
	{
		if ( Allow_Info || Allow_LogFile )
		{
			_info ( 13, stringCheckError, 1, 0 );
			_info ( 4, "��������� OrderType() - " + Close_OrderType + ". ������� ����� ���� ������ OP_BUY ��� OP_SELL �������.", 1 );
		}
		_Return_ ( 5, "Error", 0, "��������� OrderType()", "_Reverse_Check()", "��������� OrderType()" );
		return(false);
	}

//---- ��������� ��� ��������� OrderSend ( ����� Send_Symbol, _Slippage, _Comment, Send_MagicNumber � Send_Color ),
//---- � ���� ���� ������ - �������
	if ( _OrderCheck( Send_Symbol, Send_OrderType, Send_Volume, Send_OpenPrice, Send_StopLoss, Send_TakeProfit, Send_Expiration ) < 0 )
	{
		if ( Allow_Info )	{ _info ( 13, stringCheckError, 1, 0 ); }
		if ( Allow_LogFile ) { _FileWrite( "������ ��� �������� ����������!" ); }
		_Return_ ( 6, "Error", 0, stringInvalidParameters, "_Reverse_Check()", "Error" );
		return(false);
	}

	if ( Allow_Info ) { _info ( 13, stringCheckOK, 0, 0 ); }
return(true);
}
bool _Reverse_RefreshValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Close_OrderTicket, SELECT_BY_TICKET ) )
	{
		Close_GetLastError = GetLastError();
		_Return_ ( 5, "Error", Close_GetLastError, ErrorDescription( Close_GetLastError ), "OrderSelect( " + Close_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Close_GetLastError + " ( " + ErrorDescription( Close_GetLastError ) + " )" );
		return(false);
	}

	bool refreshed = false;
	Close_OrderType = OrderType();
	Close_Volume	 = NormalizeDouble( OrderLots(), _Digits );

//---- ��������� Close_ClosePrice
	if ( Close_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Close_ClosePrice, _Digits ) != NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_ASK ), _Digits ) )
		{
			Close_ClosePrice = NormalizeDouble( MarketInfo ( Close_OrderSymbol, MODE_ASK ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed �lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
			}
		}
	}
	if ( Close_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Close_ClosePrice, _Digits ) != NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_BID ), _Digits ) )
		{
			Close_ClosePrice = NormalizeDouble( MarketInfo( Close_OrderSymbol, MODE_BID ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 1, "Refreshed �lose Price = " + DoubleToStr( Close_ClosePrice, _Digits ) );
			}
		}
	}

	Send_Volume = NormalizeDouble( Close_Volume, 2 );
	if ( Close_OrderType == OP_BUY ) { Send_OrderType = OP_SELL; }
	if ( Close_OrderType == OP_SELL ) { Send_OrderType = OP_BUY; }

//---- ��������� ���� �������� ��� BUY � SELL �������
	if ( Send_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Send_OpenPrice, _Digits ) != NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits ) )
		{
			Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_ASK ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 2, "Refreshed Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
			}
		}
	}
	if ( Send_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Send_OpenPrice, _Digits ) != NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits ) )
		{
			Send_OpenPrice = NormalizeDouble( MarketInfo( Send_Symbol, MODE_BID ), _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 2, "Refreshed Open Price = " + DoubleToStr( Send_OpenPrice, _Digits ) );
			}
		}
	}
return(true);
}

string	Trailing_Symbol = "", Trailing_OrderComment = "";
int		Trailing_OrderTicket = 0, Trailing_TrailingStop = 0, Trailing_BreakEven_After = 0, Trailing_OrderType = 0, Trailing_OrderMagicNumber = 0, Trailing_StartTickCount = 0, Trailing_GetLastError = 0;
double	Trailing_OrderOpenPrice = 0.0, Trailing_OrderStopLoss = 0.0, Trailing_OrderTakeProfit = 0.0, Trailing_New_StopLoss = 0.0, Trailing_Bid = 0.0, Trailing_Ask = 0.0;
datetime	Trailing_OrderExpiration = 0;
color		Trailing_Color = CLR_NONE;
bool		Trailing_Result = false;

double Trailing_Luft = 0.0;

/////////////////////////////////////////////////////////////////////////////////
/**/ int _TrailingStop ( int _OrderTicket, int TrailingStop, color _Color = -2, int BreakEven_After = 0, int LuftPoints = -1 )
/////////////////////////////////////////////////////////////////////////////////
// TrailingStop + ����� ����������
// ��� �������� �������� ���������� "1", ��� ����������� ������������� ������� - "0", ��� ������ "-1", ��� ������ � ������, ���������� "-2"
/////////////////////////////////////////////////////////////////////////////////
{
//---- ������������� ����������
	Trailing_Symbol = ""; Trailing_OrderComment = "";
	Trailing_OrderTicket = _OrderTicket; Trailing_TrailingStop = TrailingStop; Trailing_BreakEven_After = BreakEven_After; Trailing_OrderType = 0; Trailing_OrderMagicNumber = 0; Trailing_StartTickCount = GetTickCount();
	Trailing_OrderOpenPrice = 0.0; Trailing_OrderStopLoss = 0.0; Trailing_OrderTakeProfit = 0.0; Trailing_New_StopLoss = 0.0; Trailing_Bid = 0.0; Trailing_Ask = 0.0;
	Trailing_OrderExpiration = 0;
	Trailing_Color = _Color;
	Trailing_Result = false;
	StartTime = TimeLocal();

	if ( Trailing_TrailingStop <= 0 ) { return(0); }

	if ( LuftPoints < 0 ) { Trailing_Luft = _Spread; } else { Trailing_Luft = LuftPoints * _Point; }

//---- �������� �� ����������, ������� ����� ������ � ����������� ��, ���� ������ ��� ������ ������ - �������
	if ( _TrailingStop_SetValue() == false ) { return(-2); }

//---- ���� ������� �� ����������, ��� �������� �� ���������� ������ �� ���� ��� ���������, �������
	if ( _TrailingStop_NoChange() == false ) { return(0); }

//---- ������� ����������
	_TrailingStop_Info();

//---- ��������� ���������. ���� ���� ������ - �������
	if ( _TrailingStop_Check() == false ) { return(-1); }

//---- ��� ����������� ��������, ����� ����� ��������� ����������, etc... ���� ���� ������ - �������
	int _Check_ = _Check_(7);
	if ( _Check_ < 0 ) { return(_Check_); }

//---- ���� �-��� �������� ������ �������, ��������� ���������� � ��������� ��. ���� ���� ������ - �������
	if ( GetTickCount() - Trailing_StartTickCount > 1000 )
	{
		if ( _TrailingStop_RefreshValue() == false ) { TradeIsNotBusy(); return(-2); }
		if ( _TrailingStop_Check() == false ) { TradeIsNotBusy(); return(-1); }
	}

//---- ����������, �������
	Trailing_Result = OrderModify( Trailing_OrderTicket, Trailing_OrderOpenPrice, Trailing_New_StopLoss, Trailing_OrderTakeProfit, Trailing_OrderExpiration, Trailing_Color );
	Trailing_GetLastError = GetLastError();

//---- ���� ���� ������,
	if ( !Trailing_Result || Trailing_GetLastError > 0 )
	{
//---- ����� �� ��������� ��� ������,
		Processing_Error ( Trailing_GetLastError, "TrailingStop" );
//---- ����������� �������� �����
		TradeIsNotBusy();
//---- ������� ���������� � _TradeLog, � ������,
		_Return_ ( 7, "Error", Trailing_GetLastError, ErrorDescription( Trailing_GetLastError ), "OrderModify(...)", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
//---- � �������, ��������� -1.
		return(-1);
	}
//---- ���� �� ������,
//---- ����������� �������� �����
	TradeIsNotBusy();

//---- ��������� - ������������� �� ��������� �������� �, ���� ���, ������� ���������� � �������, ��������� -4.
	for ( int x = 0; x < 5; x ++ )
	{
		Sleep(1000);
		if ( !OrderSelect( Trailing_OrderTicket, SELECT_BY_TICKET ) )
		{
			Trailing_GetLastError = GetLastError();
			_Print_ ( 7, "OrderSelect( " + Trailing_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
			continue;
		}
		if ( NormalizeDouble( Trailing_OrderStopLoss, _Digits ) != NormalizeDouble( OrderStopLoss(), _Digits ) )
		{ break; }
		else
		{
			Processing_Error ( 0, "TrailingStop" );
			_Return_ ( 7, "Error", 0, "�������� �� ��� �������������", "OrderModify(...)", "�������� �� ��� �������������" );
			return(-4);
		}
	}

//---- ������ �������� � ����������,
	_TrailingStop_SetArrow();

//---- ������� ����������,
	_info ( 14, stringSuccessfully );
	_Return_ ( 7, "OK", 0, "", "OrderModify(...)", "OK" );

//---- � �������, ��������� 1.
return(1);
}

bool _TrailingStop_SetValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Trailing_OrderTicket, SELECT_BY_TICKET ) )
	{
		Trailing_GetLastError = GetLastError();
		_Return_ ( 7,  "Error", Trailing_GetLastError, ErrorDescription( Trailing_GetLastError ), "OrderSelect( " + Trailing_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
		return(false);
	}

	Trailing_Symbol				= OrderSymbol();
	Trailing_OrderComment		= OrderComment();
	Trailing_OrderType			= OrderType();
	Trailing_OrderMagicNumber	= OrderMagicNumber();
	Trailing_OrderOpenPrice		= NormalizeDouble ( OrderOpenPrice(), _Digits );
	Trailing_OrderStopLoss		= NormalizeDouble ( OrderStopLoss(), _Digits );
	Trailing_OrderTakeProfit	= NormalizeDouble ( OrderTakeProfit(), _Digits );
	Trailing_OrderExpiration	= OrderExpiration();

	Trailing_Bid					= NormalizeDouble( MarketInfo ( Trailing_Symbol, MODE_BID ), _Digits );
	Trailing_Ask					= NormalizeDouble( MarketInfo ( Trailing_Symbol, MODE_ASK ), _Digits );

//---- ���� ���� �� ���������, ������������� OrderSellColor(��� "��������" �������) ��� OrderBuyColor(��� "�������" �������)
	if ( Trailing_Color < -1 )
	{
		Trailing_Color = OrderSellColor;
		if ( Trailing_OrderType == OP_BUY || Trailing_OrderType == OP_BUYLIMIT || Trailing_OrderType == OP_BUYSTOP )
		{ Trailing_Color = OrderBuyColor; }
	}
	else
	{
		if ( Trailing_Color < 0 ) Trailing_Color = CLR_NONE;
	}

//---- ������� ���� ������������� ����� ��������
	if ( Trailing_OrderType == OP_BUY ) { Trailing_New_StopLoss = NormalizeDouble( Trailing_Bid - Trailing_TrailingStop * _Point, _Digits ); }
	if ( Trailing_OrderType == OP_SELL ) { Trailing_New_StopLoss = NormalizeDouble( Trailing_Ask + Trailing_TrailingStop * _Point, _Digits ); }

return(true);
}
bool _TrailingStop_NoChange ()
{
	if ( Trailing_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Trailing_Bid - Trailing_OrderOpenPrice, _Digits ) <= NormalizeDouble( Trailing_BreakEven_After * _Point, _Digits ) ) { return(false); }
		if ( NormalizeDouble( Trailing_OrderStopLoss, _Digits ) > 0 && NormalizeDouble( Trailing_New_StopLoss - Trailing_OrderStopLoss, _Digits ) <= NormalizeDouble( Trailing_Luft, _Digits ) ) { return(false); }
	}
	if ( Trailing_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Trailing_OrderOpenPrice - Trailing_Ask, _Digits ) <= NormalizeDouble( Trailing_BreakEven_After * _Point, _Digits ) ) { return(false); }
		if ( NormalizeDouble( Trailing_OrderStopLoss, _Digits ) > 0 && NormalizeDouble( Trailing_OrderStopLoss - Trailing_New_StopLoss, _Digits ) <= NormalizeDouble( Trailing_Luft, _Digits ) ) { return(false); }
	}
return(true);
}
void _TrailingStop_Info ()
{
	if ( !Allow_Info && !Allow_LogFile ) { return(0); }

	_FileWrite ( "- - - - - - - - - - - - - - - - TrailingStop Start - - - - - - - - - - - - - - - - - -" );

	_info ( 10, "Trailing Stop (" + Trailing_TrailingStop + " points) ��� ������� � " + Trailing_OrderTicket + ", " + strOrderType ( Trailing_OrderType ) + "..." );
	_info ( 1, "Old Stop Loss = " + DoubleToStr( Trailing_OrderStopLoss, _Digits ) );
	_info ( 2, "New Stop Loss = " + DoubleToStr( Trailing_New_StopLoss, _Digits ) );
	_info ( 3, "" );
	_info ( 4, "" );

	_info ( 11, "Comment = " + Trailing_OrderComment, 0, 0 );
	_info ( 12, "MagicNumber = " + Trailing_OrderMagicNumber, 0, 0 );
	_info ( 13, "" );
	_info ( 14, "" );
}
bool _TrailingStop_Check ()
{
	if ( Allow_Info )	{ _info ( 13, stringCheck, 0, 0 ); }

//---- ���� TrailingStop ����� ���������� �� �� ����������, ������� ���������� ����������, �������.
	if ( Trailing_TrailingStop < MarketInfo( Trailing_Symbol, MODE_STOPLEVEL ) + 1 )
	{
		if ( Allow_Info ) { _info ( 13, "��������� ���������.....������ ���������� �������� ��� ������!", 1, 0 ); }
		if ( Allow_LogFile ) { _FileWrite( "������ ���������� �������� ��� ������! TrailingStop �������..." ); }
		_Return_ ( 7, "Error", 0, "������ ���������� �������� ��� ������", "_TrailingStop_Check()", "������ ���������� �������� ��� ������" );
		return(false);
	}

	if ( Trailing_OrderType > OP_SELL )
	{
		if ( Allow_Info || Allow_LogFile )
		{
			_info ( 13, stringCheckError, 1, 0 );
			_info ( 14, "��������� OrderType() - " + Trailing_OrderType + ". �-��� TrailingStop ������������� ������ ��� OP_BUY ��� OP_SELL �������.", 1 );
		}
		_Return_ ( 7, "Error", 0, "��������� OrderType()", "_TrailingStop_Check()", "��������� OrderType()" );
		return(false);
	}

	if ( Allow_Info ) { _info ( 13, stringCheckOK, 0, 0 ); }

return(true);
}
bool _TrailingStop_RefreshValue ()
{
//---- �������� �����, � ���� ��������� ������ - �������
	if ( !OrderSelect( Trailing_OrderTicket, SELECT_BY_TICKET ) )
	{
		Trailing_GetLastError = GetLastError();
		_Return_ ( 7,  "Error", Trailing_GetLastError, ErrorDescription( Trailing_GetLastError ), "OrderSelect( " + Trailing_OrderTicket + ", SELECT_BY_TICKET )", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
		return(false);
	}

	Trailing_Symbol				= OrderSymbol();
	Trailing_OrderComment		= OrderComment();
	Trailing_OrderType			= OrderType();
	Trailing_OrderMagicNumber	= OrderMagicNumber();
	Trailing_OrderOpenPrice		= NormalizeDouble ( OrderOpenPrice(), _Digits );
	Trailing_OrderStopLoss		= NormalizeDouble ( OrderStopLoss(), _Digits );
	Trailing_OrderTakeProfit	= NormalizeDouble ( OrderTakeProfit(), _Digits );
	Trailing_OrderExpiration	= OrderExpiration();

	Trailing_Bid					= NormalizeDouble( MarketInfo ( Trailing_Symbol, MODE_BID ), _Digits );
	Trailing_Ask					= NormalizeDouble( MarketInfo ( Trailing_Symbol, MODE_ASK ), _Digits );

	bool refreshed = false;
//---- ��������� ����� ������� ���������
	if ( Trailing_OrderType == OP_BUY )
	{
		if ( NormalizeDouble( Trailing_New_StopLoss, _Digits ) != NormalizeDouble( Trailing_Bid - Trailing_TrailingStop * _Point, _Digits ) )
		{
			Trailing_New_StopLoss = NormalizeDouble( Trailing_Bid - Trailing_TrailingStop * _Point, _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - TrailingStop Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 2, "Refreshed New Stop Loss = " + DoubleToStr( Trailing_New_StopLoss, _Digits ) );
			}
		}
	}
	if ( Trailing_OrderType == OP_SELL )
	{
		if ( NormalizeDouble( Trailing_New_StopLoss, _Digits ) != NormalizeDouble( Trailing_Ask + Trailing_TrailingStop * _Point, _Digits ) )
		{
			Trailing_New_StopLoss = NormalizeDouble( Trailing_Ask + Trailing_TrailingStop * _Point, _Digits );
			if ( Allow_Info || Allow_LogFile )
			{
				if ( !refreshed ) { _FileWrite ( "- - - - - - - - - - - - - - - - TrailingStop Refresh  - - - - - - - - - - - - - - - - -" ); refreshed = true; }
				_info ( 2, "Refreshed New Stop Loss = " + DoubleToStr( Trailing_New_StopLoss, _Digits ) );
			}
		}
	}
return(true);
}
void _TrailingStop_SetArrow ()
{
	if ( Trailing_Color == CLR_NONE ) { return(0); }

	string arrow_description = Trailing_OrderComment + "\nId " + Trailing_OrderMagicNumber + "\nModified by TrailingStop";

	string sl_name = "#" + Trailing_OrderTicket + " sl modified ";
	string new_arrow = "#" + Trailing_OrderTicket + " old sl" + TimeToStr( Time[1] );

	ObjectSetText( sl_name, arrow_description, 10 );
	GetLastError();
/*	if ( ObjectCreate( new_arrow, OBJ_ARROW, 0, Time[1], Trailing_OrderStopLoss ) )
	{
		if ( !ObjectSet( new_arrow, OBJPROP_ARROWCODE, 4 ) )
		{
			Trailing_GetLastError = GetLastError();
			_Print_ ( 7, "ObjectSet( " + new_arrow + ", OBJPROP_ARROWCODE, 4 )", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
		}
		if ( !ObjectSetText( new_arrow, arrow_description, 10 ) )
		{
			Trailing_GetLastError = GetLastError();
			_Print_ ( 7, "ObjectSetText( " + new_arrow + ", " + arrow_description + ", 10 )", "Error #" + Trailing_GetLastError + " ( " + ErrorDescription( Trailing_GetLastError ) + " )" );
		}
	}*/
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _Check_( int FunctionId )
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(0); }
	int _return = 0;
//---- ��������� ���������� � ��������. ���� IsConnected == false - �������
	int _IsConnected = _IsConnected();
	if ( _IsConnected < 0 )
	{
		if ( _IsConnected == -1 ) { _return = -300; _Return_( FunctionId, "Error", 0, stringNoConnection, "_IsConnected()", stringNoConnection ); }
		if ( _IsConnected == -2 ) { _return = -302; _Return_( FunctionId, "Error", 0, stringStopped, "_IsConnected()", stringStopped ); }
		if ( _IsConnected == -3 ) { _return = -303; _Return_( FunctionId, "Error", 0, stringTimeOut, "_IsConnected()", stringTimeOut ); }
		return(_return);
	}

//---- ��������� IsTradeAllowed. ���� IsTradeAllowed == false - �������
	int _IsTradeAllowed = _IsTradeAllowed();
	if ( _IsTradeAllowed < 0 )
	{
		if ( _IsTradeAllowed == -1 ) { _return = -301; _Return_( FunctionId, "Error", 0, stringTradeNotAllow, "_IsTradeAllowed()", stringTradeNotAllow ); }
		if ( _IsTradeAllowed == -2 ) { _return = -302; _Return_( FunctionId, "Error", 0, stringStopped, "_IsTradeAllowed()", stringStopped ); }
		if ( _IsTradeAllowed == -3 ) { _return = -303; _Return_( FunctionId, "Error", 0, stringTimeOut, "_IsTradeAllowed()", stringTimeOut ); }
		return(_return);
	}

//---- ��� ������������ ��������� ������ � �������� ���
	int _TradeIsBusy = TradeIsBusy();
	if ( _TradeIsBusy < 0 )
	{
		if ( _TradeIsBusy == -2 ) { _return = -302; _Return_( FunctionId, "Error", 0, stringStopped, "TradeIsBusy()", stringStopped ); }
		if ( _TradeIsBusy == -3 ) { _return = -303; _Return_( FunctionId, "Error", 0, stringTimeOut, "TradeIsBusy()", stringTimeOut ); }
		return(_return);
	}

//---- ����� ����� ��������� ����������
	int _PauseBeforeTrade = _PauseBeforeTrade();
	if ( _PauseBeforeTrade < 0 )
	{
		if ( _PauseBeforeTrade == -2 ) { _return = -302; _Return_( FunctionId, "Error", 0, stringStopped, "_PauseBeforeTrade()", stringStopped ); }
		if ( _PauseBeforeTrade == -3 ) { _return = -303; _Return_( FunctionId, "Error", 0, stringTimeOut, "_PauseBeforeTrade()", stringTimeOut ); }
		TradeIsNotBusy();
		return(_return);
	}
return(_return);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _Return_ ( int FunctionId, string TradeLog_Result, int TradeLog_GetLastError, string TradeLog_ErrorDescription, string SubFunction, string SubFunctionResult )
/////////////////////////////////////////////////////////////////////////////////
{
	_TradeLog_ 	( FunctionId, TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription );
	_Print_ 		( FunctionId, SubFunction, SubFunctionResult );

	switch ( FunctionId )
	{
		case 1:  _FileWrite ( "- - - - - - - - - - - - - - - - - OrderSend Finish - - - - - - - - - - - - - - - - - -" ); break;
		case 2:  _FileWrite ( "- - - - - - - - - - - - - - - - - OrderModify Finish - - - - - - - - - - - - - - - - -" ); break;
		case 3:  _FileWrite ( "- - - - - - - - - - - - - - - - - OrderClose Finish  - - - - - - - - - - - - - - - - -" ); break;
		case 4:  _FileWrite ( "- - - - - - - - - - - - - - - - - OrderDelete Finish - - - - - - - - - - - - - - - - -" ); break;
		case 5:	break;
		case 6:  _FileWrite ( "- - - - - - - - - - - - - - - - - - Reverse Finish - - - - - - - - - - - - - - - - - -" ); break;
		case 7:  _FileWrite ( "- - - - - - - - - - - - - - - - - TrailingStop Finish  - - - - - - - - - - - - - - - -" ); break;
		default: Print( "trade_lib&info_lib - _Return_( ", FunctionId, ", ", TradeLog_Result, ", ", TradeLog_GetLastError, ", ", TradeLog_ErrorDescription, ", ", SubFunction, ", ", SubFunctionResult, " ) - Unknown function with Id ", FunctionId );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _TradeLog_( int FunctionId, string TradeLog_Result, int TradeLog_GetLastError, string TradeLog_ErrorDescription )
/////////////////////////////////////////////////////////////////////////////////
{
	switch ( FunctionId )
	{
		case 1:  _TradeLog ( Send_Result, Send_MagicNumber, Send_Comment, "OrderSend ( \"" + Send_Symbol + "\", " + Send_OrderType + ", " + DoubleToStr( Send_Volume, 2 ) + ", " + DoubleToStr( Send_OpenPrice, _Digits ) + ", " + Send_Slippage + ", " + DoubleToStr( Send_StopLoss, _Digits ) + ", " + DoubleToStr( Send_TakeProfit, _Digits ) + ", " + Send_Comment + ", " + Send_MagicNumber + ", " + Send_Expiration + ", " + Send_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Send_StartTickCount ) / 1000 ); break;
		case 2:  _TradeLog ( Modify_OrderTicket, Modify_OrderMagicNumber, Modify_OrderComment, "OrderModify ( " + Modify_OrderTicket + ", " + DoubleToStr( Modify_New_OpenPrice, _Digits ) + ", " + DoubleToStr( Modify_New_StopLoss, _Digits ) + ", " + DoubleToStr( Modify_New_TakeProfit, _Digits ) + ", " + Modify_New_Expiration + ", " + Modify_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Modify_StartTickCount ) / 1000 ); break;
		case 3:  _TradeLog ( Close_OrderTicket, Close_OrderMagicNumber, Close_OrderComment, "OrderClose( " + Close_OrderTicket + ", " + DoubleToStr( Close_Volume, 2 ) + ", " + Close_Slippage + ", " + Close_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Close_StartTickCount ) / 1000 ); break;
		case 4:  _TradeLog ( Delete_OrderTicket, Delete_OrderMagicNumber, Delete_OrderComment, "OrderDelete( " + Delete_OrderTicket + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Delete_StartTickCount ) / 1000 ); break;
		case 5:  _TradeLog ( Close_OrderTicket, Close_OrderMagicNumber, Close_OrderComment, "Reverse( " + Close_OrderTicket + ", " + Close_Slippage + ", " + Close_Color + ", " + Send_Color + " )" + " - - - " + "OrderClose( " + Close_OrderTicket + ", " + DoubleToStr( Close_Volume, 2 ) + ", " + Close_Slippage + ", " + Close_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Close_StartTickCount ) / 1000 ); break;
		case 6:  _TradeLog ( Send_Result, Send_MagicNumber, Send_Comment, "Reverse( " + Close_OrderTicket + ", " + Close_Slippage + ", " + Close_Color + ", " + Send_Color + " )" + " - - - " + "OrderSend ( \"" + Send_Symbol + "\", " + Send_OrderType + ", " + DoubleToStr( Send_Volume, 2 ) + ", " + DoubleToStr( Send_OpenPrice, _Digits ) + ", " + Send_Slippage + ", " + DoubleToStr( Send_StopLoss, _Digits ) + ", " + DoubleToStr( Send_TakeProfit, _Digits ) + ", " + Send_Comment + ", " + Send_MagicNumber + ", " + Send_Expiration + ", " + Send_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Close_StartTickCount ) / 1000 ); break;
		case 7:  _TradeLog ( Trailing_OrderTicket, Trailing_OrderMagicNumber, Trailing_OrderComment, "TrailingStop ( " + Trailing_OrderTicket + ", " + Trailing_TrailingStop + ", " + Trailing_Color + " )", TradeLog_Result, TradeLog_GetLastError, TradeLog_ErrorDescription, ( GetTickCount() - Trailing_StartTickCount ) / 1000 ); break;
		default: Print( "trade_lib&info_lib - _TradeLog_( ", FunctionId, ", ", TradeLog_Result, ", ", TradeLog_GetLastError, ", ", TradeLog_ErrorDescription, " ) - Unknown function with Id ", FunctionId );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _Print_( int FunctionId, string SubFunction, string SubFunctionResult )
/////////////////////////////////////////////////////////////////////////////////
{
	switch ( FunctionId )
	{
		case 1:  Print( "trade_lib&info_lib - _OrderSend ( ", Send_Symbol, ", ", Send_OrderType, ", ", DoubleToStr( Send_Volume, 2 ), ", ", DoubleToStr( Send_OpenPrice, _Digits ), ", ", Send_Slippage, ", ", DoubleToStr( Send_StopLoss, _Digits ), ", ", DoubleToStr( Send_TakeProfit, _Digits ), ", ", Send_Comment, ", ", Send_MagicNumber, ", ", Send_Expiration, ", ", Send_Color, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Send_StartTickCount ) / 1000, " sec." ); break;
		case 2:  Print( "trade_lib&info_lib - _OrderModify ( ", Modify_OrderTicket, ", ", DoubleToStr( Modify_New_OpenPrice, _Digits ), ", ", DoubleToStr( Modify_New_StopLoss, _Digits ), ", ", DoubleToStr( Modify_New_TakeProfit, _Digits ), ", ", Modify_New_Expiration, ", ", Modify_Color, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Modify_StartTickCount ) / 1000, " sec." ); break;
		case 3:  Print( "trade_lib&info_lib - _OrderClose( ", Close_OrderTicket, ", ", Close_Volume, ", ", Close_Slippage, ", ", Close_Color, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Close_StartTickCount ) / 1000, " sec." ); break;
		case 4:  Print( "trade_lib&info_lib - _OrderDelete( ", Delete_OrderTicket, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Delete_StartTickCount ) / 1000, " sec." ); break;
		case 5:  Print( "trade_lib&info_lib - _Reverse( " + Close_OrderTicket + ", " + Close_Slippage + ", " + Close_Color + ", " + Send_Color + " ) - - - " + "OrderClose( ", Close_OrderTicket, ", ", Close_Volume, ", ", Close_Slippage, ", ", Close_Color, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Close_StartTickCount ) / 1000, " sec." ); break;
		case 6:  Print( "trade_lib&info_lib - _Reverse( " + Close_OrderTicket + ", " + Close_Slippage + ", " + Close_Color + ", " + Send_Color + " ) - - - " + "OrderSend ( ", Send_Symbol, ", ", Send_OrderType, ", ", DoubleToStr( Send_Volume, 2 ), ", ", DoubleToStr( Send_OpenPrice, _Digits ), ", ", Send_Slippage, ", ", DoubleToStr( Send_StopLoss, _Digits ), ", ", DoubleToStr( Send_TakeProfit, _Digits ), ", ", Send_Comment, ", ", Send_MagicNumber, ", ", Send_Expiration, ", ", Send_Color, " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Close_StartTickCount ) / 1000, " sec." ); break;
		case 7:  Print( "trade_lib&info_lib - _TrailingStop ( " + Trailing_OrderTicket + ", " + Trailing_TrailingStop + ", " + Trailing_Color + " ) - ", SubFunction, " - ", SubFunctionResult, ". Work time: ", ( GetTickCount() - Trailing_StartTickCount ) / 1000, " sec." ); break;
		default: Print( "trade_lib&info_lib - _Print_( ", FunctionId, ", ", SubFunction, ", ", SubFunctionResult, " ) - Unknown function with Id ", FunctionId );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int TradeIsBusy()
/////////////////////////////////////////////////////////////////////////////////
// ������������� ���������� ���������� TradeIsBusy � 1
// ���� �������, ���������� 1
// ���� _IsStopped(), ���������� -2
// ���� _IsTimeOut(), ���������� -3
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(1); }
	int _GetLastError;

	while( true )
	{
		if ( _IsStopped() ) { return(-2); }
		if ( _IsTimeOut() ) { return(-3); }
		if ( GlobalVariableCheck( "TradeIsBusy" ) ) { break; }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - TradeIsBusy() - GlobalVariableCheck ( \"TradeIsBusy\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			Sleep(100);
			continue;
		}

		if ( GlobalVariableSet ( "TradeIsBusy", 1.0 ) > 0 ) { return(1); }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - TradeIsBusy() - GlobalVariableSet ( \"TradeIsBusy\", 0.0 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			Sleep(100);
			continue;
		}
		Sleep(100);
	}
	while( true )
	{
		if ( _IsStopped() ) { return(-2); }
		if ( _IsTimeOut() ) { return(-3); }
		if ( GlobalVariableSetOnCondition( "TradeIsBusy", 1.0, 0.0 ) ) { return(1); }
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - TradeIsBusy() - GlobalVariableSetOnCondition ( \"TradeIsBusy\", 1.0, 0.0 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}

		_info ( 14, "���, ���� ������ ������� �������� ���������...", 0, 0 );
		Sleep(1000);
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _PauseBeforeTrade ()
/////////////////////////////////////////////////////////////////////////////////
// ���� �� ������� ��������� �������� �������� ������ ������ PauseBeforeTrade ���, ���...
// ������������� ���������� ���������� LastTradeTime � ��������� �����
// ���� �������, ���������� 1
// ���� IsStopped(), ���������� -2
// ���� _IsTimeOut(), ���������� -3
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(1); }
	int _GetLastError;
	GetLastError();

	while( true )
	{
		if ( _IsStopped() ) { TradeIsNotBusy(); return(-2); }
		if ( _IsTimeOut() ) { TradeIsNotBusy(); return(-3); }
		if ( GlobalVariableCheck( "LastTradeTime" ) ) break;
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - _PauseBeforeTrade() - GlobalVariableCheck ( \"LastTradeTime\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}

		if ( GlobalVariableSet ( "LastTradeTime", TimeLocal() ) > 0 )
		{
			_info ( 14, "" );
			return(1);
		}
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 ) Print( "trade_lib&info_lib - _PauseBeforeTrade() - GlobalVariableSet ( \"LastTradeTime\", ", TimeLocal(), " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	
		Sleep(100);
	}

	double _LastTradeTime, RealPauseBeforeTrade, RequirementPauseBeforeTrade;

	while( true )
	{
		if ( _IsStopped() ) { TradeIsNotBusy(); return(-2); }
		if ( _IsTimeOut() ) { TradeIsNotBusy(); return(-3); }

		_LastTradeTime = GlobalVariableGet ( "LastTradeTime" );
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - _PauseBeforeTrade() - GlobalVariableGet ( \"LastTradeTime\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			continue;
		}

		RealPauseBeforeTrade				= TimeLocal() - _LastTradeTime;
		RequirementPauseBeforeTrade	= PauseBeforeTrade;
		if ( RealPauseBeforeTrade < RequirementPauseBeforeTrade )
		{
			_info ( 14, "����� ����� ��������� ����������. �������� " + DoubleToStr( RequirementPauseBeforeTrade - RealPauseBeforeTrade, 0 ) + " ���.", 0, 0 );
			Sleep(1000);
		}
		else
		{
			break;
		}
	}

	while( true )
	{
		if ( _IsStopped() ) { TradeIsNotBusy(); return(-2); }
		if ( _IsTimeOut() ) { TradeIsNotBusy(); return(-3); }
		if ( GlobalVariableSet ( "LastTradeTime", TimeLocal() ) > 0 )
		{
			_info( 14, "" );
			return(1);
		}

		_GetLastError = GetLastError();
		if ( _GetLastError != 0 ) Print( "trade_lib&info_lib - _PauseBeforeTrade() - GlobalVariableSet ( \"LastTradeTime\", ", TimeLocal(), " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );

		Sleep(100);
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void TradeIsNotBusy ()
/////////////////////////////////////////////////////////////////////////////////
// ������������� ���������� ���������� TradeIsBusy � 0
// �������� "�� �����", �.�. ���� �� ��������
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(0); }
	int _GetLastError;
	GetLastError();
	while( true )
	{
		if ( GlobalVariableCheck( "TradeIsBusy" ) )
		{
			if ( GlobalVariableGet( "TradeIsBusy" ) == 0.0 ) return(1);
			_GetLastError = GetLastError();
			if ( _GetLastError != 0 )
			{
				Print( "trade_lib&info_lib - TradeIsNotBusy() - GlobalVariableGet ( \"TradeIsBusy\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
				continue;
			}

			if ( GlobalVariableSet( "TradeIsBusy", 0.0 ) > 0 ) return(1);
			_GetLastError = GetLastError();
			if ( _GetLastError != 0 )
			{ Print( "trade_lib&info_lib - TradeIsNotBusy() - GlobalVariableSet ( \"TradeIsBusy\", 0.0 ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" ); }

			Sleep(100);
		}
		else
		{
			_GetLastError = GetLastError();
			if ( _GetLastError != 0 )
			{
				Print( "trade_lib&info_lib - TradeIsNotBusy() - GlobalVariableCheck ( \"TradeIsBusy\" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
				continue;
			}
			return(1);
		}
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _IsConnected ()
/////////////////////////////////////////////////////////////////////////////////
// ���� IsConnected() == true, ���������� 1
// ���� IsConnected() == false, ���������� -1
// ���� IsStopped(), ���������� -2
// ���� _IsTimeOut(), ���������� -3
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(1); }
	for ( int z = 0; z < 50; z ++ )
	{
		if ( IsConnected() ) { return(1); }
		if ( _IsStopped() ) { return(-2); }
		if ( _IsTimeOut() ) { return(-3); }
		Sleep(100);
	}
	_info ( 14, stringNoConnection + "!", 1 );
return(-1);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int _IsTradeAllowed ()
/////////////////////////////////////////////////////////////////////////////////
// ���� IsTradeAllowed() == true, ���������� 1
// ���� IsTradeAllowed() == false, ���������� -1
// ���� IsStopped(), ���������� -2
// ���� _IsTimeOut(), ���������� -3
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsTesting() ) { return(1); }
	for ( int z = 0; z < 50; z ++ )
	{
		if ( IsTradeAllowed() ) { return(1); }
		if ( _IsStopped() ) { return(-2); }
		if ( _IsTimeOut() ) { return(-3); }
		Sleep(100);
	}
	_info ( 14, stringTradeNotAllow + "!", 1 );
return(-1);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ bool _IsStopped ()
/////////////////////////////////////////////////////////////////////////////////
{
	if ( IsStopped() )
	{
		_info ( 14, stringStopped + "!", 1 );
		return(true);
	}
	return(false);
}
/////////////////////////////////////////////////////////////////////////////////
/**/ bool _IsTimeOut ()
/////////////////////////////////////////////////////////////////////////////////
{
	if ( TimeLocal() - StartTime > MaxWaitingTime && MaxWaitingTime > 0 && StartTime > 0 )
	{
		_info ( 4, stringTimeOut + "!", 1 );
		return(true);
	}
	return(false);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void Processing_Error ( int ErrorCode, string Function )
/////////////////////////////////////////////////////////////////////////////////
{
	int _GetLastError;

/*ERR_ORDER_LOCKED - � ��������� ��������� ������ ������, ������ �� ��������� ����� ������
ERR_OFF_QUOTES - ��� ���, ����� ����� ��������� �������� ����� ��������� ��������������� �����
ERR_PRICE_CHANGED - ���� ���� ����������, ����� ����� ��������� �������� � ����������� �����
ERR_INVALID_PRICE - ����� ������ � �������� � ������������� ������, �� � ����� �������� ���� �� ����� ����. ����� ��������������� � ��������� ��� ��������
ERR_REQUOTE - ������ ������, ���������� ��� �� ��� �������� ����� �������� ���� � ����� �������� �����

*/	string ErrorDescription = ErrorDescription( ErrorCode );
	int ErrorAction = 0;
	if ( ErrorCode == 4   ||
		  ErrorCode == 6	 ||
		  ErrorCode == 8	 ||
		  ErrorCode == 132  ||
		  ErrorCode == 137  ||
		  ErrorCode == 141  ||
		  ErrorCode == 146 ) ErrorAction = 1;

	if ( ErrorCode == 5   ||
		  ErrorCode == 64  ||
		  ErrorCode == 65 ) ErrorAction = 2;

	string ActionStr = "������� ���������� ������...";

	switch ( ErrorAction )
	{
		case 1:
		{
/*			ActionStr = "�������� �� ����� ��������� 5 �����...";
			while ( !IsStopped() )
			{
				if ( GlobalVariableSet ( "LastTradeTime", TimeLocal() + 300 ) > 0 ) { break; }
				_GetLastError = GetLastError();
				Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - GlobalVariableSet ( \"LastTradeTime\", ", TimeLocal() + 300, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
				Sleep(100);
			}
*/			break;
		}
		case 2:
		{
			ActionStr = "������� ���������� ������...";
			while ( !IsStopped() )
			{
				if ( GlobalVariableSet ( strComment + "-return!", -ErrorCode ) > 0 ) { break; }
				_GetLastError = GetLastError();
				Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - GlobalVariableSet ( \"", strComment, "-return!\", ", -ErrorCode, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
				Sleep(100);
			}
			break;
		}
	}

	clear_info();
	_info ( 1, Function + " Error!!!", 1 );
	_info ( 2, "GetLastError = " + ErrorCode, 1 );
	_info ( 3, ErrorDescription, 1 );
	_info ( 4, ActionStr, 1 );

	string subject = strComment + " - Error #" + ErrorCode + " ( " + ErrorDescription + " )!";
	string mail_text = "Acc#" + AccountNumber() + "  " + strComment + " - " + Function + "() Error #" + ErrorCode;
	string log_text =
	"+--------------------Expert-Information----------------------------+\n" +
	"+ ExpertName		= " + ExpertName + "\n" +
	"+ ChartSymbol		= " + _Symbol + "\n" +
	"+ ChartPeriod		= " + strPeriod + "\n" +
	"+------------------------------------------------------------------+\n" +
	"\n" +
	"+--------------------Error-Information-----------------------------+\n" +
	"+ LocalTime		= " + TimeToStr( TimeLocal(), TIME_DATE | TIME_SECONDS ) + "\n" +
	"+ Function		= " + Function + "\n" +
	"+ GetLastError		= " + ErrorCode + "\n" +
	"+ ErrorDescription	= " + ErrorDescription + "\n" +
	"+ Action		= " + ActionStr + "\n" +
	"+------------------------------------------------------------------+\n" +
	"\n" +
	"+--------------------Market-Information----------------------------+\n" +
	"+ Bid			= " + DoubleToStr( MarketInfo( _Symbol, MODE_BID ), _Digits ) + "\n" +
	"+ Ask			= " + DoubleToStr( MarketInfo( _Symbol, MODE_ASK ), _Digits ) + "\n" +
	"+ Spread		= " + DoubleToStr( _Spread, _Digits ) + "\n" +
	"+ StopLevel		= " + DoubleToStr( _StopLevel, _Digits ) + "\n" +
	"+------------------------------------------------------------------+\n" +
	"\n" +
	"+-------------------LastBar-Information----------------------------+\n" +
	"+ Time [0]		= " + TimeToStr( iTime( _Symbol, _Period, 0 ) )  + "\n" +
	"+ Open [0]		= " + DoubleToStr( iOpen( _Symbol, _Period, 0 ), _Digits ) + "\n" +
	"+ High [0]		= " + DoubleToStr( iHigh( _Symbol, _Period, 0 ), _Digits ) + "\n" +
	"+ Low  [0]		= " + DoubleToStr( iLow ( _Symbol, _Period, 0 ), _Digits ) + "\n" +
	"+ Close[0]		= " + DoubleToStr( iClose( _Symbol, _Period, 0 ), _Digits ) + "\n" +
	"+------------------------------------------------------------------+\n" +
	"\n" +
	"+--------------------Server-Information----------------------------+\n" +
	"+ ServerAddress		= " + ServerAddress()		+ "\n" +
	"+ ServerTime		= " + TimeToStr( CurTime(), TIME_DATE | TIME_SECONDS ) + "\n" +
	"+------------------------------------------------------------------+\n" +
	"\n" +
	"+--------------------Account-Information---------------------------+\n" +
	"+ AccountNumber		= " + AccountNumber()								+ "\n" +
	"+ AccountName		= " + AccountName()										+ "\n" +
	"+ AccountEquity		= " + DoubleToStr( AccountEquity(), 2 )		+ "\n" +
	"+ AccountFreeMargin	= " + DoubleToStr( AccountFreeMargin(), 2 )	+ "\n" +
	"+ AccountMargin		= " + DoubleToStr( AccountMargin(), 2 )		+ "\n" +
	"+ \n" +
	"+ AccountBalance	= " + DoubleToStr( AccountBalance(), 2 )			+ "\n" +
	"+ AccountProfit		= " + DoubleToStr( AccountProfit(), 2 )		+ "\n" +
	"+ AccountCredit		= " + DoubleToStr( AccountCredit(), 2 )		+ "\n" +
	"+ AccountCurrency	= " + AccountCurrency()								+ "\n" +
	"+ AccountLeverage	= " + AccountLeverage()								+ "\n" +
	"+------------------------------------------------------------------+\n";

	if ( Allow_ErrorMail && !IsTesting() )
	{
		if ( IsConnected() )
		{
			SendMail( mail_text, "" );
			_GetLastError = GetLastError();
			if ( _GetLastError > 0 )
			{ Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - SendMail ( ", subject, ", ", mail_text, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" ); }
		}
		else
		{
			Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - No connection with server. SendMail canceled" );
		}
	}
	
	if ( Allow_ErrorLogFile )
	{
		string file_name = "_ErrorLogs\\" + strComment + "\\" + TimeToStr( TimeLocal(), TIME_DATE ) + ".txt";
		int file_handle = FileOpen ( file_name, FILE_READ | FILE_WRITE, " " );
	
		if ( file_handle > 0 )
		{
			if ( FileSeek ( file_handle, 0, SEEK_END ) )
			{
				if ( FileWrite ( file_handle, subject + "\n", log_text, "\n\n" ) < 0 )
				{
					_GetLastError = GetLastError();
					Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - FileWrite (...) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
				}
			}
			else
			{
				_GetLastError = GetLastError();
				Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - FileSeek ( " + file_handle + ", 0, SEEK_END ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			}
			FileClose ( file_handle );
		}
		else
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - Processing_Error( ", ErrorCode, ", ", Function, " ) - FileOpen( ", file_name, ", FILE_READ | FILE_WRITE, \" \" ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		}
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void _TradeLog ( int Ticket, int MagicNumber, string _Comment, string Action, string Result = "OK", int Error = 0, string ErrorDescription = "", int WorkTime = 0 )
/////////////////////////////////////////////////////////////////////////////////
// ������ � ���-���� ���������� � �������� ��������.
//
// ��� ������������� � �������������� ������� ����.
// � �������� ��� ( ��� �������: ���� ������ - 2005.08.03):
// ...\MetaTrader 4\experts\files\_TradeLog\2005.08.03.csv
// ��� ��������������� ����� ����� ������������ ������ TradeLog_Format, ���������� ��� � "���������" ���.
// ���������� �� ������������� � ��������������� �����.
/////////////////////////////////////////////////////////////////////////////////
{
	if ( !Allow_TradeLogFile ) { return(0); }

	int _GetLastError;

	string file_name = "_TradeLogs\\" + TimeToStr( TimeLocal(), TIME_DATE ) + ".csv";
	int file_handle = FileOpen ( file_name, FILE_READ | FILE_WRITE );
	if ( file_handle > 0 )
	{
		if ( FileSeek ( file_handle, 0, SEEK_END ) )
		{
			string Error_str = DoubleToStr( Error, 0 );
			if ( FileWrite ( file_handle, TimeToStr( TimeLocal(), TIME_SECONDS ), ExpertName, _Symbol, _Period, "( " + strPeriod + " )", Ticket, MagicNumber, _Comment, Action, Result, ErrorDescription, Error_str, WorkTime ) < 0 )
			{
				_GetLastError = GetLastError();
				Print( "trade_lib&info_lib - _TradeLog ( ", Ticket, ", ", MagicNumber, ", ", _Comment, ", ", Action, ", ", Result, ", ", Error, " ) - FileWrite ( ", TimeToStr( TimeLocal(), TIME_SECONDS ), ", ", ExpertName, ", ", _Symbol, ", ", _Period, ", ( " + strPeriod + " ), ", Ticket, ", ", MagicNumber, ", ", _Comment, ", ", Action, ", ", Result, ", ", Error, ", ", ErrorDescription, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			}
		}
		else
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - _TradeLog ( ", Ticket, ", ", MagicNumber, ", ", _Comment, ", ", Action, ", ", Result, ", ", Error, " ) - FileSeek ( " + file_handle + ", 0, SEEK_END ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		}
		FileClose ( file_handle );
	}
	else
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - _TradeLog ( ", Ticket, ", ", MagicNumber, ", ", _Comment, ", ", Action, ", ", Result, ", ", Error, " ) - FileOpen( \"", file_name, "\", FILE_READ | FILE_WRITE ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ void ReportToCSV( string file_name = "", int magic = -1, string comment = "-1", string symbol = "-1", int magic1 = -1 )
/////////////////////////////////////////////////////////////////////////////////
{
	int Tickets[], _HistoryTotal = OrdersHistoryTotal(), _HistoryValidate = 0, _GetLastError;
	ArrayResize( Tickets, _HistoryTotal );

	for ( int i = 0; i < _HistoryTotal; i++ )
	{
		if ( !OrderSelect( i, SELECT_BY_POS, MODE_HISTORY ) ) continue;
		if ( symbol != "-1" && OrderSymbol() != _Symbol ) continue;
		if ( comment != "-1" && StringFind( OrderComment(), comment, 0 ) < 0 ) continue;
		if ( magic != -1 && OrderMagicNumber() != magic )
		{
			if ( magic1 != -1 && OrderMagicNumber() != magic1 ) continue;
		}

		Tickets[_HistoryValidate] = OrderTicket();
		_HistoryValidate ++;
	}

	if ( _HistoryValidate <= 0 )
	{
		Print( "ReportToCSV: ��� �������, ���������� ��������!" );
		return;
	}

	ArrayResize( Tickets, _HistoryValidate );
	ArraySort( Tickets );

	if ( file_name == "" ) file_name = "_Reports\\" + strComment + ".csv";
	int file_handle = FileOpen ( file_name, FILE_WRITE );
	if ( file_handle < 0 )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - ReportToCSV ( ", magic, ", ", comment, " ) - FileOpen( \"", file_name, "\", FILE_WRITE ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		return(-1);
	}

	FileWrite( file_handle, 
		"Ticket", 
		"OpenTime", 
		"Type", 
		"Lots", 
		"Symbol", 
		"OpenPrice", 
		"StopLoss", 
		"TakeProfit", 
		"CloseTime", 
		"ClosePrice", 
		"Swap", 
		"Profit", 
		"Comment" );

	for ( i = 0; i < _HistoryValidate; i++ )
	{
		if ( !OrderSelect( Tickets[i], SELECT_BY_TICKET ) ) continue;
		FileWrite( file_handle, 
			Tickets[i], 
			TimeToStr( OrderOpenTime() ), 
			strOrderType( OrderType() ), 
			DoubleToStr( OrderLots(), 2 ), 
			Symbol(), 
			DoubleToStr( OrderOpenPrice()	, _Digits ), 
			DoubleToStr( OrderStopLoss()	, _Digits ), 
			DoubleToStr( OrderTakeProfit(), _Digits ), 
			TimeToStr( OrderCloseTime() ), 
			DoubleToStr( OrderClosePrice(), _Digits ), 
			DoubleToStr( OrderSwap(), 2 ), 
			DoubleToStr( OrderProfit(), 2 ), 
			OrderComment() );
	}
	FileClose( file_handle );
}

/////////////////////////////////////////////////////////////////////////////////
/**/ string strOrderType( int intOrderType )
/////////////////////////////////////////////////////////////////////////////////
// ���������� OrderType � ���� ������
/////////////////////////////////////////////////////////////////////////////////
{
	switch ( intOrderType )
	{
		case OP_BUY:			return("Buy"					);
		case OP_SELL:			return("Sell"					);
		case OP_BUYLIMIT:		return("BuyLimit"				);
		case OP_BUYSTOP:		return("BuyStop"				);
		case OP_SELLLIMIT:	return("SellLimit"			);
		case OP_SELLSTOP:		return("SellStop"				);
		default:					return("UnknownOrderType"	);
	}
}
/////////////////////////////////////////////////////////////////////////////////
/**/ int intOrderType( string strOrderType )
/////////////////////////////////////////////////////////////////////////////////
// ���������� OrderType � ���� ������
/////////////////////////////////////////////////////////////////////////////////
{
	if ( strOrderType == "Buy"			) { return(OP_BUY			); }
	if ( strOrderType == "Sell"		) { return(OP_SELL		); }
	if ( strOrderType == "BuyLimit"	) { return(OP_BUYLIMIT	); }
	if ( strOrderType == "BuyStop"	) { return(OP_BUYSTOP	); }
	if ( strOrderType == "SellLimit"	) { return(OP_SELLLIMIT	); }
	if ( strOrderType == "SellStop"	) { return(OP_SELLSTOP	); }
	return(-1);
}
/////////////////////////////////////////////////////////////////////////////////
/**/ int intOrderType2( string strOrderType )
/////////////////////////////////////////////////////////////////////////////////
// ���������� OrderType � ���� ������
/////////////////////////////////////////////////////////////////////////////////
{
	if ( strOrderType == "Buy"			) { return(OP_BUY			); }
	if ( strOrderType == "Sell"		) { return(OP_SELL		); }
	if ( strOrderType == "Buy-limit"	) { return(OP_BUYLIMIT	); }
	if ( strOrderType == "Buy-stop"	) { return(OP_BUYSTOP	); }
	if ( strOrderType == "Sell-limit") { return(OP_SELLLIMIT	); }
	if ( strOrderType == "Sell-stop"	) { return(OP_SELLSTOP	); }
	return(-1);
}

/////////////////////////////////////////////////////////////////////////////////
/**/ string strPeriod( int intPeriod )
/////////////////////////////////////////////////////////////////////////////////
// ���������� Period � ���� ������
/////////////////////////////////////////////////////////////////////////////////
{
	switch ( intPeriod )
	{
		case PERIOD_MN1: return("Monthly");
		case PERIOD_W1:  return("Weekly");
		case PERIOD_D1:  return("Daily");
		case PERIOD_H4:  return("H4");
		case PERIOD_H1:  return("H1");
		case PERIOD_M30: return("M30");
		case PERIOD_M15: return("M15");
		case PERIOD_M5:  return("M5");
		case PERIOD_M1:  return("M1");
		default:		     return("UnknownPeriod");
	}
}

/////////////////////////////////////////////////////////////////////////////////
/**/ int intPeriod( string strPeriod )
/////////////////////////////////////////////////////////////////////////////////
// ���������� Period � ���� ������ �����
/////////////////////////////////////////////////////////////////////////////////
{
	if ( strPeriod == "Monthly"	)	{ return(PERIOD_MN1	); }
	if ( strPeriod == "Weekly"		)	{ return(PERIOD_W1	); }
	if ( strPeriod == "Daily"		)	{ return(PERIOD_D1	); }
	if ( strPeriod == "H4"			)	{ return(PERIOD_H4	); }
	if ( strPeriod == "H1"			)	{ return(PERIOD_H1	); }
	if ( strPeriod == "M30"			)	{ return(PERIOD_M30	); }
	if ( strPeriod == "M15"			)	{ return(PERIOD_M15	); }
	if ( strPeriod == "M5"			)	{ return(PERIOD_M5	); }
	if ( strPeriod == "M1"			)	{ return(PERIOD_M1	); }
	return(-1);
}


//---- �-��� �� stdlib.mq4 ����������� 10.08.2006 (build 208)
string ErrorDescription(int error_code)
  {
   string error_string;
//----
   switch(error_code)
     {
      //---- codes returned from trade server
      case 0:
      case 1:   error_string="no error";                                                  break;
      case 2:   error_string="common error";                                              break;
      case 3:   error_string="invalid trade parameters";                                  break;
      case 4:   error_string="trade server is busy";                                      break;
      case 5:   error_string="old version of the client terminal";                        break;
      case 6:   error_string="no connection with trade server";                           break;
      case 7:   error_string="not enough rights";                                         break;
      case 8:   error_string="too frequent requests";                                     break;
      case 9:   error_string="malfunctional trade operation (never returned error)";      break;
      case 64:  error_string="account disabled";                                          break;
      case 65:  error_string="invalid account";                                           break;
      case 128: error_string="trade timeout";                                             break;
      case 129: error_string="invalid price";                                             break;
      case 130: error_string="invalid stops";                                             break;
      case 131: error_string="invalid trade volume";                                      break;
      case 132: error_string="market is closed";                                          break;
      case 133: error_string="trade is disabled";                                         break;
      case 134: error_string="not enough money";                                          break;
      case 135: error_string="price changed";                                             break;
      case 136: error_string="off quotes";                                                break;
      case 137: error_string="broker is busy (never returned error)";                     break;
      case 138: error_string="requote";                                                   break;
      case 139: error_string="order is locked";                                           break;
      case 140: error_string="long positions only allowed";                               break;
      case 141: error_string="too many requests";                                         break;
      case 145: error_string="modification denied because order too close to market";     break;
      case 146: error_string="trade context is busy";                                     break;
      case 147: error_string="expirations are denied by broker";                          break;
      case 148: error_string="amount of open and pending orders has reached the limit";   break;
      //---- mql4 errors
      case 4000: error_string="no error (never generated code)";                                                 break;
      case 4001: error_string="wrong function pointer";                                   break;
      case 4002: error_string="array index is out of range";                              break;
      case 4003: error_string="no memory for function call stack";                        break;
      case 4004: error_string="recursive stack overflow";                                 break;
      case 4005: error_string="not enough stack for parameter";                           break;
      case 4006: error_string="no memory for parameter string";                           break;
      case 4007: error_string="no memory for temp string";                                break;
      case 4008: error_string="not initialized string";                                   break;
      case 4009: error_string="not initialized string in array";                          break;
      case 4010: error_string="no memory for array\' string";                             break;
      case 4011: error_string="too long string";                                          break;
      case 4012: error_string="remainder from zero divide";                               break;
      case 4013: error_string="zero divide";                                              break;
      case 4014: error_string="unknown command";                                          break;
      case 4015: error_string="wrong jump (never generated error)";                       break;
      case 4016: error_string="not initialized array";                                    break;
      case 4017: error_string="dll calls are not allowed";                                break;
      case 4018: error_string="cannot load library";                                      break;
      case 4019: error_string="cannot call function";                                     break;
      case 4020: error_string="expert function calls are not allowed";                    break;
      case 4021: error_string="not enough memory for temp string returned from function"; break;
      case 4022: error_string="system is busy (never generated error)";                   break;
      case 4050: error_string="invalid function parameters count";                        break;
      case 4051: error_string="invalid function parameter value";                         break;
      case 4052: error_string="string function internal error";                           break;
      case 4053: error_string="some array error";                                         break;
      case 4054: error_string="incorrect series array using";                             break;
      case 4055: error_string="custom indicator error";                                   break;
      case 4056: error_string="arrays are incompatible";                                  break;
      case 4057: error_string="global variables processing error";                        break;
      case 4058: error_string="global variable not found";                                break;
      case 4059: error_string="function is not allowed in testing mode";                  break;
      case 4060: error_string="function is not confirmed";                                break;
      case 4061: error_string="send mail error";                                          break;
      case 4062: error_string="string parameter expected";                                break;
      case 4063: error_string="integer parameter expected";                               break;
      case 4064: error_string="double parameter expected";                                break;
      case 4065: error_string="array as parameter expected";                              break;
      case 4066: error_string="requested history data in update state";                   break;
      case 4099: error_string="end of file";                                              break;
      case 4100: error_string="some file error";                                          break;
      case 4101: error_string="wrong file name";                                          break;
      case 4102: error_string="too many opened files";                                    break;
      case 4103: error_string="cannot open file";                                         break;
      case 4104: error_string="incompatible access to a file";                            break;
      case 4105: error_string="no order selected";                                        break;
      case 4106: error_string="unknown symbol";                                           break;
      case 4107: error_string="invalid price parameter for trade function";               break;
      case 4108: error_string="invalid ticket";                                           break;
      case 4109: error_string="trade is not allowed in the expert properties";            break;
      case 4110: error_string="longs are not allowed in the expert properties";           break;
      case 4111: error_string="shorts are not allowed in the expert properties";          break;
      case 4200: error_string="object is already exist";                                  break;
      case 4201: error_string="unknown object property";                                  break;
      case 4202: error_string="object is not exist";                                      break;
      case 4203: error_string="unknown object type";                                      break;
      case 4204: error_string="no object name";                                           break;
      case 4205: error_string="object coordinates error";                                 break;
      case 4206: error_string="no specified subwindow";                                   break;
      default:   error_string="unknown error";
     }
//----
   return(error_string);
  }

//+------------------------------------------------------------------+
//| ������������ �������
//+------------------------------------------------------------------+

// ������ �������� ������� ���������� �� ���������� ���
int pre_OrdersArray[][2]; // [���������� �������][� ������, ��� �������]

// ���������� �������
int eventBuyClosed_SL  = 0, eventBuyClosed_TP  = 0;
int eventSellClosed_SL = 0, eventSellClosed_TP = 0;
int eventBuyLimitDeleted_Exp  = 0, eventBuyStopDeleted_Exp  = 0;
int eventSellLimitDeleted_Exp = 0, eventSellStopDeleted_Exp = 0;
int eventBuyLimitOpened  = 0, eventBuyStopOpened  = 0;
int eventSellLimitOpened = 0, eventSellStopOpened = 0;

void CheckEvents( int magic = 0 )
{
	// ���� ������� �������
	static bool first = true;
	// ��� ��������� ������
	int _GetLastError = 0;
	// ����� ���������� �������
	int _OrdersTotal = OrdersTotal();
	// ���-�� �������, ��������������� ��������� (������� ���������� � �������� MagicNumber),
	// ���������� �� ������� ���
	int now_OrdersTotal = 0;
	// ���-�� �������, ��������������� ���������, ���������� �� ���������� ���
	static int pre_OrdersTotal = 0;
	// ������ �������� ������� ���������� �� ������� ���
	int now_OrdersArray[][2]; // [� � ������][� ������, ��� �������]
	// ������� ����� ������� � ������� now_OrdersArray (��� ��������)
	int now_CurOrder = 0;
	// ������� ����� ������� � ������� pre_OrdersArray (��� ��������)
	int pre_CurOrder = 0;

	// ������ ��� �������� ���������� �������� ������� ������� ����
	int now_ClosedOrdersArray[6][3]; // [��� ������][��� ��������]
	// ������ ��� �������� ���������� ����������� ���������� �������
	int now_OpenedPendingOrders[4]; // [��� ������]

	// ��������� �����
	bool OrderClosed = true, PendingOrderOpened = false;
	// ��������� ����������
	int ticket = 0, type = -1, close_type = -1;

	//�������� ���������� �������
	eventBuyClosed_SL  = 0; eventBuyClosed_TP  = 0;
	eventSellClosed_SL = 0; eventSellClosed_TP = 0;
	eventBuyLimitDeleted_Exp  = 0; eventBuyStopDeleted_Exp  = 0;
	eventSellLimitDeleted_Exp = 0; eventSellStopDeleted_Exp = 0;
	eventBuyLimitOpened  = 0; eventBuyStopOpened  = 0;
	eventSellLimitOpened = 0; eventSellStopOpened = 0;

	// �������� ������ ������� �������� ������� ��� ������� ���-��
	ArrayResize( now_OrdersArray, MathMax( _OrdersTotal, 1 ) );
	// �������� ������
	ArrayInitialize( now_OrdersArray, 0.0 );

	// �������� ������� �������� ������� � ����������� �������
	ArrayInitialize( now_ClosedOrdersArray, 0.0 );
	ArrayInitialize( now_OpenedPendingOrders, 0.0 );

	//+------------------------------------------------------------------+
	//| ���������� ��� ������� � ���������� � ������ ������ ��, �������
	//| ������������� ���������
	//+------------------------------------------------------------------+
	for ( int z = _OrdersTotal - 1; z >= 0; z -- )
	{
		if ( !OrderSelect( z, SELECT_BY_POS ) )
		{
			_GetLastError = GetLastError();
			Print( "OrderSelect( ", z, ", SELECT_BY_POS ) - Error #", _GetLastError );
			continue;
		}
		// ������� ���������� ������� �� �������� ������� � � �������� MagicNumber
		if ( OrderMagicNumber() == magic && OrderSymbol() == Symbol() )
		{
			now_OrdersArray[now_OrdersTotal][0] = OrderTicket();
			now_OrdersArray[now_OrdersTotal][1] = OrderType();
			now_OrdersTotal ++;
		}
	}
	// �������� ������ ������� �������� ������� ��� ���-�� �������, ��������������� ���������
	ArrayResize( now_OrdersArray, MathMax( now_OrdersTotal, 1 ) );

	//+------------------------------------------------------------------+
	//| ���������� ������ ������� ����������� ����, � ������� ������� ��������� ������� �
	//| ��������� ���������� �������
	//+------------------------------------------------------------------+
	for ( pre_CurOrder = 0; pre_CurOrder < pre_OrdersTotal; pre_CurOrder ++ )
	{
		// ���������� ����� � ��� ������
		ticket = pre_OrdersArray[pre_CurOrder][0];
		type   = pre_OrdersArray[pre_CurOrder][1];
		// ������������, ��� ���� ��� �������, �� ��� ���������
		OrderClosed = true;
		// ������������, ��� ���� ��� ��� ���������� �����, �� �� �� ��������
		PendingOrderOpened = false;

		// ���������� ��� ������� �� �������� ������ �������� �������
		for ( now_CurOrder = 0; now_CurOrder < now_OrdersTotal; now_CurOrder ++ )
		{
			// ���� ������� � ����� ������� ���� � ������,
			if ( ticket == now_OrdersArray[now_CurOrder][0] )
			{
				// ������ ������� �� ���� ������� (����� �� ��� �����)
				OrderClosed = false;

				// ���� � ��� ���������,
				if ( type != now_OrdersArray[now_CurOrder][1] )
				{
					// ������ ��� ��� ���������� �����, � �� ��������
					PendingOrderOpened = true;
				}
				break;
			}
		}
		// ���� ���� ������� ������� (����� �����),
		if ( OrderClosed )
		{
			// �������� �
			if ( !OrderSelect( ticket, SELECT_BY_TICKET ) )
			{
				_GetLastError = GetLastError();
				Print( "OrderSelect( ", ticket, ", SELECT_BY_TICKET ) - Error #", _GetLastError );
				continue;
			}
			// � ����������, ��� ��������� ������� (�������� �����):
			if ( type < 2 )
			{
				// ��� � ����: 0 - �������, 1 - ��, 2 - ��
				close_type = 0;
				if ( StringFind( OrderComment(), "[sl]" ) >= 0 ) close_type = 1;
				if ( StringFind( OrderComment(), "[tp]" ) >= 0 ) close_type = 2;
			}
			else
			{
				// ���������� ������: 0 - �������, 1 - ����� ���������
				close_type = 0;
				if ( StringFind( OrderComment(), "expiration" ) >= 0 ) close_type = 1;
			}
			
			// � ���������� � ������ �������� �������, ��� ����� � ����� type 
			// �������� ��� ��������������� close_type
			now_ClosedOrdersArray[type][close_type] ++;
			continue;
		}
		// ���� �������� ���������� �����,
		if ( PendingOrderOpened )
		{
			// ���������� � ������ ����������� �������, ��� ����� � ����� type ��������
			now_OpenedPendingOrders[type-2] ++;
			continue;
		}
	}

	//+------------------------------------------------------------------+
	//| ��� ����������� ���������� ������� - ��������� ���������� ������� ������ ��������
	//+------------------------------------------------------------------+
	// ���� ��� �� ������ ������ ��������
	if ( !first )
	{
		// ���������� ��� �������� ������� ������������ ���������� �������
		for ( type = 2; type < 6; type ++ )
		{
			// � ���� ������� �� ������ (����� ������ ���� ��������), ������ �������� ����������
			if ( now_OpenedPendingOrders[type-2] > 0 )
				SetOpenEvent( type );
		}

		// ���������� ��� �������� ������� �������� �������
		for ( type = 0; type < 6; type ++ )
		{
			for ( close_type = 0; close_type < 3; close_type ++ )
			{
				// � ���� ������� �� ������ (���� ������� �������), ������ �������� ����������
				if ( now_ClosedOrdersArray[type][close_type] > 0 )
					SetCloseEvent( type, close_type );
			}
		}
	}
	else
	{
		first = false;
	}

	//---- ��������� ������ ������� ������� � ������ ���������� �������
	ArrayResize( pre_OrdersArray, MathMax( now_OrdersTotal, 1 ) );
	for ( now_CurOrder = 0; now_CurOrder < now_OrdersTotal; now_CurOrder ++ )
	{
		pre_OrdersArray[now_CurOrder][0] = now_OrdersArray[now_CurOrder][0];
		pre_OrdersArray[now_CurOrder][1] = now_OrdersArray[now_CurOrder][1];
	}
	pre_OrdersTotal = now_OrdersTotal;
}
void SetOpenEvent( int SetOpenEvent_type )
{
	switch ( SetOpenEvent_type )
	{
		case OP_BUYLIMIT: eventBuyLimitOpened ++; return(0);
		case OP_BUYSTOP: eventBuyStopOpened ++; return(0);
		case OP_SELLLIMIT: eventSellLimitOpened ++; return(0);
		case OP_SELLSTOP: eventSellStopOpened ++; return(0);
	}
}
void SetCloseEvent( int SetCloseEvent_type, int SetCloseEvent_close_type )
{
	switch ( SetCloseEvent_type )
	{
		case OP_BUY:
		{
			if ( SetCloseEvent_close_type == 1 ) eventBuyClosed_SL ++;
			if ( SetCloseEvent_close_type == 2 ) eventBuyClosed_TP ++;
			return(0);
		}
		case OP_SELL:
		{
			if ( SetCloseEvent_close_type == 1 ) eventSellClosed_SL ++;
			if ( SetCloseEvent_close_type == 2 ) eventSellClosed_TP ++;
			return(0);
		}
		case OP_BUYLIMIT:
		{
			if ( SetCloseEvent_close_type == 1 ) eventBuyLimitDeleted_Exp ++;
			return(0);
		}
		case OP_BUYSTOP:
		{
			if ( SetCloseEvent_close_type == 1 ) eventBuyStopDeleted_Exp ++;
			return(0);
		}
		case OP_SELLLIMIT:
		{
			if ( SetCloseEvent_close_type == 1 ) eventSellLimitDeleted_Exp ++;
			return(0);
		}
		case OP_SELLSTOP:
		{
			if ( SetCloseEvent_close_type == 1 ) eventSellStopDeleted_Exp ++;
			return(0);
		}
	}
}

bool LoadVariables( string& variables[], string file_name = "" )
{
	if ( IsTesting() ) return(true);

	int all = ArraySize( variables ), cur;
	for ( cur = 0; cur < all; cur ++ ) variables[cur] = "";

	if ( file_name == "" ) file_name = StringConcatenate( "_ExpertSaves\\", WindowExpertName(), " (", Symbol(), ", ", strPeriod( Period() ), ", ", _MagicNumber, ").dat" );
	int handle  = FileOpen( file_name, FILE_CSV | FILE_READ ), _GetLastError = 0;
	if ( handle < 0 )
	{
		_GetLastError = GetLastError();
		if ( _GetLastError != 4103 ) Print( "trade_lib&info_lib - LoadVariables() - FileOpen( \"", file_name, "\", FILE_CSV | FILE_READ ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		return(false);
	}

	for ( cur = 0; cur < all; cur ++ )
	{
		variables[cur] = FileReadString( handle );
		_GetLastError = GetLastError();
		if ( _GetLastError != 0 )
		{
			Print( "trade_lib&info_lib - LoadVariables() - FileReadString( ", handle, " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			FileClose( handle );
			return(false);
		}
	}

	FileClose( handle );
	return(true);
}
void SaveVariables( string variables[], string file_name = "" )
{
	if ( IsTesting() ) return(true);

	if ( file_name == "" ) file_name = StringConcatenate( "_ExpertSaves\\", WindowExpertName(), " (", Symbol(), ", ", strPeriod( Period() ), ", ", _MagicNumber, ").dat" );
	int handle  = FileOpen( file_name, FILE_CSV | FILE_WRITE ), _GetLastError = 0;
	if ( handle < 0 )
	{
		_GetLastError = GetLastError();
		Print( "trade_lib&info_lib - SaveVariables() - FileOpen( \"", file_name, "\", FILE_CSV | FILE_WRITE ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
		return(-1);
	}

	int all = ArraySize( variables ), cur;
	for ( cur = 0; cur < all; cur ++ )
	{
		if ( FileWrite( handle, variables[cur] ) < 0 )
		{
			_GetLastError = GetLastError();
			Print( "trade_lib&info_lib - SaveVariables() - FileWrite( ", handle, ", ", variables[cur], " ) - Error #", _GetLastError, " ( ", ErrorDescription( _GetLastError ), " )" );
			break;
		}
	}

	FileClose( handle );
}

