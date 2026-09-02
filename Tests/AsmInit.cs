namespace StockSharp.Tests;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Logging;
using Ecng.Serialization;
using Ecng.UnitTesting;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Compilation;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

[TestClass]
public static class AsmInit
{
	private const int _defaultPostTradeBars = 3;
	private static readonly TimeSpan _replayTimeout = TimeSpan.FromSeconds(60);
	private static readonly TimeSpan _minPostTradeHorizon = TimeSpan.FromMinutes(5);
	private static readonly TimeSpan _maxPostTradeHorizon = TimeSpan.FromDays(3);

	private readonly static MarketDataStorageCache _cache = new();
	private static LogManager _logManager;

	public static Security Security1 { get; private set; }
	public static Security Security2 { get; private set; }

	[AssemblyInitialize]
	public static async Task Init(TestContext _)
	{
		_logManager = new();
		_logManager.Listeners.Add(new ConsoleLogListener());

		await CompilationExtensions.Init(Paths.FileSystem, _logManager.Application, [], default);

		var drive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);
		var storageRegistry = new StorageRegistry { DefaultDrive = drive };

		SecurityId[] secIds = [Paths.HistoryDefaultSecurity.ToSecurityId(), Paths.HistoryDefaultSecurity2.ToSecurityId()];
		var dts = (await secIds.ToAsyncEnumerable().SelectMany(id => drive.GetAvailableDataTypesAsync(id, StorageFormats.Binary)).ToListAsync())
			.Where(dt => dt.IsTFCandles || dt == DataType.Level1)
			.Distinct()
			.ToArray();
		var days = Paths.HistoryBeginDate.Range(Paths.HistoryEndDate, TimeSpan.FromDays(1)).ToArray();

		foreach (var day in days)
		{
			foreach (var secId in secIds)
			{
				foreach (var dt in dts)
				{
					await foreach (var msg in _cache.GetMessagesAsync(secId, dt, day, date => storageRegistry.GetStorage(secId, dt).LoadAsync(date)))
					{
					}
				}
			}
		}

		var secId1 = Paths.HistoryDefaultSecurity;
		Security1 = new Security { Id = secId1 };

		var secId2 = Paths.HistoryDefaultSecurity2;
		Security2 = new Security { Id = secId2 };

		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000000m;

		ConfigManager.RegisterService<ISecurityProvider>(new CollectionSecurityProvider([Security1, Security2]));
		ConfigManager.RegisterService<IPortfolioProvider>(new CollectionPortfolioProvider([pf]));
	}

	public static async Task RunStrategy<T>(T strategy, Action<T, Security> extra = null, TimeSpan? postTradeHorizon = null, TimeSpan? replayDuration = null)
		where T : Strategy
	{
		if (postTradeHorizon is { } requestedPostTradeHorizon && requestedPostTradeHorizon <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(postTradeHorizon), postTradeHorizon, "Post-trade horizon must be positive.");
		if (replayDuration is { } requestedReplayDuration && requestedReplayDuration <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(replayDuration), replayDuration, "Replay duration must be positive.");

		var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath) };

		var startTime = Paths.HistoryBeginDate;
		// StopDate is inclusive. Paths.HistoryEndDate points to midnight, so use the
		// end of that date to replay all messages from the last packaged day.
		var availableStopTime = Paths.HistoryEndDate.Date.AddDays(1).AddTicks(-1);
		var requestedStopTime = replayDuration is { } duration ? startTime.Add(duration) : availableStopTime;
		var stopTime = requestedStopTime < availableStopTime ? requestedStopTime : availableStopTime;

		var security1 = new Security { Id = Paths.HistoryDefaultSecurity };
		var security2 = new Security { Id = Paths.HistoryDefaultSecurity2 };
		var portfolio = Portfolio.CreateSimulator();
		portfolio.CurrentValue = 1000000m;

		var securityProvider = new CollectionSecurityProvider([security1, security2]);
		var portfolioProvider = new CollectionPortfolioProvider([portfolio]);

		using var connector = new HistoryEmulationConnector(securityProvider, portfolioProvider, storageRegistry)
		{
			// The harness owns shutdown ordering. Avoid a connector-side unsubscribe sweep
			// racing the strategy's own StopAsync teardown.
			IsAutoUnSubscribeOnDisconnect = false,
			StopOnSubscriptionError = false,
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
				StorageCache = _cache,
			}
		};

		strategy.Portfolio = portfolio;
		strategy.Security = security1;
		strategy.Connector = connector;
		strategy.Volume = 1;
		strategy.WaitRulesOnStop = false;
		extra?.Invoke(strategy, security2);
		var cancelOrdersWhenStopping = strategy.CancelOrdersWhenStopping;

		var clone = strategy.TypedClone();

		var sync = new object();
		Exception error = null;
		var orders = new HashSet<long>();
		var orderStates = new List<string>();
		var tradeStates = new List<string>();
		var tradesCount = 0;
		var orderRegisterFailures = new List<string>();
		var orderCancelFailures = new List<string>();
		var orderEditFailures = new List<string>();
		var massOrderCancelFailures = new List<string>();
		var subscriptionFailures = new List<string>();
		var orderRegisterFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var orderCancelFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var orderEditFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var massOrderCancelFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var subscriptionFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var connectorFailures = new List<string>();
		var connectorFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var cleanupFailures = new List<string>();
		var cleanupFailureKeys = new HashSet<string>(StringComparer.Ordinal);
		var orderRegisterFailureCount = 0;
		var orderCancelFailureCount = 0;
		var orderEditFailureCount = 0;
		var massOrderCancelFailureCount = 0;
		var subscriptionFailureCount = 0;
		var connectorFailureCount = 0;
		var cleanupFailureCount = 0;
		TimeSpan? nativeCandleTimeFrame = null;
		TimeSpan? actualPostTradeHorizon = null;
		DateTime? coverageTime = null;
		DateTime? postTradeTarget = null;
		DateTime? postTradeReachedTime = null;
		var shutdownRequested = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		var strategyStopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var connectorSuspended = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var connectorStopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var harnessShutdownStarted = false;
		var connectorTerminalInitiatedShutdown = false;
		DateTime? connectorTerminalBoundaryTime = null;
		ChannelStates? connectorTerminalBoundaryState = null;
		var strategyStoppedBeforeConnector = false;
		string shutdownReason = null;

		TimeSpan resolvePostTradeHorizon()
		{
			if (postTradeHorizon is { } requested)
				return requested;

			var candleTimeFrame = nativeCandleTimeFrame ?? TimeSpan.FromMinutes(1);
			var ticks = candleTimeFrame.Ticks > _maxPostTradeHorizon.Ticks / _defaultPostTradeBars
				? _maxPostTradeHorizon.Ticks
				: candleTimeFrame.Ticks * _defaultPostTradeBars;

			return TimeSpan.FromTicks(Math.Clamp(ticks, _minPostTradeHorizon.Ticks, _maxPostTradeHorizon.Ticks));
		}

		void tryCompleteCoverage()
		{
			if (orders.Count > 0 && tradesCount > 0 && coverageTime is null)
			{
				coverageTime = connector.CurrentTime;
				actualPostTradeHorizon = resolvePostTradeHorizon();
				postTradeTarget = coverageTime.Value.Add(actualPostTradeHorizon.Value);

				if (connector.CurrentTime >= postTradeTarget)
				{
					postTradeReachedTime = connector.CurrentTime;
				}
			}
		}

		static string describeSubscription(Subscription subscription)
			=> subscription is null
				? "subscription=<unknown>"
				: $"subscription=#{subscription.TransactionId} {subscription.DataType}, security={subscription.SecurityId}";

		static string describeException(Exception failure)
		{
			if (failure is null)
				return "<unknown>";

			const int maxLength = 4000;
			var text = failure.ToString();
			return text.Length <= maxLength ? text : $"{text[..maxLength]}... <truncated>";
		}

		string describeOrderFailure(Subscription subscription, OrderFail fail)
		{
			var order = fail?.Order;
			var unformedIndicators = strategy.Indicators.Count(indicator => !indicator.IsFormed);

			return $"{describeSubscription(subscription)}, orderTx={order?.TransactionId}, failTx={fail?.TransactionId}, " +
				$"orderSecurity={order?.Security?.Id ?? "<null>"}, portfolio={order?.Portfolio?.Name ?? "<null>"}, " +
				$"side={order?.Side}, type={order?.Type}, price={order?.Price}, volume={order?.Volume}, state={order?.State}, " +
				$"formed={strategy.IsFormed}, online={strategy.IsOnline}, unformedIndicators={unformedIndicators}/{strategy.Indicators.Count}, " +
				$"time={fail?.ServerTime:O}, error={fail?.Error?.Message ?? fail?.ToString() ?? "<unknown>"}";
		}

		bool requestShutdown(string reason, bool connectorTerminalBoundary = false)
		{
			lock (sync)
			{
				if (!shutdownRequested.TrySetResult(reason))
					return false;

				if (connectorTerminalBoundary && !harnessShutdownStarted)
				{
					connectorTerminalInitiatedShutdown = true;
					connectorTerminalBoundaryTime = connector.CurrentTime;
					connectorTerminalBoundaryState = connector.State;
				}

				return true;
			}
		}

		void recordConnectorFailure(string source, Exception failure)
		{
			var key = $"{failure?.GetType().FullName}:{failure?.Message}";

			lock (sync)
			{
				if (connectorFailureKeys.Add(key))
				{
					connectorFailureCount++;
					if (connectorFailures.Count < 20)
						connectorFailures.Add($"{source}: {describeException(failure)}");
				}
			}

			requestShutdown($"{source} failure");
		}

		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Suspended)
				connectorSuspended.TrySetResult(true);

			if (state == ChannelStates.Stopping)
			{
				// Do not await strategy shutdown while the connector holds its state lock.
				// The TCS continuation starts the central async shutdown after this callback
				// returns, giving Strategy.StopAsync a chance to run before final disconnect.
				requestShutdown("connector terminal boundary", connectorTerminalBoundary: true);
			}

			if (state == ChannelStates.Stopped)
			{
				connectorStopped.TrySetResult(true);
				requestShutdown("connector stopped", connectorTerminalBoundary: true);
			}
		};

		connector.Error += failure => recordConnectorFailure("connector", failure);
		connector.ConnectionError += failure => recordConnectorFailure("connection", failure);
		connector.ConnectionErrorEx += (adapter, failure) => recordConnectorFailure($"connection ({adapter?.GetType().Name ?? "unknown adapter"})", failure);

		strategy.Error += (_, failure) =>
		{
			lock (sync)
				error ??= failure;

			requestShutdown("strategy error");
		};

		strategy.ProcessStateChanged += changedStrategy =>
		{
			if (!ReferenceEquals(changedStrategy, strategy) || changedStrategy.ProcessState != ProcessStates.Stopped)
				return;

			var stoppedEarly = false;

			lock (sync)
			{
				var connectorIsTerminal = connector.State is ChannelStates.Stopping or ChannelStates.Stopped;
				stoppedEarly = !harnessShutdownStarted && !connectorIsTerminal;
				strategyStoppedBeforeConnector |= stoppedEarly;
			}

			strategyStopped.TrySetResult(true);

			if (stoppedEarly)
				requestShutdown("strategy stopped before connector");
		};

		strategy.OrderReceived += (s, o) =>
		{
			lock (sync)
			{
				orders.Add(o.TransactionId);
				if (orderStates.Count < 20)
					orderStates.Add($"#{o.TransactionId} {o.Security?.Id ?? "<null>"} {o.Side} {o.Type} {o.State}, price={o.Price}, volume={o.Volume}, balance={o.Balance}, time={o.Time:O}");
				tryCompleteCoverage();
			}
		};

		strategy.OwnTradeReceived += (s, t) =>
		{
			lock (sync)
			{
				tradesCount++;
				if (tradeStates.Count < 20)
					tradeStates.Add($"order=#{t.Order?.TransactionId}, {t.Order?.Side}, price={t.Trade?.Price}, volume={t.Trade?.Volume}, time={t.Trade?.ServerTime:O}");
				tryCompleteCoverage();
			}
		};

		strategy.SubscriptionStarted += subscription =>
		{
			if (!subscription.DataType.IsTFCandles || subscription.DataType.Arg is not TimeSpan timeFrame || timeFrame <= TimeSpan.Zero)
				return;

			lock (sync)
			{
				if (nativeCandleTimeFrame is null || timeFrame > nativeCandleTimeFrame)
					nativeCandleTimeFrame = timeFrame;
			}
		};

		connector.CurrentTimeChanged += _ =>
		{
			lock (sync)
			{
				// This callback only records coverage. The strict test now runs the complete
				// history window, so it never stops the strategy in the middle of dispatch.
				if (postTradeTarget is null || connector.CurrentTime < postTradeTarget)
					return;

				postTradeReachedTime ??= connector.CurrentTime;
			}
		};

		static string getOrderFailureKey(Subscription subscription, OrderFail fail)
			=> $"{subscription?.TransactionId}|{fail?.TransactionId}|{fail?.Order?.TransactionId}|{fail?.ServerTime:O}|" +
				$"{fail?.Error?.GetType().FullName}|{fail?.Error?.Message}";

		static string getSubscriptionFailureKey(string operation, Subscription subscription, Exception failure)
			=> $"{operation}|{subscription?.TransactionId}|{failure?.GetType().FullName}|{failure?.Message}";

		connector.OrderRegisterFailReceived += (subscription, fail) =>
		{
			lock (sync)
			{
				if (orderRegisterFailureKeys.Add(getOrderFailureKey(subscription, fail)))
				{
					orderRegisterFailureCount++;
					if (orderRegisterFailures.Count < 20)
						orderRegisterFailures.Add(describeOrderFailure(subscription, fail));
				}
			}

			requestShutdown("order registration failure");
		};

		connector.OrderCancelFailReceived += (subscription, fail) =>
		{
			lock (sync)
			{
				if (orderCancelFailureKeys.Add(getOrderFailureKey(subscription, fail)))
				{
					orderCancelFailureCount++;
					if (orderCancelFailures.Count < 20)
						orderCancelFailures.Add(describeOrderFailure(subscription, fail));
				}
			}

			requestShutdown("order cancellation failure");
		};

		connector.OrderEditFailReceived += (subscription, fail) =>
		{
			lock (sync)
			{
				if (orderEditFailureKeys.Add(getOrderFailureKey(subscription, fail)))
				{
					orderEditFailureCount++;
					if (orderEditFailures.Count < 20)
						orderEditFailures.Add(describeOrderFailure(subscription, fail));
				}
			}

			requestShutdown("order editing failure");
		};

		connector.MassOrderCancelFailed2 += (transactionId, failure, time) =>
		{
			lock (sync)
			{
				var key = $"{transactionId}|{time:O}|{failure?.GetType().FullName}|{failure?.Message}";
				if (massOrderCancelFailureKeys.Add(key))
				{
					massOrderCancelFailureCount++;
					if (massOrderCancelFailures.Count < 20)
						massOrderCancelFailures.Add($"transaction=#{transactionId}, time={time:O}, error={failure?.Message ?? "<unknown>"}");
				}
			}

			requestShutdown("mass order cancellation failure");
		};

		connector.SubscriptionFailed += (subscription, failure, isSubscribe) =>
		{
			lock (sync)
			{
				var operation = isSubscribe ? "subscribe" : "unsubscribe";
				if (subscriptionFailureKeys.Add(getSubscriptionFailureKey(operation, subscription, failure)))
				{
					subscriptionFailureCount++;
					if (subscriptionFailures.Count < 20)
						subscriptionFailures.Add($"{operation} {describeSubscription(subscription)}, error={failure?.Message ?? "<unknown>"}");
				}
			}

			requestShutdown("subscription failure");
		};

		connector.SubscriptionStopped += (subscription, failure) =>
		{
			if (failure is null)
				return;

			lock (sync)
			{
				if (subscriptionFailureKeys.Add(getSubscriptionFailureKey("stopped", subscription, failure)))
				{
					subscriptionFailureCount++;
					if (subscriptionFailures.Count < 20)
						subscriptionFailures.Add($"stopped {describeSubscription(subscription)}, error={failure.Message}");
				}
			}

			requestShutdown("subscription stopped with error");
		};

		//logManager.Sources.Add(connector);
		//logManager.Sources.Add(strategy);

		void recordCleanupFailure(string stage, Exception failure)
		{
			var key = $"{stage}:{failure.GetType().FullName}:{failure.Message}";

			lock (sync)
			{
				if (cleanupFailureKeys.Add(key))
				{
					cleanupFailureCount++;
					if (cleanupFailures.Count < 20)
						cleanupFailures.Add($"{stage}: {failure.Message}");
				}
			}
		}

		async Task performShutdown()
		{
			lock (sync)
				harnessShutdownStarted = true;

			using (var suspendSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
			{
				try
				{
					if (connector.State == ChannelStates.Started)
						await connector.SuspendAsync(suspendSource.Token);

					if (connector.State == ChannelStates.Suspending)
						await Task.WhenAny(connectorSuspended.Task, connectorStopped.Task).WaitAsync(suspendSource.Token);
				}
				catch (InvalidOperationException) when (connector.State is ChannelStates.Stopping or ChannelStates.Stopped)
				{
					// The finite replay reached its terminal transition between the state
					// check and SuspendAsync. That is an equally safe shutdown boundary.
				}
				catch (Exception failure)
				{
					recordCleanupFailure("connector suspend", failure);
				}
			}

			using (var stopSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
			{
				try
				{
					if (strategy.ProcessState == ProcessStates.Started)
					{
						// A finite test window is not a strategy signal. Avoid creating
						// artificial cancel failures after replay has ended; restore the
						// configured value before clone/settings validation.
						strategy.CancelOrdersWhenStopping = false;
						await strategy.StopAsync(stopSource.Token);
					}

					if (strategy.ProcessState != ProcessStates.Stopped)
						await strategyStopped.Task.WaitAsync(stopSource.Token);
				}
				catch (Exception failure)
				{
					recordCleanupFailure("strategy stop", failure);
				}
				finally
				{
					strategy.CancelOrdersWhenStopping = cancelOrdersWhenStopping;
				}
			}

			using (var disconnectSource = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
			{
				try
				{
					if (connector.ConnectionState == ConnectionStates.Connected)
						await connector.DisconnectAsync(disconnectSource.Token);

					if (connector.State != ChannelStates.Stopped)
						await connectorStopped.Task.WaitAsync(disconnectSource.Token);
				}
				catch (ArgumentException) when (connector.ConnectionState != ConnectionStates.Connected)
				{
					// Historical replay or another shutdown path disconnected first.
				}
				catch (Exception failure)
				{
					recordCleanupFailure("connector disconnect", failure);
				}
			}
		}

		var shutdownSync = new object();
		Task shutdownTask = null;

		Task stopStrategyThenDisconnect()
		{
			lock (shutdownSync)
				return shutdownTask ??= performShutdown();
		}

		(bool completed, Exception execError) result = default;
		var replayTimedOut = false;

		try
		{
			using (var setupSource = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
				await connector.ConnectAsync(setupSource.Token);

			using var replayTimeoutSource = new CancellationTokenSource();
			var replayTimeout = Task.Delay(_replayTimeout, replayTimeoutSource.Token);
			var execTask = strategy.ExecAsync(_ => connector.StartAsync(CancellationToken.None), CancellationToken.None).AsTask();
			var completedTask = await Task.WhenAny(execTask, shutdownRequested.Task, replayTimeout);

			if (completedTask == replayTimeout)
			{
				replayTimedOut = true;
				shutdownReason = "replay timeout";
				await stopStrategyThenDisconnect();
			}
			else if (completedTask == shutdownRequested.Task)
			{
				shutdownReason = await shutdownRequested.Task;
				await stopStrategyThenDisconnect();
			}

			try
			{
				result = await execTask.WaitAsync(TimeSpan.FromSeconds(5));
			}
			catch (TimeoutException)
			{
				result = (false, null);
			}

			replayTimeoutSource.Cancel();
			shutdownReason ??= shutdownRequested.Task.IsCompletedSuccessfully
				? shutdownRequested.Task.Result
				: "strategy stopped";
		}
		finally
		{
			try
			{
				await stopStrategyThenDisconnect();
			}
			finally
			{
				strategy.Connector = null;
			}
		}

		string getDiagnostics()
		{
			lock (sync)
			{
				return $"strategy={strategy.GetType().FullName}, replay={startTime:O}..{stopTime:O}, " +
					$"current={connector.CurrentTime:O}, finished={connector.IsFinished}, state={strategy.ProcessState}, " +
					$"formed={strategy.IsFormed}, online={strategy.IsOnline}, shutdown={shutdownReason ?? "n/a"}, " +
					$"terminalBoundary={connectorTerminalBoundaryState?.ToString() ?? "n/a"}@{connectorTerminalBoundaryTime:O}, " +
					$"nativeCandleTf={nativeCandleTimeFrame?.ToString() ?? "n/a"}, postTradeHorizon={actualPostTradeHorizon?.ToString() ?? "n/a"}, " +
					$"coverageTime={coverageTime:O}, postTradeTarget={postTradeTarget:O}, postTradeReached={postTradeReachedTime:O}, " +
					$"orders={orders.Count}, activeOrders={strategy.Orders.Count(order => StateValidator.IsActive(order.State))}, " +
					$"trades={tradesCount}, position={strategy.Position}.";
			}
		}

		string[] registerFailures;
		string[] cancelFailures;
		string[] editFailures;
		string[] massCancelFailures;
		string[] subscribeFailures;
		string[] observedConnectorFailures;
		string[] observedCleanupFailures;
		string[] observedOrders;
		string[] observedTrades;
		int registerFailureCount;
		int cancelFailureCount;
		int editFailureCount;
		int massCancelFailureCount;
		int subscribeFailureCount;
		int finalConnectorFailureCount;
		int finalCleanupFailureCount;
		int ordersCount;
		int finalTradesCount;
		bool reachedPostTradeTarget;
		bool terminalInitiatedShutdown;
		bool strategyStoppedEarly;

		lock (sync)
		{
			registerFailures = orderRegisterFailures.ToArray();
			cancelFailures = orderCancelFailures.ToArray();
			editFailures = orderEditFailures.ToArray();
			massCancelFailures = massOrderCancelFailures.ToArray();
			subscribeFailures = subscriptionFailures.ToArray();
			observedConnectorFailures = connectorFailures.ToArray();
			observedCleanupFailures = cleanupFailures.ToArray();
			observedOrders = orderStates.ToArray();
			observedTrades = tradeStates.ToArray();
			registerFailureCount = orderRegisterFailureCount;
			cancelFailureCount = orderCancelFailureCount;
			editFailureCount = orderEditFailureCount;
			massCancelFailureCount = massOrderCancelFailureCount;
			subscribeFailureCount = subscriptionFailureCount;
			finalConnectorFailureCount = connectorFailureCount;
			finalCleanupFailureCount = cleanupFailureCount;
			ordersCount = orders.Count;
			finalTradesCount = tradesCount;
			reachedPostTradeTarget = postTradeReachedTime is not null;
			terminalInitiatedShutdown = connectorTerminalInitiatedShutdown;
			strategyStoppedEarly = strategyStoppedBeforeConnector;
		}

		if (finalConnectorFailureCount > 0)
			Assert.Fail(
				$"Connector failed {finalConnectorFailureCount} time(s): {string.Join("; ", observedConnectorFailures)} {getDiagnostics()} " +
				$"Orders: {string.Join("; ", observedOrders)} Trades: {string.Join("; ", observedTrades)}");

		if (registerFailureCount > 0)
			Assert.Fail($"Order registration failed {registerFailureCount} time(s): {string.Join("; ", registerFailures)} {getDiagnostics()}");

		if (cancelFailureCount > 0)
			Assert.Fail($"Order cancellation failed {cancelFailureCount} time(s): {string.Join("; ", cancelFailures)} {getDiagnostics()}");

		if (editFailureCount > 0)
			Assert.Fail($"Order editing failed {editFailureCount} time(s): {string.Join("; ", editFailures)} {getDiagnostics()}");

		if (massCancelFailureCount > 0)
			Assert.Fail($"Mass order cancellation failed {massCancelFailureCount} time(s): {string.Join("; ", massCancelFailures)} {getDiagnostics()}");

		if (subscribeFailureCount > 0)
			Assert.Fail($"Subscription operation failed {subscribeFailureCount} time(s): {string.Join("; ", subscribeFailures)} {getDiagnostics()}");

		if (error is not null)
			throw error;

		if (result.execError is not null)
			throw result.execError;

		if (replayTimedOut)
			Assert.Fail($"Historical replay exceeded the {_replayTimeout} budget. {getDiagnostics()}");

		if (finalCleanupFailureCount > 0)
			Assert.Fail($"Harness cleanup failed {finalCleanupFailureCount} time(s): {string.Join("; ", observedCleanupFailures)} {getDiagnostics()}");

		result.completed.AssertTrue($"Strategy execution did not complete successfully. {getDiagnostics()} Orders: {string.Join("; ", observedOrders)} Trades: {string.Join("; ", observedTrades)}");

		(!strategyStoppedEarly).AssertTrue($"Strategy stopped before the connector entered a terminal boundary. {getDiagnostics()}");
		terminalInitiatedShutdown.AssertTrue(
			$"The connector reached its terminal boundary only after a harness, strategy, or error shutdown was requested. {getDiagnostics()}");
		Assert.AreEqual(ProcessStates.Stopped, strategy.ProcessState, $"Strategy did not reach its terminal state. {getDiagnostics()}");
		Assert.AreEqual(ChannelStates.Stopped, connector.State, $"Emulation connector did not reach its terminal state. {getDiagnostics()}");

		ordersCount.AssertGreater(0, $"No orders were created by the strategy. {getDiagnostics()}");

		finalTradesCount.AssertGreater(0, $"No trades were created by the strategy. {getDiagnostics()} Orders: {string.Join("; ", observedOrders)}");

		reachedPostTradeTarget.AssertTrue(
			$"Strategy stopped before the required post-trade horizon. {getDiagnostics()} " +
			$"Orders: {string.Join("; ", observedOrders)} Trades: {string.Join("; ", observedTrades)}");

		// // Check the distribution of trades over the entire period
		// var firstTradeTime = strategy.MyTrades.Min(t => t.Trade.ServerTime);
		// var lastTradeTime = strategy.MyTrades.Max(t => t.Trade.ServerTime);

		// // The time of the first and last trade should not be too close to the start/end of the period
		// var totalPeriod = (stopTime - startTime).TotalSeconds;
		// var firstOffset = (firstTradeTime - startTime).TotalSeconds / totalPeriod;
		// var lastOffset = (stopTime - lastTradeTime).TotalSeconds / totalPeriod;

		// // The first trade should not be later than 15% from the start, the last not earlier than 15% before the end
		// (firstOffset < 0.85).AssertTrue($"First trade too late: {firstTradeTime}");
		// (lastOffset < 0.85).AssertTrue($"Last trade too early: {lastTradeTime}");

		// // Trades should be distributed over at least 70% of the period
		// var tradesSpan = (lastTradeTime - firstTradeTime).TotalSeconds / totalPeriod;
		// (tradesSpan > 0.7).AssertTrue($"Trades are not distributed enough: {tradesSpan:P0}");

		strategy.Reset();
		clone.Reset();

		static void validateSettingsStorage(SettingsStorage s1, SettingsStorage s2, string name)
		{
			s1.Count.AreEqual(s2.Count, name);

			foreach (var (k, v) in s1)
			{
				if (v is SettingsStorage v1)
					validateSettingsStorage(v1, (SettingsStorage)s2[k], k);
				else if (k != nameof(IIndicator.Id))
					v.AreEqual(s2[k], k);
			}
		}

		static void validateValue(object value, object cloneValue, string name)
		{
			if (ReferenceEquals(value, cloneValue))
				return;

			if (value is null || cloneValue is null)
			{
				(value is null).AreEqual(cloneValue is null, name);
				return;
			}

			switch (value)
			{
				case Security security when cloneValue is Security cloneSecurity:
					security.Id.AreEqual(cloneSecurity.Id, name);
					return;

				case Portfolio portfolio when cloneValue is Portfolio clonePortfolio:
					portfolio.Name.AreEqual(clonePortfolio.Name, name);
					return;

				case SettingsStorage storage when cloneValue is SettingsStorage cloneStorage:
					validateSettingsStorage(storage, cloneStorage, name);
					return;

				case string:
					value.AreEqual(cloneValue, name);
					return;

				// A parameter of the clone is a different object holding the same setting, so what is
				// compared is the setting. Diagram strategies keep theirs in a set, where two equal sets
				// need not enumerate in the same order, so they are matched by name.
				case IStrategyParam param when cloneValue is IStrategyParam cloneParam:
					validateValue(param.Value, cloneParam.Value, $"{name} ({param.Id})");
					return;

				case IEnumerable enumerable when cloneValue is IEnumerable cloneEnumerable:
				{
					var values = enumerable.Cast<object>().ToArray();
					var cloneValues = cloneEnumerable.Cast<object>().ToArray();
					values.Length.AreEqual(cloneValues.Length, name);

					if (values.All(v => v is IStrategyParam))
					{
						var byName = cloneValues.Cast<IStrategyParam>().ToDictionary(p => p.Id);

						foreach (IStrategyParam param in values.Cast<IStrategyParam>())
						{
							byName.TryGetValue(param.Id, out var cloneParam).AssertTrue($"{name}: the clone has no '{param.Id}'.");
							validateValue(param.Value, cloneParam.Value, $"{name} ({param.Id})");
						}

						return;
					}

					for (var i = 0; i < values.Length; i++)
						validateValue(values[i], cloneValues[i], $"{name}[{i}]");

					return;
				}
			}

			value.AreEqual(cloneValue, name);
		}

		foreach (var field in strategy.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
		{
			var fv = field.GetValue(strategy);
			var fv2 = field.GetValue(clone);

			if (fv is IStrategyParam sp)
			{
				var cloneParam = (IStrategyParam)fv2;
				validateValue(sp.Value, cloneParam.Value, field.Name);
			}
			else if (fv is IIndicator i)
			{
				if (fv2 is not null)
					validateSettingsStorage(i.Save(), ((IIndicator)fv2).Save(), field.Name);
			}
			// Anything else that persists itself -- a diagram composition, for one -- is an object graph
			// the strategy worked through during the replay while the clone sat untouched, so the two
			// legitimately differ by now. What has to match after the round-trip is the parameters, and
			// those are compared above.
			else if (fv is IPersistable && fv2 is IPersistable)
			{
			}
			else
				validateValue(fv, fv2, field.Name);
		}
	}
}
