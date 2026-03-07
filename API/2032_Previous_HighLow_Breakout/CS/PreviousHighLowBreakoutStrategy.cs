using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades breakouts of the previous candle's high or low.
/// The strategy uses a trailing stop and take profit for risk management.
/// </summary>
public class PreviousHighLowBreakoutStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownCandles;

	private decimal _previousHigh;
	private decimal _previousLow;
	private bool _isFirstCandle = true;
	private int _cooldown;

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Cooldown period between trades in candles.
	/// </summary>
	public int CooldownCandles
	{
		get => _cooldownCandles.Value;
		set => _cooldownCandles.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public PreviousHighLowBreakoutStrategy()
	{
		_stopLoss = Param(nameof(StopLoss), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss in price points", "Risk Management");

		_takeProfit = Param(nameof(TakeProfit), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit in price points", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Time frame for candles", "General");

		_cooldownCandles = Param(nameof(CooldownCandles), 300)
			.SetDisplay("Cooldown", "Cooldown between trades in candles", "General");
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
		_previousHigh = default;
		_previousLow = default;
		_isFirstCandle = true;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection(
			new Unit(StopLoss, UnitTypes.Absolute),
			new Unit(TakeProfit, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirstCandle)
		{
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			_isFirstCandle = false;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_previousHigh = candle.HighPrice;
			_previousLow = candle.LowPrice;
			return;
		}

		var price = candle.ClosePrice;

		if (price > _previousHigh && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = CooldownCandles;
		}
		else if (price < _previousLow && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = CooldownCandles;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}
