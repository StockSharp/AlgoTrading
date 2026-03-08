using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Order stabilization strategy - trades breakouts after periods of low volatility.
/// When candle body is small (stabilization), waits for a directional breakout.
/// Goes long on bullish breakout candle after stabilization, short on bearish.
/// </summary>
public class OrderStabilizationStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stabilizationFactor;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevBody;
	private decimal _prevAtr;
	private bool _hasPrev;

	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal StabilizationFactor { get => _stabilizationFactor.Value; set => _stabilizationFactor.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public OrderStabilizationStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetDisplay("ATR Period", "ATR period for volatility", "Indicators");

		_stabilizationFactor = Param(nameof(StabilizationFactor), 0.5m)
			.SetDisplay("Stabilization Factor", "Body must be less than ATR * factor for stabilization", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
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
		_prevBody = 0m;
		_prevAtr = 0m;
		_hasPrev = false;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var threshold = atrValue * StabilizationFactor;

		if (!_hasPrev)
		{
			_prevBody = body;
			_prevAtr = atrValue;
			_hasPrev = true;
			return;
		}

		var prevThreshold = _prevAtr * StabilizationFactor;
		var wasStabilized = _prevBody < prevThreshold;

		// After stabilization, trade breakout candle
		if (wasStabilized && body > threshold)
		{
			var bullish = candle.ClosePrice > candle.OpenPrice;

			if (bullish && Position <= 0)
			{
				if (Position < 0)
					BuyMarket();
				BuyMarket();
			}
			else if (!bullish && Position >= 0)
			{
				if (Position > 0)
					SellMarket();
				SellMarket();
			}
		}

		_prevBody = body;
		_prevAtr = atrValue;
	}
}
