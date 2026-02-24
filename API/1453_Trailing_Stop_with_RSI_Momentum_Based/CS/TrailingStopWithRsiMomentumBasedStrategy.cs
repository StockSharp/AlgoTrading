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
/// Momentum breakout with RSI filter and trailing stop.
/// </summary>
public class TrailingStopWithRsiMomentumBasedStrategy : Strategy
{
	private readonly StrategyParam<int> _momentumLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevMomentum;
	private decimal _entryPrice;
	private bool _trailActive;
	private decimal _trailLevel;

	public int MomentumLength { get => _momentumLength.Value; set => _momentumLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }
	public decimal TrailPercent { get => _trailPercent.Value; set => _trailPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TrailingStopWithRsiMomentumBasedStrategy()
	{
		_momentumLength = Param(nameof(MomentumLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Length", "Momentum period", "Parameters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Parameters");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "Overbought level", "Parameters");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Oversold level", "Parameters");

		_trailPercent = Param(nameof(TrailPercent), 1m)
			.SetDisplay("Trail %", "Trailing distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe", "General");
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
		_prevMomentum = 0m;
		_entryPrice = 0m;
		_trailActive = false;
		_trailLevel = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var momentum = new Momentum { Length = MomentumLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(momentum, rsi, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal momentumValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var mom0 = momentumValue;
		var momChange = mom0 - _prevMomentum;

		// Trailing stop management
		if (Position > 0)
		{
			if (!_trailActive)
			{
				_trailActive = true;
				_trailLevel = candle.HighPrice;
			}
			else
			{
				_trailLevel = Math.Max(_trailLevel, candle.HighPrice);
				var stop = _trailLevel * (1 - TrailPercent / 100m);
				if (candle.LowPrice <= stop)
				{
					SellMarket();
					_trailActive = false;
					_prevMomentum = mom0;
					return;
				}
			}
		}
		else if (Position < 0)
		{
			if (!_trailActive)
			{
				_trailActive = true;
				_trailLevel = candle.LowPrice;
			}
			else
			{
				_trailLevel = Math.Min(_trailLevel, candle.LowPrice);
				var stop = _trailLevel * (1 + TrailPercent / 100m);
				if (candle.HighPrice >= stop)
				{
					BuyMarket();
					_trailActive = false;
					_prevMomentum = mom0;
					return;
				}
			}
		}
		else
		{
			_trailActive = false;
		}

		// Entry: RSI + momentum confirmation
		if (rsiValue >= RsiOverbought && mom0 > 0 && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_trailActive = false;
		}
		else if (rsiValue <= RsiOversold && mom0 < 0 && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_trailActive = false;
		}

		_prevMomentum = mom0;
	}
}
