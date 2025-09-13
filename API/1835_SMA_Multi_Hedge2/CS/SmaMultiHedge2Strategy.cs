using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-currency hedging strategy based on SMA trend and correlation.
/// </summary>
public class SmaMultiHedge2Strategy : Strategy
{
	private readonly StrategyParam<Security> _hedgeSecurity;
	private readonly StrategyParam<int> _smaPeriod;
	private readonly StrategyParam<int> _correlationPeriod;
	private readonly StrategyParam<decimal> _expectedCorrelation;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<bool> _followBase;

	private SimpleMovingAverage _directionSma;
	private SimpleMovingAverage _corrBaseSma;
	private SimpleMovingAverage _corrHedgeSma;

	private readonly Queue<decimal> _baseDiffs = [];
	private readonly Queue<decimal> _hedgeDiffs = [];

	private decimal _prevSma1;
	private decimal _prevSma2;

	/// <summary>Security used for hedging.</summary>
	public Security HedgeSecurity
	{
		get => _hedgeSecurity.Value;
		set => _hedgeSecurity.Value = value;
	}

	/// <summary>SMA period for trend detection.</summary>
	public int SmaPeriod
	{
		get => _smaPeriod.Value;
		set => _smaPeriod.Value = value;
	}

	/// <summary>Correlation calculation period.</summary>
	public int CorrelationPeriod
	{
		get => _correlationPeriod.Value;
		set => _correlationPeriod.Value = value;
	}

	/// <summary>Expected correlation threshold.</summary>
	public decimal ExpectedCorrelation
	{
		get => _expectedCorrelation.Value;
		set => _expectedCorrelation.Value = value;
	}

	/// <summary>Profit target in money.</summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>Candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Hedge direction follows base security when true.</summary>
	public bool FollowBase
	{
		get => _followBase.Value;
		set => _followBase.Value = value;
	}

	public SmaMultiHedge2Strategy()
	{
		_hedgeSecurity = Param<Security>(nameof(HedgeSecurity));
		_smaPeriod = Param(nameof(SmaPeriod), 20)
			.SetDisplay("SMA Period", "Period for trend SMA", "Parameters")
			.SetCanOptimize(true);
		_correlationPeriod = Param(nameof(CorrelationPeriod), 20)
			.SetDisplay("Correlation Period", "Period for correlation", "Parameters")
			.SetCanOptimize(true);
		_expectedCorrelation = Param(nameof(ExpectedCorrelation), 0.8m)
			.SetDisplay("Expected Correlation", "Threshold for hedge activation", "Parameters")
			.SetCanOptimize(true);
		_profitTarget = Param(nameof(ProfitTarget), 30m)
			.SetDisplay("Profit Target", "Take profit value", "Parameters");
		_candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(1)))
			.SetDisplay("Candle Type", "Candles for analysis", "Parameters");
		_followBase = Param(nameof(FollowBase), true)
			.SetDisplay("Follow Base", "Hedge direction follows base", "Parameters");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_directionSma = new SimpleMovingAverage { Length = SmaPeriod };
		_corrBaseSma = new SimpleMovingAverage { Length = CorrelationPeriod };
		_corrHedgeSma = new SimpleMovingAverage { Length = CorrelationPeriod };

		// Subscribe to base security candles
		var baseSub = SubscribeCandles(CandleType);
		baseSub
			.Bind(_directionSma, _corrBaseSma, ProcessBase)
			.Start();

		// Subscribe to hedge security candles
		if (HedgeSecurity != null)
		{
			var hedgeSub = SubscribeCandles(CandleType, security: HedgeSecurity);
			hedgeSub
				.Bind(_corrHedgeSma, ProcessHedge)
				.Start();
		}

		// Enable profit protection
		StartProtection(takeProfit: new Unit(ProfitTarget, UnitTypes.Currency));
	}

	private void ProcessBase(ICandleMessage candle, decimal directionSma, decimal corrSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Determine trend using three SMA values
		var trend = 0;
		if (_prevSma2 < _prevSma1 && _prevSma1 < directionSma)
			trend = 1;
		else if (_prevSma2 > _prevSma1 && _prevSma1 > directionSma)
			trend = -1;

		_prevSma2 = _prevSma1;
		_prevSma1 = directionSma;

		// Update base differences for correlation
		var diff = candle.ClosePrice - corrSma;
		_baseDiffs.Enqueue(diff);
		while (_baseDiffs.Count > CorrelationPeriod)
			_baseDiffs.Dequeue();

		// Execute trades when data ready
		if (_baseDiffs.Count == CorrelationPeriod && _hedgeDiffs.Count == CorrelationPeriod)
		{
			var corr = CalculateCorrelation();
			if (Math.Abs(corr) >= ExpectedCorrelation)
				ExecuteTrades(trend, corr);
		}
		else if (_hedgeDiffs.Count == 0)
		{
			// Trade only base when hedge not ready
			TradeBase(trend);
		}
	}

	private void ProcessHedge(ICandleMessage candle, decimal corrSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var diff = candle.ClosePrice - corrSma;
		_hedgeDiffs.Enqueue(diff);
		while (_hedgeDiffs.Count > CorrelationPeriod)
			_hedgeDiffs.Dequeue();
	}

	private void TradeBase(int trend)
	{
		if (trend == 1 && Position <= 0)
			BuyMarket();
		else if (trend == -1 && Position >= 0)
			SellMarket();
	}

	private void ExecuteTrades(int trend, decimal corr)
	{
		if (trend == 0 || HedgeSecurity == null)
		{
			TradeBase(trend);
			return;
		}

		// Base trade
		TradeBase(trend);

		// Hedge trade direction
		var sameDirection = FollowBase == (corr > 0);

		if (sameDirection)
		{
			if (trend == 1)
				BuyMarket(HedgeSecurity);
			else if (trend == -1)
				SellMarket(HedgeSecurity);
		}
		else
		{
			if (trend == 1)
				SellMarket(HedgeSecurity);
			else if (trend == -1)
				BuyMarket(HedgeSecurity);
		}
	}

	private decimal CalculateCorrelation()
	{
		var baseArray = _baseDiffs.ToArray();
		var hedgeArray = _hedgeDiffs.ToArray();

		decimal sumBase = 0;
		decimal sumHedge = 0;
		decimal sumBaseSq = 0;
		decimal sumHedgeSq = 0;
		decimal sumProduct = 0;

		for (var i = 0; i < baseArray.Length; i++)
		{
			var x = baseArray[i];
			var y = hedgeArray[i];
			sumBase += x;
			sumHedge += y;
			sumBaseSq += x * x;
			sumHedgeSq += y * y;
			sumProduct += x * y;
		}

		var count = CorrelationPeriod;
		var numerator = count * sumProduct - sumBase * sumHedge;
		var denominator = Math.Sqrt((double)((count * sumBaseSq - sumBase * sumBase) * (count * sumHedgeSq - sumHedge * sumHedge)));

		if (denominator == 0)
			return 0;

		return (decimal)(numerator / denominator);
	}
}

