using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// NAS100 and gold smart scalping strategy.
/// Between StartHour and EndHour (UTC) a long opens when the close is above EMA9, the session VWAP and the 15 minute EMA200,
/// RSI is above 50 and volume spikes above its average; a short is the mirror image. An opposite signal reverses the position.
/// The stop loss and take profit are ATR multiples from the entry, optionally trailing, the size risks RiskPercent of equity
/// on the stop distance and a new entry waits CooldownMins after the previous one.
/// </summary>
public class Nas100AndGoldSmartScalpingProEnhancedV2Strategy : Strategy
{
	private const int _emaLength = 9;
	private const int _rsiLength = 14;
	private const int _atrLength = 14;
	private const int _trendLength = 200;
	private const int _volumeLength = 20;
	private const decimal _volumeSpikeFactor = 1.5m;

	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _riskPercent;
	private readonly StrategyParam<decimal> _atrMultiplierSl;
	private readonly StrategyParam<decimal> _atrMultiplierTp;
	private readonly StrategyParam<int> _cooldownMins;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<bool> _useTrailing;

	private decimal? _trendEma;
	private DateTime _vwapDate;
	private decimal _vwapPriceVolume;
	private decimal _vwapVolume;
	private DateTime? _lastEntryTime;
	private decimal _stopPrice;
	private decimal _takePrice;
	private decimal _stopDistance;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Percent of equity risked on the stop distance.
	/// </summary>
	public decimal RiskPercent
	{
		get => _riskPercent.Value;
		set => _riskPercent.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the stop loss.
	/// </summary>
	public decimal AtrMultiplierSl
	{
		get => _atrMultiplierSl.Value;
		set => _atrMultiplierSl.Value = value;
	}

	/// <summary>
	/// ATR multiplier for the take profit.
	/// </summary>
	public decimal AtrMultiplierTp
	{
		get => _atrMultiplierTp.Value;
		set => _atrMultiplierTp.Value = value;
	}

	/// <summary>
	/// Minutes to wait after an entry before the next one.
	/// </summary>
	public int CooldownMins
	{
		get => _cooldownMins.Value;
		set => _cooldownMins.Value = value;
	}

	/// <summary>
	/// Session start hour (UTC).
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour (UTC).
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Trail the stop by the ATR stop distance.
	/// </summary>
	public bool UseTrailing
	{
		get => _useTrailing.Value;
		set => _useTrailing.Value = value;
	}

	/// <summary>
	/// Constructor.
	/// </summary>
	public Nas100AndGoldSmartScalpingProEnhancedV2Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");

		_riskPercent = Param(nameof(RiskPercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Risk %", "Percent of equity risked per trade", "Risk");

		_atrMultiplierSl = Param(nameof(AtrMultiplierSl), 1m)
			.SetGreaterThanZero()
			.SetDisplay("ATR SL Mult", "ATR multiplier for the stop loss", "Risk");

		_atrMultiplierTp = Param(nameof(AtrMultiplierTp), 2m)
			.SetGreaterThanZero()
			.SetDisplay("ATR TP Mult", "ATR multiplier for the take profit", "Risk");

		_cooldownMins = Param(nameof(CooldownMins), 30)
			.SetNotNegative()
			.SetDisplay("Cooldown (min)", "Minutes between entries", "General");

		_startHour = Param(nameof(StartHour), 13)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (UTC)", "Session");

		_endHour = Param(nameof(EndHour), 20)
			.SetRange(0, 24)
			.SetDisplay("End Hour", "Session end hour (UTC)", "Session");

		_useTrailing = Param(nameof(UseTrailing), false)
			.SetDisplay("Use Trailing", "Trail the stop by the ATR stop distance", "Risk");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType), (Security, TimeSpan.FromMinutes(15).TimeFrame())];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	private void ResetState()
	{
		_trendEma = null;
		_vwapDate = default;
		_vwapPriceVolume = 0m;
		_vwapVolume = 0m;
		_lastEntryTime = null;
		_stopPrice = 0m;
		_takePrice = 0m;
		_stopDistance = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var ema = new ExponentialMovingAverage { Length = _emaLength };
		var rsi = new RelativeStrengthIndex { Length = _rsiLength };
		var atr = new AverageTrueRange { Length = _atrLength };
		var volumeAverage = new SimpleMovingAverage { Length = _volumeLength };
		var trendEma = new ExponentialMovingAverage { Length = _trendLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ema, rsi, atr, (candle, emaValue, rsiValue, atrValue) => ProcessCandle(candle, emaValue, rsiValue, atrValue, volumeAverage))
			.Start();

		SubscribeCandles(TimeSpan.FromMinutes(15).TimeFrame())
			.Bind(trendEma, (candle, value) =>
			{
				if (candle.State == CandleStates.Finished && trendEma.IsFormed)
					_trendEma = value;
			})
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal emaValue, decimal rsiValue, decimal atrValue, SimpleMovingAverage volumeAverage)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var avgValue = volumeAverage.Process(new DecimalIndicatorValue(volumeAverage, candle.TotalVolume, candle.ServerTime) { IsFinal = true });

		// Session VWAP restarts every UTC day.
		var date = candle.OpenTime.Date;
		if (date != _vwapDate)
		{
			_vwapDate = date;
			_vwapPriceVolume = 0m;
			_vwapVolume = 0m;
		}

		var typical = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;
		_vwapPriceVolume += typical * candle.TotalVolume;
		_vwapVolume += candle.TotalVolume;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (ManagePosition(candle))
			return;

		if (_trendEma is not decimal trend || !volumeAverage.IsFormed || _vwapVolume <= 0 || atrValue <= 0)
			return;

		var hour = candle.OpenTime.Hour;
		if (hour < StartHour || hour >= EndHour)
			return;

		if (_lastEntryTime is DateTime last && candle.OpenTime - last < TimeSpan.FromMinutes(CooldownMins))
			return;

		var close = candle.ClosePrice;
		var vwap = _vwapPriceVolume / _vwapVolume;
		var volumeSpike = candle.TotalVolume > avgValue.ToDecimal() * _volumeSpikeFactor;

		var longSignal = close > emaValue && close > vwap && rsiValue > 50 && close > trend && volumeSpike;
		var shortSignal = close < emaValue && close < vwap && rsiValue < 50 && close < trend && volumeSpike;

		if (longSignal && Position <= 0)
		{
			Enter(candle, true, atrValue);
		}
		else if (shortSignal && Position >= 0)
		{
			Enter(candle, false, atrValue);
		}
	}

	private void Enter(ICandleMessage candle, bool isLong, decimal atr)
	{
		var close = candle.ClosePrice;
		_stopDistance = atr * AtrMultiplierSl;

		var volume = Volume;
		var equity = Portfolio?.CurrentValue ?? 0m;
		if (equity > 0 && _stopDistance > 0)
		{
			var riskVolume = equity * RiskPercent / 100m / _stopDistance;
			if (riskVolume > 0)
				volume = riskVolume;
		}

		if (isLong)
		{
			BuyMarket(volume + Math.Abs(Position));
			_stopPrice = close - _stopDistance;
			_takePrice = close + atr * AtrMultiplierTp;
		}
		else
		{
			SellMarket(volume + Math.Abs(Position));
			_stopPrice = close + _stopDistance;
			_takePrice = close - atr * AtrMultiplierTp;
		}

		_lastEntryTime = candle.OpenTime;
	}

	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}

			if (UseTrailing)
				_stopPrice = Math.Max(_stopPrice, candle.ClosePrice - _stopDistance);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(-Position);
				return true;
			}

			if (UseTrailing)
				_stopPrice = Math.Min(_stopPrice, candle.ClosePrice + _stopDistance);
		}

		return false;
	}
}
