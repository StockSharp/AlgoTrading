using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Simplified grid trading strategy based on FreedX approach.
/// Places orders at grid levels between top and bottom range.
/// </summary>
public class FreedxGridBacktestStrategy : Strategy
{
	public enum GridMode
	{
		Neutral,
		Long,
		Short
	}

	private readonly StrategyParam<decimal> _topLevel;
	private readonly StrategyParam<decimal> _bottomLevel;
	private readonly StrategyParam<int> _gridLevels;
	private readonly StrategyParam<GridMode> _mode;
	private readonly StrategyParam<DataType> _candleType;

	private decimal _reference;
	private decimal _step;
	private int _longIndex;
	private int _shortIndex;

	/// <summary>
	/// Top price of the grid range.
	/// </summary>
	public decimal TopLevel
	{
		get => _topLevel.Value;
		set => _topLevel.Value = value;
	}

	/// <summary>
	/// Bottom price of the grid range.
	/// </summary>
	public decimal BottomLevel
	{
		get => _bottomLevel.Value;
		set => _bottomLevel.Value = value;
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
	/// Trading mode for the grid.
	/// </summary>
	public GridMode Mode
	{
		get => _mode.Value;
		set => _mode.Value = value;
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
	/// Constructor.
	/// </summary>
	public FreedxGridBacktestStrategy()
	{
		_topLevel = Param(nameof(TopLevel), 44000m)
			.SetDisplay("Top Level", "Upper bound of the grid", "Grid Settings");

		_bottomLevel = Param(nameof(BottomLevel), 39000m)
			.SetDisplay("Bottom Level", "Lower bound of the grid", "Grid Settings");

		_gridLevels = Param(nameof(GridLevels), 10)
			.SetGreaterThanZero()
			.SetDisplay("Grid Levels", "Number of grid levels", "Grid Settings");

		_mode = Param(nameof(Mode), GridMode.Neutral)
			.SetDisplay("Mode", "Trading direction", "Grid Settings");

		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles", "General");
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
		_reference = 0m;
		_step = 0m;
		_longIndex = 0;
		_shortIndex = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_step = (TopLevel - BottomLevel) / (GridLevels - 1);
		_reference = (TopLevel + BottomLevel) / 2m;

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

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		// Long side
		if (Mode == GridMode.Neutral || Mode == GridMode.Long)
		{
			while (candle.LowPrice <= _reference - _step * (_longIndex + 1))
			{
				BuyMarket(Volume);
				_longIndex++;
			}

			while (_longIndex > 0 && candle.HighPrice >= _reference - _step * (_longIndex - 1))
			{
				SellMarket(Volume);
				_longIndex--;
			}
		}

		// Short side
		if (Mode == GridMode.Neutral || Mode == GridMode.Short)
		{
			while (candle.HighPrice >= _reference + _step * (_shortIndex + 1))
			{
				SellMarket(Volume);
				_shortIndex++;
			}

			while (_shortIndex > 0 && candle.LowPrice <= _reference + _step * (_shortIndex - 1))
			{
				BuyMarket(Volume);
				_shortIndex--;
			}
		}
	}
}
