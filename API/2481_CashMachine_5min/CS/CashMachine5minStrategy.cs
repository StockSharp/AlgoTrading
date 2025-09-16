using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Cash Machine strategy converted from MQL to StockSharp.
/// Combines DeMarker and Stochastic oscillator crossovers
/// with staged profit protection on five minute candles.
/// </summary>
public class CashMachine5minStrategy : Strategy
{
	private readonly StrategyParam<decimal> _hiddenTakeProfit;
	private readonly StrategyParam<decimal> _hiddenStopLoss;
	private readonly StrategyParam<decimal> _targetTp1;
	private readonly StrategyParam<decimal> _targetTp2;
	private readonly StrategyParam<decimal> _targetTp3;
	private readonly StrategyParam<int> _deMarkerLength;
	private readonly StrategyParam<int> _stochasticLength;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevDeMarker;
	private decimal? _prevStochasticK;
	private int _longStage;
	private int _shortStage;
	private decimal? _longTrailingStop;
	private decimal? _shortTrailingStop;
	private decimal _pipSize;

	/// <summary>
	/// Hidden take profit in pips.
	/// </summary>
	public decimal HiddenTakeProfit
	{
		get => _hiddenTakeProfit.Value;
		set => _hiddenTakeProfit.Value = value;
	}

	/// <summary>
	/// Hidden stop loss in pips.
	/// </summary>
	public decimal HiddenStopLoss
	{
		get => _hiddenStopLoss.Value;
		set => _hiddenStopLoss.Value = value;
	}

	/// <summary>
	/// First profit target in pips.
	/// </summary>
	public decimal TargetTp1
	{
		get => _targetTp1.Value;
		set => _targetTp1.Value = value;
	}

	/// <summary>
	/// Second profit target in pips.
	/// </summary>
	public decimal TargetTp2
	{
		get => _targetTp2.Value;
		set => _targetTp2.Value = value;
	}

	/// <summary>
	/// Third profit target in pips.
	/// </summary>
	public decimal TargetTp3
	{
		get => _targetTp3.Value;
		set => _targetTp3.Value = value;
	}

	/// <summary>
	/// DeMarker period length.
	/// </summary>
	public int DeMarkerLength
	{
		get => _deMarkerLength.Value;
		set => _deMarkerLength.Value = value;
	}

	/// <summary>
	/// Stochastic oscillator lookback period.
	/// </summary>
	public int StochasticLength
	{
		get => _stochasticLength.Value;
		set => _stochasticLength.Value = value;
	}

	/// <summary>
	/// %K smoothing length for Stochastic oscillator.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// %D smoothing length for Stochastic oscillator.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
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
	/// Initializes a new instance of the <see cref="CashMachine5minStrategy"/> class.
	/// </summary>
	public CashMachine5minStrategy()
	{
		_hiddenTakeProfit = Param(nameof(HiddenTakeProfit), 60m)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Take Profit", "Hidden take profit in pips", "Risk");

		_hiddenStopLoss = Param(nameof(HiddenStopLoss), 30m)
			.SetGreaterThanZero()
			.SetDisplay("Hidden Stop Loss", "Hidden stop loss in pips", "Risk");

		_targetTp1 = Param(nameof(TargetTp1), 20m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP1", "First profit target in pips", "Risk");

		_targetTp2 = Param(nameof(TargetTp2), 35m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP2", "Second profit target in pips", "Risk");

		_targetTp3 = Param(nameof(TargetTp3), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Target TP3", "Third profit target in pips", "Risk");

		_deMarkerLength = Param(nameof(DeMarkerLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("DeMarker Length", "DeMarker lookback period", "Indicators");

		_stochasticLength = Param(nameof(StochasticLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic Length", "Stochastic oscillator period", "Indicators");

		_stochasticK = Param(nameof(StochasticK), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %K", "%K smoothing length", "Indicators");

		_stochasticD = Param(nameof(StochasticD), 3)
			.SetGreaterThanZero()
			.SetDisplay("Stochastic %D", "%D smoothing length", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Primary candle series", "General");

		_pipSize = 0.0001m;
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

		_prevDeMarker = null;
		_prevStochasticK = null;
		_longStage = 0;
		_shortStage = 0;
		_longTrailingStop = null;
		_shortTrailingStop = null;
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

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, deMarker);

			var stochArea = CreateChartArea();
			if (stochArea != null)
			{
				DrawIndicator(stochArea, stochastic);
			}

			DrawOwnTrades(area);
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

		if (stochastic.K is not decimal stochK)
			return;

		if (Position == 0)
		{
			_longStage = 0;
			_shortStage = 0;
			_longTrailingStop = null;
			_shortTrailingStop = null;
		}

		if (Position == 0 && _prevDeMarker is decimal prevDe && _prevStochasticK is decimal prevStoch)
		{
			var longSignal = prevDe < 0.30m && deMarker >= 0.30m && prevStoch < 20m && stochK >= 20m;
			var shortSignal = prevDe > 0.70m && deMarker <= 0.70m && prevStoch > 80m && stochK <= 80m;

			if (longSignal)
			{
				BuyMarket();
			}
			else if (shortSignal)
			{
				SellMarket();
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

		_prevDeMarker = deMarker;
		_prevStochasticK = stochK;
	}

	private void ManageLongPosition(ICandleMessage candle)
	{
		var entry = PositionPrice;
		if (entry <= 0m || _pipSize <= 0m)
			return;

		var stopLossPrice = entry - HiddenStopLoss * _pipSize;
		var takeProfitPrice = entry + HiddenTakeProfit * _pipSize;

		if (candle.LowPrice <= stopLossPrice || candle.HighPrice >= takeProfitPrice)
		{
			SellMarket(Position);
			return;
		}

		var target3 = entry + TargetTp3 * _pipSize;
		var target2 = entry + TargetTp2 * _pipSize;
		var target1 = entry + TargetTp1 * _pipSize;

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

		if (_longTrailingStop is decimal trailing && candle.LowPrice <= trailing)
		{
			SellMarket(Position);
		}
	}

	private void ManageShortPosition(ICandleMessage candle)
	{
		var entry = PositionPrice;
		if (entry <= 0m || _pipSize <= 0m)
			return;

		var stopLossPrice = entry + HiddenStopLoss * _pipSize;
		var takeProfitPrice = entry - HiddenTakeProfit * _pipSize;

		if (candle.HighPrice >= stopLossPrice || candle.LowPrice <= takeProfitPrice)
		{
			BuyMarket(Math.Abs(Position));
			return;
		}

		var target3 = entry - TargetTp3 * _pipSize;
		var target2 = entry - TargetTp2 * _pipSize;
		var target1 = entry - TargetTp1 * _pipSize;

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
			BuyMarket(Math.Abs(Position));
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

