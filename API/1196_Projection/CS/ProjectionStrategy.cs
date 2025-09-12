using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Projection strategy based on average daily open changes.
/// Calculates thresholds around the daily open and trades on breakouts.
/// </summary>
public class ProjectionStrategy : Strategy
{
	private readonly StrategyParam<decimal> _targetMultiple;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _calculationPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SMA? _changeSma;
	private decimal _prevOpen;
	private decimal _limitLong;
	private decimal _limitShort;
	private decimal _stopLong;
	private decimal _stopShort;

	/// <summary>
	/// Multiplier applied to average percentage change.
	/// </summary>
	public decimal TargetMultiple
	{
		get => _targetMultiple.Value;
		set => _targetMultiple.Value = value;
	}

	/// <summary>
	/// Threshold coefficient used to compute breakout levels.
	/// </summary>
	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	/// <summary>
	/// Number of days used to average percentage change.
	/// </summary>
	public int CalculationPeriod
	{
		get => _calculationPeriod.Value;
		set => _calculationPeriod.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public ProjectionStrategy()
	{
		_targetMultiple = Param(nameof(TargetMultiple), 0.2m)
			.SetDisplay("Target Multiple", "Multiplier for average change", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_threshold = Param(nameof(Threshold), 1m)
			.SetDisplay("Threshold", "Threshold percentage factor", "General")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_calculationPeriod = Param(nameof(CalculationPeriod), 5)
			.SetDisplay("Calculation Period", "Days for averaging", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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

		_prevOpen = 0m;
		_limitLong = 0m;
		_limitShort = 0m;
		_stopLong = 0m;
		_stopShort = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_changeSma = new SMA { Length = CalculationPeriod };

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var open = candle.OpenPrice;

		if (_prevOpen == 0m)
		{
			_prevOpen = open;
			return;
		}

		var change = Math.Abs((open - _prevOpen) / Math.Abs(_prevOpen) * 100m);
		var changeValue = _changeSma!.Process(candle.OpenTime, change);

		if (!changeValue.IsFinal)
		{
			_prevOpen = open;
			return;
		}

		var avgChange = changeValue.GetValue<decimal>() * TargetMultiple;
		var threshold = avgChange / 5m * Threshold / 100m * open;
		var stop = avgChange / 5m * 0.5m / 100m * open;

		_limitLong = open + threshold;
		_limitShort = open - threshold;
		_stopLong = _limitLong - stop;
		_stopShort = _limitShort + stop;

		if (Position <= 0 && candle.HighPrice >= _limitLong)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (Position >= 0 && candle.LowPrice <= _limitShort)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position > 0 && candle.LowPrice <= _stopLong)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && candle.HighPrice >= _stopShort)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevOpen = open;
	}
}
