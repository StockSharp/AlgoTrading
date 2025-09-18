using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert advisor "Aussie Surfer Ltd" to the StockSharp high level API.
/// Bollinger Bands reversals trigger entries while the Alligator teeth slope and pip-based
/// risk controls manage exits.
/// </summary>
public class AussieSurferLtdStrategy : Strategy
{
	private const decimal Tolerance = 1e-6m;

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<bool> _enableTrailingStop;
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<int> _teethPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private SmoothedMovingAverage _teeth = null!;

	private decimal _pipSize;
	private decimal? _previousUpperBand;
	private decimal? _previousLowerBand;
	private decimal? _previous2UpperBand;
	private decimal? _previous2LowerBand;
	private decimal? _previousOpenPrice;
	private decimal? _previous2OpenPrice;
	private decimal? _previousHigh;
	private decimal? _previousLow;
	private decimal? _previousTeeth;
	private decimal? _previous2Teeth;

	private decimal? _longStopPrice;
	private decimal? _longTakeProfit;
	private decimal? _shortStopPrice;
	private decimal? _shortTakeProfit;

	/// <summary>
	/// Trading volume expressed in lots or contracts.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips. Zero disables the protective stop.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips. Zero disables the target.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enable or disable the trailing stop update that follows completed candles.
	/// </summary>
	public bool EnableTrailingStop
	{
		get => _enableTrailingStop.Value;
		set => _enableTrailingStop.Value = value;
	}

	/// <summary>
	/// Period for the Bollinger Bands.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for the Bollinger Bands.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Smoothed moving average period that rebuilds the Alligator teeth line.
	/// </summary>
	public int TeethPeriod
	{
		get => _teethPeriod.Value;
		set => _teethPeriod.Value = value;
	}

	/// <summary>
	/// Candle type used for analysis.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="AussieSurferLtdStrategy"/>.
	/// </summary>
	public AussieSurferLtdStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 0.30m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Base trade size in lots or contracts", "Trading")
			.SetCanOptimize(true)
			.SetOptimize(0.10m, 0.60m, 0.10m);

		_stopLossPips = Param(nameof(StopLossPips), 46)
			.SetGreaterOrEqualThanZero()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(20, 80, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 0)
			.SetGreaterOrEqualThanZero()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(0, 120, 20);

		_enableTrailingStop = Param(nameof(EnableTrailingStop), true)
			.SetDisplay("Enable Trailing", "Follow price with a trailing stop when stop-loss is active", "Risk");

		_bollingerPeriod = Param(nameof(BollingerPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Length of the Bollinger Bands window", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 2.5m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for the bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1.5m, 3.5m, 0.5m);

		_teethPeriod = Param(nameof(TeethPeriod), 21)
			.SetGreaterThanZero()
			.SetDisplay("Teeth Period", "Length of the Alligator teeth smoothed moving average", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(13, 34, 3);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe for the strategy", "General");
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

		_teeth = null!;
		_pipSize = 0m;
		_previousUpperBand = null;
		_previousLowerBand = null;
		_previous2UpperBand = null;
		_previous2LowerBand = null;
		_previousOpenPrice = null;
		_previous2OpenPrice = null;
		_previousHigh = null;
		_previousLow = null;
		_previousTeeth = null;
		_previous2Teeth = null;
		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (EnableTrailingStop && StopLossPips <= 0)
			throw new InvalidOperationException("Trailing stop requires a positive stop-loss distance.");

		_pipSize = Security?.PriceStep ?? throw new InvalidOperationException("Security price step is required to convert pips to price.");

		_teeth = new SmoothedMovingAverage { Length = TeethPeriod };

		Volume = OrderVolume;

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(bollinger, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawIndicator(area, _teeth);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var medianPrice = (candle.HighPrice + candle.LowPrice) / 2m;
		var teethValue = _teeth.Process(medianPrice, candle.OpenTime, true);

		if (!teethValue.IsFinal)
		{
			UpdateReferences(candle, upperBand, lowerBand, null);
			return;
		}

		var teeth = teethValue.ToDecimal();

		ManageOpenPosition(candle);

		if (IsFormedAndOnlineAndAllowTrading())
		{
			EvaluateEntries(candle, upperBand, lowerBand);
		}

		UpdateReferences(candle, upperBand, lowerBand, teeth);
	}

	private void ManageOpenPosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			var shouldCloseByAlligator = _previous2Teeth.HasValue && _previousTeeth.HasValue && _previous2Teeth.Value > _previousTeeth.Value + Tolerance;
			if (shouldCloseByAlligator)
			{
				SellMarket(Position);
				ResetStops();
				return;
			}

			if (_longTakeProfit.HasValue && candle.HighPrice >= _longTakeProfit.Value)
			{
				SellMarket(Position);
				ResetStops();
				return;
			}

			if (EnableTrailingStop && StopLossPips > 0)
				UpdateTrailingForLong();

			if (_longStopPrice.HasValue && candle.LowPrice <= _longStopPrice.Value)
			{
				SellMarket(Position);
				ResetStops();
			}
		}
		else if (Position < 0)
		{
			var shouldCloseByAlligator = _previous2Teeth.HasValue && _previousTeeth.HasValue && _previous2Teeth.Value < _previousTeeth.Value - Tolerance;
			if (shouldCloseByAlligator)
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
				return;
			}

			if (_shortTakeProfit.HasValue && candle.LowPrice <= _shortTakeProfit.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
				return;
			}

			if (EnableTrailingStop && StopLossPips > 0)
				UpdateTrailingForShort();

			if (_shortStopPrice.HasValue && candle.HighPrice >= _shortStopPrice.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetStops();
			}
		}
	}

	private void EvaluateEntries(ICandleMessage candle, decimal upperBand, decimal lowerBand)
	{
		var previousOpen = _previousOpenPrice;
		var previousLower = _previousLowerBand;
		var previousUpper = _previousUpperBand;
		var earlierLower = _previous2LowerBand;
		var earlierUpper = _previous2UpperBand;

		var longSignal = previousOpen.HasValue && previousLower.HasValue && earlierLower.HasValue &&
			previousOpen.Value > previousLower.Value + Tolerance &&
			candle.OpenPrice < earlierLower.Value - Tolerance;

		var shortSignal = previousOpen.HasValue && previousUpper.HasValue && earlierUpper.HasValue &&
			previousOpen.Value < previousUpper.Value - Tolerance &&
			candle.OpenPrice > earlierUpper.Value + Tolerance;

		if (longSignal && Position <= 0)
		{
			var volume = OrderVolume + (Position < 0 ? Math.Abs(Position) : 0m);
			BuyMarket(volume);
			InitializeLongRisk(candle.OpenPrice);
		}
		else if (shortSignal && Position >= 0)
		{
			var volume = OrderVolume + (Position > 0 ? Math.Abs(Position) : 0m);
			SellMarket(volume);
			InitializeShortRisk(candle.OpenPrice);
		}
	}

	private void InitializeLongRisk(decimal entryPrice)
	{
		_longTakeProfit = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : null;
		_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : null;
		_shortTakeProfit = null;
		_shortStopPrice = null;
	}

	private void InitializeShortRisk(decimal entryPrice)
	{
		_shortTakeProfit = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : null;
		_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : null;
		_longTakeProfit = null;
		_longStopPrice = null;
	}

	private void UpdateTrailingForLong()
	{
		if (!_previousHigh.HasValue || !_longStopPrice.HasValue)
			return;

		var candidate = _previousHigh.Value - StopLossPips * _pipSize;
		if (candidate > _longStopPrice.Value + _pipSize)
			_longStopPrice = candidate;
	}

	private void UpdateTrailingForShort()
	{
		if (!_previousLow.HasValue || !_shortStopPrice.HasValue)
			return;

		var candidate = _previousLow.Value + StopLossPips * _pipSize;
		if (candidate < _shortStopPrice.Value - _pipSize)
			_shortStopPrice = candidate;
	}

	private void ResetStops()
	{
		_longStopPrice = null;
		_longTakeProfit = null;
		_shortStopPrice = null;
		_shortTakeProfit = null;
	}

	private void UpdateReferences(ICandleMessage candle, decimal upperBand, decimal lowerBand, decimal? teeth)
	{
		_previous2UpperBand = _previousUpperBand;
		_previousUpperBand = upperBand;

		_previous2LowerBand = _previousLowerBand;
		_previousLowerBand = lowerBand;

		_previous2OpenPrice = _previousOpenPrice;
		_previousOpenPrice = candle.OpenPrice;

		if (teeth.HasValue)
		{
			_previous2Teeth = _previousTeeth;
			_previousTeeth = teeth.Value;
		}

		_previousHigh = candle.HighPrice;
		_previousLow = candle.LowPrice;
	}
}
