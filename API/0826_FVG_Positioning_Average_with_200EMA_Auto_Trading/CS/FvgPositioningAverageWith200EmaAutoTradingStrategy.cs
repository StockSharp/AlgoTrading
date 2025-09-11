using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// FVG Positioning Average with 200EMA Auto Trading strategy.
/// Uses averages of bullish and bearish fair value gaps combined with a 200 EMA.
/// </summary>
public class FvgPositioningAverageWith200EmaAutoTradingStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _fvgLookback;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<decimal> _riskReward;

	private SimpleMovingAverage _upAverage;
	private SimpleMovingAverage _downAverage;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;
	private Highest _highest;
	private Lowest _lowest;

	private decimal _upAvgValue;
	private decimal _downAvgValue;
	private decimal _prevClose;
	private ICandleMessage _prevCandle;
	private ICandleMessage _prev2Candle;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int FvgLookback { get => _fvgLookback.Value; set => _fvgLookback.Value = value; }
	public decimal AtrMultiplier { get => _atrMultiplier.Value; set => _atrMultiplier.Value = value; }
	public int LookbackPeriod { get => _lookbackPeriod.Value; set => _lookbackPeriod.Value = value; }
	public int EmaPeriod { get => _emaPeriod.Value; set => _emaPeriod.Value = value; }
	public decimal RiskReward { get => _riskReward.Value; set => _riskReward.Value = value; }

	public FvgPositioningAverageWith200EmaAutoTradingStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for candles", "General");

		_fvgLookback = Param(nameof(FvgLookback), 30)
		.SetGreaterThanZero()
		.SetDisplay("FVG Lookback", "Number of FVG values in average", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 5);

		_atrMultiplier = Param(nameof(AtrMultiplier), 0.25m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier for FVG size", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(0.25m, 1m, 0.25m);

		_lookbackPeriod = Param(nameof(LookbackPeriod), 20)
		.SetGreaterThanZero()
		.SetDisplay("Lookback Period", "Bars for recent high/low", "Risk");

		_emaPeriod = Param(nameof(EmaPeriod), 200)
		.SetGreaterThanZero()
		.SetDisplay("EMA Period", "EMA calculation length", "Indicators");

		_riskReward = Param(nameof(RiskReward), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Risk Reward", "Take profit to stop ratio", "Risk");
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

		_upAvgValue = default;
		_downAvgValue = default;
		_prevClose = default;
		_prevCandle = default;
		_prev2Candle = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_atr = new AverageTrueRange { Length = 200 };
		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_upAverage = new SimpleMovingAverage { Length = FvgLookback };
		_downAverage = new SimpleMovingAverage { Length = FvgLookback };
		_highest = new Highest { Length = LookbackPeriod };
		_lowest = new Lowest { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_atr, _ema, _highest, _lowest, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal atr, decimal ema, decimal recentHigh, decimal recentLow)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_prevCandle == null || _prev2Candle == null)
		{
			_prev2Candle = _prevCandle;
			_prevCandle = candle;
			_prevClose = candle.ClosePrice;
			return;
		}

		var fvgUp = candle.LowPrice > _prev2Candle.HighPrice &&
		_prevCandle.ClosePrice > _prev2Candle.HighPrice &&
		(candle.LowPrice - _prev2Candle.HighPrice) > atr * AtrMultiplier;

		var fvgDown = candle.HighPrice < _prev2Candle.LowPrice &&
		_prevCandle.ClosePrice < _prev2Candle.LowPrice &&
		(_prev2Candle.LowPrice - candle.HighPrice) > atr * AtrMultiplier;

		if (fvgUp)
		{
			var value = _prev2Candle.HighPrice;
			var res = _upAverage.Process(value);
			if (res.IsFinal)
			_upAvgValue = res.GetValue<decimal>();
		}

		if (fvgDown)
		{
			var value = _prev2Candle.LowPrice;
			var res = _downAverage.Process(value);
			if (res.IsFinal)
			_downAvgValue = res.GetValue<decimal>();
		}

		var crossoverDown = _prevClose <= _downAvgValue && candle.ClosePrice > _downAvgValue;
		var crossunderUp = _prevClose >= _upAvgValue && candle.ClosePrice < _upAvgValue;

		var longCondition = crossoverDown && candle.ClosePrice > ema && _upAvgValue > ema && _downAvgValue > ema;
		var shortCondition = crossunderUp && candle.ClosePrice < ema && _upAvgValue < ema && _downAvgValue < ema;

		if (longCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var stopLoss = recentLow;
			var takeProfit = candle.ClosePrice + (candle.ClosePrice - stopLoss) * RiskReward;
			BuyMarket(volume);
			SellStop(volume, stopLoss);
			SellLimit(volume, takeProfit);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			var stopLoss = recentHigh;
			var takeProfit = candle.ClosePrice - (recentHigh - candle.ClosePrice) * RiskReward;
			SellMarket(volume);
			BuyStop(volume, stopLoss);
			BuyLimit(volume, takeProfit);
		}

		_prev2Candle = _prevCandle;
		_prevCandle = candle;
		_prevClose = candle.ClosePrice;
	}
}
