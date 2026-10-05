using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Combo 2/20 EMA and Bandpass Filter strategy.
/// An Ehlers bandpass filter of the median price is combined with a fast/slow EMA trend: long when the fast EMA is above the slow
/// one and the filter is above BpfSellZone, short when the fast EMA is below the slow one and the filter is below BpfBuyZone.
/// The position closes when neither signal holds and nothing is traded before StartDate.
/// </summary>
public class Combo220EmaBandpassFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _bpfLength;
	private readonly StrategyParam<decimal> _bpfDelta;
	private readonly StrategyParam<decimal> _bpfSellZone;
	private readonly StrategyParam<decimal> _bpfBuyZone;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _price1;
	private decimal? _price2;
	private decimal _bp1;
	private decimal _bp2;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength
	{
		get => _slowEmaLength.Value;
		set => _slowEmaLength.Value = value;
	}

	/// <summary>
	/// Bandpass filter period.
	/// </summary>
	public int BpfLength
	{
		get => _bpfLength.Value;
		set => _bpfLength.Value = value;
	}

	/// <summary>
	/// Bandpass filter bandwidth.
	/// </summary>
	public decimal BpfDelta
	{
		get => _bpfDelta.Value;
		set => _bpfDelta.Value = value;
	}

	/// <summary>
	/// Filter level the long signal has to exceed.
	/// </summary>
	public decimal BpfSellZone
	{
		get => _bpfSellZone.Value;
		set => _bpfSellZone.Value = value;
	}

	/// <summary>
	/// Filter level the short signal has to fall below.
	/// </summary>
	public decimal BpfBuyZone
	{
		get => _bpfBuyZone.Value;
		set => _bpfBuyZone.Value = value;
	}

	/// <summary>
	/// Date trading starts from.
	/// </summary>
	public DateTimeOffset StartDate
	{
		get => _startDate.Value;
		set => _startDate.Value = value;
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
	/// Constructor.
	/// </summary>
	public Combo220EmaBandpassFilterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 2)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "EMA");

		_slowEmaLength = Param(nameof(SlowEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "EMA");

		_bpfLength = Param(nameof(BpfLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("BPF Length", "Bandpass filter period", "Bandpass");

		_bpfDelta = Param(nameof(BpfDelta), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("BPF Delta", "Bandpass filter bandwidth", "Bandpass");

		_bpfSellZone = Param(nameof(BpfSellZone), 5m)
			.SetDisplay("BPF Sell Zone", "Filter level the long signal has to exceed", "Bandpass");

		_bpfBuyZone = Param(nameof(BpfBuyZone), -5m)
			.SetDisplay("BPF Buy Zone", "Filter level the short signal has to fall below", "Bandpass");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(2005, 1, 1, 0, 0, 0, TimeSpan.Zero))
			.SetDisplay("Start Date", "Date trading starts from", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		ResetState();
	}

	private void ResetState()
	{
		_price1 = null;
		_price2 = null;
		_bp1 = 0m;
		_bp2 = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(fastEma, slowEma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal fastEma, decimal slowEma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = (candle.HighPrice + candle.LowPrice) / 2m;
		decimal? bp = null;

		if (_price2 is decimal price2)
		{
			var beta = Math.Cos(Math.PI * (360.0 / BpfLength) / 180.0);
			var gamma = 1.0 / Math.Cos(Math.PI * (720.0 * (double)BpfDelta / BpfLength) / 180.0);
			var alpha = gamma - Math.Sqrt(gamma * gamma - 1.0);

			var value = 0.5 * (1.0 - alpha) * (double)(price - price2) + beta * (1.0 + alpha) * (double)_bp1 - alpha * (double)_bp2;
			bp = (decimal)value;
			_bp2 = _bp1;
			_bp1 = (decimal)value;
		}

		_price2 = _price1;
		_price1 = price;

		if (bp is not decimal filter)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (candle.OpenTime < StartDate.UtcDateTime)
		{
			ClosePosition();
			return;
		}

		var longSignal = fastEma > slowEma && filter > BpfSellZone;
		var shortSignal = fastEma < slowEma && filter < BpfBuyZone;

		if (longSignal)
		{
			if (Position <= 0)
				BuyMarket(Volume + Math.Abs(Position));
		}
		else if (shortSignal)
		{
			if (Position >= 0)
				SellMarket(Volume + Math.Abs(Position));
		}
		else
		{
			ClosePosition();
		}
	}

	private void ClosePosition()
	{
		if (Position > 0)
			SellMarket(Position);
		else if (Position < 0)
			BuyMarket(-Position);
	}
}
