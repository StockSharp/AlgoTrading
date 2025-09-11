using System;
using System.Collections.Generic;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// DCA Simulation for CryptoCommunity strategy.
/// Performs periodic purchases and safety orders with trailing take profit.
/// </summary>
public class DcaSimulationForCryptoCommunityStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _baseOrder;
	private readonly StrategyParam<bool> _dcaEnabled;
	private readonly StrategyParam<decimal> _dcaAmount;
	private readonly StrategyParam<int> _dcaFrequency;
	private readonly StrategyParam<bool> _safeOrderEnabled;
	private readonly StrategyParam<decimal> _safeOrder;
	private readonly StrategyParam<decimal> _priceDeviation;
	private readonly StrategyParam<decimal> _safeOrderVolumeScale;
	private readonly StrategyParam<decimal> _safeOrderStepScale;
	private readonly StrategyParam<int> _maxSafeOrders;
	private readonly StrategyParam<bool> _takeProfitEnable;
	private readonly StrategyParam<decimal> _takeProfitPercent;
	private readonly StrategyParam<decimal> _takeProfitGrowPercent;
	private readonly StrategyParam<decimal> _trailingPercent;
	private readonly StrategyParam<bool> _useDateFilter;
	private readonly StrategyParam<DateTimeOffset> _startDate;
	private readonly StrategyParam<DateTimeOffset> _endDate;

	private int _currentSo;
	private decimal _lastHigh;
	private int _barIndex;
	private int _entryBarIndex;
	private int _nextDcaBar;
	private decimal? _previousHighValue;
	private decimal? _originalTtpValue;

	/// <summary>
	/// Candle type for calculations.
	/// </summary>
	public DataType CandleType { get => _candleType.Value; set => _candleType.Value = value; }

	/// <summary>
	/// Initial order size in USD.
	/// </summary>
	public decimal BaseOrder { get => _baseOrder.Value; set => _baseOrder.Value = value; }

	/// <summary>
	/// Enable periodic DCA orders.
	/// </summary>
	public bool DcaEnabled { get => _dcaEnabled.Value; set => _dcaEnabled.Value = value; }

	/// <summary>
	/// USD amount for each DCA order.
	/// </summary>
	public decimal DcaAmount { get => _dcaAmount.Value; set => _dcaAmount.Value = value; }

	/// <summary>
	/// Interval in candles between DCA orders.
	/// </summary>
	public int DcaFrequency { get => _dcaFrequency.Value; set => _dcaFrequency.Value = value; }

	/// <summary>
	/// Enable safety orders on drawdown.
	/// </summary>
	public bool SafeOrderEnabled { get => _safeOrderEnabled.Value; set => _safeOrderEnabled.Value = value; }

	/// <summary>
	/// USD amount for first safety order.
	/// </summary>
	public decimal SafeOrder { get => _safeOrder.Value; set => _safeOrder.Value = value; }

	/// <summary>
	/// Price drop percentage to trigger safety orders.
	/// </summary>
	public decimal PriceDeviation { get => _priceDeviation.Value; set => _priceDeviation.Value = value; }

	/// <summary>
	/// Multiplier for each additional safety order.
	/// </summary>
	public decimal SafeOrderVolumeScale { get => _safeOrderVolumeScale.Value; set => _safeOrderVolumeScale.Value = value; }

	/// <summary>
	/// Distance multiplier between safety orders.
	/// </summary>
	public decimal SafeOrderStepScale { get => _safeOrderStepScale.Value; set => _safeOrderStepScale.Value = value; }

	/// <summary>
	/// Maximum number of safety orders.
	/// </summary>
	public int MaxSafeOrders { get => _maxSafeOrders.Value; set => _maxSafeOrders.Value = value; }

	/// <summary>
	/// Enable take profit logic.
	/// </summary>
	public bool TakeProfitEnable { get => _takeProfitEnable.Value; set => _takeProfitEnable.Value = value; }

	/// <summary>
	/// Target profit percentage.
	/// </summary>
	public decimal TakeProfitPercent { get => _takeProfitPercent.Value; set => _takeProfitPercent.Value = value; }

	/// <summary>
	/// Growth rate with safety orders.
	/// </summary>
	public decimal TakeProfitGrowPercent { get => _takeProfitGrowPercent.Value; set => _takeProfitGrowPercent.Value = value; }

	/// <summary>
	/// Trailing stop percent.
	/// </summary>
	public decimal TrailingPercent { get => _trailingPercent.Value; set => _trailingPercent.Value = value; }

	/// <summary>
	/// Enable start and end dates.
	/// </summary>
	public bool UseDateFilter { get => _useDateFilter.Value; set => _useDateFilter.Value = value; }

	/// <summary>
	/// Begin date for trading.
	/// </summary>
	public DateTimeOffset StartDate { get => _startDate.Value; set => _startDate.Value = value; }

	/// <summary>
	/// End date for trading.
	/// </summary>
	public DateTimeOffset EndDate { get => _endDate.Value; set => _endDate.Value = value; }

	/// <summary>
	/// Initializes parameters.
	/// </summary>
	public DcaSimulationForCryptoCommunityStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromDays(1).TimeFrame())
		.SetDisplay("Candle Type", "Candle type for calculations.", "General");

		_baseOrder = Param(nameof(BaseOrder), 100m)
		.SetDisplay("Base Order", "Initial order size in USD.", "General")
		.SetGreaterThanZero();

		_dcaEnabled = Param(nameof(DcaEnabled), true)
		.SetDisplay("DCA Enabled", "Enable periodic DCA orders.", "DCA");

		_dcaAmount = Param(nameof(DcaAmount), 10m)
		.SetDisplay("DCA Amount", "USD amount for each DCA order.", "DCA")
		.SetGreaterThanZero();

		_dcaFrequency = Param(nameof(DcaFrequency), 30)
		.SetDisplay("DCA Frequency", "Interval in candles between DCA orders.", "DCA")
		.SetGreaterThanZero();

		_safeOrderEnabled = Param(nameof(SafeOrderEnabled), false)
		.SetDisplay("Safety Orders Enabled", "Enable safety orders on drawdown.", "Safety Orders");

		_safeOrder = Param(nameof(SafeOrder), 100m)
		.SetDisplay("Safety Order", "USD amount for first safety order.", "Safety Orders")
		.SetGreaterThanZero();

		_priceDeviation = Param(nameof(PriceDeviation), 15m)
		.SetDisplay("Price Deviation %", "Price drop percentage to trigger safety orders.", "Safety Orders")
		.SetGreaterThanZero();

		_safeOrderVolumeScale = Param(nameof(SafeOrderVolumeScale), 1.6m)
		.SetDisplay("Volume Scale", "Multiplier for each additional safety order.", "Safety Orders")
		.SetGreaterThanZero();

		_safeOrderStepScale = Param(nameof(SafeOrderStepScale), 1m)
		.SetDisplay("Step Scale", "Distance multiplier between safety orders.", "Safety Orders")
		.SetGreaterThanZero();

		_maxSafeOrders = Param(nameof(MaxSafeOrders), 3000)
		.SetDisplay("Max Safety Orders", "Maximum number of safety orders.", "Safety Orders")
		.SetGreaterThanZero();

		_takeProfitEnable = Param(nameof(TakeProfitEnable), false)
		.SetDisplay("Take Profit Enabled", "Enable take profit logic.", "Exit Rules");

		_takeProfitPercent = Param(nameof(TakeProfitPercent), 1000m)
		.SetDisplay("Take Profit %", "Target profit percentage.", "Exit Rules")
		.SetGreaterThanZero();

		_takeProfitGrowPercent = Param(nameof(TakeProfitGrowPercent), 1.1m)
		.SetDisplay("Take Profit Growth", "Growth rate with safety orders.", "Exit Rules")
		.SetGreaterThanZero();

		_trailingPercent = Param(nameof(TrailingPercent), 25m)
		.SetDisplay("Trailing %", "Trailing stop percent.", "Exit Rules")
		.SetGreaterThanZero();

		_useDateFilter = Param(nameof(UseDateFilter), false)
		.SetDisplay("Use Date Filter", "Enable start and end dates.", "Date Filter");

		_startDate = Param(nameof(StartDate), new DateTimeOffset(new DateTime(2021, 11, 1)))
		.SetDisplay("Start Date", "Begin date for trading.", "Date Filter");

		_endDate = Param(nameof(EndDate), new DateTimeOffset(new DateTime(9999, 1, 1)))
		.SetDisplay("End Date", "End date for trading.", "Date Filter");
	}

	/// <inheritdoc />
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, CandleType)];
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_currentSo = 0;
		_lastHigh = 0m;
		_barIndex = 0;
		_entryBarIndex = -1;
		_nextDcaBar = -1;
		_previousHighValue = null;
		_originalTtpValue = null;

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		StartProtection();

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

		_barIndex++;

		var time = candle.OpenTime;
		if (UseDateFilter && (time < StartDate || time > EndDate))
			return;

		var price = candle.ClosePrice;

		if (Position == 0 && price > 0 && !_previousHighValue.HasValue)
			{
			var qty = BaseOrder / price;
			if (qty > 0)
				{
				BuyMarket(qty);
				_currentSo = 1;
				_lastHigh = candle.HighPrice;
				_entryBarIndex = _barIndex;
				_nextDcaBar = _entryBarIndex + DcaFrequency;
			}
			return;
		}

		if (Position <= 0)
			return;

		if (candle.HighPrice > _lastHigh)
			{
			if (_currentSo == 1)
				_lastHigh = candle.HighPrice;
			else
			{
				_currentSo = 1;
				_lastHigh = candle.HighPrice;
			}
		}

		if (DcaEnabled && _barIndex >= _nextDcaBar)
			{
			var qty = DcaAmount / price;
			if (qty > 0)
				BuyMarket(qty);

			_nextDcaBar += DcaFrequency;
		}

		decimal? threshold = null;
		if (SafeOrderEnabled && _currentSo > 0 && _currentSo <= MaxSafeOrders)
			{
			if (SafeOrderStepScale == 1m)
				threshold = _lastHigh - (_lastHigh * (PriceDeviation / 100m) * SafeOrderStepScale * _currentSo);
			else
			{
				var pow = (decimal)Math.Pow((double)SafeOrderStepScale, _currentSo);
				threshold = _lastHigh - (_lastHigh * ((PriceDeviation / 100m * pow - PriceDeviation / 100m) / (SafeOrderStepScale - 1m)));
			}
		}

		if (threshold.HasValue && price <= threshold.Value && !_previousHighValue.HasValue)
			{
			var volumeMultiplier = (decimal)Math.Pow((double)SafeOrderVolumeScale, _currentSo - 1);
			var qty = SafeOrder * volumeMultiplier / price;
			if (qty > 0)
				{
				BuyMarket(qty);
				_currentSo++;
			}
		}

		if (!TakeProfitEnable)
			return;

		var baseLevel = PositionAvgPrice * (1m + TakeProfitPercent / 100m);
		var takeProfitLevel = baseLevel + baseLevel * _currentSo * (TakeProfitGrowPercent / 100m);

		if (price >= takeProfitLevel || _previousHighValue.HasValue)
			{
			if (TrailingPercent > 0m)
				{
				if (!_previousHighValue.HasValue)
					{
					_previousHighValue = price;
					_originalTtpValue = price;
				}
				else if (price >= _previousHighValue.Value)
				{
					_previousHighValue = price;
				}
				else
				{
					var prevPerc = (_previousHighValue.Value - _originalTtpValue.Value) / _originalTtpValue.Value;
					var currPerc = (price - _originalTtpValue.Value) / _originalTtpValue.Value;
					if (prevPerc - currPerc >= TrailingPercent / 100m)
						CloseAll();
				}
			}
			else
			{
				CloseAll();
			}
		}
	}

	private void CloseAll()
	{
		ClosePosition();

		_currentSo = 0;
		_previousHighValue = null;
		_originalTtpValue = null;
		_entryBarIndex = -1;
		_nextDcaBar = -1;
	}
}
