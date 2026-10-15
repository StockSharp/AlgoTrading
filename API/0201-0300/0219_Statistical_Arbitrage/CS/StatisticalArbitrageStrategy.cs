using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Statistical arbitrage strategy.
/// Each instrument is compared with the simple moving average of its last LookbackPeriod closes on candles of the same time. The first
/// instrument below its average while the second is above its own buys the first and sells the second, the mirror does the opposite,
/// reversing an opposite pair. Both legs close once the first instrument closes back across its average, or once the spread, the first
/// close minus the second, moves StopLossPercent of its entry value against the pair.
/// </summary>
public class StatisticalArbitrageStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<Security> _secondSecurity;

	private readonly Dictionary<DateTime, decimal> _firstCloses = [];
	private readonly Dictionary<DateTime, decimal> _secondCloses = [];
	private readonly Queue<decimal> _firstHistory = [];
	private readonly Queue<decimal> _secondHistory = [];
	// 1 while long the spread, -1 while short it, 0 while flat.
	private int _side;
	private decimal _entrySpread;

	/// <summary>
	/// Number of closes each moving average spans.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Adverse spread move, in percent of the entry spread, that closes the pair.
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
	/// Second security in the pair.
	/// </summary>
	public Security SecondSecurity
	{
		get => _secondSecurity.Value;
		set => _secondSecurity.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public StatisticalArbitrageStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Closes each moving average spans", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop-loss %", "Adverse spread move in percent of the entry spread", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_secondSecurity = Param<Security>(nameof(SecondSecurity))
			.SetDisplay("Second Security", "Second security in the pair", "General")
			.SetRequired();
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (SecondSecurity, CandleType)];
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

		if (SecondSecurity == null)
			throw new InvalidOperationException("Second security is not specified.");

		ResetState();

		var firstSubscription = SubscribeCandles(CandleType);
		firstSubscription.Bind(candle => ProcessCandle(candle, _firstCloses)).Start();

		var secondSubscription = SubscribeCandles(CandleType, security: SecondSecurity);
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
		_firstHistory.Clear();
		_secondHistory.Clear();
		_side = 0;
		_entrySpread = 0;
	}

	private void ProcessCandle(ICandleMessage candle, Dictionary<DateTime, decimal> closes)
	{
		if (candle.State != CandleStates.Finished)
			return;

		closes[candle.OpenTime] = candle.ClosePrice;

		// The spread needs both instruments' candles of the same time, whichever arrives last.
		if (!_firstCloses.TryGetValue(candle.OpenTime, out var first) || !_secondCloses.TryGetValue(candle.OpenTime, out var second))
			return;

		foreach (var stale in _firstCloses.Keys.Where(t => t <= candle.OpenTime).ToArray())
			_firstCloses.Remove(stale);

		foreach (var stale in _secondCloses.Keys.Where(t => t <= candle.OpenTime).ToArray())
			_secondCloses.Remove(stale);

		_firstHistory.Enqueue(first);
		_secondHistory.Enqueue(second);

		if (_firstHistory.Count > LookbackPeriod)
		{
			_firstHistory.Dequeue();
			_secondHistory.Dequeue();
		}

		if (_firstHistory.Count < LookbackPeriod)
			return;

		var firstAverage = _firstHistory.Average();
		var secondAverage = _secondHistory.Average();
		var spread = first - second;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stopDistance = Math.Abs(_entrySpread) * StopLossPercent / 100;

		if (first < firstAverage && second > secondAverage && _side <= 0)
		{
			MoveLegs(1);
			_entrySpread = spread;
		}
		else if (first > firstAverage && second < secondAverage && _side >= 0)
		{
			MoveLegs(-1);
			_entrySpread = spread;
		}
		else if (_side > 0 && (first > firstAverage || (StopLossPercent > 0 && spread <= _entrySpread - stopDistance)))
		{
			MoveLegs(0);
		}
		else if (_side < 0 && (first < firstAverage || (StopLossPercent > 0 && spread >= _entrySpread + stopDistance)))
		{
			MoveLegs(0);
		}
	}

	private void MoveLegs(int side)
	{
		_side = side;

		// Long the spread holds the first instrument and is short the second, each by Volume.
		var firstChange = side * Volume - Position;

		if (firstChange > 0)
			BuyMarket(firstChange);
		else if (firstChange < 0)
			SellMarket(-firstChange);

		var secondChange = -side * Volume - (GetPositionValue(SecondSecurity, Portfolio) ?? 0m);

		if (secondChange > 0)
			BuyMarket(secondChange, SecondSecurity);
		else if (secondChange < 0)
			SellMarket(-secondChange, SecondSecurity);
	}
}
