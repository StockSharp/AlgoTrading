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
/// Suffic369 breakout strategy based on moving averages and Bollinger Bands.
/// </summary>
public class Suffic369Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _highMaLength;
	private readonly StrategyParam<int> _lowMaLength;
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<bool> _useStopLoss;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<bool> _useTakeProfit;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<bool> _useTrailingStop;
	private readonly StrategyParam<decimal> _trailingStopPoints;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _fastCloseSma;
	private SimpleMovingAverage _highPriceSma;
	private SimpleMovingAverage _lowPriceSma;
	private BollingerBands _bollinger;

	private decimal? _previousFastClose;
	private decimal? _previousHighMa;
	private decimal? _previousLowMa;
	private bool _hasPreviousValues;

	private decimal _priceStep;
	private decimal? _longEntryPrice;
	private decimal? _shortEntryPrice;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;

	/// <summary>
	/// Fast SMA length applied to closing prices.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// SMA length applied to high prices.
	/// </summary>
	public int HighMaLength
	{
		get => _highMaLength.Value;
		set => _highMaLength.Value = value;
	}

	/// <summary>
	/// SMA length applied to low prices.
	/// </summary>
	public int LowMaLength
	{
		get => _lowMaLength.Value;
		set => _lowMaLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation multiplier.
	/// </summary>
	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
	}

	/// <summary>
	/// Enable stop-loss handling.
	/// </summary>
	public bool UseStopLoss
	{
		get => _useStopLoss.Value;
		set => _useStopLoss.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Enable take-profit handling.
	/// </summary>
	public bool UseTakeProfit
	{
		get => _useTakeProfit.Value;
		set => _useTakeProfit.Value = value;
	}

	/// <summary>
	/// Take-profit distance in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Enable trailing stop management.
	/// </summary>
	public bool UseTrailingStop
	{
		get => _useTrailingStop.Value;
		set => _useTrailingStop.Value = value;
	}

	/// <summary>
	/// Trailing stop distance in price steps.
	/// </summary>
	public decimal TrailingStopPoints
	{
		get => _trailingStopPoints.Value;
		set => _trailingStopPoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Suffic369Strategy"/> class.
	/// </summary>
	public Suffic369Strategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast SMA Length", "Period of the SMA built on close prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);

		_highMaLength = Param(nameof(HighMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("High SMA Length", "Period of the SMA built on high prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_lowMaLength = Param(nameof(LowMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Low SMA Length", "Period of the SMA built on low prices", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 12, 1);

		_bollingerLength = Param(nameof(BollingerLength), 156)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Number of candles in the Bollinger Bands window", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50, 250, 10);

		_bollingerDeviation = Param(nameof(BollingerDeviation), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.1m);

		_useStopLoss = Param(nameof(UseStopLoss), true)
			.SetDisplay("Use Stop Loss", "Enable protective stop-loss", "Risk Management");

		_stopLossPoints = Param(nameof(StopLossPoints), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop-loss distance measured in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 80m, 5m);

		_useTakeProfit = Param(nameof(UseTakeProfit), true)
			.SetDisplay("Use Take Profit", "Enable take-profit target", "Risk Management");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit Points", "Take-profit distance measured in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(20m, 150m, 5m);

		_useTrailingStop = Param(nameof(UseTrailingStop), true)
			.SetDisplay("Use Trailing Stop", "Enable trailing stop logic", "Risk Management");

		_trailingStopPoints = Param(nameof(TrailingStopPoints), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Stop Points", "Trailing stop offset measured in price steps", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 100m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle source", "General");
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

		_fastCloseSma = null;
		_highPriceSma = null;
		_lowPriceSma = null;
		_bollinger = null;

		_previousFastClose = null;
		_previousHighMa = null;
		_previousLowMa = null;
		_hasPreviousValues = false;

		_longEntryPrice = null;
		_shortEntryPrice = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		_priceStep = Security?.PriceStep ?? 0m;
		if (_priceStep <= 0m)
			_priceStep = 1m;

		_fastCloseSma = new SimpleMovingAverage { Length = FastMaLength };
		_highPriceSma = new SimpleMovingAverage { Length = HighMaLength };
		_lowPriceSma = new SimpleMovingAverage { Length = LowMaLength };
		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation
		};

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(_bollinger, ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastCloseValue = _fastCloseSma.Process(new CandleIndicatorValue(candle, candle.ClosePrice));
		var highMaValue = _highPriceSma.Process(new CandleIndicatorValue(candle, candle.HighPrice));
		var lowMaValue = _lowPriceSma.Process(new CandleIndicatorValue(candle, candle.LowPrice));

		if (!bollingerValue.IsFinal)
		{
			UpdatePreviousValues(fastCloseValue, highMaValue, lowMaValue);
			return;
		}

		var bands = (BollingerBandsValue)bollingerValue;
		if (bands.UpBand is not decimal upper || bands.LowBand is not decimal lower)
		{
			UpdatePreviousValues(fastCloseValue, highMaValue, lowMaValue);
			return;
		}

		if (!_fastCloseSma.IsFormed || !_highPriceSma.IsFormed || !_lowPriceSma.IsFormed)
		{
			UpdatePreviousValues(fastCloseValue, highMaValue, lowMaValue);
			return;
		}

		var fastClose = fastCloseValue.GetValue<decimal>();
		var highMa = highMaValue.GetValue<decimal>();
		var lowMa = lowMaValue.GetValue<decimal>();

		if (!_hasPreviousValues)
		{
			UpdatePreviousValues(fastClose, highMa, lowMa);
			_hasPreviousValues = true;
			return;
		}

		if (_previousFastClose is null || _previousHighMa is null || _previousLowMa is null)
		{
			UpdatePreviousValues(fastClose, highMa, lowMa);
			return;
		}

		var longSignal = _previousFastClose.Value < _previousHighMa.Value && fastClose > highMa && candle.ClosePrice < lower;
		var shortSignal = _previousFastClose.Value > _previousLowMa.Value && fastClose < lowMa && candle.ClosePrice > upper;

		var exitTriggered = false;

		if (Position > 0)
		{
			if (shortSignal)
			{
				SellMarket(Position);
				ResetLongState();
				exitTriggered = true;
			}
			else
			{
				exitTriggered = HandleLongRisk(candle);
			}
		}
		else if (Position < 0)
		{
			if (longSignal)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				exitTriggered = true;
			}
			else
			{
				exitTriggered = HandleShortRisk(candle);
			}
		}

		if (exitTriggered)
		{
			UpdatePreviousValues(fastClose, highMa, lowMa);
			return;
		}

		if (Position == 0 && IsFormedAndOnlineAndAllowTrading())
		{
			if (longSignal)
			{
				BuyMarket(Volume);
				_longEntryPrice = candle.ClosePrice;
				_longTrailingStop = null;
				_shortEntryPrice = null;
				_shortTrailingStop = null;
			}
			else if (shortSignal)
			{
				SellMarket(Volume);
				_shortEntryPrice = candle.ClosePrice;
				_shortTrailingStop = null;
				_longEntryPrice = null;
				_longTrailingStop = null;
			}
		}
		else if (Position > 0)
		{
			_longEntryPrice ??= candle.ClosePrice;
		}
		else if (Position < 0)
		{
			_shortEntryPrice ??= candle.ClosePrice;
		}

		UpdatePreviousValues(fastClose, highMa, lowMa);
	}

	private bool HandleLongRisk(ICandleMessage candle)
	{
		if (!_longEntryPrice.HasValue)
			_longEntryPrice = candle.ClosePrice;

		var entryPrice = _longEntryPrice.Value;
		var step = _priceStep;

		if (UseStopLoss && StopLossPoints > 0m && step > 0m)
		{
			var stopPrice = entryPrice - StopLossPoints * step;
			if (candle.LowPrice <= stopPrice)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}

		if (UseTakeProfit && TakeProfitPoints > 0m && step > 0m)
		{
			var targetPrice = entryPrice + TakeProfitPoints * step;
			if (candle.HighPrice >= targetPrice)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}

		if (UseTrailingStop && TrailingStopPoints > 0m && step > 0m)
		{
			var distance = TrailingStopPoints * step;
			if (candle.ClosePrice - entryPrice > distance)
			{
				var candidate = candle.ClosePrice - distance;
				if (!_longTrailingStop.HasValue || candidate > _longTrailingStop.Value)
					_longTrailingStop = candidate;
			}

			if (_longTrailingStop.HasValue && candle.LowPrice <= _longTrailingStop.Value)
			{
				SellMarket(Position);
				ResetLongState();
				return true;
			}
		}

		return false;
	}

	private bool HandleShortRisk(ICandleMessage candle)
	{
		if (!_shortEntryPrice.HasValue)
			_shortEntryPrice = candle.ClosePrice;

		var entryPrice = _shortEntryPrice.Value;
		var step = _priceStep;

		if (UseStopLoss && StopLossPoints > 0m && step > 0m)
		{
			var stopPrice = entryPrice + StopLossPoints * step;
			if (candle.HighPrice >= stopPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		if (UseTakeProfit && TakeProfitPoints > 0m && step > 0m)
		{
			var targetPrice = entryPrice - TakeProfitPoints * step;
			if (candle.LowPrice <= targetPrice)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		if (UseTrailingStop && TrailingStopPoints > 0m && step > 0m)
		{
			var distance = TrailingStopPoints * step;
			if (entryPrice - candle.ClosePrice > distance)
			{
				var candidate = candle.ClosePrice + distance;
				if (!_shortTrailingStop.HasValue || candidate < _shortTrailingStop.Value)
					_shortTrailingStop = candidate;
			}

			if (_shortTrailingStop.HasValue && candle.HighPrice >= _shortTrailingStop.Value)
			{
				BuyMarket(Math.Abs(Position));
				ResetShortState();
				return true;
			}
		}

		return false;
	}

	private void ResetLongState()
	{
		_longEntryPrice = null;
		_longTrailingStop = null;
	}

	private void ResetShortState()
	{
		_shortEntryPrice = null;
		_shortTrailingStop = null;
	}

	private void UpdatePreviousValues(IIndicatorValue fastValue, IIndicatorValue highValue, IIndicatorValue lowValue)
	{
		if (fastValue.IsFinal)
			_previousFastClose = fastValue.GetValue<decimal>();

		if (highValue.IsFinal)
			_previousHighMa = highValue.GetValue<decimal>();

		if (lowValue.IsFinal)
			_previousLowMa = lowValue.GetValue<decimal>();
	}

	private void UpdatePreviousValues(decimal fastValue, decimal highValue, decimal lowValue)
	{
		_previousFastClose = fastValue;
		_previousHighMa = highValue;
		_previousLowMa = lowValue;
	}
}