using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Generic strategy that reacts to moving average crossovers with optional risk management features.
/// </summary>
public class ChameleonStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailPercent;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<TimeSpan> _startTime;
	private readonly StrategyParam<TimeSpan> _endTime;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevFast;
	private decimal _prevSlow;
	private decimal _entryPrice;
	private decimal _highestPrice;
	private decimal _lowestPrice;
	private decimal _stopDistance;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Use stop loss.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Use trailing stop.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop percentage.
	/// </summary>
	public decimal TrailPercent
	{
		get => _trailPercent.Value;
		set => _trailPercent.Value = value;
	}

	/// <summary>
	/// Use take profit.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Risk to reward ratio for take profit.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <summary>
	/// Use time session filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Session start time.
	/// </summary>
	public TimeSpan StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// Session end time.
	/// </summary>
	public TimeSpan EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
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
	/// Initializes a new instance of <see cref="ChameleonStrategy"/>.
	/// </summary>
	public ChameleonStrategy()
	{
		_fastLength = Param(nameof(FastLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Fast Length", "Fast MA length", "Indicators")
			.SetCanOptimize(true);

		_slowLength = Param(nameof(SlowLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Slow Length", "Slow MA length", "Indicators")
			.SetCanOptimize(true);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable stop loss", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk")
			.SetCanOptimize(true);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop", "Risk");

		_trailPercent = Param(nameof(TrailPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trail %", "Trailing stop percent", "Risk")
			.SetCanOptimize(true);

		_useTakeProfit = Param(nameof(UseTakeProfit), false)
			.SetDisplay("Use Take Profit", "Enable take profit", "Risk");

		_riskReward = Param(nameof(RiskReward), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Risk:Reward", "Risk to reward ratio", "Risk")
			.SetCanOptimize(true);

		_useTimeFilter = Param(nameof(UseTimeFilter), false)
			.SetDisplay("Use Session", "Enable session filter", "General");

		_startTime = Param(nameof(StartTime), new TimeSpan(9, 30, 0))
			.SetDisplay("Start", "Session start time", "General");

		_endTime = Param(nameof(EndTime), new TimeSpan(16, 0, 0))
			.SetDisplay("End", "Session end time", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for strategy", "General");
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
		_prevFast = 0;
		_prevSlow = 0;
		_entryPrice = 0;
		_highestPrice = 0;
		_lowestPrice = 0;
		_stopDistance = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		StartProtection();

		var fastMa = new SimpleMovingAverage { Length = FastLength };
		var slowMa = new SimpleMovingAverage { Length = SlowLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastMa, slowMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa, "Fast MA");
			DrawIndicator(area, slowMa, "Slow MA");
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (UseTimeFilter)
		{
			var timeOfDay = candle.OpenTime.TimeOfDay;
			if (timeOfDay < StartTime || timeOfDay > EndTime)
			{
				_prevFast = fast;
				_prevSlow = slow;
				return;
			}
		}

		var longSignal = fast > slow && _prevFast <= _prevSlow;
		var shortSignal = fast < slow && _prevFast >= _prevSlow;

		if (Position == 0)
		{
			if (longSignal)
			{
				var volume = Volume;
				BuyMarket(volume);
				_entryPrice = candle.ClosePrice;
				_highestPrice = _entryPrice;
				_lowestPrice = _entryPrice;
				_stopDistance = _entryPrice * StopLossPercent / 100m;
			}
			else if (shortSignal)
			{
				var volume = Volume;
				SellMarket(volume);
				_entryPrice = candle.ClosePrice;
				_highestPrice = _entryPrice;
				_lowestPrice = _entryPrice;
				_stopDistance = _entryPrice * StopLossPercent / 100m;
			}
		}
		else if (Position > 0)
		{
			if (shortSignal)
			{
				SellMarket(Position + Volume);
				_entryPrice = candle.ClosePrice;
				_highestPrice = _entryPrice;
				_lowestPrice = _entryPrice;
				_stopDistance = _entryPrice * StopLossPercent / 100m;
			}
			else
			{
				if (UseTrailingStop)
				{
					if (candle.HighPrice > _highestPrice)
						_highestPrice = candle.HighPrice;

					var trail = _highestPrice * (1 - TrailPercent / 100m);
					if (candle.LowPrice <= trail)
					{
						SellMarket(Position);
						ResetPosition();
						_prevFast = fast;
						_prevSlow = slow;
						return;
					}
				}

				if (UseStopLoss && candle.LowPrice <= _entryPrice - _stopDistance)
				{
					SellMarket(Position);
					ResetPosition();
				}
				else if (UseTakeProfit)
				{
					var target = _entryPrice + _stopDistance * RiskReward;
					if (candle.HighPrice >= target)
					{
						SellMarket(Position);
						ResetPosition();
					}
				}
			}
		}
		else if (Position < 0)
		{
			if (longSignal)
			{
				BuyMarket(Math.Abs(Position) + Volume);
				_entryPrice = candle.ClosePrice;
				_highestPrice = _entryPrice;
				_lowestPrice = _entryPrice;
				_stopDistance = _entryPrice * StopLossPercent / 100m;
			}
			else
			{
				if (UseTrailingStop)
				{
					if (candle.LowPrice < _lowestPrice)
						_lowestPrice = candle.LowPrice;

					var trail = _lowestPrice * (1 + TrailPercent / 100m);
					if (candle.HighPrice >= trail)
					{
						BuyMarket(Math.Abs(Position));
						ResetPosition();
						_prevFast = fast;
						_prevSlow = slow;
						return;
					}
				}

				if (UseStopLoss && candle.HighPrice >= _entryPrice + _stopDistance)
				{
					BuyMarket(Math.Abs(Position));
					ResetPosition();
				}
				else if (UseTakeProfit)
				{
					var target = _entryPrice - _stopDistance * RiskReward;
					if (candle.LowPrice <= target)
					{
						BuyMarket(Math.Abs(Position));
						ResetPosition();
					}
				}
			}
		}

		_prevFast = fast;
		_prevSlow = slow;
	}

	private void ResetPosition()
	{
		_entryPrice = 0;
		_highestPrice = 0;
		_lowestPrice = 0;
		_stopDistance = 0;
	}
}

