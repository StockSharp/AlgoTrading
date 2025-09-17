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
/// Port of the "MACD 1 MIN SCALPER" MetaTrader expert advisor.
/// Combines weighted moving averages, multi-timeframe MACD confirmation and momentum strength checks.
/// </summary>
public class Macd1MinScalperStrategy : Strategy
{
	private static readonly int[] MetaTraderMinutes = { 1, 5, 15, 30, 60, 240, 1440, 10080, 43200 };

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _fastMaPeriod;
	private readonly StrategyParam<int> _slowMaPeriod;
	private readonly StrategyParam<int> _macdFastPeriod;
	private readonly StrategyParam<int> _macdSlowPeriod;
	private readonly StrategyParam<int> _macdSignalPeriod;
	private readonly StrategyParam<int> _momentumPeriod;
	private readonly StrategyParam<decimal> _momentumThreshold;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<decimal> _stopLossPips;

	private WeightedMovingAverage _fastMa = null!;
	private WeightedMovingAverage _slowMa = null!;
	private WeightedMovingAverage _h1FastMa = null!;
	private WeightedMovingAverage _h1SlowMa = null!;
	private Momentum _momentum = null!;
	private MovingAverageConvergenceDivergenceSignal _macdBase = null!;
	private MovingAverageConvergenceDivergenceSignal _macdH1 = null!;
	private MovingAverageConvergenceDivergenceSignal _macdMonthly = null!;

	private decimal? _fastMaValue;
	private decimal? _slowMaValue;
	private decimal? _h1FastValue;
	private decimal? _h1SlowValue;
	private decimal? _macdBaseMain;
	private decimal? _macdBaseSignal;
	private decimal? _macdH1Main;
	private decimal? _macdH1Signal;
	private decimal? _macdMonthlyMain;
	private decimal? _macdMonthlySignal;

	private readonly Queue<decimal> _momentumDeviations = new();

	private DataType _momentumType;
	private DataType _h1Type;
	private DataType _monthlyType;
	private bool _useBaseForMomentum;
	private bool _useBaseForH1;
	private bool _useBaseForMonthly;
	private decimal _pipSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="Macd1MinScalperStrategy"/> class.
	/// </summary>
	public Macd1MinScalperStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Base Candle", "Working timeframe used for main signals.", "General");

		_orderVolume = Param(nameof(OrderVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume used for every market entry.", "Trading");

		_fastMaPeriod = Param(nameof(FastMaPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Fast WMA", "Length of the fast weighted moving average.", "Indicators");

		_slowMaPeriod = Param(nameof(SlowMaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("Slow WMA", "Length of the slow weighted moving average.", "Indicators");

		_macdFastPeriod = Param(nameof(MacdFastPeriod), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period for the MACD calculation.", "Indicators");

		_macdSlowPeriod = Param(nameof(MacdSlowPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period for the MACD calculation.", "Indicators");

		_macdSignalPeriod = Param(nameof(MacdSignalPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal EMA period for the MACD calculation.", "Indicators");

		_momentumPeriod = Param(nameof(MomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Period", "Length of the momentum filter timeframe.", "Indicators");

		_momentumThreshold = Param(nameof(MomentumThreshold), 0.3m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Momentum Threshold", "Minimal deviation from 100 required to confirm strength.", "Filters");

		_takeProfitPips = Param(nameof(TakeProfitPips), 50m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit (pips)", "Take-profit distance expressed in pips.", "Risk");

		_stopLossPips = Param(nameof(StopLossPips), 20m)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss (pips)", "Stop-loss distance expressed in pips.", "Risk");
	}

	/// <summary>
	/// Timeframe used for the main trading logic.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Volume used for new entries.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set
		{
			_orderVolume.Value = value;
			Volume = value;
		}
	}

	/// <summary>
	/// Fast weighted moving average length.
	/// </summary>
	public int FastMaPeriod
	{
		get => _fastMaPeriod.Value;
		set => _fastMaPeriod.Value = value;
	}

	/// <summary>
	/// Slow weighted moving average length.
	/// </summary>
	public int SlowMaPeriod
	{
		get => _slowMaPeriod.Value;
		set => _slowMaPeriod.Value = value;
	}

	/// <summary>
	/// Fast EMA period for MACD.
	/// </summary>
	public int MacdFastPeriod
	{
		get => _macdFastPeriod.Value;
		set => _macdFastPeriod.Value = value;
	}

	/// <summary>
	/// Slow EMA period for MACD.
	/// </summary>
	public int MacdSlowPeriod
	{
		get => _macdSlowPeriod.Value;
		set => _macdSlowPeriod.Value = value;
	}

	/// <summary>
	/// Signal EMA period for MACD.
	/// </summary>
	public int MacdSignalPeriod
	{
		get => _macdSignalPeriod.Value;
		set => _macdSignalPeriod.Value = value;
	}

	/// <summary>
	/// Period used by the momentum indicator.
	/// </summary>
	public int MomentumPeriod
	{
		get => _momentumPeriod.Value;
		set => _momentumPeriod.Value = value;
	}

	/// <summary>
	/// Minimal deviation from 100 required for the momentum filter.
	/// </summary>
	public decimal MomentumThreshold
	{
		get => _momentumThreshold.Value;
		set => _momentumThreshold.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_fastMaValue = null;
		_slowMaValue = null;
		_h1FastValue = null;
		_h1SlowValue = null;
		_macdBaseMain = null;
		_macdBaseSignal = null;
		_macdH1Main = null;
		_macdH1Signal = null;
		_macdMonthlyMain = null;
		_macdMonthlySignal = null;

		_momentumDeviations.Clear();

		_momentumType = default;
		_h1Type = default;
		_monthlyType = default;
		_useBaseForMomentum = false;
		_useBaseForH1 = false;
		_useBaseForMonthly = false;
		_pipSize = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		if (!TryGetTimeFrame(CandleType, out var baseFrame))
			throw new InvalidOperationException("Macd1MinScalperStrategy requires a timeframe candle type.");

		_momentumType = ResolveMomentumDataType(CandleType, baseFrame);
		_h1Type = TimeSpan.FromHours(1).TimeFrame();
		_monthlyType = TimeSpan.FromMinutes(43200).TimeFrame();

		_useBaseForMomentum = _momentumType == CandleType;
		_useBaseForH1 = _h1Type == CandleType;
		_useBaseForMonthly = _monthlyType == CandleType;

		_fastMa = CreateWeightedMovingAverage(FastMaPeriod);
		_slowMa = CreateWeightedMovingAverage(SlowMaPeriod);
		_macdBase = CreateMacd();

		_h1FastMa = _useBaseForH1 ? _fastMa : CreateWeightedMovingAverage(FastMaPeriod);
		_h1SlowMa = _useBaseForH1 ? _slowMa : CreateWeightedMovingAverage(SlowMaPeriod);
		_macdH1 = _useBaseForH1 ? _macdBase : CreateMacd();

		_macdMonthly = _useBaseForMonthly ? _macdBase : CreateMacd();
		_momentum = new Momentum { Length = MomentumPeriod };

		var baseSubscription = SubscribeCandles(CandleType);
		baseSubscription.BindEx(_fastMa, _slowMa, _macdBase, ProcessBaseCandle).Start();

		if (!_useBaseForH1)
		{
			SubscribeCandles(_h1Type).BindEx(_h1FastMa, _h1SlowMa, _macdH1, ProcessHigherCandle).Start();
		}

		if (!_useBaseForMomentum)
		{
			SubscribeCandles(_momentumType).Bind(_momentum, ProcessMomentumCandle).Start();
		}

		if (!_useBaseForMonthly)
		{
			SubscribeCandles(_monthlyType).BindEx(_macdMonthly, ProcessMonthlyCandle).Start();
		}

		_pipSize = CalculatePipSize();

		var takeProfit = TakeProfitPips > 0m ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;
		var stopLoss = StopLossPips > 0m ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;

		if (takeProfit != null || stopLoss != null)
		{
			StartProtection(takeProfit: takeProfit, stopLoss: stopLoss);
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, baseSubscription);
			DrawIndicator(area, _fastMa);
			DrawIndicator(area, _slowMa);
			DrawIndicator(area, _macdBase);
			DrawOwnTrades(area);
		}
	}

	private void ProcessBaseCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !macdValue.IsFinal)
			return;

		var fast = fastValue.ToDecimal();
		var slow = slowValue.ToDecimal();

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		_fastMaValue = fast;
		_slowMaValue = slow;
		_macdBaseMain = macdMain;
		_macdBaseSignal = macdSignal;

		if (_useBaseForH1)
		{
			_h1FastValue = fast;
			_h1SlowValue = slow;
			_macdH1Main = macdMain;
			_macdH1Signal = macdSignal;
		}

		if (_useBaseForMonthly)
		{
			_macdMonthlyMain = macdMain;
			_macdMonthlySignal = macdSignal;
		}

		if (_useBaseForMomentum)
		{
			var momentumValue = _momentum.Process(candle.ClosePrice, candle.CloseTime, true);
			if (momentumValue.IsFinal)
			{
				UpdateMomentum(momentumValue.ToDecimal());
			}
		}

		EvaluateSignals(candle);
	}

	private void ProcessHigherCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFinal || !slowValue.IsFinal || !macdValue.IsFinal)
			return;

		_h1FastValue = fastValue.ToDecimal();
		_h1SlowValue = slowValue.ToDecimal();

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		_macdH1Main = macdMain;
		_macdH1Signal = macdSignal;
	}

	private void ProcessMomentumCandle(ICandleMessage candle, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_momentum.IsFormed)
			return;

		UpdateMomentum(momentumValue);
	}

	private void ProcessMonthlyCandle(ICandleMessage candle, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!macdValue.IsFinal)
			return;

		var macd = (MovingAverageConvergenceDivergenceSignalValue)macdValue;
		if (macd.Macd is not decimal macdMain || macd.Signal is not decimal macdSignal)
			return;

		_macdMonthlyMain = macdMain;
		_macdMonthlySignal = macdSignal;
	}

	private void UpdateMomentum(decimal momentum)
	{
		var deviation = Math.Abs(100m - momentum);
		_momentumDeviations.Enqueue(deviation);

		while (_momentumDeviations.Count > 3)
		{
			_momentumDeviations.Dequeue();
		}
	}

	private void EvaluateSignals(ICandleMessage candle)
	{
		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_fastMaValue is not decimal fast || _slowMaValue is not decimal slow)
			return;

		if (_h1FastValue is not decimal higherFast || _h1SlowValue is not decimal higherSlow)
			return;

		if (_macdBaseMain is not decimal macdMain || _macdBaseSignal is not decimal macdSignal)
			return;

		if (_macdH1Main is not decimal macdH1Main || _macdH1Signal is not decimal macdH1Signal)
			return;

		if (_macdMonthlyMain is not decimal macdMonthlyMain || _macdMonthlySignal is not decimal macdMonthlySignal)
			return;

		var momentumConfirmed = false;
		foreach (var deviation in _momentumDeviations)
		{
			if (deviation >= MomentumThreshold)
			{
				momentumConfirmed = true;
				break;
			}
		}

		if (!momentumConfirmed)
			return;

		var longSignal = fast > slow && higherFast > higherSlow &&
			macdMain > macdSignal && macdH1Main > macdH1Signal && macdMonthlyMain > macdMonthlySignal;

		var shortSignal = fast < slow && higherFast < higherSlow &&
			macdMain < macdSignal && macdH1Main < macdH1Signal && macdMonthlyMain < macdMonthlySignal;

		var position = Position;

		if (longSignal && position <= 0m)
		{
			var volume = GetEntryVolume(position);
			if (volume > 0m)
			{
				BuyMarket(volume);
			}
		}
		else if (shortSignal && position >= 0m)
		{
			var volume = GetEntryVolume(position);
			if (volume > 0m)
			{
				SellMarket(volume);
			}
		}
	}

	private decimal GetEntryVolume(decimal currentPosition)
	{
		var volume = OrderVolume;
		if (volume <= 0m)
			return 0m;

		if (currentPosition > 0m)
		{
			volume += currentPosition;
		}
		else if (currentPosition < 0m)
		{
			volume += Math.Abs(currentPosition);
		}

		return AdjustVolume(volume);
	}

	private decimal AdjustVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
			return volume;

		var step = security.VolumeStep;
		if (step > 0m)
		{
			var steps = Math.Max(1m, Math.Round(volume / step, MidpointRounding.AwayFromZero));
			volume = steps * step;
		}

		var minVolume = security.MinVolume;
		if (minVolume > 0m && volume < minVolume)
			volume = minVolume.Value;

		var maxVolume = security.MaxVolume;
		if (maxVolume > 0m && volume > maxVolume)
			volume = maxVolume.Value;

		return volume;
	}

	private WeightedMovingAverage CreateWeightedMovingAverage(int length)
	{
		return new WeightedMovingAverage
		{
			Length = length,
			CandlePrice = CandlePrice.Typical
		};
	}

	private MovingAverageConvergenceDivergenceSignal CreateMacd()
	{
		return new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFastPeriod },
				LongMa = { Length = MacdSlowPeriod }
			},
			SignalMa = { Length = MacdSignalPeriod }
		};
	}

	private static bool TryGetTimeFrame(DataType type, out TimeSpan frame)
	{
		if (type.MessageType == typeof(TimeFrameCandleMessage) && type.Arg is TimeSpan span)
		{
			frame = span;
			return true;
		}

		frame = default;
		return false;
	}

	private DataType ResolveMomentumDataType(DataType baseType, TimeSpan baseFrame)
	{
		var minutes = (int)Math.Round(baseFrame.TotalMinutes);
		var index = Array.IndexOf(MetaTraderMinutes, minutes);

		if (index >= 0)
		{
			var newIndex = Math.Min(MetaTraderMinutes.Length - 1, index + 1);
			var resolved = MetaTraderMinutes[newIndex];
			return TimeSpan.FromMinutes(resolved).TimeFrame();
		}

		var multiplied = TimeSpan.FromTicks(baseFrame.Ticks * 4);
		return multiplied.TimeFrame();
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var priceStep = security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return 1m;

		return security.Decimals is 3 or 5 ? priceStep * 10m : priceStep;
	}
}
