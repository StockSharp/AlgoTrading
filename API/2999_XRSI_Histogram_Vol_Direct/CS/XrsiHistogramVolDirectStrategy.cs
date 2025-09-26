using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on the XRSI Histogram Vol Direct expert advisor.
/// </summary>
public class XrsiHistogramVolDirectStrategy : Strategy
{
	/// <summary>
	/// Supported smoothing types for the RSI*volume series.
	/// </summary>
	public enum XrsiSmoothMethod
	{
		Sma,
		Ema,
		Smma,
		Wma,
	}

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _smoothLength;
	private readonly StrategyParam<XrsiSmoothMethod> _smoothMethod;
	private readonly StrategyParam<bool> _useTickVolume;
	private readonly StrategyParam<bool> _allowBuyOpen;
	private readonly StrategyParam<bool> _allowSellOpen;
	private readonly StrategyParam<bool> _allowBuyClose;
	private readonly StrategyParam<bool> _allowSellClose;

	private RSI? _rsi;
	private IIndicator _mainSmoother;
	private IIndicator _volumeSmoother;
	private decimal? _previousMainValue;
	private int? _previousColor;
	private int? _previousPreviousColor;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public int SmoothLength
	{
		get => _smoothLength.Value;
		set => _smoothLength.Value = value;
	}

	public XrsiSmoothMethod SmoothMethod
	{
		get => _smoothMethod.Value;
		set => _smoothMethod.Value = value;
	}

	public bool UseTickVolume
	{
		get => _useTickVolume.Value;
		set => _useTickVolume.Value = value;
	}

	public bool AllowBuyOpen
	{
		get => _allowBuyOpen.Value;
		set => _allowBuyOpen.Value = value;
	}

	public bool AllowSellOpen
	{
		get => _allowSellOpen.Value;
		set => _allowSellOpen.Value = value;
	}

	public bool AllowBuyClose
	{
		get => _allowBuyClose.Value;
		set => _allowBuyClose.Value = value;
	}

	public bool AllowSellClose
	{
		get => _allowSellClose.Value;
		set => _allowSellClose.Value = value;
	}

	public XrsiHistogramVolDirectStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles used by the strategy", "General");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI lookback period", "Indicator");

		_smoothLength = Param(nameof(SmoothLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing Length", "Length of the moving average applied to RSI*volume", "Indicator");

		_smoothMethod = Param(nameof(SmoothMethod), XrsiSmoothMethod.Sma)
			.SetDisplay("Smoothing Method", "Type of moving average used for smoothing", "Indicator")
			.SetCanOptimize(false);

		_useTickVolume = Param(nameof(UseTickVolume), true)
			.SetDisplay("Use Tick Volume", "Use tick count instead of traded volume", "Indicator");

		_allowBuyOpen = Param(nameof(AllowBuyOpen), true)
			.SetDisplay("Allow Buy Open", "Enable opening long positions", "Trading");

		_allowSellOpen = Param(nameof(AllowSellOpen), true)
			.SetDisplay("Allow Sell Open", "Enable opening short positions", "Trading");

		_allowBuyClose = Param(nameof(AllowBuyClose), true)
			.SetDisplay("Allow Buy Close", "Enable closing long positions on opposite signal", "Trading");

		_allowSellClose = Param(nameof(AllowSellClose), true)
			.SetDisplay("Allow Sell Close", "Enable closing short positions on opposite signal", "Trading");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rsi = new RSI { Length = RsiPeriod };
		_mainSmoother = CreateSmoother();
		_volumeSmoother = CreateSmoother();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rsi);
		}
	}

	private IIndicator CreateSmoother()
	{
		return SmoothMethod switch
		{
			XrsiSmoothMethod.Sma => new SMA { Length = SmoothLength },
			XrsiSmoothMethod.Ema => new EMA { Length = SmoothLength },
			XrsiSmoothMethod.Smma => new SMMA { Length = SmoothLength },
			XrsiSmoothMethod.Wma => new WMA { Length = SmoothLength },
			_ => new SMA { Length = SmoothLength },
		};
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading() || _mainSmoother == null || _volumeSmoother == null)
			return;

		var volume = GetVolume(candle);
		if (volume <= 0m)
			return;

		// Convert RSI into a volume-weighted oscillator around zero.
		var centeredRsi = (rsiValue - 50m) * volume;

		// Smooth both the oscillator and the raw volume with the selected averaging method.
		var smoothedValue = _mainSmoother.Process(new DecimalIndicatorValue(_mainSmoother, centeredRsi, candle.OpenTime));
		var smoothedVolume = _volumeSmoother.Process(new DecimalIndicatorValue(_volumeSmoother, volume, candle.OpenTime));

		if (smoothedValue is not DecimalIndicatorValue { IsFinal: true, Value: decimal mainValue })
			return;

		// The original advisor only checks that the smoothing is ready, the actual volume level is not used later.
		if (smoothedVolume is not DecimalIndicatorValue { IsFinal: true })
			return;

		var currentColor = CalculateColor(mainValue);

		// Use the two previous colors to mimic the buffer based logic from MQL.
		if (_previousPreviousColor.HasValue && _previousColor.HasValue)
		{
			HandleSignals(_previousPreviousColor.Value, _previousColor.Value);
		}

		_previousPreviousColor = _previousColor;
		_previousColor = currentColor;
		_previousMainValue = mainValue;
	}

	private void HandleSignals(int olderColor, int lastColor)
	{
		// Color 0 corresponds to rising oscillator values in the original indicator.
		if (olderColor == 0)
		{
			// Close short positions before flipping to long setups.
			if (AllowSellClose && Position < 0)
			{
				BuyMarket();
			}

			// Open a new long position when the latest bar turns down from a previous rise.
			if (AllowBuyOpen && lastColor == 1 && Position <= 0)
			{
				BuyMarket();
			}
		}
		else if (olderColor == 1)
		{
			// Close long positions before switching to short setups.
			if (AllowBuyClose && Position > 0)
			{
				SellMarket();
			}

			// Open a new short position when the latest bar turns up from a previous fall.
			if (AllowSellOpen && lastColor == 0 && Position >= 0)
			{
				SellMarket();
			}
		}
	}

	private int CalculateColor(decimal currentValue)
	{
		if (!_previousMainValue.HasValue)
			return 0;

		if (currentValue > _previousMainValue.Value)
			return 0;

		if (currentValue < _previousMainValue.Value)
			return 1;

		return _previousColor ?? 0;
	}

	private decimal GetVolume(ICandleMessage candle)
	{
		if (UseTickVolume)
			return candle.TotalTicks ?? 0m;

		return candle.TotalVolume ?? candle.Volume ?? 0m;
	}
}
