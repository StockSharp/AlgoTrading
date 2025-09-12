using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Grid trading strategy with arithmetic or geometric spacing.
/// </summary>
public class TianDiGridMergeStrategy : Strategy
{
	private readonly StrategyParam<decimal> _upperBound;
	private readonly StrategyParam<decimal> _lowerBound;
	private readonly StrategyParam<int> _gridQty;
	private readonly StrategyParam<decimal> _orderValue;
	private readonly StrategyParam<bool> _useArithmetic;
	private readonly StrategyParam<bool> _useGeometric;
	private readonly StrategyParam<bool> _longEnabled;
	private readonly StrategyParam<bool> _shortEnabled;
	private readonly StrategyParam<bool> _closeOnProfit;
	private readonly StrategyParam<DataType> _candleType;

	private decimal[] _grid;
	private bool[] _opened;
	private decimal[] _qty;
	private decimal _gridWidth;

	public decimal UpperBound { get => _upperBound.Value; set => _upperBound.Value = value; }
	public decimal LowerBound { get => _lowerBound.Value; set => _lowerBound.Value = value; }
	public int GridQty { get => _gridQty.Value; set => _gridQty.Value = value; }
	public decimal OrderValue { get => _orderValue.Value; set => _orderValue.Value = value; }
	public bool UseArithmetic { get => _useArithmetic.Value; set => _useArithmetic.Value = value; }
	public bool UseGeometric { get => _useGeometric.Value; set => _useGeometric.Value = value; }
	public bool LongEnabled { get => _longEnabled.Value; set => _longEnabled.Value = value; }
	public bool ShortEnabled { get => _shortEnabled.Value; set => _shortEnabled.Value = value; }
	public bool CloseOnProfit { get => _closeOnProfit.Value; set => _closeOnProfit.Value = value; }
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	public TianDiGridMergeStrategy()
	{
		_upperBound = Param(nameof(UpperBound), 3900m);
		_lowerBound = Param(nameof(LowerBound), 900m);
		_gridQty = Param(nameof(GridQty), 20);
		_orderValue = Param(nameof(OrderValue), 100m);
		_useArithmetic = Param(nameof(UseArithmetic), true);
		_useGeometric = Param(nameof(UseGeometric), false);
		_longEnabled = Param(nameof(LongEnabled), true);
		_shortEnabled = Param(nameof(ShortEnabled), false);
		_closeOnProfit = Param(nameof(CloseOnProfit), true);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame());
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);
		_buildGrid();
		var sub = SubscribeCandles(CandleType);
		sub.Bind(ProcessCandle).Start();
	}

	private void _buildGrid()
	{
		_grid = new decimal[GridQty];
		_opened = new bool[GridQty];
		_qty = new decimal[GridQty];
		if (UseGeometric && !UseArithmetic)
		{
			var factor = Math.Pow((double)(UpperBound / LowerBound), 1.0 / (GridQty - 1));
			for (var i = 0; i < GridQty; i++)
				_grid[i] = LowerBound * (decimal)Math.Pow(factor, i);
		}
		else
		{
			_gridWidth = (UpperBound - LowerBound) / (GridQty - 1);
			for (var i = 0; i < GridQty; i++)
				_grid[i] = LowerBound + _gridWidth * i;
		}
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;
		for (var i = 1; i < GridQty; i++)
		{
			var level = _grid[i];
			var prev = _grid[i - 1];
			if (LongEnabled && !ShortEnabled)
			{
				if (price < level && !_opened[i - 1])
				{
					var q = OrderValue / price;
					BuyMarket(q);
					_opened[i - 1] = true;
					_qty[i - 1] = q;
				}
				else if (price > level && _opened[i - 1])
				{
					SellMarket(_qty[i - 1]);
					_opened[i - 1] = false;
				}
			}
			else if (ShortEnabled && !LongEnabled)
			{
				if (price > prev && !_opened[i])
				{
					var q = OrderValue / price;
					SellMarket(q);
					_opened[i] = true;
					_qty[i] = q;
				}
				else if (price < prev && _opened[i])
				{
					BuyMarket(_qty[i]);
					_opened[i] = false;
				}
			}
		}

		if (CloseOnProfit)
		{
			var step = _gridWidth == 0m ? _grid[1] - _grid[0] : _gridWidth;
			if (Position > 0 && price >= PositionAvgPrice + step)
				SellMarket(Position);
			else if (Position < 0 && price <= PositionAvgPrice - step)
				BuyMarket(-Position);
		}
	}
}
