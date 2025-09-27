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

using StockSharp;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Manual keyboard trading strategy converted from the MetaTrader eKeyboardTrader expert advisor.
/// The conversion exposes keyboard actions (buy, sell, close) as boolean parameters and mirrors
/// the protective stop and take-profit distance controls using StockSharp's high-level API.
/// </summary>
public class EKeyboardTraderStrategy : Strategy
{
	private static readonly TimeSpan CommandCooldown = TimeSpan.FromSeconds(1);

	private readonly StrategyParam<decimal> _orderVolume;
	private readonly StrategyParam<int> _stopLossPoints;
	private readonly StrategyParam<int> _takeProfitPoints;
	private readonly StrategyParam<int> _slippagePoints;
	private readonly StrategyParam<bool> _buyRequest;
	private readonly StrategyParam<bool> _sellRequest;
	private readonly StrategyParam<bool> _closeRequest;

	private decimal? _bestBidPrice;
	private decimal? _bestAskPrice;
	private decimal _pointValue;
	private bool _protectionStarted;
	private DateTimeOffset _lastCommandTime;

	/// <summary>
	/// Base volume used for manual market orders.
	/// </summary>
	public decimal OrderVolume
	{
		get => _orderVolume.Value;
		set => _orderVolume.Value = value;
	}

	/// <summary>
	/// Distance to the stop loss expressed in MetaTrader points.
	/// </summary>
	public int StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Distance to the take profit expressed in MetaTrader points.
	/// </summary>
	public int TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Allowed slippage for manual market orders (for logging purposes).
	/// </summary>
	public int SlippagePoints
	{
		get => _slippagePoints.Value;
		set => _slippagePoints.Value = value;
	}

	/// <summary>
	/// Set to true to trigger a manual market buy order.
	/// </summary>
	public bool BuyRequest
	{
		get => _buyRequest.Value;
		set => _buyRequest.Value = value;
	}

	/// <summary>
	/// Set to true to trigger a manual market sell order.
	/// </summary>
	public bool SellRequest
	{
		get => _sellRequest.Value;
		set => _sellRequest.Value = value;
	}

	/// <summary>
	/// Set to true to close the entire position at market price.
	/// </summary>
	public bool CloseRequest
	{
		get => _closeRequest.Value;
		set => _closeRequest.Value = value;
	}

	public EKeyboardTraderStrategy()
	{
		_orderVolume = Param(nameof(OrderVolume), 1m)
			.SetGreaterThanZero()
			.SetDisplay("Order Volume", "Volume submitted when manual market requests are executed.", "Manual Controls");

		_stopLossPoints = Param(nameof(StopLossPoints), 0)
			.SetNotNegative()
			.SetDisplay("Stop Loss (points)", "Protective stop loss distance expressed in MetaTrader points.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 0)
			.SetNotNegative()
			.SetDisplay("Take Profit (points)", "Protective take profit distance expressed in MetaTrader points.", "Risk");

		_slippagePoints = Param(nameof(SlippagePoints), 0)
			.SetNotNegative()
			.SetDisplay("Slippage (points)", "Maximum tolerated slippage used for informational logging.", "Manual Controls")
			.SetCanOptimize(false);

		_buyRequest = Param(nameof(BuyRequest), false)
			.SetDisplay("Buy Request", "Set to true to send a market buy order (auto reset).", "Manual Controls")
			.SetCanOptimize(false);

		_sellRequest = Param(nameof(SellRequest), false)
			.SetDisplay("Sell Request", "Set to true to send a market sell order (auto reset).", "Manual Controls")
			.SetCanOptimize(false);

		_closeRequest = Param(nameof(CloseRequest), false)
			.SetDisplay("Close Request", "Set to true to close the entire position (auto reset).", "Manual Controls")
			.SetCanOptimize(false);
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return [(Security, DataType.Level1)];
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_bestBidPrice = null;
		_bestAskPrice = null;
		_pointValue = 0m;
		_protectionStarted = false;
		_lastCommandTime = default;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		if (Security == null)
			throw new InvalidOperationException("Security is not specified.");

		if (Portfolio == null)
			throw new InvalidOperationException("Portfolio is not specified.");

		_pointValue = CalculatePointValue();

		SubscribeLevel1()
			.Bind(ProcessLevel1)
			.Start();
	}

	private decimal CalculatePointValue()
	{
		if (Security?.PriceStep is decimal step && step > 0m)
			return step;

		if (Security?.Step is decimal legacyStep && legacyStep > 0m)
			return legacyStep;

		return 0m;
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
			_bestBidPrice = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
			_bestAskPrice = ask;

		ProcessManualCommands();
	}

	private void ProcessManualCommands()
	{
		var buyRequested = BuyRequest;
		var sellRequested = SellRequest;
		var closeRequested = CloseRequest;

		if (!buyRequested && !sellRequested && !closeRequested)
			return;

		if (!IsOnline)
			return;

		if (Security == null || Portfolio == null)
			return;

		var now = CurrentTime;
		if (_lastCommandTime != default && now - _lastCommandTime < CommandCooldown)
			return;

		var executed = false;

		if (buyRequested)
		{
			if (_bestAskPrice == null)
				return;

			BuyMarket(OrderVolume);
			LogInfo($"Manual buy request sent. Approximate ask price: {_bestAskPrice:0.#####}. Slippage tolerance: {SlippagePoints} points.");
			BuyRequest = false;
			executed = true;
		}

		if (sellRequested)
		{
			if (_bestBidPrice == null)
				return;

			SellMarket(OrderVolume);
			LogInfo($"Manual sell request sent. Approximate bid price: {_bestBidPrice:0.#####}. Slippage tolerance: {SlippagePoints} points.");
			SellRequest = false;
			executed = true;
		}

		if (closeRequested)
		{
			ClosePosition();
			LogInfo("Manual close request sent. Flattening the current position.");
			CloseRequest = false;
			executed = true;
		}

		if (executed)
		{
			_lastCommandTime = now;
			EnsureProtectionConfigured();
		}
	}

	private void EnsureProtectionConfigured()
	{
		if (_protectionStarted)
			return;

		var stopUnit = CreateProtectionUnit(StopLossPoints);
		var takeUnit = CreateProtectionUnit(TakeProfitPoints);

		if (stopUnit == null && takeUnit == null)
			return;

		StartProtection(
			takeProfit: takeUnit,
			stopLoss: stopUnit,
			useMarketOrders: true,
			isStopTrailing: false);

		_protectionStarted = true;
	}

	private Unit CreateProtectionUnit(int distancePoints)
	{
		if (distancePoints <= 0)
			return null;

		if (_pointValue <= 0m)
			return null;

		return new Unit(distancePoints * _pointValue, UnitTypes.Absolute);
	}
}

