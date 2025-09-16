using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy using Stochastic Oscillator crossovers similar to the iStochKomposter MQL expert.
/// Opens long when %K crosses above the lower level and opens short when crossing below the upper level.
/// Existing opposite positions are closed on new signals.
/// </summary>
public class StochKomposterStrategy : Strategy
{
	private readonly StrategyParam<int> _kPeriod;
	private readonly StrategyParam<int> _dPeriod;
	private readonly StrategyParam<decimal> _upLevel;
	private readonly StrategyParam<decimal> _downLevel;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<decimal> _takeProfit;

	private decimal? _prevK;

	/// <summary>
	/// Length of %K line.
	/// </summary>
	public int KPeriod
	{
		get => _kPeriod.Value;
		set => _kPeriod.Value = value;
	}

	/// <summary>
	/// Length of %D line.
	/// </summary>
	public int DPeriod
	{
		get => _dPeriod.Value;
		set => _dPeriod.Value = value;
	}

	/// <summary>
	/// Level above which market is considered overbought.
	/// </summary>
	public decimal UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Level below which market is considered oversold.
	/// </summary>
	public decimal DownLevel
	{
		get => _downLevel.Value;
		set => _downLevel.Value = value;
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
	/// Absolute stop loss in price units.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Absolute take profit in price units.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="StochKomposterStrategy"/>.
	/// </summary>
	public StochKomposterStrategy()
	{
		_kPeriod = Param(nameof(KPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("K Period", "Stochastic %K calculation period", "Indicators");

		_dPeriod = Param(nameof(DPeriod), 3)
			.SetGreaterThanZero()
			.SetDisplay("D Period", "Stochastic %D smoothing period", "Indicators");

		_upLevel = Param(nameof(UpLevel), 70m)
			.SetDisplay("Upper Level", "Overbought threshold", "Indicators");

		_downLevel = Param(nameof(DownLevel), 30m)
			.SetDisplay("Lower Level", "Oversold threshold", "Indicators");

		_stopLoss = Param(nameof(StopLoss), 1000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Absolute stop loss", "Risk");

		_takeProfit = Param(nameof(TakeProfit), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Absolute take profit", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var stochastic = new StochasticOscillator
		{
			K = { Length = KPeriod },
			D = { Length = DPeriod },
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(stochastic, ProcessCandle).Start();

		StartProtection(
			stopLoss: new Unit(StopLoss, UnitTypes.Absolute),
			takeProfit: new Unit(TakeProfit, UnitTypes.Absolute));

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, stochastic);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue stochValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var value = (StochasticOscillatorValue)stochValue;
		var k = value.K;

		if (_prevK is null)
		{
			_prevK = k;
			return;
		}

		var prev = _prevK.Value;

		var buySignal = prev <= DownLevel && k > DownLevel;
		var sellSignal = prev >= UpLevel && k < UpLevel;

		if (buySignal)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (sellSignal)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}

		_prevK = k;
	}
}
