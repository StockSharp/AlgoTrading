using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA crossover with demand/supply zone proximity and SL/TP.
/// </summary>
public class MaCrossoverDemandSupplyZonesSltpStrategy : Strategy
{
	private readonly StrategyParam<int> _shortMaLength;
	private readonly StrategyParam<int> _longMaLength;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private EMA _shortMa;
	private EMA _longMa;
	private decimal _prevShort;
	private decimal _prevLong;
	private bool _initialized;
	private decimal _entryPrice;

	public int ShortMaLength { get => _shortMaLength.Value; set => _shortMaLength.Value = value; }
	public int LongMaLength { get => _longMaLength.Value; set => _longMaLength.Value = value; }
	public decimal StopLossPercent { get => _stopLossPercent.Value; set => _stopLossPercent.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MaCrossoverDemandSupplyZonesSltpStrategy()
	{
		_shortMaLength = Param(nameof(ShortMaLength), 9).SetGreaterThanZero()
			.SetDisplay("Short MA", "Short MA period", "Indicators");
		_longMaLength = Param(nameof(LongMaLength), 21).SetGreaterThanZero()
			.SetDisplay("Long MA", "Long MA period", "Indicators");
		_stopLossPercent = Param(nameof(StopLossPercent), 1m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 2m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevShort = 0;
		_prevLong = 0;
		_initialized = false;
		_entryPrice = 0;

		_shortMa = new EMA { Length = ShortMaLength };
		_longMa = new EMA { Length = LongMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_shortMa, _longMa, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _shortMa);
			DrawIndicator(area, _longMa);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal shortMa, decimal longMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_shortMa.IsFormed || !_longMa.IsFormed)
			return;

		if (!_initialized)
		{
			_prevShort = shortMa;
			_prevLong = longMa;
			_initialized = true;
			return;
		}

		var crossUp = _prevShort <= _prevLong && shortMa > longMa;
		var crossDown = _prevShort >= _prevLong && shortMa < longMa;

		if (crossUp && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}
		else if (crossDown && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_entryPrice = candle.ClosePrice;
		}

		if (Position > 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);
			if (candle.ClosePrice <= sl || candle.ClosePrice >= tp)
				SellMarket(Math.Abs(Position));
		}
		else if (Position < 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);
			if (candle.ClosePrice >= sl || candle.ClosePrice <= tp)
				BuyMarket(Math.Abs(Position));
		}

		_prevShort = shortMa;
		_prevLong = longMa;
	}
}
