namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Adaptive moving average and RSI based averaging strategy.
/// Replicates the behaviour of the AMA Trader MetaTrader expert using the high level API.
/// </summary>
public class AmaTraderStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<decimal> _lotSize;
	private readonly StrategyParam<int> _rsiLength;
	private readonly StrategyParam<int> _stepLength;
	private readonly StrategyParam<decimal> _rsiLevelUp;
	private readonly StrategyParam<decimal> _rsiLevelDown;
	private readonly StrategyParam<int> _amaLength;
	private readonly StrategyParam<int> _amaFastPeriod;
	private readonly StrategyParam<int> _amaSlowPeriod;
	private readonly StrategyParam<decimal> _profitTarget;
	private readonly StrategyParam<decimal> _withdrawalAmount;

	private readonly Queue<decimal> _rsiValues = new();

	private RSI _rsi = null!;
	private KaufmanAdaptiveMovingAverage _ama = null!;

	private decimal _longVolume;
	private decimal _shortVolume;
	private decimal _longAveragePrice;
	private decimal _shortAveragePrice;
	private decimal _lastWithdrawalPnL;

	/// <summary>
	/// Initializes a new instance of the <see cref="AmaTraderStrategy"/> class.
	/// </summary>
	public AmaTraderStrategy()
	{
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
		.SetDisplay("Candle Type", "Timeframe for indicator calculations.", "General");

		_lotSize = Param(nameof(LotSize), 0.1m)
		.SetDisplay("Lot Size", "Order volume used for each entry.", "Trading")
		.SetGreaterThanZero();

		_rsiLength = Param(nameof(RsiLength), 14)
		.SetDisplay("RSI Length", "Number of periods for the RSI oscillator.", "RSI")
		.SetGreaterThanZero();

		_stepLength = Param(nameof(StepLength), 3)
		.SetDisplay("Step Length", "Number of recent RSI values required for confirmation (0 uses only the latest value).", "RSI");

		_rsiLevelUp = Param(nameof(RsiLevelUp), 70m)
		.SetDisplay("RSI Upper Level", "Overbought threshold for RSI.", "RSI");

		_rsiLevelDown = Param(nameof(RsiLevelDown), 30m)
		.SetDisplay("RSI Lower Level", "Oversold threshold for RSI.", "RSI");

		_amaLength = Param(nameof(AmaLength), 9)
		.SetDisplay("AMA Length", "Smoothing period for the adaptive moving average.", "AMA")
		.SetGreaterThanZero();

		_amaFastPeriod = Param(nameof(AmaFastPeriod), 2)
		.SetDisplay("AMA Fast Period", "Fast smoothing constant length.", "AMA")
		.SetGreaterThanZero();

		_amaSlowPeriod = Param(nameof(AmaSlowPeriod), 30)
		.SetDisplay("AMA Slow Period", "Slow smoothing constant length.", "AMA")
		.SetGreaterThanZero();

		_profitTarget = Param(nameof(ProfitTarget), 50m)
		.SetDisplay("Profit Target", "Close all positions when unrealized profit exceeds this value (0 disables).", "Risk");

		_withdrawalAmount = Param(nameof(WithdrawalAmount), 1000m)
		.SetDisplay("Withdrawal Amount", "Close all positions when realized profit grows by this value (0 disables).", "Risk");
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
	/// Volume used for each averaging order.
	/// </summary>
	public decimal LotSize
	{
		get => _lotSize.Value;
		set => _lotSize.Value = value;
	}

	/// <summary>
	/// RSI calculation period.
	/// </summary>
	public int RsiLength
	{
		get => _rsiLength.Value;
		set => _rsiLength.Value = value;
	}

	/// <summary>
	/// Number of RSI values to inspect.
	/// </summary>
	public int StepLength
	{
		get => _stepLength.Value;
		set => _stepLength.Value = value;
	}

	/// <summary>
	/// RSI overbought level.
	/// </summary>
	public decimal RsiLevelUp
	{
		get => _rsiLevelUp.Value;
		set => _rsiLevelUp.Value = value;
	}

	/// <summary>
	/// RSI oversold level.
	/// </summary>
	public decimal RsiLevelDown
	{
		get => _rsiLevelDown.Value;
		set => _rsiLevelDown.Value = value;
	}

	/// <summary>
	/// AMA smoothing length.
	/// </summary>
	public int AmaLength
	{
		get => _amaLength.Value;
		set => _amaLength.Value = value;
	}

	/// <summary>
	/// AMA fast smoothing constant.
	/// </summary>
	public int AmaFastPeriod
	{
		get => _amaFastPeriod.Value;
		set => _amaFastPeriod.Value = value;
	}

	/// <summary>
	/// AMA slow smoothing constant.
	/// </summary>
	public int AmaSlowPeriod
	{
		get => _amaSlowPeriod.Value;
		set => _amaSlowPeriod.Value = value;
	}

	/// <summary>
	/// Unrealized profit level that closes all positions.
	/// </summary>
	public decimal ProfitTarget
	{
		get => _profitTarget.Value;
		set => _profitTarget.Value = value;
	}

	/// <summary>
	/// Realized profit step that triggers a reset.
	/// </summary>
	public decimal WithdrawalAmount
	{
		get => _withdrawalAmount.Value;
		set => _withdrawalAmount.Value = value;
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

		_rsiValues.Clear();
		_longVolume = 0m;
		_shortVolume = 0m;
		_longAveragePrice = 0m;
		_shortAveragePrice = 0m;
		_lastWithdrawalPnL = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		Volume = LotSize;
		_lastWithdrawalPnL = PnL;

		_rsi = new RSI
		{
			Length = RsiLength
		};

		_ama = new KaufmanAdaptiveMovingAverage
		{
			Length = AmaLength,
			FastSCPeriod = AmaFastPeriod,
			SlowSCPeriod = AmaSlowPeriod
		};

		var subscription = SubscribeCandles(CandleType);
		subscription
			.Bind(_rsi, _ama, ProcessCandle)
			.Start();

		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, _ama);
			DrawIndicator(area, _rsi);
			DrawOwnTrades(area);
		}
	}

	private void ProcessCandle(ICandleMessage candle, decimal rsiValue, decimal amaValue)
	{
		if (candle.State != CandleStates.Finished)
			return;

		if (!IsFormedAndOnlineAndAllowTrading())
			return;

		Volume = LotSize;

		UpdateRsiValues(rsiValue);

		var currentPrice = candle.ClosePrice;

		if (ProfitTarget > 0m)
		{
			var openPnL = GetLongUnrealizedPnL(currentPrice) + GetShortUnrealizedPnL(currentPrice);
			if (openPnL >= ProfitTarget)
			{
				CloseAllPositions();
				return;
			}
		}

		if (WithdrawalAmount > 0m)
		{
			var realizedPnL = PnL;
			if (realizedPnL - _lastWithdrawalPnL >= WithdrawalAmount)
			{
				CloseAllPositions();
				_lastWithdrawalPnL = realizedPnL;
				return;
			}
		}

		var stepLength = Math.Max(1, StepLength);
		var oversold = false;
		var overbought = false;

		var inspected = 0;
		foreach (var value in _rsiValues)
		{
			if (value < RsiLevelDown)
				oversold = true;
			if (value > RsiLevelUp)
				overbought = true;

			inspected++;
			if (inspected >= stepLength)
				break;
		}

		if (currentPrice > amaValue && oversold)
		{
			var longPnL = GetLongUnrealizedPnL(currentPrice);
			if (longPnL < 0m)
				BuyMarket();
			BuyMarket();
			return;
		}

		if (currentPrice < amaValue && overbought)
		{
			var shortPnL = GetShortUnrealizedPnL(currentPrice);
			if (shortPnL < 0m)
				SellMarket();
			SellMarket();
		}
	}

	private void UpdateRsiValues(decimal rsiValue)
	{
		var stepLength = Math.Max(1, StepLength);

		_rsiValues.Enqueue(rsiValue);

		while (_rsiValues.Count > stepLength)
			_rsiValues.Dequeue();
	}

	private decimal GetLongUnrealizedPnL(decimal currentPrice)
	{
		return _longVolume <= 0m ? 0m : _longVolume * (currentPrice - _longAveragePrice);
	}

	private decimal GetShortUnrealizedPnL(decimal currentPrice)
	{
		return _shortVolume <= 0m ? 0m : _shortVolume * (_shortAveragePrice - currentPrice);
	}

	private void CloseAllPositions()
	{
		if (_longVolume > 0m)
			SellMarket(_longVolume);

		if (_shortVolume > 0m)
			BuyMarket(_shortVolume);
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		if (trade.Order == null || trade.Trade.Security != Security)
			return;

		var volume = trade.Trade.Volume;
		var price = trade.Trade.Price;

		if (trade.Order.Side == Sides.Buy)
		{
			if (_shortVolume > 0m)
			{
				var closingVolume = Math.Min(_shortVolume, volume);
				_shortVolume -= closingVolume;
				volume -= closingVolume;

				if (_shortVolume <= 0m)
				{
					_shortVolume = 0m;
					_shortAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _longVolume + volume;
				_longAveragePrice = newVolume == 0m ? 0m : (_longAveragePrice * _longVolume + price * volume) / newVolume;
				_longVolume = newVolume;
			}
		}
		else if (trade.Order.Side == Sides.Sell)
		{
			if (_longVolume > 0m)
			{
				var closingVolume = Math.Min(_longVolume, volume);
				_longVolume -= closingVolume;
				volume -= closingVolume;

				if (_longVolume <= 0m)
				{
					_longVolume = 0m;
					_longAveragePrice = 0m;
				}
			}

			if (volume > 0m)
			{
				var newVolume = _shortVolume + volume;
				_shortAveragePrice = newVolume == 0m ? 0m : (_shortAveragePrice * _shortVolume + price * volume) / newVolume;
				_shortVolume = newVolume;
			}
		}
	}
}
