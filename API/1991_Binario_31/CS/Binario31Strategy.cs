namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Breakout strategy based on EMA channel from binario_31 script.
/// Places stop orders around the channel and manages trailing stop.
/// </summary>
public class Binario31Strategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _pipDifference;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaHigh;
	private ExponentialMovingAverage _emaLow;

	private decimal _buyLevel;
	private decimal _sellLevel;
	private decimal _buyStop;
	private decimal _sellStop;
	private decimal _buyTarget;
	private decimal _sellTarget;

	private decimal _stopPrice;
	private decimal _targetPrice;

	/// <summary>
	/// EMA period.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Distance from EMA to entry in price steps.
	/// </summary>
	public decimal PipDifference { get => _pipDifference.Value; set => _pipDifference.Value = value; }

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume { get => _volume.Value; set => _volume.Value = value; }

	/// <summary>
	/// Type of candles to process.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public Binario31Strategy()
	{
		_emaLength = Param(nameof(EmaLength), 144)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period for high and low bands", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 200, 10);

		_pipDifference = Param(nameof(PipDifference), 25m)
			.SetGreaterThanZero()
			.SetDisplay("Pip Difference", "Distance from EMA to entry", "General")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_takeProfit = Param(nameof(TakeProfit), 850m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_trailingStop = Param(nameof(TrailingStop), 850m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop in price steps", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		ResetLevels();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaHigh = new ExponentialMovingAverage { Length = EmaLength };
		_emaLow = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var step = Security.PriceStep ?? 1m;

		var emaHigh = _emaHigh.Process(candle.HighPrice, candle.OpenTime, true).ToDecimal();
		var emaLow = _emaLow.Process(candle.LowPrice, candle.OpenTime, true).ToDecimal();

		if (!IsFormedAndOnlineAndAllowTrading() || !_emaHigh.IsFormed || !_emaLow.IsFormed)
			return;

		var close = candle.ClosePrice;

		// When price is inside the channel calculate entry levels.
		if (Position == 0 && close < emaHigh && close > emaLow)
		{
			_buyLevel = emaHigh + PipDifference * step;
			_buyStop = emaLow - step;
			_buyTarget = _buyLevel + TakeProfit * step;

			_sellLevel = emaLow - PipDifference * step;
			_sellStop = emaHigh + step;
			_sellTarget = _sellLevel - TakeProfit * step;
		}

		if (Position == 0)
		{
			if (_buyLevel != 0m && candle.HighPrice >= _buyLevel)
			{
				BuyMarket(Volume);
				_stopPrice = _buyStop;
				_targetPrice = _buyTarget;
				_buyLevel = _sellLevel = 0m;
			}
			else if (_sellLevel != 0m && candle.LowPrice <= _sellLevel)
			{
				SellMarket(Volume);
				_stopPrice = _sellStop;
				_targetPrice = _sellTarget;
				_buyLevel = _sellLevel = 0m;
			}
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || close <= _stopPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetLevels();
			}
			else if (candle.HighPrice >= _targetPrice || close >= _targetPrice)
			{
				SellMarket(Math.Abs(Position));
				ResetLevels();
			}
			else if (TrailingStop > 0m)
			{
				var trail = close - TrailingStop * step;
				if (trail > _stopPrice)
					_stopPrice = trail;
			}
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || close >= _stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetLevels();
			}
			else if (candle.LowPrice <= _targetPrice || close <= _targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetLevels();
			}
			else if (TrailingStop > 0m)
			{
				var trail = close + TrailingStop * step;
				if (_stopPrice == 0m || trail < _stopPrice)
					_stopPrice = trail;
			}
		}
	}

	private void ResetLevels()
	{
		_buyLevel = 0m;
		_sellLevel = 0m;
		_buyStop = 0m;
		_sellStop = 0m;
		_buyTarget = 0m;
		_sellTarget = 0m;
		_stopPrice = 0m;
		_targetPrice = 0m;
	}
}
