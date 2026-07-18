using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Tunnel strategy that uses Bollinger Bands to define a price channel.
/// Buys when price crosses above the lower band (reversal from oversold),
/// sells when price crosses below the upper band (reversal from overbought).
/// </summary>
public class TunnelGen4Strategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbWidth;
	private readonly StrategyParam<decimal> _stepPips;

	private BollingerBands _bb;

	private decimal _prevClose;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal _entryPrice;

	/// <summary>
	/// Bollinger Bands period length.
	/// </summary>
	public int BbLength
	{
		get => _bbLength.Value;
		set => _bbLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands width (standard deviations).
	/// </summary>
	public decimal BbWidth
	{
		get => _bbWidth.Value;
		set => _bbWidth.Value = value;
	}

	/// <summary>
	/// Step distance expressed in pips for profit target.
	/// </summary>
	public decimal StepPips
	{
		get => _stepPips.Value;
		set => _stepPips.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public TunnelGen4Strategy()
	{
		_bbLength = Param(nameof(BbLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger Bands period", "Indicator");

		_bbWidth = Param(nameof(BbWidth), 2.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Width", "Bollinger Bands width", "Indicator");

		_stepPips = Param(nameof(StepPips), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Step (pips)", "Distance between tunnel anchors", "Trading");
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

		_bb = null;
		_prevClose = 0;
		_prevUpper = 0;
		_prevLower = 0;
		_entryPrice = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_bb = new BollingerBands
		{
			Length = BbLength,
			Width = BbWidth
		};

		var subscription = SubscribeCandles(TimeSpan.FromMinutes(5).TimeFrame());
		subscription.BindEx(_bb, OnProcess);
		subscription.Start();
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bb = (BollingerBandsValue)value;
		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower)
			return;

		if (!_bb.IsFormed)
		{
			_prevClose = candle.ClosePrice;
			_prevUpper = upper;
			_prevLower = lower;
			return;
		}

		var close = candle.ClosePrice;

		// Buy signal: price crosses above lower band from below
		if (_prevClose < _prevLower && close >= lower && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();

			BuyMarket();
			_entryPrice = close;
		}
		// Sell signal: price crosses below upper band from above
		else if (_prevClose > _prevUpper && close <= upper && Position >= 0)
		{
			if (Position > 0)
				SellMarket();

			SellMarket();
			_entryPrice = close;
		}

		// Exit on profit target if in position
		if (Position > 0 && _entryPrice > 0)
		{
			var pipValue = Security?.PriceStep ?? 1m;
			var target = _entryPrice + StepPips * pipValue;
			if (close >= target)
			{
				SellMarket();
				_entryPrice = 0;
			}
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var pipValue = Security?.PriceStep ?? 1m;
			var target = _entryPrice - StepPips * pipValue;
			if (close <= target)
			{
				BuyMarket();
				_entryPrice = 0;
			}
		}

		_prevClose = close;
		_prevUpper = upper;
		_prevLower = lower;
	}
}
