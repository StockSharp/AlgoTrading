using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Double CCI Confirmed Hull MA Reversal strategy.
/// Long only: buys when the close crosses above the Hull Moving Average while both the fast and the slow CCI are above zero.
/// At entry the stop is set StopLossAtrMultiplier ATRs below the close and the trailing activation level TrailingActivationMultiplier
/// ATRs above it. The long closes when a candle low reaches the stop, or, once a high has reached the activation level, when the
/// close falls below the trailing EMA.
/// </summary>
public class DoubleCciConfirmedHullMovingAverageReversalStrategy : Strategy
{
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _trailingActivationMultiplier;
	private readonly StrategyParam<int> _fastCciPeriod;
	private readonly StrategyParam<int> _slowCciPeriod;
	private readonly StrategyParam<int> _hullMaLength;
	private readonly StrategyParam<int> _trailingEmaLength;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevClose;
	private decimal? _prevHma;
	private decimal _stopPrice;
	private decimal _activationPrice;
	private bool _trailingActive;

	/// <summary>
	/// Stop distance in ATRs below the entry close.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Profit in ATRs that activates the trailing EMA exit.
	/// </summary>
	public decimal TrailingActivationMultiplier
	{
		get => _trailingActivationMultiplier.Value;
		set => _trailingActivationMultiplier.Value = value;
	}

	/// <summary>
	/// Fast CCI period.
	/// </summary>
	public int FastCciPeriod
	{
		get => _fastCciPeriod.Value;
		set => _fastCciPeriod.Value = value;
	}

	/// <summary>
	/// Slow CCI period.
	/// </summary>
	public int SlowCciPeriod
	{
		get => _slowCciPeriod.Value;
		set => _slowCciPeriod.Value = value;
	}

	/// <summary>
	/// Hull Moving Average period.
	/// </summary>
	public int HullMaLength
	{
		get => _hullMaLength.Value;
		set => _hullMaLength.Value = value;
	}

	/// <summary>
	/// Trailing EMA period.
	/// </summary>
	public int TrailingEmaLength
	{
		get => _trailingEmaLength.Value;
		set => _trailingEmaLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrPeriod
	{
		get => _atrPeriod.Value;
		set => _atrPeriod.Value = value;
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
	public DoubleCciConfirmedHullMovingAverageReversalStrategy()
	{
		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 1.75m)
			.SetGreaterThanZero()
			.SetDisplay("Stop ATR Mult", "Stop distance in ATRs below the entry close", "Risk");

		_trailingActivationMultiplier = Param(nameof(TrailingActivationMultiplier), 2.25m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Activation Mult", "Profit in ATRs that activates the trailing EMA exit", "Risk");

		_fastCciPeriod = Param(nameof(FastCciPeriod), 25)
			.SetGreaterThanZero()
			.SetDisplay("Fast CCI", "Fast CCI period", "Indicators");

		_slowCciPeriod = Param(nameof(SlowCciPeriod), 50)
			.SetGreaterThanZero()
			.SetDisplay("Slow CCI", "Slow CCI period", "Indicators");

		_hullMaLength = Param(nameof(HullMaLength), 34)
			.SetGreaterThanZero()
			.SetDisplay("Hull MA Length", "Hull Moving Average period", "Indicators");

		_trailingEmaLength = Param(nameof(TrailingEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Trailing EMA", "Trailing EMA period", "Risk");

		_atrPeriod = Param(nameof(AtrPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period", "Risk");

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
		_prevClose = null;
		_prevHma = null;
		_stopPrice = 0m;
		_activationPrice = 0m;
		_trailingActive = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var hma = new HullMovingAverage { Length = HullMaLength };
		var fastCci = new CommodityChannelIndex { Length = FastCciPeriod };
		var slowCci = new CommodityChannelIndex { Length = SlowCciPeriod };
		var trailingEma = new ExponentialMovingAverage { Length = TrailingEmaLength };
		var atr = new AverageTrueRange { Length = AtrPeriod };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(hma, fastCci, slowCci, trailingEma, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, hma);
			DrawIndicator(area, trailingEma);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, fastCci);
				DrawIndicator(oscillators, slowCci);
			}
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue hmaValue, IIndicatorValue fastCciValue, IIndicatorValue slowCciValue, IIndicatorValue emaValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!hmaValue.IsFormed || !fastCciValue.IsFormed || !slowCciValue.IsFormed || !emaValue.IsFormed || !atrValue.IsFormed)
			return;

		var hma = hmaValue.GetValue<decimal>();
		var fastCci = fastCciValue.GetValue<decimal>();
		var slowCci = slowCciValue.GetValue<decimal>();
		var trailingEma = emaValue.GetValue<decimal>();
		var atr = atrValue.GetValue<decimal>();
		var close = candle.ClosePrice;

		var prevClose = _prevClose;
		var prevHma = _prevHma;

		_prevClose = close;
		_prevHma = hma;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice)
			{
				SellMarket(Position);
				return;
			}

			if (candle.HighPrice >= _activationPrice)
				_trailingActive = true;

			if (_trailingActive && close < trailingEma)
				SellMarket(Position);

			return;
		}

		if (prevClose is not decimal pc || prevHma is not decimal ph)
			return;

		var crossUp = pc <= ph && close > hma;

		if (Position == 0 && crossUp && fastCci > 0 && slowCci > 0)
		{
			_stopPrice = close - atr * StopLossAtrMultiplier;
			_activationPrice = close + atr * TrailingActivationMultiplier;
			_trailingActive = false;
			BuyMarket(Volume);
		}
	}
}
