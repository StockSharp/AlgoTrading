using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Volatility capture strategy using RSI and Bollinger bands.
/// </summary>
public class VolatilityCaptureRsiBollingerStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _multiplier;
	private readonly StrategyParam<bool> _useRsi;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<int> _rsiSmaPeriod;
	private readonly StrategyParam<decimal> _boughtLevel;
	private readonly StrategyParam<decimal> _soldLevel;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<DataType> _candleType;

	private BollingerBands _bollinger;
	private RelativeStrengthIndex _rsi;
	private SimpleMovingAverage _rsiSma;

	private decimal _presentBand;
	private decimal _prevBand;
	private decimal? _prevPriceSource;

	/// <summary>
	/// Bollinger calculation length.
	/// </summary>
	public int BollingerLength { get => _bollingerLength.Value; set => _bollingerLength.Value = value; }

	/// <summary>
	/// Deviation multiplier for Bollinger bands.
	/// </summary>
	public decimal Multiplier { get => _multiplier.Value; set => _multiplier.Value = value; }

	/// <summary>
	/// Enable RSI filter.
	/// </summary>
	public bool UseRsi { get => _useRsi.Value; set => _useRsi.Value = value; }

	/// <summary>
	/// RSI period.
	/// </summary>
	public int RsiPeriod { get => _rsiPeriod.Value; set => _rsiPeriod.Value = value; }

	/// <summary>
	/// SMA period applied to RSI.
	/// </summary>
	public int RsiSmaPeriod { get => _rsiSmaPeriod.Value; set => _rsiSmaPeriod.Value = value; }

	/// <summary>
	/// Upper RSI threshold.
	/// </summary>
	public decimal BoughtRangeLevel { get => _boughtLevel.Value; set => _boughtLevel.Value = value; }

	/// <summary>
	/// Lower RSI threshold.
	/// </summary>
	public decimal SoldRangeLevel { get => _soldLevel.Value; set => _soldLevel.Value = value; }

	/// <summary>
	/// Trade direction.
	/// </summary>
	public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="VolatilityCaptureRsiBollingerStrategy"/>.
	/// </summary>
	public VolatilityCaptureRsiBollingerStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 50)
		.SetRange(10, 100)
		.SetDisplay("Bollinger Length", "Period for Bollinger calculation", "Indicators")
		.SetCanOptimize(true);

		_multiplier = Param(nameof(Multiplier), 2.7183m)
		.SetRange(1m, 5m)
		.SetDisplay("Multiplier", "Deviation multiplier for Bollinger bands", "Indicators")
		.SetCanOptimize(true);

		_useRsi = Param(nameof(UseRsi), true)
		.SetDisplay("Use RSI", "Enable RSI filter", "Strategy");

		_rsiPeriod = Param(nameof(RsiPeriod), 10)
		.SetRange(5, 30)
		.SetDisplay("RSI Period", "RSI calculation period", "Indicators")
		.SetCanOptimize(true);

		_rsiSmaPeriod = Param(nameof(RsiSmaPeriod), 5)
		.SetRange(2, 20)
		.SetDisplay("RSI SMA Period", "SMA applied to RSI", "Indicators")
		.SetCanOptimize(true);

		_boughtLevel = Param(nameof(BoughtRangeLevel), 55m)
		.SetRange(0m, 100m)
		.SetDisplay("Bought Range Level", "Upper threshold for RSI SMA", "Strategy")
		.SetCanOptimize(true);

		_soldLevel = Param(nameof(SoldRangeLevel), 50m)
		.SetRange(0m, 100m)
		.SetDisplay("Sold Range Level", "Lower threshold for RSI SMA", "Strategy")
		.SetCanOptimize(true);

		_direction = Param(nameof(Direction), (Sides?)null)
			.SetDisplay("Trade Direction", "Choose long, short or both", "General");

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
		_presentBand = 0m;
		_prevBand = 0m;
		_prevPriceSource = null;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_bollinger = new BollingerBands
		{
			Length = BollingerLength,
			Width = Multiplier
		};

		_rsi = new RelativeStrengthIndex
		{
			Length = RsiPeriod
		};

		_rsiSma = new SimpleMovingAverage
		{
			Length = RsiSmaPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_bollinger, _rsi, _rsiSma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _bollinger);
			DrawOwnTrades(area);
		}

		StartProtection();
	}

	private void ProcessCandle(ICandleMessage candle, decimal middle, decimal upper, decimal lower, decimal rsi, decimal rsiSma)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_bollinger.IsFormed || !_rsiSma.IsFormed)
			return;

		var priceSource = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m;

		var allowLong = Direction != Sides.Sell;
		var allowShort = Direction != Sides.Buy;

		var longSignal1 = _prevPriceSource is decimal prevPrice1 && prevPrice1 <= _prevBand && priceSource > _prevBand;
		var shortSignal1 = _prevPriceSource is decimal prevPrice2 && prevPrice2 >= _prevBand && priceSource < _prevBand;

		var longSignal2 = rsiSma > BoughtRangeLevel;
		var shortSignal2 = rsiSma < SoldRangeLevel;

		if (candle.ClosePrice > _prevBand)
		_presentBand = Math.Max(_prevBand, lower);
		else if (candle.ClosePrice < _prevBand)
		_presentBand = Math.Min(_prevBand, upper);
		else
		_presentBand = 0m;

		if (allowLong && longSignal1 && (!UseRsi || longSignal2) && Position <= 0)
		{
		_presentBand = lower;
		var volume = Volume + Math.Abs(Position);
		BuyMarket(volume);
		}
		else if (allowShort && shortSignal1 && (!UseRsi || shortSignal2) && Position >= 0)
		{
		_presentBand = upper;
		var volume = Volume + Math.Abs(Position);
		SellMarket(volume);
		}

		if (Position > 0 && priceSource < _presentBand)
		SellMarket(Math.Abs(Position));
		else if (Position < 0 && priceSource > _presentBand)
		BuyMarket(Math.Abs(Position));

		_prevPriceSource = priceSource;
		_prevBand = _presentBand;
	}
}
