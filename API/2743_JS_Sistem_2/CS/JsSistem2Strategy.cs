using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// JS Sistem 2 trend-following strategy converted from MetaTrader 5.
/// Combines exponential moving averages, MACD histogram (OsMA), and Relative Vigor Index filters.
/// Includes trailing stop based on recent candle shadows and configurable stop/target distances.
/// </summary>
public class JsSistem2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _minBalance;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _minDifferencePips;
	private readonly StrategyParam<int> _volatilityPeriod;
	private readonly StrategyParam<bool> _trailingEnabled;
	private readonly StrategyParam<int> _trailingIndentPips;
	private readonly StrategyParam<int> _maFastPeriod;
	private readonly StrategyParam<int> _maMediumPeriod;
	private readonly StrategyParam<int> _maSlowPeriod;
	private readonly StrategyParam<int> _osmaFastPeriod;
	private readonly StrategyParam<int> _osmaSlowPeriod;
	private readonly StrategyParam<int> _osmaSignalPeriod;
	private readonly StrategyParam<int> _rviPeriod;
	private readonly StrategyParam<int> _rviSignalLength;
	private readonly StrategyParam<decimal> _rviMax;
	private readonly StrategyParam<decimal> _rviMin;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _emaFast = null!;
	private EMA _emaMedium = null!;
	private EMA _emaSlow = null!;
	private MACD _macd = null!;
	private Highest _highest = null!;
	private Lowest _lowest = null!;
	private RelativeVigorIndex _rvi = null!;
	private SimpleMovingAverage _rviSignal = null!;

	private decimal? _stopPrice;
	private decimal? _takePrice;
	private decimal _entryPrice;

	/// <summary>
	/// Minimum account balance required to allow new entries.
	/// </summary>
	public decimal MinBalance
	{
		get => _minBalance.Value;
		set => _minBalance.Value = value;
	}

	/// <summary>
	/// Volume to send with each new order.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Maximum allowed spread between fast and slow EMA in pips.
	/// </summary>
	public int MinDifferencePips
	{
		get => _minDifferencePips.Value;
		set => _minDifferencePips.Value = value;
	}

	/// <summary>
	/// Lookback for trailing stop based on candle shadows.
	/// </summary>
	public int VolatilityPeriod
	{
		get => _volatilityPeriod.Value;
		set => _volatilityPeriod.Value = value;
	}

	/// <summary>
	/// Enables trailing stop management.
	/// </summary>
	public bool TrailingEnabled
	{
		get => _trailingEnabled.Value;
		set => _trailingEnabled.Value = value;
	}

	/// <summary>
	/// Offset applied when updating trailing stop levels.
	/// </summary>
	public int TrailingIndentPips
	{
		get => _trailingIndentPips.Value;
		set => _trailingIndentPips.Value = value;
	}

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int MaFastPeriod
	{
		get => _maFastPeriod.Value;
		set => _maFastPeriod.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MaMediumPeriod
	{
		get => _maMediumPeriod.Value;
		set => _maMediumPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int MaSlowPeriod
	{
		get => _maSlowPeriod.Value;
		set => _maSlowPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA length for the MACD/OsMA filter.
	/// </summary>
	public int OsmaFastPeriod
	{
		get => _osmaFastPeriod.Value;
		set => _osmaFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA length for the MACD/OsMA filter.
	/// </summary>
	public int OsmaSlowPeriod
	{
		get => _osmaSlowPeriod.Value;
		set => _osmaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal length for the MACD/OsMA filter.
	/// </summary>
	public int OsmaSignalPeriod
	{
		get => _osmaSignalPeriod.Value;
		set => _osmaSignalPeriod.Value = value;
	}

	/// <summary>
	/// Relative Vigor Index period.
	/// </summary>
	public int RviPeriod
	{
		get => _rviPeriod.Value;
		set => _rviPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the RVI signal line.
	/// </summary>
	public int RviSignalLength
	{
		get => _rviSignalLength.Value;
		set => _rviSignalLength.Value = value;
	}

	/// <summary>
	/// Upper threshold for the RVI signal line.
	/// </summary>
	public decimal RviMax
	{
		get => _rviMax.Value;
		set => _rviMax.Value = value;
	}

	/// <summary>
	/// Lower threshold for the RVI signal line.
	/// </summary>
	public decimal RviMin
	{
		get => _rviMin.Value;
		set => _rviMin.Value = value;
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
	/// Initializes a new instance of the <see cref="JsSistem2Strategy"/> class.
	/// </summary>
	public JsSistem2Strategy()
	{
		_minBalance = Param(nameof(MinBalance), 100m)
			.SetDisplay("Min Balance", "Minimum balance to allow trading", "Risk")
			.SetCanOptimize(true);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "General")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 35)
			.SetDisplay("Stop Loss", "Stop-loss distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 40)
			.SetDisplay("Take Profit", "Take-profit distance in pips", "Risk")
			.SetCanOptimize(true);

		_minDifferencePips = Param(nameof(MinDifferencePips), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA Spread", "Maximum fast-slow EMA spread", "Filters")
			.SetCanOptimize(true);

		_volatilityPeriod = Param(nameof(VolatilityPeriod), 15)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Range", "Number of candles for trailing", "Risk")
			.SetCanOptimize(true);

		_trailingEnabled = Param(nameof(TrailingEnabled), true)
			.SetDisplay("Trailing", "Enable trailing stop", "Risk");

		_trailingIndentPips = Param(nameof(TrailingIndentPips), 1)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Offset", "Indent from candle shadows", "Risk")
			.SetCanOptimize(true);

		_maFastPeriod = Param(nameof(MaFastPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA period", "Indicators")
			.SetCanOptimize(true);

		_maMediumPeriod = Param(nameof(MaMediumPeriod), 89)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA", "Medium EMA period", "Indicators")
			.SetCanOptimize(true);

		_maSlowPeriod = Param(nameof(MaSlowPeriod), 144)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA period", "Indicators")
			.SetCanOptimize(true);

		_osmaFastPeriod = Param(nameof(OsmaFastPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Fast", "Fast EMA for MACD", "Indicators")
			.SetCanOptimize(true);

		_osmaSlowPeriod = Param(nameof(OsmaSlowPeriod), 55)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Slow", "Slow EMA for MACD", "Indicators")
			.SetCanOptimize(true);

		_osmaSignalPeriod = Param(nameof(OsmaSignalPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("OsMA Signal", "Signal period for MACD", "Indicators")
			.SetCanOptimize(true);

		_rviPeriod = Param(nameof(RviPeriod), 44)
			.SetGreaterThanZero()
			.SetDisplay("RVI Period", "Relative Vigor Index period", "Indicators")
			.SetCanOptimize(true);

		_rviSignalLength = Param(nameof(RviSignalLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("RVI Signal", "Smoothing for RVI signal", "Indicators")
			.SetCanOptimize(true);

		_rviMax = Param(nameof(RviMax), 0.04m)
			.SetDisplay("RVI Max", "Upper threshold for RVI signal", "Filters")
			.SetCanOptimize(true);

		_rviMin = Param(nameof(RviMin), -0.04m)
			.SetDisplay("RVI Min", "Lower threshold for RVI signal", "Filters")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles used for calculations", "General");
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

		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_emaFast = new EMA { Length = MaFastPeriod };
		_emaMedium = new EMA { Length = MaMediumPeriod };
		_emaSlow = new EMA { Length = MaSlowPeriod };
		_macd = new MACD
		{
			ShortPeriod = OsmaFastPeriod,
			LongPeriod = OsmaSlowPeriod,
			SignalPeriod = OsmaSignalPeriod
		};
		_highest = new Highest { Length = VolatilityPeriod };
		_lowest = new Lowest { Length = VolatilityPeriod };
		_rvi = new RelativeVigorIndex { Length = RviPeriod };
		_rviSignal = new SimpleMovingAverage { Length = RviSignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_emaFast, _emaMedium, _emaSlow, _macd, _highest, _lowest, ProcessCandle)
			.Start();

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaFast, decimal emaMedium, decimal emaSlow, decimal macdLine, decimal macdSignal, decimal highestValue, decimal lowestValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var rviValue = _rvi.Process(candle);
		var rviSignalValue = _rviSignal.Process(rviValue);

		if (!rviValue.IsFinal || !rviSignalValue.IsFinal)
			return;

		if (!_emaFast.IsFormed || !_emaMedium.IsFormed || !_emaSlow.IsFormed || !_macd.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var step = CalculatePipSize();
		if (step == 0m)
		{
			step = Security.PriceStep ?? 0m;
		}
		if (step == 0m)
			step = 1m;

		var stopDistance = StopLossPips > 0 ? StopLossPips * step : 0m;
		var takeDistance = TakeProfitPips > 0 ? TakeProfitPips * step : 0m;
		var minDifference = MinDifferencePips * step;
		var indent = TrailingIndentPips * step;

		var rvi = rviValue.ToDecimal();
		var rviSignal = rviSignalValue.ToDecimal();

		UpdateTrailingStops(candle, highestValue, lowestValue, indent);
		HandleStopsAndTargets(candle);

		var canTrade = (Portfolio?.CurrentValue ?? decimal.MaxValue) >= MinBalance;

		var emaOrderLong = emaFast > emaMedium && emaMedium > emaSlow;
		var emaOrderShort = emaFast < emaMedium && emaMedium < emaSlow;
		var emaSpreadLong = Math.Abs(emaFast - emaSlow) < minDifference;
		var emaSpreadShort = Math.Abs(emaSlow - emaFast) < minDifference;

		var longCondition = canTrade && emaOrderLong && emaSpreadLong && macdLine > 0m && rvi > rviSignal && rviSignal >= RviMax;
		var shortCondition = canTrade && emaOrderShort && emaSpreadShort && macdLine < 0m && rvi < rviSignal && rviSignal <= RviMin;

		if (longCondition && Position <= 0)
		{
			if (Position < 0)
			{
				BuyMarket(Math.Abs(Position));
				ResetOrders();
			}

			if (Volume > 0m)
			{
				BuyMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = stopDistance > 0m ? _entryPrice - stopDistance : null;
				_takePrice = takeDistance > 0m ? _entryPrice + takeDistance : null;
			}
		}
		else if (shortCondition && Position >= 0)
		{
			if (Position > 0)
			{
				SellMarket(Math.Abs(Position));
				ResetOrders();
			}

			if (Volume > 0m)
			{
				SellMarket(Volume);
				_entryPrice = candle.ClosePrice;
				_stopPrice = stopDistance > 0m ? _entryPrice + stopDistance : null;
				_takePrice = takeDistance > 0m ? _entryPrice - takeDistance : null;
			}
		}
	}

	private void UpdateTrailingStops(ICandleMessage candle, decimal highestValue, decimal lowestValue, decimal indent)
	{
		if (!TrailingEnabled)
			return;

		if (Position > 0)
		{
			var newStop = lowestValue;
			if (newStop > 0m && candle.ClosePrice - newStop > indent && newStop - _entryPrice > indent)
			{
				if (!_stopPrice.HasValue || newStop - _stopPrice.Value > indent)
				{
					_stopPrice = newStop;
				}
			}
		}
		else if (Position < 0)
		{
			var newStop = highestValue;
			if (newStop > 0m && newStop - candle.ClosePrice > indent && _entryPrice - newStop > indent)
			{
				if (!_stopPrice.HasValue || _stopPrice.Value - newStop > indent)
				{
					_stopPrice = newStop;
				}
			}
		}
	}

	private void HandleStopsAndTargets(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (_stopPrice.HasValue && candle.LowPrice <= _stopPrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetOrders();
				return;
			}

			if (_takePrice.HasValue && candle.HighPrice >= _takePrice.Value)
			{
				SellMarket(Math.Abs(Position));
				ResetOrders();
			}
		}
		else if (Position < 0)
		{
			if (_stopPrice.HasValue && candle.HighPrice >= _stopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetOrders();
				return;
			}

			if (_takePrice.HasValue && candle.LowPrice <= _takePrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetOrders();
			}
		}
	}

	private void ResetOrders()
	{
		_stopPrice = null;
		_takePrice = null;
		_entryPrice = 0m;
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security is null)
			return 0m;

		var step = security.PriceStep ?? 0m;
		if (step == 0m)
			return 0m;

		var decimals = security.Decimals;
		return decimals == 3 || decimals == 5 ? step * 10m : step;
	}
}
