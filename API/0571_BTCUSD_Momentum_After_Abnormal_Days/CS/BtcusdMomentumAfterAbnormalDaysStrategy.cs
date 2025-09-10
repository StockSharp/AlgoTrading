using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades in the direction of abnormal daily returns.
/// </summary>
public class BtcusdMomentumAfterAbnormalDaysStrategy : Strategy
{
	private readonly StrategyParam<int> _lookbackPeriod;
	private readonly StrategyParam<decimal> _kFactor;
	private readonly StrategyParam<decimal> _capitalPerTrade;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private StandardDeviation _stdDev;
	private bool _positionOpened;

	/// <summary>
	/// Lookback period for mean and standard deviation.
	/// </summary>
	public int LookbackPeriod
	{
		get => _lookbackPeriod.Value;
		set => _lookbackPeriod.Value = value;
	}

	/// <summary>
	/// Multiplier for standard deviation threshold.
	/// </summary>
	public decimal KFactor
	{
		get => _kFactor.Value;
		set => _kFactor.Value = value;
	}

	/// <summary>
	/// Capital allocated per trade.
	/// </summary>
	public decimal CapitalPerTrade
	{
		get => _capitalPerTrade.Value;
		set => _capitalPerTrade.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public BtcusdMomentumAfterAbnormalDaysStrategy()
	{
		_lookbackPeriod = Param(nameof(LookbackPeriod), 5)
			.SetGreaterThanZero()
			.SetDisplay("Lookback", "Lookback period", "General")
			.SetCanOptimize(true)
			.SetOptimize(3, 10, 1);

		_kFactor = Param(nameof(KFactor), 1.6m)
			.SetGreaterThanZero()
			.SetDisplay("K", "Standard deviation multiplier", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.1m);

		_capitalPerTrade = Param(nameof(CapitalPerTrade), 1000m)
			.SetGreaterThanZero()
			.SetDisplay("Capital", "Capital per trade", "General")
			.SetCanOptimize(true)
			.SetOptimize(500m, 5000m, 500m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
			.SetDisplay("Candle", "Candle type", "General");
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

		_sma = new SimpleMovingAverage { Length = LookbackPeriod };
		_stdDev = new StandardDeviation { Length = LookbackPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_positionOpened)
		{
			if (Position > 0)
				SellMarket(Position);
			else if (Position < 0)
				BuyMarket(Math.Abs(Position));

			_positionOpened = false;
		}

		var dayReturn = (candle.ClosePrice - candle.OpenPrice) / candle.OpenPrice * 100m;

		var meanValue = _sma.Process(dayReturn);
		var stdValue = _stdDev.Process(dayReturn);

		if (!meanValue.IsFinal || !stdValue.IsFinal)
			return;

		var mean = meanValue.ToDecimal();
		var std = stdValue.ToDecimal();

		var upper = mean + KFactor * std;
		var lower = mean - KFactor * std;

		var volume = CapitalPerTrade / candle.ClosePrice;

		if (dayReturn > upper && Position <= 0)
		{
			BuyMarket(volume + Math.Abs(Position));
			_positionOpened = true;
		}
		else if (dayReturn < lower && Position >= 0)
		{
			SellMarket(volume + Math.Abs(Position));
			_positionOpened = true;
		}
	}
}
