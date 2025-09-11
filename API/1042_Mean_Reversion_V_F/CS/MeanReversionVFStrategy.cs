using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

public class MeanReversionVFStrategy : Strategy
{
	public enum MaType
	{
		Wma,
		Sma,
		Rma,
		Ema,
		Hma
	}

	private readonly StrategyParam<MaType> _maType;
	private readonly StrategyParam<int> _maLength;
	private readonly StrategyParam<decimal> _deviation1;
	private readonly StrategyParam<decimal> _deviation2;
	private readonly StrategyParam<decimal> _deviation3;
	private readonly StrategyParam<decimal> _deviation4;
	private readonly StrategyParam<decimal> _deviation5;
	private readonly StrategyParam<decimal> _unitsLevel1;
	private readonly StrategyParam<decimal> _unitsLevel2;
	private readonly StrategyParam<decimal> _unitsLevel3;
	private readonly StrategyParam<decimal> _unitsLevel4;
	private readonly StrategyParam<decimal> _unitsLevel5;
	private readonly StrategyParam<bool> _useUnits;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<bool> _trailingEnabled;
	private readonly StrategyParam<decimal> _trailingDistancePercent;
	private readonly StrategyParam<DataType> _candleType;

	private int _entries;
	private decimal _s2;
	private decimal _s3;
	private decimal _s4;
	private decimal _s5;
	private bool _trailingActive;
	private decimal _trailPrice;
	private decimal _extremePrice;

	public MaType MovingAverageType { get => _maType.Value; set => _maType.Value = value; }
	public int MaLength { get => _maLength.Value; set => _maLength.Value = value; }
	public decimal Deviation1 { get => _deviation1.Value; set => _deviation1.Value = value; }
	public decimal Deviation2 { get => _deviation2.Value; set => _deviation2.Value = value; }
	public decimal Deviation3 { get => _deviation3.Value; set => _deviation3.Value = value; }
	public decimal Deviation4 { get => _deviation4.Value; set => _deviation4.Value = value; }
	public decimal Deviation5 { get => _deviation5.Value; set => _deviation5.Value = value; }
	public decimal UnitsLevel1 { get => _unitsLevel1.Value; set => _unitsLevel1.Value = value; }
	public decimal UnitsLevel2 { get => _unitsLevel2.Value; set => _unitsLevel2.Value = value; }
	public decimal UnitsLevel3 { get => _unitsLevel3.Value; set => _unitsLevel3.Value = value; }
	public decimal UnitsLevel4 { get => _unitsLevel4.Value; set => _unitsLevel4.Value = value; }
	public decimal UnitsLevel5 { get => _unitsLevel5.Value; set => _unitsLevel5.Value = value; }
	public bool UseUnits { get => _useUnits.Value; set => _useUnits.Value = value; }
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }
	public bool TrailingEnabled { get => _trailingEnabled.Value; set => _trailingEnabled.Value = value; }
	public decimal TrailingDistancePercent { get => _trailingDistancePercent.Value; set => _trailingDistancePercent.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public MeanReversionVFStrategy()
	{
		_maType = Param(nameof(MovingAverageType), MaType.Wma)
			.SetDisplay("MA Type", "Type of moving average", "General");

		_maLength = Param(nameof(MaLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("MA Length", "Moving average length", "General");

		_deviation1 = Param(nameof(Deviation1), 1.3m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation 1 %", "First deviation percent", "Levels");
		_deviation2 = Param(nameof(Deviation2), 7.5m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation 2 %", "Second deviation percent", "Levels");
		_deviation3 = Param(nameof(Deviation3), 13.3m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation 3 %", "Third deviation percent", "Levels");
		_deviation4 = Param(nameof(Deviation4), 21.1m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation 4 %", "Fourth deviation percent", "Levels");
		_deviation5 = Param(nameof(Deviation5), 33.7m)
			.SetGreaterThanZero()
			.SetDisplay("Deviation 5 %", "Fifth deviation percent", "Levels");

		_unitsLevel1 = Param(nameof(UnitsLevel1), 50m)
			.SetGreaterThanZero()
			.SetDisplay("Level 1 Units", "First level cash or units", "Risk");
		_unitsLevel2 = Param(nameof(UnitsLevel2), 100m)
			.SetGreaterThanZero()
			.SetDisplay("Level 2 Units", "Second level cash or units", "Risk");
		_unitsLevel3 = Param(nameof(UnitsLevel3), 200m)
			.SetGreaterThanZero()
			.SetDisplay("Level 3 Units", "Third level cash or units", "Risk");
		_unitsLevel4 = Param(nameof(UnitsLevel4), 400m)
			.SetGreaterThanZero()
			.SetDisplay("Level 4 Units", "Fourth level cash or units", "Risk");
		_unitsLevel5 = Param(nameof(UnitsLevel5), 600m)
			.SetGreaterThanZero()
			.SetDisplay("Level 5 Units", "Fifth level cash or units", "Risk");

		_useUnits = Param(nameof(UseUnits), false)
			.SetDisplay("Use Units", "If true quantity is in units otherwise in cash", "General");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1.67m)
			.SetGreaterThanZero()
			.SetDisplay("Take Profit %", "Target take profit percent", "Risk");

		_trailingEnabled = Param(nameof(TrailingEnabled), false)
			.SetDisplay("Enable Trailing", "Enable trailing take profit", "Risk");

		_trailingDistancePercent = Param(nameof(TrailingDistancePercent), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Trailing Distance %", "Trailing distance percent", "Risk");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		ResetState();
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartProtection();

		var ma = CreateMa(MovingAverageType, MaLength);

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(ma, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var l1 = maValue * (1 - Deviation1 / 100m);
		var l2 = maValue * (1 - Deviation2 / 100m);
		var l3 = maValue * (1 - Deviation3 / 100m);
		var l4 = maValue * (1 - Deviation4 / 100m);
		var l5 = maValue * (1 - Deviation5 / 100m);

		if (Position > 0)
		{
			var tpPrice = PositionPrice * (1 + TakeProfitPercent / 100m);

			if (TrailingEnabled)
			{
				if (!_trailingActive)
				{
					if (candle.HighPrice >= tpPrice)
					{
						_trailingActive = true;
						_extremePrice = candle.HighPrice;
						_trailPrice = _extremePrice * (1 - TrailingDistancePercent / 100m);
					}
				}
				else
				{
					if (candle.HighPrice > _extremePrice)
					{
						_extremePrice = candle.HighPrice;
						_trailPrice = _extremePrice * (1 - TrailingDistancePercent / 100m);
					}

					if (candle.LowPrice <= _trailPrice)
					{
						SellMarket(Position);
						ResetState();
						return;
					}
				}
			}
			else
			{
				if (candle.HighPrice >= tpPrice)
				{
					SellMarket(Position);
					ResetState();
					return;
				}
			}
		}

		if (candle.ClosePrice < l1 && _entries == 0)
		{
			BuyLevel(UnitsLevel1, candle.ClosePrice);
			_entries = 1;
			_s2 = l2;
		}
		else if (candle.ClosePrice < _s2 && _entries == 1)
		{
			BuyLevel(UnitsLevel2, candle.ClosePrice);
			_entries = 2;
			_s3 = l3;
		}
		else if (candle.ClosePrice < _s3 && _entries == 2)
		{
			BuyLevel(UnitsLevel3, candle.ClosePrice);
			_entries = 3;
			_s4 = l4;
		}
		else if (candle.ClosePrice < _s4 && _entries == 3)
		{
			BuyLevel(UnitsLevel4, candle.ClosePrice);
			_entries = 4;
			_s5 = l5;
		}
		else if (candle.ClosePrice < _s5 && _entries == 4)
		{
			BuyLevel(UnitsLevel5, candle.ClosePrice);
			_entries = 5;
		}
	}

	private void BuyLevel(decimal units, decimal price)
	{
		var volume = UseUnits ? units : units / price;
		BuyMarket(volume);
	}

	private void ResetState()
	{
		_entries = 0;
		_s2 = 0m;
		_s3 = 0m;
		_s4 = 0m;
		_s5 = 0m;
		_trailingActive = false;
		_trailPrice = 0m;
		_extremePrice = 0m;
	}

	private static LengthIndicator<decimal> CreateMa(MaType type, int length)
	{
		return type switch
		{
			MaType.Sma => new SimpleMovingAverage { Length = length },
			MaType.Ema => new ExponentialMovingAverage { Length = length },
			MaType.Wma => new WeightedMovingAverage { Length = length },
			MaType.Rma => new SmoothedMovingAverage { Length = length },
			MaType.Hma => new HullMovingAverage { Length = length },
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
		};
	}
}

