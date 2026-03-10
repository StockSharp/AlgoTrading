using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the RSI crossover with an EMA of RSI (RSIOMA style).
/// Buys when RSI crosses above its EMA and sells when RSI crosses below.
/// </summary>
public class ExpRsiomaStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevRsi;
	private decimal _prevEma;
	private bool _hasPrev;

	/// <summary>RSI calculation length.</summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>EMA smoothing period.</summary>
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }

	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public ExpRsiomaStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 21)
			.SetDisplay("RSI Period", "RSI calculation length", "Parameters")
			.SetGreaterThanZero();

		_emaPeriod = Param(nameof(EmaPeriod), 14)
			.SetDisplay("EMA Period", "EMA smoothing period", "Parameters")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevRsi = 0m;
		_prevEma = 0m;
		_hasPrev = false;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_hasPrev = false;

		var rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		var ema = new ExponentialMovingAverage { Length = EmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(rsi, ema, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal emaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_hasPrev)
		{
			var crossUp = _prevRsi <= _prevEma && rsiValue > emaValue;
			var crossDown = _prevRsi >= _prevEma && rsiValue < emaValue;

			if (crossUp && rsiValue > 52m && emaValue > 50m && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (crossDown && rsiValue < 48m && emaValue < 50m && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}

		_prevRsi = rsiValue;
		_prevEma = emaValue;
		_hasPrev = true;
	}
}
