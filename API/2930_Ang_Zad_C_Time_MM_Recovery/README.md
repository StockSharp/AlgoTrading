# Ang Zad C Time MM Recovery Strategy

## Overview
Ang Zad C Time MM Recovery Strategy is a C# port of the MetaTrader 5 expert advisor `Exp_Ang_Zad_C_Tm_MMRec`. The strategy combines the custom Ang_Zad_C channel indicator with a configurable trading session filter and an adaptive position size model that reduces risk after a configurable number of losing trades.

## Indicator logic
The Ang_Zad_C indicator builds two adaptive envelopes around the price. Each envelope is updated by comparing the chosen applied price of the current and previous candle and moving toward the new price with the smoothing factor **Ki**. The upper and lower lines are evaluated on historical bars defined by **Signal Bar** to avoid acting on unfinished candles.

## Trading rules
* **Long entry** – When the upper line was above the lower line on the previous reference bar and crosses below or touches the lower line on the most recent reference bar. When this happens any open short position is closed before a new long is opened (if enabled).
* **Short entry** – When the upper line was below the lower line on the previous reference bar and crosses above or touches the lower line on the most recent reference bar. Any open long position is closed before a new short is opened (if enabled).
* **Long exit** – When the upper line is below the lower line on the previous reference bar. The exit can be disabled via **Enable Long Exit**.
* **Short exit** – When the upper line is above the lower line on the previous reference bar. The exit can be disabled via **Enable Short Exit**.

## Money management and protections
* Trading is allowed only inside the configured time window when **Use Time Filter** is enabled. Positions opened earlier are closed once the session ends.
* The trade volume is selected between **Normal Volume** and **Small Volume** depending on how many losing trades occurred for each side. After **Buy Loss Trigger** losing long trades (or **Sell Loss Trigger** losing short trades) the reduced volume is used until a profitable trade resets the counter.
* Optional stop loss and take profit levels are registered using price step distances defined by **Stop Loss Steps** and **Take Profit Steps**.

## Parameters
| Name | Description |
| ---- | ----------- |
| Candle Type | Timeframe of the candles used by the indicator and signals. |
| Ki | Smoothing coefficient of the Ang_Zad_C envelopes. |
| Applied Price | Which candle price is fed to the indicator. |
| Signal Bar | How many bars back are used for signal evaluation (1 = previous closed bar). |
| Use Time Filter / Trade Start / Trade End | Enable session based trading and set the start and end time of the session. |
| Enable Long/Short Entry | Allow opening new long or short trades. |
| Enable Long/Short Exit | Allow the strategy to close positions on indicator reversal. |
| Buy/Sell Loss Trigger | Number of losing trades before the reduced volume is applied. |
| Small Volume / Normal Volume | Order sizes used for reduced and normal risk. |
| Stop Loss Steps / Take Profit Steps | Distance for protective orders expressed in price steps. |

## Conversion notes
* The logic follows the original MQL5 code, including the directional cross checks and time window behaviour.
* The adaptive money management is implemented by tracking realised profit and loss per direction and switching to the reduced volume after the configured number of losses.
* Indicator computations avoid any direct buffer access and are processed on finished candles using the high-level StockSharp API.
