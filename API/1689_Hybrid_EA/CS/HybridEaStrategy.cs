using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hybrid EA strategy based on the Relative Vigor Index.
/// Enters when the difference between RVI and its signal exceeds a threshold and uses fixed take profit and stop loss.
/// </summary>
public class HybridEaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<int> _rviLength;
	private readonly StrategyParam<int> _signalLength;
	private readonly StrategyParam<decimal> _differenceThreshold;
	private readonly StrategyParam<decimal> _takeProfit;
	private readonly StrategyParam<decimal> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeVigorIndex _rvi;
	private SimpleMovingAverage _signal;
	private decimal? _prevDiff;

	/// <summary>
	/// Trade volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Length for RVI calculation.
	/// </summary>
	public int RviLength
	{
		get => _rviLength.Value;
		set => _rviLength.Value = value;
	}

	/// <summary>
	/// Length for RVI signal line.
	/// </summary>
	public int SignalLength
	{
		get => _signalLength.Value;
		set => _signalLength.Value = value;
	}

	/// <summary>
	/// Difference threshold between RVI and signal line.
	/// </summary>
	public decimal DifferenceThreshold
	{
		get => _differenceThreshold.Value;
		set => _differenceThreshold.Value = value;
	}

	/// <summary>
	/// Take profit in price points.
	/// </summary>
	public decimal TakeProfit
	{
		get => _takeProfit.Value;
		set => _takeProfit.Value = value;
	}

	/// <summary>
	/// Stop loss in price points.
	/// </summary>
	public decimal StopLoss
	{
		get => _stopLoss.Value;
		set => _stopLoss.Value = value;
	}

	/// <summary>
	/// Candle type to process.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="HybridEaStrategy"/>.
	/// </summary>
	public HybridEaStrategy()
	{
		_volume = Param(nameof(Volume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Volume", "Trade volume", "General")
			.SetCanOptimize(true);

		_rviLength = Param(nameof(RviLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RVI Length", "Length for RVI", "General")
			.SetCanOptimize(true);

		_signalLength = Param(nameof(SignalLength), 4)
			.SetGreaterThanZero()
			.SetDisplay("Signal Length", "Length for signal line", "General")
			.SetCanOptimize(true);

		_differenceThreshold = Param(nameof(DifferenceThreshold), 0.05m)
			.SetGreaterThanZero()
			.SetDisplay("Difference", "RVI difference threshold", "General")
			.SetCanOptimize(true);

		_takeProfit = Param(nameof(TakeProfit), 18m)
			.SetDisplay("Take Profit", "Take profit in points", "General")
			.SetCanOptimize(true);

		_stopLoss = Param(nameof(StopLoss), 9m)
			.SetDisplay("Stop Loss", "Stop loss in points", "General")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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

		_rvi = default;
		_signal = default;
		_prevDiff = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_rvi = new RelativeVigorIndex { Length = RviLength };
		_signal = new SimpleMovingAverage { Length = SignalLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(_rvi, _signal, ProcessCandle).Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _rvi);
			DrawIndicator(area, _signal);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rvi, decimal signal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var diff = rvi - signal;

		if (_prevDiff <= DifferenceThreshold && diff > DifferenceThreshold && Position <= 0)
		{
			var vol = Volume + (Position < 0 ? -Position : 0m);
			BuyMarket(vol);
		}
		else if (_prevDiff >= -DifferenceThreshold && diff < -DifferenceThreshold && Position >= 0)
		{
			var vol = Volume + (Position > 0 ? Position : 0m);
			SellMarket(vol);
		}

		var step = Security.PriceStep ?? 1m;

		if (Position > 0 && PositionPrice is decimal longEntry)
		{
			var profit = (candle.ClosePrice - longEntry) / step;
			if (TakeProfit > 0m && profit >= TakeProfit)
				SellMarket(Position);
			else if (StopLoss > 0m && profit <= -StopLoss)
				SellMarket(Position);
		}
		else if (Position < 0 && PositionPrice is decimal shortEntry)
		{
			var profit = (shortEntry - candle.ClosePrice) / step;
			if (TakeProfit > 0m && profit >= TakeProfit)
				BuyMarket(-Position);
			else if (StopLoss > 0m && profit <= -StopLoss)
				BuyMarket(-Position);
		}

		_prevDiff = diff;
	}
}
