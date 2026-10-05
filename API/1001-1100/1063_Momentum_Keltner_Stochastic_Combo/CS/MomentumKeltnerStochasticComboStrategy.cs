using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Momentum Keltner Stochastic Combo strategy.
/// The Keltner stochastic places the close inside a Keltner channel (EMA basis, ATR width) on a 0-100 scale.
/// Goes long when momentum is positive and the stochastic is below Threshold, short when momentum is negative and
/// the stochastic is above Threshold. A long exits when the stochastic rises above Threshold and a short when it falls
/// below it. Position size can grow with realized profit, and a fixed stop in points protects every position.
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
			.SetOptimize(5, 15, 1);

		_keltnerLength = Param(nameof(KeltnerLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Keltner EMA Length", "EMA length for Keltner basis", "Indicators")
			.SetOptimize(5, 20, 1);

		_keltnerMultiplier = Param(nameof(KeltnerMultiplier), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Keltner Mult", "Keltner multiplier", "Indicators")
			.SetOptimize(0.5m, 2m, 0.1m);

		_threshold = Param(nameof(Threshold), 99m)
			.SetRange(0m, 100m)
			.SetDisplay("Stochastic Threshold", "Threshold for Keltner stochastic", "Indicators")
			.SetOptimize(50m, 100m, 5m);

		_atrLength = Param(nameof(AtrLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length for Keltner", "Indicators")
			.SetOptimize(10, 30, 1);

		_slPoints = Param(nameof(SlPoints), 1185m)
			.SetNotNegative()
			.SetDisplay("Stop Loss Points", "Stop loss in price points", "Risk Management")
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

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var ema = new ExponentialMovingAverage { Length = KeltnerLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var momentum = new Momentum { Length = MomLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, atr, momentum, ProcessCandle)
			.Start();

		var step = Security?.PriceStep ?? 1m;
		StartProtection(new Unit(), SlPoints > 0m ? new Unit(SlPoints * step, UnitTypes.Absolute) : new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, momentum);
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
		var width = upper - lower;
		if (width == 0m)
			return;

		var keltnerStoch = 100m * (candle.ClosePrice - lower) / width;
		var size = GetContracts();

		if (momentumValue > 0m && keltnerStoch < Threshold && Position <= 0)
			BuyMarket(size + Math.Abs(Position));
		else if (momentumValue < 0m && keltnerStoch > Threshold && Position >= 0)
			SellMarket(size + Math.Abs(Position));
		else if (Position > 0 && keltnerStoch > Threshold)
			SellMarket(Position);
		else if (Position < 0 && keltnerStoch < Threshold)
			BuyMarket(-Position);
	}

	private decimal GetContracts()
	{
		var contracts = (decimal)BaseContracts;

		if (EnableScaling)
		{
			// Every full EquityStep of equity above InitialCapital adds one contract, losses take them away.
			var equity = InitialCapital + PnL;
			var steps = Math.Floor((equity - InitialCapital) / EquityStep);
			contracts = Math.Max(BaseContracts, BaseContracts + steps);
		}

		return Math.Min(contracts, MaxContracts);
	}
}
