using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// TTM-based grid trading strategy.
/// </summary>
public class TTMGridStrategy : Strategy
{
	private readonly StrategyParam<int> _ttmPeriod;
	private readonly StrategyParam<int> _gridLevels;
	private readonly StrategyParam<decimal> _gridSpacing;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _gridBasePrice;
	private int _gridDirection = -1;

	private ExponentialMovingAverage _lowEma;
	private ExponentialMovingAverage _highEma;

	/// <summary>
	/// EMA period for TTM calculation.
	/// </summary>
	public int TtmPeriod
	{
		get => _ttmPeriod.Value;
		set => _ttmPeriod.Value = value;
	}

	/// <summary>
	/// Number of grid levels.
	/// </summary>
	public int GridLevels
	{
		get => _gridLevels.Value;
		set => _gridLevels.Value = value;
	}

	/// <summary>
	/// Grid spacing as percentage.
	/// </summary>
	public decimal GridSpacing
	{
		get => _gridSpacing.Value;
		set => _gridSpacing.Value = value;
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
	/// Initializes a new instance of the <see cref="TTMGridStrategy"/>.
	/// </summary>
	public TTMGridStrategy()
	{
		_ttmPeriod = Param(nameof(TtmPeriod), 6)
			.SetGreaterThanZero()
			.SetDisplay("TTM Period", "EMA period for TTM calculation", "Indicators")
			.SetCanOptimize(true);

		_gridLevels = Param(nameof(GridLevels), 5)
			.SetRange(2, 20)
			.SetDisplay("Grid Levels", "Number of price levels in the grid", "Strategy")
			.SetCanOptimize(true);

		_gridSpacing = Param(nameof(GridSpacing), 0.01m)
			.SetRange(0.001m, 0.05m)
			.SetDisplay("Grid Spacing", "Distance between grid levels (fraction)", "Strategy")
			.SetCanOptimize(true);

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

		_gridBasePrice = 0m;
		_gridDirection = -1;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_lowEma = new ExponentialMovingAverage
		{
			Length = TtmPeriod,
			CandlePrice = CandlePrice.Low
		};

		_highEma = new ExponentialMovingAverage
		{
			Length = TtmPeriod,
			CandlePrice = CandlePrice.High
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_lowEma, _highEma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _lowEma);
			DrawIndicator(area, _highEma);
			DrawOwnTrades(area);
		}
	}

	private void OnProcess(ICandleMessage candle, decimal lowMa, decimal highMa)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (!_lowEma.IsFormed || !_highEma.IsFormed)
			return;

		var lowThird = (highMa - lowMa) / 3m + lowMa;
		var highThird = 2m * (highMa - lowMa) / 3m + lowMa;

		var currentState = candle.ClosePrice > highThird
			? 1
			: candle.ClosePrice < lowThird ? 0 : -1;

		if (currentState != -1 && currentState != _gridDirection)
		{
			_gridBasePrice = candle.ClosePrice;
			_gridDirection = currentState;
		}

		if (_gridDirection == -1)
			return;

		for (var i = 1; i <= GridLevels; i++)
		{
			var multiplier = i * GridSpacing;

			if (_gridDirection == 1)
			{
				var buyLevel = _gridBasePrice * (1 - multiplier);
				var sellLevel = _gridBasePrice * (1 + multiplier);

				if (candle.LowPrice <= buyLevel)
					BuyMarket();

				if (candle.HighPrice >= sellLevel)
					SellMarket();
			}
			else // sell grid
			{
				var buyLevel = _gridBasePrice * (1 + multiplier);
				var sellLevel = _gridBasePrice * (1 - multiplier);

				if (candle.HighPrice >= buyLevel)
					BuyMarket();

				if (candle.LowPrice <= sellLevel)
					SellMarket();
			}
		}
	}
}
