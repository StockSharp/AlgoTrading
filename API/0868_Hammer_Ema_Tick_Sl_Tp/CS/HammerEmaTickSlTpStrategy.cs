using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hammer and inverted hammer strategy with EMA filter and tick-based risk management.
/// </summary>
public class HammerEmaTickSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _stopLossTicks;
	private readonly StrategyParam<int> _takeProfitTicks;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _stopLossDistance;
	private decimal _takeProfitDistance;

	private decimal? _prevClose1;
	private decimal? _prevClose2;

	/// <summary>
	/// EMA period length.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// Stop loss distance in ticks.
	/// </summary>
	public int StopLossTicks
	{
		get => _stopLossTicks.Value;
		set => _stopLossTicks.Value = value;
	}

	/// <summary>
	/// Take profit distance in ticks.
	/// </summary>
	public int TakeProfitTicks
	{
		get => _takeProfitTicks.Value;
		set => _takeProfitTicks.Value = value;
	}

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public HammerEmaTickSlTpStrategy()
	{
		_emaLength = Param(nameof(EmaLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "EMA period length", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(20, 100, 10);

		_stopLossTicks = Param(nameof(StopLossTicks), 1)
			.SetGreaterOrEqualZero()
			.SetDisplay("Stop Loss Ticks", "Stop loss distance in ticks", "Risk");

		_takeProfitTicks = Param(nameof(TakeProfitTicks), 10)
			.SetGreaterOrEqualZero()
			.SetDisplay("Take Profit Ticks", "Take profit distance in ticks", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
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
		_prevClose1 = null;
		_prevClose2 = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var tick = Security?.PriceStep ?? 1m;
		_stopLossDistance = StopLossTicks * tick;
		_takeProfitDistance = TakeProfitTicks * tick;

		StartProtection(
			takeProfit: new Unit(_takeProfitDistance, UnitTypes.Absolute),
			stopLoss: new Unit(_stopLossDistance, UnitTypes.Absolute));

		var ema = new ExponentialMovingAverage { Length = EmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal ema)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (_prevClose1 is null || _prevClose2 is null)
		{
			_prevClose2 = _prevClose1;
			_prevClose1 = candle.ClosePrice;
			return;
		}

		var body = Math.Abs(candle.ClosePrice - candle.OpenPrice);
		var upperWick = candle.HighPrice - Math.Max(candle.ClosePrice, candle.OpenPrice);
		var lowerWick = Math.Min(candle.ClosePrice, candle.OpenPrice) - candle.LowPrice;

		var hammer = lowerWick > body * 2m && upperWick < body * 0.5m && _prevClose1 < _prevClose2 && candle.ClosePrice < _prevClose1;
		var invertedHammer = upperWick > body * 2m && lowerWick < body * 0.5m && _prevClose1 > _prevClose2 && candle.ClosePrice > _prevClose1;

		if (hammer && candle.ClosePrice > ema && Position <= 0)
			BuyMarket();
		else if (invertedHammer && candle.ClosePrice < ema && Position >= 0)
			SellMarket();

		_prevClose2 = _prevClose1;
		_prevClose1 = candle.ClosePrice;
	}
}
