using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades based on RSI Failure Swing pattern.
/// A failure swing occurs when RSI reverses direction without crossing through centerline.
/// Uses cooldown to control trade frequency.
/// </summary>
public class RsiFailureSwingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _oversoldLevel;
	private readonly StrategyParam<decimal> _overboughtLevel;
	private readonly StrategyParam<int> _cooldownBars;

	private RelativeStrengthIndex _rsi;

	private decimal _prevRsi;
	private decimal _prevPrevRsi;
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
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
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
	public RsiFailureSwingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetDisplay("RSI Period", "Period for RSI", "RSI Settings")
			.SetRange(2, 50);

		_oversoldLevel = Param(nameof(OversoldLevel), 40m)
			.SetDisplay("Oversold Level", "RSI oversold threshold", "RSI Settings")
			.SetRange(10m, 45m);

		_overboughtLevel = Param(nameof(OverboughtLevel), 60m)
			.SetDisplay("Overbought Level", "RSI overbought threshold", "RSI Settings")
			.SetRange(55m, 90m);

		_cooldownBars = Param(nameof(CooldownBars), 400)
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
		_rsi = default;
		_prevRsi = 0;
		_prevPrevRsi = 0;
		_cooldown = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Need at least 2 previous RSI values
		if (_prevRsi == 0 || _prevPrevRsi == 0)
		{
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevPrevRsi = _prevRsi;
			_prevRsi = rsiValue;
			return;
		}

		// Bullish Failure Swing: RSI was oversold, rose, pulled back but stayed above prior low
		var isBullish = _prevPrevRsi < OversoldLevel &&
			_prevRsi > _prevPrevRsi &&
			rsiValue < _prevRsi &&
			rsiValue > _prevPrevRsi;

		// Bearish Failure Swing: RSI was overbought, fell, bounced but stayed below prior high
		var isBearish = _prevPrevRsi > OverboughtLevel &&
			_prevRsi < _prevPrevRsi &&
			rsiValue > _prevRsi &&
			rsiValue < _prevPrevRsi;

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
			// Exit long when RSI crosses above overbought or reverses from peak
			if (rsiValue > OverboughtLevel || (rsiValue < 45 && _prevRsi > 45))
			{
				SellMarket();
				_cooldown = CooldownBars;
			}
		}
		else if (Position < 0)
		{
			// Exit short when RSI crosses below oversold or reverses from trough
			if (rsiValue < OversoldLevel || (rsiValue > 55 && _prevRsi < 55))
			{
				BuyMarket();
				_cooldown = CooldownBars;
			}
		}

		_prevPrevRsi = _prevRsi;
		_prevRsi = rsiValue;
	}
}
