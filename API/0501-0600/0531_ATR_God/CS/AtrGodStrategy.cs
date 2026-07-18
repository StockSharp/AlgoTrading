using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Supertrend indicator with ATR-based risk management.
/// Trades supertrend direction changes with cooldown.
/// </summary>
public class AtrGodStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private bool _prevIsPriceAboveSupertrend;
	private decimal _prevSupertrendValue;
	private int _barIndex;
	private int _lastTradeBar;

	/// <summary>
	/// ATR period for Supertrend calculation.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Supertrend.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
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
	/// Initialize strategy parameters.
	/// </summary>
	public AtrGodStrategy()
	{
		_period = Param(nameof(Period), 10)
			.SetDisplay("Period", "ATR period for Supertrend", "Indicators");

		_multiplier = Param(nameof(Multiplier), 3m)
			.SetDisplay("Multiplier", "ATR multiplier for Supertrend", "Indicators");

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevIsPriceAboveSupertrend = false;
		_prevSupertrendValue = 0m;
		_barIndex = 0;
		_lastTradeBar = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barIndex++;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2;
		var basicUpper = medianPrice + Multiplier * atrValue;
		var basicLower = medianPrice - Multiplier * atrValue;

		decimal supertrendValue;

		if (_prevSupertrendValue == 0m)
		{
			supertrendValue = candle.ClosePrice > medianPrice ? basicLower : basicUpper;
			_prevSupertrendValue = supertrendValue;
			_prevIsPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
			return;
		}

		if (_prevSupertrendValue <= candle.HighPrice)
		{
			supertrendValue = Math.Max(basicLower, _prevSupertrendValue);
		}
		else if (_prevSupertrendValue >= candle.LowPrice)
		{
			supertrendValue = Math.Min(basicUpper, _prevSupertrendValue);
		}
		else
		{
			supertrendValue = candle.ClosePrice > _prevSupertrendValue ? basicLower : basicUpper;
		}

		var isPriceAboveSupertrend = candle.ClosePrice > supertrendValue;
		var crossedAbove = isPriceAboveSupertrend && !_prevIsPriceAboveSupertrend;
		var crossedBelow = !isPriceAboveSupertrend && _prevIsPriceAboveSupertrend;

		var cooldownOk = _barIndex - _lastTradeBar > CooldownBars;

		if (crossedAbove && Position <= 0 && cooldownOk)
		{
			BuyMarket();
			_lastTradeBar = _barIndex;
		}
		else if (crossedBelow && Position >= 0 && cooldownOk)
		{
			SellMarket();
			_lastTradeBar = _barIndex;
		}

		_prevSupertrendValue = supertrendValue;
		_prevIsPriceAboveSupertrend = isPriceAboveSupertrend;
	}
}
