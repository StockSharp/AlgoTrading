using System;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Multi-Step FlexiSuperTrend strategy - SuperTrend filter with deviation oscillator and multi-level take profit.
/// </summary>
public class MultiStepFlexiSuperTrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _atrPeriod;
	private readonly StrategyParam<decimal> _atrFactor;
	private readonly StrategyParam<int> _smaLength;
private readonly StrategyParam<Sides?> _direction;
	private readonly StrategyParam<decimal> _takeProfitLevel1;
	private readonly StrategyParam<decimal> _takeProfitLevel2;
	private readonly StrategyParam<decimal> _takeProfitLevel3;
	private readonly StrategyParam<decimal> _takeProfitPercent1;
	private readonly StrategyParam<decimal> _takeProfitPercent2;
	private readonly StrategyParam<decimal> _takeProfitPercent3;

	private SuperTrend _superTrend;
	private SimpleMovingAverage _sma;

	private decimal _tpPrice1;
	private decimal _tpPrice2;
	private decimal _tpPrice3;
	private decimal _tpVol1;
	private decimal _tpVol2;
	private decimal _tpVol3;
	private bool _tp1Done;
	private bool _tp2Done;
	private bool _tp3Done;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// ATR period for SuperTrend.
	/// </summary>
	public int AtrPeriod { get => _atrPeriod.Value; set => _atrPeriod.Value = value; }

	/// <summary>
	/// ATR multiplier for SuperTrend.
	/// </summary>
	public decimal AtrFactor { get => _atrFactor.Value; set => _atrFactor.Value = value; }

	/// <summary>
	/// SMA length for deviation smoothing.
	/// </summary>
	public int SmaLength { get => _smaLength.Value; set => _smaLength.Value = value; }

	/// <summary>
	/// Allowed trading direction.
	/// </summary>
public Sides? Direction { get => _direction.Value; set => _direction.Value = value; }

	/// <summary>
	/// First take profit level (fraction).
	/// </summary>
	public decimal TakeProfitLevel1 { get => _takeProfitLevel1.Value; set => _takeProfitLevel1.Value = value; }

	/// <summary>
	/// Second take profit level (fraction).
	/// </summary>
	public decimal TakeProfitLevel2 { get => _takeProfitLevel2.Value; set => _takeProfitLevel2.Value = value; }

	/// <summary>
	/// Third take profit level (fraction).
	/// </summary>
	public decimal TakeProfitLevel3 { get => _takeProfitLevel3.Value; set => _takeProfitLevel3.Value = value; }

	/// <summary>
	/// Portion to close at first level.
	/// </summary>
	public decimal TakeProfitPercent1 { get => _takeProfitPercent1.Value; set => _takeProfitPercent1.Value = value; }

	/// <summary>
	/// Portion to close at second level.
	/// </summary>
	public decimal TakeProfitPercent2 { get => _takeProfitPercent2.Value; set => _takeProfitPercent2.Value = value; }

	/// <summary>
	/// Portion to close at third level.
	/// </summary>
	public decimal TakeProfitPercent3 { get => _takeProfitPercent3.Value; set => _takeProfitPercent3.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MultiStepFlexiSuperTrendStrategy"/> class.
	/// </summary>
	public MultiStepFlexiSuperTrendStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");

		_atrPeriod = Param(nameof(AtrPeriod), 10)
			.SetGreaterThanZero()
			.SetDisplay("ATR Period", "ATR period for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);

		_atrFactor = Param(nameof(AtrFactor), 3m)
			.SetRange(0.5m, 10m)
			.SetDisplay("ATR Factor", "ATR factor for SuperTrend", "SuperTrend")
			.SetCanOptimize(true)
			.SetOptimize(1m, 6m, 0.5m);

		_smaLength = Param(nameof(SmaLength), 10)
			.SetGreaterThanZero()
			.SetDisplay("SMA Length", "Length of deviation smoothing", "Oscillator")
			.SetCanOptimize(true)
			.SetOptimize(5, 20, 5);
	 _direction = Param(nameof(Direction), null)
	        .SetDisplay("Trade Direction", "Allowed trading direction", "Strategy");

		_takeProfitLevel1 = Param(nameof(TakeProfitLevel1), 0.02m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Level 1", "First take profit level", "Take Profit");

		_takeProfitLevel2 = Param(nameof(TakeProfitLevel2), 0.08m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Level 2", "Second take profit level", "Take Profit");

		_takeProfitLevel3 = Param(nameof(TakeProfitLevel3), 0.18m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Level 3", "Third take profit level", "Take Profit");

		_takeProfitPercent1 = Param(nameof(TakeProfitPercent1), 0.3m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Percent 1", "Portion to close at level 1", "Take Profit");

		_takeProfitPercent2 = Param(nameof(TakeProfitPercent2), 0.2m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Percent 2", "Portion to close at level 2", "Take Profit");

		_takeProfitPercent3 = Param(nameof(TakeProfitPercent3), 0.15m)
			.SetRange(0m, 1m)
			.SetDisplay("Take Profit Percent 3", "Portion to close at level 3", "Take Profit");
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_superTrend = new() { Length = AtrPeriod, Multiplier = AtrFactor };
		_sma = new() { Length = SmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_superTrend, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _superTrend);
			DrawIndicator(area, _sma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal superTrendValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!_superTrend.IsFormed)
			return;

		var deviation = candle.ClosePrice - superTrendValue;
		var smaValue = _sma.Process(new DecimalIndicatorValue(_sma, deviation));

		if (!smaValue.IsFinal || smaValue is not DecimalIndicatorValue smaResult)
			return;

		var osc = smaResult.Value;
		var direction = candle.ClosePrice > superTrendValue ? -1 : 1;
	 var allowLong = Direction is null || Direction == Sides.Buy;
	var allowShort = Direction is null || Direction == Sides.Sell;

		if (allowLong && direction < 0 && osc > 0 && Position <= 0)
		{
			var baseVolume = Volume;
			BuyMarket(baseVolume + Math.Abs(Position));

			_tpPrice1 = candle.ClosePrice * (1 + TakeProfitLevel1);
			_tpPrice2 = candle.ClosePrice * (1 + TakeProfitLevel2);
			_tpPrice3 = candle.ClosePrice * (1 + TakeProfitLevel3);
			_tpVol1 = baseVolume * TakeProfitPercent1;
			_tpVol2 = baseVolume * TakeProfitPercent2;
			_tpVol3 = baseVolume * TakeProfitPercent3;
			_tp1Done = _tp2Done = _tp3Done = false;
		}
		else if (allowShort && direction > 0 && osc < 0 && Position >= 0)
		{
			var baseVolume = Volume;
			SellMarket(baseVolume + Math.Abs(Position));

			_tpPrice1 = candle.ClosePrice * (1 - TakeProfitLevel1);
			_tpPrice2 = candle.ClosePrice * (1 - TakeProfitLevel2);
			_tpPrice3 = candle.ClosePrice * (1 - TakeProfitLevel3);
			_tpVol1 = baseVolume * TakeProfitPercent1;
			_tpVol2 = baseVolume * TakeProfitPercent2;
			_tpVol3 = baseVolume * TakeProfitPercent3;
			_tp1Done = _tp2Done = _tp3Done = false;
		}
		else
		{
			if (Position > 0)
			{
				if (!_tp1Done && candle.HighPrice >= _tpPrice1)
				{
					SellMarket(Math.Min(Position, _tpVol1));
					_tp1Done = true;
				}

				if (!_tp2Done && candle.HighPrice >= _tpPrice2)
				{
					SellMarket(Math.Min(Position, _tpVol2));
					_tp2Done = true;
				}

				if (!_tp3Done && candle.HighPrice >= _tpPrice3)
				{
					SellMarket(Math.Min(Position, _tpVol3));
					_tp3Done = true;
				}

				if (direction > 0)
					SellMarket(Math.Abs(Position));
			}
			else if (Position < 0)
			{
				var absPos = Math.Abs(Position);

				if (!_tp1Done && candle.LowPrice <= _tpPrice1)
				{
					BuyMarket(Math.Min(absPos, _tpVol1));
					_tp1Done = true;
				}

				if (!_tp2Done && candle.LowPrice <= _tpPrice2)
				{
					BuyMarket(Math.Min(absPos, _tpVol2));
					_tp2Done = true;
				}

				if (!_tp3Done && candle.LowPrice <= _tpPrice3)
				{
					BuyMarket(Math.Min(absPos, _tpVol3));
					_tp3Done = true;
				}

				if (direction < 0)
					BuyMarket(Math.Abs(Position));
			}
		}
	}
}
