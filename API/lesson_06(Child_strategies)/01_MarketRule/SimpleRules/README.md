# Explanation of Market Depth Rules in SimpleRules Strategy

## Overview

This strategy sets up various rules based on market depth updates (`OrderBookReceived`) to perform logging actions. These rules help track market conditions and demonstrate different methods to apply rules within the StockSharp environment.

## Strategy Initialization and Rule Setup

### OnStarted Method
When the strategy starts, it subscribes to trade ticks and market depth for the specified security and then sets up multiple rules to react to changes in market depth:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    var tickSub = Connector.SubscribeTrades(Security);
    var mdSub = Connector.SubscribeMarketDepth(Security);

    // Rule setup using various methods
    SetupRuleMethod1(mdSub);
    SetupRuleMethod2(mdSub);
    SetupNestedRule(mdSub);

    base.OnStarted(time);
}
```

### Rule Method 1
This method uses a direct approach to create a rule that logs the best bid and ask prices when a market depth update is received, applying the rule once:

```csharp
void SetupRuleMethod1(IMarketDepthSubscription mdSub)
{
    mdSub.WhenOrderBookReceived(Connector).Do((depth) =>
    {
        this.AddInfoLog($"The rule WhenOrderBookReceived �1 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
    }).Once().Apply(this);
}
```

### Rule Method 2
This method shows another way to create and apply a market depth rule. It separates the rule creation from its application, demonstrating flexibility in handling and extending rule conditions:

```csharp
void SetupRuleMethod2(IMarketDepthSubscription mdSub)
{
    var whenMarketDepthChanged = mdSub.WhenOrderBookReceived(Connector);
    whenMarketDepthChanged.Do((depth) =>
    {
        this.AddInfoLog($"The rule WhenOrderBookReceived �2 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");
    }).Once().Apply(this);
}
```

### Nested Rules
This setup introduces a rule within another rule. It illustrates how complex conditions can be monitored by nesting rules, where a second rule triggers repeatedly within the context established by the first rule:

```csharp
void SetupNestedRule(IMarketDepthSubscription mdSub)
{
    mdSub.WhenOrderBookReceived(Connector).Do((depth) =>
    {
        this.AddInfoLog($"The rule WhenOrderBookReceived �3 BestBid={depth.GetBestBid()}, BestAsk={depth.GetBestAsk()}");

        // Not a Once rule - this will continue to apply
        mdSub.WhenOrderBookReceived(Connector).Do((depth1) =>
        {
            this.AddInfoLog($"The rule WhenOrderBookReceived �4 BestBid={depth1.GetBestBid()}, BestAsk={depth1.GetBestAsk()}");
        }).Apply(this);
    }).Once().Apply(this);
}
```

## Conclusion

The `SimpleRules` strategy leverages StockSharp's powerful event-driven model to implement real-time market data handling through rules. These rules enable the strategy to respond dynamically to changes in market conditions, demonstrating various methods to configure and apply these rules for different scenarios. This example effectively showcases how strategies can be enriched with complex logic to monitor, analyze, and react to live trading data.