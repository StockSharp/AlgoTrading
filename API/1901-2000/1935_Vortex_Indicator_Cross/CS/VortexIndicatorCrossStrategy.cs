using System;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Vortex indicator cross strategy.
/// Goes long when VI+ crosses above VI- and short on the opposite signal.
/// </summary>
public class VortexIndicatorCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _stopLoss;
	private readonly StrategyParam<int> _takeProfit;
	private readonly StrategyParam<decimal> _minSpread;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevPlus;
	private decimal _prevMinus;
	private bool _isInitialized;
	private int _barsSinceTrade;

	/// <summary>
	/// Vortex indicator period length.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Stop loss in price steps.
	/// </summary>
	public int StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price steps.
	/// </summary>
	public int TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Minimum VI spread required for a signal.
	/// </summary>
	public decimal MinSpread
	{
		get => _minSpread.Value;
		set => _minSpread.Value = value;
	}

	/// <summary>
	/// Bars to wait after a completed trade.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public VortexIndicatorCrossStrategy()
	{
		_length = Param(nameof(Length), 14)
			.SetGreaterThanZero()
			.SetDisplay("Vortex Length", "Period for Vortex indicator", "General")
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Vortex calculation", "General");

		_stopLoss = Param(nameof(StopLoss), 1200)
			.SetDisplay("Stop Loss", "Protective stop in price steps", "General");

		_takeProfit = Param(nameof(TakeProfit), 2500)
			.SetDisplay("Take Profit", "Target profit in price steps", "General");

		_minSpread = Param(nameof(MinSpread), 0.08m)
			.SetDisplay("Min Spread", "Minimum VI spread required for entry", "General");

		_cooldownBars = Param(nameof(CooldownBars), 2)
			.SetDisplay("Cooldown Bars", "Bars to wait after a completed trade", "General");
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

		_prevPlus = 0m;
		_prevMinus = 0m;
		_isInitialized = false;
		_barsSinceTrade = CooldownBars;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));

		var vortex = new VortexIndicator { Length = Length };
		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(vortex, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue vortexValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var typed = (VortexIndicatorValue)vortexValue;

		if (typed.PlusVi is not decimal viPlus || typed.MinusVi is not decimal viMinus)
			return;

		if (_barsSinceTrade < CooldownBars)
			_barsSinceTrade++;

		if (!_isInitialized)
		{
			_prevPlus = viPlus;
			_prevMinus = viMinus;
			_isInitialized = true;
			return;
		}

		var spread = Math.Abs(viPlus - viMinus);
		var longSignal = _prevPlus <= _prevMinus && viPlus > viMinus && spread >= MinSpread;
		var shortSignal = _prevPlus >= _prevMinus && viPlus < viMinus && spread >= MinSpread;

		if (_barsSinceTrade >= CooldownBars)
		{
			if (longSignal && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
			else if (shortSignal && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
				_barsSinceTrade = 0;
			}
		}

		_prevPlus = viPlus;
		_prevMinus = viMinus;
	}
}
