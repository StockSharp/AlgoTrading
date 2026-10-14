using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// MA ADX strategy.
/// A close crossing above the MaPeriod SMA while ADX is above AdxThreshold goes long and a cross below goes short; the reverse cross
/// closes the position and, with ADX still strong, opens the other side. The target lies TakeProfitAtrMultiplier ATR from the entry close
/// and is checked on candle closes, and a percent stop limits the loss.
/// </summary>
public class MaAdxStrategy : Strategy
{
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _adxPeriod;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal _prevMa;
	private decimal _targetPrice;

	/// <summary>
	/// Period of the SMA.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Period of ADX.
	/// </summary>
	public int AdxPeriod
	{
		get => _adxPeriod.Value;
		set => _adxPeriod.Value = value;
	}

	/// <summary>
	/// ADX level of a strong trend.
	/// </summary>
	public decimal AdxThreshold
	{
		get => _adxThreshold.Value;
		set => _adxThreshold.Value = value;
	}

	/// <summary>
	/// Target distance from the entry in ATRs.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Period of the target ATR.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>
	/// Stop loss percentage from entry price.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
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
	public MaAdxStrategy()
	{
		_maPeriod = Param(nameof(MaPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Period of the SMA", "Indicators");

		_adxPeriod = Param(nameof(AdxPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ADX Period", "Period of ADX", "Indicators");

		_adxThreshold = Param(nameof(AdxThreshold), 25m)
			.SetDisplay("ADX Threshold", "ADX level of a strong trend", "Indicators");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 2m)
			.SetNotNegative()
			.SetDisplay("Take Profit ATR", "Target distance from the entry in ATRs", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Period of the target ATR", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 2m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

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
		_prevClose = null;
		_prevMa = default;
		_targetPrice = default;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevMa = default;
		_targetPrice = default;

		var sma = new SimpleMovingAverage { Length = MaPeriod };
		var adx = new AverageDirectionalIndex { Length = AdxPeriod };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(sma, adx, atr, ProcessCandle)
			.Start();

		StartProtection(new Unit(), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

		// The stop has to see prices between candles, not only at their close.
		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		}

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, sma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, adx);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue smaValue, IIndicatorValue adxValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!smaValue.IsFormed || !adxValue.IsFormed || !atrValue.IsFormed)
			return;

		if (adxValue is not AverageDirectionalIndexValue { MovingAverage: decimal strength })
			return;

		var ma = smaValue.GetValue<decimal>();
		var close = candle.ClosePrice;
		var prevClose = _prevClose;
		var prevMa = _prevMa;
		_prevClose = close;
		_prevMa = ma;

		if (prevClose is not decimal lastClose)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var atr = atrValue.GetValue<decimal>();
		var crossUp = lastClose <= prevMa && close > ma;
		var crossDown = lastClose >= prevMa && close < ma;
		var strong = strength > AdxThreshold;

		if (crossUp && strong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_targetPrice = close + TakeProfitAtrMultiplier * atr;
		}
		else if (crossDown && strong && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_targetPrice = close - TakeProfitAtrMultiplier * atr;
		}
		else if (Position > 0 && (crossDown || (TakeProfitAtrMultiplier > 0 && close >= _targetPrice)))
		{
			SellMarket(Position);
		}
		else if (Position < 0 && (crossUp || (TakeProfitAtrMultiplier > 0 && close <= _targetPrice)))
		{
			BuyMarket(-Position);
		}
	}
}
