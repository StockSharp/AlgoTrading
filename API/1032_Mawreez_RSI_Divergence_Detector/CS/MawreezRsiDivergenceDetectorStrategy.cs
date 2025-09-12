using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mawreez' RSI Divergence Detector strategy.
/// Buys when price makes a lower low with RSI making a higher low and sells on opposite signal.
/// </summary>
public class MawreezRsiDivergenceDetectorStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _minDivLength;
	private readonly StrategyParam<int> _maxDivLength;

	private readonly RelativeStrengthIndex _rsi;

	private decimal[] _priceHistory = Array.Empty<decimal>();
	private decimal[] _rsiHistory = Array.Empty<decimal>();
	private int _index;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public int MinDivLength { get => _minDivLength.Value; set => _minDivLength.Value = value; }
	public int MaxDivLength { get => _maxDivLength.Value; set => _maxDivLength.Value = value; }

	public MawreezRsiDivergenceDetectorStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "Period for RSI calculation", "Indicator")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_minDivLength = Param(nameof(MinDivLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Minimum Divergence Length", "Shortest lookback to check", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_maxDivLength = Param(nameof(MaxDivLength), 28)
			.SetGreaterThanZero()
			.SetDisplay("Maximum Divergence Length", "Longest lookback to check", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_rsi = new RelativeStrengthIndex { Length = RsiLength };
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceHistory = new decimal[MaxDivLength + 1];
		_rsiHistory = new decimal[MaxDivLength + 1];
		_index = 0;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		var pos = _index % _priceHistory.Length;
		_priceHistory[pos] = price;
		_rsiHistory[pos] = rsi;
		_index++;

		if (_index <= MaxDivLength)
			return;

		decimal totalDiv = 0m;
		int count = 0;
		int winner = 0;

		for (var l = MinDivLength; l <= MaxDivLength; l++)
		{
			var idx = (_index - l - 1) % _priceHistory.Length;
			var pastPrice = _priceHistory[idx];
			var pastRsi = _rsiHistory[idx];

			var dsrc = price - pastPrice;
			var dosc = rsi - pastRsi;

			if (Math.Sign(dsrc) == Math.Sign(dosc))
				continue;

			totalDiv += Math.Abs(dsrc) + Math.Abs(dosc);
			count++;

			if (winner == 0)
			{
				if (dsrc < 0 && dosc > 0)
					winner = 1; // bullish
				else if (dsrc > 0 && dosc < 0)
					winner = -1; // bearish
			}
		}

		if (count == 0)
			return;

		if (winner > 0 && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (winner < 0 && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
