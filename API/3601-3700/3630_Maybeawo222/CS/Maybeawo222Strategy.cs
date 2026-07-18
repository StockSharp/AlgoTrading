using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Moving average crossover strategy with price crossing the SMA line.
/// Buys when candle opens below MA and closes above it, sells vice versa.
/// </summary>
public class Maybeawo222Strategy : Strategy
{
	private readonly StrategyParam<int> _movingPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private ExponentialMovingAverage _ema;
	private decimal? _prevClose;
	private decimal? _prevMa;

	public int MovingPeriod
	{
		get => _movingPeriod.Value;
		set => _movingPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public Maybeawo222Strategy()
	{
		_movingPeriod = Param(nameof(MovingPeriod), 20)
			.SetDisplay("MA Period", "Simple moving average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_ema = new ExponentialMovingAverage { Length = MovingPeriod };
		_prevClose = null;
		_prevMa = null;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_ema.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevMa = maValue;
			return;
		}

		var close = candle.ClosePrice;
		if (_prevClose is null || _prevMa is null)
		{
			_prevClose = close;
			_prevMa = maValue;
			return;
		}

		// Buy signal: candle crosses MA from below to above
		var buySignal = _prevClose <= _prevMa && close > maValue;
		// Sell signal: candle crosses MA from above to below
		var sellSignal = _prevClose >= _prevMa && close < maValue;

		if (buySignal && Position <= 0)
		{
			BuyMarket(Position < 0 ? Math.Abs(Position) + 1 : 1);
		}
		else if (sellSignal && Position >= 0)
		{
			SellMarket(Position > 0 ? Math.Abs(Position) + 1 : 1);
		}

		_prevClose = close;
		_prevMa = maValue;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_ema = null;
		_prevClose = null;
		_prevMa = null;

		base.OnReseted();
	}
}
