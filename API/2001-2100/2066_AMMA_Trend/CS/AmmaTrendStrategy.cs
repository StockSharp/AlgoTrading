using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// AMMA Trend strategy using Modified Moving Average direction changes.
/// Buys when MMA turns up, sells when MMA turns down.
/// </summary>
public class AmmaTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _maPeriod;

	private decimal? _mma0;
	private decimal? _mma1;
	private decimal? _mma2;
	private decimal? _mma3;
	private readonly StrategyParam<bool> _allowLongEntry, _allowShortEntry, _allowLongExit, _allowShortExit;
	private Order _pending;

	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }
	public int MaPeriod { get => _maPeriod.Value; set => _maPeriod.Value = value; }

	public bool AllowLongEntry { get => _allowLongEntry.Value; set => _allowLongEntry.Value = value; }
	public bool AllowShortEntry { get => _allowShortEntry.Value; set => _allowShortEntry.Value = value; }
	public bool AllowLongExit { get => _allowLongExit.Value; set => _allowLongExit.Value = value; }
	public bool AllowShortExit { get => _allowShortExit.Value; set => _allowShortExit.Value = value; }
	public AmmaTrendStrategy()
	{
		_allowLongEntry = Param(nameof(AllowLongEntry), true);
		_allowShortEntry = Param(nameof(AllowShortEntry), true);
		_allowLongExit = Param(nameof(AllowLongExit), true);
		_allowShortExit = Param(nameof(AllowShortExit), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use for analysis", "General");

		_maPeriod = Param(nameof(MaPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("AMMA Period", "Period of the modified moving average", "Indicator");
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
		_mma0 = _mma1 = _mma2 = _mma3 = null;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_pending = null;

		var mma = new SmoothedMovingAverage { Length = MaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(mma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, mma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal mmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_mma3 = _mma2;
		_mma2 = _mma1;
		_mma1 = _mma0;
		_mma0 = mmaValue;

		if (_mma1 is null || _mma2 is null || _mma3 is null)
			return;

		var rising = _mma2 < _mma3 && _mma1 > _mma2 && _mma0 > _mma1;
		var falling = _mma2 > _mma3 && _mma1 < _mma2 && _mma0 < _mma1;
		if (!IsFormedAndOnlineAndAllowTrading() || (_pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed))) return;
		if (rising)
		{
			if (Position < 0m && AllowShortExit) _pending = BuyMarket(-Position + (AllowLongEntry ? Volume : 0m));
			else if (Position == 0m && AllowLongEntry) _pending = BuyMarket(Volume);
		}
		else if (falling)
		{
			if (Position > 0m && AllowLongExit) _pending = SellMarket(Position + (AllowShortEntry ? Volume : 0m));
			else if (Position == 0m && AllowShortEntry) _pending = SellMarket(Volume);
		}
	}
}
