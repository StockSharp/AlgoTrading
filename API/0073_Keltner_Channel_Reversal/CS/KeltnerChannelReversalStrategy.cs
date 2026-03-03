using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Keltner Channel Reversal strategy.
/// Enters long when price is below lower Keltner Channel with a bullish candle.
/// Enters short when price is above upper Keltner Channel with a bearish candle.
/// Exits at middle band.
/// </summary>
public class KeltnerChannelReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private int _cooldown;

	/// <summary>
	/// EMA period for Keltner Channel.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier for Keltner Channel.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public KeltnerChannelReversalStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "Period for EMA in Keltner Channel", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2.0m)
			.SetNotNegative()
			.SetDisplay("ATR Multiplier", "Multiplier for ATR in Keltner Channel", "Indicators");

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
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_cooldown = 0;

		var keltner = new KeltnerChannels
		{
			Length = EmaPeriod,
			Multiplier = AtrMultiplier,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(keltner, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, keltner);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue keltnerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!keltnerValue.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_cooldown > 0)
		{
			_cooldown--;
			return;
		}

		var kc = (IKeltnerChannelsValue)keltnerValue;
		var upper = kc.Upper;
		var lower = kc.Lower;
		var middle = kc.Middle;

		if (upper == null || lower == null || middle == null)
			return;

		var isBullish = candle.ClosePrice > candle.OpenPrice;
		var isBearish = candle.ClosePrice < candle.OpenPrice;

		if (Position == 0 && candle.ClosePrice < lower.Value && isBullish)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
		else if (Position == 0 && candle.ClosePrice > upper.Value && isBearish)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position > 0 && candle.ClosePrice > middle.Value)
		{
			SellMarket();
			_cooldown = CooldownBars;
		}
		else if (Position < 0 && candle.ClosePrice < middle.Value)
		{
			BuyMarket();
			_cooldown = CooldownBars;
		}
	}
}
