using System;
using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Strategy that repeatedly modifies a pending or protective order to measure broker spread and execution latency.
/// </summary>
public class CheckExecutionStrategy : Strategy
{
	private readonly StrategyParam<int> _iterations;
	private readonly StrategyParam<CheckExecutionOrderType> _orderMode;
	private readonly StrategyParam<decimal> _pendingOffset;
	private readonly StrategyParam<decimal> _stopLossOffset;

	private readonly SimpleMovingAverage _spreadAverage = new();
	private readonly SimpleMovingAverage _executionAverage = new();

	private Order _pendingOrder;
	private Order _stopLossOrder;
	private bool _awaitingResponse;
	private DateTimeOffset? _lastRequestTime;
	private int _completedIterations;
	private decimal? _averageSpread;
	private decimal? _averageExecution;

	public CheckExecutionStrategy()
	{

		_iterations = Param(nameof(Iterations), 30)
		.SetDisplay("Iterations", "Number of modify attempts (1-500).", "General")
		.SetCanOptimize(true);

		_orderMode = Param(nameof(OrderMode), CheckExecutionOrderType.Pending)
		.SetDisplay("Order Mode", "Select pending or market order workflow.", "General");

		_pendingOffset = Param(nameof(PendingOffset), 100m)
		.SetDisplay("Pending Offset", "Distance in price steps above the ask for the test pending order.", "General")
		.SetCanOptimize(true);

		_stopLossOffset = Param(nameof(StopLossOffset), 100m)
		.SetDisplay("Stop Offset", "Distance in price steps below the ask for the protective stop order.", "General")
		.SetCanOptimize(true);

		UpdateIndicatorLength();
	}


	public int Iterations
	{
		get => _iterations.Value;
		set
		{
			_iterations.Value = value;
			UpdateIndicatorLength();
		}
	}

	public CheckExecutionOrderType OrderMode
	{
		get => _orderMode.Value;
		set => _orderMode.Value = value;
	}

	public decimal PendingOffset
	{
		get => _pendingOffset.Value;
		set => _pendingOffset.Value = value;
	}

	public decimal StopLossOffset
	{
		get => _stopLossOffset.Value;
		set => _stopLossOffset.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		ResetState();
		UpdateIndicatorLength();

		StartProtection();

		SubscribeLevel1(Security)
		.Bind(ProcessLevel1)
		.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		CancelOrderIfActive(ref _pendingOrder);
		CancelOrderIfActive(ref _stopLossOrder);

		base.OnStopped();
	}

	private void ProcessLevel1(Level1ChangeMessage message)
	{
		var ask = message.TryGetDecimal(Level1Fields.AskPrice);
		var bid = message.TryGetDecimal(Level1Fields.BidPrice);

		if (ask is not decimal askPrice || bid is not decimal bidPrice)
		return;


		var priceStep = GetPriceStep();
		if (priceStep <= 0m)
		return;

		// Update rolling spread statistics using the latest best bid/ask quote.
		var spreadInSteps = (askPrice - bidPrice) / priceStep;
		if (spreadInSteps > 0m)
		{
			var spreadValue = _spreadAverage.Process(message.ServerTime, spreadInSteps);
			if (spreadValue.IsFinal && spreadValue.GetValue<decimal>() is decimal spreadAvg)
			_averageSpread = Math.Round(spreadAvg, 4);
		}

		if (_completedIterations >= Iterations)
		return;

		if (!IsFormedAndOnlineAndAllowTrading())
		return;

		if (OrderMode == CheckExecutionOrderType.Pending)
		{
			HandlePendingWorkflow(askPrice, priceStep);
		}
		else
		{
			HandleMarketWorkflow(askPrice, priceStep);
		}
	}

	private void HandlePendingWorkflow(decimal askPrice, decimal priceStep)
	{
		if (_pendingOrder == null)
		{
			var volume = NormalizeVolume(Volume);
			if (volume <= 0m)
			return;

			var pendingPrice = NormalizePrice(askPrice + PendingOffset * priceStep);
			if (pendingPrice <= 0m)
			return;

			// Place the initial pending order to start the latency measurement sequence.
			_pendingOrder = BuyStop(volume, pendingPrice);
			return;
		}

		if (_pendingOrder.State == OrderStates.Done ||
		_pendingOrder.State == OrderStates.Failed ||
		_pendingOrder.State == OrderStates.Cancelled)
		{
			_pendingOrder = null;
			return;
		}

		if (_awaitingResponse)
		return;

		var targetPrice = NormalizePrice(askPrice + PendingOffset * priceStep);
		var currentPrice = _pendingOrder.Price ?? 0m;

		if (targetPrice <= 0m || Math.Abs(currentPrice - targetPrice) < priceStep / 2m)
		return;

		// Re-register the pending order at the new price and start the execution timer.
		_lastRequestTime = CurrentTime;
		_awaitingResponse = true;
		ReRegisterOrder(_pendingOrder, targetPrice, _pendingOrder.Volume ?? NormalizeVolume(Volume));
	}

	private void HandleMarketWorkflow(decimal askPrice, decimal priceStep)
	{
		var normalizedVolume = NormalizeVolume(Volume);
		if (normalizedVolume <= 0m)
		return;

		if (_pendingOrder == null)
		{
			// Submit the market entry order once and wait for fills before managing the protective stop.
			_pendingOrder = BuyMarket(normalizedVolume);
			return;
		}

		if (_pendingOrder.State == OrderStates.Done)
		{
			_pendingOrder = null;
		}
		else if (_pendingOrder.State == OrderStates.Failed ||
		_pendingOrder.State == OrderStates.Cancelled)
		{
			_pendingOrder = null;
			return;
		}

		if (Position <= 0m)
		return;

		var stopPrice = NormalizePrice(askPrice - StopLossOffset * priceStep);
		if (stopPrice <= 0m)
		return;

		if (_stopLossOrder == null)
		{
			// Create the protective stop order so it can be repeatedly updated for latency checks.
			_stopLossOrder = SellStop(Math.Abs(Position), stopPrice);
			_lastRequestTime = CurrentTime;
			_awaitingResponse = true;
			return;
		}

		if (_stopLossOrder.State == OrderStates.Done ||
		_stopLossOrder.State == OrderStates.Failed ||
		_stopLossOrder.State == OrderStates.Cancelled)
		{
			_stopLossOrder = null;
			return;
		}

		if (_awaitingResponse)
		return;

		var currentStopPrice = _stopLossOrder.Price ?? 0m;
		if (Math.Abs(currentStopPrice - stopPrice) < priceStep / 2m)
		return;

		// Re-register the stop order to the latest offset from the ask price.
		_lastRequestTime = CurrentTime;
		_awaitingResponse = true;
		ReRegisterOrder(_stopLossOrder, stopPrice, _stopLossOrder.Volume ?? Math.Abs(Position));
	}

	/// <inheritdoc />
	protected override void OnOrderChanged(Order order)
	{
		base.OnOrderChanged(order);

		if (order == null)
		return;

		if (order == _pendingOrder)
		{
			if (OrderMode == CheckExecutionOrderType.Market && order.State == OrderStates.Done)
			{
				_pendingOrder = null;
			}
			else if (order.State == OrderStates.Failed || order.State == OrderStates.Cancelled)
			{
				_pendingOrder = null;
			}
		}
		else if (order == _stopLossOrder)
		{
			if (order.State == OrderStates.Done ||
			order.State == OrderStates.Failed ||
			order.State == OrderStates.Cancelled)
			{
				_stopLossOrder = null;
			}
		}

		if (!_awaitingResponse || _lastRequestTime is null)
		return;

		if (order != _pendingOrder && order != _stopLossOrder)
		return;

		if (order.State != OrderStates.Active && order.State != OrderStates.Done)
		return;

		var completionTime = order.LastChangeTime ?? order.Time ?? CurrentTime;
		var delay = (decimal)(completionTime - _lastRequestTime.Value).TotalMilliseconds;
		if (delay < 0m)
		delay = 0m;

		var executionValue = _executionAverage.Process(completionTime, delay);
		if (executionValue.IsFinal && executionValue.GetValue<decimal>() is decimal executionAvg)
		_averageExecution = Math.Round(executionAvg, 2);

		_completedIterations++;
		_awaitingResponse = false;
		_lastRequestTime = null;

		if (_completedIterations >= Iterations)
		FinalizeCheck();
	}

	private void FinalizeCheck()
	{
		if (OrderMode == CheckExecutionOrderType.Pending)
		{
			CancelOrderIfActive(ref _pendingOrder);
		}
		else
		{
			CancelOrderIfActive(ref _stopLossOrder);

			var position = Position;
			if (position > 0m)
			{
				SellMarket(position);
			}
		}

		var spreadText = _averageSpread?.ToString("0.####") ?? "n/a";
		var executionText = _averageExecution?.ToString("0.##") ?? "n/a";

		LogInfo($"Check completed. Average spread: {spreadText} steps. Average execution: {executionText} ms. Iterations: {_completedIterations}.");
	}

	private decimal NormalizePrice(decimal price)
	{
		var step = GetPriceStep();
		if (step <= 0m)
		return price;

		var scaled = price / step;
		scaled = Math.Round(scaled, MidpointRounding.AwayFromZero);
		return scaled * step;
	}

	private decimal NormalizeVolume(decimal volume)
	{
		var security = Security;
		if (security == null)
		return volume;

		var step = security.VolumeStep ?? 0m;
		var min = security.MinVolume ?? 0m;
		var max = security.MaxVolume ?? 0m;

		var normalized = volume;
		if (step > 0m)
		normalized = Math.Round(normalized / step, MidpointRounding.AwayFromZero) * step;

		if (min > 0m && normalized < min)
		normalized = min;

		if (max > 0m && normalized > max)
		normalized = max;

		return normalized;
	}

	private decimal GetPriceStep()
	{
		var security = Security;
		var step = security?.PriceStep ?? 0m;
		return step > 0m ? step : 0.0001m;
	}

	private void CancelOrderIfActive(ref Order order)
	{
		var current = order;
		if (current == null)
		return;

		if (current.State == OrderStates.Active || current.State == OrderStates.Pending)
		CancelOrder(current);

		order = null;
	}

	private void ResetState()
	{
		_pendingOrder = null;
		_stopLossOrder = null;
		_completedIterations = 0;
		_awaitingResponse = false;
		_lastRequestTime = null;
		_averageSpread = null;
		_averageExecution = null;
		_spreadAverage.Reset();
		_executionAverage.Reset();
	}

	private void UpdateIndicatorLength()
	{
		var length = Math.Max(1, Math.Min(500, _iterations.Value));
		if (_iterations.Value != length)
		_iterations.Value = length;

		_spreadAverage.Length = length;
		_executionAverage.Length = length;
	}
}

public enum CheckExecutionOrderType
{
	Pending,
	Market
}
