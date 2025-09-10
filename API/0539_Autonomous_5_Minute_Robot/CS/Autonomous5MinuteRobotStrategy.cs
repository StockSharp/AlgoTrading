using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Autonomous 5-Minute Robot strategy based on trend and volume imbalance.
/// </summary>
public class Autonomous5MinuteRobotStrategy : Strategy
{
	private const int Lookback = 6;

	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _volumeLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _sma;
	private Shift _shift;

	/// <summary>
	/// Moving average length for trend detection.
	/// </summary>
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }

	/// <summary>
	/// Volume lookback length (unused).
	/// </summary>
	public int VolumeLength { get => _volumeLength.Value; set => _volumeLength.Value = value; }

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public Autonomous5MinuteRobotStrategy()
	{
		_maLength = Param(nameof(MaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Trend MA Length", "Moving average length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 5);

		_volumeLength = Param(nameof(VolumeLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("Volume Length", "Volume lookback length", "Parameters")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 1);

		_stopLossPercent = Param(nameof(StopLossPercent), 3m)
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 29m)
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(10m, 50m, 5m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for strategy", "General");
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

		_sma = new SimpleMovingAverage { Length = MaLength };
		_shift = new Shift { Length = Lookback };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_sma, _shift, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}

		StartProtection(
			new Unit(TakeProfitPercent, UnitTypes.Percent),
			new Unit(StopLossPercent, UnitTypes.Percent));
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue, decimal prevClose)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_sma.IsFormed || !_shift.IsFormed)
			return;

		var isBullish = candle.ClosePrice > maValue;
		var isBearish = candle.ClosePrice < maValue;

		var buyVolume = candle.HighPrice != candle.LowPrice
			? candle.TotalVolume * (candle.ClosePrice - candle.LowPrice) / (candle.HighPrice - candle.LowPrice)
			: 0m;
		var sellVolume = candle.HighPrice != candle.LowPrice
			? candle.TotalVolume * (candle.HighPrice - candle.ClosePrice) / (candle.HighPrice - candle.LowPrice)
			: 0m;

		var uptrend = candle.ClosePrice > prevClose && isBullish;
		var downtrend = candle.ClosePrice < prevClose && isBearish;

		var longCondition = uptrend && buyVolume > sellVolume;
		var shortCondition = downtrend && sellVolume > buyVolume;

		if (longCondition && Position <= 0)
		{
			if (Position < 0)
				BuyMarket(Position.Abs());
			BuyMarket(Volume);
		}
		else if (shortCondition && Position >= 0)
		{
			if (Position > 0)
				SellMarket(Position);
			SellMarket(Volume);
		}
	}
}
