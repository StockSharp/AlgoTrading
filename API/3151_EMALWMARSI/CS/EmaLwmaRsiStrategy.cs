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
/// Exponential and linear weighted moving average crossover confirmed by RSI.
/// </summary>
public class EmaLwmaRsiStrategy : Strategy
{
	private readonly StrategyParam<int> _stopLossPips;
	private readonly StrategyParam<int> _takeProfitPips;
	private readonly StrategyParam<int> _emaPeriod;
	private readonly StrategyParam<int> _lwmaPeriod;
	private readonly StrategyParam<int> _maShift;
	private readonly StrategyParam<int> _rsiPeriod;
	private readonly StrategyParam<AppliedPriceTypes> _maAppliedPrice;
	private readonly StrategyParam<AppliedPriceTypes> _rsiAppliedPrice;
	private readonly StrategyParam<DataType> _candleType;

	private ExponentialMovingAverage _ema;
	private WeightedMovingAverage _lwma;
	private RelativeStrengthIndex _rsi;
	private Shift _emaShiftIndicator;
	private Shift _lwmaShiftIndicator;

	private decimal? _previousEma;
	private decimal? _previousLwma;
	private decimal _pipSize;

	private bool _pendingBuy;
	private bool _pendingSell;
	private bool _orderInFlight;

	public EmaLwmaRsiStrategy()
	{
		_stopLossPips = Param(nameof(StopLossPips), 150)
			.SetNotNegative()
			.SetDisplay("Stop Loss (pips)", "Protective stop distance in pips. Zero disables the stop.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 300, 10);

		_takeProfitPips = Param(nameof(TakeProfitPips), 150)
			.SetNotNegative()
			.SetDisplay("Take Profit (pips)", "Target distance in pips. Zero disables the target.", "Risk")
			.SetCanOptimize(true)
			.SetOptimize(10, 300, 10);

		_emaPeriod = Param(nameof(EmaPeriod), 28)
			.SetGreaterThanZero()
			.SetDisplay("EMA period", "Length of the exponential moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 100, 1);

		_lwmaPeriod = Param(nameof(LwmaPeriod), 8)
			.SetGreaterThanZero()
			.SetDisplay("LWMA period", "Length of the linear weighted moving average.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(3, 50, 1);

		_maShift = Param(nameof(MaShift), 0)
			.SetNotNegative()
			.SetDisplay("MA shift", "Forward shift applied to both moving averages (bars).", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(0, 5, 1);

		_rsiPeriod = Param(nameof(RsiPeriod), 14)
			.SetGreaterThanZero()
			.SetDisplay("RSI period", "Number of bars for the RSI smoothing window.", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(5, 50, 1);

		_maAppliedPrice = Param(nameof(MaAppliedPrice), AppliedPriceTypes.Weighted)
			.SetDisplay("MA applied price", "Candle price forwarded to EMA and LWMA.", "Indicators");

		_rsiAppliedPrice = Param(nameof(RsiAppliedPrice), AppliedPriceTypes.Weighted)
			.SetDisplay("RSI applied price", "Price source used by the RSI.", "Indicators");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(1).TimeFrame())
			.SetDisplay("Candle type", "Type of candles analyzed by the strategy.", "General");
	}

	public int StopLossPips
	{
		get => _stopLossPips.Value;
		set => _stopLossPips.Value = value;
	}

	public int TakeProfitPips
	{
		get => _takeProfitPips.Value;
		set => _takeProfitPips.Value = value;
	}

	public int EmaPeriod
	{
		get => _emaPeriod.Value;
		set => _emaPeriod.Value = value;
	}

	public int LwmaPeriod
	{
		get => _lwmaPeriod.Value;
		set => _lwmaPeriod.Value = value;
	}

	public int MaShift
	{
		get => _maShift.Value;
		set => _maShift.Value = value;
	}

	public int RsiPeriod
	{
		get => _rsiPeriod.Value;
		set => _rsiPeriod.Value = value;
	}

	public AppliedPriceTypes MaAppliedPrice
	{
		get => _maAppliedPrice.Value;
		set => _maAppliedPrice.Value = value;
	}

	public AppliedPriceTypes RsiAppliedPrice
	{
		get => _rsiAppliedPrice.Value;
		set => _rsiAppliedPrice.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		yield return (Security, CandleType);
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_ema = null;
		_lwma = null;
		_rsi = null;
		_emaShiftIndicator = null;
		_lwmaShiftIndicator = null;
		_previousEma = null;
		_previousLwma = null;
		_pipSize = 0m;
		_pendingBuy = false;
		_pendingSell = false;
		_orderInFlight = false;
	}

	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_ema = new ExponentialMovingAverage { Length = EmaPeriod };
		_lwma = new WeightedMovingAverage { Length = LwmaPeriod };
		_rsi = new RelativeStrengthIndex { Length = RsiPeriod };
		_emaShiftIndicator = MaShift > 0 ? new Shift { Length = MaShift } : null;
		_lwmaShiftIndicator = MaShift > 0 ? new Shift { Length = MaShift } : null;
		_previousEma = null;
		_previousLwma = null;

		_pipSize = CalculatePipSize();

		var stopLossUnit = StopLossPips > 0 ? new Unit(StopLossPips * _pipSize, UnitTypes.Point) : null;
		var takeProfitUnit = TakeProfitPips > 0 ? new Unit(TakeProfitPips * _pipSize, UnitTypes.Point) : null;

		if (stopLossUnit != null || takeProfitUnit != null)
			StartProtection(takeProfit: takeProfitUnit, stopLoss: stopLossUnit);
		else
			StartProtection();

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(ProcessCandle)
			.Start();

		var priceArea = CreateChartArea();
		if (priceArea != null)
		{
			DrawCandles(priceArea, subscription);
			if (_ema != null)
				DrawIndicator(priceArea, _ema);
			if (_lwma != null)
				DrawIndicator(priceArea, _lwma);
			DrawOwnTrades(priceArea);
		}

		if (_rsi != null)
		{
			var rsiArea = CreateChartArea();
			if (rsiArea != null)
				DrawIndicator(rsiArea, _rsi);
		}
	}

	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		_orderInFlight = false;

		if (_pendingBuy || _pendingSell)
			ProcessPendingOrders();
	}

	protected override void OnOrderFailed(Order order, OrderFail fail)
	{
		base.OnOrderFailed(order, fail);

		_orderInFlight = false;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (_ema == null || _lwma == null || _rsi == null)
			return;

		var maPrice = GetAppliedPrice(candle, MaAppliedPrice);
		var rsiPrice = GetAppliedPrice(candle, RsiAppliedPrice);
		var time = candle.OpenTime;

		var emaValue = _ema.Process(maPrice, time, true).ToDecimal();
		var lwmaValue = _lwma.Process(maPrice, time, true).ToDecimal();
		var rsiValue = _rsi.Process(rsiPrice, time, true).ToDecimal();

		if (!_ema.IsFormed || !_lwma.IsFormed || !_rsi.IsFormed)
			return;

		if (_emaShiftIndicator != null)
		{
			emaValue = _emaShiftIndicator.Process(emaValue, time, true).ToDecimal();
			if (!_emaShiftIndicator.IsFormed)
				return;
		}

		if (_lwmaShiftIndicator != null)
		{
			lwmaValue = _lwmaShiftIndicator.Process(lwmaValue, time, true).ToDecimal();
			if (!_lwmaShiftIndicator.IsFormed)
				return;
		}

		var buySignal = false;
		var sellSignal = false;

		if (_previousEma.HasValue && _previousLwma.HasValue)
		{
			// Detect EMA crossing above LWMA with RSI confirmation.
			buySignal = emaValue < lwmaValue && _previousEma.Value > _previousLwma.Value && rsiValue > 50m;

			// Detect EMA crossing below LWMA with RSI confirmation.
			sellSignal = emaValue > lwmaValue && _previousEma.Value < _previousLwma.Value && rsiValue < 50m;
		}

		_previousEma = emaValue;
		_previousLwma = lwmaValue;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		if (buySignal)
		{
			_pendingBuy = true;
			_pendingSell = false;
		}
		else if (sellSignal)
		{
			_pendingSell = true;
			_pendingBuy = false;
		}

		ProcessPendingOrders();
	}

	private void ProcessPendingOrders()
	{
		if (!_pendingBuy && !_pendingSell)
			return;

		if (_orderInFlight)
			return;

		var volume = Volume;
		if (volume <= 0m)
			return;

		if (_pendingBuy)
		{
			if (Position < 0m)
			{
				// Close the short position before reversing to long.
				ClosePosition();
				_orderInFlight = true;
				return;
			}

			if (Position == 0m)
			{
				// Enter the new long position once flat.
				BuyMarket(volume);
				_orderInFlight = true;
				_pendingBuy = false;
			}
			else
			{
				_pendingBuy = false;
			}

			return;
		}

		if (_pendingSell)
		{
			if (Position > 0m)
			{
				// Close the long position before reversing to short.
				ClosePosition();
				_orderInFlight = true;
				return;
			}

			if (Position == 0m)
			{
				// Enter the new short position once flat.
				SellMarket(volume);
				_orderInFlight = true;
				_pendingSell = false;
			}
			else
			{
				_pendingSell = false;
			}
		}
	}

	private decimal CalculatePipSize()
	{
		var security = Security;
		if (security == null)
			return 1m;

		var step = security.PriceStep ?? 1m;
		var multiplier = security.Decimals is 3 or 5 ? 10m : 1m;
		return step * multiplier;
	}

	private static decimal GetAppliedPrice(ICandleMessage candle, AppliedPriceTypes priceType)
	{
		return priceType switch
		{
			AppliedPriceTypes.Close => candle.ClosePrice,
			AppliedPriceTypes.Open => candle.OpenPrice,
			AppliedPriceTypes.High => candle.HighPrice,
			AppliedPriceTypes.Low => candle.LowPrice,
			AppliedPriceTypes.Median => (candle.HighPrice + candle.LowPrice) / 2m,
			AppliedPriceTypes.Typical => (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3m,
			AppliedPriceTypes.Weighted => (candle.HighPrice + candle.LowPrice + 2m * candle.ClosePrice) / 4m,
			AppliedPriceTypes.Average => (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4m,
			_ => candle.ClosePrice,
		};
	}

	public enum AppliedPriceTypes
	{
		Close,
		Open,
		High,
		Low,
		Median,
		Typical,
		Weighted,
		Average,
	}
}

