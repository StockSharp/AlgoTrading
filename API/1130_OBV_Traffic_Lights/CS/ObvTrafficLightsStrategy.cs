using System;
using System.Collections.Generic;
using System.Drawing;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// OBV Traffic Lights strategy.
/// Uses Heikin Ashi based OBV with three EMA "traffic lights".
/// Long when OBV and fast EMA are above slow EMA.
/// Short when OBV and fast EMA are below slow EMA.
/// </summary>
public class ObvTrafficLightsStrategy : Strategy
{
	private readonly StrategyParam<int> _fastLength;
	private readonly StrategyParam<int> _mediumLength;
	private readonly StrategyParam<int> _slowLength;
	private readonly StrategyParam<int> _donchianLength;
	private readonly StrategyParam<int> _smoothing;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _fastEma;
	private ExponentialMovingAverage _mediumEma;
	private ExponentialMovingAverage _slowEma;
	private Highest _highest;
	private Lowest _lowest;
	private ExponentialMovingAverage _haOpenEma;
	private ExponentialMovingAverage _haCloseEma;

	private decimal _prevHaOpen;
	private decimal _prevHaClose;
	private decimal _obv;

	/// <summary>
	/// Fast EMA period.
	/// </summary>
	public int FastLength
	{
		get => _fastLength.Value;
		set => _fastLength.Value = value;
	}

	/// <summary>
	/// Medium EMA period.
	/// </summary>
	public int MediumLength
	{
		get => _mediumLength.Value;
		set => _mediumLength.Value = value;
	}

	/// <summary>
	/// Slow EMA period.
	/// </summary>
	public int SlowLength
	{
		get => _slowLength.Value;
		set => _slowLength.Value = value;
	}

	/// <summary>
	/// Donchian baseline period.
	/// </summary>
	public int DonchianLength
	{
		get => _donchianLength.Value;
		set => _donchianLength.Value = value;
	}

	/// <summary>
	/// EMA smoothing for Heikin Ashi values.
	/// </summary>
	public int Smoothing
	{
		get => _smoothing.Value;
		set => _smoothing.Value = value;
	}

	/// <summary>
	/// Candle type for strategy calculation.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initialize <see cref="ObvTrafficLightsStrategy"/>.
	/// </summary>
	public ObvTrafficLightsStrategy()
	{
		_fastLength = Param(nameof(FastLength), 5)
			.SetGreaterThanZero()
			.SetDisplay("Fast EMA", "Fast EMA length", "Indicators");

		_mediumLength = Param(nameof(MediumLength), 9)
			.SetGreaterThanZero()
			.SetDisplay("Medium EMA", "Medium EMA length", "Indicators");

		_slowLength = Param(nameof(SlowLength), 14)
			.SetGreaterThanZero()
			.SetDisplay("Slow EMA", "Slow EMA length", "Indicators");

		_donchianLength = Param(nameof(DonchianLength), 26)
			.SetGreaterThanZero()
			.SetDisplay("Donchian Length", "Donchian baseline period", "Indicators");

		_smoothing = Param(nameof(Smoothing), 1)
			.SetGreaterThanZero()
			.SetDisplay("Smoothing", "EMA smoothing for Heikin Ashi", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Candle type for calculation", "General");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		_prevHaOpen = default;
		_prevHaClose = default;
		_obv = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_fastEma = new ExponentialMovingAverage { Length = FastLength };
		_mediumEma = new ExponentialMovingAverage { Length = MediumLength };
		_slowEma = new ExponentialMovingAverage { Length = SlowLength };
		_highest = new Highest { Length = DonchianLength };
		_lowest = new Lowest { Length = DonchianLength };
		_haOpenEma = new ExponentialMovingAverage { Length = Smoothing };
		_haCloseEma = new ExponentialMovingAverage { Length = Smoothing };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _fastEma, Color.Green);
			DrawIndicator(area, _mediumEma, Color.Yellow);
			DrawIndicator(area, _slowEma, Color.Red);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Calculate Heikin Ashi values
		decimal haOpenRaw;
		decimal haCloseRaw;

		if (_prevHaOpen == 0m)
		{
			haOpenRaw = (candle.OpenPrice + candle.ClosePrice) / 2m;
			haCloseRaw = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}
		else
		{
			haOpenRaw = (_prevHaOpen + _prevHaClose) / 2m;
			haCloseRaw = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		}

		_prevHaOpen = haOpenRaw;
		_prevHaClose = haCloseRaw;

		var haOpen = _haOpenEma.Process(haOpenRaw).ToDecimal();
		var haClose = _haCloseEma.Process(haCloseRaw).ToDecimal();

		// OBV calculation based on Heikin Ashi close vs open
		var vol = haClose > haOpen ? candle.TotalVolume : haClose < haOpen ? -candle.TotalVolume : 0m;
		_obv += vol;

		var fastValue = _fastEma.Process(_obv).ToDecimal();
		var mediumValue = _mediumEma.Process(_obv).ToDecimal();
		var slowValue = _slowEma.Process(_obv).ToDecimal();
		var highestValue = _highest.Process(_obv).ToDecimal();
		var lowestValue = _lowest.Process(_obv).ToDecimal();
		var baseline = (highestValue + lowestValue) / 2m;

		LogInfo($"OBV: {_obv}, Fast: {fastValue}, Medium: {mediumValue}, Slow: {slowValue}, Baseline: {baseline}");

		if (!_fastEma.IsFormed || !_slowEma.IsFormed)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var goLong = _obv > slowValue && fastValue > slowValue;
		var goShort = _obv < slowValue && fastValue < slowValue;

		if (goLong && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
			LogInfo("Enter long");
		}
		else if (goShort && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
			LogInfo("Enter short");
		}
		else if (!goLong && !goShort)
		{
			if (Position > 0)
			{
				SellMarket(Position);
				LogInfo("Exit long");
			}
			else if (Position < 0)
			{
				BuyMarket(-Position);
				LogInfo("Exit short");
			}
		}
	}
}
