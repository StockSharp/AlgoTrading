using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Pure price action breakout strategy with 1:5 risk-reward.
/// Uses EMA crossover with RSI and volume confirmation. Stop-loss is based on ATR and take-profit is five times the risk.
/// </summary>
public class PurePriceActionBreakoutWith15RRStrategy : Strategy
{
	private readonly StrategyParam<int> _fastPeriod;
	private readonly StrategyParam<int> _slowPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<int> _volumePeriod;
	private readonly StrategyParam<decimal> _stopLossFactor;
	private readonly StrategyParam<decimal> _riskRewardRatio;
	private readonly StrategyParam<int> _maxTradesPerDay;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma = null!;
	private ExponentialMovingAverage _slowEma = null!;
	private RelativeStrengthIndex _rsi = null!;
	private AverageTrueRange _atr = null!;
	private SimpleMovingAverage _volumeSma = null!;

	private decimal _prevFast;
	private decimal _prevSlow;
	private DateTime _currentDay;
	private int _tradesToday;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastPeriod
	{
		get => _fastPeriod.Value;
		set => _fastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowPeriod
	{
		get => _slowPeriod.Value;
		set => _slowPeriod.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Volume SMA period.
	/// </summary>
	public int VolumePeriod
	{
		get => _volumePeriod.Value;
		set => _volumePeriod.Value = value;
	}

	/// <summary>
	/// Stop-loss ATR multiplier.
	/// </summary>
	public decimal StopLossFactor
	{
		get => _stopLossFactor.Value;
		set => _stopLossFactor.Value = value;
	}

	/// <summary>
	/// Risk-reward ratio.
	/// </summary>
	public decimal RiskRewardRatio
	{
		get => _riskRewardRatio.Value;
		set => _riskRewardRatio.Value = value;
	}

	/// <summary>
	/// Maximum trades per day.
	/// </summary>
	public int MaxTradesPerDay
	{
		get => _maxTradesPerDay.Value;
		set => _maxTradesPerDay.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="PurePriceActionBreakoutWith15RRStrategy"/>.
	/// </summary>
	public PurePriceActionBreakoutWith15RRStrategy()
	{
		_fastPeriod = Param(nameof(FastPeriod), 9)
		.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 15, 1);

		_slowPeriod = Param(nameof(SlowPeriod), 21)
		.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(15, 30, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
		.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_atrPeriod = Param(nameof(AtrPeriod), 14)
		.SetDisplay("ATR Period", "ATR period for stop-loss", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_volumePeriod = Param(nameof(VolumePeriod), 20)
		.SetDisplay("Volume SMA", "Volume SMA period", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 5);

		_stopLossFactor = Param(nameof(StopLossFactor), 1.5m)
		.SetDisplay("Stop-Loss Factor", "ATR multiplier for stop-loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 3m, 0.5m);

		_riskRewardRatio = Param(nameof(RiskRewardRatio), 5m)
		.SetDisplay("Risk Reward", "Take-profit to stop-loss ratio", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(2m, 6m, 1m);

		_maxTradesPerDay = Param(nameof(MaxTradesPerDay), 5)
		.SetDisplay("Max Trades", "Maximum trades per day", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1, 10, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevFast = 0m;
		_prevSlow = 0m;
		_currentDay = default;
		_tradesToday = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (CandleType.Arg is TimeSpan tf && tf != TimeSpan.FromMinutes(5) && tf != TimeSpan.FromMinutes(15))
		throw new ArgumentException("Only 5 or 15 minute candles are supported.");

		_fastEma = new ExponentialMovingAverage { Length = FastPeriod };
		_slowEma = new ExponentialMovingAverage { Length = SlowPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		_volumeSma = new SimpleMovingAverage { Length = VolumePeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_fastEma, _slowEma, _rsi, _atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, _fastEma);
		DrawIndicator(area, _slowEma);
		DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal rsi, decimal atr)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var volAvg = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		if (!_fastEma.IsFormed || !_slowEma.IsFormed || !_rsi.IsFormed || !_atr.IsFormed || !_volumeSma.IsFormed)
		{
		_prevFast = fast;
		_prevSlow = slow;
		return;
		}

		var day = candle.OpenTime.Date;
		if (day != _currentDay)
		{
		_currentDay = day;
		_tradesToday = 0;
		}

		if (_tradesToday >= MaxTradesPerDay)
		{
		if (Position != 0)
		CloseAll();
		return;
		}

		if (Position == 0)
		CancelActiveOrders();

		var volCondition = candle.TotalVolume > volAvg;
		var longCross = _prevFast <= _prevSlow && fast > slow;
		var shortCross = _prevFast >= _prevSlow && fast < slow;

		if (longCross && rsi > 50m && volCondition && Position <= 0)
		{
		CancelActiveOrders();
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		var stopPrice = candle.ClosePrice - atr * StopLossFactor;
		var takePrice = candle.ClosePrice + (candle.ClosePrice - stopPrice) * RiskRewardRatio;
		SellStop(volume, stopPrice);
		SellLimit(volume, takePrice);
		_tradesToday++;
		}
		else if (shortCross && rsi < 50m && volCondition && Position >= 0)
		{
		CancelActiveOrders();
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		var stopPrice = candle.ClosePrice + atr * StopLossFactor;
		var takePrice = candle.ClosePrice - (stopPrice - candle.ClosePrice) * RiskRewardRatio;
		BuyStop(volume, stopPrice);
		BuyLimit(volume, takePrice);
		_tradesToday++;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
