using System;
using System.Collections.Generic;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
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
	private readonly StrategyParam<Unit> _stopLoss;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevChinkou;
	private decimal _prevPrice;
	private bool _isFirst;

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
	/// Stop loss value.
	/// </summary>
	public Unit StopLoss { get => _stopLoss.Value; set => _stopLoss.Value = value; }

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

		_rsiBuyLevel = Param(nameof(RsiBuyLevel), 70m)
			.SetDisplay("RSI Buy Level", "Minimum RSI for long", "RSI");

		_rsiSellLevel = Param(nameof(RsiSellLevel), 30m)
			.SetDisplay("RSI Sell Level", "Maximum RSI for short", "RSI");

		_stopLoss = Param(nameof(StopLoss), new Unit(2, UnitTypes.Percent))
			.SetDisplay("Stop Loss", "Stop loss percent or value", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
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
		_prevChinkou = 0m;
		_prevPrice = 0m;
		_isFirst = true;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanPeriod }
		};

		var rsi = new RSI { Length = RsiPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(ichimoku, rsi, ProcessCandle)
			.Start();

		StartProtection(new(), StopLoss);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue ichValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ich = (IchimokuValue)ichValue;

		if (ich.Chinkou is not decimal chinkou ||
			ich.SenkouA is not decimal senkouA ||
			ich.SenkouB is not decimal senkouB)
			return;

		if (!rsiValue.IsFinal)
			return;

		var rsi = rsiValue.GetValue<decimal>();

		var upperKumo = Math.Max(senkouA, senkouB);
		var lowerKumo = Math.Min(senkouA, senkouB);

		if (_isFirst)
		{
			_prevChinkou = chinkou;
			_prevPrice = candle.ClosePrice;
			_isFirst = false;
			return;
		}

		var crossUp = chinkou > candle.ClosePrice && _prevChinkou <= _prevPrice;
		var crossDown = chinkou < candle.ClosePrice && _prevChinkou >= _prevPrice;

		_prevChinkou = chinkou;
		_prevPrice = candle.ClosePrice;

		if (crossUp && candle.ClosePrice > upperKumo && chinkou > upperKumo && rsi > RsiBuyLevel && Position <= 0)
		{
			BuyMarket();
		}
		else if (crossDown && candle.ClosePrice < lowerKumo && chinkou < lowerKumo && rsi < RsiSellLevel && Position >= 0)
		{
			SellMarket();
		}
	}
}
