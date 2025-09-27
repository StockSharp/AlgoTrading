namespace StockSharp.Samples.Strategies;

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

using StockSharp.Algo.Candles;

/// <summary>
/// Strategy that replicates the MetaTrader Expert_AML_CCI logic using candlestick pattern recognition and the CCI oscillator.
/// </summary>
public class AmlCciMeetingLinesStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<int> _averageBodyPeriod;
	private readonly StrategyParam<decimal> _longEntryCciLevel;
	private readonly StrategyParam<decimal> _shortEntryCciLevel;
	private readonly StrategyParam<decimal> _extremeCciLevel;

	private readonly List<ICandleMessage> _candles = new();
	private readonly List<decimal> _cciValues = new();

	/// <summary>
	/// Candle type parameter.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Period for the Commodity Channel Index.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Number of previous bodies used to compute the average body length.
	/// </summary>
	public int AverageBodyPeriod
	{
		get => _averageBodyPeriod.Value;
		set => _averageBodyPeriod.Value = value;
	}

	/// <summary>
	/// Oversold level required for confirming long entries.
	/// </summary>
	public decimal LongEntryCciLevel
	{
		get => _longEntryCciLevel.Value;
		set => _longEntryCciLevel.Value = value;
	}

	/// <summary>
	/// Overbought level required for confirming short entries.
	/// </summary>
	public decimal ShortEntryCciLevel
	{
		get => _shortEntryCciLevel.Value;
		set => _shortEntryCciLevel.Value = value;
	}

	/// <summary>
	/// Extreme CCI level used to detect exit signals.
	/// </summary>
	public decimal ExtremeCciLevel
	{
		get => _extremeCciLevel.Value;
		set => _extremeCciLevel.Value = value;
	}

	/// <summary>
	/// Initializes strategy parameters.
	/// </summary>
	public AmlCciMeetingLinesStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for analysis", "General");

		_cciPeriod = Param(nameof(CciPeriod), 18)
			.SetRange(5, 100)
			.SetDisplay("CCI Period", "Length of the Commodity Channel Index", "Indicators")
			.SetCanOptimize(true);

		_averageBodyPeriod = Param(nameof(AverageBodyPeriod), 3)
			.SetRange(1, 10)
			.SetDisplay("Average Body Period", "Number of candles used to estimate body size", "Patterns")
			.SetCanOptimize(true);

		_longEntryCciLevel = Param(nameof(LongEntryCciLevel), -50m)
			.SetRange(-200m, 0m)
			.SetDisplay("Long Entry CCI", "CCI level confirming bullish Meeting Lines", "Signals")
			.SetCanOptimize(true);

		_shortEntryCciLevel = Param(nameof(ShortEntryCciLevel), 50m)
			.SetRange(0m, 200m)
			.SetDisplay("Short Entry CCI", "CCI level confirming bearish Meeting Lines", "Signals")
			.SetCanOptimize(true);

		_extremeCciLevel = Param(nameof(ExtremeCciLevel), 80m)
			.SetRange(20m, 200m)
			.SetDisplay("Extreme CCI", "Extreme level for exit crossovers", "Signals")
			.SetCanOptimize(true);
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

		_candles.Clear();
		_cciValues.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var cci = new CommodityChannelIndex { Length = CciPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(cci, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, cci);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_candles.Add(candle);
		if (_candles.Count > AverageBodyPeriod + 5)
			_candles.RemoveAt(0);

		_cciValues.Add(cciValue);
		if (_cciValues.Count > AverageBodyPeriod + 5)
			_cciValues.RemoveAt(0);

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_candles.Count < Math.Max(AverageBodyPeriod, 2) || _cciValues.Count < 2)
			return;

		var lastCci = _cciValues[^1];
		var previousCci = _cciValues[^2];

		var hasBullishPattern = IsBullishMeetingLines();
		var hasBearishPattern = IsBearishMeetingLines();

		if (hasBullishPattern && lastCci <= LongEntryCciLevel && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			LogInfo($"Buy signal: bullish Meeting Lines with CCI {lastCci:F2}.");
		}
		else if (hasBearishPattern && lastCci >= ShortEntryCciLevel && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			LogInfo($"Sell signal: bearish Meeting Lines with CCI {lastCci:F2}.");
		}
		else if (Position < 0 && ShouldExitShort(lastCci, previousCci))
		{
			BuyMarket(Math.Abs(Position));
			LogInfo($"Exit short: CCI crossed an extreme level. Current {lastCci:F2}, previous {previousCci:F2}.");
		}
		else if (Position > 0 && ShouldExitLong(lastCci, previousCci))
		{
			SellMarket(Position);
			LogInfo($"Exit long: CCI crossed an extreme level. Current {lastCci:F2}, previous {previousCci:F2}.");
		}
	}

	private bool IsBullishMeetingLines()
	{
		if (_candles.Count < Math.Max(AverageBodyPeriod, 2))
			return false;

		var avgBody = CalculateAverageBody();
		var last = GetCandleFromEnd(1);
		var previous = GetCandleFromEnd(2);

		return previous.OpenPrice - previous.ClosePrice > avgBody
			&& last.ClosePrice - last.OpenPrice > avgBody
			&& Math.Abs(last.ClosePrice - previous.ClosePrice) < avgBody * 0.1m;
	}

	private bool IsBearishMeetingLines()
	{
		if (_candles.Count < Math.Max(AverageBodyPeriod, 2))
			return false;

		var avgBody = CalculateAverageBody();
		var last = GetCandleFromEnd(1);
		var previous = GetCandleFromEnd(2);

		return last.OpenPrice - last.ClosePrice > avgBody
			&& previous.ClosePrice - previous.OpenPrice > avgBody
			&& Math.Abs(last.ClosePrice - previous.ClosePrice) < avgBody * 0.1m;
	}

	private decimal CalculateAverageBody()
	{
		var period = Math.Max(AverageBodyPeriod, 1);

		decimal sum = 0m;
		for (var i = 1; i <= period; i++)
		{
			var candle = GetCandleFromEnd(i);
			sum += Math.Abs(candle.ClosePrice - candle.OpenPrice);
		}

		return period == 0 ? 0m : sum / period;
	}

	private ICandleMessage GetCandleFromEnd(int index)
	{
		return _candles[^index];
	}

	private bool ShouldExitShort(decimal currentCci, decimal previousCci)
	{
		var extreme = ExtremeCciLevel;
		return (currentCci > -extreme && previousCci < -extreme)
			|| (currentCci < extreme && previousCci > extreme);
	}

	private bool ShouldExitLong(decimal currentCci, decimal previousCci)
	{
		var extreme = ExtremeCciLevel;
		return (currentCci < extreme && previousCci > extreme)
			|| (currentCci < -extreme && previousCci > -extreme);
	}
}

