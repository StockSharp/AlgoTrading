using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MaxProfitMinLossOptionsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _stopLossPerc;
	private readonly StrategyParam<decimal> _trailProfitPerc;
	private readonly StrategyParam<decimal> _minTrendPercent;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _maFast;
	private ExponentialMovingAverage _maSlow;
	private RelativeStrengthIndex _rsi;

	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _hasPrev;
	private int _barsFromSignal;

	public int FastLength { get => _fastLength.Value; set => _fastLength.Value = value; }
	public int SlowLength { get => _slowLength.Value; set => _slowLength.Value = value; }
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	public decimal StopLossPerc { get => _stopLossPerc.Value; set => _stopLossPerc.Value = value; }
	public decimal TrailProfitPerc { get => _trailProfitPerc.Value; set => _trailProfitPerc.Value = value; }
	public decimal MinTrendPercent { get => _minTrendPercent.Value; set => _minTrendPercent.Value = value; }
	public int SignalCooldownBars { get => _signalCooldownBars.Value; set => _signalCooldownBars.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaxProfitMinLossOptionsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast EMA period", "General");
		_slowLength = Param(nameof(SlowLength), 48)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow EMA period", "General");
		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "General");
		_stopLossPerc = Param(nameof(StopLossPerc), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Hard stop loss percent", "General");
		_trailProfitPerc = Param(nameof(TrailProfitPerc), 5m)
			.SetGreaterThanZero()
			.SetDisplay("Trail Profit %", "Trailing exit percent", "General");
		_minTrendPercent = Param(nameof(MinTrendPercent), 0.20m)
			.SetGreaterThanZero()
			.SetDisplay("Min Trend %", "Minimum EMA spread in percent", "General");
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
		_maFast = null;
		_maSlow = null;
		_rsi = null;
		_entryPrice = 0m;
		_highestPrice = 0m;
		_lowestPrice = 0m;
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barsFromSignal = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		StartProtection(null, null);

		_maFast = new ExponentialMovingAverage { Length = FastLength };
		_maSlow = new ExponentialMovingAverage { Length = SlowLength };
		_rsi = new RelativeStrengthIndex { Length = RsiLength };

		_entryPrice = 0;
		_highestPrice = 0;
		_lowestPrice = decimal.MaxValue;
		_prevFast = 0m;
		_prevSlow = 0m;
		_hasPrev = false;
		_barsFromSignal = SignalCooldownBars;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_maFast, _maSlow, _rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal maFast, decimal maSlow, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_maFast.IsFormed || !_maSlow.IsFormed || !_rsi.IsFormed)
			return;

		var crossedUp = _hasPrev && _prevFast <= _prevSlow && maFast > maSlow;
		var crossedDown = _hasPrev && _prevFast >= _prevSlow && maFast < maSlow;
		_hasPrev = true;
		_prevFast = maFast;
		_prevSlow = maSlow;

		var close = candle.ClosePrice;
		if (close <= 0m)
			return;

		var trendPercent = Math.Abs(maFast - maSlow) / close * 100m;
		_barsFromSignal++;

		if (_barsFromSignal >= SignalCooldownBars && crossedUp && rsi >= 55m && trendPercent >= MinTrendPercent && Position <= 0)
		{
			BuyMarket();
			_entryPrice = close;
			_highestPrice = close;
			_barsFromSignal = 0;
		}
		else if (_barsFromSignal >= SignalCooldownBars && crossedDown && rsi <= 45m && trendPercent >= MinTrendPercent && Position >= 0)
		{
			SellMarket();
			_entryPrice = close;
			_lowestPrice = close;
			_barsFromSignal = 0;
		}

		if (Position > 0)
		{
			_highestPrice = Math.Max(_highestPrice, close);
			var stop = _entryPrice * (1m - StopLossPerc / 100m);
			var trail = _highestPrice * (1m - TrailProfitPerc / 100m);
			var exit = Math.Max(stop, trail);
			if (close <= exit)
				SellMarket();
		}
		else if (Position < 0)
		{
			_lowestPrice = Math.Min(_lowestPrice, close);
			var stop = _entryPrice * (1m + StopLossPerc / 100m);
			var trail = _lowestPrice * (1m + TrailProfitPerc / 100m);
			var exit = Math.Min(stop, trail);
			if (close >= exit)
				BuyMarket();
		}
	}
}
