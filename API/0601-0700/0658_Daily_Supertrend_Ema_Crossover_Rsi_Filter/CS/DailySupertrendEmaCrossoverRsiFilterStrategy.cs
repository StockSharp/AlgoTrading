using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Supertrend EMA crossover strategy with RSI filter.
/// A fast EMA crossing above the slow EMA goes long when Supertrend is up and RSI is below RsiOverbought;
/// a cross below goes short when Supertrend is down and RSI is above RsiOversold. An opposite signal reverses the position.
/// Stop loss and take profit sit at ATR multiples from the entry close, measured with the ATR of the signal bar.
/// </summary>
public class DailySupertrendEmaCrossoverRsiFilterStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _slowEmaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _stopLossMultiplier;
	private readonly StrategyParam<decimal> _takeProfitMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<decimal> _supertrendMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevFast;
	private decimal? _prevSlow;
	private decimal? _stopPrice;
	private decimal? _takePrice;

	/// <summary>
	/// Fast EMA length.
	/// </summary>
	public int FastEmaLength { get => _fastEmaLength.Value; set => _fastEmaLength.Value = value; }

	/// <summary>
	/// Slow EMA length.
	/// </summary>
	public int SlowEmaLength { get => _slowEmaLength.Value; set => _slowEmaLength.Value = value; }

	/// <summary>
	/// ATR length used by the stops and by Supertrend.
	/// </summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }

	/// <summary>
	/// Stop loss distance in ATR.
	/// </summary>
	public decimal StopLossMultiplier { get => _stopLossMultiplier.Value; set => _stopLossMultiplier.Value = value; }

	/// <summary>
	/// Take profit distance in ATR.
	/// </summary>
	public decimal TakeProfitMultiplier { get => _takeProfitMultiplier.Value; set => _takeProfitMultiplier.Value = value; }

	/// <summary>
	/// RSI length.
	/// </summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }

	/// <summary>
	/// RSI level that blocks longs.
	/// </summary>
	public decimal RsiOverbought { get => _rsiOverbought.Value; set => _rsiOverbought.Value = value; }

	/// <summary>
	/// RSI level that blocks shorts.
	/// </summary>
	public decimal RsiOversold { get => _rsiOversold.Value; set => _rsiOversold.Value = value; }

	/// <summary>
	/// ATR multiplier of Supertrend.
	/// </summary>
	public decimal SupertrendMultiplier { get => _supertrendMultiplier.Value; set => _supertrendMultiplier.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Constructor.
	/// </summary>
	public DailySupertrendEmaCrossoverRsiFilterStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_slowEmaLength = Param(nameof(SlowEmaLength), 6)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_atrLength = Param(nameof(AtrLength), 3)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR length used by the stops and by Supertrend", "Indicators");

		_stopLossMultiplier = Param(nameof(StopLossMultiplier), 2.5m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop loss distance in ATR", "Risk");

		_takeProfitMultiplier = Param(nameof(TakeProfitMultiplier), 4m)
			.SetNotNegative()
			.SetDisplay("Take Profit ATR", "Take profit distance in ATR", "Risk");

		_rsiLength = Param(nameof(RsiLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI length", "Indicators");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "RSI level that blocks longs", "Indicators");

		_rsiOversold = Param(nameof(RsiOversold), 30m)
			.SetDisplay("RSI Oversold", "RSI level that blocks shorts", "Indicators");

		_supertrendMultiplier = Param(nameof(SupertrendMultiplier), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Supertrend Multiplier", "ATR multiplier of Supertrend", "Indicators");

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
		_prevFast = null;
		_prevSlow = null;
		_stopPrice = null;
		_takePrice = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var slowEma = new ExponentialMovingAverage { Length = SlowEmaLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var supertrend = new SuperTrend { Length = AtrLength, Multiplier = SupertrendMultiplier };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastEma, slowEma, atr, rsi, supertrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, slowEma);
			DrawIndicator(area, supertrend);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue, IIndicatorValue rsiValue, IIndicatorValue supertrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!fastValue.IsFormed || !slowValue.IsFormed || !atrValue.IsFormed || !rsiValue.IsFormed || !supertrendValue.IsFormed)
			return;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var prevFast = _prevFast;
		var prevSlow = _prevSlow;
		_prevFast = fast;
		_prevSlow = slow;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if ((_stopPrice is decimal stop && candle.LowPrice <= stop) || (_takePrice is decimal take && candle.HighPrice >= take))
			{
				SellMarket(Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}
		else if (Position < 0)
		{
			if ((_stopPrice is decimal stop && candle.HighPrice >= stop) || (_takePrice is decimal take && candle.LowPrice <= take))
			{
				BuyMarket(-Position);
				_stopPrice = null;
				_takePrice = null;
				return;
			}
		}

		if (prevFast is not decimal pf || prevSlow is not decimal ps)
			return;

		var isUpTrend = ((SuperTrendIndicatorValue)supertrendValue).IsUpTrend;
		var rsi = rsiValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var crossUp = pf <= ps && fast > slow;
		var crossDown = pf >= ps && fast < slow;

		if (crossUp && isUpTrend && rsi < RsiOverbought && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_stopPrice = StopLossMultiplier > 0 ? close - atr * StopLossMultiplier : null;
			_takePrice = TakeProfitMultiplier > 0 ? close + atr * TakeProfitMultiplier : null;
		}
		else if (crossDown && !isUpTrend && rsi > RsiOversold && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_stopPrice = StopLossMultiplier > 0 ? close + atr * StopLossMultiplier : null;
			_takePrice = TakeProfitMultiplier > 0 ? close - atr * TakeProfitMultiplier : null;
		}
	}
}
