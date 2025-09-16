using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual timeframe Fisher Transform strategy.
/// Uses Fisher Transform on a higher timeframe to define trend.
/// Entries are generated on a lower timeframe when Fisher crosses its previous value.
/// </summary>
public class FisherTransformX2Strategy : Strategy
{
	private readonly StrategyParam<int> _trendLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<DataType> _trendCandleType;
	private readonly StrategyParam<DataType> _signalCandleType;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;
	private readonly StrategyParam<bool> _buyCloseSignal;
	private readonly StrategyParam<bool> _sellCloseSignal;

	private FisherTransform _trendFisher;
	private FisherTransform _signalFisher;

	private decimal _prevTrendFisher;
	private bool _isFirstTrend = true;
	private int _trendDirection;

	private decimal _prevSignalFisher;
	private decimal _prevPrevSignalFisher;
	private int _signalCount;

	/// <summary>
	/// Length for trend Fisher Transform.
	/// </summary>
	public int TrendLength { get => _trendLength.Value; set => _trendLength.Value = value; }

	/// <summary>
	/// Length for signal Fisher Transform.
	/// </summary>
	public int SignalLength { get => _signalLength.Value; set => _signalLength.Value = value; }

	/// <summary>
	/// Candle type for trend calculation.
	/// </summary>
	public DataType TrendCandleType { get => _trendCandleType.Value; set => _trendCandleType.Value = value; }

	/// <summary>
	/// Candle type for signal generation.
	/// </summary>
	public DataType SignalCandleType { get => _signalCandleType.Value; set => _signalCandleType.Value = value; }

	/// <summary>
	/// Take profit in points.
	/// </summary>
	public decimal TakeProfit { get => _takeProfitPoints.Value; set => _takeProfitPoints.Value = value; }

	/// <summary>
	/// Stop loss in points.
	/// </summary>
	public decimal StopLoss { get => _stopLossPoints.Value; set => _stopLossPoints.Value = value; }

	/// <summary>
	/// Enable opening long positions.
	/// </summary>
	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }

	/// <summary>
	/// Enable opening short positions.
	/// </summary>
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }

	/// <summary>
	/// Close long positions on trend change.
	/// </summary>
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }

	/// <summary>
	/// Close short positions on trend change.
	/// </summary>
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }

	/// <summary>
	/// Close long positions on signal timeframe cross.
	/// </summary>
	public bool BuyCloseSignal { get => _buyCloseSignal.Value; set => _buyCloseSignal.Value = value; }

	/// <summary>
	/// Close short positions on signal timeframe cross.
	/// </summary>
	public bool SellCloseSignal { get => _sellCloseSignal.Value; set => _sellCloseSignal.Value = value; }

	/// <summary>
	/// Initializes a new instance of <see cref="FisherTransformX2Strategy"/>.
	/// </summary>
	public FisherTransformX2Strategy()
	{
		_trendLength = Param(nameof(TrendLength), 10)
			.SetDisplay("Trend Length", "Fisher length for trend timeframe", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_signalLength = Param(nameof(SignalLength), 10)
			.SetDisplay("Signal Length", "Fisher length for signal timeframe", "General")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_trendCandleType = Param(nameof(TrendCandleType), TimeSpan.FromHours(6).TimeFrame())
			.SetDisplay("Trend Timeframe", "Candle type for trend determination", "General");

		_signalCandleType = Param(nameof(SignalCandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Signal Timeframe", "Candle type for entries", "General");

		_takeProfitPoints = Param(nameof(TakeProfit), 2000m)
			.SetDisplay("Take Profit", "Take profit in points", "Risk");

		_stopLossPoints = Param(nameof(StopLoss), 1000m)
			.SetDisplay("Stop Loss", "Stop loss in points", "Risk");

		_buyOpen = Param(nameof(BuyOpen), true)
			.SetDisplay("Allow Buy", "Enable opening long positions", "Switches");

		_sellOpen = Param(nameof(SellOpen), true)
			.SetDisplay("Allow Sell", "Enable opening short positions", "Switches");

		_buyClose = Param(nameof(BuyClose), true)
			.SetDisplay("Close Buy on Trend", "Close longs on opposite trend", "Switches");

		_sellClose = Param(nameof(SellClose), true)
			.SetDisplay("Close Sell on Trend", "Close shorts on opposite trend", "Switches");

		_buyCloseSignal = Param(nameof(BuyCloseSignal), false)
			.SetDisplay("Close Buy on Signal", "Close longs on signal cross", "Switches");

		_sellCloseSignal = Param(nameof(SellCloseSignal), false)
			.SetDisplay("Close Sell on Signal", "Close shorts on signal cross", "Switches");
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
		_prevTrendFisher = 0m;
		_isFirstTrend = true;
		_trendDirection = 0;
		_prevSignalFisher = 0m;
		_prevPrevSignalFisher = 0m;
		_signalCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection(new Unit(TakeProfit, UnitTypes.Point), new Unit(StopLoss, UnitTypes.Point));

		_trendFisher = new FisherTransform { Length = TrendLength };
		_signalFisher = new FisherTransform { Length = SignalLength };

		var trendSub = SubscribeCandles(TrendCandleType);
		trendSub.Bind(ProcessTrend).Start();

		var signalSub = SubscribeCandles(SignalCandleType);
		signalSub.Bind(ProcessSignal).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, signalSub);
			DrawIndicator(area, _signalFisher);
			DrawOwnTrades(area);
		}
	}

	private void ProcessTrend(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished || !_trendFisher.IsFormed)
			return;

		if (_isFirstTrend)
		{
			_prevTrendFisher = value;
			_isFirstTrend = false;
			return;
		}

		_trendDirection = value > _prevTrendFisher ? 1 : value < _prevTrendFisher ? -1 : _trendDirection;
		_prevTrendFisher = value;
	}

	private void ProcessSignal(ICandleMessage candle, decimal value)
	{
		if (candle.State != CandleStates.Finished || !_signalFisher.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_signalCount < 2)
		{
			if (_signalCount == 0)
				_prevSignalFisher = value;
			else
			{
				_prevPrevSignalFisher = _prevSignalFisher;
				_prevSignalFisher = value;
			}

			_signalCount++;
			return;
		}

		var buyClose = BuyCloseSignal && _prevSignalFisher < _prevPrevSignalFisher;
		var sellClose = SellCloseSignal && _prevSignalFisher > _prevPrevSignalFisher;

		if (_trendDirection < 0 && BuyClose)
			buyClose = true;
		else if (_trendDirection > 0 && SellClose)
			sellClose = true;

		if (buyClose && Position > 0)
			SellMarket(Position);

		if (sellClose && Position < 0)
			BuyMarket(-Position);

		if (_trendDirection < 0 && SellOpen && value >= _prevSignalFisher && _prevSignalFisher < _prevPrevSignalFisher && Position <= 0)
			SellMarket(Volume);

		if (_trendDirection > 0 && BuyOpen && value <= _prevSignalFisher && _prevSignalFisher > _prevPrevSignalFisher && Position >= 0)
			BuyMarket(Volume);

		_prevPrevSignalFisher = _prevSignalFisher;
		_prevSignalFisher = value;
	}
}
