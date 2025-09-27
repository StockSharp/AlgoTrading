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
/// Port of the "cloudzs trade 2" MetaTrader 4 expert advisor.
/// Combines stochastic reversals with fractal confirmations and mirrors the original trailing logic.
/// </summary>
public class CloudzsTrade2Strategy : Strategy
{
	private readonly StrategyParam<decimal> _lotSplitter;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _takeProfitOffset;
	private readonly StrategyParam<decimal> _trailingStopOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;
	private readonly StrategyParam<decimal> _minProfitOffset;
	private readonly StrategyParam<decimal> _profitPointsOffset;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<int> _slowingPeriod;
	private readonly StrategyParam<int> _method;
	private readonly StrategyParam<int> _priceMode;
	private readonly StrategyParam<bool> _useStochasticCondition;
	private readonly StrategyParam<bool> _useFractalCondition;
	private readonly StrategyParam<bool> _closeOnOpposite;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;

	private decimal _previousK;
	private decimal _previousD;
	private decimal _lastK;
	private decimal _lastD;
	private bool _hasPrevious;
	private bool _hasLast;

	private decimal _high1;
	private decimal _high2;
	private decimal _high3;
	private decimal _high4;
	private decimal _high5;
	private decimal _low1;
	private decimal _low2;
	private decimal _low3;
	private decimal _low4;
	private decimal _low5;
	private FractalTypes? _latestFractal;
	private FractalTypes? _previousFractal;
	private int _fractalSeedCount;

	private decimal? _stopPrice;
	private decimal? _takeProfitPrice;
	private decimal _entryPrice;
	private decimal _maxFavorableMove;
	private DateTime? _lastExitDate;

	private enum FractalTypes
	{
		Up,
		Down
	}

	/// <summary>
	/// Lot coefficient used to estimate order size from account value.
	/// </summary>
	public decimal LotSplitter
	{
		get => _lotSplitter.Value;
		set => _lotSplitter.Value = value;
	}

	/// <summary>
	/// Maximum allowed order size.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Take profit distance expressed in price units.
	/// </summary>
	public decimal TakeProfitOffset
	{
		get => _takeProfitOffset.Value;
		set => _takeProfitOffset.Value = value;
	}

	/// <summary>
	/// Trailing stop distance expressed in price units.
	/// </summary>
	public decimal TrailingStopOffset
	{
		get => _trailingStopOffset.Value;
		set => _trailingStopOffset.Value = value;
	}

	/// <summary>
	/// Stop loss distance expressed in price units.
	/// </summary>
	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <summary>
	/// Minimum profit required to keep the position open after reaching the <see cref="ProfitPointsOffset"/> threshold.
	/// </summary>
	public decimal MinProfitOffset
	{
		get => _minProfitOffset.Value;
		set => _minProfitOffset.Value = value;
	}

	/// <summary>
	/// Profit cushion that must be reached before <see cref="MinProfitOffset"/> becomes active.
	/// </summary>
	public decimal ProfitPointsOffset
	{
		get => _profitPointsOffset.Value;
		set => _profitPointsOffset.Value = value;
	}

	/// <summary>
	/// Lookback period for the stochastic oscillator.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing period for the %K line.
	/// </summary>
	public int SlowingPeriod
	{
		get => _slowingPeriod.Value;
		set => _slowingPeriod.Value = value;
	}

	/// <summary>
	/// Original stochastic method identifier (kept for reference).
	/// </summary>
	public int Method
	{
		get => _method.Value;
		set => _method.Value = value;
	}

	/// <summary>
	/// Original price mode identifier (kept for reference).
	/// </summary>
	public int PriceMode
	{
		get => _priceMode.Value;
		set => _priceMode.Value = value;
	}

	/// <summary>
	/// Enable stochastic based entry logic.
	/// </summary>
	public bool UseStochasticCondition
	{
		get => _useStochasticCondition.Value;
		set => _useStochasticCondition.Value = value;
	}

	/// <summary>
	/// Enable fractal based entry logic.
	/// </summary>
	public bool UseFractalCondition
	{
		get => _useFractalCondition.Value;
		set => _useFractalCondition.Value = value;
	}

	/// <summary>
	/// Close the active position when the opposite signal appears.
	/// </summary>
	public bool CloseOnOpposite
	{
		get => _closeOnOpposite.Value;
		set => _closeOnOpposite.Value = value;
	}

	/// <summary>
	/// Candle series used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CloudzsTrade2Strategy"/> class.
	/// </summary>
	public CloudzsTrade2Strategy()
	{
		_lotSplitter = Param(nameof(LotSplitter), 0.1m)
			.SetGreaterThan(0m)
			.SetDisplay("Lot Splitter", "Coefficient used to derive order size", "Trading");

		_maxVolume = Param(nameof(MaxVolume), 0m)
			.SetDisplay("Max Volume", "Maximum volume limit (0 disables the cap)", "Trading");

		_takeProfitOffset = Param(nameof(TakeProfitOffset), 0m)
			.SetDisplay("Take Profit", "Take profit distance in price units", "Risk");

		_trailingStopOffset = Param(nameof(TrailingStopOffset), 0.01m)
			.SetDisplay("Trailing Stop", "Trailing stop distance in price units", "Risk");

		_stopLossOffset = Param(nameof(StopLossOffset), 0.05m)
			.SetDisplay("Stop Loss", "Stop loss distance in price units", "Risk");

		_minProfitOffset = Param(nameof(MinProfitOffset), 0m)
			.SetDisplay("Min Profit", "Minimum profit to keep after pullback", "Risk");

		_profitPointsOffset = Param(nameof(ProfitPointsOffset), 0m)
			.SetDisplay("Profit Points", "Favorable move required before min profit rule", "Risk");

		_kPeriod = Param(nameof(KPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Base length of the stochastic oscillator", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Smoothing length for the stochastic signal", "Indicators");

		_slowingPeriod = Param(nameof(SlowingPeriod), 4)
			.SetGreaterThanZero()
			.SetDisplay("Slowing", "Additional smoothing length for %K", "Indicators");

		_method = Param(nameof(Method), 3)
			.SetDisplay("Method", "Original MQL MA method identifier", "Indicators");

		_priceMode = Param(nameof(PriceMode), 1)
			.SetDisplay("Price Mode", "Original MQL price mode identifier", "Indicators");

		_useStochasticCondition = Param(nameof(UseStochasticCondition), true)
			.SetDisplay("Use Stochastic", "Enable stochastic reversal filter", "Signals");

		_useFractalCondition = Param(nameof(UseFractalCondition), true)
			.SetDisplay("Use Fractals", "Enable double fractal confirmation", "Signals");

		_closeOnOpposite = Param(nameof(CloseOnOpposite), true)
			.SetDisplay("Close On Opposite", "Exit when the opposite signal fires", "Risk");

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

		_previousK = 0m;
		_previousD = 0m;
		_lastK = 0m;
		_lastD = 0m;
		_hasPrevious = false;
		_hasLast = false;

		_high1 = _high2 = _high3 = _high4 = _high5 = 0m;
		_low1 = _low2 = _low3 = _low4 = _low5 = 0m;
		_latestFractal = null;
		_previousFractal = null;
		_fractalSeedCount = 0;

		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_maxFavorableMove = 0m;
		_lastExitDate = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

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

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateFractals(candle);

		var stochasticSignal = UseStochasticCondition ? EvaluateStochasticSignal(stochasticValue) : 0;
		var fractalSignal = UseFractalCondition ? EvaluateFractalSignal() : 0;

		var combinedSignal = 0;
		if (stochasticSignal == 2 || fractalSignal == 2)
			combinedSignal = 2;
		else if (stochasticSignal == 1 || fractalSignal == 1)
			combinedSignal = 1;

		ManageOpenPosition(candle, combinedSignal);

		if (Position != 0)
			return;

		if (_lastExitDate.HasValue && _lastExitDate.Value == candle.OpenTime.Date)
			return;

		if (combinedSignal == 0)
			return;

		var volume = CalculateOrderVolume(candle.ClosePrice);
		if (volume <= 0m)
			return;

		Volume = volume;

		if (combinedSignal == 1)
		{
			BuyMarket(volume);
			InitializeTargets(candle.ClosePrice, true);
		}
		else if (combinedSignal == 2)
		{
			SellMarket(volume);
			InitializeTargets(candle.ClosePrice, false);
		}
	}

	private decimal CalculateOrderVolume(decimal price)
	{
		if (price <= 0m)
			return LotSplitter;

		var accountValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		if (accountValue <= 0m)
			accountValue = LotSplitter;

		var estimated = LotSplitter * accountValue / price;
		var normalized = Math.Floor(estimated * 10m) / 10m;

		if (normalized <= 0m)
			normalized = LotSplitter;

		if (MaxVolume > 0m && normalized > MaxVolume)
			normalized = MaxVolume;

		return normalized;
	}

	private int EvaluateStochasticSignal(IIndicatorValue stochasticValue)
	{
		if (_stochastic is null || stochasticValue is not StochasticOscillatorValue typed)
			return 0;

		if (typed.K is not decimal currentK || typed.D is not decimal currentD)
			return 0;

		if (!_hasLast)
		{
			_lastK = currentK;
			_lastD = currentD;
			_hasLast = true;
			return 0;
		}

		if (!_hasPrevious)
		{
			_previousK = _lastK;
			_previousD = _lastD;
			_lastK = currentK;
			_lastD = currentD;
			_hasPrevious = true;
			return 0;
		}

		var sellSignal = _lastD >= 80m && _previousD <= _previousK && _lastD >= _lastK;
		var buySignal = _lastD <= 20m && _previousD >= _previousK && _lastD <= _lastK;

		_previousK = _lastK;
		_previousD = _lastD;
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
		_high1 = _high2;
		_high2 = _high3;
		_high3 = _high4;
		_high4 = _high5;
		_high5 = candle.HighPrice;

		_low1 = _low2;
		_low2 = _low3;
		_low3 = _low4;
		_low4 = _low5;
		_low5 = candle.LowPrice;

		if (_fractalSeedCount < 5)
		{
			_fractalSeedCount++;
			return;
		}

		var upFractal = _high3 > _high1 && _high3 > _high2 && _high3 > _high4 && _high3 > _high5;
		var downFractal = _low3 < _low1 && _low3 < _low2 && _low3 < _low4 && _low3 < _low5;

		if (upFractal)
			RegisterFractal(FractalTypes.Up);

		if (downFractal)
			RegisterFractal(FractalTypes.Down);
	}

	private void RegisterFractal(FractalTypes type)
	{
		_previousFractal = _latestFractal;
		_latestFractal = type;
	}

	private int EvaluateFractalSignal()
	{
		if (_latestFractal is null || _previousFractal is null)
			return 0;

		if (_latestFractal == FractalTypes.Up && _previousFractal == FractalTypes.Up)
			return 2;

		if (_latestFractal == FractalTypes.Down && _previousFractal == FractalTypes.Down)
			return 1;

		return 0;
	}

	private void ManageOpenPosition(ICandleMessage candle, int combinedSignal)
	{
		if (Position == 0)
			return;

		if (Position > 0)
			ManageLongPosition(candle, combinedSignal);
		else
			ManageShortPosition(candle, combinedSignal);
	}

	private void ManageLongPosition(ICandleMessage candle, int combinedSignal)
	{
		UpdateTrailingStop(candle, true);

		if (_stopPrice is decimal stop && candle.LowPrice <= stop)
		{
			SellMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		if (_takeProfitPrice is decimal take && candle.HighPrice >= take)
		{
			SellMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		var currentGain = candle.ClosePrice - _entryPrice;
		var favorable = candle.HighPrice - _entryPrice;
		if (favorable > _maxFavorableMove)
			_maxFavorableMove = favorable;

		if (ProfitPointsOffset > 0m && _maxFavorableMove >= ProfitPointsOffset && currentGain <= MinProfitOffset)
		{
			SellMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		if (CloseOnOpposite && combinedSignal == 2)
		{
			SellMarket(Math.Abs(Position));
			FinalizeExit(candle);
		}
	}

	private void ManageShortPosition(ICandleMessage candle, int combinedSignal)
	{
		UpdateTrailingStop(candle, false);

		if (_stopPrice is decimal stop && candle.HighPrice >= stop)
		{
			BuyMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		if (_takeProfitPrice is decimal take && candle.LowPrice <= take)
		{
			BuyMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		var currentGain = _entryPrice - candle.ClosePrice;
		var favorable = _entryPrice - candle.LowPrice;
		if (favorable > _maxFavorableMove)
			_maxFavorableMove = favorable;

		if (ProfitPointsOffset > 0m && _maxFavorableMove >= ProfitPointsOffset && currentGain <= MinProfitOffset)
		{
			BuyMarket(Math.Abs(Position));
			FinalizeExit(candle);
			return;
		}

		if (CloseOnOpposite && combinedSignal == 1)
		{
			BuyMarket(Math.Abs(Position));
			FinalizeExit(candle);
		}
	}

	private void UpdateTrailingStop(ICandleMessage candle, bool isLong)
	{
		if (TrailingStopOffset <= 0m)
			return;

		if (isLong)
		{
			var potentialStop = candle.ClosePrice - TrailingStopOffset;
			if (_stopPrice is null || potentialStop > _stopPrice)
			{
				if (potentialStop > _entryPrice)
					_stopPrice = potentialStop;
			}
		}
		else
		{
			var potentialStop = candle.ClosePrice + TrailingStopOffset;
			if (_stopPrice is null || potentialStop < _stopPrice)
			{
				if (potentialStop < _entryPrice)
					_stopPrice = potentialStop;
			}
		}
	}

	private void InitializeTargets(decimal entryPrice, bool isLong)
	{
		_entryPrice = entryPrice;
		_maxFavorableMove = 0m;

		_stopPrice = StopLossOffset > 0m
			? isLong ? entryPrice - StopLossOffset : entryPrice + StopLossOffset
			: null;

		_takeProfitPrice = TakeProfitOffset > 0m
			? isLong ? entryPrice + TakeProfitOffset : entryPrice - TakeProfitOffset
			: null;
	}

	private void FinalizeExit(ICandleMessage candle)
	{
		_stopPrice = null;
		_takeProfitPrice = null;
		_entryPrice = 0m;
		_maxFavorableMove = 0m;
		_lastExitDate = candle.OpenTime.Date;
	}
}

