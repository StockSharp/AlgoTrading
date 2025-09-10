using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that combines ALMA filter with UT Bot trailing stop.
/// Enters long when multiple conditions align and UT Bot gives a buy signal.
/// Short entries occur on UT Bot sell signals and fast EMA crossunder.
/// Exits are handled either by UT Bot trailing stop or ATR-based stop/target.
/// </summary>
public class AlmaUtBotConfluenceStrategy : Strategy
{
	private readonly StrategyParam<int> _fastEmaLength;
	private readonly StrategyParam<int> _emaLength;
	private readonly StrategyParam<int> _atrLength;
	private readonly StrategyParam<int> _adxLength;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<decimal> _bbMultiplier;
	private readonly StrategyParam<decimal> _stopLossAtrMultiplier;
	private readonly StrategyParam<decimal> _takeProfitAtrMultiplier;
	private readonly StrategyParam<int> _timeBasedExit;
	private readonly StrategyParam<int> _utKeyValue;
	private readonly StrategyParam<int> _utAtrPeriod;
	private readonly StrategyParam<int> _volumeMaLength;
	private readonly StrategyParam<decimal> _minVolumeMultiplier;
	private readonly StrategyParam<int> _baseCooldownBars;
	private readonly StrategyParam<decimal> _minAtr;
	private readonly StrategyParam<bool> _useUtExit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _xAtrTrailingStop;
	private decimal _prevSrc;
	private decimal _prevStop;
	private decimal _prevFastEma;
	private decimal _prevClose;
	private int _barIndex;
	private int? _lastBuyIndex;
	private int? _entryIndex;
	private decimal _entryPrice;
	private string _lastSignal = string.Empty;

	private const decimal RsiOversold = 30m;
	private const decimal AdxThreshold = 30m;

	/// <summary>
	/// Length for fast EMA (default: 20).
	/// </summary>
	public int FastEmaLength
	{
		get => _fastEmaLength.Value;
		set => _fastEmaLength.Value = value;
	}

	/// <summary>
	/// Length for long-term EMA (default: 72).
	/// </summary>
	public int EmaLength
	{
		get => _emaLength.Value;
		set => _emaLength.Value = value;
	}

	/// <summary>
	/// ATR length (default: 14).
	/// </summary>
	public int AtrLength
	{
		get => _atrLength.Value;
		set => _atrLength.Value = value;
	}

	/// <summary>
	/// ADX length (default: 10).
	/// </summary>
	public int AdxLength
	{
		get => _adxLength.Value;
		set => _adxLength.Value = value;
	}

	/// <summary>
	/// RSI length (default: 14).
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Bollinger Band multiplier (default: 3.0).
	/// </summary>
	public decimal BbMultiplier
	{
		get => _bbMultiplier.Value;
		set => _bbMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for stop loss (default: 5.0).
	/// </summary>
	public decimal StopLossAtrMultiplier
	{
		get => _stopLossAtrMultiplier.Value;
		set => _stopLossAtrMultiplier.Value = value;
	}

	/// <summary>
	/// ATR multiplier for take profit (default: 4.0).
	/// </summary>
	public decimal TakeProfitAtrMultiplier
	{
		get => _takeProfitAtrMultiplier.Value;
		set => _takeProfitAtrMultiplier.Value = value;
	}

	/// <summary>
	/// Time-based exit in bars (default: 0 - disabled).
	/// </summary>
	public int TimeBasedExit
	{
		get => _timeBasedExit.Value;
		set => _timeBasedExit.Value = value;
	}

	/// <summary>
	/// UT Bot key value (default: 1).
	/// </summary>
	public int UtKeyValue
	{
		get => _utKeyValue.Value;
		set => _utKeyValue.Value = value;
	}

	/// <summary>
	/// ATR period for UT Bot (default: 10).
	/// </summary>
	public int UtAtrPeriod
	{
		get => _utAtrPeriod.Value;
		set => _utAtrPeriod.Value = value;
	}

	/// <summary>
	/// Volume MA length for filter (default: 20).
	/// </summary>
	public int VolumeMaLength
	{
		get => _volumeMaLength.Value;
		set => _volumeMaLength.Value = value;
	}

	/// <summary>
	/// Minimum volume multiplier (default: 0.8).
	/// </summary>
	public decimal MinVolumeMultiplier
	{
		get => _minVolumeMultiplier.Value;
		set => _minVolumeMultiplier.Value = value;
	}

	/// <summary>
	/// Base cooldown in bars between buys (default: 7).
	/// </summary>
	public int BaseCooldownBars
	{
		get => _baseCooldownBars.Value;
		set => _baseCooldownBars.Value = value;
	}

	/// <summary>
	/// Minimum ATR value to allow trades (default: 0.005).
	/// </summary>
	public decimal MinAtr
	{
		get => _minAtr.Value;
		set => _minAtr.Value = value;
	}

	/// <summary>
	/// Use UT Bot trailing stop for exits (default: true).
	/// </summary>
	public bool UseUtExit
	{
		get => _useUtExit.Value;
		set => _useUtExit.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public AlmaUtBotConfluenceStrategy()
	{
		_fastEmaLength = Param(nameof(FastEmaLength), 20).SetDisplay("Fast EMA Length", "Length for fast EMA", "Main");

		_emaLength = Param(nameof(EmaLength), 72).SetDisplay("EMA Length", "Length for long-term EMA", "Main");

		_atrLength = Param(nameof(AtrLength), 14).SetDisplay("ATR Length", "ATR period", "Main");

		_adxLength = Param(nameof(AdxLength), 10).SetDisplay("ADX Length", "ADX period", "Main");

		_rsiLength = Param(nameof(RsiLength), 14).SetDisplay("RSI Length", "RSI period", "Main");

		_bbMultiplier =
			Param(nameof(BbMultiplier), 3m).SetDisplay("Bollinger Multiplier", "Bollinger Band width", "Main");

		_stopLossAtrMultiplier = Param(nameof(StopLossAtrMultiplier), 5m)
									 .SetDisplay("Stop Loss ATR Mult", "ATR multiplier for stop loss", "Risk");

		_takeProfitAtrMultiplier = Param(nameof(TakeProfitAtrMultiplier), 4m)
									   .SetDisplay("Take Profit ATR Mult", "ATR multiplier for take profit", "Risk");

		_timeBasedExit =
			Param(nameof(TimeBasedExit), 0).SetDisplay("Time Exit Bars", "Bars after which exit occurs", "Risk");

		_utKeyValue = Param(nameof(UtKeyValue), 1).SetDisplay("UT Key", "UT Bot key value", "UT Bot");

		_utAtrPeriod = Param(nameof(UtAtrPeriod), 10).SetDisplay("UT ATR Period", "ATR period for UT Bot", "UT Bot");

		_volumeMaLength =
			Param(nameof(VolumeMaLength), 20).SetDisplay("Volume MA Length", "SMA period for volume", "Filters");

		_minVolumeMultiplier = Param(nameof(MinVolumeMultiplier), 0.8m)
								   .SetDisplay("Min Volume Mult", "Volume must exceed SMA * multiplier", "Filters");

		_baseCooldownBars =
			Param(nameof(BaseCooldownBars), 7).SetDisplay("Base Cooldown", "Base cooldown in bars", "Filters");

		_minAtr = Param(nameof(MinAtr), 0.005m).SetDisplay("Min ATR", "Minimum ATR to allow trades", "Filters");

		_useUtExit =
			Param(nameof(UseUtExit), true).SetDisplay("Use UT Exit", "Use UT Bot trailing stop for exits", "Exit");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
						  .SetDisplay("Candle Type", "Type of candles", "Main");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var fastEma = new ExponentialMovingAverage { Length = FastEmaLength };
		var alma = new ArnaudLegouxMovingAverage { Length = 15, Offset = 0.65m, Sigma = 6m };
		var ema = new ExponentialMovingAverage { Length = EmaLength };
		var adx = new AverageDirectionalIndex { Length = AdxLength };
		var rsi = new RelativeStrengthIndex { Length = RsiLength };
		var atr = new AverageTrueRange { Length = AtrLength };
		var bollinger = new BollingerBands { Length = 20, Width = BbMultiplier };
		var atrUt = new AverageTrueRange { Length = UtAtrPeriod };
		var volumeSma = new SimpleMovingAverage { Length = VolumeMaLength };
		var atrAvg = new SimpleMovingAverage { Length = 50 };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(new IIndicator[] { fastEma, alma, ema, adx, rsi, atr, bollinger, atrUt, volumeSma, atrAvg },
					ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, fastEma);
			DrawIndicator(area, alma);
			DrawIndicator(area, ema);
			DrawIndicator(area, bollinger);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue[] values)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var fastEma = values[0].ToDecimal();
		var alma = values[1].ToDecimal();
		var ema = values[2].ToDecimal();
		var adxValue = (AverageDirectionalIndexValue)values[3];
		if (adxValue.MovingAverage is not decimal adx)
			return;
		var rsi = values[4].ToDecimal();
		var atr = values[5].ToDecimal();
		var bb = (BollingerBandsValue)values[6];
		if (bb.UpBand is not decimal bbUpper || bb.LowBand is not decimal bbLower)
			return;
		var atrUt = values[7].ToDecimal();
		var volumeMa = values[8].ToDecimal();
		var atrAvg = values[9].ToDecimal();

		var src = candle.ClosePrice;
		var nLoss = UtKeyValue * atrUt;

		if (_barIndex == 0)
			_xAtrTrailingStop = src + nLoss;

		if (src > _prevStop && _prevSrc > _prevStop)
			_xAtrTrailingStop = Math.Max(_prevStop, src - nLoss);
		else if (src < _prevStop && _prevSrc < _prevStop)
			_xAtrTrailingStop = Math.Min(_prevStop, src + nLoss);
		else
			_xAtrTrailingStop = src > _prevStop ? src - nLoss : src + nLoss;

		var buyUt = src > _xAtrTrailingStop && _prevSrc <= _prevStop;
		var sellUt = src < _xAtrTrailingStop && _prevSrc >= _prevStop;

		bool volumeFilter = candle.TotalVolume > volumeMa * MinVolumeMultiplier;
		bool volatilityFilter = atr > MinAtr;
		int cooldown = atrAvg > 0 ? Math.Max(1, (int)Math.Round(BaseCooldownBars * (atr / atrAvg))) : BaseCooldownBars;

		bool crossUnderFastEma = _prevClose > _prevFastEma && candle.ClosePrice < fastEma;

		bool buyCondition = volatilityFilter && volumeFilter && candle.ClosePrice > ema && candle.ClosePrice > alma &&
							rsi > RsiOversold && adx > AdxThreshold && candle.ClosePrice < bbUpper && buyUt &&
							(_lastBuyIndex is null || _barIndex - _lastBuyIndex > cooldown) && _lastSignal != "BUY";

		bool sellCondition = volatilityFilter && volumeFilter && crossUnderFastEma && sellUt && _lastSignal != "SELL";

		if (buyCondition && Position <= 0)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
			_lastBuyIndex = _barIndex;
			_entryIndex = _barIndex;
			_entryPrice = candle.ClosePrice;
			_lastSignal = "BUY";
		}
		else if (sellCondition && Position >= 0)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
			_entryIndex = _barIndex;
			_entryPrice = candle.ClosePrice;
			_lastSignal = "SELL";
		}
		else
		{
			ManageExit(candle, atr, src);
		}

		_prevFastEma = fastEma;
		_prevClose = candle.ClosePrice;
		_prevSrc = src;
		_prevStop = _xAtrTrailingStop;
		_barIndex++;
	}

	private void ManageExit(ICandleMessage candle, decimal atr, decimal src)
	{
		if (UseUtExit)
		{
			if (Position > 0 && src < _xAtrTrailingStop && _prevSrc >= _prevStop)
			{
				SellMarket(Position);
				_lastSignal = string.Empty;
			}
			else if (Position < 0 && src > _xAtrTrailingStop && _prevSrc <= _prevStop)
			{
				BuyMarket(Math.Abs(Position));
				_lastSignal = string.Empty;
			}
		}
		else if (Position != 0)
		{
			var stopLoss = atr * StopLossAtrMultiplier;
			var takeProfit = atr * TakeProfitAtrMultiplier;
			var timeExit = TimeBasedExit > 0 && _entryIndex.HasValue && _barIndex - _entryIndex.Value >= TimeBasedExit;

			if (Position > 0)
			{
				if (candle.ClosePrice <= _entryPrice - stopLoss || candle.ClosePrice >= _entryPrice + takeProfit ||
					timeExit)
				{
					SellMarket(Position);
					_lastSignal = string.Empty;
				}
			}
			else
			{
				if (candle.ClosePrice >= _entryPrice + stopLoss || candle.ClosePrice <= _entryPrice - takeProfit ||
					timeExit)
				{
					BuyMarket(Math.Abs(Position));
					_lastSignal = string.Empty;
				}
			}
		}
	}
}
