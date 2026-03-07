using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Doubler strategy using double EMA confirmation with trailing stop management.
/// Enters long when both fast and medium EMAs are above slow EMA.
/// Enters short when both fast and medium EMAs are below slow EMA.
/// </summary>
public class DoublerStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _medPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private ExponentialMovingAverage _fast;
	private ExponentialMovingAverage _med;
	private ExponentialMovingAverage _slow;

	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MedPeriod
	{
		get => _medPeriod.Value;
		set => _medPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public DoublerStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast Period", "Fast EMA period", "Indicator");

		_medPeriod = Param(nameof(MedPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Medium Period", "Medium EMA period", "Indicator");

		_slowPeriod = Param(nameof(SlowPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow Period", "Slow EMA period", "Indicator");

		_stopLossPoints = Param(nameof(StopLossPoints), 150)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 300)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price steps", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, TimeSpan.FromMinutes(5).TimeFrame());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fast = null;
		_med = null;
		_slow = null;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fast = new ExponentialMovingAverage { Length = FastPeriod };
		_med = new ExponentialMovingAverage { Length = MedPeriod };
		_slow = new ExponentialMovingAverage { Length = SlowPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_fast, _med, _slow, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal medValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fast.IsFormed || !_med.IsFormed || !_slow.IsFormed)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;

		// Check SL/TP
		if (Position > 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close <= _entryPrice - StopLossPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 100;
				return;
			}

			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 100;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 100;
				return;
			}

			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 100;
				return;
			}
		}

		// Double confirmation: both fast and med above slow for long
		if (fastValue > slowValue && medValue > slowValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 100;
		}
		// Double confirmation: both fast and med below slow for short
		else if (fastValue < slowValue && medValue < slowValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 100;
		}
	}
}
