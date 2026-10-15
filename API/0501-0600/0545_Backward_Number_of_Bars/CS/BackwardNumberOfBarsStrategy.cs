using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Backward Number of Bars strategy.
/// Uses momentum over N bars with EMA trend filter.
/// Buys when price is above N-bar-ago price in uptrend, sells when below in downtrend.
/// </summary>
public class BackwardNumberOfBarsStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMomentum;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// Momentum lookback length.
	/// </summary>
	public int MomentumLength
	{
		get => _momentumLength.Value;
		set => _momentumLength.Value = value;
	}

	/// <summary>
	/// EMA trend filter period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public BackwardNumberOfBarsStrategy()
	{
		_momentumLength = Param(nameof(MomentumLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "ROC lookback period", "Indicators");

		_emaLength = Param(nameof(EmaLength), 40)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA trend filter period", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

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
		_prevMomentum = 0;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var roc = new RateOfChange { Length = MomentumLength };
		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(roc, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rocValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		// ROC crosses above zero with uptrend = buy
		var longSignal = _prevMomentum <= 0 && rocValue > 0 && candle.ClosePrice > emaValue;
		// ROC crosses below zero with downtrend = sell
		var shortSignal = _prevMomentum >= 0 && rocValue < 0 && candle.ClosePrice < emaValue;

		if (longSignal && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (shortSignal && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevMomentum = rocValue;
	}
}
