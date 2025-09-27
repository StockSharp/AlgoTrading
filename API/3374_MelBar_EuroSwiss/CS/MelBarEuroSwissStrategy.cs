using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MelBar EuroSwiss breakout strategy using Bollinger Bands and RVI exit filter.
/// Translated from the original MQL5 expert advisor.
/// </summary>
public class MelBarEuroSwissStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolumeParam;
	private readonly StrategyParam<int> _bollingerPeriodParam;
	private readonly StrategyParam<decimal> _bollingerDeviationParam;
	private readonly StrategyParam<int> _rviPeriodParam;
	private readonly StrategyParam<decimal> _rviLevelParam;
	private readonly StrategyParam<decimal> _stopLossPipsParam;
	private readonly StrategyParam<decimal> _takeProfitPipsParam;
	private readonly StrategyParam<decimal> _pipSizeParam;
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<decimal> _epsilonParam;

	private BollingerBands _bollinger = null!;
	private RelativeVigorIndex _rvi = null!;

	private decimal? _previousOpen;
	private decimal? _previousPreviousOpen;
	private decimal? _previousUpperBand;
	private decimal? _previousPreviousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _previousPreviousLowerBand;
	private decimal? _previousRvi;
	private decimal? _previousPreviousRvi;
	private decimal? _previousPreviousPreviousRvi;

	/// <summary>
	/// Initializes a new instance of the <see cref="MelBarEuroSwissStrategy"/> class.
	/// </summary>
	public MelBarEuroSwissStrategy()
	{
		_tradeVolumeParam = Param(nameof(TradeVolume), 0.2m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Default trade volume per entry", "Trading")
		.SetCanOptimize(true)
		.SetOptimize(0.05m, 0.5m, 0.05m);

		_bollingerPeriodParam = Param(nameof(BollingerPeriod), 18)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Period", "Number of candles for Bollinger Bands", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 30, 2);

		_bollingerDeviationParam = Param(nameof(BollingerDeviation), 2.75m)
		.SetGreaterThanZero()
		.SetDisplay("Bollinger Deviation", "Standard deviation multiplier", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1.5m, 3.5m, 0.25m);

		_rviPeriodParam = Param(nameof(RviPeriod), 15)
		.SetGreaterThanZero()
		.SetDisplay("RVI Period", "Length for Relative Vigor Index", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10, 25, 1);

		_rviLevelParam = Param(nameof(RviLevel), 0.3m)
		.SetGreaterThanZero()
		.SetDisplay("RVI Level", "Threshold for closing positions", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(0.1m, 0.6m, 0.05m);

		_stopLossPipsParam = Param(nameof(StopLossPips), 13m)
		.SetRange(0m, decimal.MaxValue)
		.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(5m, 30m, 1m);

		_takeProfitPipsParam = Param(nameof(TakeProfitPips), 61m)
		.SetRange(0m, decimal.MaxValue)
		.SetDisplay("Take Profit (pips)", "Target distance in pips", "Risk")
		.SetCanOptimize(true)
		.SetOptimize(30m, 120m, 5m);

		_pipSizeParam = Param(nameof(PipSize), 0.0001m)
		.SetGreaterThanZero()
		.SetDisplay("Pip Size", "Value of one pip in price terms", "Risk");

		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
		.SetDisplay("Candle Type", "Candle type used for calculations", "Common");
	}

	/// <summary>
	/// Trade volume in lots.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolumeParam.Value;
		set => _tradeVolumeParam.Value = value;
	}

	/// <summary>
	/// Bollinger Bands averaging period.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriodParam.Value;
		set => _bollingerPeriodParam.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviationParam.Value;
		set => _bollingerDeviationParam.Value = value;
	}

	/// <summary>
	/// Relative Vigor Index calculation period.
	/// </summary>
	public int RviPeriod
	{
		get => _rviPeriodParam.Value;
		set => _rviPeriodParam.Value = value;
	}

	/// <summary>
	/// Absolute RVI threshold used to exit trades.
	/// </summary>
	public decimal RviLevel
	{
		get => _rviLevelParam.Value;
		set => _rviLevelParam.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPipsParam.Value;
		set => _stopLossPipsParam.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPipsParam.Value;
		set => _takeProfitPipsParam.Value = value;
	}

	public decimal Epsilon
	{
		get => _epsilonParam.Value;
		set => _epsilonParam.Value = value;
	}

	/// <summary>
	/// Pip size in price units.
	/// </summary>
	public decimal PipSize
	{
		get => _pipSizeParam.Value;
		set => _pipSizeParam.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

		_previousOpen = default;
		_previousPreviousOpen = default;
		_previousUpperBand = default;
		_previousPreviousUpperBand = default;
		_previousLowerBand = default;
		_previousPreviousLowerBand = default;
		_previousRvi = default;
		_previousPreviousRvi = default;
		_previousPreviousPreviousRvi = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Prepare indicators for Bollinger breakout and RVI exit logic.
		_bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		_rvi = new RelativeVigorIndex { Length = RviPeriod };

		// Subscribe to candle updates and bind indicators for automatic processing.
		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_bollinger, _rvi, ProcessCandle)
		.Start();

		// Enable risk management using the configured pip distances.
		StartProtection(
		takeProfit: TakeProfitPips > 0 ? new Unit(TakeProfitPips * PipSize, UnitTypes.Absolute) : default,
		stopLoss: StopLossPips > 0 ? new Unit(StopLossPips * PipSize, UnitTypes.Absolute) : default);

		// Draw indicators and trades on the chart when available.
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawIndicator(area, _rvi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rviValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		if (!bollingerValue.IsFinal || !rviValue.IsFinal)
		return;

		var bollingerBands = (BollingerBandsValue)bollingerValue;

		if (bollingerBands.UpBand is not decimal currentUpperBand)
		return;

		if (bollingerBands.LowBand is not decimal currentLowerBand)
		return;

		var currentRvi = rviValue.ToDecimal();
		var rviForSignal = _previousPreviousPreviousRvi;

		var currentOpen = candle.OpenPrice;
		var previousOpen = _previousOpen;
		var previousPreviousOpen = _previousPreviousOpen;
		var previousUpperBand = _previousUpperBand;
		var previousPreviousUpperBand = _previousPreviousUpperBand;
		var previousLowerBand = _previousLowerBand;
		var previousPreviousLowerBand = _previousPreviousLowerBand;

		if (IsFormedAndOnlineAndAllowTrading())
		{
			// Manage exits using the RVI threshold from earlier candles.
			if (rviForSignal.HasValue)
			{
				if (Position > 0 && rviForSignal.Value > RviLevel + Epsilon)
				{
					SellMarket(Position);
				}
				else if (Position < 0 && rviForSignal.Value < -RviLevel - Epsilon)
				{
					BuyMarket(-Position);
				}
			}

			// Evaluate entry breakouts only when the strategy is flat.
			if (Position == 0 && previousOpen.HasValue && previousPreviousOpen.HasValue &&
			previousUpperBand.HasValue && previousPreviousUpperBand.HasValue &&
			previousLowerBand.HasValue && previousPreviousLowerBand.HasValue)
			{
				var canOpenLong = currentOpen < previousLowerBand.Value - Epsilon &&
				previousOpen.Value > previousPreviousLowerBand.Value + Epsilon;

				var canOpenShort = currentOpen > previousUpperBand.Value + Epsilon &&
				previousOpen.Value < previousPreviousUpperBand.Value - Epsilon;

				if (canOpenLong && !canOpenShort)
				{
					BuyMarket(TradeVolume);
				}
				else if (canOpenShort && !canOpenLong)
				{
					SellMarket(TradeVolume);
				}
			}
		}

		// Update cached values for the next signal evaluation.
		_previousPreviousOpen = previousOpen;
		_previousOpen = currentOpen;

		_previousPreviousUpperBand = previousUpperBand;
		_previousUpperBand = currentUpperBand;

		_previousPreviousLowerBand = previousLowerBand;
		_previousLowerBand = currentLowerBand;

		_previousPreviousPreviousRvi = _previousPreviousRvi;
		_previousPreviousRvi = _previousRvi;
		_previousRvi = currentRvi;
	}
}
