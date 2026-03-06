using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class McotsIntuitionStrategy : Strategy
{
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private decimal _prevRsi;
	private decimal _prevMomentum;
	private decimal _takeProfitPrice;
	private decimal _stopLossPrice;
	private bool _hasPrev;
	private int _barsFromSignal;
	private int _barIndex;
	private int _entryBar;

	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }
	public decimal MomentumThreshold { get => _momentumThreshold.Value; set => _momentumThreshold.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public McotsIntuitionStrategy()
	{
		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "General");
		_momentumThreshold = Param(nameof(MomentumThreshold), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Threshold", "Minimum RSI momentum", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 30)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown Bars", "Minimum bars between entries", "General");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_rsi = null;
		_prevRsi = 0m;
		_prevMomentum = 0m;
		_takeProfitPrice = 0m;
		_stopLossPrice = 0m;
		_hasPrev = false;
		_barsFromSignal = 0;
		_barIndex = 0;
		_entryBar = -1;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_prevRsi = 0;
		_prevMomentum = 0;
		_hasPrev = false;
		_takeProfitPrice = 0;
		_stopLossPrice = 0;
		_barsFromSignal = SignalCooldownBars;
		_barIndex = 0;
		_entryBar = -1;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_rsi.IsFormed)
		{
			_prevRsi = rsiValue;
			return;
		}

		var momentum = rsiValue - _prevRsi;
		_barIndex++;

		if (!_hasPrev)
		{
			_prevRsi = rsiValue;
			_prevMomentum = momentum;
			_hasPrev = true;
			return;
		}

		_barsFromSignal++;

		if (Position == 0)
		{
			var threshold = Math.Max(MomentumThreshold, 8m);
			var longSignal = _prevMomentum <= threshold && momentum > threshold && rsiValue >= 58m;

			if (_barsFromSignal >= SignalCooldownBars && longSignal)
			{
				BuyMarket();
				_takeProfitPrice = candle.ClosePrice * 1.03m;
				_stopLossPrice = candle.ClosePrice * 0.98m;
				_barsFromSignal = 0;
				_entryBar = _barIndex;
			}
		}
		else if (Position > 0)
		{
			var timedExit = _entryBar >= 0 && _barIndex - _entryBar >= 16;
			if (candle.HighPrice >= _takeProfitPrice || candle.LowPrice <= _stopLossPrice || timedExit)
				SellMarket();
		}

		_prevRsi = rsiValue;
		_prevMomentum = momentum;
	}
}
