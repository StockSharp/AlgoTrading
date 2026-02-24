using System;
using System.Collections.Generic;
using Ecng.Common;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on difference of two moving averages with standard deviation filter.
/// Buys when xdin change exceeds K1*StdDev, sells when below -K1*StdDev.
/// </summary>
public class ColorXdinMAStDevStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mainLength;
	private readonly StrategyParam<int> _plusLength;
	private readonly StrategyParam<int> _stdPeriod;
	private readonly StrategyParam<decimal> _k1;

	private StandardDeviation _stdDev;
	private decimal? _prevXdin;

	public ColorXdinMAStDevStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");

		_mainLength = Param(nameof(MainLength), 10)
			.SetDisplay("Main MA Length", "Length of primary moving average", "Parameters");

		_plusLength = Param(nameof(PlusLength), 20)
			.SetDisplay("Plus MA Length", "Length of secondary moving average", "Parameters");

		_stdPeriod = Param(nameof(StdPeriod), 9)
			.SetDisplay("StdDev Period", "Period for standard deviation of MA changes", "Parameters");

		_k1 = Param(nameof(K1), 0.5m)
			.SetDisplay("Filter K1", "Multiplier for standard deviation filter", "Parameters");
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevXdin = null;
		_stdDev = new StandardDeviation { Length = StdPeriod };

		var mainMa = new SimpleMovingAverage { Length = MainLength };
		var plusMa = new SimpleMovingAverage { Length = PlusLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mainMa, plusMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mainMa);
			DrawIndicator(area, plusMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mainValue, decimal plusValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// xdin = extrapolated price using MA difference
		var xdin = mainValue * 2m - plusValue;

		if (_prevXdin is null)
		{
			_prevXdin = xdin;
			return;
		}

		var change = xdin - _prevXdin.Value;
		_prevXdin = xdin;

		var stdResult = _stdDev.Process(new DecimalIndicatorValue(_stdDev, change, candle.ServerTime) { IsFinal = true });
		if (!_stdDev.IsFormed)
			return;

		var stDev = stdResult.ToDecimal();
		if (stDev == 0)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var filter = K1 * stDev;

		if (change > filter && Position <= 0)
			BuyMarket();
		else if (change < -filter && Position >= 0)
			SellMarket();
	}
}
