//=====================================================================
//	Управление торговлей с помощью графичекой панели
//=====================================================================

//---------------------------------------------------------------------
#property copyright 	"Dima S., 2010 г."
#property link      	"dimascub@mail.ru"
#property version   	"1.00"
#property description "Trade Control Panel"
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	История версий:
//---------------------------------------------------------------------
//	24.11.2010г. - V1.00
//	 - НАЧАЛЬНАЯ ВЕРСИЯ;
//
//---------------------------------------------------------------------


//---------------------------------------------------------------------
//	Подключаемые библиотеки:
//---------------------------------------------------------------------
#include	<ChartObjects\ChartObjectsBmpControls.mqh>
#include	<ChartObjects\ChartObjectsTxtControls.mqh>
#include	<Trade\Trade.mqh>
//---------------------------------------------------------------------
double		stop_loss, take_profit;
//---------------------------------------------------------------------
CTrade		trade;
//---------------------------------------------------------------------

//=====================================================================
//	Внешние задаваемые параметры:
//=====================================================================
input string	TradeLotsList = "0.01; 0.02; 0.05; 0.10; 0.20; 0.50; 1.00; 2.00; 5.00;";
input double	LosBreakingStopLoss = 10.0;
input int			UpDownBorderShift = 100;
input int			LeftRightBorderShift = 850;
input color		TitlesColor = MediumSpringGreen;
input color		GMTTimeColor = LawnGreen;
input color		LocalTimeColor = Pink;
//---------------------------------------------------------------------


//---------------------------------------------------------------------
string	TradeLotsStr_Array[ ];
double	TradeLots_Array[ ];
int			lots_count;
//---------------------------------------------------------------------

//---------------------------------------------------------------------
CChartObjectBmpLabel*			control_panel1_Ptr = NULL;
CChartObjectBmpLabel*			control_panel2_Ptr = NULL;
CChartObjectBmpLabel*			control_panel3_Ptr = NULL;
CChartObjectBmpLabel*			button_buy_Ptr = NULL;
CChartObjectBmpLabel*			button_sell_Ptr = NULL;
CChartObjectBmpLabel*			button_close_all_Ptr = NULL;
CChartObjectBmpLabel*			button_loss_breaking_Ptr = NULL;
CChartObjectBmpLabel*			button_reverse_Ptr = NULL;
CChartObjectBmpLabel*			button_tradelots_Ptr[ ];
CChartObjectLabel*				trade_lots_display_Ptr[ ];
CChartObjectLabel*				curr_trade_lots_display_Ptr;
CChartObjectLabel*				curr_time_gmt_display_Ptr;
CChartObjectLabel*				curr_time_local_display_Ptr;
CChartObjectLabel*				title_time_gmt_display_Ptr;
CChartObjectLabel*				title_time_local_display_Ptr;
//---------------------------------------------------------------------

//---------------------------------------------------------------------
bool			is_first_init = true;
//---------------------------------------------------------------------
double		current_trade_lots = 0.0;
datetime	curr_gmt_time;
datetime	curr_local_time;


//---------------------------------------------------------------------
#define		WIDTH			128
#define		HEIGHT		128
#define		FONTSIZE	10
//---------------------------------------------------------------------

//---------------------------------------------------------------------
//	Обработчик события инициализации:
//---------------------------------------------------------------------
int
OnInit( )
{
	current_trade_lots = 0.0;

//	Список ТФ:
	lots_count = StringToArrayString( TradeLotsList, TradeLotsStr_Array );
	if( lots_count > 9 )
	{
		lots_count = 9;
	}
	ArrayResize( TradeLots_Array, lots_count );
	for( int k = 0; k < lots_count; k++ )
	{
		TradeLots_Array[ k ] = NormalizeDouble( StringToDouble( TradeLotsStr_Array[ k ] ), 2 );
	}

	if( is_first_init != true )
	{
		DeleteGraphObjects( );
	}
	InitGraphObjects( );
	is_first_init = false;

	EventSetTimer( 1 );
	ChartRedraw( 0 );

	return( 0 );
}

//---------------------------------------------------------------------
//	Обработчик события о поступлении нового тика по текущему символу:
//---------------------------------------------------------------------
void
OnTick( )
{
}

//---------------------------------------------------------------------
//	Обработчик события на графике:
//---------------------------------------------------------------------
void
OnChartEvent( const int _id, const long& lparam, const double& dparam, const string& _sparam )
{
	if( _id == CHARTEVENT_OBJECT_CLICK )
	{
		if( _sparam == button_buy_Ptr.Name( ))
		{
			Sleep( 100 );
			button_buy_Ptr.State( true );
			ChartRedraw( 0 );
			if( current_trade_lots > 0.01 )
			{
				OpenBuy( );
			}
		}
		else if( _sparam == button_sell_Ptr.Name( ))
		{
			Sleep( 100 );
			button_sell_Ptr.State( true );
			ChartRedraw( 0 );
			if( current_trade_lots > 0.01 )
			{
				OpenSell( );
			}
		}
		else if( _sparam == button_close_all_Ptr.Name( ))
		{
			Sleep( 100 );
			button_close_all_Ptr.State( true );
			ChartRedraw( 0 );
			LongPositionClose( );
			ShortPositionClose( );
		}
		else if( _sparam == button_loss_breaking_Ptr.Name( ))
		{
			Sleep( 100 );
			button_loss_breaking_Ptr.State( true );
			ChartRedraw( 0 );
			LossBreakPosition( );
		}
		else if( _sparam == button_reverse_Ptr.Name( ) )
		{
			Sleep( 100 );
			button_reverse_Ptr.State( true );
			ChartRedraw( 0 );
			ReversePosition( );
		}
		else if( StringFind( _sparam, "TradeLotsButton" ) != -1 )
		{
			string	str = StringSubstr( _sparam, StringLen( "TradeLotsButton"));
			int		lots_ind = ( int )StringToInteger( str );
			if( button_tradelots_Ptr[ lots_ind ].State( ) == true )
			{
				current_trade_lots += NormalizeDouble( StringToDouble( TradeLotsStr_Array[ lots_ind ] ), 2 );
			}
			else
			{
				current_trade_lots -= NormalizeDouble( StringToDouble( TradeLotsStr_Array[ lots_ind ] ), 2 );
			}
			current_trade_lots = NormalizeDouble( current_trade_lots, 2 );
			curr_trade_lots_display_Ptr.Description( DoubleToString( current_trade_lots, 2 ) + " lots");
			ChartRedraw( 0 );
		}
	}
}

//---------------------------------------------------------------------
//	Обработчик события де-инициализации:
//---------------------------------------------------------------------
void
OnDeinit( const int _reason )
{
	DeleteGraphObjects( );
}

//---------------------------------------------------------------------
//	Обработчик события от таймера:
//---------------------------------------------------------------------
void
OnTimer( )
{
	curr_gmt_time = TimeGMT( );
	curr_local_time = TimeLocal( );
	if( CheckPointer( curr_time_gmt_display_Ptr ) != POINTER_INVALID )
	{
		curr_time_gmt_display_Ptr.Description( TimeToString( curr_gmt_time, /*TIME_DATE |*/ TIME_MINUTES | TIME_SECONDS ));
	}
	if( CheckPointer( curr_time_local_display_Ptr ) != POINTER_INVALID )
	{
		curr_time_local_display_Ptr.Description( TimeToString( curr_local_time, /*TIME_DATE |*/ TIME_MINUTES | TIME_SECONDS ));
	}

	ChartRedraw( 0 );
}

//---------------------------------------------------------------------
//	Инициализация графических объектов:
//---------------------------------------------------------------------
void
InitGraphObjects( )
{
	control_panel1_Ptr = new CChartObjectBmpLabel( );
	control_panel1_Ptr.Create( 0, "TradeControlPanel1", 0, LeftRightBorderShift - 75, UpDownBorderShift + 60 );
	control_panel1_Ptr.BmpFileOn( "TradeControlPanel1.BMP" );
	control_panel1_Ptr.BmpFileOff( "TradeControlPanel1.BMP" );
	control_panel1_Ptr.State( true );

	control_panel2_Ptr = new CChartObjectBmpLabel( );
	control_panel2_Ptr.Create( 0, "TradeControlPanel2", 0, LeftRightBorderShift + 90, UpDownBorderShift + 60 );
	control_panel2_Ptr.BmpFileOn( "TradeControlPanel2.BMP" );
	control_panel2_Ptr.BmpFileOff( "TradeControlPanel2.BMP" );
	control_panel2_Ptr.State( true );

	control_panel3_Ptr = new CChartObjectBmpLabel( );
	control_panel3_Ptr.Create( 0, "TradeControlPanel3", 0, LeftRightBorderShift - 75, UpDownBorderShift - 5 );
	control_panel3_Ptr.BmpFileOn( "TradeControlPanel3.BMP" );
	control_panel3_Ptr.BmpFileOff( "TradeControlPanel3.BMP" );
	control_panel3_Ptr.State( true );

	title_time_gmt_display_Ptr = new CChartObjectLabel( );
	title_time_gmt_display_Ptr.Create( 0, "TtlGMTTimeLabel", 0, LeftRightBorderShift - 55, UpDownBorderShift + 35 );
	title_time_gmt_display_Ptr.Color(  );
	title_time_gmt_display_Ptr.Font( "Arial" );
	title_time_gmt_display_Ptr.FontSize( 10 );
	title_time_gmt_display_Ptr.Description( "Time GMT:" );

	title_time_local_display_Ptr = new CChartObjectLabel( );
	title_time_local_display_Ptr.Create( 0, "TtlLocalTimeLabel", 0, LeftRightBorderShift + 145, UpDownBorderShift + 35 );
	title_time_local_display_Ptr.Color( Yellow );
	title_time_local_display_Ptr.Font( "Arial" );
	title_time_local_display_Ptr.FontSize( 10 );
	title_time_local_display_Ptr.Description( "Time Local:" );

	curr_gmt_time = TimeGMT( );
	curr_time_gmt_display_Ptr = new CChartObjectLabel( );
	curr_time_gmt_display_Ptr.Create( 0, "GMTTimeLabel", 0, LeftRightBorderShift + 15, UpDownBorderShift + 35 );
	curr_time_gmt_display_Ptr.Color( GMTTimeColor );
	curr_time_gmt_display_Ptr.Font( "Arial" );
	curr_time_gmt_display_Ptr.FontSize( 10 );
	curr_time_gmt_display_Ptr.Description( TimeToString( curr_gmt_time, /*TIME_DATE |*/ TIME_MINUTES | TIME_SECONDS ));

	curr_local_time = TimeLocal( );
	curr_time_local_display_Ptr = new CChartObjectLabel( );
	curr_time_local_display_Ptr.Create( 0, "LocalTimeLabel", 0, LeftRightBorderShift + 225, UpDownBorderShift + 35 );
	curr_time_local_display_Ptr.Color( LocalTimeColor );
	curr_time_local_display_Ptr.Font( "Arial" );
	curr_time_local_display_Ptr.FontSize( 10 );
	curr_time_local_display_Ptr.Description( TimeToString( curr_local_time, /*TIME_DATE |*/ TIME_MINUTES | TIME_SECONDS ));

	ArrayResize( button_tradelots_Ptr, lots_count );
	ArrayResize( trade_lots_display_Ptr, lots_count );
	for( int i = 0; i < lots_count; i++ )
	{
		int		d2 = ( i / 3 ) * 10;
		button_tradelots_Ptr[ i ] = new CChartObjectBmpLabel( );
		button_tradelots_Ptr[ i ].Create( 0, "TradeLotsButton" + IntegerToString( i ), 0, LeftRightBorderShift - 30, UpDownBorderShift + d2 + 75 + i * 20 );
		button_tradelots_Ptr[ i ].BmpFileOn( "CheackBox-On.BMP" );
		button_tradelots_Ptr[ i ].BmpFileOff( "CheackBox-Off.BMP" );
		button_tradelots_Ptr[ i ].State( false );
		
		trade_lots_display_Ptr[ i ] = new CChartObjectLabel( );
		trade_lots_display_Ptr[ i ].Create( 0, "TradeLotsLabel" + IntegerToString( i ), 0, LeftRightBorderShift - 40, UpDownBorderShift + d2 + 80 + i * 20 );
		trade_lots_display_Ptr[ i ].Anchor( ANCHOR_RIGHT );
		trade_lots_display_Ptr[ i ].Color( TitlesColor );
		trade_lots_display_Ptr[ i ].Font( "Arial" );
		trade_lots_display_Ptr[ i ].FontSize( 10 );
		trade_lots_display_Ptr[ i ].Description( TradeLotsStr_Array[ i ] );
	}

	curr_trade_lots_display_Ptr = new CChartObjectLabel( );
	curr_trade_lots_display_Ptr.Create( 0, "CurrTradeLots", 0, LeftRightBorderShift + 80, UpDownBorderShift + 170 );
	curr_trade_lots_display_Ptr.Anchor( ANCHOR_RIGHT );
	curr_trade_lots_display_Ptr.Color( TitlesColor );
	curr_trade_lots_display_Ptr.Font( "Arial" );
	curr_trade_lots_display_Ptr.FontSize( 14 );
	curr_trade_lots_display_Ptr.Description( DoubleToString( current_trade_lots, 2 ) + " lots");


	button_buy_Ptr = new CChartObjectBmpLabel( );
	button_buy_Ptr.Create( 0, "BuyButton", 0, LeftRightBorderShift, UpDownBorderShift + 75 );
	button_buy_Ptr.BmpFileOn( "BUY-Button-ON.BMP" );
	button_buy_Ptr.BmpFileOff( "BUY-Button-OFF.BMP" );
	button_buy_Ptr.State( true );

	button_sell_Ptr = new CChartObjectBmpLabel( );
	button_sell_Ptr.Create( 0, "SellButton", 0, LeftRightBorderShift, UpDownBorderShift + 200 );
	button_sell_Ptr.BmpFileOn( "SELL-Button-ON.BMP" );
	button_sell_Ptr.BmpFileOff( "SELL-Button-OFF.BMP" );
	button_sell_Ptr.State( true );

	button_close_all_Ptr = new CChartObjectBmpLabel( );
	button_close_all_Ptr.Create( 0, "CloseAllButton", 0, LeftRightBorderShift + 100, UpDownBorderShift + 70 );
	button_close_all_Ptr.BmpFileOn( "CloseAll-ON.BMP" );
	button_close_all_Ptr.BmpFileOff( "CloseAll-OFF.BMP" );
	button_close_all_Ptr.State( true );

	button_loss_breaking_Ptr = new CChartObjectBmpLabel( );
	button_loss_breaking_Ptr.Create( 0, "LossBreakButton", 0, LeftRightBorderShift + 100, UpDownBorderShift + 150 );
	button_loss_breaking_Ptr.BmpFileOn( "LossBreak-ON.BMP" );
	button_loss_breaking_Ptr.BmpFileOff( "LossBreak-OFF.BMP" );
	button_loss_breaking_Ptr.State( true );

	button_reverse_Ptr = new CChartObjectBmpLabel( );
	button_reverse_Ptr.Create( 0, "Reverse Position", 0, LeftRightBorderShift + 100, UpDownBorderShift + 230 );
	button_reverse_Ptr.BmpFileOn( "Reverse-ON.BMP" );
	button_reverse_Ptr.BmpFileOff( "Reverse-OFF.BMP" );
	button_reverse_Ptr.State( true );

}

//---------------------------------------------------------------------
//	Удаление графических объектов:
//---------------------------------------------------------------------
void
DeleteGraphObjects( )
{
	EventKillTimer( );

	if( CheckPointer( button_buy_Ptr ) != POINTER_INVALID )
	{
		delete( button_buy_Ptr );
	}

	if( CheckPointer( button_sell_Ptr ) != POINTER_INVALID )
	{
		delete( button_sell_Ptr );
	}

	if( CheckPointer( button_close_all_Ptr ) != POINTER_INVALID )
	{
		delete( button_close_all_Ptr );
	}

	if( CheckPointer( button_loss_breaking_Ptr ) != POINTER_INVALID )
	{
		delete( button_loss_breaking_Ptr );
	}

	if( CheckPointer( button_reverse_Ptr ) != POINTER_INVALID )
	{
		delete( button_reverse_Ptr );
	}
	
	if( lots_count > 0 )
	{
		for( int i = 0; i < lots_count; i++ )
		{
			if( CheckPointer( button_tradelots_Ptr[ i ] ) != POINTER_INVALID )
			{
				delete( button_tradelots_Ptr[ i ] );
			}
			
			if( CheckPointer( trade_lots_display_Ptr[ i ] ) != POINTER_INVALID )
			{
				delete( trade_lots_display_Ptr[ i ] );
			}
			
		}
	}

	if( CheckPointer( curr_trade_lots_display_Ptr ) != POINTER_INVALID )
	{
		delete( curr_trade_lots_display_Ptr );
	}

	if( CheckPointer( control_panel1_Ptr ) != POINTER_INVALID )
	{
		delete( control_panel1_Ptr );
	}

	if( CheckPointer( control_panel2_Ptr ) != POINTER_INVALID )
	{
		delete( control_panel2_Ptr );
	}

	if( CheckPointer( control_panel3_Ptr ) != POINTER_INVALID )
	{
		delete( control_panel3_Ptr );
	}

	if( CheckPointer( curr_time_local_display_Ptr ) != POINTER_INVALID )
	{
		delete( curr_time_local_display_Ptr );
	}

	if( CheckPointer( curr_time_gmt_display_Ptr ) != POINTER_INVALID )
	{
		delete( curr_time_gmt_display_Ptr );
	}

	if( CheckPointer( title_time_gmt_display_Ptr ) != POINTER_INVALID )
	{
		delete( title_time_gmt_display_Ptr );
	}

	if( CheckPointer( title_time_local_display_Ptr ) != POINTER_INVALID )
	{
		delete( title_time_local_display_Ptr );
	}
}

//---------------------------------------------------------------------
//	Перенос любых слов из строки в массив:
//---------------------------------------------------------------------
int
StringToArrayString( string st, string& ad[ ], string _delimiter = ";" )
{
	int			i = 0, np;
	string	stp;

	ArrayResize( ad, 0 );
	while( StringLen( st ) > 0 )
	{
		np = StringFind( st, _delimiter );
		if( np < 0 )
		{
			stp = st;
			st = "";
		}
		else
		{
			stp = StringSubstr( st, 0, np );
			st = StringSubstr( st, np + 1 );
		}
		i++;
		ArrayResize( ad, i );
		StringTrimLeft( stp );
		ad[ i - 1 ] = stp;
	}

	return( ArraySize( ad ));
}

//---------------------------------------------------------------------
//	Открытие длинной позиции:
//---------------------------------------------------------------------
bool
OpenBuy( )
{
	datetime	dt_start = TimeCurrent( );
	datetime	dt_curr;

	bool		result = false;
	while( result != true )
	{
		result = trade.PositionOpen( Symbol( ), ORDER_TYPE_BUY, current_trade_lots, SymbolInfoDouble( Symbol( ), SYMBOL_ASK ), stop_loss, take_profit );
		if( result == false )
		{
			uint	ch_ret_code = trade.CheckResultRetcode( );
			switch( ch_ret_code )
			{
				case	TRADE_RETCODE_NO_MONEY:																	// не достаточно денег на счету
					return( false );
			}

			dt_curr = TimeCurrent( );
			uint	send_ret_code = trade.ResultRetcode( );
			switch( send_ret_code )
			{
				case	TRADE_RETCODE_REQUOTE:																	// реквота
					if( dt_curr - dt_start >= 3 * 60 )
					{
						return( false );
					}
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_TIMEOUT:																	// запрос отменен по истечению времени
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 5000 );
					continue;

				case	TRADE_RETCODE_PRICE_CHANGED:														// цены изменились
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_PRICE_OFF:																// отсутствуют котировки для обработки запроса
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_CONNECTION:																// нет соединения с торговым сервером
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 30000 );
					continue;
			}
		}
	}

	return( true );
}

//---------------------------------------------------------------------
//	Открытие короткой позиции:
//---------------------------------------------------------------------
bool
OpenSell( )
{
	datetime	dt_start = TimeCurrent( );
	datetime	dt_curr;

	bool		result = false;
	while( result != true )
	{
		result = trade.PositionOpen( Symbol( ), ORDER_TYPE_SELL, current_trade_lots, SymbolInfoDouble( Symbol( ), SYMBOL_BID ), stop_loss, take_profit );
		if( result == false )
		{
			uint	ch_ret_code = trade.CheckResultRetcode( );
			switch( ch_ret_code )
			{
				case	TRADE_RETCODE_NO_MONEY:																	// не достаточно денег на счету
					return( false );
			}

			dt_curr = TimeCurrent( );
			uint	send_ret_code = trade.ResultRetcode( );
			switch( send_ret_code )
			{
				case	TRADE_RETCODE_REQUOTE:																	// реквота
					if( dt_curr - dt_start >= 3 * 60 )
					{
						return( false );
					}
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_TIMEOUT:																	// запрос отменен по истечению времени
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 5000 );
					continue;

				case	TRADE_RETCODE_PRICE_CHANGED:														// цены изменились
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_PRICE_OFF:																// отсутствуют котировки для обработки запроса
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 2000 );
					continue;

				case	TRADE_RETCODE_CONNECTION:																// нет соединения с торговым сервером
					if( dt_curr - dt_start >= 5 * 60 )
					{
						return( false );
					}
					Sleep( 30000 );
					continue;
			}
		}
	}

	return( true );
}

//---------------------------------------------------------------------
//	Закрытие длинной позиции:
//---------------------------------------------------------------------
void
LongPositionClose( )
{
	if( PositionSelect( Symbol( )) != true )
	{
		return;
	}

	if( PositionGetInteger( POSITION_TYPE ) == POSITION_TYPE_BUY )
	{
		trade.PositionClose( Symbol( ), 50 );
	}
}

//---------------------------------------------------------------------
//	Закрытие короткой позиции:
//---------------------------------------------------------------------
void
ShortPositionClose( )
{
	if( PositionSelect( Symbol( )) != true )
	{
		return;
	}

	if( PositionGetInteger( POSITION_TYPE ) == POSITION_TYPE_SELL )
	{
		trade.PositionClose( Symbol( ), 50 );
	}
}

//---------------------------------------------------------------------
//	Переворот позиции:
//---------------------------------------------------------------------
void
ReversePosition( )
{
	if( PositionSelect( Symbol( )) != true )
	{
		return;
	}

	if( PositionGetInteger( POSITION_TYPE ) == POSITION_TYPE_SELL )
	{
		trade.PositionClose( Symbol( ), 50 );
		OpenBuy( );
	}
	else if( PositionGetInteger( POSITION_TYPE ) == POSITION_TYPE_BUY )
	{
		trade.PositionClose( Symbol( ), 50 );
		OpenSell( );
	}
}

//---------------------------------------------------------------------
//	Проверить и отработать перевод позиции в БУ:
//---------------------------------------------------------------------
//	Возвращает:
//	 -1 - при выполнении операции возникла ошибка;
//	  1 - позиция успешно переведена в Б/У;
//		0 - перевод позиции в Б/У не требуется;
//---------------------------------------------------------------------
int
LossBreakPosition( )
{
	if( PositionSelect( Symbol( )) != true )
	{
		return( -1 );
	}

	double		price_stop_lb;
	double		price_open = PositionGetDouble( POSITION_PRICE_OPEN );
	double		price_current = PositionGetDouble( POSITION_PRICE_CURRENT );
	double		price_stop_loss = PositionGetDouble( POSITION_SL );
	double		price_take_profit = PositionGetDouble( POSITION_TP );
	long			type = ( long )PositionGetInteger(  POSITION_TYPE );

	if( type == POSITION_TYPE_BUY )
	{
		price_stop_lb = NormalizeDouble( price_open + NormalizeDouble( LosBreakingStopLoss * Point( ), Digits( )), Digits( ));

		if(( price_stop_loss == 0.0 ) || ( price_stop_loss < price_stop_lb ))
		{
			Print( "# LB Position : BUY : ", Symbol( ), ", Open Price = ", price_open, ", Curr Price = ", price_current, ", LB Stop Price = ", price_stop_lb );
			trade.PositionModify( Symbol( ), price_stop_lb, price_take_profit );
			return( 1 );
		}
	}
	else if( type == POSITION_TYPE_SELL )
	{
		price_stop_lb = NormalizeDouble( price_open - NormalizeDouble( LosBreakingStopLoss * Point( ), Digits( )), Digits( ));

		if(( price_stop_loss == 0.0 ) || ( price_stop_loss > price_stop_lb ))
		{
			Print( "# LB Position : SELL : ", Symbol( ), ", Open Price = ", price_open, ", Curr Price = ", price_current, ", LB Stop Price = ", price_stop_lb );
			trade.PositionModify( Symbol( ), price_stop_lb, price_take_profit );
			return( 1 );
		}
	}

	return( 0 );
}
