using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy combining momentum and Keltner stochastic.
/// </summary>
public class MomentumKeltnerStochasticComboStrategy : Strategy
{
	private readonly StrategyParam<int> _momLength;
	private readonly StrategyParam<int> _keltnerLength;
	private readonly StrategyParam<decimal> _keltnerMultiplier;
	private readonly StrategyParam<decimal> _threshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _slPoints;

	private readonly StrategyParam<bool> _enableScaling;
	private readonly StrategyParam<int> _baseContracts;
	private readonly StrategyParam<decimal> _initialCapital;
	private readonly StrategyParam<decimal> _equityStep;
	private readonly StrategyParam<int> _maxContracts;

	private readonly StrategyParam<DataType> _candleType;

	public int MomLength
	{
		get => _momLength.Value;
		set => _momLength.Value = value;
	}

	public int KeltnerLength
	{
		get => _keltnerLength.Value;
		set => _keltnerLength.Value = value;
	}

	public decimal KeltnerMultiplier
	{
		get => _keltnerMultiplier.Value;
		set => _keltnerMultiplier.Value = value;
	}

	public decimal Threshold
	{
		get => _threshold.Value;
		set => _threshold.Value = value;
	}

	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	public decimal SlPoints
	{
		get => _slPoints.Value;
		set => _slPoints.Value = value;
	}

	public bool EnableScaling
	{
		get => _enableScaling.Value;
		set => _enableScaling.Value = value;
	}

	public int BaseContracts
	{
		get => _baseContracts.Value;
		set => _baseContracts.Value = value;
	}

	public decimal InitialCapital
	{
		get => _initialCapital.Value;
		set => _initialCapital.Value = value;
	}

	public decimal EquityStep
	{
		get => _equityStep.Value;
		set => _equityStep.Value = value;
	}

	public int MaxContracts
	{
		get => _maxContracts.Value;
		set => _maxContracts.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public MomentumKeltnerStochasticComboStrategy()
	{
		_momLength = Param(nameof(MomLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("Momentum Lookback", "Momentum lookback length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 15, 1);

		_keltnerLength = Param(nameof(KeltnerLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Keltner EMA Length", "EMA length for Keltner basis", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Mult", "Keltner multiplier", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 2m, 0.1m);

		_threshold = Param(nameof(Threshold), 99m)
			.SetRange(0m, 100m)
			.SetDisplay("Stochastic Threshold", "Threshold for Keltner stochastic", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(50m, 100m, 5m);

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length for Keltner", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 30, 1);

		_slPoints = Param(nameof(SlPoints), 1185m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss Points", "Stop loss in price points", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(500m, 2000m, 100m);

		_enableScaling = Param(nameof(EnableScaling), true)
			.SetDisplay("Enable Dynamic Contracts", "Use equity based position sizing", "Money Management");

		_baseContracts = Param(nameof(BaseContracts), 1)
			.SetGreaterThanZero()
			.SetDisplay("Base Contracts", "Initial contract size", "Money Management");

		_initialCapital = Param(nameof(InitialCapital), 30000m)
			.SetGreaterThanZero()
			.SetDisplay("Initial Capital", "Starting capital", "Money Management");

		_equityStep = Param(nameof(EquityStep), 150000m)
			.SetGreaterThanZero()
			.SetDisplay("Equity Step", "Equity step for contract change", "Money Management");

		_maxContracts = Param(nameof(MaxContracts), 15)
			.SetGreaterThanZero()
			.SetDisplay("Max Contracts", "Maximum contracts allowed", "Money Management");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles for calculations", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema = new ExponentialMovingAverage { Length = KeltnerLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var momentum = new Momentum { Length = MomLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ema, atr, momentum, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(0, UnitTypes.Absolute),
			stopLoss: new Unit(SlPoints, UnitTypes.Absolute)
		);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, atr);
			DrawIndicator(area, momentum);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal atrValue, decimal momentumValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var upper = emaValue + KeltnerMultiplier * atrValue;
		var lower = emaValue - KeltnerMultiplier * atrValue;
		var denominator = upper - lower;
		var keltnerStoch = denominator != 0m ? 100m * (candle.ClosePrice - lower) / denominator : 50m;

		var longCondition = momentumValue > 0m && keltnerStoch < Threshold;
		var shortCondition = momentumValue < 0m && keltnerStoch > Threshold;

		var exitLongCondition = Position > 0 && keltnerStoch > Threshold;
		var exitShortCondition = Position < 0 && keltnerStoch < Threshold;

		var contractSize = BaseContracts;

		if (EnableScaling)
		{
			var profitLoss = PnL;
			var contractIncrease = (int)Math.Floor(Math.Max(profitLoss, 0m) / EquityStep);
			var contractDecrease = (int)Math.Floor(Math.Abs(Math.Min(profitLoss, 0m)) / EquityStep);
			contractSize = Math.Max(BaseContracts + contractIncrease - contractDecrease, BaseContracts);
		}

		if (contractSize > MaxContracts)
			contractSize = MaxContracts;

		if (longCondition && Position <= 0)
		{
			var volume = contractSize + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortCondition && Position >= 0)
		{
			var volume = contractSize + Math.Abs(Position);
			SellMarket(volume);
		}

		if (exitLongCondition)
			SellMarket(Math.Abs(Position));
		else if (exitShortCondition)
			BuyMarket(Math.Abs(Position));
	}
}
