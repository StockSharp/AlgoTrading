namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Strategy based on two moving averages and directional movement index.
/// Long when fast MA is above slow MA and +DI exceeds -DI.
/// Short when fast MA is below slow MA and -DI exceeds +DI.
/// </summary>
public class X2MADigitDm361Strategy : Strategy
{
	private readonly StrategyParam<int> _fastMaLength;
	private readonly StrategyParam<int> _slowMaLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;

	/// <summary>
	/// Fast moving average length.
	/// </summary>
	public int FastMaLength
	{
		get => _fastMaLength.Value;
		set => _fastMaLength.Value = value;
	}

	/// <summary>
	/// Slow moving average length.
	/// </summary>
	public int SlowMaLength
	{
		get => _slowMaLength.Value;
		set => _slowMaLength.Value = value;
	}

	/// <summary>
	/// ADX calculation length.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
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
	/// Stop loss percent.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percent.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="X2MADigitDm361Strategy"/>.
	/// </summary>
	public X2MADigitDm361Strategy()
	{
		_fastMaLength = Param(nameof(FastMaLength), 12)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Length of fast moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(5, 25, 1);

		_slowMaLength = Param(nameof(SlowMaLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Length of slow moving average", "Moving Averages")
			.SetCanOptimize(true)
			.SetOptimize(3, 15, 1);

		_adxLength = Param(nameof(AdxLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "Length of Average Directional Index", "Directional Movement")
			.SetCanOptimize(true)
			.SetOptimize(7, 28, 7);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candles timeframe", "General");

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(0.5m, 3m, 0.5m);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk Management")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 0.5m);
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

		var fastMa = new SimpleMovingAverage { Length = FastMaLength };
		var slowMa = new SimpleMovingAverage { Length = SlowMaLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.BindEx(fastMa, slowMa, adx, ProcessCandle)
			.Start();

		StartProtection(
			takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
			stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
			isStopTrailing: false,
			useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawIndicator(area, adx);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastMaValue, IIndicatorValue slowMaValue, IIndicatorValue adxValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!fastMaValue.IsFinal || !slowMaValue.IsFinal || !adxValue.IsFinal)
			return;

		var fast = fastMaValue.ToDecimal();
		var slow = slowMaValue.ToDecimal();
		var adx = (AverageDirectionalIndexValue)adxValue;

		if (fast > slow && adx.PlusDi > adx.MinusDi && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (fast < slow && adx.MinusDi > adx.PlusDi && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
	}
}

