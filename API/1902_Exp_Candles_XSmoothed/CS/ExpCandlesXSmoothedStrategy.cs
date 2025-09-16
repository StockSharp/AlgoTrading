using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on smoothed candle highs and lows.
/// Applies a weighted moving average to high and low prices and
/// enters on breakouts beyond these smoothed levels plus a buffer.
/// </summary>
public class ExpCandlesXSmoothedStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private WeightedMovingAverage _highMa;
	private WeightedMovingAverage _lowMa;

	/// <summary>
	/// Moving average period length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Breakout level in points.
	/// </summary>
	public decimal Level
	{
		get => _level.Value;
		set => _level.Value = value;
	}

	/// <summary>
	/// Candle type for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Permission to open long positions.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Permission to open short positions.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Permission to close long positions.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Permission to close short positions.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ExpCandlesXSmoothedStrategy"/>.
	/// </summary>
	public ExpCandlesXSmoothedStrategy()
	{
		_maLength = Param(nameof(MaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Smoothing period", "Indicators");

		_level = Param(nameof(Level), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Level", "Breakout level in points", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Open", "Allow opening long positions", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Open", "Allow opening short positions", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Close", "Allow closing long positions", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Close", "Allow closing short positions", "Trading");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highMa = new WeightedMovingAverage { Length = MaLength };
		_lowMa = new WeightedMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _highMa);
			DrawIndicator(area, _lowMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		var highVal = _highMa.Process(candle.HighPrice);
		var lowVal = _lowMa.Process(candle.LowPrice);

		if (!highVal.IsFinal || !lowVal.IsFinal)
			return;

		if (candle.State != CandleStates.Finished)
			return;

		var level = Level * Security.PriceStep;
		var smoothedHigh = highVal.ToDecimal();
		var smoothedLow = lowVal.ToDecimal();

		var breakUp = candle.ClosePrice > smoothedHigh + level;
		var breakDown = candle.ClosePrice < smoothedLow - level;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (breakUp)
		{
			if (SellPosClose && Position < 0)
				BuyMarket(-Position);

			if (BuyPosOpen && Position <= 0)
				BuyMarket(Volume);
		}
		else if (breakDown)
		{
			if (BuyPosClose && Position > 0)
				SellMarket(Position);

			if (SellPosOpen && Position >= 0)
				SellMarket(Volume);
		}
	}
}
