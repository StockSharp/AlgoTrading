namespace StockSharp.Samples.Strategies;

using System;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Adaptive RSI Strategy
/// </summary>
public class AdaptiveRsiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _length;

	private RelativeStrengthIndex _rsi;
	private decimal? _arsiPrev;
	private decimal? _arsiPrevPrev;

	public AdaptiveRsiStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_length = Param(nameof(Length), 14)
			.SetDisplay("RSI Length", "RSI period", "Parameters")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_arsiPrev = null;
		_arsiPrevPrev = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RelativeStrengthIndex { Length = Length };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var alpha = 2m * Math.Abs(rsiValue / 100m - 0.5m);
		var src = candle.ClosePrice;

		var prev = _arsiPrev ?? src;
		var arsi = alpha * src + (1 - alpha) * prev;

		if (_arsiPrevPrev is not null)
		{
			var longCondition = _arsiPrev <= _arsiPrevPrev && arsi > _arsiPrev;
			var shortCondition = _arsiPrev >= _arsiPrevPrev && arsi < _arsiPrev;

			if (longCondition && Position <= 0)
				RegisterBuy();
			else if (shortCondition && Position >= 0)
				RegisterSell();
		}

		_arsiPrevPrev = _arsiPrev;
		_arsiPrev = arsi;
	}
}
