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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Conversion of the MetaTrader "bobnaley" expert advisor.
/// Buys when a falling moving average and oversold stochastic align with price strength.
/// Sells when a rising moving average and overbought stochastic align with price weakness.
/// </summary>
public class BobnaleyStrategy : Strategy
{
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _stochasticPeriod;
	private readonly StrategyParam<int> _stochasticK;
	private readonly StrategyParam<int> _stochasticD;
	private readonly StrategyParam<decimal> _stochasticOversold;
	private readonly StrategyParam<decimal> _stochasticOverbought;
	private readonly StrategyParam<decimal> _minimumBalance;
	private readonly StrategyParam<decimal> _baseVolume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _maCurrent;
	private decimal? _maPrevious1;
	private decimal? _maPrevious2;

	private decimal? _stochCurrent;
	private decimal? _stochPrevious1;
	private decimal? _stochPrevious2;

	/// <summary>
	/// Take profit distance in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss distance in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Period of the simple moving average.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Lookback period of the stochastic oscillator.
	/// </summary>
	public int StochasticPeriod
	{
		get => _stochasticPeriod.Value;
		set => _stochasticPeriod.Value = value;
	}

	/// <summary>
	/// Smoothing length for the %K line.
	/// </summary>
	public int StochasticK
	{
		get => _stochasticK.Value;
		set => _stochasticK.Value = value;
	}

	/// <summary>
	/// Smoothing length for the %D line.
	/// </summary>
	public int StochasticD
	{
		get => _stochasticD.Value;
		set => _stochasticD.Value = value;
	}

	/// <summary>
	/// Oversold threshold for the stochastic main line.
	/// </summary>
	public decimal StochasticOversold
	{
		get => _stochasticOversold.Value;
		set => _stochasticOversold.Value = value;
	}

	/// <summary>
	/// Overbought threshold for the stochastic main line.
	/// </summary>
	public decimal StochasticOverbought
	{
		get => _stochasticOverbought.Value;
		set => _stochasticOverbought.Value = value;
	}

	/// <summary>
	/// Minimal portfolio value required to allow new entries.
	/// </summary>
	public decimal MinimumBalance
	{
		get => _minimumBalance.Value;
		set => _minimumBalance.Value = value;
	}

	/// <summary>
	/// Fixed order volume used for entries.
	/// </summary>
	public decimal BaseVolume
	{
		get => _baseVolume.Value;
		set => _baseVolume.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy with default parameters from the original script.
	/// </summary>
	public BobnaleyStrategy()
	{
		_takeProfit = Param(nameof(TakeProfit), 0.007m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Take Profit", "Target distance in price units", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.002m, 0.02m, 0.001m);

		_stopLoss = Param(nameof(StopLoss), 0.0035m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Stop Loss", "Protective stop distance in price units", "Risk Management")
		.SetCanOptimize(true)
		.SetOptimize(0.001m, 0.01m, 0.0005m);

		_maPeriod = Param(nameof(MaPeriod), 76)
		.SetGreaterThanZero()
		.SetDisplay("MA Period", "Length of the simple moving average", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(20, 120, 10);

		_stochasticPeriod = Param(nameof(StochasticPeriod), 5)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic Period", "Lookback period for the stochastic oscillator", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(5, 21, 2);

		_stochasticK = Param(nameof(StochasticK), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %K", "Smoothing length for the %K line", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_stochasticD = Param(nameof(StochasticD), 3)
		.SetGreaterThanZero()
		.SetDisplay("Stochastic %D", "Smoothing length for the %D line", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(1, 5, 1);

		_stochasticOversold = Param(nameof(StochasticOversold), 30m)
		.SetNotNegative()
		.SetDisplay("Stochastic Oversold", "Oversold threshold for the main line", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(10m, 40m, 5m);

		_stochasticOverbought = Param(nameof(StochasticOverbought), 70m)
		.SetNotNegative()
		.SetDisplay("Stochastic Overbought", "Overbought threshold for the main line", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(60m, 90m, 5m);

		_minimumBalance = Param(nameof(MinimumBalance), 5000m)
		.SetGreaterThanOrEqualZero()
		.SetDisplay("Minimum Balance", "Minimal portfolio value required for new trades", "Risk Management");

		_baseVolume = Param(nameof(BaseVolume), 5m)
		.SetGreaterThanZero()
		.SetDisplay("Base Volume", "Fixed order volume used for entries", "Trading");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
		.SetDisplay("Candle Type", "Time frame for indicator calculations", "General");
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

		_maCurrent = null;
		_maPrevious1 = null;
		_maPrevious2 = null;

		_stochCurrent = null;
		_stochPrevious1 = null;
		_stochPrevious2 = null;
		}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		Volume = BaseVolume;

		base.OnStarted(time);

		StartProtection(
		takeProfit: new Unit(TakeProfit, UnitTypes.Absolute),
		stopLoss: new Unit(StopLoss, UnitTypes.Absolute));

		var movingAverage = new SimpleMovingAverage
		{
		Length = MaPeriod
	};

		var stochastic = new Stochastic
		{
		Length = StochasticPeriod,
		K = StochasticK,
		D = StochasticD
	};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(movingAverage, stochastic, ProcessCandle)
		.Start();

		var area = CreateChartArea();
		if (area != null)
		{
		DrawCandles(area, subscription);
		DrawIndicator(area, movingAverage);
		DrawIndicator(area, stochastic);
		DrawOwnTrades(area);
		}
		}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal stochasticMain, decimal stochasticSignal)
	{
		// The stochastic signal line is not required for the original logic but is kept for completeness.
		_ = stochasticSignal;
		if (candle.State != CandleStates.Finished)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		var balance = Portfolio?.CurrentValue;
		if (balance.HasValue && balance.Value < MinimumBalance)
		return;

		UpdateHistory(ref _maPrevious2, ref _maPrevious1, ref _maCurrent, maValue);
		UpdateHistory(ref _stochPrevious2, ref _stochPrevious1, ref _stochCurrent, stochasticMain);

		if (_maCurrent is null || _maPrevious1 is null || _maPrevious2 is null)
		return;

		if (_stochCurrent is null || _stochPrevious1 is null || _stochPrevious2 is null)
		return;

		var ma0 = _maCurrent.Value;
		var ma1 = _maPrevious1.Value;
		var ma2 = _maPrevious2.Value;

		var stoch0 = _stochCurrent.Value;
		var stoch1 = _stochPrevious1.Value;
		var stoch2 = _stochPrevious2.Value;

		var price = candle.ClosePrice;

		var buyCondition = ma0 < ma1 && ma1 < ma2 && price > ma0 && stoch1 > stoch2 && stoch0 < StochasticOversold;
		var sellCondition = ma0 > ma1 && ma1 > ma2 && price < ma0 && stoch1 < stoch2 && stoch0 > StochasticOverbought;

		if (buyCondition && Position == 0)
		{
		BuyMarket(Volume);
		}
		else if (sellCondition && Position == 0)
		{
		SellMarket(Volume);
		}
		}

	private static void UpdateHistory(ref decimal? oldest, ref decimal? previous, ref decimal? current, decimal newValue)
	{
		oldest = previous;
		previous = current;
		current = newValue;
		}
}

