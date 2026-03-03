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

	private ExponentialMovingAverage _shortMa;
	private ExponentialMovingAverage _longMa;
	private decimal _prevShort;
	private decimal _prevLong;
	private bool _initialized;
	private decimal _entryPrice;
	private int _cooldown;

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
		_stopLossPercent = Param(nameof(StopLossPercent), 7m).SetGreaterThanZero()
			.SetDisplay("SL %", "Stop loss percent", "Risk");
		_takeProfitPercent = Param(nameof(TakeProfitPercent), 10m).SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percent", "Risk");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(20).TimeFrame())
			.SetDisplay("Candle Type", "Candles", "General");
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

		_prevShort = default;
		_prevLong = default;
		_initialized = false;
		_entryPrice = default;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_shortMa = new ExponentialMovingAverage { Length = ShortMaLength };
		_longMa = new ExponentialMovingAverage { Length = LongMaLength };

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

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevShort = shortMa;
			_prevLong = longMa;
			return;
		}

		var crossUp = _prevShort <= _prevLong && shortMa > longMa;
		var crossDown = _prevShort >= _prevLong && shortMa < longMa;

		if (crossUp && Position <= 0)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_cooldown = 10;
		}
		else if (crossDown && Position >= 0)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_cooldown = 10;
		}

		// SL/TP for long
		if (Position > 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m - StopLossPercent / 100m);
			var tp = _entryPrice * (1m + TakeProfitPercent / 100m);
			if (candle.ClosePrice <= sl || candle.ClosePrice >= tp)
			{
				SellMarket();
				_entryPrice = 0;
				_cooldown = 15;
			}
		}
		// SL/TP for short
		else if (Position < 0 && _entryPrice > 0)
		{
			var sl = _entryPrice * (1m + StopLossPercent / 100m);
			var tp = _entryPrice * (1m - TakeProfitPercent / 100m);
			if (candle.ClosePrice >= sl || candle.ClosePrice <= tp)
			{
				BuyMarket();
				_entryPrice = 0;
				_cooldown = 15;
			}
		}

		_prevShort = shortMa;
		_prevLong = longMa;
	}
}
