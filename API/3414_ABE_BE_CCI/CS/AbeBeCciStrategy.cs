using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on Bullish/Bearish Engulfing patterns confirmed by CCI.
/// </summary>
public class AbeBeCciStrategy : Strategy
{
	private readonly Queue<decimal> _bodyLengths = new();
	private readonly Queue<decimal> _closeValues = new();
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _entryOversoldLevel;
	private readonly StrategyParam<decimal> _entryOverboughtLevel;
	private readonly StrategyParam<decimal> _exitLevel;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private decimal _bodySum;
	private decimal _closeSum;
	private decimal? _currentBodyAverage;
	private decimal? _lastCloseAverage;
	private decimal? _previousCloseAverage;
	private ICandleMessage? _previousCandle;
	private decimal? _previousCci;

	/// <summary>
	/// CCI period length.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of candles used to average body size and close prices.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Oversold threshold for long confirmation (negative value).
	/// </summary>
	public decimal EntryOversoldLevel
	{
		get => _entryOversoldLevel.Value;
		set => _entryOversoldLevel.Value = value;
	}

	/// <summary>
	/// Overbought threshold for short confirmation (positive value).
	/// </summary>
	public decimal EntryOverboughtLevel
	{
		get => _entryOverboughtLevel.Value;
		set => _entryOverboughtLevel.Value = value;
	}

	/// <summary>
	/// Absolute CCI level used for exit signals.
	/// </summary>
	public decimal ExitLevel
	{
		get => _exitLevel.Value;
		set => _exitLevel.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="AbeBeCciStrategy"/>.
	/// </summary>
	public AbeBeCciStrategy()
	{
		_cciPeriod = Param(nameof(CciPeriod), 49)
		.SetGreaterThanZero()
		.SetDisplay("CCI Period", "Number of candles used for the Commodity Channel Index", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(14, 80, 1);

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 11)
		.SetGreaterThanZero()
		.SetDisplay("Body Average Period", "Candles used to estimate typical body size", "Pattern")
		.SetCanOptimize(true)
		.SetOptimize(5, 30, 1);

		_entryOversoldLevel = Param(nameof(EntryOversoldLevel), -50m)
		.SetDisplay("Oversold Level", "CCI threshold for bullish engulfing confirmation", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(-100m, -10m, 5m);

		_entryOverboughtLevel = Param(nameof(EntryOverboughtLevel), 50m)
		.SetDisplay("Overbought Level", "CCI threshold for bearish engulfing confirmation", "Indicator")
		.SetCanOptimize(true)
		.SetOptimize(10m, 100m, 5m);

		_exitLevel = Param(nameof(ExitLevel), 80m)
		.SetGreaterThanZero()
		.SetDisplay("Exit Level", "Absolute CCI level triggering position exit", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(40m, 120m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe of candles processed by the strategy", "General");
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

		_bodyLengths.Clear();
		_closeValues.Clear();
		_bodySum = 0m;
		_closeSum = 0m;
		_currentBodyAverage = null;
		_lastCloseAverage = null;
		_previousCloseAverage = null;
		_previousCandle = null;
		_previousCci = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_cci, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var previousCandle = _previousCandle;
		var previousCci = _previousCci;

		UpdateRollingStatistics(candle);

		if (!_cci.IsFormed)
		{
			_previousCci = cciValue;
			_previousCandle = candle;
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_previousCci = cciValue;
			_previousCandle = candle;
			return;
		}

		if (previousCandle == null || !_currentBodyAverage.HasValue || !_previousCloseAverage.HasValue)
		{
			_previousCci = cciValue;
			_previousCandle = candle;
			return;
		}

		var bodyAverage = _currentBodyAverage.Value;
		var previousCloseAverage = _previousCloseAverage.Value;
		var previousMidpoint = (previousCandle.OpenPrice + previousCandle.ClosePrice) / 2m;

		var bullishEngulfing = previousCandle.OpenPrice > previousCandle.ClosePrice &&
		candle.ClosePrice > candle.OpenPrice &&
		candle.ClosePrice - candle.OpenPrice > bodyAverage &&
		candle.ClosePrice > previousCandle.OpenPrice &&
		previousMidpoint < previousCloseAverage &&
		candle.OpenPrice < previousCandle.ClosePrice;

		var bearishEngulfing = previousCandle.OpenPrice < previousCandle.ClosePrice &&
		candle.OpenPrice > candle.ClosePrice &&
		candle.OpenPrice - candle.ClosePrice > bodyAverage &&
		candle.ClosePrice < previousCandle.OpenPrice &&
		previousMidpoint > previousCloseAverage &&
		candle.OpenPrice > previousCandle.ClosePrice;

		var longEntrySignal = bullishEngulfing && cciValue <= EntryOversoldLevel;
		var shortEntrySignal = bearishEngulfing && cciValue >= EntryOverboughtLevel;

		var exitLongSignal = false;
		var exitShortSignal = false;

		if (previousCci.HasValue)
		{
			var prevCci = previousCci.Value;

			if (cciValue < ExitLevel && prevCci > ExitLevel)
			{
				exitLongSignal = true;
				exitShortSignal = true;
			}

			if (cciValue < -ExitLevel && prevCci > -ExitLevel)
			exitLongSignal = true;

			if (cciValue > -ExitLevel && prevCci < -ExitLevel)
			exitShortSignal = true;
		}

		if (exitLongSignal && Position > 0m)
		{
			LogInfo($"Closing long at {candle.ClosePrice} due to CCI exit signal. Current CCI: {cciValue:F2}");
			SellMarket(Position);
		}

		if (exitShortSignal && Position < 0m)
		{
			LogInfo($"Closing short at {candle.ClosePrice} due to CCI exit signal. Current CCI: {cciValue:F2}");
			BuyMarket(-Position);
		}

		if (longEntrySignal && Position <= 0m)
		{
			var volume = Volume + (Position < 0m ? -Position : 0m);
			LogInfo($"Entering long at {candle.ClosePrice}. Bullish engulfing confirmed by CCI {cciValue:F2}");
			BuyMarket(volume);
		}
		else if (shortEntrySignal && Position >= 0m)
		{
			var volume = Volume + (Position > 0m ? Position : 0m);
			LogInfo($"Entering short at {candle.ClosePrice}. Bearish engulfing confirmed by CCI {cciValue:F2}");
			SellMarket(volume);
		}

		_previousCci = cciValue;
		_previousCandle = candle;
	}

	private void UpdateRollingStatistics(ICandleMessage candle)
	{
		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		_bodyLengths.Enqueue(body);
		_bodySum += body;

		if (_bodyLengths.Count > BodyAveragePeriod)
		_bodySum -= _bodyLengths.Dequeue();

		_currentBodyAverage = _bodyLengths.Count > 0 ? _bodySum / _bodyLengths.Count : (decimal?)null;

		_previousCloseAverage = _lastCloseAverage;

		_closeValues.Enqueue(candle.ClosePrice);
		_closeSum += candle.ClosePrice;

		if (_closeValues.Count > BodyAveragePeriod)
		_closeSum -= _closeValues.Dequeue();

		_lastCloseAverage = _closeValues.Count > 0 ? _closeSum / _closeValues.Count : (decimal?)null;
	}
}
