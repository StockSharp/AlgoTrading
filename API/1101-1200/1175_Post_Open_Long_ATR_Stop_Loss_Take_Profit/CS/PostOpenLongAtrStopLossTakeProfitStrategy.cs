namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Long-only opening-session resistance breakouts with Bollinger, EMA, RSI and ADX filters
/// and actual-fill protection at frozen signal ATR distances.
/// </summary>
public class PostOpenLongAtrStopLossTakeProfitStrategy : Strategy
{
	private readonly StrategyParam<int> _bbLength;
	private readonly StrategyParam<decimal> _bbMult;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _emaLongLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiThreshold;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<decimal> _adxThreshold;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrStopLossMultiplier;
	private readonly StrategyParam<decimal> _atrTakeProfitMultiplier;
	private readonly StrategyParam<TimeSpan> _sessionStart;
	private readonly StrategyParam<TimeSpan> _sessionDuration;
	private readonly StrategyParam<int> _resistancePeriod;
	private readonly StrategyParam<decimal> _basisProximity;
	private readonly StrategyParam<DataType> _candleType;
	private readonly Queue<decimal> _highs = new();
	private Order _pendingOrder;
	private Unit _stopDistance;
	private Unit _takeDistance;
	private bool _protectionStarted;

	/// <summary>
	/// Initializes the published indicator and ATR settings and a configurable daily UTC opening window.
	/// </summary>
	public PostOpenLongAtrStopLossTakeProfitStrategy()
	{
		_bbLength = Param(nameof(BbLength), 14).SetGreaterThanZero()
			.SetDisplay("BB Length", "Bollinger close-price SMA and deviation period", "Indicators");
		_bbMult = Param(nameof(BbMult), 1.5m).SetGreaterThanZero()
			.SetDisplay("BB Mult", "Bollinger population deviation multiplier", "Indicators");
		_emaLength = Param(nameof(EmaLength), 10).SetGreaterThanZero()
			.SetDisplay("EMA Length", "Short close-price EMA period", "Indicators");
		_emaLongLength = Param(nameof(EmaLongLength), 200).SetGreaterThanZero()
			.SetDisplay("EMA Long Length", "Long close-price EMA period", "Indicators");
		_rsiLength = Param(nameof(RsiLength), 7).SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Indicators");
		_rsiThreshold = Param(nameof(RsiThreshold), 30m).SetRange(0m, 100m)
			.SetDisplay("RSI Threshold", "RSI must be strictly above this value", "Indicators");
		_adxLength = Param(nameof(AdxLength), 7).SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Indicators");
		_adxThreshold = Param(nameof(AdxThreshold), 10m).SetRange(0m, 100m)
			.SetDisplay("ADX Threshold", "ADX must be strictly above this value", "Indicators");
		_atrLength = Param(nameof(AtrLength), 14).SetGreaterThanZero()
			.SetDisplay("ATR Length", "Wilder ATR period", "Indicators");
		_atrStopLossMultiplier = Param(nameof(AtrStopLossMultiplier), 2m).SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "Frozen entry ATR stop distance", "Protection");
		_atrTakeProfitMultiplier = Param(nameof(AtrTakeProfitMultiplier), 4m).SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "Frozen entry ATR take-profit distance", "Protection");
		_sessionStart = Param(nameof(SessionStart), TimeSpan.FromMinutes(570))
			.SetRange(TimeSpan.Zero, TimeSpan.FromDays(1) - TimeSpan.FromTicks(1))
			.SetDisplay("Session Start", "Inclusive daily opening time in UTC", "Entry");
		_sessionDuration = Param(nameof(SessionDuration), TimeSpan.FromHours(1))
			.SetRange(TimeSpan.FromTicks(1), TimeSpan.FromDays(1))
			.SetDisplay("Session Duration", "Opening-window duration; the end is exclusive", "Entry");
		_resistancePeriod = Param(nameof(ResistancePeriod), 5).SetGreaterThanZero()
			.SetDisplay("Resistance Period", "Prior finished highs excluding the signal candle", "Entry");
		_basisProximity = Param(nameof(BasisProximity), 1m).SetGreaterThanZero()
			.SetDisplay("Basis Proximity", "Maximum distance from the basis in Bollinger half-widths", "Entry");
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of finished signal candles", "General");
		OrderRegistering += order => _pendingOrder = order;
	}

	/// <summary>Close-price Bollinger period.</summary>
	public int BbLength { get => _bbLength.Value; set => _bbLength.Value = value; }
	/// <summary>Bollinger deviation multiplier.</summary>
	public decimal BbMult { get => _bbMult.Value; set => _bbMult.Value = value; }
	/// <summary>Short close-price EMA period.</summary>
	public int EmaLength { get => _emaLength.Value; set => _emaLength.Value = value; }
	/// <summary>Long close-price EMA period.</summary>
	public int EmaLongLength { get => _emaLongLength.Value; set => _emaLongLength.Value = value; }
	/// <summary>RSI period.</summary>
	public int RsiLength { get => _rsiLength.Value; set => _rsiLength.Value = value; }
	/// <summary>Exclusive lower RSI threshold.</summary>
	public decimal RsiThreshold { get => _rsiThreshold.Value; set => _rsiThreshold.Value = value; }
	/// <summary>ADX period.</summary>
	public int AdxLength { get => _adxLength.Value; set => _adxLength.Value = value; }
	/// <summary>Exclusive lower ADX threshold.</summary>
	public decimal AdxThreshold { get => _adxThreshold.Value; set => _adxThreshold.Value = value; }
	/// <summary>Wilder ATR period.</summary>
	public int AtrLength { get => _atrLength.Value; set => _atrLength.Value = value; }
	/// <summary>Positive stop distance in signal ATR units.</summary>
	public decimal AtrStopLossMultiplier { get => _atrStopLossMultiplier.Value; set => _atrStopLossMultiplier.Value = value; }
	/// <summary>Positive take-profit distance in signal ATR units.</summary>
	public decimal AtrTakeProfitMultiplier { get => _atrTakeProfitMultiplier.Value; set => _atrTakeProfitMultiplier.Value = value; }
	/// <summary>Inclusive daily opening time in UTC.</summary>
	public TimeSpan SessionStart { get => _sessionStart.Value; set => _sessionStart.Value = value; }
	/// <summary>Opening-window duration; supports midnight wrap and excludes its end.</summary>
	public TimeSpan SessionDuration { get => _sessionDuration.Value; set => _sessionDuration.Value = value; }
	/// <summary>Number of prior finished highs defining resistance.</summary>
	public int ResistancePeriod { get => _resistancePeriod.Value; set => _resistancePeriod.Value = value; }
	/// <summary>Maximum basis distance in Bollinger half-widths.</summary>
	public decimal BasisProximity { get => _basisProximity.Value; set => _basisProximity.Value = value; }
	/// <summary>Candle type; five minutes by default.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType), (Security, DataType.Level1)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_highs.Clear();
		_pendingOrder = null;
		_stopDistance = _takeDistance = null;
		_protectionStarted = false;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		if (SessionStart < TimeSpan.Zero || SessionStart >= TimeSpan.FromDays(1)
			|| SessionDuration <= TimeSpan.Zero || SessionDuration > TimeSpan.FromDays(1))
			throw new ArgumentException("The UTC opening time must be within a day and duration within (0, 24 hours].");
		_highs.Clear();
		_pendingOrder = null;
		var quotes = new Subscription(DataType.Level1, Security);
		quotes.MarketData.BuildField = Level1Fields.BestBidPrice;
		SubscribeLevel1(quotes).Bind(ObserveProtectionQuote).Start();
		var bb = new BollingerBands { Length = BbLength, Width = BbMult };
		var fast = new ExponentialMovingAverage { Length = EmaLength };
		var slow = new ExponentialMovingAverage { Length = EmaLongLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx([bb, fast, slow, rsi, adx, atr], ProcessCandle, allowEmpty: true).Start();
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bb);
			DrawIndicator(area, fast);
			DrawIndicator(area, slow);
			DrawOwnTrades(area);
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// Native bid-price protection runs before this callback.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;
		var resistance = _highs.Count == ResistancePeriod ? _highs.Max() : (decimal?)null;
		_highs.Enqueue(candle.HighPrice);
		if (_highs.Count > ResistancePeriod)
			_highs.Dequeue();
		if (resistance is null || values.Any(value => value.IsEmpty || !value.IsFormed)
			|| !IsFormedAndOnlineAndAllowTrading() || Position != 0m
			|| _pendingOrder is not null && _pendingOrder.State is not (OrderStates.Done or OrderStates.Failed))
			return;
		var bb = (IBollingerBandsValue)values[0];
		var adx = (IAverageDirectionalIndexValue)values[4];
		if (bb.MovingAverage is not decimal basis || bb.UpBand is not decimal upper || adx.MovingAverage is not decimal strength)
			return;
		var age = (candle.OpenTime.TimeOfDay - SessionStart + TimeSpan.FromDays(1)).Ticks % TimeSpan.TicksPerDay;
		var close = candle.ClosePrice;
		var atr = values[5].ToDecimal();
		if (age >= SessionDuration.Ticks || close <= resistance || close < candle.OpenPrice
			|| Math.Abs(close - basis) > (upper - basis) * BasisProximity
			|| close <= values[1].ToDecimal() || values[1].ToDecimal() <= values[2].ToDecimal()
			|| values[3].ToDecimal() <= RsiThreshold || strength <= AdxThreshold || atr <= 0m)
			return;
		_stopDistance ??= new Unit();
		_takeDistance ??= new Unit();
		_stopDistance.Value = atr * AtrStopLossMultiplier;
		_takeDistance.Value = atr * AtrTakeProfitMultiplier;
		if (!_protectionStarted)
		{
			StartProtection(_takeDistance, _stopDistance, useMarketOrders: true, isLocalStop: true);
			_protectionStarted = true;
		}
		RegisterOrder(new Order
		{
			Security = Security,
			Portfolio = Portfolio,
			Type = OrderTypes.Market,
			Side = Sides.Buy,
			Volume = Volume,
			Comment = "Post-open breakout entry",
		});
	}
}
