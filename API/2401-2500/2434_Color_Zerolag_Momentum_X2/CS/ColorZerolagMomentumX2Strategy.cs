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
/// Momentum cross strategy on two timeframes using zero lag moving average.
/// </summary>
public class ColorZerolagMomentumX2Strategy : Strategy
{
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<int> _trendMomentumPeriod;
	private readonly StrategyParam<int> _trendMaLength;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<int> _signalMomentumPeriod;
	private readonly StrategyParam<int> _signalMaLength;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private int _trend;
	private decimal? _prevSignalMomentum;
	private decimal? _prevSignalMa;

	/// <summary>
	/// Candle type for trend detection.
	/// </summary>
	public DataType TrendCandleType
	{
		get => _trendCandleType.Value;
		set => _trendCandleType.Value = value;
	}

	/// <summary>
	/// Momentum period on trend timeframe.
	/// </summary>
	public int TrendMomentumPeriod
	{
		get => _trendMomentumPeriod.Value;
		set => _trendMomentumPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length on trend timeframe.
	/// </summary>
	public int TrendMaLength
	{
		get => _trendMaLength.Value;
		set => _trendMaLength.Value = value;
	}

	/// <summary>
	/// Candle type for signals.
	/// </summary>
	public DataType SignalCandleType
	{
		get => _signalCandleType.Value;
		set => _signalCandleType.Value = value;
	}

	/// <summary>
	/// Momentum period on signal timeframe.
	/// </summary>
	public int SignalMomentumPeriod
	{
		get => _signalMomentumPeriod.Value;
		set => _signalMomentumPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length on signal timeframe.
	/// </summary>
	public int SignalMaLength
	{
		get => _signalMaLength.Value;
		set => _signalMaLength.Value = value;
	}

	/// <summary>
	/// Allow long entries.
	/// </summary>
	public bool BuyPosOpen
	{
		get => _buyPosOpen.Value;
		set => _buyPosOpen.Value = value;
	}

	/// <summary>
	/// Allow short entries.
	/// </summary>
	public bool SellPosOpen
	{
		get => _sellPosOpen.Value;
		set => _sellPosOpen.Value = value;
	}

	/// <summary>
	/// Close long positions on opposite signal.
	/// </summary>
	public bool BuyPosClose
	{
		get => _buyPosClose.Value;
		set => _buyPosClose.Value = value;
	}

	/// <summary>
	/// Close short positions on opposite signal.
	/// </summary>
	public bool SellPosClose
	{
		get => _sellPosClose.Value;
		set => _sellPosClose.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="ColorZerolagMomentumX2Strategy"/>.
	/// </summary>
	public ColorZerolagMomentumX2Strategy()
	{
		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Trend Timeframe", "Candle type for trend", "General");

		_trendMomentumPeriod = Param(nameof(TrendMomentumPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Trend Momentum Period", "Momentum length for trend", "Parameters")
			;

		_trendMaLength = Param(nameof(TrendMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trend Smooth Length", "Zero lag MA length for trend", "Parameters")
			;

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Signal Timeframe", "Candle type for signals", "General");

		_signalMomentumPeriod = Param(nameof(SignalMomentumPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Signal Momentum Period", "Momentum length for signals", "Parameters")
			;

		_signalMaLength = Param(nameof(SignalMaLength), 8)
			.SetGreaterThanZero()
			.SetDisplay("Signal Smooth Length", "Zero lag MA length for signals", "Parameters")
			;

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
			.SetDisplay("Buy Entries", "Enable long entries", "Signals");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
			.SetDisplay("Sell Entries", "Enable short entries", "Signals");

		_buyPosClose = Param(nameof(BuyPosClose), true)
			.SetDisplay("Buy Exits", "Close longs on opposite signal", "Signals");

		_sellPosClose = Param(nameof(SellPosClose), true)
			.SetDisplay("Sell Exits", "Close shorts on opposite signal", "Signals");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, TrendCandleType), (Security, SignalCandleType)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_trend = 0;
		_prevSignalMomentum = default;
		_prevSignalMa = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		StartProtection(
			takeProfit: new Unit(2, UnitTypes.Percent),
			stopLoss: new Unit(1, UnitTypes.Percent));

		var trendFast = new ExponentialMovingAverage { Length = TrendMaLength };
		var trendSlow = new ExponentialMovingAverage { Length = TrendMomentumPeriod };
		var trendSub = SubscribeCandles(TrendCandleType);
		trendSub.Bind(trendFast, trendSlow, ProcessTrend).Start();

		var signalFast = new ExponentialMovingAverage { Length = SignalMaLength };
		var signalSlow = new ExponentialMovingAverage { Length = SignalMomentumPeriod };
		var signalSub = SubscribeCandles(SignalCandleType);
		signalSub.Bind(signalFast, signalSlow, ProcessSignal).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSub);
			DrawIndicator(area, signalFast);
			DrawIndicator(area, signalSlow);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_trend = fast > slow ? 1 : -1;
	}

	private void ProcessSignal(ICandleMessage candle, decimal fast, decimal slow)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevSignalMomentum is null || _prevSignalMa is null)
		{
			_prevSignalMomentum = fast;
			_prevSignalMa = slow;
			return;
		}

		var buyOpen = BuyPosOpen && _prevSignalMomentum <= _prevSignalMa && fast > slow;
		var sellOpen = SellPosOpen && _prevSignalMomentum >= _prevSignalMa && fast < slow;

		if (buyOpen && Position == 0)
			BuyMarket();
		else if (sellOpen && Position == 0)
			SellMarket();

		_prevSignalMomentum = fast;
		_prevSignalMa = slow;
	}
}