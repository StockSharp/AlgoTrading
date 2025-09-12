using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Hull Suite strategy - trades when Hull moving average changes direction.
/// </summary>
public class HullSuite1RiskNoSlTpStrategy : Strategy
{
	private readonly StrategyParam<int> _hullLength;
	private readonly StrategyParam<HullVariation> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private HullMovingAverage _hma;
	private ExponentialMovingAverage _ehmaFull;
	private ExponentialMovingAverage _ehmaHalf;
	private ExponentialMovingAverage _ehmaResult;
	private WeightedMovingAverage _thmaFull;
	private WeightedMovingAverage _thmaHalf;
	private WeightedMovingAverage _thmaThird;
	private WeightedMovingAverage _thmaResult;
	private IIndicator _hullIndicator;

	private decimal _prevHull;
	private decimal _prevHull2;
	private bool _isInitialized;

	/// <summary>
	/// Hull length.
	/// </summary>
	public int HullLength
	{
		get => _hullLength.Value;
		set => _hullLength.Value = value;
	}

	/// <summary>
	/// Hull variation.
	/// </summary>
	public HullVariation Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
	}

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the strategy.
	/// </summary>
	public HullSuite1RiskNoSlTpStrategy()
	{
		_hullLength = Param(nameof(HullLength), 55)
						  .SetGreaterThanZero()
						  .SetDisplay("Hull Length", "Period for Hull calculations", "Indicators")
						  .SetCanOptimize(true)
						  .SetOptimize(20, 80, 5);

		_mode = Param(nameof(Mode), HullVariation.Hma)
					.SetDisplay("Hull Variation", "Type of Hull moving average", "Indicators");

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

		_prevHull = default;
		_prevHull2 = default;
		_isInitialized = false;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		switch (Mode)
		{
		case HullVariation.Hma:
			_hma = new HullMovingAverage { Length = HullLength };
			_hullIndicator = _hma;
			break;
		case HullVariation.Ehma:
			_ehmaFull = new ExponentialMovingAverage { Length = HullLength };
			_ehmaHalf = new ExponentialMovingAverage { Length = HullLength / 2 };
			_ehmaResult = new ExponentialMovingAverage { Length = (int)Math.Round(Math.Sqrt(HullLength)) };
			_hullIndicator = _ehmaResult;
			break;
		case HullVariation.Thma:
			_thmaFull = new WeightedMovingAverage { Length = HullLength };
			_thmaHalf = new WeightedMovingAverage { Length = HullLength / 2 };
			_thmaThird = new WeightedMovingAverage { Length = Math.Max(1, HullLength / 3) };
			_thmaResult = new WeightedMovingAverage { Length = HullLength };
			_hullIndicator = _thmaResult;
			break;
		}

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			if (_hullIndicator != null)
				DrawIndicator(area, _hullIndicator);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var hull = CalculateHull(candle.ClosePrice);
		if (hull is not decimal hullValue)
			return;

		if (!_isInitialized)
		{
			_prevHull2 = hullValue;
			_prevHull = hullValue;
			_isInitialized = true;
			return;
		}

		var isBull = hullValue > _prevHull2;
		var isBear = hullValue < _prevHull2;

		if (isBull && Position <= 0)
			BuyMarket(Volume + Math.Abs(Position));
		else if (isBear && Position >= 0)
			SellMarket(Volume + Math.Abs(Position));

		_prevHull2 = _prevHull;
		_prevHull = hullValue;
	}

	private decimal? CalculateHull(decimal price)
	{
		switch (Mode)
		{
			case HullVariation.Hma:
			{
				return _hma.Process(price).ToNullableDecimal();
			}
			case HullVariation.Ehma:
			{
				var emaFull = _ehmaFull.Process(price).ToNullableDecimal();
				var emaHalf = _ehmaHalf.Process(price).ToNullableDecimal();
				if (emaFull is not decimal full || emaHalf is not decimal half)
					return null;
				var diff = 2m * half - full;
				return _ehmaResult.Process(diff).ToNullableDecimal();
			}
			case HullVariation.Thma:
			{
				var wmaFull = _thmaFull.Process(price).ToNullableDecimal();
				var wmaHalf = _thmaHalf.Process(price).ToNullableDecimal();
				var wmaThird = _thmaThird.Process(price).ToNullableDecimal();
				if (wmaFull is not decimal full || wmaHalf is not decimal half ||
					wmaThird is not decimal third)
					return null;
				var diff = 3m * third - half - full;
				return _thmaResult.Process(diff).ToNullableDecimal();
			}
			default:
				throw new InvalidOperationException(Mode.ToString());
		}
	}
}

public enum HullVariation
{
	Hma,
	Ehma,
	Thma
}
