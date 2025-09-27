namespace StockSharp.Samples.Strategies;

using System;
using System.Threading;
using System.Threading.Tasks;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public enum CheckOpenOrdersMode
{
	CheckAllTypes = 0,
	CheckOnlyBuy = 1,
	CheckOnlySell = 2,
}

/// <summary>
/// Strategy that opens a few sample trades and reports whether orders matching the configured filter remain active.
/// </summary>
public class CheckOpenOrdersStrategy : Strategy
{
	private readonly StrategyParam<decimal> _tradeVolume;
	private readonly StrategyParam<decimal> _stopLossPoints;
	private readonly StrategyParam<decimal> _takeProfitPoints;
	private readonly StrategyParam<decimal> _slippagePoints;
	private readonly StrategyParam<int> _waitTimeMilliseconds;
	private readonly StrategyParam<CheckOpenOrdersMode> _mode;

	private CancellationTokenSource _ordersCts;
	private string _modeDescription = string.Empty;
	private string _orderTypesDescription = string.Empty;
	private string _lastStatusMessage;
	private decimal _lastBidPrice;
	private decimal _lastAskPrice;
	private decimal _lastTradePrice;

	/// <summary>
	/// Initializes a new instance of the <see cref="CheckOpenOrdersStrategy"/> class.
	/// </summary>
	public CheckOpenOrdersStrategy()
	{
		_tradeVolume = Param(nameof(TradeVolume), 0.01m)
		.SetGreaterThanZero()
		.SetDisplay("Trade Volume", "Lot size used for the demonstration orders.", "Trading");

		_stopLossPoints = Param(nameof(StopLossPoints), 100m)
		.SetGreaterThanZero()
		.SetDisplay("Stop Loss (points)", "Distance in broker points for the sample protective stop order.", "Risk");

		_takeProfitPoints = Param(nameof(TakeProfitPoints), 400m)
		.SetGreaterThanZero()
		.SetDisplay("Take Profit (points)", "Distance in broker points for the sample profit target.", "Risk");

		_slippagePoints = Param(nameof(SlippagePoints), 7m)
		.SetGreaterThanZero()
		.SetDisplay("Slippage (points)", "Execution buffer expressed in broker points (informational).", "Execution");

		_waitTimeMilliseconds = Param(nameof(WaitTimeMilliseconds), 2000)
		.SetGreaterThanZero()
		.SetDisplay("Wait Time (ms)", "Delay in milliseconds between the sample orders.", "Execution");

		_mode = Param(nameof(Mode), CheckOpenOrdersMode.CheckAllTypes)
		.SetDisplay("Order Filter", "Type of market positions monitored by the status message.", "Monitoring");
	}

	/// <summary>
	/// Lot size for every sample order.
	/// </summary>
	public decimal TradeVolume
	{
		get => _tradeVolume.Value;
		set
		{
			_tradeVolume.Value = value;
			UpdateNormalizedVolume();
		}
	}

	/// <summary>
	/// Stop-loss distance in broker points.
	/// </summary>
	public decimal StopLossPoints
	{
		get => _stopLossPoints.Value;
		set => _stopLossPoints.Value = value;
	}

	/// <summary>
	/// Take-profit distance in broker points.
	/// </summary>
	public decimal TakeProfitPoints
	{
		get => _takeProfitPoints.Value;
		set => _takeProfitPoints.Value = value;
	}

	/// <summary>
	/// Slippage allowance expressed in broker points.
	/// </summary>
	public decimal SlippagePoints
	{
		get => _slippagePoints.Value;
		set
		{
			_slippagePoints.Value = value;
			UpdateSlippage();
		}
	}

	/// <summary>
	/// Delay in milliseconds between demonstration orders.
	/// </summary>
	public int WaitTimeMilliseconds
	{
		get => _waitTimeMilliseconds.Value;
		set => _waitTimeMilliseconds.Value = value;
	}

	/// <summary>
	/// Type of open orders checked by the status report.
	/// </summary>
	public CheckOpenOrdersMode Mode
	{
		get => _mode.Value;
		set
		{
			_mode.Value = value;
			UpdateModeDescription();
			UpdateStatusMessage();
		}
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		CancelPendingOperations();
		_modeDescription = string.Empty;
		_orderTypesDescription = string.Empty;
		_lastStatusMessage = null;
		_lastBidPrice = 0m;
		_lastAskPrice = 0m;
		_lastTradePrice = 0m;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		UpdateNormalizedVolume();
		UpdateSlippage();
		UpdateModeDescription();
		UpdateStatusMessage();

		SubscribeLevel1();

		_ordersCts = new CancellationTokenSource();
		_ = Task.Run(() => OpenSampleOrdersAsync(_ordersCts.Token));
	}

	/// <inheritdoc />
	protected override void OnStopping()
	{
		CancelPendingOperations();

		base.OnStopping();
	}

	/// <inheritdoc />
	protected override void OnLevel1(Security security, Level1ChangeMessage message)
	{
		base.OnLevel1(security, message);

		if (security != Security)
		return;

		if (message.TryGetDecimal(Level1Fields.BestBidPrice) is decimal bid)
		_lastBidPrice = bid;

		if (message.TryGetDecimal(Level1Fields.BestAskPrice) is decimal ask)
		_lastAskPrice = ask;

		if (message.TryGetDecimal(Level1Fields.LastTradePrice) is decimal last)
		_lastTradePrice = last;
	}

	/// <inheritdoc />
	protected override void OnNewMyTrade(MyTrade trade)
	{
		base.OnNewMyTrade(trade);

		UpdateStatusMessage();
	}

	/// <inheritdoc />
	protected override void OnPositionChanged(decimal delta)
	{
		base.OnPositionChanged(delta);

		UpdateStatusMessage();
	}

	private void UpdateNormalizedVolume()
	{
		var volume = _tradeVolume.Value;
		var security = Security;

		if (security != null)
		{
			var minVolume = security.VolumeMin ?? security.VolumeStep;
			if (minVolume.HasValue && minVolume.Value > 0m && volume < minVolume.Value)
			volume = minVolume.Value;

			var maxVolume = security.VolumeMax;
			if (maxVolume.HasValue && maxVolume.Value > 0m && volume > maxVolume.Value)
			volume = maxVolume.Value;

			var step = security.VolumeStep;
			if (step.HasValue && step.Value > 0m)
			{
				var steps = Math.Max(1m, Math.Round(volume / step.Value, MidpointRounding.AwayFromZero));
				volume = steps * step.Value;
			}
		}

		Volume = Math.Max(volume, 0m);
	}

	private void UpdateSlippage()
	{
		Slippage = GetPointDistance(_slippagePoints.Value);
	}

	private void UpdateModeDescription()
	{
		_modeDescription = Mode switch
		{
			CheckOpenOrdersMode.CheckOnlyBuy => "Checking for buy market open orders only",
			CheckOpenOrdersMode.CheckOnlySell => "Checking sell market open orders only",
			_ => "Checking all market open orders"
		};

		_orderTypesDescription = Mode switch
		{
			CheckOpenOrdersMode.CheckOnlyBuy => "buy",
			CheckOpenOrdersMode.CheckOnlySell => "sell",
			_ => "buy and sell"
		};
	}

	private void UpdateStatusMessage()
	{
		if (_modeDescription.IsEmpty())
		return;

		var hasOpenOrders = HasOpenOrders();
		var message = $"Option chosen: {_modeDescription}. Are there any current {_orderTypesDescription} open orders? {(hasOpenOrders ? "Yes" : "No")}";

		if (message == _lastStatusMessage)
		return;

		_lastStatusMessage = message;
		LogInfo(message);
	}

	private bool HasOpenOrders()
	{
		return Mode switch
		{
			CheckOpenOrdersMode.CheckOnlyBuy => Position > 0m,
			CheckOpenOrdersMode.CheckOnlySell => Position < 0m,
			_ => Position != 0m,
		};
	}

	private async Task OpenSampleOrdersAsync(CancellationToken token)
	{
		try
		{
			var security = Security;
			var portfolio = Portfolio;

			if (security == null || portfolio == null)
			{
				LogWarning("Security or portfolio is not assigned. Sample orders will not be sent.");
				return;
			}

			var volume = Volume;
			if (volume <= 0m)
			{
				LogWarning("Trade volume is not positive. Sample orders will not be sent.");
				return;
			}

			var waitDelay = TimeSpan.FromMilliseconds(Math.Max(0, WaitTimeMilliseconds));
			var stopPoints = StopLossPoints;
			var takePoints = TakeProfitPoints;

			await SendMarketOrderAsync(Sides.Buy, volume, stopPoints, takePoints, token);
			if (token.IsCancellationRequested)
			return;

			if (waitDelay > TimeSpan.Zero)
			await Task.Delay(waitDelay, token);

			await SendMarketOrderAsync(Sides.Buy, volume, stopPoints, takePoints, token);
			if (token.IsCancellationRequested)
			return;

			if (waitDelay > TimeSpan.Zero)
			await Task.Delay(waitDelay, token);

			await SendMarketOrderAsync(Sides.Sell, volume, stopPoints, takePoints, token);
		}
		catch (TaskCanceledException)
		{
			// Cancellation requested; no action required.
		}
		catch (Exception error)
		{
			LogWarning("Sample order task failed: {0}", error.Message);
		}
	}

	private async Task SendMarketOrderAsync(Sides side, decimal volume, decimal stopPoints, decimal takePoints, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		return;

		var action = side == Sides.Buy ? "buy" : "sell";
		LogInfo("Sending sample {0} order with volume {1:0.#####}.", action, volume);

		if (side == Sides.Buy)
		BuyMarket(volume);
		else
		SellMarket(volume);

		var referencePrice = GetReferencePrice(side);
		if (referencePrice <= 0m)
		{
			LogWarning("Reference price for protective orders is unavailable.");
			return;
		}

		ApplyProtections(side, referencePrice, volume, stopPoints, takePoints);

		await Task.CompletedTask;
	}

	private void ApplyProtections(Sides side, decimal referencePrice, decimal volume, decimal stopPoints, decimal takePoints)
	{
		var resultingPosition = side == Sides.Buy ? Position + volume : Position - volume;

		if (stopPoints > 0m)
			SetStopLoss(stopPoints, referencePrice, resultingPosition);

		if (takePoints > 0m)
			SetTakeProfit(takePoints, referencePrice, resultingPosition);
	}

	private decimal GetReferencePrice(Sides side)
	{
		if (_lastTradePrice > 0m)
		return _lastTradePrice;

		if (side == Sides.Buy)
		return _lastAskPrice > 0m ? _lastAskPrice : _lastBidPrice;

		return _lastBidPrice > 0m ? _lastBidPrice : _lastAskPrice;
	}

	private decimal GetPointDistance(decimal points)
	{
		var step = Security?.PriceStep ?? 0m;
		return step > 0m ? points * step : points;
	}

	private void CancelPendingOperations()
	{
		if (_ordersCts == null)
		return;

		_ordersCts.Cancel();
		_ordersCts.Dispose();
		_ordersCts = null;
	}
}
