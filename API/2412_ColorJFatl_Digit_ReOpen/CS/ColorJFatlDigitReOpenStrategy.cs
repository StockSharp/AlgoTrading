using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Jurik moving average trend strategy with re-entry at fixed price steps.
/// Opens long when JMA turns up and closes short positions.
/// Opens short when JMA turns down and closes long positions.
/// Adds additional positions when price moves by a defined step in favor of the trade.
/// </summary>
public class ColorJFatlDigitReOpenStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<int> _jmaLength;
	private readonly StrategyParam<int> _priceStepParam;
	private readonly StrategyParam<int> _maxPositions;
	private readonly StrategyParam<bool> _buyPosOpen;
	private readonly StrategyParam<bool> _sellPosOpen;
	private readonly StrategyParam<bool> _buyPosClose;
	private readonly StrategyParam<bool> _sellPosClose;

	private decimal? _prevJma;
	private int _prevDirection;
	private decimal? _lastEntryPrice;
	private int _positionsOpened;
	private decimal _priceStep;

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Jurik moving average length.
	/// </summary>
	public int JmaLength { get => _jmaLength.Value; set => _jmaLength.Value = value; }

	/// <summary>
	/// Re-entry price step in points.
	/// </summary>
	public int PriceStep { get => _priceStepParam.Value; set => _priceStepParam.Value = value; }

	/// <summary>
	/// Maximum number of positions in one direction.
	/// </summary>
	public int MaxPositions { get => _maxPositions.Value; set => _maxPositions.Value = value; }

	/// <summary>
	/// Allow opening long positions.
	/// </summary>
	public bool BuyPosOpen { get => _buyPosOpen.Value; set => _buyPosOpen.Value = value; }

	/// <summary>
	/// Allow opening short positions.
	/// </summary>
	public bool SellPosOpen { get => _sellPosOpen.Value; set => _sellPosOpen.Value = value; }

	/// <summary>
	/// Allow closing long positions.
	/// </summary>
	public bool BuyPosClose { get => _buyPosClose.Value; set => _buyPosClose.Value = value; }

	/// <summary>
	/// Allow closing short positions.
	/// </summary>
	public bool SellPosClose { get => _sellPosClose.Value; set => _sellPosClose.Value = value; }

	/// <summary>
	/// Initialize strategy parameters.
	/// </summary>
	public ColorJFatlDigitReOpenStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for calculations", "General");

		_jmaLength = Param(nameof(JmaLength), 5)
		.SetGreaterThanZero()
		.SetDisplay("JMA Length", "Jurik moving average length", "Indicators")
		.SetCanOptimize(true)
		.SetOptimize(2, 20, 1);

		_priceStepParam = Param(nameof(PriceStep), 300)
		.SetGreaterThanZero()
		.SetDisplay("Price Step", "Price step in points for re-entry", "Risk Management");

		_maxPositions = Param(nameof(MaxPositions), 10)
		.SetGreaterThanZero()
		.SetDisplay("Max Positions", "Maximum number of positions", "Risk Management");

		_buyPosOpen = Param(nameof(BuyPosOpen), true)
		.SetDisplay("Open Long", "Allow long entries", "Trading");

		_sellPosOpen = Param(nameof(SellPosOpen), true)
		.SetDisplay("Open Short", "Allow short entries", "Trading");

		_buyPosClose = Param(nameof(BuyPosClose), true)
		.SetDisplay("Close Long", "Allow closing longs", "Trading");

		_sellPosClose = Param(nameof(SellPosClose), true)
		.SetDisplay("Close Short", "Allow closing shorts", "Trading");
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
		_prevJma = null;
		_prevDirection = 0;
		_lastEntryPrice = null;
		_positionsOpened = 0;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_priceStep = (Security?.PriceStep ?? 0m) * PriceStep;

		var jma = new JurikMovingAverage { Length = JmaLength };

		var subscription = SubscribeCandles(CandleType);
		subscription
		.Bind(jma, ProcessCandle)
		.Start();

		StartProtection();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, jma);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal jmaValue)
	{
		if (candle.State != CandleStates.Finished)
		return;

		var direction = _prevJma is decimal prev ? (jmaValue > prev ? 1 : jmaValue < prev ? -1 : 0) : 0;

		// close positions on opposite signals
		if (direction == -1 && Position > 0 && BuyPosClose)
		{
			SellMarket();
			_positionsOpened = 0;
			_lastEntryPrice = null;
		}
		else if (direction == 1 && Position < 0 && SellPosClose)
		{
			BuyMarket();
			_positionsOpened = 0;
			_lastEntryPrice = null;
		}

		// initial entries
		if (direction == 1 && _prevDirection != 1 && BuyPosOpen && Position <= 0)
		{
			BuyMarket();
			_positionsOpened = 1;
			_lastEntryPrice = candle.ClosePrice;
		}
		else if (direction == -1 && _prevDirection != -1 && SellPosOpen && Position >= 0)
		{
			SellMarket();
			_positionsOpened = 1;
			_lastEntryPrice = candle.ClosePrice;
		}
		// re-entry logic
		else if (Position > 0 && BuyPosOpen && _positionsOpened < MaxPositions && _lastEntryPrice is decimal lastBuy && candle.ClosePrice - lastBuy >= _priceStep)
		{
			BuyMarket();
			_positionsOpened++;
			_lastEntryPrice = candle.ClosePrice;
		}
		else if (Position < 0 && SellPosOpen && _positionsOpened < MaxPositions && _lastEntryPrice is decimal lastSell && lastSell - candle.ClosePrice >= _priceStep)
		{
			SellMarket();
			_positionsOpened++;
			_lastEntryPrice = candle.ClosePrice;
		}

		_prevDirection = direction;
		_prevJma = jmaValue;
	}
}
