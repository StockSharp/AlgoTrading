using System;
using System.Collections.Generic;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dark Cloud Cover / Piercing Line pattern strategy confirmed by the Money Flow Index.
/// Converted from the MetaTrader Expert Advisor "Expert_ADC_PL_MFI" (MQL/299).
/// </summary>
public class CdcPlMfiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<int> _bodyAveragePeriod;
	private readonly StrategyParam<decimal> _longEntryLevel;
	private readonly StrategyParam<decimal> _shortEntryLevel;
	private readonly StrategyParam<decimal> _exitLowerLevel;
	private readonly StrategyParam<decimal> _exitUpperLevel;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;

	private SimpleMovingAverage _bodyAverage = null!;
	private SimpleMovingAverage _closeAverage = null!;

	private ICandleMessage? _previous;
	private decimal? _prevMfi;
	private decimal? _bodyAvgPrev;
	private decimal? _closeAvgPrev;
	private decimal? _closeAvgPrev2;

	/// <summary>
	/// Initializes a new instance of the <see cref="CdcPlMfiStrategy"/> class.
	/// </summary>
	public CdcPlMfiStrategy()
	{
		Volume = 1m;

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles used for pattern recognition", "General");

		_mfiPeriod = Param(nameof(MfiPeriod), 49)
			.SetGreaterThanZero()
			.SetDisplay("MFI Period", "Period of the Money Flow Index", "Indicator");

		_bodyAveragePeriod = Param(nameof(BodyAveragePeriod), 11)
			.SetGreaterThanZero()
			.SetDisplay("Body Average Period", "Period for the average candle body filter", "Indicator");

		_longEntryLevel = Param(nameof(LongEntryLevel), 40m)
			.SetRange(0m, 100m)
			.SetDisplay("Long Entry Level", "MFI level that confirms bullish signals", "Signal");

		_shortEntryLevel = Param(nameof(ShortEntryLevel), 60m)
			.SetRange(0m, 100m)
			.SetDisplay("Short Entry Level", "MFI level that confirms bearish signals", "Signal");

		_exitLowerLevel = Param(nameof(ExitLowerLevel), 30m)
			.SetRange(0m, 100m)
			.SetDisplay("Exit Lower Level", "MFI level used to close short positions", "Exit");

		_exitUpperLevel = Param(nameof(ExitUpperLevel), 70m)
			.SetRange(0m, 100m)
			.SetDisplay("Exit Upper Level", "MFI level used to close long positions", "Exit");

		_stopLossPips = Param(nameof(StopLossPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Protective target distance in pips", "Risk");
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
	/// Period of the Money Flow Index indicator.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Period for the average candle body filter.
	/// </summary>
	public int BodyAveragePeriod
	{
		get => _bodyAveragePeriod.Value;
		set => _bodyAveragePeriod.Value = value;
	}

	/// <summary>
	/// Money Flow Index threshold that validates bullish signals.
	/// </summary>
	public decimal LongEntryLevel
	{
		get => _longEntryLevel.Value;
		set => _longEntryLevel.Value = value;
	}

	/// <summary>
	/// Money Flow Index threshold that validates bearish signals.
	/// </summary>
	public decimal ShortEntryLevel
	{
		get => _shortEntryLevel.Value;
		set => _shortEntryLevel.Value = value;
	}

	/// <summary>
	/// Lower Money Flow Index level that closes short positions on upward reversals.
	/// </summary>
	public decimal ExitLowerLevel
	{
		get => _exitLowerLevel.Value;
		set => _exitLowerLevel.Value = value;
	}

	/// <summary>
	/// Upper Money Flow Index level that closes long positions on downward reversals.
	/// </summary>
	public decimal ExitUpperLevel
	{
		get => _exitUpperLevel.Value;
		set => _exitUpperLevel.Value = value;
	}

	/// <summary>
	/// Protective stop distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Protective target distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
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

		_previous = null;
		_prevMfi = null;
		_bodyAvgPrev = null;
		_closeAvgPrev = null;
		_closeAvgPrev2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bodyAverage = new SimpleMovingAverage { Length = BodyAveragePeriod };
		_closeAverage = new SimpleMovingAverage { Length = BodyAveragePeriod };
		var mfi = new MoneyFlowIndex { Length = MfiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(mfi, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: ToPriceUnit(TakeProfitPips),
			stopLoss: ToPriceUnit(StopLossPips)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mfi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mfiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var previous = _previous;
		var prevMfi = _prevMfi;

		if (previous != null && _bodyAvgPrev.HasValue)
		{
			var avgBody = _bodyAvgPrev.Value;

			if (_closeAvgPrev2.HasValue && IsPiercingLine(candle, previous, avgBody, _closeAvgPrev2.Value) && mfiValue < LongEntryLevel)
			{
				EnterLong();
			}

			if (_closeAvgPrev.HasValue && IsDarkCloudCover(candle, previous, avgBody, _closeAvgPrev.Value) && mfiValue > ShortEntryLevel)
			{
				EnterShort();
			}
		}

		if (prevMfi.HasValue)
		{
			HandleExits(prevMfi.Value, mfiValue);
		}

		_prevMfi = mfiValue;

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var bodyAvgValue = _bodyAverage.Process(body, candle.CloseTime, true).ToNullableDecimal();
		var closeAvgValue = _closeAverage.Process(candle.ClosePrice, candle.CloseTime, true).ToNullableDecimal();

		_closeAvgPrev2 = _closeAvgPrev;
		_closeAvgPrev = closeAvgValue;
		_bodyAvgPrev = bodyAvgValue;
		_previous = candle;
	}

	private void EnterLong()
	{
		var volume = Volume + (Position < 0 ? Math.Abs(Position) : 0m);
		if (volume > 0m)
			BuyMarket(volume);
	}

	private void EnterShort()
	{
		var volume = Volume + (Position > 0 ? Position : 0m);
		if (volume > 0m)
			SellMarket(volume);
	}

	private void HandleExits(decimal previousMfi, decimal currentMfi)
	{
		var closeShort =
			(previousMfi <= ExitLowerLevel && currentMfi > ExitLowerLevel) ||
			(previousMfi <= ExitUpperLevel && currentMfi > ExitUpperLevel);

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		var closeLong =
			(previousMfi >= ExitUpperLevel && currentMfi < ExitUpperLevel) ||
			(previousMfi >= ExitLowerLevel && currentMfi < ExitLowerLevel);

		if (closeLong && Position > 0)
		{
			SellMarket(Position);
		}
	}

	private static bool IsPiercingLine(ICandleMessage current, ICandleMessage previous, decimal avgBody, decimal trendAverage)
	{
		if (current.ClosePrice <= current.OpenPrice)
			return false;

		if (previous.ClosePrice >= previous.OpenPrice)
			return false;

		var currentBody = Math.Abs(current.ClosePrice - current.OpenPrice);
		var previousBody = Math.Abs(previous.OpenPrice - previous.ClosePrice);

		if (currentBody <= avgBody || previousBody <= avgBody)
			return false;

		var midpoint = (previous.OpenPrice + previous.ClosePrice) / 2m;

		if (current.OpenPrice >= previous.LowPrice)
			return false;

		if (current.ClosePrice <= midpoint)
			return false;

		if (current.ClosePrice >= previous.OpenPrice)
			return false;

		if (previous.ClosePrice >= trendAverage)
			return false;

		return true;
	}

	private static bool IsDarkCloudCover(ICandleMessage current, ICandleMessage previous, decimal avgBody, decimal trendAverage)
	{
		if (current.ClosePrice >= current.OpenPrice)
			return false;

		if (previous.ClosePrice <= previous.OpenPrice)
			return false;

		var currentBody = Math.Abs(current.OpenPrice - current.ClosePrice);
		var previousBody = Math.Abs(previous.ClosePrice - previous.OpenPrice);

		if (currentBody <= avgBody || previousBody <= avgBody)
			return false;

		var midpoint = (previous.OpenPrice + previous.ClosePrice) / 2m;

		if (current.OpenPrice <= previous.HighPrice)
			return false;

		if (current.ClosePrice >= midpoint)
			return false;

		if (current.ClosePrice <= previous.OpenPrice)
			return false;

		if (previous.ClosePrice <= trendAverage)
			return false;

		return true;
	}

	private Unit? ToPriceUnit(decimal pips)
	{
		if (pips <= 0m)
			return null;

		var step = Security?.PriceStep;
		if (step is null || step.Value <= 0m)
			return null;

		var distance = pips * step.Value;
		return new Unit(distance, UnitTypes.Price);
	}
}