using System;
using System.Collections.Generic;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// EMA grid strategy with optional martingale sizing and cooldown between grids.
/// </summary>
public class EmaGridMartingaleCooldownStrategy : Strategy
{
	private readonly StrategyParam<int> _ema1Length;
	private readonly StrategyParam<int> _ema2Length;
	private readonly StrategyParam<int> _ema3Length;
	private readonly StrategyParam<int> _ema4Length;
	private readonly StrategyParam<int> _maxEntries;
	private readonly StrategyParam<int> _gridStepPips;
	private readonly StrategyParam<decimal> _baseOrderSize;
	private readonly StrategyParam<bool> _useMartingale;
	private readonly StrategyParam<decimal> _martingaleFactor;
	private readonly StrategyParam<bool> _closeAtWeighted;
	private readonly StrategyParam<int> _bufferPips;
	private readonly StrategyParam<int> _cooldownBars;
	private readonly StrategyParam<DataType> _candleType;

	private bool _inGrid;
	private int _entryCount;
	private decimal _lastEntryPrice;
	private int _lastCloseBar;
	private int _barIndex;
	private bool _prevGrp1Bull;
	private bool _prevGrp2Bull;
	private decimal _avgPrice;
	private decimal _positionVolume;
	private decimal _stepPrice;
	private decimal _bufferPrice;

	/// <summary>
	/// Length of EMA 1.
	/// </summary>
	public int Ema1Length { get => _ema1Length.Value; set => _ema1Length.Value = value; }

	/// <summary>
	/// Length of EMA 2.
	/// </summary>
	public int Ema2Length { get => _ema2Length.Value; set => _ema2Length.Value = value; }

	/// <summary>
	/// Length of EMA 3.
	/// </summary>
	public int Ema3Length { get => _ema3Length.Value; set => _ema3Length.Value = value; }

	/// <summary>
	/// Length of EMA 4.
	/// </summary>
	public int Ema4Length { get => _ema4Length.Value; set => _ema4Length.Value = value; }

	/// <summary>
	/// Maximum number of grid entries.
	/// </summary>
	public int MaxGridEntries { get => _maxEntries.Value; set => _maxEntries.Value = value; }

	/// <summary>
	/// Grid step in pips.
	/// </summary>
	public int GridStepPips { get => _gridStepPips.Value; set => _gridStepPips.Value = value; }

	/// <summary>
	/// Base order size.
	/// </summary>
	public decimal BaseOrderSize { get => _baseOrderSize.Value; set => _baseOrderSize.Value = value; }

	/// <summary>
	/// Use martingale sizing.
	/// </summary>
	public bool UseMartingale { get => _useMartingale.Value; set => _useMartingale.Value = value; }

	/// <summary>
	/// Martingale factor.
	/// </summary>
	public decimal MartingaleFactor { get => _martingaleFactor.Value; set => _martingaleFactor.Value = value; }

	/// <summary>
	/// Close position at weighted price.
	/// </summary>
	public bool CloseAtWeighted { get => _closeAtWeighted.Value; set => _closeAtWeighted.Value = value; }

	/// <summary>
	/// Buffer in pips for weighted close.
	/// </summary>
	public int BufferPips { get => _bufferPips.Value; set => _bufferPips.Value = value; }

	/// <summary>
	/// Cooldown bars between grids.
	/// </summary>
	public int CooldownBars { get => _cooldownBars.Value; set => _cooldownBars.Value = value; }

	/// <summary>
	/// Candle type.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initializes a new instance of the <see cref="EmaGridMartingaleCooldownStrategy"/> class.
	/// </summary>
	public EmaGridMartingaleCooldownStrategy()
	{
		_ema1Length = Param(nameof(Ema1Length), 10)
			.SetGreaterThanZero()
			.SetDisplay("EMA 1 Length", "Fast EMA length group 1", "EMA").SetCanOptimize(true);

		_ema2Length = Param(nameof(Ema2Length), 20)
			.SetGreaterThanZero()
			.SetDisplay("EMA 2 Length", "Slow EMA length group 1", "EMA").SetCanOptimize(true);

		_ema3Length = Param(nameof(Ema3Length), 50)
			.SetGreaterThanZero()
			.SetDisplay("EMA 3 Length", "Fast EMA length group 2", "EMA").SetCanOptimize(true);

		_ema4Length = Param(nameof(Ema4Length), 100)
			.SetGreaterThanZero()
			.SetDisplay("EMA 4 Length", "Slow EMA length group 2", "EMA").SetCanOptimize(true);

		_maxEntries = Param(nameof(MaxGridEntries), 5)
			.SetGreaterThanZero()
			.SetDisplay("Max Grid Entries", "Maximum number of grid orders", "Grid").SetCanOptimize(true);

		_gridStepPips = Param(nameof(GridStepPips), 20)
			.SetGreaterThanZero()
			.SetDisplay("Grid Step (pips)", "Distance between grid orders", "Grid").SetCanOptimize(true);

		_baseOrderSize = Param(nameof(BaseOrderSize), 0.1m)
			.SetGreaterThanZero()
			.SetDisplay("Base Order Size", "Initial order quantity", "Trading").SetCanOptimize(true);

		_useMartingale = Param(nameof(UseMartingale), true)
			.SetDisplay("Use Martingale", "Enable martingale sizing", "Trading");

		_martingaleFactor = Param(nameof(MartingaleFactor), 2m)
			.SetGreaterThanZero()
			.SetDisplay("Martingale Factor", "Multiplier for martingale sizing", "Trading").SetCanOptimize(true);

		_closeAtWeighted = Param(nameof(CloseAtWeighted), true)
			.SetDisplay("Close At Weighted", "Close at average price plus buffer", "Exit");

		_bufferPips = Param(nameof(BufferPips), 0)
			.SetDisplay("Close Buffer (pips)", "Buffer above average price", "Exit").SetCanOptimize(true);

		_cooldownBars = Param(nameof(CooldownBars), 20)
			.SetDisplay("Cooldown Bars", "Bars to wait between grids", "General").SetCanOptimize(true);

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
		_inGrid = false;
		_entryCount = 0;
		_lastEntryPrice = 0m;
		_lastCloseBar = -1;
		_barIndex = 0;
		_prevGrp1Bull = false;
		_prevGrp2Bull = false;
		_avgPrice = 0m;
		_positionVolume = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		var ema1 = new EMA { Length = Ema1Length };
		var ema2 = new EMA { Length = Ema2Length };
		var ema3 = new EMA { Length = Ema3Length };
		var ema4 = new EMA { Length = Ema4Length };

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ema1, ema2, ema3, ema4, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, ema1);
			DrawIndicator(area, ema2);
			DrawIndicator(area, ema3);
			DrawIndicator(area, ema4);
			DrawOwnTrades(area);
		}

		var pipValue = (Security.PriceStep ?? 1m) * 10m;
		_stepPrice = GridStepPips * pipValue;
		_bufferPrice = BufferPips * pipValue;
	}

	private void ProcessCandle(ICandleMessage candle, decimal e1, decimal e2, decimal e3, decimal e4)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var grp1Bull = e1 > e2;
		var grp2Bull = e3 > e4;
		var buySignal = grp1Bull && grp2Bull && !(_prevGrp1Bull && _prevGrp2Bull);
		var canOpenNewGrid = _lastCloseBar < 0 || _barIndex - _lastCloseBar > CooldownBars;

		if (buySignal && !_inGrid && canOpenNewGrid)
		{
			CloseAll();
			_inGrid = true;
			_entryCount = 1;
			_lastEntryPrice = candle.ClosePrice;
			BuyMarket(BaseOrderSize);
		}

		if (_inGrid && _entryCount < MaxGridEntries)
		{
			if (Position > 0 && candle.LowPrice <= _lastEntryPrice - _stepPrice)
			{
				_entryCount++;
				_lastEntryPrice = candle.ClosePrice;
				var size = UseMartingale ? BaseOrderSize * (decimal)Math.Pow((double)MartingaleFactor, _entryCount - 1) : BaseOrderSize;
				BuyMarket(size);
			}
		}

		if (CloseAtWeighted && _inGrid && Position > 0)
		{
			if (candle.HighPrice >= _avgPrice + _bufferPrice)
			{
				CloseAll();
				_inGrid = false;
				_entryCount = 0;
				_lastCloseBar = _barIndex;
			}
		}

		_prevGrp1Bull = grp1Bull;
		_prevGrp2Bull = grp2Bull;
		_barIndex++;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null)
			return;

		if (trade.Order.Side == Sides.Buy)
		{
			var newVolume = _positionVolume + trade.Trade.Volume;
			_avgPrice = (_avgPrice * _positionVolume + trade.Trade.Price * trade.Trade.Volume) / newVolume;
			_positionVolume = newVolume;
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			_positionVolume -= trade.Trade.Volume;
			if (_positionVolume <= 0)
			{
				_positionVolume = 0;
				_avgPrice = 0;
				_inGrid = false;
				_entryCount = 0;
			}
		}
	}

	private void CloseAll()
	{
		if (Position > 0)
			SellMarket(Math.Abs(Position));
		else if (Position < 0)
			BuyMarket(Math.Abs(Position));
	}
}

