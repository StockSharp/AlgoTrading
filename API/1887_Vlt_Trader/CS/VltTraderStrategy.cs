using System;
using System.Linq;
using System.Collections.Generic;
using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility breakout strategy. Enters on a breakout of a low-volatility bar.
/// Uses market orders when price exceeds the high/low of the signal bar.
/// </summary>
public class VltTraderStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private Lowest _lowest = null!;
	private decimal _prevRange;
	private decimal _prevMinRange;
	private decimal _signalHigh;
	private decimal _signalLow;
	private bool _pendingBreakout;

	/// <summary>
	/// Indicator period for lowest range.
	/// </summary>
	public int Period { get => _period.Value; set => _period.Value = value; }

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="VltTraderStrategy"/> class.
	/// </summary>
	public VltTraderStrategy()
	{
		_period = Param(nameof(Period), 9)
			.SetDisplay("Period", "Indicator period", "General")
			.SetOptimize(5, 30, 5);

		_stopLoss = Param(nameof(StopLoss), 550m)
			.SetDisplay("Stop loss", "Stop loss in price steps", "Risk")
			.SetOptimize(100m, 2000m, 100m);

		_takeProfit = Param(nameof(TakeProfit), 550m)
			.SetDisplay("Take profit", "Take profit in price steps", "Risk")
			.SetOptimize(100m, 2000m, 100m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Candles for calculation", "General");
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
		_prevRange = 0m;
		_prevMinRange = decimal.MaxValue;
		_pendingBreakout = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_lowest = new Lowest { Length = Period };
		_prevRange = 0m;
		_prevMinRange = decimal.MaxValue;
		_pendingBreakout = false;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(
			takeProfit: new Unit(TakeProfit * step, UnitTypes.Absolute),
			stopLoss: new Unit(StopLoss * step, UnitTypes.Absolute));

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

		var range = candle.HighPrice - candle.LowPrice;
		var lowestResult = _lowest.Process(range, candle.OpenTime, true);

		if (!_lowest.IsFormed)
			return;

		var minRange = lowestResult.ToDecimal();

		// Check for pending breakout entry
		if (_pendingBreakout && Position == 0)
		{
			if (candle.ClosePrice > _signalHigh)
			{
				BuyMarket();
				_pendingBreakout = false;
			}
			else if (candle.ClosePrice < _signalLow)
			{
				SellMarket();
				_pendingBreakout = false;
			}
		}

		// Detect low-volatility signal: range drops below minimum
		var isSignal = range <= minRange && _prevRange > _prevMinRange;

		_prevRange = range;
		_prevMinRange = minRange;

		if (isSignal && Position == 0)
		{
			_signalHigh = candle.HighPrice;
			_signalLow = candle.LowPrice;
			_pendingBreakout = true;
		}
	}
}
