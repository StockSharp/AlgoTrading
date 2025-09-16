using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// GO strategy based on moving averages of open, high, low and close prices.
/// Closes opposite positions when the GO value changes sign.
/// Opens new trades in the direction of the GO value until the maximum
/// allowed number of positions is reached.
/// </summary>
public class GoStrategy : Strategy
{
	private readonly StrategyParam<decimal> _risk;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private DateTimeOffset _lastTime;
	private IIndicator? _openMa;
	private IIndicator? _highMa;
	private IIndicator? _lowMa;
	private IIndicator? _closeMa;

	/// <summary>
	/// Risk percentage used to calculate trade volume.
	/// </summary>
	public decimal Risk
	{
		get => _risk.Value;
		set => _risk.Value = value;
	}

	/// <summary>
	/// Maximum number of positions allowed in one direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Moving average type used in calculations.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average period.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for indicators and trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes <see cref="GoStrategy"/>.
	/// </summary>
	public GoStrategy()
	{
		_risk = Param(nameof(Risk), 30m)
			.SetDisplay("Risk %", "Risk percentage for volume calculation", "General")
			.SetGreaterThanZero();

		_maxPositions = Param(nameof(MaxPositions), 5)
			.SetDisplay("Max Positions", "Maximum number of positions", "General")
			.SetGreaterThanOrEqual(1);

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.SMA)
			.SetDisplay("MA Type", "Moving average type", "Indicator");

		_maPeriod = Param(nameof(MaPeriod), 174)
			.SetDisplay("MA Period", "Moving average period", "Indicator")
			.SetGreaterThanZero();

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_lastTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_openMa = CreateMovingAverage(MaType, MaPeriod);
		_highMa = CreateMovingAverage(MaType, MaPeriod);
		_lowMa = CreateMovingAverage(MaType, MaPeriod);
		_closeMa = CreateMovingAverage(MaType, MaPeriod);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_openMa, _highMa, _lowMa, _closeMa, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _openMa);
			DrawIndicator(area, _highMa);
			DrawIndicator(area, _lowMa);
			DrawIndicator(area, _closeMa);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal open, decimal high, decimal low, decimal close)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var go = ((close - open) + (high - open) + (low - open) + (close - low) + (close - high)) * candle.TotalVolume;

		if (go < 0 && Position > 0)
		{
			// Close long positions when GO becomes negative
			SellMarket(Position);
		}
		else if (go > 0 && Position < 0)
		{
			// Close short positions when GO becomes positive
			BuyMarket(-Position);
		}

		if (candle.OpenTime == _lastTime || go == 0)
			return;

		var volume = CalculateVolume();
		if (volume <= 0)
			return;

		if (go > 0 && Position < MaxPositions * volume)
		{
			// Open long position
			BuyMarket(volume);
			_lastTime = candle.OpenTime;
		}
		else if (go < 0 && Position > -MaxPositions * volume)
		{
			// Open short position
			SellMarket(volume);
			_lastTime = candle.OpenTime;
		}
	}

	private decimal CalculateVolume()
	{
		if (Portfolio == null)
			return LotCheck(Volume);

		var lot = Math.Round(Portfolio.CurrentValue * Risk / 100000m, 1);
		var free = Portfolio.CurrentValue;
		if (free < 1000m * lot)
			lot = Math.Round(free * Risk / 100000m, 1);

		lot = LotCheck(lot);

		if (lot <= 0)
			lot = Volume;

		return lot;
	}

	private decimal LotCheck(decimal volume)
	{
		var step = Security.VolumeStep ?? 1m;
		if (step > 0)
			volume = step * Math.Floor(volume / step);

		var min = Security.MinVolume ?? 0m;
		if (volume < min)
			return 0m;

		var max = Security.MaxVolume ?? decimal.MaxValue;
		if (volume > max)
			volume = max;

		return volume;
	}

	private static IIndicator CreateMovingAverage(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.SMA => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.EMA => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.DEMA => new DoubleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.TEMA => new TripleExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.WMA => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VWMA => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length }
		};
	}
}

public enum MovingAverageTypeEnum
{
	SMA,
	EMA,
	DEMA,
	TEMA,
	WMA,
	VWMA
}
