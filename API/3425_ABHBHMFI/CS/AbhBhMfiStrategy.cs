using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Bullish/Bearish Harami strategy confirmed by the Money Flow Index.
/// </summary>
public class AbhBhMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _bullishThreshold;
	private readonly StrategyParam<decimal> _bearishThreshold;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;

	private MoneyFlowIndex _mfi = null!;
	private SimpleMovingAverage _bodyAverage = null!;
	private SimpleMovingAverage _closeAverage = null!;

	private (decimal Open, decimal High, decimal Low, decimal Close)? _previous;
	private (decimal Open, decimal High, decimal Low, decimal Close)? _previous2;
	private decimal? _previousBodyAverage;
	private decimal? _previousCloseAverage;
	private decimal? _previousCloseAverage2;
	private decimal? _previousMfi;
	private decimal? _previousMfi2;

	/// <summary>
	/// Initializes a new instance of the <see cref="AbhBhMfiStrategy"/> class.
	/// </summary>
	public AbhBhMfiStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for pattern detection", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 37)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Lookback for the Money Flow Index", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 80, 1);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Body SMA", "Moving average length used by the Harami filter", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 30, 1);

		_bullishThreshold = Param(nameof(BullishThreshold), 40m)
			.SetDisplay("Long Threshold", "Maximum MFI value to validate bullish Harami entries", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(20m, 60m, 5m);

		_bearishThreshold = Param(nameof(BearishThreshold), 60m)
			.SetDisplay("Short Threshold", "Minimum MFI value to validate bearish Harami entries", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(40m, 80m, 5m);

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 30m)
			.SetDisplay("Lower Exit", "MFI threshold used for both bullish and bearish exit rules", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(10m, 40m, 5m);

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 70m)
			.SetDisplay("Upper Exit", "MFI threshold confirming exhausted momentum", "Filters")
			.SetCanOptimize(true)
			.SetOptimize(60m, 90m, 5m);

		_stopLossPoints = Param(nameof(StopLossPoints), 0m)
			.SetDisplay("Stop Loss", "Protective stop in price steps (0 disables protection)", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0m)
			.SetDisplay("Take Profit", "Profit target in price steps (0 disables protection)", "Risk");
	}

	/// <summary>
	/// Candle type used for the Harami and MFI calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period of the Money Flow Index indicator.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Moving average length used to determine typical candle body size.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Maximum MFI value allowed before confirming a bullish Harami setup.
	/// </summary>
	public decimal BullishThreshold
	{
		get => _bullishThreshold.Value;
		set => _bullishThreshold.Value = value;
	}

	/// <summary>
	/// Minimum MFI value required before confirming a bearish Harami setup.
	/// </summary>
	public decimal BearishThreshold
	{
		get => _bearishThreshold.Value;
		set => _bearishThreshold.Value = value;
	}

	/// <summary>
	/// Lower MFI level used for exit cross detection.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper MFI level used for exit cross detection.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previous = null;
		_previous2 = null;
		_previousBodyAverage = null;
		_previousCloseAverage = null;
		_previousCloseAverage2 = null;
		_previousMfi = null;
		_previousMfi2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_bodyAverage = new SimpleMovingAverage { Length = BodyAveragePeriod };
		_closeAverage = new SimpleMovingAverage { Length = BodyAveragePeriod };

		var step = Security?.PriceStep ?? 0m;
		Unit stopLoss = null;
		Unit takeProfit = null;

		if (step > 0m)
		{
			if (StopLossPoints > 0m)
				stopLoss = new Unit(StopLossPoints * step, UnitTypes.Absolute);

			if (TakeProfitPoints > 0m)
				takeProfit = new Unit(TakeProfitPoints * step, UnitTypes.Absolute);
		}

		if (stopLoss != null || takeProfit != null)
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		else
			StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_mfi, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _mfi);
			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var bodyValue = _bodyAverage.Process(Math.Abs(candle.OpenPrice - candle.ClosePrice));
		var closeValue = _closeAverage.Process(candle.ClosePrice);

		var currentBodyAverage = bodyValue.IsFinal ? bodyValue.GetValue<decimal>() : (decimal?)null;
		var currentCloseAverage = closeValue.IsFinal ? closeValue.GetValue<decimal>() : (decimal?)null;

		if (_previous is { } prev && _previous2 is { } prev2 &&
			_previousBodyAverage is decimal avgBody && _previousCloseAverage2 is decimal closeAvg2 &&
			_previousMfi is decimal prevMfi && _previousMfi2 is decimal prevMfi2 &&
			IsFormedAndOnlineAndAllowTrading())
		{
			// Detect bullish Harami pattern confirmed by a downtrend filter.
			var bullishHarami = prev.Close > prev.Open &&
				(prev2.Open - prev2.Close) > avgBody &&
				prev.Close < prev2.Open &&
				prev.Open > prev2.Close &&
				((prev2.High + prev2.Low) / 2m) < closeAvg2;

			// Detect bearish Harami pattern confirmed by an uptrend filter.
			var bearishHarami = prev.Close < prev.Open &&
				(prev2.Close - prev2.Open) > avgBody &&
				prev.Close > prev2.Open &&
				prev.Open < prev2.Close &&
				((prev2.High + prev2.Low) / 2m) > closeAvg2;

			// Identify MFI crossovers that close long exposure.
			var longExit = (prevMfi > ExitLowerLevel && prevMfi2 < ExitLowerLevel) ||
				(prevMfi > ExitUpperLevel && prevMfi2 < ExitUpperLevel);

			// Identify MFI crossovers that close short exposure.
			var shortExit = (prevMfi > ExitUpperLevel && prevMfi2 < ExitUpperLevel) ||
				(prevMfi < ExitLowerLevel && prevMfi2 > ExitLowerLevel);

			if (Position > 0m && longExit)
			{
				// Close long trades once the oscillator leaves oversold or enters overbought territory.
				SellMarket(Position);
			}
			else if (Position < 0m && shortExit)
			{
				// Close short trades once the oscillator exits overbought or drops below oversold.
				BuyMarket(-Position);
			}

			if (Position <= 0m && bullishHarami && prevMfi < BullishThreshold)
			{
				// Enter long when bullish Harami aligns with depressed MFI values.
				BuyMarket(GetVolumeToOpenLong());
			}
			else if (Position >= 0m && bearishHarami && prevMfi > BearishThreshold)
			{
				// Enter short when bearish Harami aligns with elevated MFI values.
				SellMarket(GetVolumeToOpenShort());
			}
		}

		if (_mfi.IsFormed)
		{
			_previousMfi2 = _previousMfi;
			_previousMfi = mfiValue;
		}
		else
		{
			_previousMfi = null;
			_previousMfi2 = null;
		}

		if (currentBodyAverage is decimal bodyAvg)
			_previousBodyAverage = bodyAvg;
		else
			_previousBodyAverage = null;

		if (currentCloseAverage is decimal closeAvg)
		{
			_previousCloseAverage2 = _previousCloseAverage;
			_previousCloseAverage = closeAvg;
		}
		else
		{
			_previousCloseAverage = null;
			_previousCloseAverage2 = null;
		}

		_previous2 = _previous;
		_previous = (candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice);
	}

	private decimal GetVolumeToOpenLong()
	{
		var current = Position;
		if (current < 0m)
			return Volume + Math.Abs(current);

		return Volume;
	}

	private decimal GetVolumeToOpenShort()
	{
		var current = Position;
		if (current > 0m)
			return Volume + Math.Abs(current);

		return Volume;
	}
}

