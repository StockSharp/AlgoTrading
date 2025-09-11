using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that resamples price at fixed bar intervals and filters it with a moving average.
/// Enter long when the filtered value rises and price is above it, short when it falls and price is below.
/// </summary>
public class ResamplingFilterPackStrategy : Strategy
{
	public enum FilterType
	{
		Sma,
		Ema
	}

	private readonly StrategyParam<int> _barsPerSample;
	private readonly StrategyParam<FilterType> _filterType;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private MovingAverage? _ma;
	private int _barCounter;
	private decimal _currentFilter;
	private decimal _previousFilter;
	private int _filterDir;

	/// <summary>
	/// Bars between samples.
	/// </summary>
	public int BarsPerSample
	{
		get => _barsPerSample.Value;
		set => _barsPerSample.Value = value;
	}

	/// <summary>
	/// Moving average type.
	/// </summary>
	public FilterType MovingAverageType
	{
		get => _filterType.Value;
		set => _filterType.Value = value;
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
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes the strategy.
	/// </summary>
	public ResamplingFilterPackStrategy()
	{
		_barsPerSample = Param(nameof(BarsPerSample), 5)
			.SetDisplay("Bars Per Sample", "Number of bars between samples", "Filter")
			.SetCanOptimize(true)
			.SetOptimize(3, 7, 1);

		_filterType = Param(nameof(MovingAverageType), FilterType.Ema)
			.SetDisplay("Filter Type", "Moving average type", "Filter");

		_maPeriod = Param(nameof(MaPeriod), 9)
			.SetDisplay("Filter Period", "Moving average period", "Filter")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma = MovingAverageType == FilterType.Sma
			? new SimpleMovingAverage { Length = MaPeriod }
			: new ExponentialMovingAverage { Length = MaPeriod };

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
	}

	private void ProcessCandle(ICandleMessage candle, decimal ma)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (++_barCounter < BarsPerSample)
		return;

		_barCounter = 0;

		_previousFilter = _currentFilter;
		_currentFilter = ma;

		if (_currentFilter > _previousFilter)
		_filterDir = 1;
		else if (_currentFilter < _previousFilter)
		_filterDir = -1;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (_filterDir == 1 && candle.ClosePrice >= _currentFilter && Position <= 0)
		{
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		}
		else if (_filterDir == -1 && candle.ClosePrice <= _currentFilter && Position >= 0)
		{
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		}
	}
}
