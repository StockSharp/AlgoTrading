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
/// Strategy based on Adaptive Center of Gravity Oscillator.
/// Computes CG oscillator inline and trades on crossovers of CG with its prior value (signal).
/// </summary>
public class AdaptiveCgOscillatorX2Strategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _prices = new();
	private decimal _prevCg;
	private decimal _prevPrevCg;
	private int _count;
	private int _barsSinceSignal;

	public int Period { get => _period.Value; set => _period.Value = value; }
	public decimal StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }
	public decimal TakeProfit { get => _takeProfit.Value; set => _takeProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public AdaptiveCgOscillatorX2Strategy()
	{
		_period = Param(nameof(Period), 20)
			.SetGreaterThanZero()
			.SetDisplay("Period", "Lookback period for CG oscillator", "Parameters")
			.SetOptimize(5, 20, 1);

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price units", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price units", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prices.Clear();
		_prevCg = 0m;
		_prevPrevCg = 0m;
		_count = 0;
		_barsSinceSignal = int.MaxValue;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(TakeProfit, UnitTypes.Absolute),
			new Unit(StopLoss, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		_barsSinceSignal++;
		_prices.Add(price);

		if (_prices.Count > Period)
			_prices.RemoveAt(0);

		if (_prices.Count < Period)
			return;

		// Compute Center of Gravity
		decimal num = 0m;
		decimal denom = 0m;
		for (int i = 0; i < _prices.Count; i++)
		{
			num += (1 + i) * _prices[i];
			denom += _prices[i];
		}

		var cg = denom != 0 ? -num / denom + (Period + 1m) / 2m : 0m;

		_count++;
		if (_count < 3)
		{
			_prevPrevCg = _prevCg;
			_prevCg = cg;
			return;
		}

		var longSignal = cg > 0m && cg > _prevCg && _prevCg <= _prevPrevCg;
		var shortSignal = cg < 0m && cg < _prevCg && _prevCg >= _prevPrevCg;

		if (longSignal && _barsSinceSignal >= 12 && Position <= 0)
		{
			BuyMarket();
			_barsSinceSignal = 0;
		}
		else if (shortSignal && _barsSinceSignal >= 12 && Position >= 0)
		{
			SellMarket();
			_barsSinceSignal = 0;
		}

		_prevPrevCg = _prevCg;
		_prevCg = cg;
	}
}
