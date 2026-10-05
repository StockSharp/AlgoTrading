using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold breakout RR4 strategy.
/// The Donchian channel spans the previous DonchianLength candles. A close above its upper band with volume above the MaVolumeLength
/// volume SMA and the Larry Williams Large Trade Index above 50 goes long; a close below the lower band with high volume and LWTI below
/// 50 goes short. LWTI is the LwtiLength WMA of the close change over LwtiLength candles divided by the ATR, scaled around 50 and smoothed
/// with an SMA of LwtiSmooth. Entries are allowed only from StartHour to EndHour (UTC, wrapping over midnight) and once per day. The stop
/// sits at the channel middle and the target at RiskReward times that risk.
/// </summary>
public class GoldBreakoutRr4Strategy : Strategy
{
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _maVolumeLength;
	private readonly StrategyParam<int> _lwtiLength;
	private readonly StrategyParam<int> _lwtiSmooth;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _riskReward;
	private readonly StrategyParam<DataType> _candleType;

	private SimpleMovingAverage _volumeMa;
	private WeightedMovingAverage _changeWma;
	private SimpleMovingAverage _lwtiSma;
	private readonly List<decimal> _closes = [];
	private decimal? _prevUpper;
	private decimal? _prevLower;
	private DateTime? _lastTradeDay;
	private decimal _stopPrice;
	private decimal _takePrice;

	/// <summary>
	/// Donchian channel length.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Length of the volume average.
	/// </summary>
	public int MaVolumeLength
	{
		get => _maVolumeLength.Value;
		set => _maVolumeLength.Value = value;
	}

	/// <summary>
	/// LWTI length.
	/// </summary>
	public int LwtiLength
	{
		get => _lwtiLength.Value;
		set => _lwtiLength.Value = value;
	}

	/// <summary>
	/// LWTI smoothing length.
	/// </summary>
	public int LwtiSmooth
	{
		get => _lwtiSmooth.Value;
		set => _lwtiSmooth.Value = value;
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
	/// Target distance as a multiple of the stop distance.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
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
	public GoldBreakoutRr4Strategy()
	{
		_donchianLength = Param(nameof(DonchianLength), 96)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Length", "Donchian channel length", "Indicators");

		_maVolumeLength = Param(nameof(MaVolumeLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Volume MA Length", "Length of the volume average", "Indicators");

		_lwtiLength = Param(nameof(LwtiLength), 25)
			.SetGreaterThanZero()
			.SetDisplay("LWTI Length", "LWTI length", "Indicators");

		_lwtiSmooth = Param(nameof(LwtiSmooth), 5)
			.SetGreaterThanZero()
			.SetDisplay("LWTI Smooth", "LWTI smoothing length", "Indicators");

		_startHour = Param(nameof(StartHour), 20)
			.SetRange(0, 23)
			.SetDisplay("Start Hour", "Session start hour (UTC)", "Session");

		_endHour = Param(nameof(EndHour), 8)
			.SetRange(0, 23)
			.SetDisplay("End Hour", "Session end hour (UTC)", "Session");

		_riskReward = Param(nameof(RiskReward), 4m)
			.SetGreaterThanZero()
			.SetDisplay("Risk Reward", "Target distance as a multiple of the stop distance", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
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
		_closes.Clear();
		_prevUpper = null;
		_prevLower = null;
		_lastTradeDay = null;
		_stopPrice = 0m;
		_takePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		ResetState();

		var donchian = new DonchianChannels { Length = DonchianLength };
		var atr = new AverageTrueRange { Length = LwtiLength };

		_volumeMa = new SimpleMovingAverage { Length = MaVolumeLength };
		_changeWma = new WeightedMovingAverage { Length = LwtiLength };
		_lwtiSma = new SimpleMovingAverage { Length = LwtiSmooth };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(donchian, atr, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue donchianValue, IIndicatorValue atrValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// The channel is measured on the candles before this one.
		var upper = _prevUpper;
		var lower = _prevLower;

		if (donchianValue.IsFormed && donchianValue is IDonchianChannelsValue { UpperBand: decimal u, LowerBand: decimal l })
		{
			_prevUpper = u;
			_prevLower = l;
		}

		var volumeAvg = _volumeMa.Process(new DecimalIndicatorValue(_volumeMa, candle.TotalVolume, candle.OpenTime) { IsFinal = true });
		var lwti = ProcessLwti(candle, atrValue);

		if (ManagePosition(candle))
			return;

		if (upper is not decimal channelHigh || lower is not decimal channelLow || lwti is not decimal lwtiValue || !_volumeMa.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (Position != 0 || !InSession(candle.OpenTime.Hour) || _lastTradeDay == candle.OpenTime.Date)
			return;

		var close = candle.ClosePrice;
		var highVolume = candle.TotalVolume > volumeAvg.GetValue<decimal>();
		var middle = (channelHigh + channelLow) / 2;

		if (close > channelHigh && highVolume && lwtiValue > 50m && close > middle)
		{
			BuyMarket(Volume);
			_stopPrice = middle;
			_takePrice = close + (close - middle) * RiskReward;
			_lastTradeDay = candle.OpenTime.Date;
		}
		else if (close < channelLow && highVolume && lwtiValue < 50m && close < middle)
		{
			SellMarket(Volume);
			_stopPrice = middle;
			_takePrice = close - (middle - close) * RiskReward;
			_lastTradeDay = candle.OpenTime.Date;
		}
	}

	private decimal? ProcessLwti(ICandleMessage candle, IIndicatorValue atrValue)
	{
		_closes.Add(candle.ClosePrice);
		if (_closes.Count > LwtiLength + 1)
			_closes.RemoveAt(0);

		if (_closes.Count <= LwtiLength)
			return null;

		var change = candle.ClosePrice - _closes[0];
		var wma = _changeWma.Process(new DecimalIndicatorValue(_changeWma, change, candle.OpenTime) { IsFinal = true });

		if (!_changeWma.IsFormed || !atrValue.IsFormed)
			return null;

		var atr = atrValue.GetValue<decimal>();
		if (atr <= 0)
			return null;

		var raw = wma.GetValue<decimal>() / atr * 50m + 50m;
		var smooth = _lwtiSma.Process(new DecimalIndicatorValue(_lwtiSma, raw, candle.OpenTime) { IsFinal = true });

		return _lwtiSma.IsFormed ? smooth.GetValue<decimal>() : null;
	}

	private bool InSession(int hour)
	{
		return StartHour <= EndHour
			? hour >= StartHour && hour < EndHour
			: hour >= StartHour || hour < EndHour;
	}

	// Returns true when the position was closed on this candle.
	private bool ManagePosition(ICandleMessage candle)
	{
		if (Position > 0 && _stopPrice > 0)
		{
			if (candle.LowPrice <= _stopPrice || candle.HighPrice >= _takePrice)
			{
				SellMarket(Position);
				return true;
			}
		}
		else if (Position < 0 && _stopPrice > 0)
		{
			if (candle.HighPrice >= _stopPrice || candle.LowPrice <= _takePrice)
			{
				BuyMarket(Math.Abs(Position));
				return true;
			}
		}

		return false;
	}
}
