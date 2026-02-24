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
/// Conversion of the "MartingailExpert v1.0 Stochastic" MetaTrader expert advisor.
/// Implements stochastic based entries with martingale averaging and cluster take profits.
/// </summary>
public class MartingailExpertV10StochasticStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stepPoints;
	private readonly StrategyParam<int> _stepMode;
	private readonly StrategyParam<decimal> _profitFactorPoints;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _zoneBuy;
	private readonly StrategyParam<decimal> _zoneSell;
	private readonly StrategyParam<DataType> _candleType;

	private StochasticOscillator _stochastic;

	private decimal _pointSize;
	private decimal? _prevK;
	private decimal? _prevD;

	private decimal _buyLastPrice;
	private decimal _buyLastVolume;
	private decimal _buyTotalVolume;
	private decimal _buyWeightedSum;
	private int _buyOrderCount;
	private decimal _buyTakeProfit;

	private decimal _sellLastPrice;
	private decimal _sellLastVolume;
	private decimal _sellTotalVolume;
	private decimal _sellWeightedSum;
	private int _sellOrderCount;
	private decimal _sellTakeProfit;

	/// <summary>
	/// Distance in points that price has to travel against the latest entry before adding.
	/// </summary>
	public decimal StepPoints
	{
		get => _stepPoints.Value;
		set => _stepPoints.Value = value;
	}

	/// <summary>
	/// Step mode: 0 - fixed, 1 - fixed plus extra points per filled order.
	/// </summary>
	public int StepMode
	{
		get => _stepMode.Value;
		set => _stepMode.Value = value;
	}

	/// <summary>
	/// Profit target in points applied to every open order.
	/// </summary>
	public decimal ProfitFactorPoints
	{
		get => _profitFactorPoints.Value;
		set => _profitFactorPoints.Value = value;
	}

	/// <summary>
	/// Martingale multiplier for the next averaging order.
	/// </summary>
	public decimal Multiplier
	{
		get => _multiplier.Value;
		set => _multiplier.Value = value;
	}

	/// <summary>
	/// Stochastic %K lookback period.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Stochastic %D smoothing length.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Minimum stochastic level that confirms long setups.
	/// </summary>
	public decimal ZoneBuy
	{
		get => _zoneBuy.Value;
		set => _zoneBuy.Value = value;
	}

	/// <summary>
	/// Maximum stochastic level that confirms short setups.
	/// </summary>
	public decimal ZoneSell
	{
		get => _zoneSell.Value;
		set => _zoneSell.Value = value;
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
	/// Initializes a new instance of <see cref="MartingailExpertV10StochasticStrategy"/>.
	/// </summary>
	public MartingailExpertV10StochasticStrategy()
	{
		_stepPoints = Param(nameof(StepPoints), 500m)
			.SetGreaterThanZero()
			.SetDisplay("Step", "Price step in points before averaging", "Martingale");

		_stepMode = Param(nameof(StepMode), 0)
			.SetDisplay("Step Mode", "0 - fixed step, 1 - step plus extra points per order", "Martingale");

		_profitFactorPoints = Param(nameof(ProfitFactorPoints), 300m)
			.SetGreaterThanZero()
			.SetDisplay("Profit Factor", "Points multiplied by order count for take profit", "Martingale");

		_multiplier = Param(nameof(Multiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("Multiplier", "Martingale multiplier for averaging", "Martingale");

		_kPeriod = Param(nameof(KPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("%K Period", "Stochastic %K lookback", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("%D Period", "Stochastic %D smoothing", "Indicators");

		_zoneBuy = Param(nameof(ZoneBuy), 50m)
			.SetDisplay("Zone Buy", "%D lower bound to allow buys", "Indicators");

		_zoneSell = Param(nameof(ZoneSell), 50m)
			.SetDisplay("Zone Sell", "%D upper bound to allow sells", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe used for processing", "General");

		Volume = 1;
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
		_stochastic = null;
		_pointSize = 0m;
		_prevK = null;
		_prevD = null;
		ResetLongState();
		ResetShortState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		_pointSize = Security?.PriceStep ?? 1m;
		if (_pointSize <= 0m) _pointSize = 1m;

		_stochastic = new StochasticOscillator();
		_stochastic.K.Length = KPeriod;
		_stochastic.D.Length = DPeriod;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_stochastic, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);

			var indArea = CreateChartArea();
			if (indArea != null)
				DrawIndicator(indArea, _stochastic);
		}

		base.OnStarted2(time);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochasticValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (stochasticValue is not StochasticOscillatorValue stoch)
			return;

		if (stoch.K is not decimal currentK || stoch.D is not decimal currentD)
			return;

		if (!_stochastic.IsFormed)
		{
			_prevK = currentK;
			_prevD = currentD;
			return;
		}

		var tradingAllowed = IsFormedAndOnlineAndAllowTrading();

		ManageClusters(candle, tradingAllowed);

		if (!tradingAllowed)
		{
			_prevK = currentK;
			_prevD = currentD;
			return;
		}

		// Entry logic: stochastic crossover in oversold/overbought zones
		if (Position == 0m && _buyOrderCount == 0 && _sellOrderCount == 0
			&& _prevK is decimal prevK && _prevD is decimal prevD)
		{
			if (prevK > prevD && prevD > ZoneBuy)
			{
				OpenLong(candle.ClosePrice);
			}
			else if (prevK < prevD && prevD < ZoneSell)
			{
				OpenShort(candle.ClosePrice);
			}
		}

		_prevK = currentK;
		_prevD = currentD;
	}

	private void ManageClusters(ICandleMessage candle, bool tradingAllowed)
	{
		if (Position > 0m && _buyOrderCount > 0)
		{
			HandleLongCluster(candle, tradingAllowed);
		}
		else if (Position < 0m && _sellOrderCount > 0)
		{
			HandleShortCluster(candle, tradingAllowed);
		}
		else if (Position == 0m)
		{
			if (_buyOrderCount > 0 || _sellOrderCount > 0)
			{
				ResetLongState();
				ResetShortState();
			}
		}
	}

	private void HandleLongCluster(ICandleMessage candle, bool tradingAllowed)
	{
		if (!tradingAllowed || _pointSize <= 0m)
			return;

		// Check take profit first
		if (_buyTakeProfit > 0m && candle.HighPrice >= _buyTakeProfit)
		{
			SellMarket(Math.Abs(Position));
			ResetLongState();
			return;
		}

		// Average down
		var currentCount = Math.Max(1, _buyOrderCount);
		var stepPts = StepMode == 0
			? StepPoints
			: StepPoints + Math.Max(0m, currentCount * 2m - 2m);
		var addTrigger = _buyLastPrice - stepPts * _pointSize;

		if (_buyLastVolume > 0m && candle.LowPrice <= addTrigger)
		{
			var nextVolume = Math.Max(1m, Math.Round(_buyLastVolume * Multiplier));
			BuyMarket(nextVolume);

			var executionPrice = candle.ClosePrice;
			_buyLastVolume = nextVolume;
			_buyLastPrice = executionPrice;
			_buyTotalVolume += nextVolume;
			_buyWeightedSum += executionPrice * nextVolume;
			_buyOrderCount++;
			RecalcLongTp();
		}
	}

	private void HandleShortCluster(ICandleMessage candle, bool tradingAllowed)
	{
		if (!tradingAllowed || _pointSize <= 0m)
			return;

		// Check take profit first
		if (_sellTakeProfit > 0m && candle.LowPrice <= _sellTakeProfit)
		{
			BuyMarket(Math.Abs(Position));
			ResetShortState();
			return;
		}

		// Average up
		var currentCount = Math.Max(1, _sellOrderCount);
		var stepPts = StepMode == 0
			? StepPoints
			: StepPoints + Math.Max(0m, currentCount * 2m - 2m);
		var addTrigger = _sellLastPrice + stepPts * _pointSize;

		if (_sellLastVolume > 0m && candle.HighPrice >= addTrigger)
		{
			var nextVolume = Math.Max(1m, Math.Round(_sellLastVolume * Multiplier));
			SellMarket(nextVolume);

			var executionPrice = candle.ClosePrice;
			_sellLastVolume = nextVolume;
			_sellLastPrice = executionPrice;
			_sellTotalVolume += nextVolume;
			_sellWeightedSum += executionPrice * nextVolume;
			_sellOrderCount++;
			RecalcShortTp();
		}
	}

	private void OpenLong(decimal price)
	{
		BuyMarket(Volume);

		_buyLastPrice = price;
		_buyLastVolume = Volume;
		_buyTotalVolume = Volume;
		_buyWeightedSum = price * Volume;
		_buyOrderCount = 1;
		RecalcLongTp();

		ResetShortState();
	}

	private void OpenShort(decimal price)
	{
		SellMarket(Volume);

		_sellLastPrice = price;
		_sellLastVolume = Volume;
		_sellTotalVolume = Volume;
		_sellWeightedSum = price * Volume;
		_sellOrderCount = 1;
		RecalcShortTp();

		ResetLongState();
	}

	private void RecalcLongTp()
	{
		var avg = _buyTotalVolume > 0 ? _buyWeightedSum / _buyTotalVolume : _buyLastPrice;
		_buyTakeProfit = avg + ProfitFactorPoints * _pointSize;
	}

	private void RecalcShortTp()
	{
		var avg = _sellTotalVolume > 0 ? _sellWeightedSum / _sellTotalVolume : _sellLastPrice;
		_sellTakeProfit = avg - ProfitFactorPoints * _pointSize;
	}

	private void ResetLongState()
	{
		_buyLastPrice = 0m;
		_buyLastVolume = 0m;
		_buyTotalVolume = 0m;
		_buyWeightedSum = 0m;
		_buyOrderCount = 0;
		_buyTakeProfit = 0m;
	}

	private void ResetShortState()
	{
		_sellLastPrice = 0m;
		_sellLastVolume = 0m;
		_sellTotalVolume = 0m;
		_sellWeightedSum = 0m;
		_sellOrderCount = 0;
		_sellTakeProfit = 0m;
	}
}
