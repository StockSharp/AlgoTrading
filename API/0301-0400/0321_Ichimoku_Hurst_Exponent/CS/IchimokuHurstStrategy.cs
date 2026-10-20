namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Ichimoku cloud crossings filtered by the rescaled range of logarithmic returns, with entry-frozen ATR protection.
/// </summary>
public class IchimokuHurstStrategy : Strategy
{
	private readonly StrategyParam<int> _tenkanPeriod;
	private readonly StrategyParam<int> _kijunPeriod;
	private readonly StrategyParam<int> _senkouSpanBPeriod;
	private readonly StrategyParam<int> _hurstPeriod;
	private readonly StrategyParam<decimal> _hurstThreshold;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _signalCooldownBars;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _stopLossAtr;

	private Ichimoku _ichimoku;
	private HurstExponent _hurst;
	private AverageTrueRange _atr;
	private decimal? _previousClose;
	private decimal? _previousTenkan;
	private decimal? _previousKijun;
	private int _cooldownRemaining;
	private Order _pendingOrder;

	/// <summary>Tenkan rolling high/low midpoint period.</summary>
	public int TenkanPeriod
	{
		get => _tenkanPeriod.Value;
		set => _tenkanPeriod.Value = value;
	}

	/// <summary>Kijun midpoint period and cloud displacement.</summary>
	public int KijunPeriod
	{
		get => _kijunPeriod.Value;
		set => _kijunPeriod.Value = value;
	}

	/// <summary>Senkou B rolling high/low midpoint period.</summary>
	public int SenkouSpanBPeriod
	{
		get => _senkouSpanBPeriod.Value;
		set => _senkouSpanBPeriod.Value = value;
	}

	/// <summary>Number of closes supplying the Hurst log-return window.</summary>
	public int HurstPeriod
	{
		get => _hurstPeriod.Value;
		set => _hurstPeriod.Value = value;
	}

	/// <summary>Strict lower threshold for a formed Hurst estimate.</summary>
	public decimal HurstThreshold
	{
		get => _hurstThreshold.Value;
		set => _hurstThreshold.Value = value;
	}

	/// <summary>Signal candle type.</summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>Finished candles between signal orders; exits bypass this entry restriction.</summary>
	public int SignalCooldownBars
	{
		get => _signalCooldownBars.Value;
		set => _signalCooldownBars.Value = value;
	}

	/// <summary>Wilder ATR period.</summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
	}

	/// <summary>ATR stop distance multiplier; zero disables protection for the run.</summary>
	public decimal StopLossAtr
	{
		get => _stopLossAtr.Value;
		set => _stopLossAtr.Value = value;
	}

	/// <summary>Initializes the strategy parameters.</summary>
	public IchimokuHurstStrategy()
	{
		_tenkanPeriod = Param(nameof(TenkanPeriod), 9)
			.SetDisplay("Tenkan Period", "Tenkan rolling high/low midpoint period", "Ichimoku")
			.SetOptimize(5, 15, 1);
		_kijunPeriod = Param(nameof(KijunPeriod), 26)
			.SetDisplay("Kijun Period", "Kijun midpoint period and cloud displacement", "Ichimoku")
			.SetOptimize(20, 40, 2);
		_senkouSpanBPeriod = Param(nameof(SenkouSpanBPeriod), 52)
			.SetDisplay("Senkou Span B Period", "Senkou B rolling high/low midpoint period", "Ichimoku")
			.SetOptimize(40, 70, 5);
		_hurstPeriod = Param(nameof(HurstPeriod), 100)
			.SetDisplay("Hurst Period", "Closes in the Hurst log-return window", "Hurst Exponent")
			.SetOptimize(50, 200, 10);
		_hurstThreshold = Param(nameof(HurstThreshold), 0.5m)
			.SetDisplay("Hurst Threshold", "Strict lower threshold for the Hurst estimate", "Hurst Exponent")
			.SetOptimize(0.45m, 0.6m, 0.05m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
			.SetDisplay("Candle Type", "Signal candle type", "General");
		_signalCooldownBars = Param(nameof(SignalCooldownBars), 6)
			.SetGreaterThanZero()
			.SetDisplay("Signal Cooldown", "Finished candles between signal orders; exits remain active", "Trading");
		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "Wilder ATR period", "Risk");
		_stopLossAtr = Param(nameof(StopLossAtr), 2m)
			.SetNotNegative()
			.SetDisplay("ATR Stop", "Entry ATR multiplier; zero disables the stop for the run", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		if (Security is not null)
			yield return (Security, CandleType);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_ichimoku = null;
		_hurst = null;
		_atr = null;
		_previousClose = null;
		_previousTenkan = null;
		_previousKijun = null;
		_cooldownRemaining = 0;
		_pendingOrder = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (Security is null)
			throw new InvalidOperationException("Security is not specified.");
		if (HurstPeriod < 11)
			throw new ArgumentOutOfRangeException(nameof(HurstPeriod), "At least eleven closes are required for ten log returns.");

		_ichimoku = new Ichimoku
		{
			Tenkan = { Length = TenkanPeriod },
			Kijun = { Length = KijunPeriod },
			SenkouB = { Length = SenkouSpanBPeriod },
		};
		_hurst = new HurstExponent { Length = HurstPeriod - 1 };
		_atr = new AverageTrueRange { Length = AtrPeriod };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_ichimoku, _atr, ProcessCandle, allowEmpty: true).Start();

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Start();
		}

		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ichimoku);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue cloudValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		decimal? hurst = null;
		if (candle.ClosePrice > 0m)
		{
			if (_previousClose is decimal previous)
			{
				var value = _hurst.Process((decimal)Math.Log((double)(candle.ClosePrice / previous)), candle.OpenTime, true);
				if (!value.IsEmpty)
					hurst = value.GetValue<decimal>();
			}
			_previousClose = candle.ClosePrice;
		}
		else
		{
			_previousClose = null;
			_hurst.Reset();
		}

		if (!IsFormedAndOnlineAndAllowTrading()
			|| cloudValue is not IIchimokuValue { Tenkan: decimal tenkan, Kijun: decimal kijun, SenkouA: decimal spanA, SenkouB: decimal spanB })
			return;

		var crossUp = _previousTenkan is decimal oldTenkan && _previousKijun is decimal oldKijun
			&& oldTenkan <= oldKijun && tenkan > kijun;
		var crossDown = _previousTenkan is decimal oldFast && _previousKijun is decimal oldSlow
			&& oldFast >= oldSlow && tenkan < kijun;
		_previousTenkan = tenkan;
		_previousKijun = kijun;
		if (_cooldownRemaining > 0)
			_cooldownRemaining--;
		if (_pendingOrder is { State: not (OrderStates.Done or OrderStates.Failed) })
			return;

		var above = candle.ClosePrice > Math.Max(spanA, spanB);
		var below = candle.ClosePrice < Math.Min(spanA, spanB);
		if (Position > 0m && (below || crossDown))
			SubmitOrder(Sides.Sell, Position, "Ichimoku Hurst exit");
		else if (Position < 0m && (above || crossUp))
			SubmitOrder(Sides.Buy, Math.Abs(Position), "Ichimoku Hurst exit");
		else if (Position == 0m && _cooldownRemaining == 0 && hurst is decimal estimate && estimate > HurstThreshold
			&& !atrValue.IsEmpty && atrValue.GetValue<decimal>() > 0m && (above && crossUp || below && crossDown))
		{
			var distance = atrValue.GetValue<decimal>() * StopLossAtr;
			StartProtection(new Unit(), new Unit(distance, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
			SubmitOrder(crossUp ? Sides.Buy : Sides.Sell, Volume, "Ichimoku Hurst entry");
		}
		else
			return;

		_cooldownRemaining = SignalCooldownBars;
	}

	private void SubmitOrder(Sides side, decimal volume, string comment)
	{
		_pendingOrder = CreateOrder(side, 0m, volume);
		_pendingOrder.Type = OrderTypes.Market;
		_pendingOrder.Comment = comment;
		RegisterOrder(_pendingOrder);
	}
}
