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
/// Triangle trend-following strategy converted from the MetaTrader "Triangle v1" expert advisor.
/// </summary>
public class TriangleStrategy : Strategy
{
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _stopLossSteps;
	private readonly StrategyParam<decimal> _takeProfitSteps;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _macdCandleType;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergence _macd = null!;

	private decimal? _fastValue;
	private decimal? _slowValue;
	private decimal? _momentum1;
	private decimal? _momentum2;
	private decimal? _momentum3;
	private decimal? _macdMain;
	private decimal? _macdSignal;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _tickSize;

	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	public decimal StopLossSteps
	{
		get => _stopLossSteps.Value;
		set => _stopLossSteps.Value = value;
	}

	public decimal TakeProfitSteps
	{
		get => _takeProfitSteps.Value;
		set => _takeProfitSteps.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	public DataType MacdCandleType
	{
		get => _macdCandleType.Value;
		set => _macdCandleType.Value = value;
	}

	public TriangleStrategy()
	{
		_fastMaPeriod = Param(nameof(FastMaPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("Fast LWMA", "Fast weighted moving average on the higher timeframe", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(3, 20, 1);

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 85)
			.SetGreaterThanZero()
			.SetDisplay("Slow LWMA", "Slow weighted moving average on the higher timeframe", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(30, 150, 5);

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum filter", "Filters");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetNotNegative()
			.SetDisplay("Momentum Threshold", "Minimum deviation from 100 required at least once in the last three higher timeframe candles", "Filters");

		_macdFastLength = Param(nameof(MacdFastLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA length for the MACD trend filter", "Filters");

		_macdSlowLength = Param(nameof(MacdSlowLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA length for the MACD trend filter", "Filters");

		_macdSignalLength = Param(nameof(MacdSignalLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA length for the MACD trend filter", "Filters");

		_stopLossSteps = Param(nameof(StopLossSteps), 20m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Protective stop distance measured in price steps", "Risk")
			.SetCanOptimize(true);

		_takeProfitSteps = Param(nameof(TakeProfitSteps), 50m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Target distance measured in price steps", "Risk")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Trading Candles", "Primary timeframe used for order execution", "General");

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Trend Candles", "Higher timeframe driving LWMAs and momentum", "General");

		_macdCandleType = Param(nameof(MacdCandleType), TimeSpan.FromDays(30).TimeFrame())
			.SetDisplay("MACD Candles", "Very high timeframe for MACD confirmation", "General");

		Volume = 1m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TrendCandleType), (Security, MacdCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastValue = null;
		_slowValue = null;
		_momentum1 = null;
		_momentum2 = null;
		_momentum3 = null;
		_macdMain = null;
		_macdSignal = null;
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tickSize = Security?.PriceStep ?? 1m;

		_fastMa = new WeightedMovingAverage { Length = FastMaPeriod };
		_slowMa = new WeightedMovingAverage { Length = SlowMaPeriod };
		_momentum = new Momentum { Length = MomentumPeriod };
		_macd = new MovingAverageConvergenceDivergence
		{
			FastLength = MacdFastLength,
			SlowLength = MacdSlowLength,
			SignalLength = MacdSignalLength
		};

		var trendSubscription = SubscribeCandles(TrendCandleType);
		trendSubscription
			.Bind(_fastMa, _slowMa, _momentum, ProcessTrend)
			.Start();

		var macdSubscription = SubscribeCandles(MacdCandleType);
		macdSubscription
			.Bind(_macd, ProcessMacd)
			.Start();

		var tradingSubscription = SubscribeCandles(CandleType);
		tradingSubscription
			.Bind(ProcessTradingCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, tradingSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal fastValue, decimal slowValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_fastValue = fastValue;
		_slowValue = slowValue;

		_momentum3 = _momentum2;
		_momentum2 = _momentum1;
		_momentum1 = momentumValue;
	}

	private void ProcessMacd(ICandleMessage candle, decimal macdValue, decimal signalValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_macdMain = macdValue;
		_macdSignal = signalValue;
	}

	private void ProcessTradingCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_fastValue is not decimal fast || _slowValue is not decimal slow)
			return;

		if (!UpdatePositionState(candle))
		{
			if (Position <= 0 && IsBullishSetup(fast, slow))
			{
				var volume = Volume + Math.Max(0m, -Position);
				_entryPrice = candle.ClosePrice;
				_stopPrice = StopLossSteps > 0m ? _entryPrice - GetStepValue(StopLossSteps) : null;
				_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice + GetStepValue(TakeProfitSteps) : null;

				BuyMarket(volume);
			}
			else if (Position >= 0 && IsBearishSetup(fast, slow))
			{
				var volume = Volume + Math.Max(0m, Position);
				_entryPrice = candle.ClosePrice;
				_stopPrice = StopLossSteps > 0m ? _entryPrice + GetStepValue(StopLossSteps) : null;
				_takeProfitPrice = TakeProfitSteps > 0m ? _entryPrice - GetStepValue(TakeProfitSteps) : null;

				SellMarket(volume);
			}
		}

		if (Position == 0)
		{
			ResetPositionState();
		}
	}

	private bool UpdatePositionState(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetPositionState();
				return true;
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(-Position);
				ResetPositionState();
				return true;
			}
		}

		return false;
	}

	private bool IsBullishSetup(decimal fast, decimal slow)
	{
		if (fast <= slow)
			return false;

		if (!IsMomentumConfirmed())
			return false;

		return _macdMain is decimal macd && _macdSignal is decimal signal && macd > signal;
	}

	private bool IsBearishSetup(decimal fast, decimal slow)
	{
		if (fast >= slow)
			return false;

		if (!IsMomentumConfirmed())
			return false;

		return _macdMain is decimal macd && _macdSignal is decimal signal && macd < signal;
	}

	private bool IsMomentumConfirmed()
	{
		if (MomentumThreshold <= 0m)
			return true;

		return CheckMomentum(_momentum1) || CheckMomentum(_momentum2) || CheckMomentum(_momentum3);
	}

	private bool CheckMomentum(decimal? value)
	{
		if (value is not decimal actual)
			return false;

		var deviation = Math.Abs(actual - 100m);
		return deviation >= MomentumThreshold;
	}

	private decimal GetStepValue(decimal steps)
	{
		return steps * _tickSize;
	}

	private void ResetPositionState()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takeProfitPrice = null;
	}
}

