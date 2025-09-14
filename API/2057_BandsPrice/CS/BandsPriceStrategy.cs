using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Mean reversion strategy based on the Bands Price indicator.
/// Opens a long position when the indicator leaves the upper zone.
/// Opens a short position when the indicator leaves the lower zone.
/// Closes positions when the indicator crosses the zero line.
/// </summary>
public class BandsPriceStrategy : Strategy
{
	private readonly StrategyParam<bool> _buyOpen;
	private readonly StrategyParam<bool> _sellOpen;
	private readonly StrategyParam<bool> _buyClose;
	private readonly StrategyParam<bool> _sellClose;

	private readonly StrategyParam<int> _bandsPeriod;
	private readonly StrategyParam<decimal> _bandsDeviation;
	private readonly StrategyParam<int> _smooth;
	private readonly StrategyParam<int> _upLevel;
	private readonly StrategyParam<int> _dnLevel;
	private readonly StrategyParam<DataType> _candleType;

	private int _prevColor = -1;
	private int _prevPrevColor = -1;

	/// <summary>
	/// Allow opening of long positions.
	/// </summary>
	public bool BuyOpen
	{
		get => _buyOpen.Value;
		set => _buyOpen.Value = value;
	}

	/// <summary>
	/// Allow opening of short positions.
	/// </summary>
	public bool SellOpen
	{
		get => _sellOpen.Value;
		set => _sellOpen.Value = value;
	}

	/// <summary>
	/// Allow closing of long positions.
	/// </summary>
	public bool BuyClose
	{
		get => _buyClose.Value;
		set => _buyClose.Value = value;
	}

	/// <summary>
	/// Allow closing of short positions.
	/// </summary>
	public bool SellClose
	{
		get => _sellClose.Value;
		set => _sellClose.Value = value;
	}

	/// <summary>
	/// Bollinger Bands period.
	/// </summary>
	public int BandsPeriod
	{
		get => _bandsPeriod.Value;
		set => _bandsPeriod.Value = value;
	}

	/// <summary>
	/// Bollinger Bands deviation.
	/// </summary>
	public decimal BandsDeviation
	{
		get => _bandsDeviation.Value;
		set => _bandsDeviation.Value = value;
	}

	/// <summary>
	/// Smoothing length for indicator values.
	/// </summary>
	public int Smooth
	{
		get => _smooth.Value;
		set => _smooth.Value = value;
	}

	/// <summary>
	/// Upper threshold level.
	/// </summary>
	public int UpLevel
	{
		get => _upLevel.Value;
		set => _upLevel.Value = value;
	}

	/// <summary>
	/// Lower threshold level.
	/// </summary>
	public int DnLevel
	{
		get => _dnLevel.Value;
		set => _dnLevel.Value = value;
	}

	/// <summary>
	/// Candle type for processing.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BandsPriceStrategy"/>.
	/// </summary>
	public BandsPriceStrategy()
	{
		_buyOpen = Param(nameof(BuyOpen), true).SetDisplay("Buy Open", "Enable long entries", "Trading");

		_sellOpen = Param(nameof(SellOpen), true).SetDisplay("Sell Open", "Enable short entries", "Trading");

		_buyClose = Param(nameof(BuyClose), true).SetDisplay("Buy Close", "Enable exit for long positions", "Trading");

		_sellClose =
			Param(nameof(SellClose), true).SetDisplay("Sell Close", "Enable exit for short positions", "Trading");

		_bandsPeriod = Param(nameof(BandsPeriod), 100)
						   .SetGreaterThanZero()
						   .SetDisplay("Bands Period", "Bollinger Bands period", "Indicator")
						   .SetCanOptimize(true)
						   .SetOptimize(50, 150, 10);

		_bandsDeviation = Param(nameof(BandsDeviation), 2m)
							  .SetGreaterThanZero()
							  .SetDisplay("Bands Deviation", "Width of Bollinger Bands", "Indicator")
							  .SetCanOptimize(true)
							  .SetOptimize(1m, 3m, 0.5m);

		_smooth = Param(nameof(Smooth), 5)
					  .SetGreaterThanZero()
					  .SetDisplay("Smoothing", "Length of smoothing SMA", "Indicator")
					  .SetCanOptimize(true)
					  .SetOptimize(3, 15, 1);

		_upLevel = Param(nameof(UpLevel), 25)
					   .SetDisplay("Upper Level", "Threshold for overbought zone", "Indicator")
					   .SetCanOptimize(true)
					   .SetOptimize(20, 40, 5);

		_dnLevel = Param(nameof(DnLevel), -25)
					   .SetDisplay("Lower Level", "Threshold for oversold zone", "Indicator")
					   .SetCanOptimize(true)
					   .SetOptimize(-40, -20, 5);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
						  .SetDisplay("Candle Type", "Timeframe for analysis", "General");
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
		_prevColor = -1;
		_prevPrevColor = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var bands = new BollingerBands { Length = BandsPeriod, Width = BandsDeviation };

		var smooth = new SMA { Length = Smooth };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(bands,
				  (candle, middle, upper, lower) =>
				  {
					  if (candle.State != CandleStates.Finished)
						  return;

					  if (!IsFormedAndOnlineAndAllowTrading())
						  return;

					  var width = upper - lower;
					  if (width == 0)
						  return;

					  var res = 100m * (candle.ClosePrice - lower) / width - 50m;

					  var smoothed = smooth.Process(res);
					  if (!smoothed.IsFinal)
						  return;

					  var jres = smoothed.GetValue<decimal>();

					  var color = 2;
					  if (jres > UpLevel)
						  color = 4;
					  else if (jres > 0)
						  color = 3;
					  if (jres < DnLevel)
						  color = 0;
					  else if (jres < 0)
						  color = 1;

					  if (_prevPrevColor != -1 && _prevColor != -1)
					  {
						  if (BuyOpen && _prevPrevColor == 4 && _prevColor < 4 && Position <= 0)
							  BuyMarket(Volume + Math.Abs(Position));

						  if (SellOpen && _prevPrevColor == 0 && _prevColor > 0 && Position >= 0)
							  SellMarket(Volume + Math.Abs(Position));

						  if (SellClose && _prevColor > 1 && Position < 0)
							  BuyMarket(Math.Abs(Position));

						  if (BuyClose && _prevColor < 2 && Position > 0)
							  SellMarket(Math.Abs(Position));
					  }

					  _prevPrevColor = _prevColor;
					  _prevColor = color;
				  })
			.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, bands);
		}
	}
}
