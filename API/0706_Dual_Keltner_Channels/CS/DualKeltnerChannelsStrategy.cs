using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using dual Keltner Channels. A position is opened after price pierces the outer band and then returns through the inner band.
/// </summary>
public class DualKeltnerChannelsStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _innerMultiplier;
	private readonly StrategyParam<decimal> _outerMultiplier;
	private readonly StrategyParam<decimal> _maxStopPercent;
	private readonly StrategyParam<decimal> _slTpRatio;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevPrice;
	private bool _waitLong;
	private bool _waitShort;

	/// <summary>
	/// EMA period for Keltner Channels.
	/// </summary>
	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for inner Keltner Channel.
	/// </summary>
	public decimal InnerMultiplier
	{
		get => _innerMultiplier.Value;
		set => _innerMultiplier.Value = value;
	}

	/// <summary>
	/// Multiplier for outer Keltner Channel.
	/// </summary>
	public decimal OuterMultiplier
	{
		get => _outerMultiplier.Value;
		set => _outerMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss percentage.
	/// </summary>
	public decimal MaxStopPercent
	{
		get => _maxStopPercent.Value;
		set => _maxStopPercent.Value = value;
	}

	/// <summary>
	/// Take-profit ratio to stop-loss.
	/// </summary>
	public decimal SlTpRatio
	{
		get => _slTpRatio.Value;
		set => _slTpRatio.Value = value;
	}

	/// <summary>
	/// Candle type for strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="DualKeltnerChannelsStrategy"/>.
	/// </summary>
	public DualKeltnerChannelsStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 50)
			.SetRange(10, 100)
			.SetDisplay("EMA Period", "EMA period for Keltner Channels", "Keltner");

		_innerMultiplier = Param(nameof(InnerMultiplier), 2.75m)
			.SetRange(1m, 5m)
			.SetDisplay("Inner Multiplier", "Inner Keltner multiplier", "Keltner");

		_outerMultiplier = Param(nameof(OuterMultiplier), 3.75m)
			.SetRange(1m, 6m)
			.SetDisplay("Outer Multiplier", "Outer Keltner multiplier", "Keltner");

		_maxStopPercent = Param(nameof(MaxStopPercent), 10m)
			.SetRange(1m, 20m)
			.SetDisplay("Max Stop %", "Stop-loss percent from entry", "Risk Management");

		_slTpRatio = Param(nameof(SlTpRatio), 1m)
			.SetRange(0.5m, 3m)
			.SetDisplay("SLTP Ratio", "Take profit ratio to stop-loss", "Risk Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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

		_prevPrice = 0m;
		_waitLong = false;
		_waitShort = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var inner = new KeltnerChannels
		{
			Length = EmaPeriod,
			Multiplier = InnerMultiplier,
		};

		var outer = new KeltnerChannels
		{
			Length = EmaPeriod,
			Multiplier = OuterMultiplier,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(inner, outer, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(MaxStopPercent * SlTpRatio, UnitTypes.Percent),
			stopLoss: new Unit(MaxStopPercent, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, inner);
			DrawIndicator(area, outer);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue innerValue, IIndicatorValue outerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevPrice = candle.ClosePrice;
			return;
		}

		var inner = (KeltnerChannelsValue)innerValue;
		var outer = (KeltnerChannelsValue)outerValue;

		var price = candle.ClosePrice;

		var crossUnderOuter = _prevPrice >= outer.Lower && price < outer.Lower;
		var crossOverInner = _prevPrice <= inner.Lower && price > inner.Lower;

		var crossOverOuter = _prevPrice <= outer.Upper && price > outer.Upper;
		var crossUnderInner = _prevPrice >= inner.Upper && price < inner.Upper;

		if (crossUnderOuter)
			_waitLong = true;

		if (crossOverOuter)
			_waitShort = true;

		if (_waitLong && crossOverInner && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_waitLong = false;
		}
		else if (_waitShort && crossUnderInner && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_waitShort = false;
		}

		if (Position > 0 && crossOverOuter)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && crossUnderOuter)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevPrice = price;
	}
}
