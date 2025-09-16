using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the "EA Stochastic" MetaTrader strategy using StockSharp high level API.
/// </summary>
public class EaStochasticStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _trailingStopPips;
	private readonly StrategyParam<int> _trailingStepPips;
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowing;
	private readonly StrategyParam<decimal> _upperLevel;
	private readonly StrategyParam<decimal> _lowerLevel;
	private readonly StrategyParam<int> _comparedBar;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;
	private decimal?[] _stochasticBuffer;
	private int _bufferIndex;
	private int _valuesStored;

	private decimal _pipSize;
	private decimal? _longStopPrice;
	private decimal? _longTakePrice;
	private decimal? _shortStopPrice;
	private decimal? _shortTakePrice;
	private bool _longExitRequested;
	private bool _shortExitRequested;

	/// <summary>
	/// Stop loss distance expressed in pips.
	/// </summary>
	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in pips.
	/// </summary>
	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in pips.
	/// </summary>
	public int TrailingStopPips
	{
		get => _trailingStopPips.Value;
		set => _trailingStopPips.Value = value;
	}

	/// <summary>
	/// Trailing step distance expressed in pips.
	/// </summary>
	public int TrailingStepPips
	{
		get => _trailingStepPips.Value;
		set => _trailingStepPips.Value = value;
	}

	/// <summary>
	/// Trade volume used for market orders.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// %K period of the stochastic oscillator.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// %D period of the stochastic oscillator.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Final smoothing applied to stochastic values.
	/// </summary>
	public int Slowing
	{
		get => _slowing.Value;
		set => _slowing.Value = value;
	}

	/// <summary>
	/// Upper stochastic threshold used for long filtering.
	/// </summary>
	public decimal UpperLevel
	{
		get => _upperLevel.Value;
		set => _upperLevel.Value = value;
	}

	/// <summary>
	/// Lower stochastic threshold used for short filtering.
	/// </summary>
	public decimal LowerLevel
	{
		get => _lowerLevel.Value;
		set => _lowerLevel.Value = value;
	}

	/// <summary>
	/// How many bars back to compare with the current stochastic value.
	/// </summary>
	public int ComparedBar
	{
		get => _comparedBar.Value;
		set => _comparedBar.Value = value;
	}

	/// <summary>
	/// Candle type processed by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="EaStochasticStrategy"/>.
	/// </summary>
	public EaStochasticStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 50)
			.SetDisplay("Stop Loss", "Stop loss distance in pips", "Risk")
			.SetRange(0, 1000);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			.SetDisplay("Take Profit", "Take profit distance in pips", "Risk")
			.SetRange(0, 2000);

		_trailingStopPips = Param(nameof(TrailingStopPips), 15)
			.SetDisplay("Trailing Stop", "Trailing stop distance in pips", "Risk")
			.SetRange(0, 1000);

		_trailingStepPips = Param(nameof(TrailingStepPips), 5)
			.SetDisplay("Trailing Step", "Trailing step distance in pips", "Risk")
			.SetRange(0, 500);

		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Order volume", "Orders");

		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "%K calculation period", "Indicators")
			.SetCanOptimize(true);

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "%D smoothing period", "Indicators")
			.SetCanOptimize(true);

		_slowing = Param(nameof(Slowing), 3)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Final smoothing period", "Indicators")
			.SetCanOptimize(true);

		_upperLevel = Param(nameof(UpperLevel), 80m)
			.SetDisplay("Upper Level", "Upper stochastic threshold", "Signals")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_lowerLevel = Param(nameof(LowerLevel), 20m)
			.SetDisplay("Lower Level", "Lower stochastic threshold", "Signals")
			.SetRange(0m, 100m)
			.SetCanOptimize(true);

		_comparedBar = Param(nameof(ComparedBar), 3)
			.SetGreaterThanZero()
			.SetDisplay("Compared Bar", "Bars back to compare", "Signals")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Candle type to subscribe", "General");
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

		_stochastic = default;
		_stochasticBuffer = default;
		_bufferIndex = 0;
		_valuesStored = 0;
		_pipSize = 0m;
		_longStopPrice = null;
		_longTakePrice = null;
		_shortStopPrice = null;
		_shortTakePrice = null;
		_longExitRequested = false;
		_shortExitRequested = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (TrailingStopPips > 0 && TrailingStepPips <= 0)
			throw new InvalidOperationException("Trailing step must be greater than zero when trailing stop is enabled.");

		_stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
			Slowing = Slowing
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		_pipSize = CalculatePipSize();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		if (Position == 0)
		{
			_longStopPrice = null;
			_longTakePrice = null;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_longExitRequested = false;
			_shortExitRequested = false;
			return;
		}

		var entryPrice = PositionPrice;

		if (Position > 0 && delta > 0)
		{
			_longStopPrice = StopLossPips > 0 ? entryPrice - StopLossPips * _pipSize : null;
			_longTakePrice = TakeProfitPips > 0 ? entryPrice + TakeProfitPips * _pipSize : null;
			_longExitRequested = false;
			_shortStopPrice = null;
			_shortTakePrice = null;
			_shortExitRequested = false;
		}
		else if (Position < 0 && delta < 0)
		{
			_shortStopPrice = StopLossPips > 0 ? entryPrice + StopLossPips * _pipSize : null;
			_shortTakePrice = TakeProfitPips > 0 ? entryPrice - TakeProfitPips * _pipSize : null;
			_shortExitRequested = false;
			_longStopPrice = null;
			_longTakePrice = null;
			_longExitRequested = false;
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Manage open trades before evaluating new entries.
		if (Position > 0)
		{
			ManageLong(candle);
		}
		else if (Position < 0)
		{
			ManageShort(candle);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var stochTyped = (StochasticOscillatorValue)stochasticValue;

		if (stochTyped.K is not decimal kValue)
			return;

		EnsureBuffer();

		_stochasticBuffer[_bufferIndex] = kValue;
		_bufferIndex = (_bufferIndex + 1) % _stochasticBuffer.Length;
		if (_valuesStored < _stochasticBuffer.Length)
			_valuesStored++;

		if (_valuesStored < _stochasticBuffer.Length)
			return;

		var currentIndex = (_bufferIndex - 1 + _stochasticBuffer.Length) % _stochasticBuffer.Length;
		var compareIndex = (currentIndex - (ComparedBar - 1) + _stochasticBuffer.Length) % _stochasticBuffer.Length;

		var comparedValue = _stochasticBuffer[compareIndex];
		if (comparedValue is not decimal previousValue)
			return;

		if (Position != 0 || _longExitRequested || _shortExitRequested)
			return;

		var shouldBuy = kValue < UpperLevel && previousValue < UpperLevel;
		var shouldSell = kValue > LowerLevel && previousValue > LowerLevel;

		if (shouldBuy)
		{
			BuyMarket(Volume);
		}
		else if (shouldSell)
		{
			SellMarket(Volume);
		}
	}

	private void ManageLong(ICandleMessage candle)
	{
		// Process take profit using candle high.
		if (_longTakePrice is decimal take && candle.HighPrice >= take)
		{
			TryCloseLong();
			return;
		}

		if (TrailingStopPips > 0)
		{
			var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
			var trailDistance = TrailingStopPips * _pipSize;
			var referencePrice = Math.Max(candle.HighPrice, candle.ClosePrice);

			if (referencePrice - PositionPrice > trailDistance + stepDistance)
			{
				var desiredStop = referencePrice - trailDistance;
				var threshold = stepDistance > 0m ? desiredStop - stepDistance : desiredStop;

				if (_longStopPrice is not decimal currentStop || currentStop < threshold)
				{
					_longStopPrice = desiredStop;
				}
			}
		}

		// Process stop loss using candle low.
		if (_longStopPrice is decimal stop && candle.LowPrice <= stop)
		{
			TryCloseLong();
		}
	}

	private void ManageShort(ICandleMessage candle)
	{
		// Process take profit using candle low.
		if (_shortTakePrice is decimal take && candle.LowPrice <= take)
		{
			TryCloseShort();
			return;
		}

		if (TrailingStopPips > 0)
		{
			var stepDistance = TrailingStepPips > 0 ? TrailingStepPips * _pipSize : 0m;
			var trailDistance = TrailingStopPips * _pipSize;
			var referencePrice = Math.Min(candle.LowPrice, candle.ClosePrice);

			if (PositionPrice - referencePrice > trailDistance + stepDistance)
			{
				var desiredStop = referencePrice + trailDistance;
				var threshold = stepDistance > 0m ? desiredStop + stepDistance : desiredStop;

				if (_shortStopPrice is not decimal currentStop || currentStop > threshold)
				{
					_shortStopPrice = desiredStop;
				}
			}
		}

		// Process stop loss using candle high.
		if (_shortStopPrice is decimal stop && candle.HighPrice >= stop)
		{
			TryCloseShort();
		}
	}

	private void TryCloseLong()
	{
		if (_longExitRequested)
			return;

		_longExitRequested = true;
		SellMarket(Position);
	}

	private void TryCloseShort()
	{
		if (_shortExitRequested)
			return;

		_shortExitRequested = true;
		BuyMarket(Math.Abs(Position));
	}

	private void EnsureBuffer()
	{
		if (_stochasticBuffer != null && _stochasticBuffer.Length == ComparedBar)
			return;

		_stochasticBuffer = new decimal?[ComparedBar];
		_bufferIndex = 0;
		_valuesStored = 0;
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;

		if (step <= 0m)
			return 1m;

		if (step < 0.001m)
			return step * 10m;

		return step;
	}
}
