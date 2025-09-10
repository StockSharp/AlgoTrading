namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on crossover of moving averages of close and open prices.
/// </summary>
public class BedoOsaimiIstrStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<bool> _useEma;

	private IIndicator _closeMa;
	private IIndicator _openMa;

	private decimal? _prevCloseMa;
	private decimal? _prevOpenMa;

	/// <summary>
	/// Candle type and timeframe for strategy calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Moving average period for both series.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Use exponential moving average instead of simple.
	/// </summary>
	public bool UseEma
	{
		get => _useEma.Value;
		set => _useEma.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BedoOsaimiIstrStrategy"/> class.
	/// </summary>
	public BedoOsaimiIstrStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_maLength = Param(nameof(MaLength), 14)
			.SetDisplay("MA Length", "Period for moving averages", "Moving Average")
			.SetRange(5, 50);

		_useEma = Param(nameof(UseEma), true)
			.SetDisplay("Use EMA", "Use exponential moving averages", "Moving Average");
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

		if (UseEma)
		{
			_closeMa = new ExponentialMovingAverage { Length = MaLength };
			_openMa = new ExponentialMovingAverage { Length = MaLength };
		}
		else
		{
			_closeMa = new SimpleMovingAverage { Length = MaLength };
			_openMa = new SimpleMovingAverage { Length = MaLength };
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_closeMa, _openMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _closeMa);
			DrawIndicator(area, _openMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal closeMa, decimal openMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevCloseMa is not decimal prevClose || _prevOpenMa is not decimal prevOpen)
		{
			_prevCloseMa = closeMa;
			_prevOpenMa = openMa;
			return;
		}

		var longEntry = closeMa > openMa && prevClose <= prevOpen;
		var shortEntry = closeMa < openMa && prevClose >= prevOpen;

		if (longEntry && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortEntry && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevCloseMa = closeMa;
		_prevOpenMa = openMa;
	}
}
