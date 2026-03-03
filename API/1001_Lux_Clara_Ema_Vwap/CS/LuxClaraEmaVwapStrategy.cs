using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Lux Clara EMA + VWAP strategy.
/// Buys on fast EMA crossing above slow EMA when above VWAP, sells on opposite.
/// </summary>
public class LuxClaraEmaVwapStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _slowEma;
	private VolumeWeightedMovingAverage _vwap;

	private decimal _prevFast;
	private decimal _prevSlow;
	private bool _isInitialized;
	private int _cooldown;

	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public LuxClaraEmaVwapStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 8)
			.SetDisplay("Fast EMA Length", "Length of fast EMA", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 21)
			.SetDisplay("Slow EMA Length", "Length of slow EMA", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe of data for strategy", "General");
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

		_prevFast = default;
		_prevSlow = default;
		_isInitialized = false;
		_cooldown = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		_vwap = new VolumeWeightedMovingAverage { Length = 20 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_fastEma, _slowEma, _vwap, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma);
			DrawIndicator(area, _slowEma);
			DrawIndicator(area, _vwap);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fast, decimal slow, decimal vwap)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!_isInitialized)
		{
			_prevFast = fast;
			_prevSlow = slow;
			_isInitialized = true;
			return;
		}

		if (_cooldown > 0)
		{
			_cooldown--;
			_prevFast = fast;
			_prevSlow = slow;
			return;
		}

		var fastCrossAbove = _prevFast <= _prevSlow && fast > slow;
		var fastCrossBelow = _prevFast >= _prevSlow && fast < slow;

		// Use VWAP as additional confirmation when formed
		var aboveVwap = !_vwap.IsFormed || candle.ClosePrice > vwap;
		var belowVwap = !_vwap.IsFormed || candle.ClosePrice < vwap;

		if (Position <= 0 && fastCrossAbove && aboveVwap)
		{
			if (Position < 0)
				BuyMarket();
			BuyMarket();
			_cooldown = 12;
		}
		else if (Position >= 0 && fastCrossBelow && belowVwap)
		{
			if (Position > 0)
				SellMarket();
			SellMarket();
			_cooldown = 12;
		}
		// Exit without VWAP condition
		else if (Position > 0 && fastCrossBelow)
		{
			SellMarket();
			_cooldown = 12;
		}
		else if (Position < 0 && fastCrossAbove)
		{
			BuyMarket();
			_cooldown = 12;
		}

		_prevFast = fast;
		_prevSlow = slow;
	}
}
