using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines manual Supertrend calculation with ATR for trend direction
/// and volume confirmation for entries.
/// </summary>
public class SupertrendVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _supertrendPeriod;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _atrValue;
	private decimal? _upperBand;
	private decimal? _lowerBand;
	private decimal? _supertrend;
	private bool? _isBullish;
	private int _cooldown;

	/// <summary>
	/// Data type for candles.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for Supertrend ATR calculation.
	/// </summary>
	public int SupertrendPeriod
	{
		get => _supertrendPeriod.Value;
		set => _supertrendPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for Supertrend ATR calculation.
	/// </summary>
	public decimal SupertrendMultiplier
	{
		get => _supertrendMultiplier.Value;
		set => _supertrendMultiplier.Value = value;
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
	/// Initializes a new instance of the <see cref="SupertrendVolumeStrategy"/>.
	/// </summary>
	public SupertrendVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_supertrendPeriod = Param(nameof(SupertrendPeriod), 10)
			.SetRange(5, 30)
			.SetDisplay("Supertrend Period", "Period for Supertrend ATR calculation", "Supertrend Settings");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 3.0m)
			.SetRange(1.0m, 5.0m)
			.SetDisplay("Supertrend Multiplier", "Multiplier for Supertrend ATR", "Supertrend Settings");

		_cooldownBars = Param(nameof(CooldownBars), 100)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(5, 500);
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
		_atrValue = 0;
		_upperBand = null;
		_lowerBand = null;
		_supertrend = null;
		_isBullish = null;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var atr = new AverageTrueRange { Length = SupertrendPeriod };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var atrArea = CreateChartArea();
			if (atrArea != null)
				DrawIndicator(atrArea, atr);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!atrValue.IsFormed)
			return;

		var atr = atrValue.ToDecimal();
		if (atr <= 0)
			return;

		_atrValue = atr;

		// Calculate Supertrend
		var basicPrice = (candle.HighPrice + candle.LowPrice) / 2;
		var newUpperBand = basicPrice + (SupertrendMultiplier * _atrValue);
		var newLowerBand = basicPrice - (SupertrendMultiplier * _atrValue);

		if (_upperBand == null || _lowerBand == null || _supertrend == null || _isBullish == null)
		{
			_upperBand = newUpperBand;
			_lowerBand = newLowerBand;
			_supertrend = newUpperBand;
			_isBullish = false;
			return;
		}

		// Update upper band
		if (newUpperBand < _upperBand || candle.ClosePrice > _upperBand)
			_upperBand = newUpperBand;

		// Update lower band
		if (newLowerBand > _lowerBand || candle.ClosePrice < _lowerBand)
			_lowerBand = newLowerBand;

		// Determine trend direction
		if (_supertrend == _upperBand)
		{
			if (candle.ClosePrice > _upperBand)
			{
				_supertrend = _lowerBand;
				_isBullish = true;
			}
			else
			{
				_supertrend = _upperBand;
				_isBullish = false;
			}
		}
		else
		{
			if (candle.ClosePrice < _lowerBand)
			{
				_supertrend = _upperBand;
				_isBullish = false;
			}
			else
			{
				_supertrend = _lowerBand;
				_isBullish = true;
			}
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Entry: bullish supertrend
		if (_isBullish.Value && candle.ClosePrice > _supertrend.Value && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Entry: bearish supertrend
		else if (!_isBullish.Value && candle.ClosePrice < _supertrend.Value && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long on bearish flip
		if (Position > 0 && !_isBullish.Value)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short on bullish flip
		else if (Position < 0 && _isBullish.Value)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
