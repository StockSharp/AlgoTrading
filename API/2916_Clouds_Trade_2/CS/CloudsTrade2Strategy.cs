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
/// Port of the "cloud's trade 2" MQL5 strategy that combines stochastic reversals with fractal confirmations.
/// </summary>
public class CloudsTrade2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<decimal> _trailingStepOffset;
	private readonly StrategyParam<decimal> _minProfitCurrency;
	private readonly StrategyParam<decimal> _minProfitPoints;
	private readonly StrategyParam<bool> _useFractals;
	private readonly StrategyParam<bool> _useStochastic;
	private readonly StrategyParam<bool> _oneTradePerDay;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowingPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;

	private decimal _priorK;
	private decimal _priorD;
	private decimal _lastK;
	private decimal _lastD;
	private bool _hasPriorStoch;
	private bool _hasLastStoch;

	private decimal _h1;
	private decimal _h2;
	private decimal _h3;
	private decimal _h4;
	private decimal _h5;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;
	private decimal _l4;
	private decimal _l5;
	private FractalType? _latestFractal;
	private FractalType? _previousFractal;
	private int _fractalBufferCount;

	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;
	private DateTime? _lastEntryDate;

	private enum FractalType
	{
		Up,
		Down
	}

	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	public decimal TrailingStepOffset
	{
		get => _trailingStepOffset.Value;
		set => _trailingStepOffset.Value = value;
	}

	public decimal MinProfitCurrency
	{
		get => _minProfitCurrency.Value;
		set => _minProfitCurrency.Value = value;
	}

	public decimal MinProfitPoints
	{
		get => _minProfitPoints.Value;
		set => _minProfitPoints.Value = value;
	}

	public bool UseFractals
	{
		get => _useFractals.Value;
		set => _useFractals.Value = value;
	}

	public bool UseStochastic
	{
		get => _useStochastic.Value;
		set => _useStochastic.Value = value;
	}

	public bool OneTradePerDay
	{
		get => _oneTradePerDay.Value;
		set => _oneTradePerDay.Value = value;
	}

	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	public int SlowingPeriod
	{
		get => _slowingPeriod.Value;
		set => _slowingPeriod.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public CloudsTrade2Strategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
		.SetGreaterThanZero()
		.SetDisplay("Order Volume", "Default order volume", "General");

		_stopLossOffset = Param(nameof(StopLossOffset), 0.005m)
		.SetDisplay("Stop Loss Offset", "Stop loss distance in price units", "Risk");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0.005m)
		.SetDisplay("Take Profit Offset", "Take profit distance in price units", "Risk");

		_trailingStopOffset = Param(nameof(TrailingStopOffset), 0m)
		.SetDisplay("Trailing Stop Offset", "Trailing stop distance in price units", "Risk");

		_trailingStepOffset = Param(nameof(TrailingStepOffset), 0.0005m)
		.SetDisplay("Trailing Step", "Minimum price improvement for trailing", "Risk");

		_minProfitCurrency = Param(nameof(MinProfitCurrency), 10m)
		.SetDisplay("Min Profit (Currency)", "Close position when unrealized profit reaches this amount", "Exit");

		_minProfitPoints = Param(nameof(MinProfitPoints), 0.001m)
		.SetDisplay("Min Profit (Points)", "Close position after this favorable price move", "Exit");

		_useFractals = Param(nameof(UseFractals), true)
		.SetDisplay("Use Fractals", "Enable fractal based signals", "Signals");

		_useStochastic = Param(nameof(UseStochastic), true)
		.SetDisplay("Use Stochastic", "Enable stochastic based signals", "Signals");

		_oneTradePerDay = Param(nameof(OneTradePerDay), true)
		.SetDisplay("One Trade Per Day", "Allow only one entry per trading day", "Risk");

		_kPeriod = Param(nameof(KPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("%K Period", "Lookback for stochastic calculation", "Stochastic");

		_dPeriod = Param(nameof(DPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("%D Period", "Smoothing length for %D line", "Stochastic");

		_slowingPeriod = Param(nameof(SlowingPeriod), 3)
		.SetGreaterThanZero()
		.SetDisplay("Slowing", "Smoothing length for %K line", "Stochastic");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for signal evaluation", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_priorK = 0m;
		_priorD = 0m;
		_lastK = 0m;
		_lastD = 0m;
		_hasPriorStoch = false;
		_hasLastStoch = false;

		_h1 = _h2 = _h3 = _h4 = _h5 = 0m;
		_l1 = _l2 = _l3 = _l4 = _l5 = 0m;
		_latestFractal = null;
		_previousFractal = null;
		_fractalBufferCount = 0;

		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_lastEntryDate = null;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = OrderVolume;

		_stochastic = new StochasticOscillator
		{
			Length = KPeriod,
			K = { Length = SlowingPeriod },
			D = { Length = DPeriod }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(_stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		Volume = OrderVolume;

		// Evaluate indicator signals for the finished candle
		var stochSignal = EvaluateStochasticSignal(stochValue);
		UpdateFractals(candle);
		var fractalSignal = GetFractalSignal();

		HandleOpenPosition(candle);

		var signal = 0;
		if (stochSignal == 2 || fractalSignal == 2)
		signal = 2;
		else if (stochSignal == 1 || fractalSignal == 1)
		signal = 1;

		if (signal == 0)
		return;

		if (Position != 0)
		return;

		if (OneTradePerDay && _lastEntryDate.HasValue && _lastEntryDate.Value == candle.OpenTime.Date)
		return;

		if (signal == 1)
		{
			BuyMarket(OrderVolume);
			InitializeTargets(candle.ClosePrice, true);
			_lastEntryDate = candle.OpenTime.Date;
		}
		else if (signal == 2)
		{
			SellMarket(OrderVolume);
			InitializeTargets(candle.ClosePrice, false);
			_lastEntryDate = candle.OpenTime.Date;
		}
	}

	private int EvaluateStochasticSignal(IIndicatorValue stochValue)
	{
		if (!UseStochastic || stochValue is not StochasticOscillatorValue typed)
		return 0;

		if (typed.K is not decimal currentK || typed.D is not decimal currentD)
		return 0;

		// Seed the buffers with the first finalized stochastic values
		if (!_hasLastStoch)
		{
			_lastK = currentK;
			_lastD = currentD;
			_hasLastStoch = true;
			return 0;
		}

		if (!_hasPriorStoch)
		{
			_priorK = _lastK;
			_priorD = _lastD;
			_lastK = currentK;
			_lastD = currentD;
			_hasPriorStoch = true;
			return 0;
		}

		var sellSignal = _lastD >= 80m && _priorD <= _priorK && _lastD >= _lastK;
		var buySignal = _lastD <= 20m && _priorD >= _priorK && _lastD <= _lastK;

		_priorK = _lastK;
		_priorD = _lastD;
		_lastK = currentK;
		_lastD = currentD;

		if (sellSignal)
		return 2;

		if (buySignal)
		return 1;

		return 0;
	}

	private void UpdateFractals(ICandleMessage candle)
	{
		// Shift the rolling window so that index 3 represents the potential fractal point
		_h1 = _h2;
		_h2 = _h3;
		_h3 = _h4;
		_h4 = _h5;
		_h5 = candle.HighPrice;

		_l1 = _l2;
		_l2 = _l3;
		_l3 = _l4;
		_l4 = _l5;
		_l5 = candle.LowPrice;

		if (_fractalBufferCount < 5)
		{
			_fractalBufferCount++;
			return;
		}

		var upFractal = _h3 > _h1 && _h3 > _h2 && _h3 > _h4 && _h3 > _h5;
		var downFractal = _l3 < _l1 && _l3 < _l2 && _l3 < _l4 && _l3 < _l5;

		if (upFractal)
		RegisterFractal(FractalType.Up);

		if (downFractal)
		RegisterFractal(FractalType.Down);
	}

	private int GetFractalSignal()
	{
		if (!UseFractals)
		return 0;

		if (_latestFractal is null || _previousFractal is null)
		return 0;

		if (_latestFractal == FractalType.Up && _previousFractal == FractalType.Up)
		return 2;

		if (_latestFractal == FractalType.Down && _previousFractal == FractalType.Down)
		return 1;

		return 0;
	}

	private void RegisterFractal(FractalType type)
	{
		_previousFractal = _latestFractal;
		_latestFractal = type;
	}

	private void HandleOpenPosition(ICandleMessage candle)
	{
		if (Position == 0)
		return;

		if (Position > 0)
		{
			// Manage protective logic for long positions
			UpdateTrailing(candle, true);

			if (_stopPrice is decimal stop && candle.LowPrice <= stop)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			var profit = (candle.ClosePrice - _entryPrice) * Position;
			var priceGain = candle.ClosePrice - _entryPrice;

			if (MinProfitCurrency > 0m && profit >= MinProfitCurrency)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (MinProfitPoints > 0m && priceGain >= MinProfitPoints)
			{
				SellMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
		else if (Position < 0)
		{
			// Manage protective logic for short positions
			UpdateTrailing(candle, false);

			if (_stopPrice is decimal stop && candle.HighPrice >= stop)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			var profit = (_entryPrice - candle.ClosePrice) * Math.Abs(Position);
			var priceGain = _entryPrice - candle.ClosePrice;

			if (MinProfitCurrency > 0m && profit >= MinProfitCurrency)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
				return;
			}

			if (MinProfitPoints > 0m && priceGain >= MinProfitPoints)
			{
				BuyMarket(Math.Abs(Position));
				ResetTradeState();
			}
		}
	}

	private void UpdateTrailing(ICandleMessage candle, bool isLong)
	{
		// Follow the original trailing stop rules using configurable offsets
		if (TrailingStopOffset <= 0m)
		return;

		if (isLong)
		{
			var profitDistance = candle.ClosePrice - _entryPrice;
			if (profitDistance > TrailingStopOffset + TrailingStepOffset)
			{
				var newStop = candle.ClosePrice - TrailingStopOffset;
				if (_stopPrice is not decimal currentStop || newStop > currentStop + TrailingStepOffset)
				_stopPrice = newStop;
			}
		}
		else
		{
			var profitDistance = _entryPrice - candle.ClosePrice;
			if (profitDistance > TrailingStopOffset + TrailingStepOffset)
			{
				var newStop = candle.ClosePrice + TrailingStopOffset;
				if (_stopPrice is not decimal currentStop || newStop < currentStop - TrailingStepOffset)
				_stopPrice = newStop;
			}
		}
	}

	private void InitializeTargets(decimal entryPrice, bool isLong)
	{
		// Store the latest entry price and prepare static protective levels
		_entryPrice = entryPrice;

		if (isLong)
		{
			_stopPrice = StopLossOffset > 0m ? entryPrice - StopLossOffset : null;
			_takeProfitPrice = TakeProfitOffset > 0m ? entryPrice + TakeProfitOffset : null;
		}
		else
		{
			_stopPrice = StopLossOffset > 0m ? entryPrice + StopLossOffset : null;
			_takeProfitPrice = TakeProfitOffset > 0m ? entryPrice - TakeProfitOffset : null;
		}
	}

	private void ResetTradeState()
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
	}
}