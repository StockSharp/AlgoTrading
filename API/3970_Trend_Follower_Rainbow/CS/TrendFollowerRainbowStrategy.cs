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

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend following strategy that combines EMA crossover, MACD confirmation,
/// Laguerre filter thresholds, rainbow moving average structure and MFI filter.
/// </summary>
public class TrendFollowerRainbowStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<int> _tradingStartHour;
	private readonly StrategyParam<int> _tradingEndHour;
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _macdFastLength;
	private readonly StrategyParam<int> _macdSlowLength;
	private readonly StrategyParam<int> _macdSignalLength;
	private readonly StrategyParam<decimal> _laguerreGamma;
	private readonly StrategyParam<decimal> _laguerreBuyThreshold;
	private readonly StrategyParam<decimal> _laguerreSellThreshold;
	private readonly StrategyParam<int> _mfiPeriod;
	private readonly StrategyParam<decimal> _mfiBuyLevel;
	private readonly StrategyParam<decimal> _mfiSellLevel;
	private readonly StrategyParam<int> _rainbowGroup1Base;
	private readonly StrategyParam<int> _rainbowGroup2Base;
	private readonly StrategyParam<int> _rainbowGroup3Base;
	private readonly StrategyParam<int> _rainbowGroup4Base;
	private readonly StrategyParam<int> _rainbowGroup5Base;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _emaFast = null!;
	private ExponentialMovingAverage _emaSlow = null!;
	private MovingAverageConvergenceDivergence _macd = null!;
	private LaguerreFilter _laguerre = null!;
	private MoneyFlowIndex _mfi = null!;
	private ExponentialMovingAverage[][] _rainbowGroups = null!;

	private decimal? _previousFastEma;
	private decimal? _previousSlowEma;
	private decimal? _previousLaguerre;
	private decimal _pointValue;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrendFollowerRainbowStrategy"/> class.
	/// </summary>
	public TrendFollowerRainbowStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetDisplay("Order Volume", "Base order volume", "Trading")
		.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 17m)
		.SetDisplay("Take Profit (pts)", "Distance in price steps for take profit", "Risk Management")
		.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
		.SetDisplay("Stop Loss (pts)", "Distance in price steps for stop loss", "Risk Management")
		.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 45m)
		.SetDisplay("Trailing Stop (pts)", "Distance in price steps for trailing stop", "Risk Management")
		.SetCanOptimize(true);

		_tradingStartHour = Param(nameof(TradingStartHour), 1)
		.SetDisplay("Start Hour", "Hour (0-23) when trading window opens", "Trading Schedule")
		.SetCanOptimize(true);

		_tradingEndHour = Param(nameof(TradingEndHour), 23)
		.SetDisplay("End Hour", "Hour (0-23) when trading window closes", "Trading Schedule")
		.SetCanOptimize(true);

		_fastEmaLength = Param(nameof(FastEmaLength), 4)
		.SetRange(2, 20)
		.SetDisplay("Fast EMA", "Length of the fast EMA", "Indicators")
		.SetCanOptimize(true);

		_slowEmaLength = Param(nameof(SlowEmaLength), 8)
		.SetRange(3, 50)
		.SetDisplay("Slow EMA", "Length of the slow EMA", "Indicators")
		.SetCanOptimize(true);

		_macdFastLength = Param(nameof(MacdFastLength), 5)
		.SetDisplay("MACD Fast", "Fast EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSlowLength = Param(nameof(MacdSlowLength), 35)
		.SetDisplay("MACD Slow", "Slow EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_macdSignalLength = Param(nameof(MacdSignalLength), 5)
		.SetDisplay("MACD Signal", "Signal EMA length for MACD", "Indicators")
		.SetCanOptimize(true);

		_laguerreGamma = Param(nameof(LaguerreGamma), 0.7m)
		.SetRange(0.1m, 0.9m)
		.SetDisplay("Laguerre Gamma", "Smoothing factor for Laguerre filter", "Indicators")
		.SetCanOptimize(true);

		_laguerreBuyThreshold = Param(nameof(LaguerreBuyThreshold), 0.15m)
		.SetDisplay("Laguerre Buy", "Threshold crossed upward for long signals", "Indicators")
		.SetCanOptimize(true);

		_laguerreSellThreshold = Param(nameof(LaguerreSellThreshold), 0.75m)
		.SetDisplay("Laguerre Sell", "Threshold crossed downward for short signals", "Indicators")
		.SetCanOptimize(true);

		_mfiPeriod = Param(nameof(MfiPeriod), 14)
		.SetDisplay("MFI Period", "Money Flow Index calculation period", "Indicators")
		.SetCanOptimize(true);

		_mfiBuyLevel = Param(nameof(MfiBuyLevel), 40m)
		.SetDisplay("MFI Buy", "Upper bound for oversold check", "Indicators")
		.SetCanOptimize(true);

		_mfiSellLevel = Param(nameof(MfiSellLevel), 60m)
		.SetDisplay("MFI Sell", "Lower bound for overbought check", "Indicators")
		.SetCanOptimize(true);

		_rainbowGroup1Base = Param(nameof(RainbowGroup1Base), 5)
		.SetDisplay("Rainbow Group 1", "Base length for the fastest rainbow bundle", "Rainbow")
		.SetCanOptimize(true);

		_rainbowGroup2Base = Param(nameof(RainbowGroup2Base), 13)
		.SetDisplay("Rainbow Group 2", "Base length for the second rainbow bundle", "Rainbow")
		.SetCanOptimize(true);

		_rainbowGroup3Base = Param(nameof(RainbowGroup3Base), 21)
		.SetDisplay("Rainbow Group 3", "Base length for the middle rainbow bundle", "Rainbow")
		.SetCanOptimize(true);

		_rainbowGroup4Base = Param(nameof(RainbowGroup4Base), 34)
		.SetDisplay("Rainbow Group 4", "Base length for the fourth rainbow bundle", "Rainbow")
		.SetCanOptimize(true);

		_rainbowGroup5Base = Param(nameof(RainbowGroup5Base), 55)
		.SetDisplay("Rainbow Group 5", "Base length for the slowest rainbow bundle", "Rainbow")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Primary candle series", "General");
	}

	/// <summary>
	/// Base order volume.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
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
	/// Trailing stop distance expressed in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// First hour (0-23) when the strategy can evaluate entries.
	/// </summary>
	public int TradingStartHour
	{
		get => _tradingStartHour.Value;
		set => _tradingStartHour.Value = value;
	}

	/// <summary>
	/// Last hour (0-23) when the strategy can evaluate entries.
	/// </summary>
	public int TradingEndHour
	{
		get => _tradingEndHour.Value;
		set => _tradingEndHour.Value = value;
	}

	/// <summary>
	/// Fast EMA length used for the crossover signal.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length used for the crossover signal.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// MACD fast EMA length.
	/// </summary>
	public int MacdFastLength
	{
		get => _macdFastLength.Value;
		set => _macdFastLength.Value = value;
	}

	/// <summary>
	/// MACD slow EMA length.
	/// </summary>
	public int MacdSlowLength
	{
		get => _macdSlowLength.Value;
		set => _macdSlowLength.Value = value;
	}

	/// <summary>
	/// MACD signal EMA length.
	/// </summary>
	public int MacdSignalLength
	{
		get => _macdSignalLength.Value;
		set => _macdSignalLength.Value = value;
	}

	/// <summary>
	/// Laguerre filter smoothing factor.
	/// </summary>
	public decimal LaguerreGamma
	{
		get => _laguerreGamma.Value;
		set => _laguerreGamma.Value = value;
	}

	/// <summary>
	/// Laguerre threshold that needs to be crossed upward to allow long signals.
	/// </summary>
	public decimal LaguerreBuyThreshold
	{
		get => _laguerreBuyThreshold.Value;
		set => _laguerreBuyThreshold.Value = value;
	}

	/// <summary>
	/// Laguerre threshold that needs to be crossed downward to allow short signals.
	/// </summary>
	public decimal LaguerreSellThreshold
	{
		get => _laguerreSellThreshold.Value;
		set => _laguerreSellThreshold.Value = value;
	}

	/// <summary>
	/// Money Flow Index period.
	/// </summary>
	public int MfiPeriod
	{
		get => _mfiPeriod.Value;
		set => _mfiPeriod.Value = value;
	}

	/// <summary>
	/// Maximum MFI level that still allows long entries.
	/// </summary>
	public decimal MfiBuyLevel
	{
		get => _mfiBuyLevel.Value;
		set => _mfiBuyLevel.Value = value;
	}

	/// <summary>
	/// Minimum MFI level that still allows short entries.
	/// </summary>
	public decimal MfiSellLevel
	{
		get => _mfiSellLevel.Value;
		set => _mfiSellLevel.Value = value;
	}

	/// <summary>
	/// Base period for the fastest rainbow bundle.
	/// </summary>
	public int RainbowGroup1Base
	{
		get => _rainbowGroup1Base.Value;
		set => _rainbowGroup1Base.Value = value;
	}

	/// <summary>
	/// Base period for the second rainbow bundle.
	/// </summary>
	public int RainbowGroup2Base
	{
		get => _rainbowGroup2Base.Value;
		set => _rainbowGroup2Base.Value = value;
	}

	/// <summary>
	/// Base period for the third rainbow bundle.
	/// </summary>
	public int RainbowGroup3Base
	{
		get => _rainbowGroup3Base.Value;
		set => _rainbowGroup3Base.Value = value;
	}

	/// <summary>
	/// Base period for the fourth rainbow bundle.
	/// </summary>
	public int RainbowGroup4Base
	{
		get => _rainbowGroup4Base.Value;
		set => _rainbowGroup4Base.Value = value;
	}

	/// <summary>
	/// Base period for the fifth rainbow bundle.
	/// </summary>
	public int RainbowGroup5Base
	{
		get => _rainbowGroup5Base.Value;
		set => _rainbowGroup5Base.Value = value;
	}

	/// <summary>
	/// Candle type used by the strategy.
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

		_previousFastEma = null;
		_previousSlowEma = null;
		_previousLaguerre = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pointValue = Security?.PriceStep ?? 0m;
		Volume = OrderVolume;

		var takeProfit = ToAbsoluteUnit(TakeProfitPoints);
		var stopLoss = ToAbsoluteUnit(StopLossPoints);
		var trailingStop = ToAbsoluteUnit(TrailingStopPoints);

		if (takeProfit != null || stopLoss != null || trailingStop != null)
		{
			StartProtection(
			takeProfit: takeProfit,
			stopLoss: stopLoss,
			trailingStop: trailingStop,
			useMarketOrders: true);
		}

		_emaFast = new ExponentialMovingAverage { Length = FastEmaLength };
		_emaSlow = new ExponentialMovingAverage { Length = SlowEmaLength };
		_macd = new MovingAverageConvergenceDivergence
		{
			ShortMa = { Length = MacdFastLength },
			LongMa = { Length = MacdSlowLength },
			SignalMa = { Length = MacdSignalLength }
		};
		_laguerre = new LaguerreFilter { Gamma = LaguerreGamma };
		_mfi = new MoneyFlowIndex { Length = MfiPeriod };
		_rainbowGroups = BuildRainbowGroups();

		var indicators = new List<IIndicator>
		{
			_emaFast,
			_emaSlow,
			_macd,
			_laguerre,
			_mfi
		};

		foreach (var group in _rainbowGroups)
		{
			indicators.AddRange(group);
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(indicators, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _emaFast);
			DrawIndicator(area, _emaSlow);
			DrawIndicator(area, _laguerre);
			DrawOwnTrades(area);
		}
	}

	private ExponentialMovingAverage[][] BuildRainbowGroups()
	{
		var offsets = new[] { 0, 2, 4, 6 };

		return new[]
		{
			offsets.Select(o => new ExponentialMovingAverage { Length = Math.Max(1, RainbowGroup1Base + o) }).ToArray(),
			offsets.Select(o => new ExponentialMovingAverage { Length = Math.Max(1, RainbowGroup2Base + o) }).ToArray(),
			offsets.Select(o => new ExponentialMovingAverage { Length = Math.Max(1, RainbowGroup3Base + o) }).ToArray(),
			offsets.Select(o => new ExponentialMovingAverage { Length = Math.Max(1, RainbowGroup4Base + o) }).ToArray(),
			offsets.Select(o => new ExponentialMovingAverage { Length = Math.Max(1, RainbowGroup5Base + o) }).ToArray()
		};
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var hour = candle.CloseTime.LocalDateTime.Hour;
		if (hour <= TradingStartHour || hour >= TradingEndHour)
		{
			UpdatePreviousValues(values);
			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			UpdatePreviousValues(values);
			return;
		}

		var index = 0;

		if (!TryGetDecimal(values[index++], out var fastEma) || !TryGetDecimal(values[index++], out var slowEma))
		{
			UpdatePreviousValues(values, fastEma, slowEma);
			return;
		}

		var macdValue = values[index++];
		if (!macdValue.IsFinal || macdValue is not MovingAverageConvergenceDivergenceValue macdData ||
		macdData.Macd is not decimal macdMain || macdData.Signal is not decimal macdSignal)
		{
			UpdatePreviousValues(values, fastEma, slowEma);
			return;
		}

		if (!TryGetDecimal(values[index++], out var laguerre))
		{
			UpdatePreviousValues(values, fastEma, slowEma);
			return;
		}

		if (!TryGetDecimal(values[index++], out var mfi))
		{
			UpdatePreviousValues(values, fastEma, slowEma, laguerre);
			return;
		}

		var rainbowValues = new List<decimal[]>(_rainbowGroups.Length);
		for (var groupIndex = 0; groupIndex < _rainbowGroups.Length; groupIndex++)
		{
			var group = _rainbowGroups[groupIndex];
			var decimals = new decimal[group.Length];

			for (var i = 0; i < group.Length; i++)
			{
				if (!TryGetDecimal(values[index++], out var rainbow))
				{
					UpdatePreviousValues(values, fastEma, slowEma, laguerre);
					return;
				}

				decimals[i] = rainbow;
			}

			rainbowValues.Add(decimals);
		}

		var rainbowBullish = rainbowValues.All(bundle => IsMonotonic(bundle, descending: true));
		var rainbowBearish = rainbowValues.All(bundle => IsMonotonic(bundle, descending: false));

		var emaCrossUp = _previousFastEma is decimal prevFast && _previousSlowEma is decimal prevSlow &&
		prevFast < prevSlow && fastEma > slowEma;

		var emaCrossDown = _previousFastEma is decimal prevFastDown && _previousSlowEma is decimal prevSlowDown &&
		prevFastDown > prevSlowDown && fastEma < slowEma;

		var laguerreBullish = _previousLaguerre is decimal prevLagBull &&
		prevLagBull <= LaguerreBuyThreshold && laguerre > LaguerreBuyThreshold;

		var laguerreBearish = _previousLaguerre is decimal prevLagBear &&
		prevLagBear >= LaguerreSellThreshold && laguerre < LaguerreSellThreshold;

		var macdBullish = macdMain > 0m && macdSignal > 0m;
		var macdBearish = macdMain < 0m && macdSignal < 0m;

		var mfiBullish = mfi < MfiBuyLevel;
		var mfiBearish = mfi > MfiSellLevel;

		if (emaCrossUp && macdBullish && laguerreBullish && rainbowBullish && mfiBullish && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (emaCrossDown && macdBearish && laguerreBearish && rainbowBearish && mfiBearish && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_previousFastEma = fastEma;
		_previousSlowEma = slowEma;
		_previousLaguerre = laguerre;
	}

	private void UpdatePreviousValues(IIndicatorValue[] values, decimal? fastEma = null, decimal? slowEma = null, decimal? laguerre = null)
	{
		var index = 0;

		fastEma ??= TryGetDecimal(values[index++], out var fast) ? fast : null;
		slowEma ??= TryGetDecimal(values[index++], out var slow) ? slow : null;
		index++;
		laguerre ??= TryGetDecimal(values[index++], out var lag) ? lag : null;

		_previousFastEma = fastEma ?? _previousFastEma;
		_previousSlowEma = slowEma ?? _previousSlowEma;
		_previousLaguerre = laguerre ?? _previousLaguerre;
	}

	private bool IsMonotonic(decimal[] values, bool descending)
	{
		for (var i = 0; i < values.Length - 1; i++)
		{
			if (descending)
			{
				if (values[i] < values[i + 1])
				return false;
			}
			else
			{
				if (values[i] > values[i + 1])
				return false;
			}
		}

		return true;
	}

	private static bool TryGetDecimal(IIndicatorValue value, out decimal result)
	{
		if (!value.IsFinal)
		{
			result = default;
			return false;
		}

		result = value.ToDecimal();
		return true;
	}

	private Unit ToAbsoluteUnit(decimal points)
	{
		if (points <= 0m || _pointValue <= 0m)
		return null;

		return new Unit(points * _pointValue, UnitTypes.Absolute);
	}
}

