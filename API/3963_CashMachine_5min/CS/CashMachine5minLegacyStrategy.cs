using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cash Machine strategy converted from the MetaTrader 4 expert advisor.
/// Uses DeMarker and Stochastic oscillator crossovers on five minute candles
/// and gradually tightens a hidden stop when profit targets are reached.
/// </summary>
public class CashMachine5minLegacyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hiddenTakeProfit;
	private readonly StrategyParam<decimal> _hiddenStopLoss;
	private readonly StrategyParam<decimal> _targetTp1;
	private readonly StrategyParam<decimal> _targetTp2;
	private readonly StrategyParam<decimal> _targetTp3;
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _deMarkerLength;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _previousDeMarker;
	private decimal? _previousStochasticK;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private int _longStage;
	private int _shortStage;
	private decimal _pipSize;

	/// <summary>
	/// Hidden take profit distance expressed in pips.
	/// </summary>
	public decimal HiddenTakeProfit
	{
		get => _hiddenTakeProfit.Value;
		set => _hiddenTakeProfit.Value = value;
	}

	/// <summary>
	/// Hidden stop loss distance expressed in pips.
	/// </summary>
	public decimal HiddenStopLoss
	{
		get => _hiddenStopLoss.Value;
		set => _hiddenStopLoss.Value = value;
	}

	/// <summary>
	/// First profit threshold in pips.
	/// </summary>
	public decimal TargetTp1
	{
		get => _targetTp1.Value;
		set => _targetTp1.Value = value;
	}

	/// <summary>
	/// Second profit threshold in pips.
	/// </summary>
	public decimal TargetTp2
	{
		get => _targetTp2.Value;
		set => _targetTp2.Value = value;
	}

	/// <summary>
	/// Third profit threshold in pips.
	/// </summary>
	public decimal TargetTp3
	{
		get => _targetTp3.Value;
		set => _targetTp3.Value = value;
	}

	/// <summary>
	/// Order volume used when opening new positions.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// DeMarker averaging period.
	/// </summary>
	public int DeMarkerLength
	{
		get => _deMarkerLength.Value;
		set => _deMarkerLength.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator length.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing factor for the Stochastic oscillator.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// %D smoothing factor for the Stochastic oscillator.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Candle type that drives indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CashMachine5minLegacyStrategy"/> class.
	/// </summary>
	public CashMachine5minLegacyStrategy()
	{
		_hiddenTakeProfit = Param(nameof(HiddenTakeProfit), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Take Profit", "Hidden take profit distance in pips", "Risk");

		_hiddenStopLoss = Param(nameof(HiddenStopLoss), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Stop Loss", "Hidden stop loss distance in pips", "Risk");

		_targetTp1 = Param(nameof(TargetTp1), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP1", "First profit threshold", "Risk");

		_targetTp2 = Param(nameof(TargetTp2), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP2", "Second profit threshold", "Risk");

		_targetTp3 = Param(nameof(TargetTp3), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP3", "Third profit threshold", "Risk");

		_orderVolume = Param(nameof(OrderVolume), 0.2m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Order volume for new trades", "Trading");

		_deMarkerLength = Param(nameof(DeMarkerLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Length", "DeMarker averaging period", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Base Stochastic length", "Indicators");

		_stochasticK = Param(nameof(StochasticK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K smoothing length", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe", "General");

		_pipSize = 0.0001m;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_previousDeMarker = null;
		_previousStochasticK = null;
		_longTrailingStop = null;
		_shortTrailingStop = null;
		_longStage = 0;
		_shortStage = 0;
		_pipSize = 0.0001m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_pipSize = CalculatePipSize();

		var deMarker = new DeMarker
		{
			Length = DeMarkerLength,
		};

		var stochastic = new StochasticOscillator
		{
			Length = StochasticLength,
			K = { Length = StochasticK },
			D = { Length = StochasticD },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(deMarker, stochastic, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, deMarker);

			var oscillatorArea = CreateChartArea();
			if (oscillatorArea != null)
			{
				DrawIndicator(oscillatorArea, stochastic);
			}

			DrawOwnTrades(priceArea);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue deMarkerValue, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!stochasticValue.IsFinal || !IsFormedAndOnlineAndAllowTrading())
			return;

		var deMarker = deMarkerValue.ToDecimal();
		var stochastic = (StochasticOscillatorValue)stochasticValue;

		if (stochastic.K is not decimal currentK)
			return;

		if (Position == 0)
		{
			// Reset trailing state whenever the strategy is flat.
			_longStage = 0;
			_shortStage = 0;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		if (Position == 0 && _previousDeMarker is decimal prevDe && _previousStochasticK is decimal prevK)
		{
			var longSignal = prevDe < 0.30m && deMarker >= 0.30m && prevK < 20m && currentK >= 20m;
			var shortSignal = prevDe > 0.70m && deMarker <= 0.70m && prevK > 80m && currentK <= 80m;

			if (longSignal && OrderVolume > 0m)
			{
				// Both oscillators crossed up from oversold zones.
				BuyMarket(OrderVolume);
			}
			else if (shortSignal && OrderVolume > 0m)
			{
				// Both oscillators crossed down from overbought zones.
				SellMarket(OrderVolume);
			}
		}
		else if (Position > 0)
		{
			ManageLongPosition(candle);
		}
		else if (Position < 0)
		{
			ManageShortPosition(candle);
		}

		_previousDeMarker = deMarker;
		_previousStochasticK = currentK;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m || _pipSize <= 0m)
			return;

		var stopLossPrice = entryPrice - HiddenStopLoss * _pipSize;
		var takeProfitPrice = entryPrice + HiddenTakeProfit * _pipSize;

		// Close long position if the hidden stop or take profit is hit.
		if (candle.LowPrice <= stopLossPrice || candle.HighPrice >= takeProfitPrice)
		{
			SellMarket(Position);
			return;
		}

		var target1 = entryPrice + TargetTp1 * _pipSize;
		var target2 = entryPrice + TargetTp2 * _pipSize;
		var target3 = entryPrice + TargetTp3 * _pipSize;

		if (_longStage < 3 && candle.HighPrice >= target3)
		{
			var newStop = candle.HighPrice - Math.Max(TargetTp3 - 13m, 0m) * _pipSize;
			_longTrailingStop = _longTrailingStop.HasValue ? Math.Max(_longTrailingStop.Value, newStop) : newStop;
			_longStage = 3;
			return;
		}

		if (_longStage < 2 && candle.HighPrice >= target2)
		{
			var newStop = candle.HighPrice - Math.Max(TargetTp2 - 13m, 0m) * _pipSize;
			_longTrailingStop = _longTrailingStop.HasValue ? Math.Max(_longTrailingStop.Value, newStop) : newStop;
			_longStage = 2;
			return;
		}

		if (_longStage < 1 && candle.HighPrice >= target1)
		{
			var newStop = candle.HighPrice - Math.Max(TargetTp1 - 13m, 0m) * _pipSize;
			_longTrailingStop = _longTrailingStop.HasValue ? Math.Max(_longTrailingStop.Value, newStop) : newStop;
			_longStage = 1;
			return;
		}

		// Exit if the trailing stop is touched after at least one target.
		if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
		{
			SellMarket(Position);
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var entryPrice = PositionPrice;
		if (entryPrice <= 0m || _pipSize <= 0m)
			return;

		var stopLossPrice = entryPrice + HiddenStopLoss * _pipSize;
		var takeProfitPrice = entryPrice - HiddenTakeProfit * _pipSize;

		// Close short position if hidden protective levels are reached.
		if (candle.HighPrice >= stopLossPrice || candle.LowPrice <= takeProfitPrice)
		{
			BuyMarket(-Position);
			return;
		}

		var target1 = entryPrice - TargetTp1 * _pipSize;
		var target2 = entryPrice - TargetTp2 * _pipSize;
		var target3 = entryPrice - TargetTp3 * _pipSize;

		if (_shortStage < 3 && candle.LowPrice <= target3)
		{
			var newStop = candle.LowPrice + (TargetTp3 + 13m) * _pipSize;
			_shortTrailingStop = _shortTrailingStop.HasValue ? Math.Min(_shortTrailingStop.Value, newStop) : newStop;
			_shortStage = 3;
			return;
		}

		if (_shortStage < 2 && candle.LowPrice <= target2)
		{
			var newStop = candle.LowPrice + (TargetTp2 + 13m) * _pipSize;
			_shortTrailingStop = _shortTrailingStop.HasValue ? Math.Min(_shortTrailingStop.Value, newStop) : newStop;
			_shortStage = 2;
			return;
		}

		if (_shortStage < 1 && candle.LowPrice <= target1)
		{
			var newStop = candle.LowPrice + (TargetTp1 + 13m) * _pipSize;
			_shortTrailingStop = _shortTrailingStop.HasValue ? Math.Min(_shortTrailingStop.Value, newStop) : newStop;
			_shortStage = 1;
			return;
		}

		if (_shortTrailingStop is decimal trailing && candle.HighPrice >= trailing)
		{
			BuyMarket(-Position);
		}
	}

	private decimal CalculatePipSize()
	{
		var step = Security?.PriceStep ?? 0m;
		if (step <= 0m)
			return 0.0001m;

		var inverse = (double)(1m / step);
		var digits = (int)Math.Round(Math.Log10(inverse));
		var adjust = (digits == 3 || digits == 5) ? 10m : 1m;

		return step * adjust;
	}
}
