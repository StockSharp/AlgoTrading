using System;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA cloud crossover strategy that trades long when the short EMA crosses above the long EMA.
/// </summary>
public class LongOnlyMtfEmaCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _shortEma;
	private ExponentialMovingAverage _longEma;
	private decimal _prevShort;
	private decimal _prevLong;
	private decimal _stopPrice;
	private decimal _takeProfit;
	private bool _isInitialized;

	public LongOnlyMtfEmaCloudStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA Length", "Period of the short EMA", "EMA Cloud Settings")
			.SetCanOptimize(true);

		_longLength = Param(nameof(LongLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA Length", "Period of the long EMA", "EMA Cloud Settings")
			.SetCanOptimize(true);

		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
			.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
			.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for EMAs", "General");
	}

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public override System.Collections.Generic.IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_prevShort = 0m;
		_prevLong = 0m;
		_stopPrice = 0m;
		_takeProfit = 0m;
		_isInitialized = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_shortEma = new ExponentialMovingAverage { Length = ShortLength };
		_longEma = new ExponentialMovingAverage { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortEma);
			DrawIndicator(area, _longEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortValue, decimal longValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_shortEma.IsFormed || !_longEma.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevShort = shortValue;
			_prevLong = longValue;
			_isInitialized = true;
			return;
		}

		var crossedUp = _prevShort <= _prevLong && shortValue > longValue;

		if (crossedUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));

			var entry = candle.ClosePrice;
			var stopOffset = StopLossPercent / 100m;
			var takeOffset = TakeProfitPercent / 100m;

			_stopPrice = entry * (1m - stopOffset);
			_takeProfit = entry * (1m + takeOffset);
		}

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
				SellMarket(Position);
			else if (candle.HighPrice >= _takeProfit)
				SellMarket(Position);
		}

		_prevShort = shortValue;
		_prevLong = longValue;
	}
}
