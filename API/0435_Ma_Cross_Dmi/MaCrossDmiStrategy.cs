namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// MA Cross + DMI Strategy
/// </summary>
public class MaCrossDmiStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleTypeParam;
	private readonly StrategyParam<int> _ma1Length;
	private readonly StrategyParam<int> _ma2Length;
	private readonly StrategyParam<bool> _ma1IsEma;
	private readonly StrategyParam<bool> _ma2IsEma;
	private readonly StrategyParam<int> _dmiLength;
	private readonly StrategyParam<int> _adxSmoothing;
	private readonly StrategyParam<int> _keyLevel;
	private readonly StrategyParam<bool> _showLong;
	private readonly StrategyParam<bool> _showShort;
	private readonly StrategyParam<bool> _useSL;
	private readonly StrategyParam<decimal> _slPercent;

	private IIndicator _ma1;
	private IIndicator _ma2;
	private DirectionalIndex _dmi;

	public MaCrossDmiStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		// Moving Average parameters
		_ma1IsEma = Param(nameof(Ma1IsEma), true)
			.SetDisplay("MA1 Type (EMA)", "Use EMA for MA1, otherwise SMA", "Moving Average");

		_ma1Length = Param(nameof(Ma1Length), 10)
			.SetDisplay("MA1 Length", "First moving average period", "Moving Average");

		_ma2IsEma = Param(nameof(Ma2IsEma), true)
			.SetDisplay("MA2 Type (EMA)", "Use EMA for MA2, otherwise SMA", "Moving Average");

		_ma2Length = Param(nameof(Ma2Length), 20)
			.SetDisplay("MA2 Length", "Second moving average period", "Moving Average");

		// DMI parameters
		_dmiLength = Param(nameof(DmiLength), 14)
			.SetDisplay("DI Length", "DMI period", "Directional Movement Index");

		_adxSmoothing = Param(nameof(AdxSmoothing), 13)
			.SetDisplay("ADX Smoothing", "ADX smoothing period", "Directional Movement Index");

		_keyLevel = Param(nameof(KeyLevel), 23)
			.SetDisplay("Key Level", "ADX key level", "Directional Movement Index");

		// Strategy parameters
		_showLong = Param(nameof(ShowLong), true)
			.SetDisplay("Long entries", "Enable long positions", "Strategy");

		_showShort = Param(nameof(ShowShort), false)
			.SetDisplay("Short entries", "Enable short positions", "Strategy");

		// Stop Loss parameters
		_useSL = Param(nameof(UseSL), false)
			.SetDisplay("Enable SL", "Enable Stop Loss", "Stop Loss");

		_slPercent = Param(nameof(SLPercent), 10m)
			.SetDisplay("SL Percent", "Stop loss percentage", "Stop Loss");
	}

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
	}

	public int Ma1Length
	{
		get => _ma1Length.Value;
		set => _ma1Length.Value = value;
	}

	public int Ma2Length
	{
		get => _ma2Length.Value;
		set => _ma2Length.Value = value;
	}

	public bool Ma1IsEma
	{
		get => _ma1IsEma.Value;
		set => _ma1IsEma.Value = value;
	}

	public bool Ma2IsEma
	{
		get => _ma2IsEma.Value;
		set => _ma2IsEma.Value = value;
	}

	public int DmiLength
	{
		get => _dmiLength.Value;
		set => _dmiLength.Value = value;
	}

	public int AdxSmoothing
	{
		get => _adxSmoothing.Value;
		set => _adxSmoothing.Value = value;
	}

	public int KeyLevel
	{
		get => _keyLevel.Value;
		set => _keyLevel.Value = value;
	}

	public bool ShowLong
	{
		get => _showLong.Value;
		set => _showLong.Value = value;
	}

	public bool ShowShort
	{
		get => _showShort.Value;
		set => _showShort.Value = value;
	}

	public bool UseSL
	{
		get => _useSL.Value;
		set => _useSL.Value = value;
	}

	public decimal SLPercent
	{
		get => _slPercent.Value;
		set => _slPercent.Value = value;
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> new[] { (Security, CandleType) };

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Initialize moving averages
		if (Ma1IsEma)
			_ma1 = new ExponentialMovingAverage { Length = Ma1Length };
		else
			_ma1 = new SimpleMovingAverage { Length = Ma1Length };

		if (Ma2IsEma)
			_ma2 = new ExponentialMovingAverage { Length = Ma2Length };
		else
			_ma2 = new SimpleMovingAverage { Length = Ma2Length };

		// Initialize DMI
		_dmi = new DirectionalIndex
		{
			Length = DmiLength
		};

		// Subscribe to candles using high-level API
		var subscription = SubscribeCandles(CandleType);
		subscription
			.BindEx(_ma1, _ma2, _dmi, OnProcess)
			.Start();

		// Setup chart
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ma1, System.Drawing.Color.Blue);
			DrawIndicator(area, _ma2, System.Drawing.Color.Orange);
			DrawOwnTrades(area);
		}

		// Enable protection if Stop Loss is enabled
		if (UseSL)
		{
			StartProtection(new(), new Unit(SLPercent, UnitTypes.Percent));
		}
	}

	private void OnProcess(ICandleMessage candle, IIndicatorValue ma1Value, IIndicatorValue ma2Value, IIndicatorValue dmiValue)
	{
		// Process only finished candles
		if (candle.State != CandleStates.Finished)
			return;

		// Wait for indicators to form
		if (!_ma1.IsFormed || !_ma2.IsFormed || !_dmi.IsFormed)
			return;

		// Get MA values
		var ma1Price = ma1Value.ToDecimal();
		var ma2Price = ma2Value.ToDecimal();

		// Get previous MA values for crossover detection
		var prevMa1 = _ma1.GetValue(1);
		var prevMa2 = _ma2.GetValue(1);

		// Get DMI values
		var dmiData = (DirectionalIndexValue)dmiValue;
		var diPlus = dmiData.Plus ?? 0m;
		var diMinus = dmiData.Minus ?? 0m;

		// DMI conditions (commented out in original, but keeping for reference)
		// var longCond = diPlus < diMinus && prevDiPlus < prevDiMinus;
		// var shortCond = diPlus > diMinus && prevDiPlus > prevDiMinus;

		// MA crossover conditions
		var longEntry = ma1Price > ma2Price && prevMa1 <= prevMa2;
		var shortEntry = ma1Price < ma2Price && prevMa1 >= prevMa2;

		// Execute trades based on settings
		if (ShowLong && !ShowShort)
		{
			if (longEntry && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (shortEntry && Position > 0)
			{
				ClosePosition();
			}
		}
		else if (!ShowLong && ShowShort)
		{
			if (shortEntry && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
			else if (longEntry && Position < 0)
			{
				ClosePosition();
			}
		}
		else if (ShowLong && ShowShort)
		{
			if (longEntry && Position <= 0)
			{
				BuyMarket(Volume + Math.Abs(Position));
			}
			else if (shortEntry && Position >= 0)
			{
				SellMarket(Volume + Math.Abs(Position));
			}
		}
	}
}