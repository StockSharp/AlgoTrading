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
/// Strategy uses Bollinger Bands breakout with moving average trend filter and ATR-based exits.
/// </summary>
public class BbsrExtremeStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerPeriod;
	private readonly StrategyParam<decimal> _bollingerMultiplier;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopMultiplier;
	private readonly StrategyParam<decimal> _atrProfitMultiplier;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _prevLower;
	private decimal _prevUpper;
	private decimal _prevClose;
	private decimal _prevMa;
	private bool _isInitialized;
	private Order _pending;

	private bool IsPending => _pending is not null && _pending.State is not (OrderStates.Done or OrderStates.Failed);

	/// <summary>
	/// Period for Bollinger Bands calculation.
	/// </summary>
	public int BollingerPeriod
	{
		get => _bollingerPeriod.Value;
		set => _bollingerPeriod.Value = value;
	}

	/// <summary>
	/// Standard deviation multiplier for Bollinger Bands.
	/// </summary>
	public decimal BollingerMultiplier
	{
		get => _bollingerMultiplier.Value;
		set => _bollingerMultiplier.Value = value;
	}

	/// <summary>
	/// Moving average length.
	/// </summary>
	public int MaLength
	{
		get => _maLength.Value;
		set => _maLength.Value = value;
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop calculation.
	/// </summary>
	public decimal AtrStopMultiplier
	{
		get => _atrStopMultiplier.Value;
		set => _atrStopMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit.
	/// </summary>
	public decimal AtrProfitMultiplier
	{
		get => _atrProfitMultiplier.Value;
		set => _atrProfitMultiplier.Value = value;
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
	/// Initializes the strategy.
	/// </summary>
	public BbsrExtremeStrategy()
	{
		_bollingerPeriod = Param(nameof(BollingerPeriod), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Period", "Bollinger Bands length", "Indicators");

		_bollingerMultiplier = Param(nameof(BollingerMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Multiplier", "Standard deviation multiplier", "Indicators");

		_maLength = Param(nameof(MaLength), 7)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Length for moving average", "Indicators");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR indicator period", "Risk Management");

		_atrStopMultiplier = Param(nameof(AtrStopMultiplier), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Stop Multiplier", "ATR multiplier for stop", "Risk Management");

		_atrProfitMultiplier = Param(nameof(AtrProfitMultiplier), 3m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Profit Multiplier", "ATR multiplier for take profit", "Risk Management");

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
		_prevLower = default;
		_prevUpper = default;
		_prevClose = default;
		_prevMa = default;
		_isInitialized = false;
		_pending = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var bollinger = new BollingerBands
		{
			Length = BollingerPeriod,
			Width = BollingerMultiplier
		};

		var ma = new EMA
		{
			Length = MaLength
		};

		var atr = new AverageTrueRange
		{
			Length = AtrLength
		};

		foreach (var field in new[] { Level1Fields.BestBidPrice, Level1Fields.BestAskPrice })
		{
			var quotes = new Subscription(DataType.Level1, Security);
			quotes.MarketData.BuildField = field;
			SubscribeLevel1(quotes).Start();
		}

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx([bollinger, ma, atr], ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}

	}


	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished
			|| values[0] is not BollingerBandsValue bb || !values[0].IsFormed
			|| bb.UpBand is not decimal upper || bb.LowBand is not decimal lower
			|| !values[1].IsFormed || !values[2].IsFormed)
			return;
		var ma = values[1].GetValue<decimal>();
		var atr = values[2].GetValue<decimal>();
		var bull = _isInitialized && _prevClose < _prevLower && candle.ClosePrice > lower && ma > _prevMa;
		var bear = _isInitialized && _prevClose > _prevUpper && candle.ClosePrice < upper && ma < _prevMa;
		_prevLower = lower;
		_prevUpper = upper;
		_prevClose = candle.ClosePrice;
		_prevMa = ma;
		_isInitialized = true;
		if (Position != 0m || IsPending || atr <= 0m || !IsFormedAndOnlineAndAllowTrading())
			return;
		if (!bull && !bear)
			return;
		// Freeze distances before registering the entry; native protection anchors them to actual fills.
		StartProtection(new Unit(atr * AtrProfitMultiplier, UnitTypes.Absolute),
			new Unit(atr * AtrStopMultiplier, UnitTypes.Absolute), useMarketOrders: true, isLocalStop: true);
		_pending = CreateOrder(bull ? Sides.Buy : Sides.Sell, 0m, Volume);
		_pending.Type = OrderTypes.Market;
		_pending.Comment = "0556Entry";
		RegisterOrder(_pending);
	}

}
