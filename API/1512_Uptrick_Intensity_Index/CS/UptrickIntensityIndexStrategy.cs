using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Uptrick Intensity Index strategy.
/// Uses trend intensity index calculated from three moving averages.
/// Buys when TII crosses above its average, sells when it crosses below.
/// </summary>
public class UptrickIntensityIndexStrategy : Strategy
{
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<int> _ma3Length;
	private readonly StrategyParam<int> _tiiMaLength;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _ma1;
	private SimpleMovingAverage _ma2;
	private SimpleMovingAverage _ma3;
	private SimpleMovingAverage _tiiMa;

	private decimal? _prevDiff;

	/// <summary>
	/// Length of first SMA.
	/// </summary>
	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	/// <summary>
	/// Length of second SMA.
	/// </summary>
	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	/// <summary>
	/// Length of third SMA.
	/// </summary>
	public int Ma3Length
	{
		get => _ma3Length.Value;
		set => _ma3Length.Value = value;
	}

	/// <summary>
	/// Length of TII moving average.
	/// </summary>
	public int TiiMaLength
	{
		get => _tiiMaLength.Value;
		set => _tiiMaLength.Value = value;
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
	/// Initializes a new instance of <see cref="UptrickIntensityIndexStrategy"/>.
	/// </summary>
	public UptrickIntensityIndexStrategy()
	{
		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("MA1 Length", "Length of first SMA", "General")
			.SetCanOptimize(true);

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA2 Length", "Length of second SMA", "General")
			.SetCanOptimize(true);

		_ma3Length = Param(nameof(Ma3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("MA3 Length", "Length of third SMA", "General")
			.SetCanOptimize(true);

		_tiiMaLength = Param(nameof(TiiMaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("TII SMA Length", "Length of TII moving average", "General")
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

		_ma1 = default;
		_ma2 = default;
		_ma3 = default;
		_tiiMa = default;
		_prevDiff = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ma1 = new SimpleMovingAverage { Length = Ma1Length };
		_ma2 = new SimpleMovingAverage { Length = Ma2Length };
		_ma3 = new SimpleMovingAverage { Length = Ma3Length };
		_tiiMa = new SimpleMovingAverage { Length = TiiMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.WhenNew(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _tiiMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;

		var ma1Value = _ma1.Process(close);
		var ma2Value = _ma2.Process(close);
		var ma3Value = _ma3.Process(close);

		if (!ma1Value.IsFinal || !ma2Value.IsFinal || !ma3Value.IsFinal)
			return;

		var rel1 = (close - ma1Value.ToDecimal()) / ma1Value.ToDecimal();
		var rel2 = (close - ma2Value.ToDecimal()) / ma2Value.ToDecimal();
		var rel3 = (close - ma3Value.ToDecimal()) / ma3Value.ToDecimal();

		var tii = (rel1 + rel2 + rel3) / 3m * 100m;

		var tiiMaValue = _tiiMa.Process(tii);
		if (!tiiMaValue.IsFinal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var tiiMa = tiiMaValue.ToDecimal();
		var diff = tii - tiiMa;

		if (_prevDiff != null)
		{
			if (_prevDiff <= 0m && diff > 0m && Position <= 0)
			{
				var volume = Volume + (Position < 0 ? -Position : 0m);
				BuyMarket(volume);
			}
			else if (_prevDiff >= 0m && diff < 0m && Position >= 0)
			{
				var volume = Volume + (Position > 0 ? Position : 0m);
				SellMarket(volume);
			}
		}

		_prevDiff = diff;
	}
}
