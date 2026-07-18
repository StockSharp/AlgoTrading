using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// CCI Hook Reversal strategy.
/// Enters long when CCI hooks up from oversold zone.
/// Enters short when CCI hooks down from overbought zone.
/// Exits when CCI crosses zero.
/// Uses cooldown to control trade frequency.
/// </summary>
public class CciHookReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _oversoldLevel;
	private readonly StrategyParam<int> _overboughtLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal? _prevCci;
	private int _cooldown;

	/// <summary>
	/// CCI period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level.
	/// </summary>
	public int OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public int OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
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
	public CciHookReversalStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetRange(14, 30)
			.SetDisplay("CCI Period", "Period for CCI", "CCI");

		_oversoldLevel = Param(nameof(OversoldLevel), -100)
			.SetRange(-150, -50)
			.SetDisplay("Oversold", "Oversold level", "CCI");

		_overboughtLevel = Param(nameof(OverboughtLevel), 100)
			.SetRange(50, 150)
			.SetDisplay("Overbought", "Overbought level", "CCI");

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
		_prevCci = null;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevCci = null;
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

		if (_prevCci == null)
		{
			_prevCci = cciValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevCci = cciValue;
			return;
		}

		// Hook up from oversold
		var oversoldHookUp = _prevCci < OversoldLevel && cciValue > _prevCci;
		// Hook down from overbought
		var overboughtHookDown = _prevCci > OverboughtLevel && cciValue < _prevCci;

		if (Position == 0 && oversoldHookUp)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && overboughtHookDown)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && cciValue < 0)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && cciValue > 0)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevCci = cciValue;
	}
}
