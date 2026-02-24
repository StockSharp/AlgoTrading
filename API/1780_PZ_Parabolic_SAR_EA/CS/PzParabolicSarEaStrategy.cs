using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Parabolic SAR strategy with ATR-based trailing stop.
/// Uses two SARs - one for entry signals and one for exit signals.
/// </summary>
public class PzParabolicSarEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeStep;
	private readonly StrategyParam<decimal> _tradeMax;
	private readonly StrategyParam<decimal> _stopStep;
	private readonly StrategyParam<decimal> _stopMax;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _entryPrice;
	private decimal _trailStop;

	public decimal TradeStep { get => _tradeStep.Value; set => _tradeStep.Value = value; }
	public decimal TradeMax { get => _tradeMax.Value; set => _tradeMax.Value = value; }
	public decimal StopStep { get => _stopStep.Value; set => _stopStep.Value = value; }
	public decimal StopMax { get => _stopMax.Value; set => _stopMax.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public PzParabolicSarEaStrategy()
	{
		_tradeStep = Param(nameof(TradeStep), 0.002m)
			.SetDisplay("Trade SAR Step", "Acceleration step for entry SAR", "Indicators");
		_tradeMax = Param(nameof(TradeMax), 0.2m)
			.SetDisplay("Trade SAR Max", "Maximum acceleration for entry SAR", "Indicators");
		_stopStep = Param(nameof(StopStep), 0.004m)
			.SetDisplay("Stop SAR Step", "Acceleration step for exit SAR", "Indicators");
		_stopMax = Param(nameof(StopMax), 0.4m)
			.SetDisplay("Stop SAR Max", "Maximum acceleration for exit SAR", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 30)
			.SetDisplay("ATR Period", "ATR period for stop distance", "Risk");
		_atrMultiplier = Param(nameof(AtrMultiplier), 2.5m)
			.SetDisplay("ATR Mult", "ATR multiplier for trailing stop", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_entryPrice = 0;
		_trailStop = 0;

		var tradeSar = new ParabolicSar { AccelerationStep = TradeStep, AccelerationMax = TradeMax };
		var stopSar = new ParabolicSar { AccelerationStep = StopStep, AccelerationMax = StopMax };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(tradeSar, stopSar, atr, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal tradeSar, decimal stopSar, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Exit on stop SAR flip or trailing stop hit
		if (Position > 0)
		{
			// Update trailing stop
			var newStop = price - atr * AtrMultiplier;
			if (newStop > _trailStop)
				_trailStop = newStop;

			if (stopSar > price || price <= _trailStop)
			{
				SellMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}
		else if (Position < 0)
		{
			var newStop = price + atr * AtrMultiplier;
			if (_trailStop == 0 || newStop < _trailStop)
				_trailStop = newStop;

			if (stopSar < price || price >= _trailStop)
			{
				BuyMarket();
				_entryPrice = 0;
				_trailStop = 0;
			}
		}

		// Entry on trade SAR signal when flat
		if (Position == 0)
		{
			if (tradeSar < price && stopSar < price)
			{
				BuyMarket();
				_entryPrice = price;
				_trailStop = price - atr * AtrMultiplier;
			}
			else if (tradeSar > price && stopSar > price)
			{
				SellMarket();
				_entryPrice = price;
				_trailStop = price + atr * AtrMultiplier;
			}
		}
	}
}
