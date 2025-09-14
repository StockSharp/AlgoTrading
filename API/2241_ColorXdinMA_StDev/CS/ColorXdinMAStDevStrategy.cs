namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on difference of two moving averages with standard deviation filter.
/// </summary>
public class ColorXdinMAStDevStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mainLength;
	private readonly StrategyParam<int> _plusLength;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;
	private readonly StrategyParam<decimal> _k2;

	private SimpleMovingAverage _mainMa;
	private SimpleMovingAverage _plusMa;
	private StandardDeviation _stdDev;

	private decimal? _prevXdin;

	public ColorXdinMAStDevStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");

		_mainLength = Param(nameof(MainLength), 10)
			.SetDisplay("Main MA Length", "Length of primary moving average", "Parameters")
			.SetCanOptimize(true);

		_plusLength = Param(nameof(PlusLength), 20)
			.SetDisplay("Plus MA Length", "Length of secondary moving average", "Parameters")
			.SetCanOptimize(true);

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Period for standard deviation of MA changes", "Parameters")
			.SetCanOptimize(true);

		_k1 = Param(nameof(K1), 1.5m)
			.SetDisplay("Filter K1", "First multiplier for standard deviation", "Parameters")
			.SetCanOptimize(true);

		_k2 = Param(nameof(K2), 2.5m)
			.SetDisplay("Filter K2", "Second multiplier for standard deviation", "Parameters")
			.SetCanOptimize(true);
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int MainLength
	{
		get => _mainLength.Value;
		set => _mainLength.Value = value;
	}

	public int PlusLength
	{
		get => _plusLength.Value;
		set => _plusLength.Value = value;
	}

	public int StdPeriod
	{
		get => _stdPeriod.Value;
		set => _stdPeriod.Value = value;
	}

	public decimal K1
	{
		get => _k1.Value;
		set => _k1.Value = value;
	}

	public decimal K2
	{
		get => _k2.Value;
		set => _k2.Value = value;
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

		_mainMa = new SimpleMovingAverage { Length = MainLength };
		_plusMa = new SimpleMovingAverage { Length = PlusLength };
		_stdDev = new StandardDeviation { Length = StdPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_mainMa, _plusMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _mainMa);
			DrawIndicator(area, _plusMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mainValue, decimal plusValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var xdin = mainValue * 2m - plusValue;

		if (_prevXdin is null)
		{
			_prevXdin = xdin;
			return;
		}

		var change = xdin - _prevXdin.Value;
		_prevXdin = xdin;

		var stDev = _stdDev.Process(change, candle.ServerTime, true).ToDecimal();

		if (!_stdDev.IsFormed || stDev == 0)
			return;

		var filter = K1 * stDev;

		if (change > filter)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (change < -filter)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
	}
}
