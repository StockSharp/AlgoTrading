using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// BONK Long Volatility strategy.
/// Long only: buys when SMA(MaFastLength) is above SMA(MaSlowLength), the candle range exceeds AtrMultiplier times ATR(AtrLength),
/// RSI lies between RsiOversold and RsiOverbought, the MACD line is above its signal and above zero, volume exceeds VolumeThreshold
/// times its VolumeSmaLength average, the close is above the fast SMA and the candle is no older than LookbackDays. The long closes on
/// the percent take profit or stop loss, or when the close falls below a trailing stop AtrMultiplier ATRs under the highest close
/// since entry.
/// </summary>
public class BonkLongVolatilityStrategy : Strategy
{
	private readonly StrategyParam<decimal> _profitTargetPercent;
	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<decimal> _atrMultiplier;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _rsiOverbought;
	private readonly StrategyParam<decimal> _rsiOversold;
	private readonly StrategyParam<int> _macdFast;
	private readonly StrategyParam<int> _macdSlow;
	private readonly StrategyParam<int> _macdSignal;
	private readonly StrategyParam<int> _volumeSmaLength;
	private readonly StrategyParam<decimal> _volumeThreshold;
	private readonly StrategyParam<int> _maFastLength;
	private readonly StrategyParam<int> _maSlowLength;
	private readonly StrategyParam<int> _lookbackDays;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeSma;
	private decimal? _highestClose;

	/// <summary>
	/// Take profit percentage from entry price.
	/// </summary>
	public decimal ProfitTargetPercent
	{
		get => _profitTargetPercent.Value;
		set => _profitTargetPercent.Value = value;
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
	/// ATR period.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ATR multiple of the range filter and the trailing stop.
	/// </summary>
	public decimal AtrMultiplier
	{
		get => _atrMultiplier.Value;
		set => _atrMultiplier.Value = value;
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
	/// Upper RSI bound for entries.
	/// </summary>
	public decimal RsiOverbought
	{
		get => _rsiOverbought.Value;
		set => _rsiOverbought.Value = value;
	}

	/// <summary>
	/// Lower RSI bound for entries.
	/// </summary>
	public decimal RsiOversold
	{
		get => _rsiOversold.Value;
		set => _rsiOversold.Value = value;
	}

	/// <summary>
	/// Fast EMA period of MACD.
	/// </summary>
	public int MacdFast
	{
		get => _macdFast.Value;
		set => _macdFast.Value = value;
	}

	/// <summary>
	/// Slow EMA period of MACD.
	/// </summary>
	public int MacdSlow
	{
		get => _macdSlow.Value;
		set => _macdSlow.Value = value;
	}

	/// <summary>
	/// Signal line period of MACD.
	/// </summary>
	public int MacdSignal
	{
		get => _macdSignal.Value;
		set => _macdSignal.Value = value;
	}

	/// <summary>
	/// Candles of the average volume.
	/// </summary>
	public int VolumeSmaLength
	{
		get => _volumeSmaLength.Value;
		set => _volumeSmaLength.Value = value;
	}

	/// <summary>
	/// Multiple of the average volume the candle volume has to exceed.
	/// </summary>
	public decimal VolumeThreshold
	{
		get => _volumeThreshold.Value;
		set => _volumeThreshold.Value = value;
	}

	/// <summary>
	/// Fast SMA period.
	/// </summary>
	public int MaFastLength
	{
		get => _maFastLength.Value;
		set => _maFastLength.Value = value;
	}

	/// <summary>
	/// Slow SMA period.
	/// </summary>
	public int MaSlowLength
	{
		get => _maSlowLength.Value;
		set => _maSlowLength.Value = value;
	}

	/// <summary>
	/// Maximum age of a signal candle in days.
	/// </summary>
	public int LookbackDays
	{
		get => _lookbackDays.Value;
		set => _lookbackDays.Value = value;
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
	public BonkLongVolatilityStrategy()
	{
		_profitTargetPercent = Param(nameof(ProfitTargetPercent), 5.0m)
			.SetNotNegative()
			.SetDisplay("Profit Target %", "Take profit percentage from entry price", "Risk");

		_stopLossPercent = Param(nameof(StopLossPercent), 3.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss %", "Stop loss percentage from entry price", "Risk");

		_atrLength = Param(nameof(AtrLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period", "Volatility");

		_atrMultiplier = Param(nameof(AtrMultiplier), 1.5m)
			.SetGreaterThanZero()
			.SetDisplay("ATR Multiplier", "ATR multiple of the range filter and the trailing stop", "Volatility");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Momentum");

		_rsiOverbought = Param(nameof(RsiOverbought), 65m)
			.SetDisplay("RSI Overbought", "Upper RSI bound for entries", "Momentum");

		_rsiOversold = Param(nameof(RsiOversold), 35m)
			.SetDisplay("RSI Oversold", "Lower RSI bound for entries", "Momentum");

		_macdFast = Param(nameof(MacdFast), 12)
			.SetGreaterThanZero()
			.SetDisplay("MACD Fast", "Fast EMA period of MACD", "Momentum");

		_macdSlow = Param(nameof(MacdSlow), 26)
			.SetGreaterThanZero()
			.SetDisplay("MACD Slow", "Slow EMA period of MACD", "Momentum");

		_macdSignal = Param(nameof(MacdSignal), 9)
			.SetGreaterThanZero()
			.SetDisplay("MACD Signal", "Signal line period of MACD", "Momentum");

		_volumeSmaLength = Param(nameof(VolumeSmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume SMA Length", "Candles of the average volume", "Volume");

		_volumeThreshold = Param(nameof(VolumeThreshold), 1.5m)
			.SetNotNegative()
			.SetDisplay("Volume Threshold", "Multiple of the average volume the candle volume has to exceed", "Volume");

		_maFastLength = Param(nameof(MaFastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast MA Length", "Fast SMA period", "Trend");

		_maSlowLength = Param(nameof(MaSlowLength), 13)
			.SetGreaterThanZero()
			.SetDisplay("Slow MA Length", "Slow SMA period", "Trend");

		_lookbackDays = Param(nameof(LookbackDays), 30)
			.SetGreaterThanZero()
			.SetDisplay("Lookback Days", "Maximum age of a signal candle in days", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
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
		_volumeSma = null;
		_highestClose = null;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_highestClose = null;
		_volumeSma = new SimpleMovingAverage { Length = VolumeSmaLength };

		var fastMa = new SimpleMovingAverage { Length = MaFastLength };
		var slowMa = new SimpleMovingAverage { Length = MaSlowLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var macd = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = MacdFast },
				LongMa = { Length = MacdSlow },
			},
			SignalMa = { Length = MacdSignal }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(fastMa, slowMa, atr, rsi, macd, ProcessCandle)
			.Start();

		StartProtection(new Unit(ProfitTargetPercent, UnitTypes.Percent), new Unit(StopLossPercent, UnitTypes.Percent), useMarketOrders: true, isLocalStop: true);

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
			DrawIndicator(area, fastMa);
			DrawIndicator(area, slowMa);
			DrawOwnTrades(area);

			var oscillators = CreateChartArea();
			if (oscillators != null)
			{
				DrawIndicator(oscillators, rsi);
				DrawIndicator(oscillators, macd);
			}
		}
	}

	private void ObserveProtectionQuote(Level1ChangeMessage quote)
	{
		// The high-level handler activates native protection before this callback, also between signal bars.
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue fastValue, IIndicatorValue slowValue, IIndicatorValue atrValue, IIndicatorValue rsiValue, IIndicatorValue macdValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeValue = _volumeSma.Process(new DecimalIndicatorValue(_volumeSma, candle.TotalVolume, candle.OpenTime) { IsFinal = true });

		if (!fastValue.IsFormed || !slowValue.IsFormed || !atrValue.IsFormed || !rsiValue.IsFormed || !macdValue.IsFormed || !volumeValue.IsFormed)
			return;

		var macdTyped = (MovingAverageConvergenceDivergenceSignalValue)macdValue;

		if (macdTyped.Macd is not decimal macd || macdTyped.Signal is not decimal signal)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var close = candle.ClosePrice;
		var atr = atrValue.GetValue<decimal>();

		if (Position > 0)
		{
			_highestClose = _highestClose is decimal h ? Math.Max(h, close) : close;

			if (close < _highestClose.Value - atr * AtrMultiplier)
			{
				SellMarket(Position);
				_highestClose = null;
			}

			return;
		}

		_highestClose = null;

		var fast = fastValue.GetValue<decimal>();
		var slow = slowValue.GetValue<decimal>();
		var rsi = rsiValue.GetValue<decimal>();
		var recent = candle.OpenTime >= CurrentTime.AddDays(-LookbackDays);

		if (Position == 0
			&& fast > slow
			&& candle.HighPrice - candle.LowPrice > atr * AtrMultiplier
			&& rsi > RsiOversold && rsi < RsiOverbought
			&& macd > signal && macd > 0
			&& candle.TotalVolume > volumeValue.GetValue<decimal>() * VolumeThreshold
			&& close > fast
			&& recent)
		{
			BuyMarket(Volume);
			_highestClose = close;
		}
	}
}
