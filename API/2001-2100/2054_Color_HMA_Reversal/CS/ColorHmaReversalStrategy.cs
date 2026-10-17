using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that trades on Hull Moving Average slope reversals.
/// Buys when HMA turns up, sells when HMA turns down.
/// </summary>
public class ColorHmaReversalStrategy : Strategy
{
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevValue1;
	private decimal _prevValue2;
	private int _count;
	private readonly StrategyParam<bool> _buyOpen, _sellOpen, _buyClose, _sellClose;
	private Order _pending;

	public int HmaPeriod { get => _hmaPeriod.Value; set => _hmaPeriod.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public bool BuyOpen { get => _buyOpen.Value; set => _buyOpen.Value = value; }
	public bool SellOpen { get => _sellOpen.Value; set => _sellOpen.Value = value; }
	public bool BuyClose { get => _buyClose.Value; set => _buyClose.Value = value; }
	public bool SellClose { get => _sellClose.Value; set => _sellClose.Value = value; }
	public ColorHmaReversalStrategy()
	{
		_buyOpen = Param(nameof(BuyOpen), true);
		_sellOpen = Param(nameof(SellOpen), true);
		_buyClose = Param(nameof(BuyClose), true);
		_sellClose = Param(nameof(SellClose), true);
		_hmaPeriod = Param(nameof(HmaPeriod), 13)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
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
		_prevValue1 = default;
		_prevValue2 = default;
		_count = default;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_count = 0;
		_pending = null;

		var hma = new HullMovingAverage { Length = HmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(hma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal hmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_count++;

		if (_count <= 2)
		{
			_prevValue2 = _prevValue1;
			_prevValue1 = hmaValue;
			return;
		}

		var wasFalling = _prevValue1 < _prevValue2;
		var wasRising = _prevValue1 > _prevValue2;
		var nowRising = hmaValue > _prevValue1;
		var nowFalling = hmaValue < _prevValue1;

		if (IsFormedAndOnlineAndAllowTrading() && (_pending is null || _pending.State is OrderStates.Done or OrderStates.Failed))
		{
			if (wasFalling && nowRising)
			{
				if (Position < 0m && SellClose) _pending = BuyMarket(-Position + (BuyOpen ? Volume : 0m));
				else if (Position == 0m && BuyOpen) _pending = BuyMarket(Volume);
			}
			else if (wasRising && nowFalling)
			{
				if (Position > 0m && BuyClose) _pending = SellMarket(Position + (SellOpen ? Volume : 0m));
				else if (Position == 0m && SellOpen) _pending = SellMarket(Volume);
			}
		}

		_prevValue2 = _prevValue1;
		_prevValue1 = hmaValue;
	}
}
