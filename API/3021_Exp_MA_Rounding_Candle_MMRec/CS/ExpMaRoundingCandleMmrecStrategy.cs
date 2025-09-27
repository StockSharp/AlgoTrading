namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy that replicates the MA Rounding Candle MMRec Expert Advisor logic.
/// </summary>
public class ExpMaRoundingCandleMmrecStrategy : Strategy
{

	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<MaSmoothingMethod> _maMethodParam;
	private readonly StrategyParam<int> _maLengthParam;
	private readonly StrategyParam<decimal> _roundingFactorParam;
	private readonly StrategyParam<decimal> _gapParam;
	private readonly StrategyParam<int> _signalBarParam;
	private readonly StrategyParam<decimal> _tradeVolumeParam;
	private readonly StrategyParam<bool> _enableLongEntriesParam;
	private readonly StrategyParam<bool> _enableShortEntriesParam;
	private readonly StrategyParam<bool> _enableLongExitsParam;
	private readonly StrategyParam<bool> _enableShortExitsParam;
	private readonly StrategyParam<int> _bullishColorParam;
	private readonly StrategyParam<int> _bearishColorParam;

	private readonly List<int> _colorHistory = new();

	private MaRoundingCalculator _openRounding;
	private MaRoundingCalculator _highRounding;
	private MaRoundingCalculator _lowRounding;
	private MaRoundingCalculator _closeRounding;

	private decimal? _previousRoundedClose;

	public ExpMaRoundingCandleMmrecStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
		.SetDisplay("Candle type", "Timeframe used to build MA Rounding candles.", "General");

		_maMethodParam = Param(nameof(SmoothingMethod), MaSmoothingMethod.Simple)
		.SetDisplay("Smoothing", "Moving average smoothing method.", "Indicator");

		_maLengthParam = Param(nameof(MaLength), 12)
		.SetGreaterThanZero()
		.SetDisplay("MA length", "Number of periods for the moving average.", "Indicator");

		_roundingFactorParam = Param(nameof(RoundingFactor), 50m)
		.SetNotNegative()
		.SetDisplay("Rounding factor", "Multiplier for price step rounding threshold.", "Indicator");

		_gapParam = Param(nameof(GapSize), 10m)
		.SetNotNegative()
		.SetDisplay("Gap filter", "Gap in price steps that keeps the synthetic open anchored to the previous close.", "Indicator");

		_signalBarParam = Param(nameof(SignalBar), 1)
		.SetNotNegative()
		.SetDisplay("Signal bar", "Index of the bar used for signals (0=current, 1=previous, ...).", "Signals");

		_tradeVolumeParam = Param(nameof(TradeVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Trade volume", "Base volume used for new positions.", "Trading");

		_enableLongEntriesParam = Param(nameof(EnableLongEntries), true)
		.SetDisplay("Enable longs", "Allow opening of long positions.", "Trading");

		_enableShortEntriesParam = Param(nameof(EnableShortEntries), true)
		.SetDisplay("Enable shorts", "Allow opening of short positions.", "Trading");

		_enableLongExitsParam = Param(nameof(EnableLongExits), true)
		.SetDisplay("Close longs", "Allow closing of existing long positions.", "Trading");

		_enableShortExitsParam = Param(nameof(EnableShortExits), true)
			.SetDisplay("Close shorts", "Allow closing of existing short positions.", "Trading");

		_bullishColorParam = Param(nameof(BullishColor), 2)
			.SetDisplay("Bullish Color", "Color index representing bullish candles.", "Signals");

		_bearishColorParam = Param(nameof(BearishColor), 0)
			.SetDisplay("Bearish Color", "Color index representing bearish candles.", "Signals");
}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public MaSmoothingMethod SmoothingMethod
	{
		get => _maMethodParam.Value;
		set => _maMethodParam.Value = value;
	}

	public int MaLength
	{
		get => _maLengthParam.Value;
		set => _maLengthParam.Value = value;
	}

	public decimal RoundingFactor
	{
		get => _roundingFactorParam.Value;
		set => _roundingFactorParam.Value = value;
	}

	public decimal GapSize
	{
		get => _gapParam.Value;
		set => _gapParam.Value = value;
	}

	public int SignalBar
	{
		get => _signalBarParam.Value;
		set => _signalBarParam.Value = value;
	}

	public decimal TradeVolume
	{
		get => _tradeVolumeParam.Value;
		set
		{
			_tradeVolumeParam.Value = value;
			Volume = value;
		}
	}

	public bool EnableLongEntries
	{
		get => _enableLongEntriesParam.Value;
		set => _enableLongEntriesParam.Value = value;
	}

	public bool EnableShortEntries
	{
		get => _enableShortEntriesParam.Value;
		set => _enableShortEntriesParam.Value = value;
	}

	public bool EnableLongExits
	{
		get => _enableLongExitsParam.Value;
		set => _enableLongExitsParam.Value = value;
	}

	public bool EnableShortExits
	{
		get => _enableShortExitsParam.Value;
		set => _enableShortExitsParam.Value = value;
	}
	/// <summary>
	/// Candle color index treated as bullish.
	/// </summary>

	public int BullishColor
	{
		get => _bullishColorParam.Value;
		set => _bullishColorParam.Value = value;
	}
	/// <summary>
	/// Candle color index treated as bearish.
	/// </summary>

	public int BearishColor
	{
		get => _bearishColorParam.Value;
		set => _bearishColorParam.Value = value;
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

		_openRounding = null;
		_highRounding = null;
		_lowRounding = null;
		_closeRounding = null;

		_colorHistory.Clear();
		_previousRoundedClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_openRounding = new MaRoundingCalculator(CreateMovingAverage());
		_highRounding = new MaRoundingCalculator(CreateMovingAverage());
		_lowRounding = new MaRoundingCalculator(CreateMovingAverage());
		_closeRounding = new MaRoundingCalculator(CreateMovingAverage());

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (_openRounding == null || _highRounding == null || _lowRounding == null || _closeRounding == null)
		return;

		var priceStep = Security?.PriceStep ?? 1m;
		var roundingThreshold = Math.Max(0m, RoundingFactor) * priceStep;
		var gapThreshold = Math.Max(0m, GapSize) * priceStep;

		var time = candle.CloseTime != default ? candle.CloseTime : candle.OpenTime;
		if (time == default)
		time = candle.ServerTime;

		var openValue = _openRounding.Process(candle.OpenPrice, time, roundingThreshold);
		var highValue = _highRounding.Process(candle.HighPrice, time, roundingThreshold);
		var lowValue = _lowRounding.Process(candle.LowPrice, time, roundingThreshold);
		var closeValue = _closeRounding.Process(candle.ClosePrice, time, roundingThreshold);

		if (!openValue.HasValue || !highValue.HasValue || !lowValue.HasValue || !closeValue.HasValue)
		return;

		if (!_openRounding.IsFormed || !_closeRounding.IsFormed || !_highRounding.IsFormed || !_lowRounding.IsFormed)
		return;

		var roundedOpen = openValue.Value;
		var roundedClose = closeValue.Value;

		if (Math.Abs(candle.OpenPrice - candle.ClosePrice) <= gapThreshold && _previousRoundedClose.HasValue)
		{
			roundedOpen = _previousRoundedClose.Value;
		}

		_previousRoundedClose = roundedClose;

		var color = DetermineColor(roundedOpen, roundedClose);

		_colorHistory.Add(color);

		var maxHistory = Math.Max(SignalBar + 2, 2);
		if (_colorHistory.Count > maxHistory)
		{
			var removeCount = _colorHistory.Count - maxHistory;
			_colorHistory.RemoveRange(0, removeCount);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var recentIndex = _colorHistory.Count - 1 - SignalBar;
		if (recentIndex < 0)
		return;

		var olderIndex = recentIndex - 1;
		if (olderIndex < 0)
		return;

		var recentColor = _colorHistory[recentIndex];
		var olderColor = _colorHistory[olderIndex];

		var closeShort = EnableShortExits && olderColor == BullishColor && recentColor != BullishColor;
		var closeLong = EnableLongExits && olderColor == BearishColor && recentColor != BearishColor;
		var openLong = EnableLongEntries && olderColor == BullishColor && recentColor != BullishColor;
		var openShort = EnableShortEntries && olderColor == BearishColor && recentColor != BearishColor;

		if (closeLong && Position > 0)
		{
			SellMarket(Position);
		}

		if (closeShort && Position < 0)
		{
			BuyMarket(Math.Abs(Position));
		}

		if (openLong && Position <= 0)
		{
			var volume = TradeVolume;
			if (Position < 0)
			{
				volume += Math.Abs(Position);
			}

			if (volume > 0)
			{
				BuyMarket(volume);
			}
		}
		else if (openShort && Position >= 0)
		{
			var volume = TradeVolume;
			if (Position > 0)
			{
				volume += Position;
			}

			if (volume > 0)
			{
				SellMarket(volume);
			}
		}
	}

	private static int DetermineColor(decimal open, decimal close)
	{
		if (open < close)
		return BullishColor;

		if (open > close)
		return BearishColor;

		return 1;
	}

	private LengthIndicator<decimal> CreateMovingAverage()
	{
		return SmoothingMethod switch
		{
			MaSmoothingMethod.Simple => new SimpleMovingAverage { Length = MaLength },
			MaSmoothingMethod.Exponential => new ExponentialMovingAverage { Length = MaLength },
			MaSmoothingMethod.Smoothed => new SmoothedMovingAverage { Length = MaLength },
			MaSmoothingMethod.Weighted => new WeightedMovingAverage { Length = MaLength },
			_ => new SimpleMovingAverage { Length = MaLength },
		};
	}

	/// <summary>
	/// Available moving average smoothing methods.
	/// </summary>
	public enum MaSmoothingMethod
	{
		/// <summary>Simple moving average.</summary>
		Simple,

		/// <summary>Exponential moving average.</summary>
		Exponential,

		/// <summary>Smoothed moving average (RMA/SMMA).</summary>
		Smoothed,

		/// <summary>Weighted moving average.</summary>
		Weighted,
	}

	private sealed class MaRoundingCalculator
	{
		private readonly LengthIndicator<decimal> _ma;
		private decimal? _previousOutput;
		private decimal? _previousMa;
		private decimal _previousDirection;

		public MaRoundingCalculator(LengthIndicator<decimal> ma)
		{
			_ma = ma ?? throw new ArgumentNullException(nameof(ma));
		}

		public bool IsFormed => _ma.IsFormed;

		public decimal? Process(decimal price, DateTimeOffset time, decimal threshold)
		{
			var value = _ma.Process(new DecimalIndicatorValue(_ma, price, time));

			if (!value.IsFinal)
			return null;

			var currentMa = value.ToDecimal();

			decimal output;

			if (_previousOutput is null || _previousMa is null)
			{
				output = currentMa;
				_previousDirection = 0m;
			}
			else
			{
				var previousOutput = _previousOutput.Value;
				var previousMa = _previousMa.Value;

				var shouldUpdate =
				currentMa > previousMa + threshold ||
				currentMa < previousMa - threshold ||
				currentMa > previousOutput + threshold ||
				currentMa < previousOutput - threshold ||
				(currentMa > previousOutput && _previousDirection == 1m) ||
				(currentMa < previousOutput && _previousDirection == -1m);

				output = shouldUpdate ? currentMa : previousOutput;

				if (output < previousOutput)
				{
					_previousDirection = -1m;
				}
				else if (output > previousOutput)
				{
					_previousDirection = 1m;
				}
			}

			_previousOutput = output;
			_previousMa = currentMa;

			return output;
		}
	}
}
