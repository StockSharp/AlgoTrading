namespace StockSharp.Samples.Strategies;

using System;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

/// <summary>
/// Master Exit Plan strategy: EMA trend following with ATR-based trailing stop exit.
/// Enters on EMA crossover, exits when price retraces by ATR multiple.
/// </summary>
public class MasterExitPlanStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;

	private decimal _entryPrice;
	private decimal _trailStop;
	private bool _wasBullish;
	private bool _hasTrendState;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FastPeriod { get => _fastPeriod.Value; set => _fastPeriod.Value = value; }
	public int SlowPeriod { get => _slowPeriod.Value; set => _slowPeriod.Value = value; }
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }

	public MasterExitPlanStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(60).TimeFrame())
			.SetDisplay("Candle Type", "Candle timeframe", "General");
		_fastPeriod = Param(nameof(FastPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators");
		_slowPeriod = Param(nameof(SlowPeriod), 60)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for trailing stop", "Risk");
		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
			.SetDisplay("ATR Multiplier", "ATR multiplier for trailing distance", "Risk");
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_entryPrice = 0;
		_trailStop = 0;
		_wasBullish = false;
		_hasTrendState = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_entryPrice = 0;
		_trailStop = 0;
		_wasBullish = false;
		_hasTrendState = false;
		var fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		var slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(fastEma, slowEma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastValue, decimal slowValue)
	{
		if (candle.State != CandleStates.Finished) return;

		var close = candle.ClosePrice;
		var range = candle.HighPrice - candle.LowPrice;
		if (range <= 0) return;

		var trailDist = range * AtrMultiplier;
		var isBullish = fastValue > slowValue;
		var crossedUp = _hasTrendState && !_wasBullish && isBullish;
		var crossedDown = _hasTrendState && _wasBullish && !isBullish;

		if (Position > 0)
		{
			var newStop = close - trailDist;
			if (newStop > _trailStop)
				_trailStop = newStop;
			if (close < _trailStop)
			{
				SellMarket();
				_trailStop = 0;
				_entryPrice = 0;
				_wasBullish = isBullish;
				_hasTrendState = true;
				return;
			}
			else if (crossedDown)
			{
				SellMarket();
				_trailStop = 0;
				_entryPrice = 0;
				_wasBullish = isBullish;
				_hasTrendState = true;
				return;
			}
		}
		else if (Position < 0)
		{
			var newStop = close + trailDist;
			if (_trailStop == 0 || newStop < _trailStop)
				_trailStop = newStop;
			if (close > _trailStop)
			{
				BuyMarket();
				_trailStop = 0;
				_entryPrice = 0;
				_wasBullish = isBullish;
				_hasTrendState = true;
				return;
			}
			else if (crossedUp)
			{
				BuyMarket();
				_trailStop = 0;
				_entryPrice = 0;
				_wasBullish = isBullish;
				_hasTrendState = true;
				return;
			}
		}

		if (Position == 0)
		{
			if (crossedUp)
			{
				BuyMarket();
				_entryPrice = close;
				_trailStop = close - trailDist;
			}
			else if (crossedDown)
			{
				SellMarket();
				_entryPrice = close;
				_trailStop = close + trailDist;
			}
		}

		_wasBullish = isBullish;
		_hasTrendState = true;
	}
}
