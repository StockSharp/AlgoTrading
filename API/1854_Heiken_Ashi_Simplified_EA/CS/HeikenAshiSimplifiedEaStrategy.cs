using System;
using System.Collections.Generic;

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
	private readonly StrategyParam<decimal> _volume;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _haOpen1;
	private decimal _haOpen2;
	private decimal _haOpen3;
	private decimal _haOpen4;

	private decimal _haClose1;
	private decimal _haClose2;
	private decimal _haClose3;

	private decimal _priceDistance;
	private int _positionCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeikenAshiSimplifiedEaStrategy"/> class.
	/// </summary>
	public HeikenAshiSimplifiedEaStrategy()
	{
		_maxPositions = Param(nameof(MaxPositions), 3)
			.SetDisplay("Max Positions", "Maximum number of positions in direction", "General")
			.SetCanOptimize(true)
			.SetOptimize(1, 5, 1);

		_distancePoints = Param(nameof(DistancePoints), 300)
			.SetDisplay("Distance Points", "Minimum distance in price steps from last HA open", "General")
			.SetCanOptimize(true)
			.SetOptimize(50, 500, 50);

		_volume = Param(nameof(Volume), 1m)
			.SetDisplay("Volume", "Trade volume per entry", "General")
			.SetCanOptimize(true)
			.SetOptimize(1m, 5m, 1m);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for Heikin Ashi calculation", "General");
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
	/// Trade volume per entry.
	/// </summary>
	public decimal Volume
	{
		get => _volume.Value;
		set => _volume.Value = value;
	}

	/// <summary>
	/// Candle type used for calculations.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
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

		_haOpen1 = 0;
		_haOpen2 = 0;
		_haOpen3 = 0;
		_haOpen4 = 0;
		_haClose1 = 0;
		_haClose2 = 0;
		_haClose3 = 0;
		_priceDistance = 0;
		_positionCount = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceDistance = DistancePoints * (Security?.MinPriceStep ?? 1m);

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

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

		if (Position == 0)
			_positionCount = 0;

		if (_haOpen4 != 0)
		{
			var a = _haOpen4;
			var b = _haOpen3;
			var c = _haOpen2;
			var s = _haOpen1;

			var e = _haClose3;
			var f = _haClose2;
			var g = _haClose1;

			var w = a - b;
			var x = b - c;
			var y = c - s;

			var direction = 0;

			if (e > a && f > b && g > c && y > 0 && y < x && x < w)
				direction = 1;
			else if (e < a && f < b && g < c && y < 0 && y > x && x > w)
				direction = -1;

			if (direction != 0)
			{
				if (Position * direction < 0)
				{
					ClosePosition();
					_positionCount = 0;
				}

				var r = candle.ClosePrice - s;

				if (r * direction > 0 && Math.Abs(r) >= _priceDistance)
				{
					if (direction > 0 && _positionCount < MaxPositions)
					{
						BuyMarket(Volume);
						_positionCount++;
					}
					else if (direction < 0 && -_positionCount < MaxPositions)
					{
						SellMarket(Volume);
						_positionCount--;
					}
				}
			}
		}

		var haClose = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m;
		decimal haOpen;

		if (_haOpen1 == 0 && _haClose1 == 0)
			haOpen = (candle.OpenPrice + candle.ClosePrice) / 2m;
		else
			haOpen = (_haOpen1 + _haClose1) / 2m;

		_haOpen4 = _haOpen3;
		_haOpen3 = _haOpen2;
		_haOpen2 = _haOpen1;
		_haOpen1 = haOpen;

		_haClose3 = _haClose2;
		_haClose2 = _haClose1;
		_haClose1 = haClose;
	}
}
