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

using StockSharp.Algo;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Port of the MetaTrader expert "FT CCI MA" that combines a weighted moving average filter with CCI thresholds and a trading window.
/// </summary>
public class FtCciMaStrategy : Strategy
{
	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<decimal> _stopLossPips;
	private readonly StrategyParam<decimal> _takeProfitPips;
	private readonly StrategyParam<bool> _useTimeFilter;
	private readonly StrategyParam<int> _startHour;
	private readonly StrategyParam<int> _endHour;
	private readonly StrategyParam<int> _cciPeriod;
	private readonly StrategyParam<decimal> _cciLevelUp;
	private readonly StrategyParam<decimal> _cciLevelDown;
	private readonly StrategyParam<decimal> _cciLevelBuy;
	private readonly StrategyParam<decimal> _cciLevelSell;
	private readonly StrategyParam<int> _maPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<DataType> _candleType;

	private CommodityChannelIndex _cci = null!;
	private WeightedMovingAverage _ma = null!;

	private decimal _pipSize;
	private readonly List<decimal> _maHistory = new();

	/// <summary>
	/// Initializes a new instance of <see cref="FtCciMaStrategy"/> with parameters mirroring the original EA inputs.
	/// </summary>
	public FtCciMaStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Trade size expressed in lots", "Trading")
			.SetCanOptimize(true);

		_stopLossPips = Param(nameof(StopLossPips), 150m)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips", "Risk")
			.SetCanOptimize(true);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150m)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Profit target distance in pips", "Risk")
			.SetCanOptimize(true);

		_useTimeFilter = Param(nameof(UseTimeFilter), true)
			.SetDisplay("Use Time Filter", "Enable the intraday trading window", "Trading");

		_startHour = Param(nameof(StartHour), 10)
			.SetDisplay("Start Hour", "Hour (0-23) when entries are allowed", "Trading")
			.SetRange(0, 23);

		_endHour = Param(nameof(EndHour), 5)
			.SetDisplay("End Hour", "Hour (0-23) when entries stop", "Trading")
			.SetRange(0, 23);

		_cciPeriod = Param(nameof(CciPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("CCI Period", "Averaging period for the Commodity Channel Index", "Indicators")
			.SetCanOptimize(true);

		_cciLevelUp = Param(nameof(CciLevelUp), 200m)
			.SetDisplay("CCI Upper Level", "Upper overbought threshold used with MA confirmation", "Indicators")
			.SetCanOptimize(true);

		_cciLevelDown = Param(nameof(CciLevelDown), -200m)
			.SetDisplay("CCI Lower Level", "Lower oversold threshold used with MA confirmation", "Indicators")
			.SetCanOptimize(true);

		_cciLevelBuy = Param(nameof(CciLevelBuy), -100m)
			.SetDisplay("CCI Buy Level", "Soft oversold threshold when price is above the MA", "Indicators")
			.SetCanOptimize(true);

		_cciLevelSell = Param(nameof(CciLevelSell), 100m)
			.SetDisplay("CCI Sell Level", "Soft overbought threshold when price is below the MA", "Indicators")
			.SetCanOptimize(true);

		_maPeriod = Param(nameof(MaPeriod), 200)
			.SetGreaterThanZero()
			.SetDisplay("MA Period", "Length of the weighted moving average", "Indicators")
			.SetCanOptimize(true);

		_maShift = Param(nameof(MaShift), 0)
			.SetDisplay("MA Shift", "Forward shift applied to the moving average line", "Indicators")
			.SetRange(0, 100);

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle Type", "Primary timeframe used for calculations", "General");
	}

	/// <summary>
	/// Base order volume expressed in lots.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Stop-loss distance in pips.
	/// </summary>
	public decimal StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	/// <summary>
	/// Take-profit distance in pips.
	/// </summary>
	public decimal TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	/// <summary>
	/// Enables the intraday time filter.
	/// </summary>
	public bool UseTimeFilter
	{
		get => _useTimeFilter.Value;
		set => _useTimeFilter.Value = value;
	}

	/// <summary>
	/// Start hour (0-23) of the trading session.
	/// </summary>
	public int StartHour
	{
		get => _startHour.Value;
		set => _startHour.Value = value;
	}

	/// <summary>
	/// End hour (0-23) of the trading session.
	/// </summary>
	public int EndHour
	{
		get => _endHour.Value;
		set => _endHour.Value = value;
	}

	/// <summary>
	/// Commodity Channel Index averaging period.
	/// </summary>
	public int CciPeriod
	{
		get => _cciPeriod.Value;
		set => _cciPeriod.Value = value;
	}

	/// <summary>
	/// Upper CCI threshold used for aggressive shorts.
	/// </summary>
	public decimal CciLevelUp
	{
		get => _cciLevelUp.Value;
		set => _cciLevelUp.Value = value;
	}

	/// <summary>
	/// Lower CCI threshold used for aggressive longs.
	/// </summary>
	public decimal CciLevelDown
	{
		get => _cciLevelDown.Value;
		set => _cciLevelDown.Value = value;
	}

	/// <summary>
	/// CCI threshold when price trades above the moving average.
	/// </summary>
	public decimal CciLevelBuy
	{
		get => _cciLevelBuy.Value;
		set => _cciLevelBuy.Value = value;
	}

	/// <summary>
	/// CCI threshold when price trades below the moving average.
	/// </summary>
	public decimal CciLevelSell
	{
		get => _cciLevelSell.Value;
		set => _cciLevelSell.Value = value;
	}

	/// <summary>
	/// Weighted moving average length.
	/// </summary>
	public int MaPeriod
	{
		get => _maPeriod.Value;
		set => _maPeriod.Value = value;
	}

	/// <summary>
	/// Forward shift applied to the moving average.
	/// </summary>
	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	/// <summary>
	/// Candle type used for indicator calculations.
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

		_pipSize = 0m;
		_maHistory.Clear();
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		InitializePipSize();

		_cci = new CommodityChannelIndex
		{
			Length = CciPeriod,
			CandlePrice = CandlePrice.Typical,
		};

		_ma = new WeightedMovingAverage
		{
			Length = MaPeriod,
			CandlePrice = CandlePrice.Weighted,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_cci, _ma, ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			DrawIndicator(priceArea, _ma);
			DrawOwnTrades(priceArea);
		}

		var cciArea = CreateChartArea("CCI");
		if (cciArea != null)
		{
			DrawIndicator(cciArea, _cci);
		}

		var stopLossUnit = StopLossPips > 0m && _pipSize > 0m
			? new Unit(StopLossPips * _pipSize, UnitTypes.Absolute)
			: null;

		var takeProfitUnit = TakeProfitPips > 0m && _pipSize > 0m
			? new Unit(TakeProfitPips * _pipSize, UnitTypes.Absolute)
			: null;

		if (stopLossUnit != null || takeProfitUnit != null)
		{
			StartProtection(stopLoss: stopLossUnit, takeProfit: takeProfitUnit);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal cciValue, decimal maValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		UpdateMaHistory(maValue);

		if (!_cci.IsFormed || !_ma.IsFormed)
			return;

		var shiftedMa = GetShiftedMa();
		if (shiftedMa is null)
			return;

		if (UseTimeFilter && !IsWithinTradingWindow(candle.CloseTime))
		{
			// Close any active exposure outside of the session.
			if (Position != 0m)
			{
				CancelActiveOrders();
				ClosePosition();
			}

			return;
		}

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		var closePrice = candle.ClosePrice;

		var allowLong = (closePrice > shiftedMa && cciValue < CciLevelBuy)
			|| (closePrice < shiftedMa && cciValue < CciLevelDown);

		if (allowLong && Position <= 0m)
		{
			EnterLong();
			return;
		}

		var allowShort = (closePrice < shiftedMa && cciValue > CciLevelSell)
			|| (closePrice > shiftedMa && cciValue > CciLevelUp);

		if (allowShort && Position >= 0m)
		{
			EnterShort();
		}
	}

	private void EnterLong()
	{
		var volume = OrderVolume + Math.Max(0m, -Position);
		if (volume <= 0m)
			return;

		// Cancel opposite orders before reversing the position.
		CancelActiveOrders();
		BuyMarket(volume);
	}

	private void EnterShort()
	{
		var volume = OrderVolume + Math.Max(0m, Position);
		if (volume <= 0m)
			return;

		// Cancel opposite orders before reversing the position.
		CancelActiveOrders();
		SellMarket(volume);
	}

	private void InitializePipSize()
	{
		_pipSize = 0m;

		var priceStep = Security?.PriceStep;
		if (priceStep != null)
		{
			_pipSize = priceStep.Value;

			// MetaTrader counts a pip as 0.0001 for 5-digit quotes, adjust accordingly.
			if (Security?.Decimals is 3 or 5)
			{
				_pipSize *= 10m;
			}
		}

		if (_pipSize <= 0m)
			_pipSize = 1m;
	}

	private void UpdateMaHistory(decimal maValue)
	{
		_maHistory.Add(maValue);

		var maxSize = Math.Max(2, MaShift + 2);
		if (_maHistory.Count > maxSize)
		{
			var removeCount = _maHistory.Count - maxSize;
			_maHistory.RemoveRange(0, removeCount);
		}
	}

	private decimal? GetShiftedMa()
	{
		if (_maHistory.Count == 0)
			return null;

		var shift = Math.Max(0, MaShift);
		if (shift >= _maHistory.Count)
			return null;

		var index = _maHistory.Count - 1 - shift;
		if (index < 0)
			return null;

		return _maHistory[index];
	}

	private bool IsWithinTradingWindow(DateTimeOffset time)
	{
		var start = Math.Clamp(StartHour, 0, 23);
		var end = Math.Clamp(EndHour, 0, 23);
		var hour = time.Hour;

		if (start == end)
			return false;

		if (start < end)
			return hour >= start && hour < end;

		return hour >= start || hour < end;
	}
}

