# Pattern Template Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **Pattern Template Strategy** is a direct translation of the "pattern.mq5" expert advisor from MQL5. The original script was written as an educational skeleton that demonstrates how to structure a trading robot with interchangeable modules for money management, signal generation, order approval, and position maintenance. The StockSharp version keeps the very same architecture while wiring the blocks into the high-level API (`SubscribeCandles`, `StrategyParam<T>`, `LogInfo`, etc.). As a result, the class acts as a living documentation sample that can be extended into a fully featured system while remaining faithful to the intent of the original MQL5 template.

This strategy **does not place any orders by default**. Each component only logs its activity so that developers can understand the execution sequence. Replace the bodies of the component methods with real logic to transform the template into a working trading algorithm.

## Workflow

1. **Initialization**
   - `OnStarted` invokes `InitializeComponents`, which recreates the four template blocks (money management, signal generator, trade request handler, and position support) and logs their activation.
   - The public parameter `LotVolume` is copied into the base `Strategy.Volume` so that any later orders can reuse it without additional wiring.
   - Candle data subscription is created through `SubscribeCandles(CandleType)` and routed to `ProcessCandle`.
2. **Per-candle processing**
   - `ProcessCandle` receives finished candles only. It logs the candle time and close price, checks that the strategy is online and allowed to trade, then executes the template pipeline.
   - The money management block returns a fixed volume (`LotVolume`).
   - The signal generator toggles between long and short suggestions to demonstrate how directions can be produced.
   - The trade request handler logs the received context and always approves the request.
   - The position support component reports that a maintenance pass has been executed.
3. **Trade events**
   - Whenever `OnNewMyTrade` is triggered (for example, after you plug in real order placement code), the template logs the event and re-runs the position maintenance block to keep the workflow symmetrical with the original MQL5 example.
4. **Lifecycle logging**
   - `OnReseted` and `OnStopped` provide additional log lines so that every stage of the strategy lifecycle is visible when replaying the template.

## Parameters

| Name | Type | Default | Description |
| ---- | ---- | ------- | ----------- |
| `CandleType` | `DataType` | 1-minute time frame | Specifies the candle subscription used to trigger the template pipeline. Change the default to another timeframe or data type when adapting the template to a different instrument. |
| `LotVolume` | `decimal` | `1` | Volume returned by the money management component and copied into `Strategy.Volume`. The parameter is fully optimizable (0.5 to 5 with a step of 0.5) so that you can quickly model fixed-position sizing. |

## Component summary

- **FixedVolumeMoneyManagement** – returns the configured `LotVolume` and provides log messages that explain when the volume is accessed.
- **TemplateSignalGenerator** – alternates between long and short directions on every call. Replace this with indicator-driven logic to obtain real trading signals.
- **LoggingTradeRequestHandler** – accepts every incoming request while documenting the volume and direction. Extend this block with filters, compliance checks, or time-based throttling.
- **LoggingPositionSupport** – runs after each candle and trade to mirror the `Support()` call in the original MQL5 version. Substitute your own stop-loss, trailing, or hedging logic here.

## Mapping from the MQL5 template

| MQL5 element | StockSharp counterpart | Notes |
| ------------ | --------------------- | ----- |
| `CVolume::Lots()` | `FixedVolumeMoneyManagement.GetVolume()` | Returns the fixed lot size. |
| `CSignal::Generator()` | `TemplateSignalGenerator.GenerateSignal()` | Supplies the direction placeholder. |
| `CRequest::Request()` | `LoggingTradeRequestHandler.TryHandle()` | Accepts requests and logs the incoming data. |
| `CSupport::Support()` | `LoggingPositionSupport.MaintainPosition()` | Called both per candle and on trade events. |
| `CStrategy::OnInitStrategy()` | `OnStarted()` | Both stages wire together the template components. |
| `CStrategy::OnTickStrategy()` | `ProcessCandle()` | Executes the sequential workflow. |
| `CStrategy::OnTradeStrategy()` | `OnNewMyTrade()` | Offers a hook when new trades are produced. |

## Customization tips

1. **Integrate real indicators**: Replace `TemplateSignalGenerator` with classes that subscribe to indicators (e.g., SMA, RSI, Bollinger Bands) and decide when to go long or short.
2. **Advanced risk control**: Modify `FixedVolumeMoneyManagement` to compute position size using ATR, account balance, or margin requirements.
3. **Order validation**: Extend `LoggingTradeRequestHandler` with timing filters, spread checks, or rule-based veto logic before sending market or limit orders.
4. **Position maintenance**: Implement trailing stops, partial exits, or hedging logic in `LoggingPositionSupport` to react to fills or periodic checks.
5. **Telemetry**: The abundant `LogInfo` calls emulate the print statements from the MQL5 script. Retain them for debugging or replace with structured logging and metrics.

## Usage notes

- The template is intentionally side-effect free until you add order placement code (`BuyMarket`, `SellMarket`, etc.).
- Because every component is a private nested class, you can easily swap implementations by editing `InitializeComponents` or converting it into a dependency-injection point.
- The class strictly follows the repository guidelines: file-scoped namespace, tab indentation, high-level API, and English-only comments.
- A Python version is not provided intentionally, matching the task requirements.

Use this strategy as a foundation for experiments, training sessions, or for rapidly prototyping new concepts without starting from an empty file.
