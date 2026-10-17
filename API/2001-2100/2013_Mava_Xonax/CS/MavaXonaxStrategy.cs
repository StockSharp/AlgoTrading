using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MAVA Xonax: crossing of the OPEN-price EMA above/below the CLOSE-price EMA
/// on the two most recently completed candles. High/low EMA values set initial risk.
/// </summary>
public class MavaXonaxStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaOpen, _emaClose, _emaHigh, _emaLow;
	private decimal _previousOpen, _previousClose;
	private bool _havePrevious;
	private decimal _longStop, _longTake, _shortStop, _shortTake;
	private int _cooldownRemaining;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MavaXonaxStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 6).SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA period", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 1).SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Bars to wait after entry or exit", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(240).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_emaOpen = _emaClose = _emaHigh = _emaLow = null;
		_havePrevious = false;
		_previousOpen = _previousClose = 0m;
		_longStop = _longTake = _shortStop = _shortTake = 0m;
		_cooldownRemaining = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_emaOpen = new EMA { Length = EmaPeriod };
		_emaClose = new EMA { Length = EmaPeriod };
		_emaHigh = new EMA { Length = EmaPeriod };
		_emaLow = new EMA { Length = EmaPeriod };
		_havePrevious = false;
		_cooldownRemaining = 0;
		SubscribeCandles(CandleType).Bind(_emaClose, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var coolingDown = _cooldownRemaining > 0;
		if (coolingDown)
			_cooldownRemaining--;

		var open = _emaOpen.Process(candle.OpenPrice, candle.ServerTime, true);
		var high = _emaHigh.Process(candle.HighPrice, candle.ServerTime, true);
		var low = _emaLow.Process(candle.LowPrice, candle.ServerTime, true);
		if (!open.IsFinal || !high.IsFinal || !low.IsFinal)
			return;

		var openEma = open.ToDecimal();
		var highEma = high.ToDecimal();
		var lowEma = low.ToDecimal();
		var formed = _emaOpen.IsFormed && _emaClose.IsFormed && _emaHigh.IsFormed && _emaLow.IsFormed;
		var closed = false;

		// Preliminary candle-close protection: true intrabar execution still needs native replay.
		if (Position > 0m && (candle.ClosePrice <= _longStop || candle.ClosePrice >= _longTake))
		{
			SellMarket(Position); // Exit the entire actual position, never just the default order size.
			_cooldownRemaining = SignalCooldownBars;
			closed = true;
		}
		else if (Position < 0m && (candle.ClosePrice >= _shortStop || candle.ClosePrice <= _shortTake))
		{
			BuyMarket(-Position);
			_cooldownRemaining = SignalCooldownBars;
			closed = true;
		}

		if (formed && _havePrevious && !closed && !coolingDown && _cooldownRemaining == 0 && Position == 0m && IsFormedAndOnlineAndAllowTrading())
		{
			var crossUp = _previousOpen <= _previousClose && openEma > closeEma;
			var crossDown = _previousOpen >= _previousClose && openEma < closeEma;
			var step = Security.PriceStep is > 0m ? Security.PriceStep.Value : 1m;

			if (crossUp || crossDown)
			{
				var takeDistance = Math.Max(highEma - lowEma, 600m * step);
				var stopDistance = crossUp
					? Math.Clamp(2m * (openEma - lowEma), step, 400m * step)
					: Math.Clamp(2m * (highEma - closeEma), step, 400m * step);

				// Candle-close anchor is provisional; actual fill-price anchoring remains unverified.
				if (crossUp)
				{
					_longStop = candle.ClosePrice - stopDistance;
					_longTake = candle.ClosePrice + takeDistance;
					BuyMarket();
				}
				else
				{
					_shortStop = candle.ClosePrice + stopDistance;
					_shortTake = candle.ClosePrice - takeDistance;
					SellMarket();
				}
				_cooldownRemaining = SignalCooldownBars;
			}
		}

		// Only completed-bar EMA values become the reference for the next crossing.
		_previousOpen = openEma;
		_previousClose = closeEma;
		_havePrevious = formed;
	}
}
