using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MACD-based Terminator strategy.
/// Opens long when MACD crosses above its signal line and short when below.
/// Supports fixed take profit, stop loss, and trailing stop.
/// </summary>
public class TerminatorV2z0Strategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _signalPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverageConvergenceDivergenceSignal _macd;
	private decimal _entryPrice;
	private decimal _trailPrice;
	private bool _isLong;
	private decimal _prevMacd;
	private decimal _prevSignal;
	private bool _isInitialized;

	/// <summary>
	/// Fast period for MACD.
	/// </summary>
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }

	/// <summary>
	/// Slow period for MACD.
	/// </summary>
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }

	/// <summary>
	/// Signal period for MACD.
	/// </summary>
	public int SignalPeriod { get => _signalPeriod.Value; set => _signalPeriod.Value = value; }

	/// <summary>
	/// Take profit distance in price points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Stop loss distance in price points.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Trailing stop distance in price points.
	/// </summary>
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }

	/// <summary>
	/// Type of candles used for analysis.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public TerminatorV2z0Strategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Fast MACD Period", "Fast period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Slow MACD Period", "Slow period for MACD", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(20, 60, 1);

		_signalPeriod = Param(nameof(SignalPeriod), 1)
			.SetGreaterThanZero()
			.SetDisplay("Signal Period", "Signal line period", "MACD")
			.SetCanOptimize(true)
			.SetOptimize(1, 10, 1);

		_takeProfit = Param(nameof(TakeProfit), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(100m, 1000m, 100m);

		_stopLoss = Param(nameof(StopLoss), 2500m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_trailingStop = Param(nameof(TrailingStop), 0m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0m, 2000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for analysis", "General");
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
		_entryPrice = 0;
		_trailPrice = 0;
		_isLong = false;
		_prevMacd = 0;
		_prevSignal = 0;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = FastPeriod },
				LongMa = { Length = SlowPeriod },
			},
			SignalMa = { Length = SignalPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_macd, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _macd);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!_isInitialized)
		{
			_prevMacd = macd;
			_prevSignal = signal;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevMacd <= _prevSignal && macd > signal;
		var crossDown = _prevMacd >= _prevSignal && macd < signal;

		if (crossUp && Position <= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLong = true;
			_trailPrice = TrailingStop > 0 ? _entryPrice - TrailingStop : 0m;
			BuyMarket(Volume + Math.Abs(Position));
		}
		else if (crossDown && Position >= 0)
		{
			_entryPrice = candle.ClosePrice;
			_isLong = false;
			_trailPrice = TrailingStop > 0 ? _entryPrice + TrailingStop : 0m;
			SellMarket(Volume + Math.Abs(Position));
		}

		if (Position != 0 && _entryPrice != 0)
			CheckStops(candle.ClosePrice);

		_prevMacd = macd;
		_prevSignal = signal;
	}

	private void CheckStops(decimal price)
	{
		if (_isLong)
		{
			if (StopLoss > 0 && price <= _entryPrice - StopLoss)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (TakeProfit > 0 && price >= _entryPrice + TakeProfit)
			{
				SellMarket(Math.Abs(Position));
				return;
			}

			if (TrailingStop > 0)
			{
				var newTrail = price - TrailingStop;
				if (newTrail > _trailPrice)
					_trailPrice = newTrail;

				if (_trailPrice > 0 && price <= _trailPrice)
				{
					SellMarket(Math.Abs(Position));
					_trailPrice = 0m;
				}
			}
		}
		else
		{
			if (StopLoss > 0 && price >= _entryPrice + StopLoss)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (TakeProfit > 0 && price <= _entryPrice - TakeProfit)
			{
				BuyMarket(Math.Abs(Position));
				return;
			}

			if (TrailingStop > 0)
			{
				var newTrail = price + TrailingStop;
				if (_trailPrice == 0m || newTrail < _trailPrice)
					_trailPrice = newTrail;

				if (_trailPrice > 0 && price >= _trailPrice)
				{
					BuyMarket(Math.Abs(Position));
					_trailPrice = 0m;
				}
			}
		}
	}
}
