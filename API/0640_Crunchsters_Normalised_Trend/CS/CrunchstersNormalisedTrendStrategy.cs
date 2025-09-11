using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that uses normalised price and Hull Moving Average.
/// Enters long when normalised price crosses above HMA and short when it crosses below.
/// </summary>
public class CrunchstersNormalisedTrendStrategy : Strategy
{
	private readonly StrategyParam<int> _normPeriod;
	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<int> _hmaOffset;
	private readonly StrategyParam<decimal> _stopMultiple;
	private readonly StrategyParam<bool> _useLong;
	private readonly StrategyParam<bool> _useShort;
	private readonly StrategyParam<DataType> _candleType;

	private readonly StandardDeviation _returnsStdDev = new();
	private readonly HullMovingAverage _hma = new();
	private readonly AverageTrueRange _atr = new();

	private decimal? _prevClose;
	private decimal _nPrice;
	private decimal _prevNPrice;
	private decimal _prevHma;
	private decimal _entryPrice;
	private decimal _stopAtr;
	private readonly Queue<decimal> _nPriceQueue = new();

	/// <summary>
	/// Constructor.
	/// </summary>
	public CrunchstersNormalisedTrendStrategy()
	{
		_normPeriod = Param(nameof(NormPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("Normalisation Period", "Lookback for returns standard deviation", "General");

		_hmaPeriod = Param(nameof(HmaPeriod), 100)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Hull Moving Average period", "Indicators");

		_hmaOffset = Param(nameof(HmaOffset), 0)
			.SetDisplay("HMA Offset", "Offset for HMA input", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 20, 1);

		_stopMultiple = Param(nameof(StopMultiple), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Stop Multiple", "ATR multiplier for stop loss", "Risk");

		_useLong = Param(nameof(UseLong), true)
			.SetDisplay("Enable Long", "Allow long trades", "General");

		_useShort = Param(nameof(UseShort), true)
			.SetDisplay("Enable Short", "Allow short trades", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy", "General");
	}

	/// <summary>
	/// Period for normalising returns.
	/// </summary>
	public int NormPeriod
	{
		get => _normPeriod.Value;
		set => _normPeriod.Value = value;
	}

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Offset applied before HMA calculation.
	/// </summary>
	public int HmaOffset
	{
		get => _hmaOffset.Value;
		set => _hmaOffset.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss.
	/// </summary>
	public decimal StopMultiple
	{
		get => _stopMultiple.Value;
		set => _stopMultiple.Value = value;
	}

	/// <summary>
	/// Use long trades.
	/// </summary>
	public bool UseLong
	{
		get => _useLong.Value;
		set => _useLong.Value = value;
	}

	/// <summary>
	/// Use short trades.
	/// </summary>
	public bool UseShort
	{
		get => _useShort.Value;
		set => _useShort.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_returnsStdDev.Length = NormPeriod;
		_hma.Length = HmaPeriod;
		_atr.Length = 14;

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_prevClose is null)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var diff = candle.ClosePrice - _prevClose.Value;
		var stdDevValue = _returnsStdDev.Process(diff, candle.OpenTime, true).ToDecimal();

		if (stdDevValue == 0)
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var nRet = diff / stdDevValue;
		_nPrice += nRet;
		_nPriceQueue.Enqueue(_nPrice);

		decimal shifted;
		if (_nPriceQueue.Count > HmaOffset)
			shifted = _nPriceQueue.Dequeue();
		else
		{
			_prevClose = candle.ClosePrice;
			return;
		}

		var hmaValue = _hma.Process(shifted, candle.OpenTime, true).ToDecimal();
		var atrValue = _atr.Process(candle).ToDecimal();

		if (UseLong && _prevNPrice <= _prevHma && _nPrice > hmaValue && Position <= 0)
		{
			BuyMarket();
			_entryPrice = candle.ClosePrice;
			_stopAtr = atrValue;
		}
		else if (UseShort && _prevNPrice >= _prevHma && _nPrice < hmaValue && Position >= 0)
		{
			SellMarket();
			_entryPrice = candle.ClosePrice;
			_stopAtr = atrValue;
		}

		if (Position > 0)
		{
			var stopPrice = _entryPrice - _stopAtr * StopMultiple;
			if (_nPrice < hmaValue || candle.ClosePrice <= stopPrice)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var stopPrice = _entryPrice + _stopAtr * StopMultiple;
			if (_nPrice > hmaValue || candle.ClosePrice >= stopPrice)
				BuyMarket(Math.Abs(Position));
		}

		_prevNPrice = _nPrice;
		_prevHma = hmaValue;
		_prevClose = candle.ClosePrice;
	}
}
