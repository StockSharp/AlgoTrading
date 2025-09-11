using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Gold breakout strategy using Donchian Channel and LWTI filter.
/// Trades only once per day within a time window and uses a 4:1 risk/reward.
/// </summary>
public class GoldBreakoutRr4Strategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _maVolumeLength;
	private readonly StrategyParam<int> _lwtiLength;
	private readonly StrategyParam<int> _lwtiSmooth;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<decimal> _riskReward;

	private SimpleMovingAverage _lwtiSma = null!;
	private DateTime _currentDay;
	private bool _tradeTaken;
	private decimal _prevUpper;
	private decimal _prevLower;
	private decimal? _prevWma;
	private decimal _longStop;
	private decimal _longTake;
	private decimal _shortStop;
	private decimal _shortTake;

	public GoldBreakoutRr4Strategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to process", "General");

		_donchianLength = Param(nameof(DonchianLength), 96)
			.SetDisplay("Donchian Length", "Period for Donchian Channel", "Indicators");

		_maVolumeLength = Param(nameof(MaVolumeLength), 30)
			.SetDisplay("Volume MA Length", "Period for volume moving average", "Indicators");

		_lwtiLength = Param(nameof(LwtiLength), 25)
			.SetDisplay("LWTI Length", "Length for weighted moving average", "Indicators");

		_lwtiSmooth = Param(nameof(LwtiSmooth), 5)
			.SetDisplay("LWTI Smooth", "Smoothing period for LWTI", "Indicators");

		_startHour = Param(nameof(StartHour), 20)
			.SetDisplay("Start Hour", "Session start hour", "Time");

		_endHour = Param(nameof(EndHour), 8)
			.SetDisplay("End Hour", "Session end hour", "Time");

		_riskReward = Param(nameof(RiskReward), 4m)
			.SetDisplay("Risk Reward", "Risk reward ratio", "Trading");
	}

	/// <summary>
	/// Candle type used by the strategy.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Donchian Channel length.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// Volume moving average length.
	/// </summary>
	public int MaVolumeLength
	{
		get => _maVolumeLength.Value;
		set => _maVolumeLength.Value = value;
	}

	/// <summary>
	/// LWTI WMA length.
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
	/// Session start hour.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// Session end hour.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Risk reward ratio.
	/// </summary>
	public decimal RiskReward
	{
		get => _riskReward.Value;
		set => _riskReward.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_prevUpper = _prevLower = 0;
		_prevWma = null;
		_tradeTaken = false;
		_currentDay = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lwtiSma = new SimpleMovingAverage { Length = LwtiSmooth };

		var donchian = new DonchianChannels { Length = DonchianLength };
		var volumeSma = new SimpleMovingAverage { Length = MaVolumeLength };
		var wma = new WeightedMovingAverage { Length = LwtiLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(donchian, volumeSma, wma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, donchian);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal middleBand, decimal upperBand, decimal lowerBand, decimal volumeMa, decimal wmaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var date = candle.OpenTime.Date;
		if (date != _currentDay)
		{
			_currentDay = date;
			_tradeTaken = false;
		}

		var start = date + TimeSpan.FromHours(StartHour);
		var end = (StartHour > EndHour ? start.AddDays(1) : start) + TimeSpan.FromHours(EndHour);
		var inSession = candle.OpenTime >= start && candle.OpenTime < end;

		var volumeCondition = candle.TotalVolume > volumeMa;

		bool lwtiLong = false;
		bool lwtiShort = false;

		if (_prevWma != null)
		{
			var lwti = wmaValue - _prevWma.Value;
			var smoothed = _lwtiSma.Process(lwti, candle.OpenTime, true).ToDecimal();

			if (_lwtiSma.IsFormed)
			{
				lwtiLong = lwti > smoothed;
				lwtiShort = lwti < smoothed;
			}
		}
		_prevWma = wmaValue;

		var breaksUpper = candle.ClosePrice > _prevUpper && upperBand > _prevUpper;
		var breaksLower = candle.ClosePrice < _prevLower && lowerBand < _prevLower;

		if (Position > 0)
		{
			if (candle.LowPrice <= _longStop || candle.HighPrice >= _longTake)
				SellMarket(Position);
		}
		else if (Position < 0)
		{
			if (candle.HighPrice >= _shortStop || candle.LowPrice <= _shortTake)
				BuyMarket(-Position);
		}

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevUpper = upperBand;
			_prevLower = lowerBand;
			return;
		}

		var longCondition = !_tradeTaken && inSession && breaksUpper && volumeCondition && lwtiLong;
		var shortCondition = !_tradeTaken && inSession && breaksLower && volumeCondition && lwtiShort;

		if (longCondition && Position <= 0)
		{
			_longStop = candle.LowPrice;
			var risk = candle.ClosePrice - _longStop;
			_longTake = candle.ClosePrice + risk * RiskReward;
			BuyMarket(Volume + Math.Abs(Position));
			_tradeTaken = true;
		}
		else if (shortCondition && Position >= 0)
		{
			_shortStop = candle.HighPrice;
			var risk = _shortStop - candle.ClosePrice;
			_shortTake = candle.ClosePrice - risk * RiskReward;
			SellMarket(Volume + Math.Abs(Position));
			_tradeTaken = true;
		}

		_prevUpper = upperBand;
		_prevLower = lowerBand;
	}
}
