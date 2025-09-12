using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// SuperTrend direction change strategy with optional fixed TP and SL.
/// </summary>
public class SupertrendTpSlProStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _factor;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _isLongEnabled;
	private readonly StrategyParam<bool> _isShortEnabled;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevDir;
	private decimal _stopLoss;
	private decimal _takeProfit;

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal Factor
	{
		get => _factor.Value;
		set => _factor.Value = value;
	}

	/// <summary>
	/// Take profit distance in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop loss distance in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool IsLongEnabled
	{
		get => _isLongEnabled.Value;
		set => _isLongEnabled.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool IsShortEnabled
	{
		get => _isShortEnabled.Value;
		set => _isShortEnabled.Value = value;
	}

	/// <summary>
	/// Use stop loss orders.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Use take profit orders.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public SupertrendTpSlProStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period for SuperTrend", "SuperTrend");

		_factor = Param(nameof(Factor), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Factor", "ATR multiplier for SuperTrend", "SuperTrend");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 1000m)
			.SetDisplay("TP Points", "Take profit distance", "Risk");

		_stopLossPoints = Param(nameof(StopLossPoints), 2000m)
			.SetDisplay("SL Points", "Stop loss distance", "Risk");

		_isLongEnabled = Param(nameof(IsLongEnabled), true)
			.SetDisplay("Enable Long", "Allow long trades", "Trading");

		_isShortEnabled = Param(nameof(IsShortEnabled), true)
			.SetDisplay("Enable Short", "Allow short trades", "Trading");

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Activate stop loss", "Risk");

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Activate take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var st = new SuperTrend { Length = AtrPeriod, Multiplier = Factor };
		var sub = SubscribeCandles(CandleType);
		sub.BindEx(st, Process).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, sub);
			DrawIndicator(area, st);
			DrawOwnTrades(area);
		}
	}

	private void Process(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var st = (SuperTrendIndicatorValue)value;
		var dir = st.IsUpTrend ? -1 : 1;

		if (_prevDir != 0)
		{
			var change = dir - _prevDir;
			if (change < 0 && IsLongEnabled && Position <= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (UseStopLoss)
					_stopLoss = candle.ClosePrice - StopLossPoints;
				if (UseTakeProfit)
					_takeProfit = candle.ClosePrice + TakeProfitPoints;
				BuyMarket(volume);
			}
			else if (change > 0 && IsShortEnabled && Position >= 0)
			{
				var volume = Volume + Math.Abs(Position);
				if (UseStopLoss)
					_stopLoss = candle.ClosePrice + StopLossPoints;
				if (UseTakeProfit)
					_takeProfit = candle.ClosePrice - TakeProfitPoints;
				SellMarket(volume);
			}
		}

		if (Position > 0)
		{
			if (UseStopLoss && candle.LowPrice <= _stopLoss)
				SellMarket(Math.Abs(Position));
			else if (UseTakeProfit && candle.HighPrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0)
		{
			if (UseStopLoss && candle.HighPrice >= _stopLoss)
				BuyMarket(Math.Abs(Position));
			else if (UseTakeProfit && candle.LowPrice <= _takeProfit)
				BuyMarket(Math.Abs(Position));
		}

		_prevDir = dir;
	}
}
