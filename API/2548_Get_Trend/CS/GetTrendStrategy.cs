using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Trend-following strategy converted from the MetaTrader "Get trend" expert.
/// </summary>
public class GetTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _m15CandleType;
	private readonly StrategyParam<DataType> _h1CandleType;
	private readonly StrategyParam<int> _maM15Length;
	private readonly StrategyParam<int> _maH1Length;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticSignalLength;
	private readonly StrategyParam<decimal> _thresholdPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<decimal> _tradeVolume;

	private SmoothedMovingAverage _maM15;
	private SmoothedMovingAverage _maH1;
	private StochasticOscillator _stochastic;

	private decimal? _maH1Value;
	private decimal? _lastH1Close;

	private decimal? _prevStochFast;
	private decimal? _prevStochSlow;

	private decimal? _entryPrice;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="GetTrendStrategy"/> class.
	/// </summary>
	public GetTrendStrategy()
	{
		_m15CandleType = Param(nameof(M15CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("M15 Candle", "Primary timeframe", "General");

		_h1CandleType = Param(nameof(H1CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("H1 Candle", "Higher timeframe", "General");

		_maM15Length = Param(nameof(MaM15Length), 99)
			.SetGreaterThanZero()
			.SetDisplay("M15 MA Length", "Smoothed MA length on M15", "Indicators")
			.SetCanOptimize(true);

		_maH1Length = Param(nameof(MaH1Length), 184)
			.SetGreaterThanZero()
			.SetDisplay("H1 MA Length", "Smoothed MA length on H1", "Indicators")
			.SetCanOptimize(true);

		_stochasticLength = Param(nameof(StochasticLength), 27)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K period", "Indicators")
			.SetCanOptimize(true);

		_stochasticSignalLength = Param(nameof(StochasticSignalLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing period", "Indicators");

		_thresholdPoints = Param(nameof(ThresholdPoints), 10m)
			.SetGreaterThanZero()
			.SetDisplay("Price Threshold", "Maximum distance from MA", "Filters")
			.SetCanOptimize(true);

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 540m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit", "Take profit distance", "Risk")
			.SetCanOptimize(true);

		_stopLossPoints = Param(nameof(StopLossPoints), 90m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss", "Stop loss distance", "Risk")
			.SetCanOptimize(true);

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 20m)
			.SetDisplay("Trailing Stop", "Trailing stop distance", "Risk")
			.SetCanOptimize(true);

		_tradeVolume = Param(nameof(TradeVolume), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Trade Volume", "Order volume", "Risk");
	}

	/// <summary>
	/// Primary trading timeframe (M15 by default).
	/// </summary>
	public DataType M15CandleType
	{
		get => _m15CandleType.Value;
		set => _m15CandleType.Value = value;
	}

	/// <summary>
	/// Confirmation timeframe (H1 by default).
	/// </summary>
	public DataType H1CandleType
	{
		get => _h1CandleType.Value;
		set => _h1CandleType.Value = value;
	}

	/// <summary>
	/// Smoothed moving average length on the 15-minute chart.
	/// </summary>
	public int MaM15Length
	{
		get => _maM15Length.Value;
		set => _maM15Length.Value = value;
	}

	/// <summary>
	/// Smoothed moving average length on the hourly chart.
	/// </summary>
	public int MaH1Length
	{
		get => _maH1Length.Value;
		set => _maH1Length.Value = value;
	}

	/// <summary>
	/// %K period of the stochastic oscillator.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// %D period of the stochastic oscillator.
	/// </summary>
	public int StochasticSignalLength
	{
		get => _stochasticSignalLength.Value;
		set => _stochasticSignalLength.Value = value;
	}

	/// <summary>
	/// Maximum allowed distance between price and the M15 moving average in points.
	/// </summary>
	public decimal ThresholdPoints
	{
		get => _thresholdPoints.Value;
		set => _thresholdPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in points.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set => _tradeVolume.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, M15CandleType), (Security, H1CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_maH1Value = null;
		_lastH1Close = null;
		_prevStochFast = null;
		_prevStochSlow = null;
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = TradeVolume;

		_maM15 = new SmoothedMovingAverage { Length = MaM15Length };
		_maH1 = new SmoothedMovingAverage { Length = MaH1Length };
		_stochastic = new StochasticOscillator
		{
			K = { Length = StochasticLength },
			D = { Length = StochasticSignalLength },
		};

		// Subscribe to 15-minute candles and bind the required indicators.
		var m15Subscription = SubscribeCandles(M15CandleType);
		m15Subscription
			.BindEx(_maM15, _stochastic, ProcessM15Candle)
			.Start();

		// Subscribe to hourly candles for trend confirmation.
		var h1Subscription = SubscribeCandles(H1CandleType);
		h1Subscription
			.Bind(_maH1, ProcessH1Candle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, m15Subscription);
			DrawIndicator(priceArea, _maM15);
			DrawOwnTrades(priceArea);

			var oscillatorArea = CreateChartArea("Stochastic");
			if (oscillatorArea != null)
			{
				DrawIndicator(oscillatorArea, _stochastic);
			}
		}
	}

	private void ProcessH1Candle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Store the latest H1 moving average and close for trend checks.
		_maH1Value = maValue;
		_lastH1Close = candle.ClosePrice;
	}

	private void ProcessM15Candle(ICandleMessage candle, IIndicatorValue maValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!maValue.IsFinal || !stochasticValue.IsFinal)
			return;

		var ma = maValue.ToDecimal();
		var stochTyped = (StochasticOscillatorValue)stochasticValue;

		if (stochTyped.K is not decimal stochFast || stochTyped.D is not decimal stochSlow)
			return;

		// Manage protective levels before looking for new entries.
		ManageOpenPosition(candle);

		if (!IsFormedAndOnlineAndAllowTrading())
			goto UpdateStochastic;

		if (_maH1Value is not decimal maH1 || _lastH1Close is not decimal priceH1)
			goto UpdateStochastic;

		var priceM15 = candle.ClosePrice;
		var priceStep = Security?.PriceStep ?? 1m;
		var threshold = ThresholdPoints * priceStep;

		var nearLowerBand = priceM15 < ma && priceH1 < maH1 && ma - priceM15 <= threshold;
		var nearUpperBand = priceM15 > ma && priceH1 > maH1 && priceM15 - ma <= threshold;

		var crossUp = _prevStochFast is decimal prevFastUp && _prevStochSlow is decimal prevSlowUp && prevFastUp < prevSlowUp && stochFast > stochSlow;
		var crossDown = _prevStochFast is decimal prevFastDown && _prevStochSlow is decimal prevSlowDown && prevFastDown > prevSlowDown && stochFast < stochSlow;

		if (nearLowerBand && stochSlow < 20m && stochFast < 20m && crossUp && Position <= 0)
		{
			EnterLong(candle.ClosePrice, priceStep);
		}
		else if (nearUpperBand && stochSlow > 80m && stochFast > 80m && crossDown && Position >= 0)
		{
			EnterShort(candle.ClosePrice, priceStep);
		}

	UpdateStochastic:
		_prevStochFast = stochFast;
		_prevStochSlow = stochSlow;
	}

	private void EnterLong(decimal entryPrice, decimal priceStep)
	{
		// Cancel opposite orders and flip the position if needed.
		CancelActiveOrders();

		var volume = TradeVolume + Math.Max(0m, -Position);
		BuyMarket(volume);

		_entryPrice = entryPrice;
		_takePrice = entryPrice + TakeProfitPoints * priceStep;
		_stopPrice = entryPrice - StopLossPoints * priceStep;
	}

	private void EnterShort(decimal entryPrice, decimal priceStep)
	{
		// Cancel opposite orders and flip the position if needed.
		CancelActiveOrders();

		var volume = TradeVolume + Math.Max(0m, Position);
		SellMarket(volume);

		_entryPrice = entryPrice;
		_takePrice = entryPrice - TakeProfitPoints * priceStep;
		_stopPrice = entryPrice + StopLossPoints * priceStep;
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
			return;

		var priceStep = Security?.PriceStep ?? 1m;
		var trailingDistance = TrailingStopPoints * priceStep;

		if (Position > 0)
		{
			if (_entryPrice is decimal entry && TrailingStopPoints > 0 && candle.ClosePrice - entry >= trailingDistance)
			{
				var candidate = candle.ClosePrice - trailingDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Max(_stopPrice.Value, candidate) : candidate;
			}

			if (_takePrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Position);
				ResetProtection();
				return;
			}

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Position);
				ResetProtection();
			}
		}
		else if (Position < 0)
		{
			if (_entryPrice is decimal entry && TrailingStopPoints > 0 && entry - candle.ClosePrice >= trailingDistance)
			{
				var candidate = candle.ClosePrice + trailingDistance;
				_stopPrice = _stopPrice.HasValue ? Math.Min(_stopPrice.Value, candidate) : candidate;
			}

			if (_takePrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
				return;
			}

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetProtection();
			}
		}
	}

	private void ResetProtection()
	{
		_entryPrice = null;
		_stopPrice = null;
		_takePrice = null;
	}
}
