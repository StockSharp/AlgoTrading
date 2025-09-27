using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using ZLEMA based MACD with risk reward targets.
/// </summary>
public class NeoImacdSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _zlemaLength;
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema1 = null!;
	private ExponentialMovingAverage _ema2 = null!;
	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private SimpleMovingAverage _signalSma = null!;
	private ExponentialMovingAverage _ema100 = null!;
	private RelativeStrengthIndex _rsi = null!;

	private decimal _prevMacd;
	private decimal _prevSignal;
	private decimal _prevHist;
	private decimal _prevPrevHist;
	private decimal _prevRsi;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// ZLEMA length.
	/// </summary>
	public int ZlemaLength { get => _zlemaLength.Value; set => _zlemaLength.Value = value; }

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// EMA filter length.
	/// </summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }

	/// <summary>
	/// Take profit to stop loss ratio.
	/// </summary>
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NeoImacdSlTpStrategy"/>.
	/// </summary>
	public NeoImacdSlTpStrategy()
	{
		_zlemaLength = Param(nameof(ZlemaLength), 34)
			.SetDisplay("ZLEMA Length", "Length for zero-lag EMA", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 100, 5);

		_shortLength = Param(nameof(ShortLength), 12)
			.SetDisplay("MACD Short Length", "Fast period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_longLength = Param(nameof(LongLength), 26)
			.SetDisplay("MACD Long Length", "Slow period for MACD", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 60, 1);

		_signalLength = Param(nameof(SignalLength), 9)
			.SetDisplay("MACD Signal Length", "Signal smoothing", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_emaLength = Param(nameof(EmaLength), 100)
			.SetDisplay("EMA Length", "EMA filter length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 200, 10);

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetDisplay("Risk-Reward Ratio", "Take profit to stop loss ratio", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_stopLossPercent = Param(nameof(StopLossPercent), 0.5m)
			.SetDisplay("Stop Loss %", "Percentage of stop loss", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 5m, 0.1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for data", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security, DataType)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_ema1?.Reset();
		_ema2?.Reset();
		_fastEma?.Reset();
		_slowEma?.Reset();
		_signalSma?.Reset();
		_ema100?.Reset();
		_rsi?.Reset();

		_prevMacd = 0m;
		_prevSignal = 0m;
		_prevHist = 0m;
		_prevPrevHist = 0m;
		_prevRsi = 0m;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema1 = new ExponentialMovingAverage { Length = ZlemaLength };
		_ema2 = new ExponentialMovingAverage { Length = ZlemaLength };
		_fastEma = new ExponentialMovingAverage { Length = ShortLength };
		_slowEma = new ExponentialMovingAverage { Length = LongLength };
		_signalSma = new SimpleMovingAverage { Length = SignalLength };
		_ema100 = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = 14 };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_ema1, _ema2, _ema100, _rsi, ProcessCandle).Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema1, decimal ema2, decimal ema100, decimal rsi)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var zlema = 2m * ema1 - ema2;
		var fast = _fastEma.Process(zlema, candle.ServerTime, true).ToDecimal();
		var slow = _slowEma.Process(zlema, candle.ServerTime, true).ToDecimal();
		var macd = fast - slow;
		var signal = _signalSma.Process(macd, candle.ServerTime, true).ToDecimal();
		var hist = macd - signal;

		var macdCrossUp = _prevMacd <= _prevSignal && macd > signal;
		var macdCrossDown = _prevMacd >= _prevSignal && macd < signal;
		var linesParallel = Math.Abs(macd - signal) < 0.03m && Math.Abs(_prevMacd - _prevSignal) < 0.03m;
		var histFalling = hist < _prevHist && _prevHist > _prevPrevHist;
		var wasAbove70 = _prevRsi > 70m && rsi <= 70m;
		var wasBelow30 = _prevRsi < 30m && rsi >= 30m;

		var longCondition = candle.ClosePrice > ema100 && macdCrossUp && !linesParallel;
		var shortCondition = candle.ClosePrice < ema100 && macdCrossDown && !linesParallel;

		var qty = Volume;

		if (longCondition && Position <= 0)
		{
			BuyMarket(qty);
			_stopPrice = candle.ClosePrice * (1m - StopLossPercent / 100m);
			_takePrice = candle.ClosePrice + (candle.ClosePrice - _stopPrice) * RiskReward;
		}
		else if (shortCondition && Position >= 0)
		{
			SellMarket(qty);
			_stopPrice = candle.ClosePrice * (1m + StopLossPercent / 100m);
			_takePrice = candle.ClosePrice - (_stopPrice - candle.ClosePrice) * RiskReward;
		}
		else if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice || macdCrossDown || histFalling || wasAbove70)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice || macdCrossUp || histFalling || wasBelow30)
				BuyMarket(Math.Abs(Position));
		}

		_prevPrevHist = _prevHist;
		_prevHist = hist;
		_prevMacd = macd;
		_prevSignal = signal;
		_prevRsi = rsi;
	}
}
