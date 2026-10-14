using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Crunchster's Normalised Trend strategy.
/// Each close-to-close return is divided by the standard deviation of the last NormPeriod returns and the results are summed
/// into a normalised price. A cross of the normalised price above its Hull moving average (taken HmaOffset bars back) goes long
/// and a cross below goes short, reversing an opposite position. A stop StopMultiple ATRs (NormPeriod long) from the entry
/// closes a losing trade.
/// </summary>
public class CrunchstersNormalisedTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _normPeriod;
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _hmaOffset;
	private readonly StrategyParam<decimal> _stopMultiple;
	private readonly StrategyParam<DataType> _candleType;

	private readonly List<decimal> _hmaHistory = [];
	private StandardDeviation _returnDeviation;
	private HullMovingAverage _hma;
	private decimal? _prevClose;
	private decimal _normalizedPrice;
	private decimal? _prevPrice;
	private decimal? _prevHma;
	private decimal? _stopPrice;

	/// <summary>
	/// Period of the return standard deviation.
	/// </summary>
	public int NormPeriod
	{
		get => _normPeriod.Value;
		set => _normPeriod.Value = value;
	}

	/// <summary>
	/// Hull moving average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Bars the Hull moving average is shifted back.
	/// </summary>
	public int HmaOffset
	{
		get => _hmaOffset.Value;
		set => _hmaOffset.Value = value;
	}

	/// <summary>
	/// ATR multiple for the stop.
	/// </summary>
	public decimal StopMultiple
	{
		get => _stopMultiple.Value;
		set => _stopMultiple.Value = value;
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
	public CrunchstersNormalisedTrendStrategy()
	{
		_normPeriod = Param(nameof(NormPeriod), 14)
			.SetRange(2, 10000)
			.SetDisplay("Norm Period", "Period of the return standard deviation", "Indicators");

		_hmaPeriod = Param(nameof(HmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull moving average period", "Indicators");

		_hmaOffset = Param(nameof(HmaOffset), 0)
			.SetNotNegative()
			.SetDisplay("HMA Offset", "Bars the Hull moving average is shifted back", "Indicators");

		_stopMultiple = Param(nameof(StopMultiple), 1m)
			.SetNotNegative()
			.SetDisplay("Stop Multiple", "ATR multiple for the stop", "Risk");

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
		_returnDeviation = null;
		_hma = null;
	}

	private void ResetState()
	{
		_hmaHistory.Clear();
		_prevClose = null;
		_normalizedPrice = 0m;
		_prevPrice = null;
		_prevHma = null;
		_stopPrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var atr = new AverageTrueRange { Length = NormPeriod };
		_returnDeviation = new StandardDeviation { Length = NormPeriod };
		_hma = new HullMovingAverage { Length = HmaPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		_prevClose = close;

		if (prevClose is not decimal lastClose || lastClose == 0)
			return;

		var ret = (close - lastClose) / lastClose;
		var deviationValue = _returnDeviation.Process(ret, candle.OpenTime, true);

		if (!deviationValue.IsFormed)
			return;

		var deviation = deviationValue.ToDecimal();
		if (deviation > 0)
			_normalizedPrice += ret / deviation;

		var price = _normalizedPrice;
		var hmaValue = _hma.Process(price, candle.OpenTime, true);

		if (!hmaValue.IsFormed)
			return;

		_hmaHistory.Add(hmaValue.ToDecimal());
		if (_hmaHistory.Count > HmaOffset + 1)
			_hmaHistory.RemoveAt(0);

		if (_hmaHistory.Count <= HmaOffset)
			return;

		var hma = _hmaHistory[0];
		var prevPrice = _prevPrice;
		var prevHma = _prevHma;
		_prevPrice = price;
		_prevHma = hma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0 && _stopPrice is decimal longStop && candle.LowPrice <= longStop)
		{
			SellMarket(Position);
			_stopPrice = null;
			return;
		}

		if (Position < 0 && _stopPrice is decimal shortStop && candle.HighPrice >= shortStop)
		{
			BuyMarket(-Position);
			_stopPrice = null;
			return;
		}

		if (prevPrice is not decimal pp || prevHma is not decimal ph || !atrValue.IsFormed)
			return;

		var stopDistance = atrValue.ToDecimal() * StopMultiple;

		if (pp <= ph && price > hma && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = StopMultiple > 0 ? close - stopDistance : null;
		}
		else if (pp >= ph && price < hma && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = StopMultiple > 0 ? close + stopDistance : null;
		}
	}
}
