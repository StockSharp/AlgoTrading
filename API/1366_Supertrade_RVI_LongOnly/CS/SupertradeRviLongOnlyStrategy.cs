using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrade RVI long-only strategy with stop loss and take profit.
/// Uses Relative Volatility Index crossing above a threshold to open long positions.
/// </summary>
public class SupertradeRviLongOnlyStrategy : Strategy
{
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<decimal> _rviThreshold;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _rewardRatio;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private StandardDeviation _stdDev;
	private ExponentialMovingAverage _upperEma;
	private ExponentialMovingAverage _lowerEma;

	private decimal _prevClose;
	private bool _hasPrevClose;
	private decimal _prevRvi;
	private bool _hasPrevRvi;

	/// <summary>
	/// Standard deviation lookback period.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// EMA smoothing period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// RVI threshold for long entries.
	/// </summary>
	public decimal RviThreshold
	{
		get => _rviThreshold.Value;
		set => _rviThreshold.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// Reward ratio multiplier.
	/// </summary>
	public decimal RewardRatio
	{
		get => _rewardRatio.Value;
		set => _rewardRatio.Value = value;
	}

	/// <summary>
	/// Order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="SupertradeRviLongOnlyStrategy"/>.
	/// </summary>
	public SupertradeRviLongOnlyStrategy()
	{
		_rviLength = Param(nameof(RviLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "StdDev lookback", "RVI")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_emaLength = Param(nameof(EmaLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA smoothing", "RVI")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_rviThreshold = Param(nameof(RviThreshold), 20m)
			.SetDisplay("RVI Threshold", "Level for long entries", "RVI")
			.SetCanOptimize(true)
			.SetOptimize(10m, 30m, 1m);

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss (%)", "Stop loss percent", "Protection");

		_rewardRatio = Param(nameof(RewardRatio), 3m)
			.SetGreaterThanZero()
			.SetDisplay("Reward Ratio", "Take profit = risk * ratio", "Protection");

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Trading");

		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Source candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_stdDev = new StandardDeviation { Length = RviLength };
		_upperEma = new ExponentialMovingAverage { Length = EmaLength };
		_lowerEma = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(RiskPercent * RewardRatio, UnitTypes.Percent),
			new Unit(RiskPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var stdDevValue = _stdDev.Process(candle.ClosePrice, candle.ServerTime, true).ToDecimal();

		if (!_hasPrevClose)
		{
			_prevClose = candle.ClosePrice;
			_hasPrevClose = true;
			return;
		}

		var change = candle.ClosePrice - _prevClose;
		_prevClose = candle.ClosePrice;

		var upper = _upperEma.Process(change <= 0 ? 0m : stdDevValue, candle.ServerTime, true).ToDecimal();
		var lower = _lowerEma.Process(change > 0 ? 0m : stdDevValue, candle.ServerTime, true).ToDecimal();

		if (!_stdDev.IsFormed || !_upperEma.IsFormed || !_lowerEma.IsFormed)
			return;

		var denom = upper + lower;
		if (denom == 0m)
			return;

		var rvi = upper / denom * 100m;

		if (!_hasPrevRvi)
		{
			_prevRvi = rvi;
			_hasPrevRvi = true;
			return;
		}

		var longSignal = _prevRvi < RviThreshold && rvi >= RviThreshold;
		_prevRvi = rvi;

		if (longSignal && Position <= 0 && IsFormedAndOnlineAndAllowTrading())
		{
			var volume = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(volume);
		}
	}
}

