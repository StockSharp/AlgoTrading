using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Dual MACD strategy using two MACD indicators with different periods.
/// </summary>
public class DualMacdStrategy : Strategy
{
	private readonly StrategyParam<int> _macd1FastLength;
	private readonly StrategyParam<int> _macd1SlowLength;
	private readonly StrategyParam<int> _macd1SignalLength;
	private readonly StrategyParam<bool> _macd1UseSmaPrice;
	private readonly StrategyParam<bool> _macd1UseSmaSignal;

	private readonly StrategyParam<int> _macd2FastLength;
	private readonly StrategyParam<int> _macd2SlowLength;
	private readonly StrategyParam<int> _macd2SignalLength;
	private readonly StrategyParam<bool> _macd2UseSmaPrice;
	private readonly StrategyParam<bool> _macd2UseSmaSignal;

	private readonly StrategyParam<decimal> _stopLossPercent;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal? _prevHist1;
	private decimal? _prevHist2;

	/// <summary>
	/// Fast length for MACD1.
	/// </summary>
	public int Macd1FastLength
	{
		get => _macd1FastLength.Value;
		set => _macd1FastLength.Value = value;
	}

	/// <summary>
	/// Slow length for MACD1.
	/// </summary>
	public int Macd1SlowLength
	{
		get => _macd1SlowLength.Value;
		set => _macd1SlowLength.Value = value;
	}

	/// <summary>
	/// Signal length for MACD1.
	/// </summary>
	public int Macd1SignalLength
	{
		get => _macd1SignalLength.Value;
		set => _macd1SignalLength.Value = value;
	}

	/// <summary>
	/// Use SMA for MACD1 price averages.
	/// </summary>
	public bool Macd1UseSmaPrice
	{
		get => _macd1UseSmaPrice.Value;
		set => _macd1UseSmaPrice.Value = value;
	}

	/// <summary>
	/// Use SMA for MACD1 signal average.
	/// </summary>
	public bool Macd1UseSmaSignal
	{
		get => _macd1UseSmaSignal.Value;
		set => _macd1UseSmaSignal.Value = value;
	}

	/// <summary>
	/// Fast length for MACD2.
	/// </summary>
	public int Macd2FastLength
	{
		get => _macd2FastLength.Value;
		set => _macd2FastLength.Value = value;
	}

	/// <summary>
	/// Slow length for MACD2.
	/// </summary>
	public int Macd2SlowLength
	{
		get => _macd2SlowLength.Value;
		set => _macd2SlowLength.Value = value;
	}

	/// <summary>
	/// Signal length for MACD2.
	/// </summary>
	public int Macd2SignalLength
	{
		get => _macd2SignalLength.Value;
		set => _macd2SignalLength.Value = value;
	}

	/// <summary>
	/// Use SMA for MACD2 price averages.
	/// </summary>
	public bool Macd2UseSmaPrice
	{
		get => _macd2UseSmaPrice.Value;
		set => _macd2UseSmaPrice.Value = value;
	}

	/// <summary>
	/// Use SMA for MACD2 signal average.
	/// </summary>
	public bool Macd2UseSmaSignal
	{
		get => _macd2UseSmaSignal.Value;
		set => _macd2UseSmaSignal.Value = value;
	}

	/// <summary>
	/// Stop loss percentage.
	/// </summary>
	public decimal StopLossPercent
	{
		get => _stopLossPercent.Value;
		set => _stopLossPercent.Value = value;
	}

	/// <summary>
	/// Take profit percentage.
	/// </summary>
	public decimal TakeProfitPercent
	{
		get => _takeProfitPercent.Value;
		set => _takeProfitPercent.Value = value;
	}

	/// <summary>
	/// Candle type to use.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DualMacdStrategy"/>.
	/// </summary>
	public DualMacdStrategy()
	{
		_macd1FastLength = Param(nameof(Macd1FastLength), 34)
		.SetGreaterThanZero()
		.SetDisplay("MACD1 Fast Length", "Fast length for MACD1", "MACD1")
		.SetCanOptimize(true);

		_macd1SlowLength = Param(nameof(Macd1SlowLength), 144)
		.SetGreaterThanZero()
		.SetDisplay("MACD1 Slow Length", "Slow length for MACD1", "MACD1")
		.SetCanOptimize(true);

		_macd1SignalLength = Param(nameof(Macd1SignalLength), 9)
		.SetGreaterThanZero()
		.SetDisplay("MACD1 Signal Length", "Signal length for MACD1", "MACD1")
		.SetCanOptimize(true);

		_macd1UseSmaPrice = Param(nameof(Macd1UseSmaPrice), false)
		.SetDisplay("MACD1 Use SMA Price", "Use SMA for MACD1 price averages", "MACD1");

		_macd1UseSmaSignal = Param(nameof(Macd1UseSmaSignal), false)
		.SetDisplay("MACD1 Use SMA Signal", "Use SMA for MACD1 signal average", "MACD1");

		_macd2FastLength = Param(nameof(Macd2FastLength), 100)
		.SetGreaterThanZero()
		.SetDisplay("MACD2 Fast Length", "Fast length for MACD2", "MACD2")
		.SetCanOptimize(true);

		_macd2SlowLength = Param(nameof(Macd2SlowLength), 200)
		.SetGreaterThanZero()
		.SetDisplay("MACD2 Slow Length", "Slow length for MACD2", "MACD2")
		.SetCanOptimize(true);

		_macd2SignalLength = Param(nameof(Macd2SignalLength), 50)
		.SetGreaterThanZero()
		.SetDisplay("MACD2 Signal Length", "Signal length for MACD2", "MACD2")
		.SetCanOptimize(true);

		_macd2UseSmaPrice = Param(nameof(Macd2UseSmaPrice), false)
		.SetDisplay("MACD2 Use SMA Price", "Use SMA for MACD2 price averages", "MACD2");

		_macd2UseSmaSignal = Param(nameof(Macd2UseSmaSignal), false)
		.SetDisplay("MACD2 Use SMA Signal", "Use SMA for MACD2 signal average", "MACD2");

		_stopLossPercent = Param(nameof(StopLossPercent), 1.0m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss %", "Stop loss percentage", "Risk Management")
		.SetCanOptimize(true);

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.5m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit %", "Take profit percentage", "Risk Management")
		.SetCanOptimize(true);

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(15).TimeFrame())
		.SetDisplay("Candle Type", "Type of candles", "General");
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

		_prevHist1 = default;
		_prevHist2 = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var macd1 = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Macd1FastLength },
				LongMa = { Length = Macd1SlowLength }
			},
			SignalMa = { Length = Macd1SignalLength }
		};

		var macd2 = new MovingAverageConvergenceDivergenceSignal
		{
			Macd =
			{
				ShortMa = { Length = Macd2FastLength },
				LongMa = { Length = Macd2SlowLength }
			},
			SignalMa = { Length = Macd2SignalLength }
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
		.BindEx(macd1, macd2, ProcessCandle)
		.Start();

		StartProtection(
		takeProfit: new Unit(TakeProfitPercent, UnitTypes.Percent),
		stopLoss: new Unit(StopLossPercent, UnitTypes.Percent),
		useMarketOrders: true);

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, macd1);
			DrawIndicator(area, macd2);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue macd1Value, IIndicatorValue macd2Value)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var macd1Typed = (MovingAverageConvergenceDivergenceSignalValue)macd1Value;
		var macd2Typed = (MovingAverageConvergenceDivergenceSignalValue)macd2Value;

		if (macd1Typed.Macd is not decimal macd1 ||
		macd1Typed.Signal is not decimal signal1)
		return;

		if (macd2Typed.Macd is not decimal macd2 ||
		macd2Typed.Signal is not decimal signal2)
		return;

		var hist1 = macd1 - signal1;
		var hist2 = macd2 - signal2;

		if (!IsFormedAndOnlineAndAllowTrading())
		{
			_prevHist1 = hist1;
			_prevHist2 = hist2;
			return;
		}

		var longReady = _prevHist2 is decimal prevH2 && prevH2 <= 0 && hist2 > 0 && hist1 > 0 && hist2 > prevH2 && Position <= 0;
		var shortReady = _prevHist2 is decimal prevH2s && prevH2s >= 0 && hist2 < 0 && hist1 < 0 && hist2 < prevH2s && Position >= 0;

		var longExit = _prevHist1 is decimal prevH1 && hist1 < 0 && hist1 < prevH1 && Position > 0;
		var shortExit = _prevHist1 is decimal prevH1s && hist1 > 0 && hist1 > prevH1s && Position < 0;

		if (longReady)
		{
			var volume = Volume + Math.Abs(Position);
			BuyMarket(volume);
		}
		else if (shortReady)
		{
			var volume = Volume + Math.Abs(Position);
			SellMarket(volume);
		}
		else if (longExit)
		{
			SellMarket(Math.Abs(Position));
		}
		else if (shortExit)
		{
			BuyMarket(Math.Abs(Position));
		}

		_prevHist1 = hist1;
		_prevHist2 = hist2;
	}
}
