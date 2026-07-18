namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// RSI trader strategy aligning price and RSI moving-average trends.
/// </summary>
public class RsiTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _shortRsiMaPeriod;
	private readonly StrategyParam<int> _longRsiMaPeriod;
	private readonly StrategyParam<int> _shortPriceMaPeriod;
	private readonly StrategyParam<int> _longPriceMaPeriod;
	private readonly StrategyParam<bool> _reverse;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _rsiValues = new();

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public int ShortRsiMaPeriod { get => _shortRsiMaPeriod.Value; set => _shortRsiMaPeriod.Value = value; }
	public int LongRsiMaPeriod { get => _longRsiMaPeriod.Value; set => _longRsiMaPeriod.Value = value; }
	public int ShortPriceMaPeriod { get => _shortPriceMaPeriod.Value; set => _shortPriceMaPeriod.Value = value; }
	public int LongPriceMaPeriod { get => _longPriceMaPeriod.Value; set => _longPriceMaPeriod.Value = value; }
	public bool Reverse { get => _reverse.Value; set => _reverse.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public RsiTraderStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14).SetDisplay("RSI Period", "RSI calculation length", "RSI").SetGreaterThanZero();
		_shortRsiMaPeriod = Param(nameof(ShortRsiMaPeriod), 12).SetDisplay("Short RSI MA", "Short moving average on RSI", "RSI").SetGreaterThanZero();
		_longRsiMaPeriod = Param(nameof(LongRsiMaPeriod), 60).SetDisplay("Long RSI MA", "Long moving average on RSI", "RSI").SetGreaterThanZero();
		_shortPriceMaPeriod = Param(nameof(ShortPriceMaPeriod), 12).SetDisplay("Short Price MA", "Short simple moving average", "Price").SetGreaterThanZero();
		_longPriceMaPeriod = Param(nameof(LongPriceMaPeriod), 60).SetDisplay("Long Price MA", "Long weighted moving average", "Price").SetGreaterThanZero();
		_reverse = Param(nameof(Reverse), false).SetDisplay("Reverse", "Flip buy/sell signals", "Trading");
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame()).SetDisplay("Candle Type", "Primary candle type", "Data");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_closes.Clear();
		_rsiValues.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);

		var maxCache = Math.Max(LongPriceMaPeriod, Math.Max(LongRsiMaPeriod + RsiPeriod, 300));
		if (_closes.Count > maxCache)
			_closes.RemoveAt(0);

		var rsi = CalculateRsi();
		if (rsi is null)
			return;

		_rsiValues.Add(rsi.Value);
		if (_rsiValues.Count > maxCache)
			_rsiValues.RemoveAt(0);

		if (_rsiValues.Count < LongRsiMaPeriod || _closes.Count < LongPriceMaPeriod)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var shortRsi = AverageLast(_rsiValues, ShortRsiMaPeriod);
		var longRsi = AverageLast(_rsiValues, LongRsiMaPeriod);
		var shortPrice = AverageLast(_closes, ShortPriceMaPeriod);
		var longPrice = WeightedAverageLast(_closes, LongPriceMaPeriod);

		var goLong = shortPrice > longPrice && shortRsi > longRsi;
		var goShort = shortPrice < longPrice && shortRsi < longRsi;
		var sideways = !goLong && !goShort;

		if (sideways && Position != 0)
		{
			if (Position > 0)
				SellMarket(Position);
			else
				BuyMarket(Math.Abs(Position));

			return;
		}

		if (Position != 0)
			return;

		if (goLong)
		{
			if (Reverse)
				SellMarket();
			else
				BuyMarket();
		}
		else if (goShort)
		{
			if (Reverse)
				BuyMarket();
			else
				SellMarket();
		}
	}

	private decimal? CalculateRsi()
	{
		if (_closes.Count <= RsiPeriod)
			return null;

		decimal gainSum = 0m;
		decimal lossSum = 0m;
		var start = _closes.Count - RsiPeriod;

		for (var i = start; i < _closes.Count; i++)
		{
			var change = _closes[i] - _closes[i - 1];
			if (change > 0m)
				gainSum += change;
			else
				lossSum -= change;
		}

		var averageGain = gainSum / RsiPeriod;
		var averageLoss = lossSum / RsiPeriod;

		if (averageLoss == 0m)
			return 100m;

		var rs = averageGain / averageLoss;
		return 100m - 100m / (1m + rs);
	}

	private static decimal AverageLast(IReadOnlyList<decimal> values, int length)
	{
		decimal sum = 0m;
		var start = values.Count - length;

		for (var i = start; i < values.Count; i++)
			sum += values[i];

		return sum / length;
	}

	private static decimal WeightedAverageLast(IReadOnlyList<decimal> values, int length)
	{
		decimal weightedSum = 0m;
		decimal weightSum = 0m;
		var start = values.Count - length;
		var weight = 1m;

		for (var i = start; i < values.Count; i++)
		{
			weightedSum += values[i] * weight;
			weightSum += weight;
			weight += 1m;
		}

		return weightSum > 0m ? weightedSum / weightSum : values[^1];
	}
}
