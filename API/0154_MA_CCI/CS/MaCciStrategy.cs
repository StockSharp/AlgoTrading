using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining Moving Average and CCI indicators.
/// Buys when price is above MA and CCI is oversold.
/// Sells when price is below MA and CCI is overbought.
/// </summary>
public class MaCciStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _cciValue;
	private int _cooldown;

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// MA period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// CCI overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
	}

	/// <summary>
	/// CCI oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
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
	/// Initialize strategy.
	/// </summary>
	public MaCciStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetRange(10, 50)
			.SetDisplay("MA Period", "Period for Moving Average", "Indicators");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(10, 30)
			.SetDisplay("CCI Period", "Period for CCI calculation", "Indicators");

		_overboughtLevel = Param(nameof(OverboughtLevel), 100m)
			.SetDisplay("Overbought Level", "CCI level considered overbought", "Trading Levels");

		_oversoldLevel = Param(nameof(OversoldLevel), -100m)
			.SetDisplay("Oversold Level", "CCI level considered oversold", "Trading Levels");

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
		_cciValue = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = MaPeriod };
		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);

		// CCI takes candle input - use BindEx as side handler
		subscription.BindEx(cci, OnCci);

		// EMA for main logic
		subscription
			.Bind(ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var cciArea = CreateChartArea();
			if (cciArea != null)
				DrawIndicator(cciArea, cci);
		}
	}

	private void OnCci(ICandleMessage candle, IIndicatorValue value)
	{
		if (!value.IsEmpty)
			_cciValue = value.ToDecimal();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		// Buy: price above MA + CCI oversold
		if (close > maValue && _cciValue < OversoldLevel && Position == 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		// Sell: price below MA + CCI overbought
		else if (close < maValue && _cciValue > OverboughtLevel && Position == 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}

		// Exit long: price crosses below MA
		if (Position > 0 && close < maValue)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		// Exit short: price crosses above MA
		else if (Position < 0 && close > maValue)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
