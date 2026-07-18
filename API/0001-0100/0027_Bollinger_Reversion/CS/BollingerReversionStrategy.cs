using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bollinger Bands mean reversion.
/// Enters when price touches bands, exits when price returns to middle.
/// </summary>
public class BollingerReversionStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<int> _maxHoldBars;

	private int _cooldown;
	private int _holdBars;

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
	/// Cooldown bars between trades.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Maximum bars to hold position before forced exit.
	/// </summary>
	public int MaxHoldBars
	{
		get => _maxHoldBars.Value;
		set => _maxHoldBars.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerReversionStrategy"/>.
	/// </summary>
	public BollingerReversionStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetRange(5, 50)
			.SetDisplay("Bollinger Period", "Period for Bollinger Bands calculation", "Indicators")
			;

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.0m)
			.SetRange(0.5m, 4m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
			;

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Number of bars to wait between trades", "General");

		_maxHoldBars = Param(nameof(MaxHoldBars), 300)
			.SetRange(1, 1000)
			.SetDisplay("Max Hold Bars", "Maximum bars to hold a position", "General");
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
		_cooldown = default;
		_holdBars = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cooldown = 0;
		_holdBars = 0;

		var bollingerBands = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollingerBands, ProcessCandle)
			.Start();

		// Setup chart visualization
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollingerBands);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!bollingerValue.IsFormed)
			return;

		// Track hold duration
		if (Position != 0)
			_holdBars++;
		else
			_holdBars = 0;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var bb = (BollingerBandsValue)bollingerValue;

		if (bb.UpBand is not decimal upper ||
			bb.LowBand is not decimal lower ||
			bb.MovingAverage is not decimal middle)
			return;

		var close = candle.ClosePrice;

		// Exit logic: revert to middle or time-based forced exit
		if (Position > 0 && (close >= middle || _holdBars >= MaxHoldBars))
		{
			SellMarket();
			_cooldown = CooldownBars;
			_holdBars = 0;
			return;
		}

		if (Position < 0 && (close <= middle || _holdBars >= MaxHoldBars))
		{
			BuyMarket();
			_cooldown = CooldownBars;
			_holdBars = 0;
			return;
		}

		// Entry logic - buy below lower band, sell above upper band
		if (Position == 0 && close < lower)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && close > upper)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
	}
}
