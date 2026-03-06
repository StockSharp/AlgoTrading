using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanDeviationIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<decimal> _level;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private AverageTrueRange _atr;
	private decimal _previousMdx;
	private bool _hasPreviousMdx;
	private int _barsFromSignal;

	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public decimal Level { get => _level.Value; set => _level.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MeanDeviationIndexStrategy()
	{
		_emaPeriod = Param(nameof(EmaPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("EMA Period", "EMA length", "General");
		_atrPeriod = Param(nameof(AtrPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR length", "General");
		_atrMultiplier = Param(nameof(AtrMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR scaling", "General");
		_level = Param(nameof(Level), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("Level", "Normalized MDI threshold", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 10)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ema = null;
		_atr = null;
		_previousMdx = 0m;
		_hasPreviousMdx = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_previousMdx = 0m;
		_hasPreviousMdx = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ema, _atr, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_ema.IsFormed || !_atr.IsFormed)
			return;

		var atrVal = atrValue * AtrMultiplier;
		if (atrVal <= 0m)
			return;

		var dev = candle.ClosePrice - emaValue;
		var mdx = dev / atrVal;

		var crossedUp = _hasPreviousMdx && _previousMdx <= Level && mdx > Level;
		var crossedDown = _hasPreviousMdx && _previousMdx >= -Level && mdx < -Level;
		_previousMdx = mdx;
		_hasPreviousMdx = true;

		_barsFromSignal++;
		if (_barsFromSignal < SignalCooldownBars)
			return;

		if (crossedUp && Position <= 0)
		{
			BuyMarket();
			_barsFromSignal = 0;
		}
		else if (crossedDown && Position >= 0)
		{
			SellMarket();
			_barsFromSignal = 0;
		}
	}
}
