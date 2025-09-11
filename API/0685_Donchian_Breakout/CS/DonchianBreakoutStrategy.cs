namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Donchian Breakout Strategy.
/// </summary>
public class DonchianBreakoutStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _entryLength;
	private readonly StrategyParam<int> _exitLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<int> _atrSmaLength;
	private readonly StrategyParam<bool> _enableLongs;
	private readonly StrategyParam<bool> _enableShorts;
	private readonly StrategyParam<bool> _useVolatilityFilter;
	private readonly StrategyParam<bool> _useVolumeFilter;

	private DonchianChannels _entryDonchian;
	private DonchianChannels _exitDonchian;
	private AverageTrueRange _atr;
	private ExponentialMovingAverage _ema;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _volumeSma;
	private SimpleMovingAverage _atrSma;

	private decimal? _longStop;
	private decimal? _shortStop;

	public DonchianBreakoutStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles", "General");

		_entryLength = Param(nameof(EntryLength), 20)
			.SetDisplay("Donchian Entry Length", "Length for entry Donchian channel", "Donchian")
			.SetCanOptimize(true);

		_exitLength = Param(nameof(ExitLength), 10)
			.SetDisplay("Donchian Exit Length", "Length for exit Donchian channel", "Donchian")
			.SetCanOptimize(true);

		_atrLength = Param(nameof(AtrLength), 14)
			.SetDisplay("ATR Length", "ATR period", "ATR")
			.SetCanOptimize(true);

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetDisplay("ATR Stop Multiplier", "Stop loss ATR multiplier", "ATR")
			.SetCanOptimize(true);

		_emaLength = Param(nameof(EmaLength), 50)
			.SetDisplay("EMA Length", "EMA trend filter period", "Filters")
			.SetCanOptimize(true);

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetDisplay("Volume SMA Length", "Volume SMA period", "Filters")
			.SetCanOptimize(true);

		_atrSmaLength = Param(nameof(AtrSmaLength), 20)
			.SetDisplay("ATR SMA Length", "ATR SMA period", "Filters")
			.SetCanOptimize(true);

		_enableLongs = Param(nameof(EnableLongs), true)
			.SetDisplay("Enable Longs", "Allow long trades", "Filters");

		_enableShorts = Param(nameof(EnableShorts), true)
			.SetDisplay("Enable Shorts", "Allow short trades", "Filters");

		_useVolatilityFilter = Param(nameof(UseVolatilityFilter), true)
			.SetDisplay("Use Volatility Filter", "ATR must be above its SMA", "Filters");

		_useVolumeFilter = Param(nameof(UseVolumeFilter), false)
			.SetDisplay("Use Volume Filter", "Volume must be above SMA", "Filters");
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int EntryLength
	{
		get => _entryLength.Value;
		set => _entryLength.Value = value;
	}

	public int ExitLength
	{
		get => _exitLength.Value;
		set => _exitLength.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
	}

	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	public int VolumeSmaLength
	{
		get => _volumeSmaLength.Value;
		set => _volumeSmaLength.Value = value;
	}

	public int AtrSmaLength
	{
		get => _atrSmaLength.Value;
		set => _atrSmaLength.Value = value;
	}

	public bool EnableLongs
	{
		get => _enableLongs.Value;
		set => _enableLongs.Value = value;
	}

	public bool EnableShorts
	{
		get => _enableShorts.Value;
		set => _enableShorts.Value = value;
	}

	public bool UseVolatilityFilter
	{
		get => _useVolatilityFilter.Value;
		set => _useVolatilityFilter.Value = value;
	}

	public bool UseVolumeFilter
	{
		get => _useVolumeFilter.Value;
		set => _useVolumeFilter.Value = value;
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

		_entryDonchian = new DonchianChannels { Length = EntryLength };
		_exitDonchian = new DonchianChannels { Length = ExitLength };
		_atr = new AverageTrueRange { Length = AtrLength };
		_ema = new ExponentialMovingAverage { Length = EmaLength };
		_rsi = new RelativeStrengthIndex { Length = 14 };
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };
		_atrSma = new SimpleMovingAverage { Length = AtrSmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_entryDonchian, _exitDonchian, _atr, _ema, _rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _entryDonchian);
			DrawIndicator(area, _exitDonchian);
			DrawIndicator(area, _ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue entryValue, IIndicatorValue exitValue, IIndicatorValue atrValue, IIndicatorValue emaValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_entryDonchian.IsFormed || !_exitDonchian.IsFormed || !_atr.IsFormed || !_ema.IsFormed || !_rsi.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var entryBands = (DonchianChannelsValue)entryValue;
		var exitBands = (DonchianChannelsValue)exitValue;

		if (entryBands.UpBand is not decimal entryHigh || entryBands.LowBand is not decimal entryLow || exitBands.UpBand is not decimal exitHigh || exitBands.LowBand is not decimal exitLow)
			return;

		var atr = atrValue.ToDecimal();
		var ema = emaValue.ToDecimal();
		var rsi = rsiValue.ToDecimal();

		var atrSma = _atrSma.Process(atr, candle.ServerTime, true).ToDecimal();
		var volSma = _volumeSma.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();

		var volatilityPass = !UseVolatilityFilter || atr > atrSma;
		var volumePass = !UseVolumeFilter || candle.TotalVolume > volSma;

		if (Position > 0)
		{
			if (candle.ClosePrice < exitLow || (_longStop.HasValue && candle.LowPrice <= _longStop.Value))
			{
				SellMarket(Position);
				_longStop = null;
			}
		}
		else if (Position < 0)
		{
			if (candle.ClosePrice > exitHigh || (_shortStop.HasValue && candle.HighPrice >= _shortStop.Value))
			{
				BuyMarket(Math.Abs(Position));
				_shortStop = null;
			}
		}
		else
		{
			var longCondition = EnableLongs && candle.ClosePrice > entryHigh && candle.ClosePrice > ema && rsi > 50m && volatilityPass && volumePass;
			var shortCondition = EnableShorts && candle.ClosePrice < entryLow && candle.ClosePrice < ema && rsi < 50m && volatilityPass && volumePass;

			if (longCondition)
			{
				var volume = Volume + Math.Abs(Position);
				BuyMarket(volume);
				_longStop = candle.ClosePrice - atr * AtrMultiplier;
				_shortStop = null;
			}
			else if (shortCondition)
			{
				var volume = Volume + Math.Abs(Position);
				SellMarket(volume);
				_shortStop = candle.ClosePrice + atr * AtrMultiplier;
				_longStop = null;
			}
		}
	}
}
