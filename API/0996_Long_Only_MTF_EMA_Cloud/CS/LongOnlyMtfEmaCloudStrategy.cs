using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA cloud crossover strategy that trades long when short EMA crosses above long EMA.
/// </summary>
public class LongOnlyMtfEmaCloudStrategy : Strategy
{
	private readonly StrategyParam<int> _shortLength;
	private readonly StrategyParam<int> _longLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _shortEma;
	private EMA _longEma;
	private decimal _prevShort;
	private decimal _prevLong;
	private decimal _stopPrice;
	private decimal _takeProfit;
	private bool _isInitialized;

	public int ShortLength { get => _shortLength.Value; set => _shortLength.Value = value; }
	public int LongLength { get => _longLength.Value; set => _longLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LongOnlyMtfEmaCloudStrategy()
	{
		_shortLength = Param(nameof(ShortLength), 21)
			.SetGreaterThanZero()
			.SetDisplay("Short EMA", "Short EMA period", "Indicators");
		_longLength = Param(nameof(LongLength), 50)
			.SetGreaterThanZero()
			.SetDisplay("Long EMA", "Long EMA period", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Loss %", "Stop loss percent", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Take profit percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = 0;
		_prevLong = 0;
		_isInitialized = false;

		_shortEma = new EMA { Length = ShortLength };
		_longEma = new EMA { Length = LongLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortEma, _longEma, ProcessCandle)
			.Start();

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
			_stopPrice = entry * (1m - StopLossPercent / 100m);
			_takeProfit = entry * (1m + TakeProfitPercent / 100m);
		}

		if (Position > 0)
		{
			if (candle.ClosePrice <= _stopPrice || candle.ClosePrice >= _takeProfit)
				SellMarket(Math.Abs(Position));
		}

		_prevShort = shortValue;
		_prevLong = longValue;
	}
}
