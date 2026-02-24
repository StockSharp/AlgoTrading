using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Three moving averages crossover strategy with trailing stop and take profit.
/// </summary>
public class Up3x1KrohaborDStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _middlePeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _trailingStop;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevMiddle;
	private bool _isInitialized;
	private decimal _entryPrice;
	private decimal _stopLossPrice;

	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int MiddlePeriod { get => _middlePeriod.Value; set => _middlePeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TrailingStop { get => _trailingStop.Value; set => _trailingStop.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public Up3x1KrohaborDStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 24)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast MA period", "MA Settings");

		_middlePeriod = Param(nameof(MiddlePeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Middle Period", "Middle MA period", "MA Settings");

		_slowPeriod = Param(nameof(SlowPeriod), 120)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow MA period", "MA Settings");

		_takeProfit = Param(nameof(TakeProfit), 1500m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Risk");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk");

		_trailingStop = Param(nameof(TrailingStop), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastMa = new SMA { Length = FastPeriod };
		var middleMa = new SMA { Length = MiddlePeriod };
		var slowMa = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, middleMa, slowMa, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal middle, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevMiddle = middle;
			_isInitialized = true;
			return;
		}

		// Relaxed crossover: fast crosses middle
		var crossUp = _prevFast <= _prevMiddle && fast > middle;
		var crossDown = _prevFast >= _prevMiddle && fast < middle;

		_prevFast = fast;
		_prevMiddle = middle;

		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
				_stopLossPrice = _entryPrice - StopLoss;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
				_stopLossPrice = _entryPrice + StopLoss;
			}
		}
		else if (Position > 0)
		{
			var newStop = candle.ClosePrice - TrailingStop;
			if (newStop > _stopLossPrice)
				_stopLossPrice = newStop;

			if (candle.ClosePrice >= _entryPrice + TakeProfit || candle.ClosePrice <= _stopLossPrice)
				SellMarket();
		}
		else if (Position < 0)
		{
			var newStop = candle.ClosePrice + TrailingStop;
			if (newStop < _stopLossPrice)
				_stopLossPrice = newStop;

			if (candle.ClosePrice <= _entryPrice - TakeProfit || candle.ClosePrice >= _stopLossPrice)
				BuyMarket();
		}
	}
}
