using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Delta neutral arbitrage strategy.
/// The spread is the first instrument's close minus Asset2Security's on candles of the same time, and its z-score is measured against the
/// mean and standard deviation of the last LookbackPeriod spreads. A z-score below minus EntryThreshold buys the first instrument and sells
/// Asset2Security in equal size, one above EntryThreshold does the opposite, reversing an opposite pair. Both legs close once the spread
/// crosses back over its mean, or once it moves StopLossPercent of its entry value against the pair.
/// </summary>
public class DeltaNeutralArbitrageStrategy : Strategy
{
	private readonly StrategyParam<Security> _asset2Security;
	private readonly StrategyParam<Portfolio> _asset2Portfolio;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _entryThreshold;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private readonly Dictionary<DateTime, decimal> _firstCloses = [];
	private readonly Dictionary<DateTime, decimal> _secondCloses = [];
	private readonly Queue<decimal> _spreads = [];
	// 1 while long the spread, -1 while short it, 0 while flat.
	private int _side;
	private decimal _entrySpread;

	/// <summary>
	/// Second asset of the pair.
	/// </summary>
	public Security Asset2Security
	{
		get => _asset2Security.Value;
		set => _asset2Security.Value = value;
	}

	/// <summary>
	/// Portfolio for the second asset; the strategy's own portfolio when empty.
	/// </summary>
	public Portfolio Asset2Portfolio
	{
		get => _asset2Portfolio.Value;
		set => _asset2Portfolio.Value = value;
	}

	/// <summary>
	/// Number of spreads the mean and standard deviation are measured over.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
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
	/// Constructor.
	/// </summary>
	public DeltaNeutralArbitrageStrategy()
	{
		_asset2Security = Param<Security>(nameof(Asset2Security))
			.SetDisplay("Asset 2", "Second asset of the pair", "Securities")
			.SetRequired();

		_asset2Portfolio = Param<Portfolio>(nameof(Asset2Portfolio))
			.SetDisplay("Portfolio 2", "Portfolio for the second asset", "Portfolios");

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Period", "Spreads the mean and standard deviation are measured over", "Parameters");

		_entryThreshold = Param(nameof(EntryThreshold), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Entry Threshold", "Z-score distance from zero that opens a pair", "Parameters");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop-loss %", "Adverse spread move in percent of the entry spread", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Asset2Security, CandleType)];
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

		if (Asset2Security == null)
			throw new InvalidOperationException("Asset2Security is not specified.");

		ResetState();

		var firstSubscription = SubscribeCandles(CandleType);
		firstSubscription.Bind(candle => ProcessCandle(candle, _firstCloses)).Start();

		var secondSubscription = SubscribeCandles(CandleType, security: Asset2Security);
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
		_spreads.Clear();
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

		var spread = first - second;

		_spreads.Enqueue(spread);

		if (_spreads.Count > LookbackPeriod)
			_spreads.Dequeue();

		if (_spreads.Count < LookbackPeriod)
			return;

		var mean = _spreads.Average();
		var deviation = (decimal)Math.Sqrt((double)_spreads.Average(s => (s - mean) * (s - mean)));

		if (deviation == 0)
			return;

		var zScore = (spread - mean) / deviation;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stopDistance = Math.Abs(_entrySpread) * StopLossPercent / 100;

		if (zScore < -EntryThreshold && _side <= 0)
		{
			MoveLegs(1);
			_entrySpread = spread;
		}
		else if (zScore > EntryThreshold && _side >= 0)
		{
			MoveLegs(-1);
			_entrySpread = spread;
		}
		else if (_side > 0 && (spread >= mean || (StopLossPercent > 0 && spread <= _entrySpread - stopDistance)))
		{
			MoveLegs(0);
		}
		else if (_side < 0 && (spread <= mean || (StopLossPercent > 0 && spread >= _entrySpread + stopDistance)))
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

		var portfolio = Asset2Portfolio ?? Portfolio;
		var secondChange = -side * Volume - (GetPositionValue(Asset2Security, portfolio) ?? 0m);

		if (secondChange != 0)
		{
			RegisterOrder(new Order
			{
				Security = Asset2Security,
				Portfolio = portfolio,
				Side = secondChange > 0 ? Sides.Buy : Sides.Sell,
				Volume = Math.Abs(secondChange),
				Type = OrderTypes.Market,
			});
		}
	}
}
