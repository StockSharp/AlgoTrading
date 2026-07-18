using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on candle direction changes, inspired by Renko-style logic.
/// Buys when candle direction flips from down to up, sells when it flips from up to down.
/// Uses ATR-based filter to only trade on significant candles.
/// </summary>
public class RenkoChartFromTicksStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _bodyAtrFactor;
	private readonly StrategyParam<int> _cooldownCandles;

	private bool? _prevUp;
	private int _barsSinceSignal;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	public decimal BodyAtrFactor
	{
		get => _bodyAtrFactor.Value;
		set => _bodyAtrFactor.Value = value;
	}

	public int CooldownCandles
	{
		get => _cooldownCandles.Value;
		set => _cooldownCandles.Value = value;
	}

	public RenkoChartFromTicksStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for significance filter", "General");
		_bodyAtrFactor = Param(nameof(BodyAtrFactor), 0.7m)
			.SetGreaterThanZero()
			.SetDisplay("Body ATR Factor", "Minimum body size as ATR fraction", "General");
		_cooldownCandles = Param(nameof(CooldownCandles), 2)
			.SetGreaterThanZero()
			.SetDisplay("Cooldown Candles", "Minimum candles between signals", "General");
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

		_prevUp = null;
		_barsSinceSignal = CooldownCandles;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevUp = null;
		_barsSinceSignal = CooldownCandles;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, atr);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_barsSinceSignal++;

		if (atrValue <= 0)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);

		if (body < atrValue * BodyAtrFactor)
			return;

		var isUp = candle.ClosePrice > candle.OpenPrice;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevUp = isUp;
			return;
		}

		if (_prevUp.HasValue && _prevUp.Value != isUp && _barsSinceSignal >= CooldownCandles)
		{
			if (isUp && Position <= 0)
			{
				BuyMarket();
				_barsSinceSignal = 0;
			}
			else if (!isUp && Position >= 0)
			{
				SellMarket();
				_barsSinceSignal = 0;
			}
		}

		_prevUp = isUp;
	}
}
