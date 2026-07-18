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
/// Randomized trading strategy converted from the "RRS Randomness in Nature" MQL expert advisor.
/// The strategy opens random market orders with optional trailing, stop-loss, take-profit and risk protection.
/// </summary>
public class RrsRandomnessStrategy : Strategy
{
	private readonly StrategyParam<TradingModes> _tradingMode;
	private readonly StrategyParam<decimal> _minVolume;
	private readonly StrategyParam<decimal> _maxVolume;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _trailingStartPoints;
	private readonly StrategyParam<decimal> _trailingGapPoints;
	private readonly StrategyParam<decimal> _maxSpreadPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<RiskModes> _riskMode;
	private readonly StrategyParam<decimal> _riskValue;
	private readonly StrategyParam<string> _tradeComment;
	private readonly StrategyParam<DataType> _candleType;

	private int _tradeCounter;
	private decimal? _trailingStopPrice;
	private bool _openLongNext;
	private decimal _entryPrice;

	/// <summary>
	/// Trading direction selection logic.
	/// </summary>
	public TradingModes Mode
	{
		get => _tradingMode.Value;
		set => _tradingMode.Value = value;
	}

	/// <summary>
	/// Minimal order volume.
	/// </summary>
	public decimal MinVolume
	{
		get => _minVolume.Value;
		set => _minVolume.Value = value;
	}

	/// <summary>
	/// Maximal order volume.
	/// </summary>
	public decimal MaxVolume
	{
		get => _maxVolume.Value;
		set => _maxVolume.Value = value;
	}

	/// <summary>
	/// Take-profit distance expressed in price steps.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Stop-loss distance expressed in price steps.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Profit distance that enables the trailing stop.
	/// </summary>
	public decimal TrailingStartPoints
	{
		get => _trailingStartPoints.Value;
		set => _trailingStartPoints.Value = value;
	}

	/// <summary>
	/// Trailing stop offset from current price measured in price steps.
	/// </summary>
	public decimal TrailingGapPoints
	{
		get => _trailingGapPoints.Value;
		set => _trailingGapPoints.Value = value;
	}

	/// <summary>
	/// Maximal spread allowed for opening trades (price steps).
	/// </summary>
	public decimal MaxSpreadPoints
	{
		get => _maxSpreadPoints.Value;
		set => _maxSpreadPoints.Value = value;
	}

	/// <summary>
	/// Slippage tolerance in price steps (informational parameter).
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Risk management mode.
	/// </summary>
	public RiskModes MoneyRiskMode
	{
		get => _riskMode.Value;
		set => _riskMode.Value = value;
	}

	/// <summary>
	/// Risk value in account currency or percent depending on the mode.
	/// </summary>
	public decimal RiskValue
	{
		get => _riskValue.Value;
		set => _riskValue.Value = value;
	}

	/// <summary>
	/// Trade comment stored for informational purposes.
	/// </summary>
	public string TradeComment
	{
		get => _tradeComment.Value;
		set => _tradeComment.Value = value;
	}

	/// <summary>
	/// Candle type used to schedule strategy checks.
	/// </summary>
	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RrsRandomnessStrategy"/> class.
	/// </summary>
	public RrsRandomnessStrategy()
	{
		_tradingMode = Param(nameof(Mode), TradingModes.DoubleSide)
			.SetDisplay("Trading Mode", "Select whether a trade is chosen every cycle or only on random matches.", "General");

		_minVolume = Param(nameof(MinVolume), 0.01m)
			.SetGreaterThanZero()
			.SetDisplay("Min Volume", "Minimal volume for a market order.", "Lot Settings");

		_maxVolume = Param(nameof(MaxVolume), 0.5m)
			.SetGreaterThanZero()
			.SetDisplay("Max Volume", "Maximum volume for a market order.", "Lot Settings");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 2000m)
			.SetNotNegative()
			.SetDisplay("Take Profit", "Take-profit distance in price steps.", "Protection");

		_stopLossPoints = Param(nameof(StopLossPoints), 3000m)
			.SetNotNegative()
			.SetDisplay("Stop Loss", "Stop-loss distance in price steps.", "Protection");

		_trailingStartPoints = Param(nameof(TrailingStartPoints), 1500m)
			.SetNotNegative()
			.SetDisplay("Trailing Start", "Profit distance that enables the trailing stop.", "Protection");

		_trailingGapPoints = Param(nameof(TrailingGapPoints), 1000m)
			.SetNotNegative()
			.SetDisplay("Trailing Gap", "Offset between current price and trailing stop.", "Protection");

		_maxSpreadPoints = Param(nameof(MaxSpreadPoints), 100m)
			.SetNotNegative()
			.SetDisplay("Max Spread", "Maximum spread allowed for new trades (price steps).", "Filters");

		_slippagePoints = Param(nameof(SlippagePoints), 3m)
			.SetNotNegative()
			.SetDisplay("Slippage", "Expected slippage in price steps. Used for reference only.", "Filters");

		_riskMode = Param(nameof(MoneyRiskMode), RiskModes.BalancePercentage)
			.SetDisplay("Risk Mode", "Choose whether risk is fixed or percentage based.", "Risk Management");

		_riskValue = Param(nameof(RiskValue), 5m)
			.SetNotNegative()
			.SetDisplay("Risk Value", "Risk amount in currency or percent.", "Risk Management");

		_tradeComment = Param(nameof(TradeComment), "RRS")
			.SetDisplay("Trade Comment", "Informational comment attached to generated orders.", "General");

		_candleType = Param(nameof(CandleType), TimeSpan.FromHours(4).TimeFrame())
			.SetDisplay("Candle Type", "Candle type used to trigger the strategy logic.", "General");
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

		_tradeCounter = 0;
		_trailingStopPrice = null;
		_openLongNext = true;
		_entryPrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		ApplyProtection(price);
		ApplyTrailing(price);
		TryOpenTrade();
	}

	private void ApplyProtection(decimal marketPrice)
	{
		if (Position == 0)
			return;

		var priceStep = Security.PriceStep ?? 0.0001m;
		if (priceStep <= 0m)
			priceStep = 0.0001m;

		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
			return;

		if (Position > 0)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = entryPrice - StopLossPoints * priceStep;
				if (marketPrice <= stopPrice)
				{
					SellMarket(Math.Abs(Position));
					_trailingStopPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entryPrice + TakeProfitPoints * priceStep;
				if (marketPrice >= takePrice)
				{
					SellMarket(Math.Abs(Position));
					_trailingStopPrice = null;
				}
			}
		}
		else if (Position < 0)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = entryPrice + StopLossPoints * priceStep;
				if (marketPrice >= stopPrice)
				{
					BuyMarket(Math.Abs(Position));
					_trailingStopPrice = null;
					return;
				}
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entryPrice - TakeProfitPoints * priceStep;
				if (marketPrice <= takePrice)
				{
					BuyMarket(Math.Abs(Position));
					_trailingStopPrice = null;
				}
			}
		}
	}

	private void ApplyTrailing(decimal marketPrice)
	{
		if (Position == 0 || TrailingGapPoints <= 0m || TrailingStartPoints <= 0m)
			return;

		var priceStep = Security.PriceStep ?? 0m;
		if (priceStep <= 0m)
			return;

		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
			return;

		var gap = TrailingGapPoints * priceStep;
		var triggerDistance = (TrailingStartPoints + TrailingGapPoints) * priceStep;

		if (Position > 0)
		{
			var profit = marketPrice - entryPrice;
			if (profit > triggerDistance)
			{
				var candidate = marketPrice - gap;
				if (_trailingStopPrice == null || candidate > _trailingStopPrice)
					_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && marketPrice <= _trailingStopPrice)
			{
				SellMarket(Math.Abs(Position));
				_trailingStopPrice = null;
			}
		}
		else if (Position < 0)
		{
			var profit = entryPrice - marketPrice;
			if (profit > triggerDistance)
			{
				var candidate = marketPrice + gap;
				if (_trailingStopPrice == null || candidate < _trailingStopPrice)
					_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && marketPrice >= _trailingStopPrice)
			{
				BuyMarket(Math.Abs(Position));
				_trailingStopPrice = null;
			}
		}
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;

		if (Position == 0m)
			_entryPrice = 0m;
	}

	private void ClosePosition()
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return;

		if (Position > 0)
			SellMarket(volume);
		else if (Position < 0)
			BuyMarket(volume);
	}

	private void TryOpenTrade()
	{
		if (Position != 0)
			return;

		if (_openLongNext)
		{
			BuyMarket(Volume);
		}
		else
		{
			SellMarket(Volume);
		}

		_openLongNext = !_openLongNext;
		_tradeCounter++;
	}

	/// <summary>
	/// Trading mode options.
	/// </summary>
	public enum TradingModes
	{
		/// <summary>
		/// Alternate between long and short entries every cycle.
		/// </summary>
		DoubleSide,

		/// <summary>
		/// Enter only when the random generator matches specific values.
		/// </summary>
		OneSide,
	}

	/// <summary>
	/// Risk management configuration.
	/// </summary>
	public enum RiskModes
	{
		/// <summary>
		/// Risk is defined as a fixed currency value.
		/// </summary>
		FixedMoney,

		/// <summary>
		/// Risk is calculated as a percentage of the portfolio value.
		/// </summary>
		BalancePercentage,
	}
}

