using System;
using System.Linq;
using System.Threading;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies;

/// <summary>
/// Closes the current symbol position once floating profit or loss reaches configured equity percentages.
/// </summary>
public class RiskProfitCloserStrategy : Strategy
{
        private readonly StrategyParam<decimal> _riskPercentage;
        private readonly StrategyParam<decimal> _profitPercentage;
        private readonly StrategyParam<TimeSpan> _timerInterval;

        private decimal? _bestBid;
        private decimal? _bestAsk;
        private int _isProcessing;

        /// <summary>
        /// Percentage of the account equity that defines the maximum tolerable loss per position.
        /// </summary>
        public decimal RiskPercentage
        {
                get => _riskPercentage.Value;
                set => _riskPercentage.Value = value;
        }

        /// <summary>
        /// Percentage of the account equity that defines the desired floating profit per position.
        /// </summary>
        public decimal ProfitPercentage
        {
                get => _profitPercentage.Value;
                set => _profitPercentage.Value = value;
        }

        /// <summary>
        /// Interval used to re-evaluate open positions when no market data arrives.
        /// </summary>
        public TimeSpan TimerInterval
        {
                get => _timerInterval.Value;
                set => _timerInterval.Value = value;
        }

        /// <summary>
        /// Initializes the strategy parameters.
        /// </summary>
        public RiskProfitCloserStrategy()
        {
                _riskPercentage = Param(nameof(RiskPercentage), 1m)
                        .SetNotNegative()
                        .SetDisplay("Risk %", "Maximum tolerated loss as percentage of equity", "Risk");

                _profitPercentage = Param(nameof(ProfitPercentage), 2m)
                        .SetNotNegative()
                        .SetDisplay("Profit %", "Desired profit as percentage of equity", "Risk");

                _timerInterval = Param(nameof(TimerInterval), TimeSpan.FromSeconds(1))
                        .SetDisplay("Timer Interval", "How often to perform safety checks", "General");
        }

        /// <inheritdoc />
        protected override void OnStarted(DateTimeOffset time)
        {
                base.OnStarted(time);

                if (Security == null)
                        throw new InvalidOperationException("Security must be assigned before starting the strategy.");

                if (Portfolio == null)
                        throw new InvalidOperationException("Portfolio must be assigned before starting the strategy.");

                if (TimerInterval <= TimeSpan.Zero)
                        throw new InvalidOperationException("Timer interval must be greater than zero.");

                // Subscribe to bid/ask updates so the latest prices are available for profit calculations.
                SubscribeLevel1()
                        .Bind(ProcessLevel1)
                        .Start();

                // Periodically check the position in case the market is idle.
                Timer.Start(TimerInterval, CheckPositions);

                // Evaluate immediately using the current portfolio snapshot.
                CheckPositions();
        }

        private void ProcessLevel1(Level1ChangeMessage message)
        {
                if (message.Changes.TryGetValue(Level1Fields.BestBidPrice, out var bidValue) && bidValue is decimal bid && bid > 0m)
                        _bestBid = bid;

                if (message.Changes.TryGetValue(Level1Fields.BestAskPrice, out var askValue) && askValue is decimal ask && ask > 0m)
                        _bestAsk = ask;

                // React to fresh quotes without waiting for the timer.
                CheckPositions();
        }

        private void CheckPositions()
        {
                if (Interlocked.Exchange(ref _isProcessing, 1) == 1)
                        return;

                try
                {
                        var portfolio = Portfolio;
                        var security = Security;

                        if (portfolio == null || security == null)
                                return;

                        var equity = portfolio.CurrentValue ?? portfolio.BeginValue ?? 0m;
                        if (equity <= 0m)
                                return;

                        var riskAmount = equity * RiskPercentage / 100m;
                        var profitAmount = equity * ProfitPercentage / 100m;

                        foreach (var position in portfolio.Positions.ToArray())
                        {
                                if (!Equals(position.Security, security))
                                        continue;

                                var quantity = position.CurrentValue ?? 0m;
                                if (quantity == 0m)
                                        continue;

                                var averagePrice = position.AveragePrice;
                                if (averagePrice is null || averagePrice <= 0m)
                                        continue;

                                var isLong = quantity > 0m;
                                var marketPrice = GetMarketPrice(isLong, security);
                                if (marketPrice <= 0m)
                                        continue;

                                var priceDifference = isLong
                                        ? marketPrice - averagePrice.Value
                                        : averagePrice.Value - marketPrice;

                                var profit = ConvertPriceToMoney(security, priceDifference, Math.Abs(quantity));

                                if (profit >= profitAmount || profit <= -riskAmount)
                                {
                                        // Flatten the position once the floating threshold is breached.
                                        ClosePosition(security);
                                }
                        }
                }
                finally
                {
                        Interlocked.Exchange(ref _isProcessing, 0);
                }
        }

        private decimal GetMarketPrice(bool isLong, Security security)
        {
                if (isLong)
                {
                        if (_bestBid is { } bid && bid > 0m)
                                return bid;
                }
                else
                {
                        if (_bestAsk is { } ask && ask > 0m)
                                return ask;
                }

                var lastTrade = security.LastTrade?.Price;
                if (lastTrade is { } tradePrice && tradePrice > 0m)
                        return tradePrice;

                var lastPrice = security.LastPrice;
                if (lastPrice is { } lp && lp > 0m)
                        return lp;

                return 0m;
        }

        private static decimal ConvertPriceToMoney(Security security, decimal priceDifference, decimal volume)
        {
                if (volume <= 0m || priceDifference == 0m)
                        return 0m;

                var priceStep = security.PriceStep;
                var stepPrice = security.StepPrice;

                if (priceStep is null || stepPrice is null || priceStep <= 0m || stepPrice <= 0m)
                        return priceDifference * volume;

                var steps = priceDifference / priceStep.Value;
                return steps * stepPrice.Value * volume;
        }
}
