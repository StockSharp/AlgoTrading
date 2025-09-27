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
/// Port of the "iCHO Trend CCIDualOnMA Filter" MetaTrader expert advisor.
/// Combines a Chaikin oscillator zero-line filter with a dual CCI confirmation based on a smoothed price series.
/// </summary>
public class iCHO_Trend_CCIDualOnMA_FilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastChaikinLength;
	private readonly StrategyParam<int> _slowChaikinLength;
	private readonly StrategyParam<MovingAverageMethod> _chaikinMethod;
	private readonly StrategyParam<int> _fastCciLength;
	private readonly StrategyParam<int> _slowCciLength;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<MovingAverageMethod> _maMethod;
	private readonly StrategyParam<AppliedPrice> _maPrice;
	private readonly StrategyParam<bool> _useClosedBar;
	private readonly StrategyParam<bool> _reverseSignals;
	private readonly StrategyParam<bool> _closeOpposite;
	private readonly StrategyParam<bool> _onlyOnePosition;
	private readonly StrategyParam<TradeModeOption> _tradeMode;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _startMinute;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _endMinute;
	private readonly StrategyParam<DataType> _candleType;

	private AccumulationDistributionLine _adLine = null!;
	private IIndicator _fastChaikin = null!;
	private IIndicator _slowChaikin = null!;
	private IIndicator _priceMa = null!;
	private CommodityChannelIndex _fastCci = null!;
	private CommodityChannelIndex _slowCci = null!;

	private decimal? _prevCho;
	private decimal? _prevFastCci;
	private decimal? _prevSlowCci;
	private DateTimeOffset? _lastSignalTime;
	private DateTimeOffset? _lastProcessedBar;

	/// <summary>
	/// Initializes a new instance of the <see cref="iCHO_Trend_CCIDualOnMA_FilterStrategy"/> class.
	/// </summary>
	public iCHO_Trend_CCIDualOnMA_FilterStrategy()
	{
		_fastChaikinLength = Param(nameof(FastChaikinLength), 3)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Fast", "Fast EMA length", "Chaikin");

		_slowChaikinLength = Param(nameof(SlowChaikinLength), 10)
		.SetGreaterThanZero()
		.SetDisplay("Chaikin Slow", "Slow EMA length", "Chaikin");

		_chaikinMethod = Param(nameof(ChaikinMethod), MovingAverageMethod.Exponential)
		.SetDisplay("Chaikin MA", "Averaging method", "Chaikin");

		_fastCciLength = Param(nameof(FastCciLength), 14)
		.SetGreaterThanZero()
		.SetDisplay("CCI Fast", "Fast CCI period", "CCI");

		_slowCciLength = Param(nameof(SlowCciLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("CCI Slow", "Slow CCI period", "CCI");

		_maLength = Param(nameof(MaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MA Length", "MA length for price preprocessing", "Filter");

		_maMethod = Param(nameof(MaMethod), MovingAverageMethod.Simple)
		.SetDisplay("MA Method", "MA method for price preprocessing", "Filter");

		_maPrice = Param(nameof(MaPrice), AppliedPrice.Close)
		.SetDisplay("MA Price", "Price type sent into MA", "Filter");

		_useClosedBar = Param(nameof(UseClosedBar), true)
		.SetDisplay("Use Closed Bar", "Process only finished candles", "Signals");

		_reverseSignals = Param(nameof(ReverseSignals), false)
		.SetDisplay("Reverse", "Invert entry direction", "Signals");

		_closeOpposite = Param(nameof(CloseOpposite), false)
		.SetDisplay("Close Opposite", "Close opposite position before entry", "Risk");

		_onlyOnePosition = Param(nameof(OnlyOnePosition), false)
		.SetDisplay("Only One", "Allow only one open position", "Risk");

		_tradeMode = Param(nameof(TradeMode), TradeModeOption.BuyAndSell)
		.SetDisplay("Trade Mode", "Allowed trade direction", "Signals");

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
		.SetDisplay("Use Time", "Enable trading window", "Time");

		_startHour = Param(nameof(StartHour), 10)
		.SetDisplay("Start Hour", "Trading window start hour", "Time");

		_startMinute = Param(nameof(StartMinute), 1)
		.SetDisplay("Start Minute", "Trading window start minute", "Time");

		_endHour = Param(nameof(EndHour), 15)
		.SetDisplay("End Hour", "Trading window end hour", "Time");

		_endMinute = Param(nameof(EndMinute), 2)
		.SetDisplay("End Minute", "Trading window end minute", "Time");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "Data");
	}

	/// <summary>
	/// Fast length of the Chaikin oscillator.
	/// </summary>
	public int FastChaikinLength
	{
		get => _fastChaikinLength.Value;
		set => _fastChaikinLength.Value = value;
	}

	/// <summary>
	/// Slow length of the Chaikin oscillator.
	/// </summary>
	public int SlowChaikinLength
	{
		get => _slowChaikinLength.Value;
		set => _slowChaikinLength.Value = value;
	}

	/// <summary>
	/// Moving average method used inside the Chaikin oscillator.
	/// </summary>
	public MovingAverageMethod ChaikinMethod
	{
		get => _chaikinMethod.Value;
		set => _chaikinMethod.Value = value;
	}

	/// <summary>
	/// Fast CCI period.
	/// </summary>
	public int FastCciLength
	{
		get => _fastCciLength.Value;
		set => _fastCciLength.Value = value;
	}

	/// <summary>
	/// Slow CCI period.
	/// </summary>
	public int SlowCciLength
	{
		get => _slowCciLength.Value;
		set => _slowCciLength.Value = value;
	}

	/// <summary>
	/// Length of the preprocessing moving average.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// Moving average method for preprocessing price.
	/// </summary>
	public MovingAverageMethod MaMethod
	{
		get => _maMethod.Value;
		set => _maMethod.Value = value;
	}

	/// <summary>
	/// Price type passed into the preprocessing MA.
	/// </summary>
	public AppliedPrice MaPrice
	{
		get => _maPrice.Value;
		set => _maPrice.Value = value;
	}

	/// <summary>
	/// Process only completed candles.
	/// </summary>
	public bool UseClosedBar
	{
		get => _useClosedBar.Value;
		set => _useClosedBar.Value = value;
	}

	/// <summary>
	/// Reverse entry direction.
	/// </summary>
	public bool ReverseSignals
	{
		get => _reverseSignals.Value;
		set => _reverseSignals.Value = value;
	}

	/// <summary>
	/// Close opposite position before opening a new one.
	/// </summary>
	public bool CloseOpposite
	{
		get => _closeOpposite.Value;
		set => _closeOpposite.Value = value;
	}

	/// <summary>
	/// Allow only a single open position at any time.
	/// </summary>
	public bool OnlyOnePosition
	{
		get => _onlyOnePosition.Value;
		set => _onlyOnePosition.Value = value;
	}

	/// <summary>
	/// Trade mode restriction.
	/// </summary>
	public TradeModeOption TradeMode
	{
		get => _tradeMode.Value;
		set => _tradeMode.Value = value;
	}

	/// <summary>
	/// Enable trading window filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Trading window start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Trading window start minute.
	/// </summary>
	public int StartMinute
	{
		get => _startMinute.Value;
		set => _startMinute.Value = value;
	}

	/// <summary>
	/// Trading window end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Trading window end minute.
	/// </summary>
	public int EndMinute
	{
		get => _endMinute.Value;
		set => _endMinute.Value = value;
	}

	/// <summary>
	/// Primary candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_prevCho = null;
		_prevFastCci = null;
		_prevSlowCci = null;
		_lastSignalTime = null;
		_lastProcessedBar = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_adLine = new AccumulationDistributionLine();
		_fastChaikin = CreateMovingAverage(ChaikinMethod, FastChaikinLength);
		_slowChaikin = CreateMovingAverage(ChaikinMethod, SlowChaikinLength);
		_priceMa = CreateMovingAverage(MaMethod, MaLength);
		_fastCci = new CommodityChannelIndex { Length = FastCciLength };
		_slowCci = new CommodityChannelIndex { Length = SlowCciLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(_adLine, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _adLine);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal adValue)
	{
		if (UseClosedBar && candle.State != CandleStates.Finished)
		return;

		var barTime = candle.CloseTime != default ? candle.CloseTime : candle.Time;
		if (_lastProcessedBar == barTime)
		return;

		var price = GetAppliedPrice(candle, MaPrice);
		var maValue = _priceMa.Process(new DecimalIndicatorValue(_priceMa, price, barTime));
		var fastCciValue = _fastCci.Process(new DecimalIndicatorValue(_fastCci, maValue.ToDecimal(), barTime));
		var slowCciValue = _slowCci.Process(new DecimalIndicatorValue(_slowCci, maValue.ToDecimal(), barTime));
		var fastChoValue = _fastChaikin.Process(new DecimalIndicatorValue(_fastChaikin, adValue, barTime));
		var slowChoValue = _slowChaikin.Process(new DecimalIndicatorValue(_slowChaikin, adValue, barTime));

		if (!fastCciValue.IsFinal || !slowCciValue.IsFinal || !fastChoValue.IsFinal || !slowChoValue.IsFinal)
		{
			_prevCho = fastChoValue.ToDecimal() - slowChoValue.ToDecimal();
			_prevFastCci = fastCciValue.ToDecimal();
			_prevSlowCci = slowCciValue.ToDecimal();
			return;
		}

		var choValue = fastChoValue.ToDecimal() - slowChoValue.ToDecimal();
		var fastCci = fastCciValue.ToDecimal();
		var slowCci = slowCciValue.ToDecimal();

		if (_prevCho is null || _prevFastCci is null || _prevSlowCci is null)
		{
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		var prevCho = _prevCho.Value;
		var prevFast = _prevFastCci.Value;
		var prevSlow = _prevSlowCci.Value;

		_lastProcessedBar = barTime;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		if (UseTimeFilter && !IsWithinTradingWindow(barTime.TimeOfDay))
		{
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		var zeroCrossUp = prevCho < 0m && choValue > 0m;
		var zeroCrossDown = prevCho > 0m && choValue < 0m;
		var cciBullCross = prevFast < 0m && prevFast < prevSlow && fastCci > slowCci;
		var cciBearCross = prevFast > 0m && prevFast > prevSlow && fastCci < slowCci;
		var choPositive = choValue > 0m;
		var choNegative = choValue < 0m;

		var closeShort = false;
		var closeLong = false;
		var wantLong = false;
		var wantShort = false;

		if (!ReverseSignals)
		{
			if (zeroCrossUp)
			{
				closeShort = true;
				if (TradeMode != TradeModeOption.SellOnly)
				wantLong = true;
			}

			if (zeroCrossDown)
			{
				closeLong = true;
				if (TradeMode != TradeModeOption.BuyOnly)
				wantShort = true;
			}

			if (choPositive && cciBullCross)
			{
				if (TradeMode != TradeModeOption.SellOnly)
				wantLong = true;
				else
				closeShort = true;
			}

			if (choNegative && cciBearCross)
			{
				if (TradeMode != TradeModeOption.BuyOnly)
				wantShort = true;
				else
				closeLong = true;
			}
		}
		else
		{
			if (zeroCrossUp)
			{
				closeLong = true;
				if (TradeMode != TradeModeOption.BuyOnly)
				wantShort = true;
			}

			if (zeroCrossDown)
			{
				closeShort = true;
				if (TradeMode != TradeModeOption.SellOnly)
				wantLong = true;
			}

			if (choPositive && cciBullCross)
			{
				if (TradeMode != TradeModeOption.BuyOnly)
				wantShort = true;
				else
				closeLong = true;
			}

			if (choNegative && cciBearCross)
			{
				if (TradeMode != TradeModeOption.SellOnly)
				wantLong = true;
				else
				closeShort = true;
			}
		}

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
			_lastSignalTime = barTime;
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		if (closeLong && Position > 0)
		{
			SellMarket(Math.Abs(Position));
			_lastSignalTime = barTime;
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		if (OnlyOnePosition && Position != 0)
		{
			_prevCho = choValue;
			_prevFastCci = fastCci;
			_prevSlowCci = slowCci;
			return;
		}

		if (wantLong && _lastSignalTime != barTime)
		{
			if (CloseOpposite && Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				_lastSignalTime = barTime;
				_prevCho = choValue;
				_prevFastCci = fastCci;
				_prevSlowCci = slowCci;
				return;
			}

			if (Position <= 0)
			{
				BuyMarket(Volume);
				_lastSignalTime = barTime;
			}
		}

		if (wantShort && _lastSignalTime != barTime)
		{
			if (CloseOpposite && Position > 0)
			{
				SellMarket(Math.Abs(Position));
				_lastSignalTime = barTime;
				_prevCho = choValue;
				_prevFastCci = fastCci;
				_prevSlowCci = slowCci;
				return;
			}

			if (Position >= 0)
			{
				SellMarket(Volume);
				_lastSignalTime = barTime;
			}
		}

		_prevCho = choValue;
		_prevFastCci = fastCci;
		_prevSlowCci = slowCci;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPrice price)
	{
		return price switch
		{
			AppliedPrice.Close => candle.ClosePrice,
			AppliedPrice.Open => candle.OpenPrice,
			AppliedPrice.High => candle.HighPrice,
			AppliedPrice.Low => candle.LowPrice,
			AppliedPrice.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPrice.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPrice.Weighted => (candle.HighPrice + candle.LowPrice + candle.ClosePrice * 2m) / 4m,
			_ => candle.ClosePrice,
		};
	}

	private bool IsWithinTradingWindow(TimeSpan current)
	{
		if (!UseTimeFilter)
		return true;

		var start = new TimeSpan(StartHour, StartMinute, 0);
		var end = new TimeSpan(EndHour, EndMinute, 0);

		if (start == end)
		return true;

		return start < end
		? current >= start && current < end
		: current >= start || current < end;
	}

	private static IIndicator CreateMovingAverage(MovingAverageMethod method, int length)
	{
		IIndicator indicator = method switch
		{
			MovingAverageMethod.Simple => new SimpleMovingAverage { Length = length },
			MovingAverageMethod.Exponential => new ExponentialMovingAverage { Length = length },
			MovingAverageMethod.Smoothed => new SmoothedMovingAverage { Length = length },
			MovingAverageMethod.LinearWeighted => new WeightedMovingAverage { Length = length },
			_ => new SimpleMovingAverage { Length = length },
		};

		return indicator;
	}

	/// <summary>
	/// Available moving average methods.
	/// </summary>
	public enum MovingAverageMethod
	{
		Simple,
		Exponential,
		Smoothed,
		LinearWeighted,
	}

	/// <summary>
	/// Price source used for preprocessing moving average.
	/// </summary>
	public enum AppliedPrice
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
	}

	/// <summary>
	/// Trade direction restriction.
	/// </summary>
	public enum TradeModeOption
	{
		BuyOnly,
		SellOnly,
		BuyAndSell,
	}
}

