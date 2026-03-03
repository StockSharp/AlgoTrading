using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Donchian Reversal strategy.
/// Enters long when price bounces from the lower Donchian Channel band.
/// Enters short when price bounces from the upper Donchian Channel band.
/// Exits at middle band.
/// Uses cooldown to control trade frequency.
/// </summary>
public class DonchianReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _period;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _prevClose;
	private int _cooldown;

	/// <summary>
	/// Donchian period.
	/// </summary>
	public int Period
	{
		get => _period.Value;
		set => _period.Value = value;
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
	public DonchianReversalStrategy()
	{
		_period = Param(nameof(Period), 20)
			.SetRange(10, 40)
			.SetDisplay("Period", "Period for Donchian Channel", "Indicators");

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
		_prevClose = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = 0;
		_cooldown = 0;

		var donchian = new DonchianChannels { Length = Period };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianIv)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!donchianIv.IsFormed)
			return;

		var dv = (IDonchianChannelsValue)donchianIv;

		if (dv.UpperBand is not decimal upper ||
			dv.LowerBand is not decimal lower ||
			dv.Middle is not decimal middle)
			return;

		if (_prevClose == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevClose = candle.ClosePrice;
			return;
		}

		// Bounce from lower band = bullish
		var bouncedFromLower = _prevClose <= lower && candle.ClosePrice > lower;
		// Bounce from upper band = bearish
		var bouncedFromUpper = _prevClose >= upper && candle.ClosePrice < upper;

		if (Position == 0 && bouncedFromLower)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && bouncedFromUpper)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice >= middle && bouncedFromUpper)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice <= middle && bouncedFromLower)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}

		_prevClose = candle.ClosePrice;
	}
}
