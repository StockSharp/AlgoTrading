using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Daytrading ES Wick Length Strategy (0661).
/// Enters long when total wick length exceeds its moving average plus offset.
/// Exits after holding a specified number of bars.
/// </summary>
public class DaytradingESWickLengthStrategy : Strategy
{
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<decimal> _maOffset;
	private readonly StrategyParam<int> _holdPeriods;
	private readonly StrategyParam<DataType> _candleType;

	private IIndicator _ma;
	private int _barsInPosition;

	/// <summary>
	/// Moving average length for wick calculation.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Type of moving average.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Offset added to moving average.
	/// </summary>
	public decimal MaOffset
	{
		get => _maOffset.Value;
		set => _maOffset.Value = value;
	}

	/// <summary>
	/// Number of bars to hold position.
	/// </summary>
	public int HoldPeriods
	{
		get => _holdPeriods.Value;
		set => _holdPeriods.Value = value;
	}

	/// <summary>
	/// Type of candles to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DaytradingESWickLengthStrategy"/>.
	/// </summary>
	public DaytradingESWickLengthStrategy()
	{
		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 40, 5);

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.VolumeWeighted)
			.SetDisplay("MA Type", "Type of moving average", "General");

		_maOffset = Param(nameof(MaOffset), 10m)
			.SetDisplay("MA Offset", "Offset added to MA", "General")
			.SetCanOptimize(true)
			.SetOptimize(5m, 20m, 5m);

		_holdPeriods = Param(nameof(HoldPeriods), 18)
			.SetGreaterThanZero()
			.SetDisplay("Hold Periods", "Bars to hold a position", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

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
		_ma = null;
		_barsInPosition = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = CreateMa(MaType, MaLength);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var upperWick = candle.HighPrice - Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowerWick = Math.Min(candle.ClosePrice, candle.OpenPrice) - candle.LowPrice;
		var totalWick = upperWick + lowerWick;

		var maVal = _ma.Process(new DecimalIndicatorValue(_ma, totalWick, candle.ServerTime)).ToNullableDecimal();
		if (!maVal.HasValue)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || !_ma.IsFormed)
			return;

		var threshold = maVal.Value + MaOffset;

		if (Position <= 0 && totalWick > threshold)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_barsInPosition = 0;
		}
		else if (Position > 0)
		{
			_barsInPosition++;

			if (_barsInPosition >= HoldPeriods)
			{
				SellMarket(Position);
				_barsInPosition = 0;
			}
		}
	}

	private static IIndicator CreateMa(MovingAverageTypeEnum type, int length)
	{
		return type switch
		{
			MovingAverageTypeEnum.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageTypeEnum.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageTypeEnum.Weighted => new WeightedMovingAverage { Length = length },
			MovingAverageTypeEnum.VolumeWeighted => new VolumeWeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};
	}

	/// <summary>
	/// Available moving average types.
	/// </summary>
	public enum MovingAverageTypeEnum
	{
		/// <summary>Simple moving average.</summary>
		Simple,
		/// <summary>Exponential moving average.</summary>
		Exponential,
		/// <summary>Weighted moving average.</summary>
		Weighted,
		/// <summary>Volume weighted moving average.</summary>
		VolumeWeighted
	}
}
