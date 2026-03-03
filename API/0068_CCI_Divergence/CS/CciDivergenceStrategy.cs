using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Divergence strategy.
/// Detects divergences between price and CCI for reversal signals.
/// Bullish: price falling but CCI rising.
/// Bearish: price rising but CCI falling.
/// </summary>
public class CciDivergenceStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevPrice;
	private decimal _prevCci;
	private int _cooldown;

	/// <summary>
	/// CCI Period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
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
	/// Cooldown bars.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public CciDivergenceStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetRange(5, 30)
			.SetDisplay("CCI Period", "Period for CCI", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_cooldownBars = Param(nameof(CooldownBars), 500)
			.SetRange(1, 1000)
			.SetDisplay("Cooldown Bars", "Bars to wait between trades", "General");
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
		_prevPrice = default;
		_prevCci = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevPrice = 0;
		_prevCci = 0;
		_cooldown = 0;

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevPrice == 0)
		{
			_prevPrice = candle.ClosePrice;
			_prevCci = cciValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrice = candle.ClosePrice;
			_prevCci = cciValue;
			return;
		}

		var bullishDiv = candle.ClosePrice < _prevPrice && cciValue > _prevCci;
		var bearishDiv = candle.ClosePrice > _prevPrice && cciValue < _prevCci;

		if (Position == 0 && bullishDiv && cciValue > -100)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bearishDiv && cciValue < 100)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && cciValue > 100)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && cciValue < -100)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevPrice = candle.ClosePrice;
		_prevCci = cciValue;
	}
}
