using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FlexiSuperTrend strategy - trades SuperTrend direction confirmed by smoothed deviation.
/// </summary>
public class FlexiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;

	private SuperTrend _superTrend;
	private SimpleMovingAverage _sma;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// ATR period for SuperTrend calculation.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR factor for SuperTrend calculation.
	/// </summary>
	public decimal AtrFactor
	{
		get => _atrFactor.Value;
		set => _atrFactor.Value = value;
	}

	/// <summary>
	/// Length for smoothing deviation.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// Enable long trades.
	/// </summary>
	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	/// <summary>
	/// Enable short trades.
	/// </summary>
	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public FlexiSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(7, 15, 2);

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("ATR Factor", "ATR factor for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_smaLength = Param(nameof(SmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length of deviation smoothing", "Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long Entries", "Enable long entries", "Strategy");

		_showShort = Param(nameof(ShowShort), true)
			.SetDisplay("Short Entries", "Enable short entries", "Strategy");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new() { Length = AtrPeriod, Multiplier = AtrFactor };
		_sma = new() { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_superTrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_superTrend.IsFormed)
			return;

		var deviation = candle.ClosePrice - superTrendValue;
		var smaValue = _sma.Process(new DecimalIndicatorValue(_sma, deviation));

		if (!smaValue.IsFinal || smaValue is not DecimalIndicatorValue smaResult)
			return;

		var osc = smaResult.Value;
		var direction = candle.ClosePrice > superTrendValue ? -1 : 1;

		if (ShowLong && direction < 0 && osc > 0 && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (ShowShort && direction > 0 && osc < 0 && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (Position > 0 && direction > 0)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && direction < 0)
		{
			BuyMarket(Math.Abs(Position));
		}
	}
}
