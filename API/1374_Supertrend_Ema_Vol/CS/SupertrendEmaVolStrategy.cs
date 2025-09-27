using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend strategy with EMA confirmation and volume filter.
/// </summary>
public class SupertrendEmaVolStrategy : Strategy
{
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<bool> _allowLong;
	private readonly StrategyParam<bool> _allowShort;
	private readonly StrategyParam<decimal> _slMultiplier;
	private readonly StrategyParam<bool> _useVolumeFilter;
	private readonly StrategyParam<int> _volumeEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _volumeEma;
	private bool _prevTrendUp;
	private bool _initialized;
	private decimal _entryPrice;

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// ATR multiplier.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	/// <summary>
	/// EMA length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Start trading from this date.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
	}

	/// <summary>
	/// Allow long trades.
	/// </summary>
	public bool AllowLong
	{
		get => _allowLong.Value;
		set => _allowLong.Value = value;
	}

	/// <summary>
	/// Allow short trades.
	/// </summary>
	public bool AllowShort
	{
		get => _allowShort.Value;
		set => _allowShort.Value = value;
	}

	/// <summary>
	/// Stop loss ATR multiplier.
	/// </summary>
	public decimal SlMultiplier
	{
		get => _slMultiplier.Value;
		set => _slMultiplier.Value = value;
	}

	/// <summary>
	/// Enable volume filter.
	/// </summary>
	public bool UseVolumeFilter
	{
		get => _useVolumeFilter.Value;
		set => _useVolumeFilter.Value = value;
	}

	/// <summary>
	/// Volume EMA length.
	/// </summary>
	public int VolumeEmaLength
	{
		get => _volumeEmaLength.Value;
		set => _volumeEmaLength.Value = value;
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
	/// Initializes a new instance of the <see cref="SupertrendEmaVolStrategy"/>.
	/// </summary>
	public SupertrendEmaVolStrategy()
	{
		_atrPeriod = Param(nameof(AtrPeriod), 10)
		.SetGreaterThanZero()
		.SetDisplay("ATR Period", "ATR period", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(5, 20, 1);

		_atrMultiplier = Param(nameof(AtrMultiplier), 3m)
		.SetGreaterThanZero()
		.SetDisplay("ATR Multiplier", "ATR multiplier", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_emaLength = Param(nameof(EmaLength), 21)
		.SetGreaterThanZero()
		.SetDisplay("EMA Length", "Length for price EMA", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(10, 50, 1);

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero))
		.SetDisplay("Start Date", "Start trading from", "General");

		_allowLong = Param(nameof(AllowLong), true)
		.SetDisplay("Allow Long", "Enable long trades", "General");

		_allowShort = Param(nameof(AllowShort), false)
		.SetDisplay("Allow Short", "Enable short trades", "General");

		_slMultiplier = Param(nameof(SlMultiplier), 2m)
		.SetGreaterThanZero()
		.SetDisplay("SL Multiplier", "ATR multiplier for stop loss", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(1m, 5m, 0.5m);

		_useVolumeFilter = Param(nameof(UseVolumeFilter), true)
		.SetDisplay("Use Volume Filter", "Enable volume filter", "Parameters");

		_volumeEmaLength = Param(nameof(VolumeEmaLength), 20)
		.SetGreaterThanZero()
		.SetDisplay("Volume EMA Length", "Length for volume EMA", "Parameters")
		.SetCanOptimize(true)
		.SetOptimize(10, 40, 1);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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
		_prevTrendUp = false;
		_initialized = false;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var supertrend = new SuperTrend { Length = AtrPeriod, Multiplier = AtrMultiplier };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };
		_volumeEma = new ExponentialMovingAverage { Length = VolumeEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(supertrend, ema, atr, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, ema);
		DrawIndicator(area, supertrend);
		DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var volEma = _volumeEma.Process(candle.TotalVolume, candle.OpenTime, true).ToDecimal();
		if (!stValue.IsFinal || !emaValue.IsFinal || !atrValue.IsFinal || !_volumeEma.IsFormed)
		return;

		if (candle.OpenTime < StartDate || !IsFormedAndOnlineAndAllowTrading())
		return;

		var st = (SuperTrendIndicatorValue)stValue;
		var trendUp = st.IsUpTrend;
		var ema = emaValue.ToDecimal();
		var atr = atrValue.ToDecimal();
		var price = candle.ClosePrice;
		var volumeOk = !UseVolumeFilter || candle.TotalVolume > volEma;

		if (!_initialized)
		{
		_prevTrendUp = trendUp;
		_initialized = true;
		return;
		}

		var buySignal = trendUp && !_prevTrendUp;
		var sellSignal = !trendUp && _prevTrendUp;

		if (sellSignal && Position > 0)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		}
		else if (buySignal && Position < 0)
		{
		BuyMarket(Math.Abs(Position));
		_entryPrice = 0m;
		}

		if (buySignal && price > ema && AllowLong && volumeOk && Position <= 0)
		{
		BuyMarket(Volume + Math.Abs(Position));
		_entryPrice = price;
		}
		else if (sellSignal && price < ema && AllowShort && volumeOk && Position >= 0)
		{
		SellMarket(Volume + Math.Abs(Position));
		_entryPrice = price;
		}

		if (Position > 0 && SlMultiplier > 0)
		{
		var stop = _entryPrice - SlMultiplier * atr;
		if (candle.LowPrice <= stop)
		{
		SellMarket(Position);
		_entryPrice = 0m;
		}
		}
		else if (Position < 0 && SlMultiplier > 0)
		{
		var stop = _entryPrice + SlMultiplier * atr;
		if (candle.HighPrice >= stop)
		{
		BuyMarket(Math.Abs(Position));
		_entryPrice = 0m;
		}
		}

		_prevTrendUp = trendUp;
	}
}
