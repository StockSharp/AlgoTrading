namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Enhanced Time Segmented Volume strategy
/// Buy when TSV is above its moving average and positive.
/// Sell when TSV is below its moving average and negative.
/// </summary>
public class EnhancedTimeSegmentedVolumeStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _tsvLength;
	private readonly StrategyParam<int> _maLength;

	private SimpleMovingAverage _tsvAverage;
	private SimpleMovingAverage _ma;
	private decimal _previousClose;

	/// <summary>
	/// Candle type for strategy calculation
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// TSV calculation length
	/// </summary>
	public int TsvLength
	{
		get => _tsvLength.Value;
		set => _tsvLength.Value = value;
	}

	/// <summary>
	/// Moving average length for TSV
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="EnhancedTimeSegmentedVolumeStrategy"/>.
	/// </summary>
	public EnhancedTimeSegmentedVolumeStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for strategy calculation", "Parameters");

		_tsvLength = Param(nameof(TsvLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("TSV Length", "Length for Time Segmented Volume calculation", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 5);

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length for TSV", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);
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
		_previousClose = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_tsvAverage = new SimpleMovingAverage { Length = TsvLength };
		_ma = new SimpleMovingAverage { Length = MaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		StartProtection(
			new Unit(3m, UnitTypes.Percent),
			new Unit(2m, UnitTypes.Percent)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.State != CandleStates.Finished)
			return;

		if (_previousClose == 0m)
		{
			_previousClose = candle.ClosePrice;
			return;
		}

		var priceChange = candle.ClosePrice - _previousClose;
		var tsvValue = candle.TotalVolume * priceChange;

		var tsvAvg = _tsvAverage.Process(tsvValue, candle.ServerTime, true).GetValue<decimal>();
		var t = tsvAvg * TsvLength;
		var m = _ma.Process(t, candle.ServerTime, true).GetValue<decimal>();

		var isLong = t > m && t > 0;
		var isShort = t < m && t < 0;

		if (isLong && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (isShort && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_previousClose = candle.ClosePrice;
	}
}
