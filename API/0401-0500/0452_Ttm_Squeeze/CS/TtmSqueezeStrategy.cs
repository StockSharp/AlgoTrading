namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// TTM Squeeze Strategy.
/// Bollinger Bands (2 deviations) and Keltner Channels (1.5 average true ranges) span SqueezeLength bars. When the squeeze is
/// off (the bands lie outside the channels) a long opens if the linear regression momentum is below zero and rising with RSI
/// above 30, and a short opens if momentum is above zero and falling with RSI below 70. An opposite signal reverses the
/// position; UseTP enables a TpPercent take profit.
/// </summary>
public class TtmSqueezeStrategy : Strategy
{
	private const decimal BollingerWidth = 2m;
	private const decimal KeltnerMultiplier = 1.5m;

	private readonly StrategyParam<int> _squeezeLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<bool> _useTp;
	private readonly StrategyParam<decimal> _tpPercent;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _closeSma;
	private SimpleMovingAverage _rangeSma;
	private Highest _highest;
	private Lowest _lowest;
	private LinearReg _momentum;

	private decimal? _prevClose;
	private decimal? _prevMomentum;

	/// <summary>
	/// Length of the bands, the channels and the momentum.
	/// </summary>
	public int SqueezeLength
	{
		get => _squeezeLength.Value;
		set => _squeezeLength.Value = value;
	}

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Enable the take profit.
	/// </summary>
	public bool UseTP
	{
		get => _useTp.Value;
		set => _useTp.Value = value;
	}

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal TpPercent
	{
		get => _tpPercent.Value;
		set => _tpPercent.Value = value;
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
	public TtmSqueezeStrategy()
	{
		_squeezeLength = Param(nameof(SqueezeLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Squeeze Length", "Length of the bands, the channels and the momentum", "Indicators");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");

		_useTp = Param(nameof(UseTP), false)
			.SetDisplay("Use TP", "Enable the take profit", "Risk");

		_tpPercent = Param(nameof(TpPercent), 1.2m)
			.SetGreaterThanZero()
			.SetDisplay("TP %", "Take profit percentage from entry price", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(30).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevClose = null;
		_prevMomentum = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_prevClose = null;
		_prevMomentum = null;

		var bollinger = new BollingerBands { Length = SqueezeLength, Width = BollingerWidth };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };

		_closeSma = new SimpleMovingAverage { Length = SqueezeLength };
		_rangeSma = new SimpleMovingAverage { Length = SqueezeLength };
		_highest = new Highest { Length = SqueezeLength };
		_lowest = new Lowest { Length = SqueezeLength };
		_momentum = new LinearReg { Length = SqueezeLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(bollinger, rsi, ProcessCandle)
			.Start();

		if (UseTP)
			StartProtection(new Unit(TpPercent, UnitTypes.Percent), new Unit(), useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
				DrawIndicator(oscillators, rsi);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue bollingerValue, IIndicatorValue rsiValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var time = candle.OpenTime;
		var close = candle.ClosePrice;

		var trueRange = _prevClose is decimal pc
			? Math.Max(candle.HighPrice - candle.LowPrice, Math.Max(Math.Abs(candle.HighPrice - pc), Math.Abs(candle.LowPrice - pc)))
			: candle.HighPrice - candle.LowPrice;
		_prevClose = close;

		var sma = _closeSma.Process(close, time, true).ToDecimal();
		var rangeAverage = _rangeSma.Process(trueRange, time, true).ToDecimal();
		var highest = _highest.Process(candle.HighPrice, time, true).ToDecimal();
		var lowest = _lowest.Process(candle.LowPrice, time, true).ToDecimal();

		if (!_closeSma.IsFormed || !_rangeSma.IsFormed || !_highest.IsFormed || !_lowest.IsFormed)
			return;

		// Momentum is the linear regression of the close's distance from the mean of the Donchian midline and the SMA.
		var basis = ((highest + lowest) / 2m + sma) / 2m;
		var momentumValue = _momentum.Process(close - basis, time, true);
		if (!_momentum.IsFormed)
			return;

		var momentum = momentumValue.ToDecimal();
		var prevMomentum = _prevMomentum;
		_prevMomentum = momentum;

		if (prevMomentum is not decimal previous)
			return;

		if (!bollingerValue.IsFormed || !rsiValue.IsFormed)
			return;

		if (bollingerValue is not IBollingerBandsValue { UpBand: decimal bbUpper, LowBand: decimal bbLower })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var kcUpper = sma + KeltnerMultiplier * rangeAverage;
		var kcLower = sma - KeltnerMultiplier * rangeAverage;
		var squeezeOff = bbUpper > kcUpper && bbLower < kcLower;

		if (!squeezeOff)
			return;

		var rsi = rsiValue.GetValue<decimal>();

		if (momentum < 0m && momentum > previous && rsi > 30m && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (momentum > 0m && momentum < previous && rsi < 70m && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));
	}
}
