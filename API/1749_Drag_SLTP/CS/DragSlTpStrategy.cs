using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses SMA crossover for entries and automatically
/// attaches stop-loss and take-profit protection at fixed distances.
/// </summary>
public class DragSlTpStrategy : Strategy
{
	private readonly StrategyParam<decimal> _slPoints;
	private readonly StrategyParam<decimal> _tpPoints;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private decimal _entryPrice;

	public decimal SlPoints { get => _slPoints.Value; set => _slPoints.Value = value; }
	public decimal TpPoints { get => _tpPoints.Value; set => _tpPoints.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public DragSlTpStrategy()
	{
		_slPoints = Param(nameof(SlPoints), 500m)
			.SetGreaterThanZero()
			.SetDisplay("SL Points", "Stop-loss distance", "Risk");

		_tpPoints = Param(nameof(TpPoints), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("TP Points", "Take-profit distance", "Risk");

		_fastPeriod = Param(nameof(FastPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast SMA period", "Indicators");

		_slowPeriod = Param(nameof(SlowPeriod), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow SMA period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var fastSma = new SMA { Length = FastPeriod };
		var slowSma = new SMA { Length = SlowPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastSma, slowSma, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		var crossUp = _prevFast <= _prevSlow && fast > slow;
		var crossDown = _prevFast >= _prevSlow && fast < slow;

		_prevFast = fast;
		_prevSlow = slow;

		if (Position == 0)
		{
			if (crossUp)
			{
				BuyMarket();
				_entryPrice = candle.ClosePrice;
			}
			else if (crossDown)
			{
				SellMarket();
				_entryPrice = candle.ClosePrice;
			}
		}
		else if (Position > 0)
		{
			var price = candle.ClosePrice;
			if (price - _entryPrice >= TpPoints || _entryPrice - price >= SlPoints || crossDown)
			{
				SellMarket();
				if (crossDown)
				{
					SellMarket();
					_entryPrice = candle.ClosePrice;
				}
			}
		}
		else if (Position < 0)
		{
			var price = candle.ClosePrice;
			if (_entryPrice - price >= TpPoints || price - _entryPrice >= SlPoints || crossUp)
			{
				BuyMarket();
				if (crossUp)
				{
					BuyMarket();
					_entryPrice = candle.ClosePrice;
				}
			}
		}
	}
}
