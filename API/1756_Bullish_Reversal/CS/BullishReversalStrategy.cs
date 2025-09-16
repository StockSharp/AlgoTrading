using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy looking for several bullish reversal candlestick patterns.
/// </summary>
public class BullishReversalStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<bool> _useTrailingStop;

	private SimpleMovingAverage _sma;
	private ICandleMessage _prev1;
	private ICandleMessage _prev2;
	private ICandleMessage _prev3;
	private decimal _stopPrice;

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Distance for trailing stop in price units.
	/// </summary>
	public decimal TrailingStop
	{
		get => _trailingStop.Value;
		set => _trailingStop.Value = value;
	}

	/// <summary>
	/// Enable trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BullishReversalStrategy"/>.
	/// </summary>
	public BullishReversalStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_maPeriod = Param(nameof(MaPeriod), 50)
			.SetDisplay("MA Period", "SMA length", "Parameters")
			.SetGreaterThanZero()
			.SetCanOptimize(true);

		_trailingStop = Param(nameof(TrailingStop), 50m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
			.SetGreaterThanZero();

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		Volume = 1;
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

		_sma = default;
		_prev1 = default;
		_prev2 = default;
		_prev3 = default;
		_stopPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_sma = new SimpleMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_sma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prev1 is null || _prev2 is null || _prev3 is null)
		{
			_prev3 = _prev2;
			_prev2 = _prev1;
			_prev1 = candle;
			return;
		}

		var open1 = _prev1.OpenPrice;
		var close1 = _prev1.ClosePrice;
		var low1 = _prev1.LowPrice;
		var open2 = _prev2.OpenPrice;
		var close2 = _prev2.ClosePrice;
		var low2 = _prev2.LowPrice;
		var open3 = _prev3.OpenPrice;
		var close3 = _prev3.ClosePrice;
		var low3 = _prev3.LowPrice;

		var abandonedBaby = open3 > close3 && open2 > close2 && low2 < low3 &&
			open1 < close1 && low1 >= low2 && close1 > open3;

		var morningDojiStar = open3 > close3 && open2 <= close2 &&
			open1 < close3 && close1 < open3;

		var threeInsideUp = open3 > close3 &&
			Math.Abs(close2 - open2) <= 0.6m * Math.Abs(open3 - close3) &&
			close2 > open2 && close1 > open1 && close1 > open3;

		var threeOutsideUp = open3 > close3 &&
			1.1m * Math.Abs(open3 - close3) < Math.Abs(open2 - close2) &&
			open2 < close2 && open1 < close1;

		var threeWhiteSoldiers = open3 < close3 && open2 < close2 && open1 < close1 &&
			close3 < close2 && close2 < close1;

		var signal = (abandonedBaby || morningDojiStar || threeInsideUp || threeOutsideUp || threeWhiteSoldiers) &&
			open1 < ma;

		if (signal && Position <= 0)
		{
			BuyMarket();
			_stopPrice = candle.ClosePrice - TrailingStop;
		}
		else if (UseTrailingStop && Position > 0)
		{
			var newStop = candle.ClosePrice - TrailingStop;

			if (_stopPrice == 0m || newStop > _stopPrice)
				_stopPrice = newStop;

			if (candle.ClosePrice <= _stopPrice)
			{
				SellMarket(Position);
				_stopPrice = 0m;
			}
		}

		_prev3 = _prev2;
		_prev2 = _prev1;
		_prev1 = candle;
	}
}
