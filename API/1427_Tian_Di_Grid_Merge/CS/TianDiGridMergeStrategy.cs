using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Collections;
using Ecng.Serialization;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy with arithmetic spacing.
/// Buys when price drops below a grid level, sells when it rises above.
/// </summary>
public class TianDiGridMergeStrategy : Strategy
{
	private readonly StrategyParam<int> _gridQty;
	private readonly StrategyParam<decimal> _gridPercent;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _basePrice;
	private decimal[] _grid;
	private int _currentLevel;

	/// <summary>Number of grid levels.</summary>
	public int GridQty { get => _gridQty.Value; set => _gridQty.Value = value; }
	/// <summary>Percentage spacing between grid levels.</summary>
	public decimal GridPercent { get => _gridPercent.Value; set => _gridPercent.Value = value; }
	/// <summary>Candle type.</summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TianDiGridMergeStrategy()
	{
		_gridQty = Param(nameof(GridQty), 10)
			.SetGreaterThanZero();
		_gridPercent = Param(nameof(GridPercent), 0.5m)
			.SetGreaterThanZero();
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnReseted()
	{
		base.OnReseted();
		_basePrice = 0;
		_grid = null;
		_currentLevel = 0;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var sma = new SimpleMovingAverage { Length = 10 };
		var sub = SubscribeCandles(CandleType);
		sub.Bind(sma, ProcessCandle).Start();
	}

	private void ProcessCandle(ICandleMessage candle, decimal smaVal)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		// Initialize grid on first candle
		if (_grid == null)
		{
			_basePrice = price;
			_grid = new decimal[GridQty * 2 + 1];
			for (var i = 0; i < _grid.Length; i++)
			{
				var offset = i - GridQty;
				_grid[i] = _basePrice * (1 + offset * GridPercent / 100m);
			}
			_currentLevel = GridQty; // middle
			return;
		}

		// Find which level price is at
		var newLevel = _currentLevel;
		for (var i = 0; i < _grid.Length; i++)
		{
			if (price < _grid[i])
			{
				newLevel = i;
				break;
			}
			if (i == _grid.Length - 1)
				newLevel = _grid.Length;
		}

		// Grid trades: price dropped to lower level => buy, rose to higher => sell
		if (newLevel < _currentLevel && Position <= 0)
		{
			BuyMarket();
		}
		else if (newLevel > _currentLevel && Position >= 0)
		{
			SellMarket();
		}

		_currentLevel = newLevel;
	}
}
