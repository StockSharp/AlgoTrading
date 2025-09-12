using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Breakout strategy based on swing highs and lows with optional MA trend filter.
/// </summary>
public class SmartMoneyConceptStrategyUncleSamStrategy : Strategy
{
	private readonly StrategyParam<int> _pivotLength;
	private readonly StrategyParam<bool> _useMaFilter;
	private readonly StrategyParam<MovingAverageTypeEnum> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _highBuffer = Array.Empty<decimal>();
	private decimal[] _lowBuffer = Array.Empty<decimal>();
	private int _bufferCount;
	private decimal? _pivotHigh;
	private decimal? _pivotLow;
	private MovingAverage? _ma;

	/// <summary>
	/// Pivot size to identify highs and lows.
	/// </summary>
	public int PivotLength
	{
		get => _pivotLength.Value;
		set => _pivotLength.Value = value;
	}

	/// <summary>
	/// Enable moving average trend filter.
	/// </summary>
	public bool UseMaFilter
	{
		get => _useMaFilter.Value;
		set => _useMaFilter.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public MovingAverageTypeEnum MaType
	{
		get => _maType.Value;
		set => _maType.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Candle type for subscription.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="SmartMoneyConceptStrategyUncleSamStrategy"/>.
	/// </summary>
	public SmartMoneyConceptStrategyUncleSamStrategy()
	{
		_pivotLength = Param(nameof(PivotLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Pivot Length", "Bars on each side for pivot detection", "General")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 5);

		_useMaFilter = Param(nameof(UseMaFilter), false)
			.SetDisplay("Enable MA Trend", "Use MA trend filter", "Trend");

		_maType = Param(nameof(MaType), MovingAverageTypeEnum.SMA)
			.SetDisplay("MA Type", "Type of moving average", "Trend");

		_maLength = Param(nameof(MaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length of moving average", "Trend");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for candles", "General");
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

		_highBuffer = Array.Empty<decimal>();
		_lowBuffer = Array.Empty<decimal>();
		_bufferCount = 0;
		_pivotHigh = null;
		_pivotLow = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_highBuffer = new decimal[PivotLength * 2 + 1];
		_lowBuffer = new decimal[PivotLength * 2 + 1];
		_bufferCount = 0;
		_pivotHigh = null;
		_pivotLow = null;

		_ma = new MovingAverage { Length = MaLength, Type = MaType switch
		{
			MovingAverageTypeEnum.SMA => MovingAverageTypes.Simple,
			MovingAverageTypeEnum.EMA => MovingAverageTypes.Exponential,
			MovingAverageTypeEnum.DEMA => MovingAverageTypes.DoubleExponential,
			MovingAverageTypeEnum.TEMA => MovingAverageTypes.TripleExponential,
			MovingAverageTypeEnum.WMA => MovingAverageTypes.Weighted,
			MovingAverageTypeEnum.VWMA => MovingAverageTypes.VolumeWeighted,
			_ => MovingAverageTypes.Simple
		}};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		for (var i = 0; i < _highBuffer.Length - 1; i++)
		{
			_highBuffer[i] = _highBuffer[i + 1];
			_lowBuffer[i] = _lowBuffer[i + 1];
		}

		_highBuffer[^1] = candle.HighPrice;
		_lowBuffer[^1] = candle.LowPrice;

		if (_bufferCount < _highBuffer.Length)
		{
			_bufferCount++;
		}
		else
		{
			var index = PivotLength;
			var ph = _highBuffer[index];
			var isHigh = true;
			for (var i = 0; i < _highBuffer.Length; i++)
			{
				if (i == index)
					continue;
				if (ph <= _highBuffer[i])
				{
					isHigh = false;
					break;
				}
			}
			if (isHigh)
				_pivotHigh = ph;

			var pl = _lowBuffer[index];
			var isLow = true;
			for (var i = 0; i < _lowBuffer.Length; i++)
			{
				if (i == index)
					continue;
				if (pl >= _lowBuffer[i])
				{
					isLow = false;
					break;
				}
			}
			if (isLow)
				_pivotLow = pl;
		}

		var longCond = _pivotHigh is decimal high && candle.ClosePrice > high;
		var shortCond = _pivotLow is decimal low && candle.ClosePrice < low;

		if (UseMaFilter)
		{
			longCond &= candle.ClosePrice > ma;
			shortCond &= candle.ClosePrice < ma;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (longCond && Position <= 0)
		{
			CancelActiveOrders();
			BuyMarket(Volume + Math.Abs(Position));
			_pivotHigh = null;
		}
		else if (shortCond && Position >= 0)
		{
			CancelActiveOrders();
			SellMarket(Volume + Math.Abs(Position));
			_pivotLow = null;
		}
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
