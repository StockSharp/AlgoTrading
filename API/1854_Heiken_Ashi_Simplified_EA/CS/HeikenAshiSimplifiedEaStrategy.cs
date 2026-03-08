using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy based on simplified Heikin Ashi pattern analysis.
/// </summary>
public class HeikenAshiSimplifiedEaStrategy : Strategy
{
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<int> _distancePoints;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _cooldownBars;

	private decimal _haOpen1;
	private decimal _haOpen2;
	private decimal _haOpen3;
	private decimal _haOpen4;
	private decimal _haClose1;
	private decimal _haClose2;
	private decimal _haClose3;
	private decimal _priceDistance;
	private int _positionCount;
	private int _cooldownRemaining;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikenAshiSimplifiedEaStrategy"/> class.
	/// </summary>
	public HeikenAshiSimplifiedEaStrategy()
	{
		_maxPositions = Param(nameof(MaxPositions), 1)
			.SetDisplay("Max Positions", "Maximum number of positions in direction", "General")
			.SetOptimize(1, 5, 1);

		_distancePoints = Param(nameof(DistancePoints), 400)
			.SetDisplay("Distance Points", "Minimum distance in price steps from last HA open", "General")
			.SetOptimize(50, 500, 50);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Heikin Ashi calculation", "General");

		_cooldownBars = Param(nameof(CooldownBars), 6)
			.SetDisplay("Cooldown Bars", "Completed candles to wait after a position change", "Trading");
	}

	/// <summary>
	/// Maximum number of positions allowed in one direction.
	/// </summary>
	public int MaxPositions
	{
		get => _maxPositions.Value;
		set => _maxPositions.Value = value;
	}

	/// <summary>
	/// Minimum distance in price steps from last Heikin Ashi open.
	/// </summary>
	public int DistancePoints
	{
		get => _distancePoints.Value;
		set => _distancePoints.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Number of completed candles to wait after a position change.
	/// </summary>
	public int CooldownBars
	{
		get => _cooldownBars.Value;
		set => _cooldownBars.Value = value;
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

		_haOpen1 = 0m;
		_haOpen2 = 0m;
		_haOpen3 = 0m;
		_haOpen4 = 0m;
		_haClose1 = 0m;
		_haClose2 = 0m;
		_haClose3 = 0m;
		_priceDistance = 0m;
		_positionCount = 0;
		_cooldownRemaining = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_priceDistance = DistancePoints * (Security.PriceStep ?? 1m);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_cooldownRemaining > 0)
			_cooldownRemaining--;

		if (Position == 0)
			_positionCount = 0;

		if (_haOpen4 != 0m)
		{
			var direction = 0;
			if (_haClose1 > _haOpen1 && _haClose2 > _haOpen2 && _haClose3 > _haOpen3)
				direction = 1;
			else if (_haClose1 < _haOpen1 && _haClose2 < _haOpen2 && _haClose3 < _haOpen3)
				direction = -1;

			if (direction != 0)
			{
				if (Position * direction < 0)
				{
					if (Position > 0)
						SellMarket();
					else if (Position < 0)
						BuyMarket();

					_positionCount = 0;
					_cooldownRemaining = CooldownBars;
				}

				var distanceFromAnchor = candle.ClosePrice - _haOpen1;
				if (_cooldownRemaining == 0 && distanceFromAnchor * direction > 0m && Math.Abs(distanceFromAnchor) >= _priceDistance)
				{
					if (direction > 0 && _positionCount < MaxPositions && Position <= 0)
					{
						BuyMarket();
						_positionCount = 1;
						_cooldownRemaining = CooldownBars;
					}
					else if (direction < 0 && _positionCount > -MaxPositions && Position >= 0)
					{
						SellMarket();
						_positionCount = -1;
						_cooldownRemaining = CooldownBars;
					}
				}
			}
		}

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		var haOpen = _haOpen1 == 0m && _haClose1 == 0m
			? (candle.OpenPrice + candle.ClosePrice) / 2m
			: (_haOpen1 + _haClose1) / 2m;

		_haOpen4 = _haOpen3;
		_haOpen3 = _haOpen2;
		_haOpen2 = _haOpen1;
		_haOpen1 = haOpen;

		_haClose3 = _haClose2;
		_haClose2 = _haClose1;
		_haClose1 = haClose;
	}
}
