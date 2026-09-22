using System;
using System.Collections.Generic;

using Ecng.Common;

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
	private const uint _initialRandomState = 3710u;

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

	private uint _randomState;
	private decimal? _trailingStopPrice;
	private bool _openLongNext;
	private decimal _entryPrice;
	private decimal? _bestBid;
	private decimal? _bestAsk;
	private decimal _priceStep;
	private decimal _stepPrice;

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
		return [(Security, CandleType), (Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();
		ResetRuntimeState();
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		if (MaxVolume < MinVolume)
			throw new InvalidOperationException($"{nameof(MaxVolume)} cannot be less than {nameof(MinVolume)}.");

		ResetRuntimeState();
		UpdateInstrumentValues();

		SubscribeCandles(CandleType)
			.Bind(ProcessCandle)
			.Start();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private void ResetRuntimeState()
	{
		_randomState = _initialRandomState;
		_trailingStopPrice = null;
		_openLongNext = true;
		_entryPrice = 0m;
		_bestBid = null;
		_bestAsk = null;
		_priceStep = 0m;
		_stepPrice = 0m;
	}

	private void UpdateInstrumentValues()
	{
		_priceStep = Security?.PriceStep ?? 0m;
		_stepPrice = 0m;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid && bid > 0m)
			_bestBid = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask && ask > 0m)
			_bestAsk = ask;

		if (message.TryGetDecimal(Level1Fields.PriceStep) is decimal priceStep && priceStep > 0m)
			_priceStep = priceStep;

		if (message.TryGetDecimal(Level1Fields.StepPrice) is decimal stepPrice && stepPrice > 0m)
			_stepPrice = stepPrice;
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		var price = candle.ClosePrice;

		if (ApplyProtection(price) || ApplyTrailing(price) || ApplyRiskControl(price))
			return;

		TryOpenTrade();
	}

	private bool ApplyProtection(decimal marketPrice)
	{
		if (Position == 0)
			return false;

		var priceStep = GetPriceStep();

		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
			return false;

		if (Position > 0)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = entryPrice - StopLossPoints * priceStep;
				if (marketPrice <= stopPrice)
					return ClosePosition("Stop loss");
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entryPrice + TakeProfitPoints * priceStep;
				if (marketPrice >= takePrice)
					return ClosePosition("Take profit");
			}
		}
		else if (Position < 0)
		{
			if (StopLossPoints > 0m)
			{
				var stopPrice = entryPrice + StopLossPoints * priceStep;
				if (marketPrice >= stopPrice)
					return ClosePosition("Stop loss");
			}

			if (TakeProfitPoints > 0m)
			{
				var takePrice = entryPrice - TakeProfitPoints * priceStep;
				if (marketPrice <= takePrice)
					return ClosePosition("Take profit");
			}
		}

		return false;
	}

	private bool ApplyTrailing(decimal marketPrice)
	{
		if (Position == 0 || TrailingGapPoints <= 0m || TrailingStartPoints <= 0m)
		{
			_trailingStopPrice = null;
			return false;
		}

		var priceStep = GetPriceStep();

		var entryPrice = _entryPrice;
		if (entryPrice <= 0m)
			return false;

		var gap = TrailingGapPoints * priceStep;
		var triggerDistance = (TrailingStartPoints + TrailingGapPoints) * priceStep;

		if (Position > 0)
		{
			var profit = marketPrice - entryPrice;
			if (profit >= triggerDistance)
			{
				var candidate = marketPrice - gap;
				if (_trailingStopPrice == null || candidate > _trailingStopPrice)
					_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && marketPrice <= _trailingStopPrice)
				return ClosePosition("Trailing stop");
		}
		else if (Position < 0)
		{
			var profit = entryPrice - marketPrice;
			if (profit >= triggerDistance)
			{
				var candidate = marketPrice + gap;
				if (_trailingStopPrice == null || candidate < _trailingStopPrice)
					_trailingStopPrice = candidate;
			}

			if (_trailingStopPrice != null && marketPrice >= _trailingStopPrice)
				return ClosePosition("Trailing stop");
		}

		return false;
	}

	private bool ApplyRiskControl(decimal marketPrice)
	{
		if (Position == 0m || _entryPrice <= 0m)
			return false;

		var riskLimit = GetRiskLimit();
		if (riskLimit is null)
			return false;

		var liquidationPrice = Position > 0m ? _bestBid ?? marketPrice : _bestAsk ?? marketPrice;
		var floatingPnL = CalculateFloatingPnL(liquidationPrice);

		return floatingPnL <= -riskLimit.Value && ClosePosition("Risk control");
	}

	private decimal? GetRiskLimit()
	{
		var risk = Math.Abs(RiskValue);

		if (MoneyRiskMode == RiskModes.FixedMoney)
			return risk;

		var portfolioValue = Portfolio?.CurrentValue ?? Portfolio?.BeginValue ?? 0m;
		return portfolioValue > 0m ? portfolioValue * risk / 100m : null;
	}

	private decimal CalculateFloatingPnL(decimal marketPrice)
	{
		var direction = Position > 0m ? 1m : -1m;
		var difference = (marketPrice - _entryPrice) * direction;
		var volume = Math.Abs(Position);

		if (_stepPrice > 0m)
			return difference / GetPriceStep() * _stepPrice * volume;

		return difference * (Security?.Multiplier ?? 1m) * volume;
	}

	/// <inheritdoc />
	protected override void OnOwnTradeReceived(MyTrade trade)
	{
		base.OnOwnTradeReceived(trade);

		if (Position != 0m && _entryPrice == 0m)
			_entryPrice = trade.Trade.Price;

		if (Position == 0m)
		{
			_entryPrice = 0m;
			_trailingStopPrice = null;
		}
	}

	private bool ClosePosition(string reason)
	{
		var volume = Math.Abs(Position);
		if (volume <= 0m)
			return false;

		SubmitMarket(Position > 0m ? Sides.Sell : Sides.Buy, volume, reason);
		_trailingStopPrice = null;
		return true;
	}

	private void TryOpenTrade()
	{
		if (Position != 0m || !IsSpreadAllowed())
			return;

		Sides? side = null;

		if (Mode == TradingModes.DoubleSide)
		{
			side = _openLongNext ? Sides.Buy : Sides.Sell;
			_openLongNext = !_openLongNext;
		}
		else if (Mode == TradingModes.OneSide)
		{
			var randomValue = NextRandomInt(6);
			side = randomValue switch
			{
				1 or 4 => Sides.Buy,
				0 or 3 => Sides.Sell,
				_ => null,
			};
		}

		if (side is null)
			return;

		var volume = GenerateVolume();
		if (volume <= 0m)
			return;

		SubmitMarket(side.Value, volume, null);
	}

	private bool IsSpreadAllowed()
	{
		if (MaxSpreadPoints <= 0m)
			return false;
		if (_bestBid is not decimal bid || _bestAsk is not decimal ask)
			return true;
		if (ask < bid)
			return false;

		return (ask - bid) / GetPriceStep() <= MaxSpreadPoints;
	}

	private decimal GenerateVolume()
	{
		var min = Math.Max(MinVolume, Security?.MinVolume ?? 0m);
		var max = Math.Min(MaxVolume, Security?.MaxVolume ?? decimal.MaxValue);
		if (max < min)
			return 0m;

		var raw = min == max ? min : min + (max - min) * (decimal)NextRandomUnit();
		var step = Security?.VolumeStep ?? 0m;
		if (step <= 0m)
			return raw;

		var first = Math.Ceiling(min / step) * step;
		var last = Math.Floor(max / step) * step;
		if (first > last)
			return 0m;

		var aligned = Math.Floor(raw / step) * step;
		return Math.Clamp(aligned, first, last);
	}

	private decimal GetPriceStep()
		=> _priceStep > 0m ? _priceStep : 0.0001m;

	private double NextRandomUnit()
	{
		_randomState = unchecked(_randomState * 1664525u + 1013904223u);
		return _randomState / 4294967296d;
	}

	private int NextRandomInt(int maxExclusive)
		=> (int)(NextRandomUnit() * maxExclusive);

	private void SubmitMarket(Sides side, decimal volume, string reason)
	{
		var order = CreateOrder(side, 0m, volume);
		order.Comment = string.IsNullOrWhiteSpace(reason)
			? TradeComment
			: string.IsNullOrWhiteSpace(TradeComment) ? reason : $"{TradeComment}: {reason}";
		RegisterOrder(order);
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

