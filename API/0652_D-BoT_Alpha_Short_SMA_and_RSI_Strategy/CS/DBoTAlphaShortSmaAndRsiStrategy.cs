namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Short strategy based on SMA and RSI.
/// </summary>
public class DBoTAlphaShortSmaAndRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _smaLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiEntry;
	private readonly StrategyParam<decimal> _rsiStop;
	private readonly StrategyParam<decimal> _rsiTakeProfit;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DateTimeOffset> _startTime;
	private readonly StrategyParam<DateTimeOffset> _endTime;

	private decimal? _trailingStop;
	private decimal? _lastLow;
	private decimal _prevRsi;
	private bool _isFirst = true;

	/// <summary>
	/// SMA length.
	/// </summary>
	public int SmaLength
	{
		get => _smaLength.Value;
		set => _smaLength.Value = value;
	}

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// RSI entry level.
	/// </summary>
	public decimal RsiEntry
	{
		get => _rsiEntry.Value;
		set => _rsiEntry.Value = value;
	}

	/// <summary>
	/// RSI stop level.
	/// </summary>
	public decimal RsiStop
	{
		get => _rsiStop.Value;
		set => _rsiStop.Value = value;
	}

	/// <summary>
	/// RSI take profit level.
	/// </summary>
	public decimal RsiTakeProfit
	{
		get => _rsiTakeProfit.Value;
		set => _rsiTakeProfit.Value = value;
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
	/// Start time.
	/// </summary>
	public DateTimeOffset StartTime
	{
		get => _startTime.Value;
		set => _startTime.Value = value;
	}

	/// <summary>
	/// End time.
	/// </summary>
	public DateTimeOffset EndTime
	{
		get => _endTime.Value;
		set => _endTime.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public DBoTAlphaShortSmaAndRsiStrategy()
	{
		_smaLength = Param(nameof(SmaLength), 200)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Simple moving average period", "Parameters")
			.SetCanOptimize(true);

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI calculation period", "Parameters")
			.SetCanOptimize(true);

		_rsiEntry = Param(nameof(RsiEntry), 51m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Entry Level", "RSI level to enter short", "Parameters")
			.SetCanOptimize(true);

		_rsiStop = Param(nameof(RsiStop), 54m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Stop Level", "RSI level to exit", "Parameters")
			.SetCanOptimize(true);

		_rsiTakeProfit = Param(nameof(RsiTakeProfit), 32m)
			.SetRange(0m, 100m)
			.SetDisplay("RSI Take Profit Level", "RSI level to take profit", "Parameters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_startTime = Param(nameof(StartTime), new DateTimeOffset(new DateTime(2017, 1, 1), TimeSpan.Zero))
			.SetDisplay("Start Time", "Strategy start time", "General");

		_endTime = Param(nameof(EndTime), new DateTimeOffset(new DateTime(2024, 1, 1), TimeSpan.Zero))
			.SetDisplay("End Time", "Strategy end time", "General");
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

		_trailingStop = null;
		_lastLow = null;
		_prevRsi = default;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var sma = new SimpleMovingAverage { Length = SmaLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(sma, rsi, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaValue, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_isFirst)
		{
			_prevRsi = rsiValue;
			_isFirst = false;
			return;
		}

		var shortCondition = _prevRsi <= RsiEntry && rsiValue > RsiEntry && candle.ClosePrice < smaValue;

		if (shortCondition)
		{
			SellMarket();
			_trailingStop = null;
			_lastLow = null;
		}

		if (Position < 0)
		{
			if (_lastLow is null || candle.ClosePrice < _lastLow)
			{
				_lastLow = candle.ClosePrice;
				_trailingStop = candle.ClosePrice;
			}

			if ((_trailingStop is not null && candle.ClosePrice > _trailingStop) ||
			rsiValue >= RsiStop ||
			rsiValue <= RsiTakeProfit)
			{
				BuyMarket();
			}
		}

		_prevRsi = rsiValue;
	}
}
