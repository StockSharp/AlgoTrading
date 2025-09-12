using System;
using System.Collections.Generic;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses linear regression of price versus volume.
/// Enters long when the predicted price crosses above its WMA and MACD is rising.
/// Enters short when MACD falls below its signal and lows are falling.
/// </summary>
public class LinearCrossTradingStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<int> _linearLength;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd = null!;
	private WeightedMovingAverage _wma = null!;

	private readonly Queue<(decimal price, decimal volume)> _window = new();
	private decimal _sumPrice;
	private decimal _sumVolume;
	private decimal _sumVolPrice;
	private decimal _sumVolSq;

	private decimal _prevPredicted;
	private decimal _prevWma;
	private decimal _prevMacd;
	private decimal _prevPrevMacd;
	private decimal _prevLow;
	private decimal _prevPrevLow;

	/// <summary>
	/// Regression length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Lookback for moving average of predicted price.
	/// </summary>
	public int LinearLength
	{
		get => _linearLength.Value;
		set => _linearLength.Value = value;
	}

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="LinearCrossTradingStrategy"/>.
	/// </summary>
	public LinearCrossTradingStrategy()
	{
		_length = Param(nameof(Length), 21)
			.SetGreaterThanZero()
			.SetDisplay("Regression Length", "Number of bars for regression", "Indicator")
			.SetCanOptimize(true);

		_linearLength = Param(nameof(LinearLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Linear Lookback", "Lookback for moving average of predicted price", "Indicator")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy", "General");
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
		_window.Clear();
		_sumPrice = 0m;
		_sumVolume = 0m;
		_sumVolPrice = 0m;
		_sumVolSq = 0m;
		_prevPredicted = 0m;
		_prevWma = 0m;
		_prevMacd = 0m;
		_prevPrevMacd = 0m;
		_prevLow = 0m;
		_prevPrevLow = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 12 },
				LongMa = { Length = 26 },
			},
			SignalMa = { Length = 9 }
		};

		_wma = new() { Length = LinearLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
			DrawIndicator(area, _wma, "MA Predicted Price");
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var predicted = ComputePredictedPrice(candle.ClosePrice, candle.TotalVolume ?? 0m);
		if (predicted is null)
		return;

		var wma = _wma.Process(predicted.Value, candle.ServerTime, true).ToDecimal();
		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)_macd.Process(predicted.Value, candle.ServerTime, true);
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
		return;

		var macdRising = macd > _prevMacd && _prevMacd > _prevPrevMacd;
		var macdFalling = macd < _prevMacd;
		var crossUp = _prevPredicted <= _prevWma && predicted.Value > wma;
		var lowFalling = candle.LowPrice < _prevLow && _prevLow < _prevPrevLow;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			if (crossUp && macdRising && macd > signal && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
			else if (macdFalling && macd < signal && lowFalling && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
		}

		_prevPrevMacd = _prevMacd;
		_prevMacd = macd;
		_prevPredicted = predicted.Value;
		_prevWma = wma;
		_prevPrevLow = _prevLow;
		_prevLow = candle.LowPrice;
	}

	private decimal? ComputePredictedPrice(decimal price, decimal volume)
	{
		_window.Enqueue((price, volume));
		_sumPrice += price;
		_sumVolume += volume;
		_sumVolPrice += volume * price;
		_sumVolSq += volume * volume;

		if (_window.Count > Length)
		{
			var old = _window.Dequeue();
			_sumPrice -= old.price;
			_sumVolume -= old.volume;
			_sumVolPrice -= old.volume * old.price;
			_sumVolSq -= old.volume * old.volume;
		}

		if (_window.Count < Length)
		return null;

		var len = _window.Count;
		var xbar = _sumVolume / len;
		var ybar = _sumPrice / len;
		var denom = _sumVolSq - len * xbar * xbar;
		if (denom == 0m)
		return null;
		var b = (_sumVolPrice - len * xbar * ybar) / denom;
		var a = ybar - b * xbar;
		return a + b * volume;
	}
}
