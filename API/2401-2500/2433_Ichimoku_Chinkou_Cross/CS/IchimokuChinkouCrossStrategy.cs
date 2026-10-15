using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Ichimoku Chinkou Span crossover strategy with RSI filter.
/// Buys when Chinkou crosses price from below above the Kumo and RSI is high.
/// Sells when Chinkou crosses price from above below the Kumo and RSI is low.
/// </summary>
public class IchimokuChinkouCrossStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanPeriod;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<decimal> _rsiBuyLevel;
	private readonly StrategyParam<decimal> _rsiSellLevel;
	private readonly StrategyParam<DataType> _candleType;

	private RelativeStrengthIndex _rsi;
	private readonly List<decimal> _closes = new();
	private readonly List<decimal> _highs = new();
	private readonly List<decimal> _lows = new();

	/// <summary>
	/// Tenkan-sen period.
	/// </summary>
	public int TenkanPeriod { get => _tenkanPeriod.Value; set => _tenkanPeriod.Value = value; }

	/// <summary>
	/// Kijun-sen period.
	/// </summary>
	public int KijunPeriod { get => _kijunPeriod.Value; set => _kijunPeriod.Value = value; }

	/// <summary>
	/// Senkou Span B period.
	/// </summary>
	public int SenkouSpanPeriod { get => _senkouSpanPeriod.Value; set => _senkouSpanPeriod.Value = value; }

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// RSI threshold for long signals.
	/// </summary>
	public decimal RsiBuyLevel { get => _rsiBuyLevel.Value; set => _rsiBuyLevel.Value = value; }

	/// <summary>
	/// RSI threshold for short signals.
	/// </summary>
	public decimal RsiSellLevel { get => _rsiSellLevel.Value; set => _rsiSellLevel.Value = value; }

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initialize <see cref="IchimokuChinkouCrossStrategy"/>.
	/// </summary>
	public IchimokuChinkouCrossStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetGreaterThanZero()
			.SetDisplay("Tenkan Period", "Tenkan-sen period", "Ichimoku");

		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetGreaterThanZero()
			.SetDisplay("Kijun Period", "Kijun-sen period", "Ichimoku");

		_senkouSpanPeriod = Param(nameof(SenkouSpanPeriod), 52)
			.SetGreaterThanZero()
			.SetDisplay("Senkou Span Period", "Senkou Span B period", "Ichimoku");

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Period", "RSI calculation period", "RSI");

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 55m)
			.SetDisplay("RSI Buy Level", "Minimum RSI for long", "RSI");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 45m)
			.SetDisplay("RSI Sell Level", "Maximum RSI for short", "RSI");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_closes.Clear();
		_highs.Clear();
		_lows.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, ProcessCandle)
			.Start();

		StartProtection(
			new Unit(2000m, UnitTypes.Absolute),
			new Unit(1000m, UnitTypes.Absolute));
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		_closes.Add(candle.ClosePrice);
		_highs.Add(candle.HighPrice);
		_lows.Add(candle.LowPrice);

		var maxCount = Math.Max(SenkouSpanPeriod, KijunPeriod) + KijunPeriod + 2;
		if (_closes.Count > maxCount)
		{
			_closes.RemoveAt(0);
			_highs.RemoveAt(0);
			_lows.RemoveAt(0);
		}

		if (_closes.Count <= KijunPeriod || _closes.Count < Math.Max(TenkanPeriod, KijunPeriod))
			return;

		var tenkan = GetMidpoint(TenkanPeriod);
		var kijun = GetMidpoint(KijunPeriod);
		var lastIndex = _closes.Count - 1;
		var prevIndex = lastIndex - 1;
		var lagIndex = lastIndex - KijunPeriod;
		var prevLagIndex = prevIndex - KijunPeriod;
		if (prevLagIndex < 0)
			return;

		var close = _closes[lastIndex];
		var prevClose = _closes[prevIndex];
		var lagClose = _closes[lagIndex];
		var prevLagClose = _closes[prevLagIndex];

		var chinkouCrossUp = close > lagClose && prevClose <= prevLagClose;
		var chinkouCrossDown = close < lagClose && prevClose >= prevLagClose;

		if (chinkouCrossUp && tenkan > kijun && rsiValue >= RsiBuyLevel && Position <= 0)
			BuyMarket();
		else if (chinkouCrossDown && tenkan < kijun && rsiValue <= RsiSellLevel && Position >= 0)
			SellMarket();
	}

	private decimal GetMidpoint(int period)
	{
		var start = _highs.Count - period;
		var highest = _highs[start];
		var lowest = _lows[start];

		for (var i = start + 1; i < _highs.Count; i++)
		{
			if (_highs[i] > highest)
				highest = _highs[i];
			if (_lows[i] < lowest)
				lowest = _lows[i];
		}

		return (highest + lowest) / 2m;
	}
}
