# Estrategia Exp Spearman Rank Correlation Histogram
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de StockSharp porta el experto de MetaTrader **Exp_SpearmanRankCorrelation_Histogram**. Se suscribe a una serie de velas configurable, calcula el histograma de correlación de rango de Spearman para cada barra completada y reacciona cuando cambia el estado codificado por color. Dependiendo del modo de operación, el algoritmo puede cerrar posiciones opuestas, revertir a una nueva operación, o esperar valores extremos antes de actuar.

## Pipeline del indicador

1. Un indicador `RankCorrelationIndex` (correlación de rango de Spearman escalada a ±100) se alimenta con los precios de cierre de las velas. La ventana de lookback está limitada por `MaxRange` y por defecto es de 14 barras.
2. La correlación bruta se normaliza al intervalo `[-1, 1]`. Cuando `InvertCorrelation` está habilitado, el signo se invierte para emular el flag `direction` de MQL.
3. El valor normalizado se compara con `HighLevel` y `LowLevel` para asignar un estado de color:
   * `4` – zona fuertemente alcista (`value > HighLevel`).
   * `3` – zona moderadamente alcista (`0 < value ≤ HighLevel`).
   * `2` – neutral (`value == 0`).
   * `1` – zona moderadamente bajista (`LowLevel ≤ value < 0`).
   * `0` – zona fuertemente bajista (`value < LowLevel`).
4. Los últimos colores se almacenan en un buffer estilo serie para que el índice `0` represente la vela cerrada más reciente, el índice `1` la anterior, y así sucesivamente.

## Flujo de trabajo de trading

* Las señales se evalúan únicamente en velas terminadas (`CandleStates.Finished`).
* El parámetro `SignalBar` define qué barra completada se inspecciona (por defecto una barra atrás). La estrategia también observa la barra inmediatamente anterior, replicando la búsqueda de doble buffer del asesor experto.
* Los interruptores de órdenes (`AllowBuyEntries`, `AllowSellEntries`, `AllowBuyExits`, `AllowSellExits`) deciden si las posiciones largas/cortas pueden abrirse o cerrarse.
* Los modos de operación reproducen el interruptor de MetaTrader:
  * **Modo 1** – cerrar la posición opuesta siempre que el color anterior sea alcista/bajista (`> 2` o `< 2`). Si se permite, abrir en la nueva dirección cuando el color reciente sale de la zona alcista (`< 3`) o bajista (`> 1`).
  * **Modo 2** – reaccionar solo a colores extremos. El extremo alcista (`4`) permite a la estrategia cerrar cortos y opcionalmente abrir largos cuando la barra más nueva cae por debajo de `4`. El extremo bajista (`0`) cierra largos y puede abrir cortos cuando la barra más nueva sube por encima de `0`.
  * **Modo 3** – una versión más estricta del Modo 2: los cortos se cierran inmediatamente en `4`, los largos en `0`, y las nuevas operaciones se permiten bajo las mismas condiciones que el Modo 2.
* `CancelActiveOrders()` se ejecuta antes de enviar nuevas órdenes de mercado para evitar solicitudes obsoletas.
* Las reversiones de posición usan el `Volume` configurado más la posición actual absoluta para que la operación cambie completamente al lado opuesto.
* Los opcionales `StopLossPoints` y `TakeProfitPoints` (unidades de precio) habilitan la gestión de riesgo basada en `StartProtection`; cuando se dejan en `0`, no se generan órdenes de protección.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal usado para el indicador y las decisiones de trading. |
| `RangeLength` | Período de lookback nominal de Spearman (limitado por `MaxRange`). |
| `MaxRange` | Límite superior para la longitud de lookback efectiva; cae a `10` si se establece en `0`. |
| `HighLevel`, `LowLevel` | Umbrales que separan las zonas alcistas y bajistas del histograma. |
| `SignalBar` | Número de barras cerradas a omitir antes de analizar el histograma. |
| `InvertCorrelation` | Invierte el signo del histograma para coincidir con el comportamiento `direction=false` de MQL. |
| `AllowBuyEntries`, `AllowSellEntries` | Habilitar apertura de posiciones largas/cortas. |
| `AllowBuyExits`, `AllowSellExits` | Habilitar cierre automático de posiciones largas/cortas existentes. |
| `TradeMode` | Selecciona la lógica del Modo 1, Modo 2 o Modo 3 del experto original. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias de protección opcionales en unidades de precio absolutas para `StartProtection`. |
| `Volume` (integrado) | Tamaño base de orden usado al abrir o revertir posiciones. |

## Diferencias con el experto de MetaTrader

* Las entradas de gestión de capital (`MM`, `MMMode`) y deslizamiento (`Deviation_`) no se replican; el dimensionamiento de posición depende de la propiedad estándar `Volume` y la configuración del bróker.
* Las funciones auxiliares MQL de `TradeAlgorithms.mqh` se reemplazan con llamadas directas a `BuyMarket`/`SellMarket` después de cancelar órdenes pendientes.
* La sugerencia de rendimiento `CalculatedBars` es innecesaria en StockSharp y se ha omitido.
* El flag `direction` está representado por `InvertCorrelation`, que simplemente refleja el signo del histograma.
* Las distancias de stop-loss y take-profit (`StopLoss_`, `TakeProfit_`) se interpretan como offsets de precio absolutos al habilitar `StartProtection`; no se realiza conversión automática de punto a precio.
* Los tiempos de señal se manejan al cierre de la vela; no hay programación diferida a la apertura de la siguiente barra.

Estos ajustes siguen las pautas de estrategia de alto nivel de StockSharp mientras preservan la lógica de señal original.
