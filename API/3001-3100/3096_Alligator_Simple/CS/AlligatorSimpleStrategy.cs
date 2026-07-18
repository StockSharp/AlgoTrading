using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified Bill Williams Alligator breakout strategy.
/// Buys when Lips > Teeth > Jaw (upward expansion).
/// Sells when Lips less than Teeth less than Jaw (downward expansion).
/// Uses stop-loss and take-profit for risk management.
/// </summary>
public class AlligatorSimpleStrategy : Strategy
{
	private readonly StrategyParam<int> _jawPeriod;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<int> _lipsPeriod;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;

	private SmoothedMovingAverage _jaw;
	private SmoothedMovingAverage _teeth;
	private SmoothedMovingAverage _lips;

	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// Period for the Alligator jaw (blue) smoothed moving average.
	/// </summary>
	public int JawPeriod
	{
		get => _jawPeriod.Value;
		set => _jawPeriod.Value = value;
	}

	/// <summary>
	/// Period for the Alligator teeth (red) smoothed moving average.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Period for the Alligator lips (green) smoothed moving average.
	/// </summary>
	public int LipsPeriod
	{
		get => _lipsPeriod.Value;
		set => _lipsPeriod.Value = value;
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
	/// Initialize <see cref="AlligatorSimpleStrategy"/>.
	/// </summary>
	public AlligatorSimpleStrategy()
	{
		_jawPeriod = Param(nameof(JawPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("Jaw Period", "Alligator jaw period", "Alligator");

		_teethPeriod = Param(nameof(TeethPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Alligator teeth period", "Alligator");

		_lipsPeriod = Param(nameof(LipsPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lips Period", "Alligator lips period", "Alligator");

		_stopLossPoints = Param(nameof(StopLossPoints), 200)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400)
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

		_jaw = null;
		_teeth = null;
		_lips = null;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_jaw = new SmoothedMovingAverage { Length = JawPeriod };
		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };
		_lips = new SmoothedMovingAverage { Length = LipsPeriod };

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.Bind(_jaw, _teeth, _lips, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal jawValue, decimal teethValue, decimal lipsValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_jaw.IsFormed || !_teeth.IsFormed || !_lips.IsFormed)
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var close = candle.ClosePrice;
		var step = Security?.PriceStep ?? 1m;

		// Check SL/TP for existing positions
		if (Position > 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close <= _entryPrice - StopLossPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 110;
				return;
			}

			if (TakeProfitPoints > 0 && close >= _entryPrice + TakeProfitPoints * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 110;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPoints > 0 && close >= _entryPrice + StopLossPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 110;
				return;
			}

			if (TakeProfitPoints > 0 && close <= _entryPrice - TakeProfitPoints * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 110;
				return;
			}
		}

		// Buy when lips > teeth > jaw (Alligator opening upward)
		if (lipsValue > teethValue && teethValue > jawValue && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
			_cooldown = 110;
		}
		// Sell when lips < teeth < jaw (Alligator opening downward)
		else if (lipsValue < teethValue && teethValue < jawValue && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
			_cooldown = 110;
		}
	}
}
