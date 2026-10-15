using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cointegration pairs trading strategy.
/// The residual is the first instrument's close minus Beta times Asset2's on candles of the same time, and its z-score is measured against
/// the mean and standard deviation of the last Period residuals. A z-score below minus EntryThreshold buys the first instrument and sells
/// Beta times as much of Asset2, one above EntryThreshold does the opposite, reversing an opposite pair. Both legs close once the z-score
/// is back within ExitThreshold of zero, or once the residual moves StopLossPercent of its entry value against the pair.
/// </summary>
public class CointegrationPairsStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _exitThreshold;
	private readonly StrategyParam<decimal> _beta;
	private readonly StrategyParam<Security> _asset2;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<DateTime, decimal> _firstCloses = [];
	private readonly Dictionary<DateTime, decimal> _secondCloses = [];
	private readonly Queue<decimal> _residuals = [];
	// 1 while long the pair, -1 while short it, 0 while flat.
	private int _side;
	private decimal _entryResidual;

	/// <summary>
	/// Number of residuals the mean and standard deviation are measured over.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// Z-score distance from zero that opens a pair.
	/// </summary>
	public decimal EntryThreshold
	{
		get => _entryThreshold.Value;
		set => _entryThreshold.Value = value;
	}

	/// <summary>
	/// Z-score distance from zero within which the pair closes.
	/// </summary>
	public decimal ExitThreshold
	{
		get => _exitThreshold.Value;
		set => _exitThreshold.Value = value;
	}

	/// <summary>
	/// Hedge ratio of Asset2 to the first instrument.
	/// </summary>
	public decimal Beta
	{
		get => _beta.Value;
		set => _beta.Value = value;
	}

	/// <summary>
	/// Second asset of the pair.
	/// </summary>
	public Security Asset2
	{
		get => _asset2.Value;
		set => _asset2.Value = value;
	}

	/// <summary>
	/// Adverse residual move, in percent of the entry residual, that closes the pair.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CointegrationPairsStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Residuals the mean and standard deviation are measured over", "Parameters");

		_entryThreshold = Param(nameof(EntryThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Z-score distance from zero that opens a pair", "Parameters");

		_exitThreshold = Param(nameof(ExitThreshold), 0.5m)
			.SetNotNegative()
			.SetDisplay("Exit Threshold", "Z-score distance from zero within which the pair closes", "Parameters");

		_beta = Param(nameof(Beta), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Beta", "Hedge ratio of Asset2 to the first instrument", "Parameters");

		_asset2 = Param<Security>(nameof(Asset2))
			.SetDisplay("Asset 2", "Second asset of the pair", "Parameters")
			.SetRequired();

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Adverse residual move in percent of the entry residual", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Asset2, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (Asset2 == null)
			throw new InvalidOperationException("Second asset is not specified.");

		ResetState();

		var firstSubscription = SubscribeCandles(CandleType);
		firstSubscription.Bind(candle => ProcessCandle(candle, _firstCloses)).Start();

		var secondSubscription = SubscribeCandles(CandleType, security: Asset2);
		secondSubscription.Bind(candle => ProcessCandle(candle, _secondCloses)).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, firstSubscription);
			DrawOwnTrades(area);
		}
	}

	private void ResetState()
	{
		_firstCloses.Clear();
		_secondCloses.Clear();
		_residuals.Clear();
		_side = 0;
		_entryResidual = 0;
	}

	private void ProcessCandle(ICandleMessage candle, Dictionary<DateTime, decimal> closes)
	{
		if (candle.State != CandleStates.Finished)
			return;

		closes[candle.OpenTime] = candle.ClosePrice;

		// The residual needs both instruments' candles of the same time, whichever arrives last.
		if (!_firstCloses.TryGetValue(candle.OpenTime, out var first) || !_secondCloses.TryGetValue(candle.OpenTime, out var second))
			return;

		foreach (var stale in _firstCloses.Keys.Where(t => t <= candle.OpenTime).ToArray())
			_firstCloses.Remove(stale);

		foreach (var stale in _secondCloses.Keys.Where(t => t <= candle.OpenTime).ToArray())
			_secondCloses.Remove(stale);

		var residual = first - Beta * second;

		_residuals.Enqueue(residual);

		if (_residuals.Count > Period)
			_residuals.Dequeue();

		if (_residuals.Count < Period)
			return;

		var mean = _residuals.Average();
		var deviation = (decimal)Math.Sqrt((double)_residuals.Average(r => (r - mean) * (r - mean)));

		if (deviation == 0)
			return;

		var zScore = (residual - mean) / deviation;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stopDistance = Math.Abs(_entryResidual) * StopLossPercent / 100;

		if (zScore < -EntryThreshold && _side <= 0)
		{
			MoveLegs(1);
			_entryResidual = residual;
		}
		else if (zScore > EntryThreshold && _side >= 0)
		{
			MoveLegs(-1);
			_entryResidual = residual;
		}
		else if (_side > 0 && (Math.Abs(zScore) < ExitThreshold || (StopLossPercent > 0 && residual <= _entryResidual - stopDistance)))
		{
			MoveLegs(0);
		}
		else if (_side < 0 && (Math.Abs(zScore) < ExitThreshold || (StopLossPercent > 0 && residual >= _entryResidual + stopDistance)))
		{
			MoveLegs(0);
		}
	}

	private void MoveLegs(int side)
	{
		_side = side;

		// Long the pair holds Volume of the first instrument and is short Beta times as much of Asset2.
		var firstChange = side * Volume - Position;

		if (firstChange > 0)
			BuyMarket(firstChange);
		else if (firstChange < 0)
			SellMarket(-firstChange);

		var secondChange = -side * Volume * Beta - (GetPositionValue(Asset2, Portfolio) ?? 0m);

		if (secondChange > 0)
			BuyMarket(secondChange, Asset2);
		else if (secondChange < 0)
			SellMarket(-secondChange, Asset2);
	}
}
