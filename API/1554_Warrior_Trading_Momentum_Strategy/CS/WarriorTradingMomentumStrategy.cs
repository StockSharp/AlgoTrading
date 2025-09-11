using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Warrior Trading inspired momentum strategy.
/// </summary>
public class WarriorTradingMomentumStrategy : Strategy
{
	private readonly StrategyParam<decimal> _gapThreshold;
	private readonly StrategyParam<decimal> _gapVolumeMultiplier;
	private readonly StrategyParam<decimal> _vwapDistance;
	private readonly StrategyParam<int> _minRedCandles;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<decimal> _trailingStopTrigger;
	private readonly StrategyParam<int> _maxDailyTrades;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private AverageTrueRange _atr;
	private RelativeStrengthIndex _rsi;
	private ExponentialMovingAverage _ema20;
	private VolumeWeightedMovingAverage _vwap;

	private decimal _stopPrice;
	private decimal _takeProfitPrice;
	private int _redCount;
	private DateTime _currentDay;
	private decimal _prevDayClose;
	private bool _gapUp;
	private int _dailyTrades;

	public decimal GapThreshold { get => _gapThreshold.Value; set => _gapThreshold.Value = value; }
	public decimal GapVolumeMultiplier { get => _gapVolumeMultiplier.Value; set => _gapVolumeMultiplier.Value = value; }
	public decimal VwapDistance { get => _vwapDistance.Value; set => _vwapDistance.Value = value; }
	public int MinRedCandles { get => _minRedCandles.Value; set => _minRedCandles.Value = value; }
	public decimal RiskRewardRatio { get => _riskRewardRatio.Value; set => _riskRewardRatio.Value = value; }
	public decimal TrailingStopTrigger { get => _trailingStopTrigger.Value; set => _trailingStopTrigger.Value = value; }
	public int MaxDailyTrades { get => _maxDailyTrades.Value; set => _maxDailyTrades.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public WarriorTradingMomentumStrategy()
	{
		_gapThreshold = Param(nameof(GapThreshold), 2m)
			.SetDisplay("Gap %", "Minimum gap percent", "Gap")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);

		_gapVolumeMultiplier = Param(nameof(GapVolumeMultiplier), 2m)
			.SetDisplay("Gap Vol Mult", "Volume multiplier", "Gap")
			.SetCanOptimize(true)
			.SetOptimize(1m, 4m, 0.5m);

		_vwapDistance = Param(nameof(VwapDistance), 0.5m)
			.SetDisplay("VWAP Dist %", "Distance from VWAP", "VWAP")
			.SetCanOptimize(true)
			.SetOptimize(0.1m, 1m, 0.1m);

		_minRedCandles = Param(nameof(MinRedCandles), 3)
			.SetDisplay("Min Red", "Red candles", "Momentum")
			.SetCanOptimize(true)
			.SetOptimize(2, 6, 1);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 2m)
			.SetDisplay("Risk Reward", "Risk reward", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);

		_trailingStopTrigger = Param(nameof(TrailingStopTrigger), 1m)
			.SetDisplay("Trail Trigger %", "Trigger percent", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.5m);

		_maxDailyTrades = Param(nameof(MaxDailyTrades), 2)
			.SetDisplay("Max Trades", "Daily trade limit", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(1, 3, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type", "General");
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
		_stopPrice = 0m;
		_takeProfitPrice = 0m;
		_redCount = 0;
		_currentDay = default;
		_prevDayClose = 0m;
		_gapUp = false;
		_dailyTrades = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_volumeMa = new SimpleMovingAverage { Length = 20 };
		_atr = new AverageTrueRange { Length = 14 };
		_rsi = new RelativeStrengthIndex { Length = 14 };
		_ema20 = new ExponentialMovingAverage { Length = 20 };
		_vwap = new VolumeWeightedMovingAverage();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_vwap, _ema20, _rsi, _atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _vwap);
			DrawIndicator(area, _ema20);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal vwapValue, decimal emaValue, decimal rsiValue, decimal atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
			if (_currentDay != default)
			{
				var gap = Math.Abs(candle.OpenPrice - _prevDayClose) / _prevDayClose * 100m;
				_gapUp = candle.OpenPrice > _prevDayClose && gap >= GapThreshold;
			}
			_currentDay = day;
			_dailyTrades = 0;
		}

		_prevDayClose = candle.ClosePrice;

		var volumeMa = _volumeMa.Process(candle.TotalVolume).ToDecimal();
		var volumeSpike = candle.TotalVolume > volumeMa * GapVolumeMultiplier;

		if (candle.ClosePrice < candle.OpenPrice)
		{
			_redCount++;
		}
		else
		{
			_redCount = 0;
		}

		var redToGreen = _redCount >= MinRedCandles && candle.ClosePrice > candle.OpenPrice && volumeSpike;
		var nearVwap = Math.Abs(candle.ClosePrice - vwapValue) / candle.ClosePrice * 100m <= VwapDistance && candle.ClosePrice > vwapValue && candle.ClosePrice > emaValue && volumeSpike;
		var gapAndGo = _gapUp && volumeSpike && candle.ClosePrice > candle.OpenPrice && rsiValue > 50 && candle.ClosePrice > emaValue;
		var prioritySetup = gapAndGo || redToGreen || nearVwap;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takeProfitPrice)
				SellMarket(Position);
			else if ((candle.ClosePrice - _stopPrice) / _stopPrice * 100m >= TrailingStopTrigger)
				_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - atrValue * 1.5m);
		}
		else if (prioritySetup && _dailyTrades < MaxDailyTrades && Position == 0)
		{
			var stopDist = atrValue * 2m;
			BuyMarket();
			_stopPrice = candle.ClosePrice - stopDist;
			_takeProfitPrice = candle.ClosePrice + stopDist * RiskRewardRatio;
			_dailyTrades++;
		}
	}
}
