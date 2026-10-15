using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bollinger Bands breakout scalping strategy.
/// Buys when price breaks below lower band (mean reversion) and sells when price breaks above upper band.
/// Uses stop-loss and take-profit for risk management.
/// </summary>
public class ScalpWiz9001Strategy : Strategy
{
	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;

	private BollingerBands _bollinger;

	private decimal _entryPrice;
	private int _cooldown;

	/// <summary>
	/// Bollinger Bands lookback period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price steps.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance in price steps.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public ScalpWiz9001Strategy()
	{
		_bandsPeriod = Param(nameof(BandsPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bands Period", "Bollinger Bands period", "Indicator");

		_bandsDeviation = Param(nameof(BandsDeviation), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bands Deviation", "Bollinger Bands deviation", "Indicator");

		_stopLossPips = Param(nameof(StopLossPips), 100)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
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

		_bollinger = null;
		_entryPrice = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bollinger = new BollingerBands
		{
			Length = BandsPeriod,
			Width = BandsDeviation
		};

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.BindEx(_bollinger, ProcessCandle);
		subscription.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)value;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower)
			return;

		if (!_bollinger.IsFormed)
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
			if (StopLossPips > 0 && close <= _entryPrice - StopLossPips * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 5;
				return;
			}

			if (TakeProfitPips > 0 && close >= _entryPrice + TakeProfitPips * step)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 5;
				return;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			if (StopLossPips > 0 && close >= _entryPrice + StopLossPips * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 5;
				return;
			}

			if (TakeProfitPips > 0 && close <= _entryPrice - TakeProfitPips * step)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 5;
				return;
			}
		}

		// Entry signals
		if (Position == 0)
		{
			// Buy when price touches lower band (mean reversion up)
			if (close <= lower)
			{
				BuyMarket();
				_entryPrice = close;
				_cooldown = 5;
			}
			// Sell when price touches upper band (mean reversion down)
			else if (close >= upper)
			{
				SellMarket();
				_entryPrice = close;
				_cooldown = 5;
			}
		}
	}
}
