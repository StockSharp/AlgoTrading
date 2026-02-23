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
/// RSI Trend Following strategy.
/// Enters long when momentum aligns across multiple indicators.
/// Uses ATR based stop-loss and EMA trailing profit.
/// </summary>
public class RsiTrendFollowingStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossAtr;
	private readonly StrategyParam<decimal> _trailingActivationAtr;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _trailingEmaLength;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _filterEma;
	private RelativeStrengthIndex _rsi;
	private StochasticOscillator _stochastic;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _trailingEma;
	private MovingAverageConvergenceDivergenceSignal _macd;

	private decimal _entryPrice;
	private decimal _stopLossLevel;
	private decimal _trailingActivationLevel;
	private decimal? _takeProfitLevel;

	/// <summary>
	/// ATR multiplier for stop-loss.
	/// </summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
	}

	/// <summary>
	/// ATR multiplier to activate trailing profit.
	/// </summary>
	public decimal TrailingActivationAtr
	{
		get => _trailingActivationAtr.Value;
		set => _trailingActivationAtr.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// Length of trailing EMA.
	/// </summary>
	public int TrailingEmaLength
	{
		get => _trailingEmaLength.Value;
		set => _trailingEmaLength.Value = value;
	}

	/// <summary>
	/// MACD fast period.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// MACD slow period.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// MACD signal period.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// The type of candles to use for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public RsiTrendFollowingStrategy()
	{
		_stopLossAtr = Param(nameof(StopLossAtr), 1.75m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Stop Loss", "ATR multiplier for stop loss", "Risk")

				.SetOptimize(1m, 3m, 0.25m);

		_trailingActivationAtr = Param(nameof(TrailingActivationAtr), 2.25m)
				.SetGreaterThanZero()
				.SetDisplay("ATR Trailing Activation", "ATR multiplier to activate trailing profit", "Risk")

				.SetOptimize(1.5m, 3.5m, 0.25m);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
				.SetGreaterThanZero()
				.SetDisplay("RSI Length", "RSI calculation period", "Indicators")

				.SetOptimize(10, 30, 1);

		_trailingEmaLength = Param(nameof(TrailingEmaLength), 20)
				.SetGreaterThanZero()
				.SetDisplay("Trailing EMA Length", "Length of trailing EMA", "Indicators")

				.SetOptimize(10, 50, 5);

		_macdFast = Param(nameof(MacdFastLength), 12)
				.SetGreaterThanZero()
				.SetDisplay("MACD Fast", "MACD fast period", "Indicators")

				.SetOptimize(8, 20, 2);

		_macdSlow = Param(nameof(MacdSlowLength), 26)
				.SetGreaterThanZero()
				.SetDisplay("MACD Slow", "MACD slow period", "Indicators")

				.SetOptimize(20, 40, 2);

		_macdSignal = Param(nameof(MacdSignalLength), 9)
				.SetGreaterThanZero()
				.SetDisplay("MACD Signal", "MACD signal period", "Indicators")

				.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
				.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_entryPrice = 0m;
		_stopLossLevel = 0m;
		_trailingActivationLevel = 0m;
		_takeProfitLevel = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_filterEma = new ExponentialMovingAverage { Length = 200 };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_stochastic = new StochasticOscillator { K = { Length = 14 }, D = { Length = 3 } };
		_atr = new AverageTrueRange { Length = 14 };
		_trailingEma = new ExponentialMovingAverage { Length = TrailingEmaLength };
		_macd = new MovingAverageConvergenceDivergenceSignal();
		_macd.Macd.ShortMa.Length = MacdFastLength;
		_macd.Macd.LongMa.Length = MacdSlowLength;
		_macd.SignalMa.Length = MacdSignalLength;

		var subscription = SubscribeCandles(CandleType);
		subscription
				.BindEx(_filterEma, _rsi, _macd, ProcessCandle)
				.Start();

		// no separate protection
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue filterEmaVal, IIndicatorValue rsiVal, IIndicatorValue macdVal)
	{
		if (candle.State != CandleStates.Finished)
				return;

		var filterEma = filterEmaVal.GetValue<decimal>();
		var rsi = rsiVal.GetValue<decimal>();

		var mv = (MovingAverageConvergenceDivergenceSignalValue)macdVal;
		var macdLine = mv.Macd ?? 0m;
		var signalLine = mv.Signal ?? 0m;

		// manually process stochastic, atr, trailingEma
		var stochResult = _stochastic.Process(candle);
		var stochTyped = stochResult as StochasticOscillatorValue;
		var k = stochTyped?.K ?? 0m;
		var d = stochTyped?.D ?? 0m;

		var atrResult = _atr.Process(candle);
		var atr = atrResult.IsFormed ? atrResult.GetValue<decimal>() : 0m;

		var trailingEmaResult = _trailingEma.Process(new DecimalIndicatorValue(_trailingEma, candle.ClosePrice, candle.ServerTime));
		var trailingEma = trailingEmaResult.GetValue<decimal>();

		if (!_filterEma.IsFormed || !_rsi.IsFormed || !_macd.IsFormed)
				return;

		if (!IsFormedAndOnlineAndAllowTrading())
				return;

		if (Position == 0)
		{
				var longCondition = k < 80m && d < 80m && macdLine > signalLine && rsi > 50m && candle.LowPrice > filterEma;
				if (longCondition)
				{
					var volume = Volume + Math.Abs(Position);
					BuyMarket(volume);
					_entryPrice = candle.ClosePrice;
					_takeProfitLevel = null;
				}
		}
		else if (Position > 0)
		{
				_stopLossLevel = _entryPrice - StopLossAtr * atr;
				_trailingActivationLevel = _entryPrice + TrailingActivationAtr * atr;

				if (_takeProfitLevel is null && candle.HighPrice > _trailingActivationLevel)
					_takeProfitLevel = trailingEma;
				else if (_takeProfitLevel is not null)
					_takeProfitLevel = trailingEma;

				if (candle.LowPrice <= _stopLossLevel || (_takeProfitLevel is decimal tp && candle.ClosePrice < tp))
				{
					SellMarket(Math.Abs(Position));
					_takeProfitLevel = null;
				}
		}
	}
}
