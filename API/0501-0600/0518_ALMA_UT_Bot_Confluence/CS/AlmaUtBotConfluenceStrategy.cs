using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// ALMA and UT Bot confluence strategy.
/// A long opens on a UT Bot buy signal when the close is above the long EMA and ALMA, volume is above its average, RSI is above
/// 30, ADX is above 30, the close is below the upper Bollinger Band and ATR is at least MinAtr. A short opens when the close
/// crosses below the fast EMA while the UT Bot is bearish under the mirrored filters. Entries are spaced by BaseCooldownBars.
/// A position closes when the UT Bot trailing stop flips against it or when price reaches the ATR stop-loss or take-profit
/// fixed at entry.
/// </summary>
public class AlmaUtBotConfluenceStrategy : Strategy
{
	private const int _almaLength = 9;
	private const decimal _almaOffset = 0.85m;
	private const int _almaSigma = 6;
	private const int _bbLength = 20;
	private const decimal _rsiLongLevel = 30m;
	private const decimal _rsiShortLevel = 70m;
	private const decimal _adxLevel = 30m;

	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<int> _utAtrPeriod;
	private readonly StrategyParam<decimal> _utKeyValue;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<int> _baseCooldownBars;
	private readonly StrategyParam<decimal> _minAtr;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private decimal? _utStop;
	private decimal _prevClose;
	private decimal? _prevFastEma;
	private int _barIndex;
	private int? _lastEntryBar;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Long-term EMA period.
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// ATR period of the stop-loss and take-profit.
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ADX period.
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
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
	/// Bollinger Bands width multiplier.
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in ATR multiples.
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Take-profit distance in ATR multiples.
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR period of the UT Bot.
	/// </summary>
	public int UtAtrPeriod
	{
		get => _utAtrPeriod.Value;
		set => _utAtrPeriod.Value = value;
	}

	/// <summary>
	/// UT Bot key value (ATR multiplier of the trailing stop).
	/// </summary>
	public decimal UtKeyValue
	{
		get => _utKeyValue.Value;
		set => _utKeyValue.Value = value;
	}

	/// <summary>
	/// Period of the volume average.
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Minimum bars between entries.
	/// </summary>
	public int BaseCooldownBars
	{
		get => _baseCooldownBars.Value;
		set => _baseCooldownBars.Value = value;
	}

	/// <summary>
	/// Minimum ATR required for entries.
	/// </summary>
	public decimal MinAtr
	{
		get => _minAtr.Value;
		set => _minAtr.Value = value;
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
	public AlmaUtBotConfluenceStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA Length", "Fast EMA period", "Trend");

		_emaLength = Param(nameof(EmaLength), 72)
			.SetGreaterThanZero()
			.SetDisplay("EMA Length", "Long-term EMA period", "Trend");

		_atrLength = Param(nameof(AtrLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("ATR Length", "ATR period of the stop-loss and take-profit", "Risk");

		_adxLength = Param(nameof(AdxLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("ADX Length", "ADX period", "Filters");

		_rsiLength = Param(nameof(RsiLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI Length", "RSI period", "Filters");

		_bbMultiplier = Param(nameof(BbMultiplier), 3.0m)
			.SetGreaterThanZero()
			.SetDisplay("BB Multiplier", "Bollinger Bands width multiplier", "Filters");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 5.0m)
			.SetNotNegative()
			.SetDisplay("Stop Loss ATR", "Stop-loss distance in ATR multiples", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 4.0m)
			.SetNotNegative()
			.SetDisplay("Take Profit ATR", "Take-profit distance in ATR multiples", "Risk");

		_utAtrPeriod = Param(nameof(UtAtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("UT ATR Period", "ATR period of the UT Bot", "UT Bot");

		_utKeyValue = Param(nameof(UtKeyValue), 1m)
			.SetGreaterThanZero()
			.SetDisplay("UT Key Value", "ATR multiplier of the UT Bot trailing stop", "UT Bot");

		_volumeMaLength = Param(nameof(VolumeMaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Period of the volume average", "Filters");

		_baseCooldownBars = Param(nameof(BaseCooldownBars), 7)
			.SetNotNegative()
			.SetDisplay("Cooldown Bars", "Minimum bars between entries", "Filters");

		_minAtr = Param(nameof(MinAtr), 0.005m)
			.SetNotNegative()
			.SetDisplay("Min ATR", "Minimum ATR required for entries", "Filters");

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
		_volumeMa = null;
		ResetState();
	}

	private void ResetState()
	{
		_utStop = null;
		_prevClose = 0m;
		_prevFastEma = null;
		_barIndex = 0;
		_lastEntryBar = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var alma = new ArnaudLegouxMovingAverage { Length = _almaLength, Offset = _almaOffset, Sigma = _almaSigma };
		var atr = new AverageTrueRange { Length = AtrLength };
		var utAtr = new AverageTrueRange { Length = UtAtrPeriod };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var bollinger = new BollingerBands { Length = _bbLength, Width = BbMultiplier };
		_volumeMa = new SimpleMovingAverage { Length = VolumeMaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { ema, fastEma, alma, atr, utAtr, rsi, adx, bollinger }, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, alma);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var volumeMa = _volumeMa.Process(candle.TotalVolume, candle.ServerTime, true).ToDecimal();
		var close = candle.ClosePrice;
		var utAtrValue = values[4];

		if (!utAtrValue.IsFormed)
		{
			_prevClose = close;
			return;
		}

		// UT Bot: an ATR trailing stop that ratchets with price and flips when price closes through it.
		var nLoss = UtKeyValue * utAtrValue.ToDecimal();
		var prevStop = _utStop ?? close - nLoss;
		var prevClose = _prevClose;
		decimal utStop;

		if (close > prevStop && prevClose > prevStop)
			utStop = Math.Max(prevStop, close - nLoss);
		else if (close < prevStop && prevClose < prevStop)
			utStop = Math.Min(prevStop, close + nLoss);
		else
			utStop = close > prevStop ? close - nLoss : close + nLoss;

		var utBuy = _utStop is not null && prevClose <= prevStop && close > utStop;
		var utSell = _utStop is not null && prevClose >= prevStop && close < utStop;
		var utBearish = close < utStop;

		_utStop = utStop;
		_prevClose = close;
		_barIndex++;

		var fastEmaValue = values[1];
		var prevFastEma = _prevFastEma;
		if (fastEmaValue.IsFormed)
			_prevFastEma = fastEmaValue.ToDecimal();

		foreach (var value in values)
		{
			if (!value.IsFormed)
				return;
		}

		if (!_volumeMa.IsFormed || prevFastEma is not decimal prevFast)
			return;

		if (values[6] is not IAverageDirectionalIndexValue { MovingAverage: decimal adx })
			return;

		if (values[7] is not BollingerBandsValue { UpBand: decimal upperBand, LowBand: decimal lowerBand })
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var ema = values[0].ToDecimal();
		var fastEma = values[1].ToDecimal();
		var alma = values[2].ToDecimal();
		var atr = values[3].ToDecimal();
		var rsi = values[5].ToDecimal();

		var cooldownOk = _lastEntryBar is not int last || _barIndex - last >= BaseCooldownBars;
		var commonFilters = cooldownOk && candle.TotalVolume > volumeMa && adx > _adxLevel && atr >= MinAtr;

		var longSignal = commonFilters && utBuy && close > ema && close > alma && rsi > _rsiLongLevel && close < upperBand;
		var crossBelowFast = prevClose >= prevFast && close < fastEma;
		var shortSignal = commonFilters && utBearish && crossBelowFast && close < ema && close < alma && rsi < _rsiShortLevel && close > lowerBand;

		if (longSignal && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			_lastEntryBar = _barIndex;
			_stopPrice = close - StopLossAtrMultiplier * atr;
			_takePrice = close + TakeProfitAtrMultiplier * atr;
		}
		else if (shortSignal && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			_lastEntryBar = _barIndex;
			_stopPrice = close + StopLossAtrMultiplier * atr;
			_takePrice = close - TakeProfitAtrMultiplier * atr;
		}
		else if (Position > 0)
		{
			var hitStop = StopLossAtrMultiplier > 0m && candle.LowPrice <= _stopPrice;
			var hitTake = TakeProfitAtrMultiplier > 0m && candle.HighPrice >= _takePrice;

			if (utSell || hitStop || hitTake)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			var hitStop = StopLossAtrMultiplier > 0m && candle.HighPrice >= _stopPrice;
			var hitTake = TakeProfitAtrMultiplier > 0m && candle.LowPrice <= _takePrice;

			if (utBuy || hitStop || hitTake)
				BuyMarket(-Position);
		}
	}
}
