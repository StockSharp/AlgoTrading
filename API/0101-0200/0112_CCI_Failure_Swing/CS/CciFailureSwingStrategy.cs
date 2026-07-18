using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on CCI Failure Swing pattern.
/// A failure swing occurs when CCI reverses direction without crossing through centerline.
/// Uses cooldown to control trade frequency.
/// </summary>
public class CciFailureSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private CommodityChannelIndex _cci;

	private decimal _prevCci;
	private decimal _prevPrevCci;
	private int _cooldown;

	/// <summary>
	/// Candle type and timeframe.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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
	/// Oversold level.
	/// </summary>
	public decimal OversoldLevel
	{
		get => _oversoldLevel.Value;
		set => _oversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought level.
	/// </summary>
	public decimal OverboughtLevel
	{
		get => _overboughtLevel.Value;
		set => _overboughtLevel.Value = value;
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
	/// Constructor.
	/// </summary>
	public CciFailureSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_cciPeriod = Param(nameof(CciPeriod), 20)
			.SetDisplay("CCI Period", "Period for CCI", "CCI Settings")
			.SetRange(5, 50);

		_oversoldLevel = Param(nameof(OversoldLevel), -50m)
			.SetDisplay("Oversold Level", "CCI oversold threshold", "CCI Settings")
			.SetRange(-200m, -20m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 50m)
			.SetDisplay("Overbought Level", "CCI overbought threshold", "CCI Settings")
			.SetRange(20m, 200m);

		_cooldownBars = Param(nameof(CooldownBars), 350)
			.SetDisplay("Cooldown Bars", "Bars between trades", "General")
			.SetRange(10, 2000);
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
		_cci = default;
		_prevCci = 0;
		_prevPrevCci = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Need at least 2 previous CCI values
		if (_prevCci == 0 || _prevPrevCci == 0)
		{
			_prevPrevCci = _prevCci;
			_prevCci = cciValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevCci = _prevCci;
			_prevCci = cciValue;
			return;
		}

		// Bullish Failure Swing: CCI was oversold, rose, pulled back but stayed above prior low
		var isBullish = _prevPrevCci < OversoldLevel &&
			_prevCci > _prevPrevCci &&
			cciValue < _prevCci &&
			cciValue > _prevPrevCci;

		// Bearish Failure Swing: CCI was overbought, fell, bounced but stayed below prior high
		var isBearish = _prevPrevCci > OverboughtLevel &&
			_prevCci < _prevPrevCci &&
			cciValue > _prevCci &&
			cciValue < _prevPrevCci;

		if (Position == 0)
		{
			if (isBullish)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
			else if (isBearish)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position > 0)
		{
			// Exit long when CCI crosses above overbought
			if (cciValue > OverboughtLevel)
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short when CCI crosses below oversold
			if (cciValue < OversoldLevel)
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevPrevCci = _prevCci;
		_prevCci = cciValue;
	}
}
