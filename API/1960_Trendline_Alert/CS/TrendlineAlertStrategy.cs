using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that reacts to price breakouts above or below predefined trendlines.
/// </summary>
public class TrendlineAlertStrategy : Strategy
{
	private readonly StrategyParam<int> _breakoutPoints;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<int> _trailingStopPoints;
	private readonly StrategyParam<decimal> _upperLine;
	private readonly StrategyParam<decimal> _lowerLine;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _lastPrice;
	private decimal _stopPrice;
	private bool _upAlerted;
	private bool _downAlerted;

	/// <summary>
	/// Breakout threshold in points.
	/// </summary>
	public int BreakoutPoints { get => _breakoutPoints.Value; set => _breakoutPoints.Value = value; }

	/// <summary>
	/// Trading start hour (0-23).
	/// </summary>
	public int StartHour { get => _startHour.Value; set => _startHour.Value = value; }

	/// <summary>
	/// Trading end hour (0-24).
	/// </summary>
	public int EndHour { get => _endHour.Value; set => _endHour.Value = value; }

	/// <summary>
	/// Enable trailing stop logic.
	/// </summary>
	public bool UseTrailingStop { get => _useTrailingStop.Value; set => _useTrailingStop.Value = value; }

	/// <summary>
	/// Trailing stop distance in points.
	/// </summary>
	public int TrailingStopPoints { get => _trailingStopPoints.Value; set => _trailingStopPoints.Value = value; }

	/// <summary>
	/// Price level of the upper trendline.
	/// </summary>
	public decimal UpperLine { get => _upperLine.Value; set => _upperLine.Value = value; }

	/// <summary>
	/// Price level of the lower trendline.
	/// </summary>
	public decimal LowerLine { get => _lowerLine.Value; set => _lowerLine.Value = value; }

	/// <summary>
	/// Type of candles to subscribe.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="TrendlineAlertStrategy"/>.
	/// </summary>
	public TrendlineAlertStrategy()
	{
		_breakoutPoints = Param(nameof(BreakoutPoints), 0)
			.SetDisplay("Breakout Points", "Additional points for breakout", "General");

		_startHour = Param(nameof(StartHour), 0)
			.SetDisplay("Start Hour", "Strategy start hour", "General");

		_endHour = Param(nameof(EndHour), 24)
			.SetDisplay("End Hour", "Strategy end hour", "General");

		_useTrailingStop = Param(nameof(UseTrailingStop), false)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Protection");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 5)
			.SetDisplay("Trailing Stop Points", "Trailing stop distance", "Protection");

		_upperLine = Param(nameof(UpperLine), 0m)
			.SetDisplay("Upper Line", "Upper trendline level", "Levels");

		_lowerLine = Param(nameof(LowerLine), 0m)
			.SetDisplay("Lower Line", "Lower trendline level", "Levels");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_lastPrice = 0;
		_stopPrice = 0;
		_upAlerted = false;
		_downAlerted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour >= EndHour)
			return;

		var step = Security.PriceStep ?? 1m;
		var threshold = BreakoutPoints * step;

		var upper = UpperLine + threshold;
		var lower = LowerLine - threshold;
		var price = candle.ClosePrice;

		if (!_upAlerted && price > upper && _lastPrice <= upper)
		{
			_upAlerted = true;
			if (Position <= 0)
				BuyMarket();
		}
		else if (!_downAlerted && price < lower && _lastPrice >= lower)
		{
			_downAlerted = true;
			if (Position >= 0)
				SellMarket();
		}

		if (UseTrailingStop)
			UpdateTrailingStop(candle);

		_lastPrice = price;
	}

	private void UpdateTrailingStop(ICandleMessage candle)
	{
		var step = Security.PriceStep ?? 1m;
		var trail = TrailingStopPoints * step;

		if (Position > 0)
		{
			_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - trail);
			if (candle.LowPrice <= _stopPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + trail);
			if (candle.HighPrice >= _stopPrice)
				BuyMarket();
		}
		else
		{
			_stopPrice = 0;
		}
	}
}
