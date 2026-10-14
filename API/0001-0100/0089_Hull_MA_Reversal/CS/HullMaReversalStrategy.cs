using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull MA Reversal strategy.
/// When the Hull MA turns from falling to rising the position turns long, and when it turns from rising to falling it turns short.
/// The stop lies AtrMultiplier ATRs beyond the entry candle's low (for a long) or high (for a short).
/// </summary>
public class HullMaReversalStrategy : Strategy
{
	/// <summary>
	/// Period of the ATR that sizes the stop.
	/// </summary>
	public const int AtrPeriod = 14;

	private readonly StrategyParam<int> _hmaPeriod;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHma;
	// Direction of the latest move of the Hull MA: 1 rising, -1 falling, 0 none yet.
	private int _slope;
	private decimal _stopPrice;

	/// <summary>
	/// Hull MA period.
	/// </summary>
	public int HmaPeriod
	{
		get => _hmaPeriod.Value;
		set => _hmaPeriod.Value = value;
	}

	/// <summary>
	/// Distance of the stop beyond the entry candle in ATR multiples.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	public HullMaReversalStrategy()
	{
		_hmaPeriod = Param(nameof(HmaPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("HMA Period", "Period for Hull Moving Average", "Indicators");

		_atrMultiplier = Param(nameof(AtrMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "Distance of the stop beyond the entry candle in ATR multiples", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
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
		_prevHma = null;
		_slope = 0;
		_stopPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevHma = null;
		_slope = 0;
		_stopPrice = default;

		var hma = new HullMovingAverage { Length = HmaPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hmaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished || !hmaValue.IsFormed || !atrValue.IsFormed)
			return;

		var hma = hmaValue.GetValue<decimal>();
		var prevHma = _prevHma;
		_prevHma = hma;

		if (prevHma is not decimal lastHma)
			return;

		var previousSlope = _slope;
		var slope = hma > lastHma ? 1 : hma < lastHma ? -1 : 0;

		if (slope != 0)
			_slope = slope;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var distance = AtrMultiplier * atrValue.GetValue<decimal>();

		if (previousSlope == -1 && slope == 1 && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.LowPrice - distance;
		}
		else if (previousSlope == 1 && slope == -1 && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = candle.HighPrice + distance;
		}
		else if (Position > 0 && close <= _stopPrice)
		{
			SellMarket(Position);
		}
		else if (Position < 0 && close >= _stopPrice)
		{
			BuyMarket(-Position);
		}
	}
}
