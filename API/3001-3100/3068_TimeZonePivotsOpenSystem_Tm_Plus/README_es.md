# Estrategia Exp Sistema Abierto de Pivotes de Zona Horaria Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de alto nivel de StockSharp del asesor experto **Exp_TimeZonePivotsOpenSystem_Tm_Plus**. Recrea el indicador propietario *TimeZonePivotsOpenSystem* que proyecta dos zonas de breakout alrededor de la apertura de la sesión diaria y opera los retrocesos que siguen a un breakout. Cada componente del script original—retraso de señal, filtro de tiempo, lógica de salida asimétrica y los preajustes de gestión de dinero—fue mapeado a parámetros explícitos para que el comportamiento sea consistente con la implementación MQL5.

## Lógica de trading

1. En el `StartHour` configurado, la estrategia registra el precio de apertura de la sesión. Luego se trazan dos niveles dinámicos a `OffsetPoints` (en puntos) por encima y por debajo de ese ancla.
2. Siempre que una vela finalizada cierra **por encima** del nivel superior, la estrategia:
   - Programa una entrada larga para ser ejecutada en la siguiente vela (respetando el retraso `SignalBar`) solo si la barra actual ya no está por encima de la banda.
   - Cierra cualquier posición corta abierta inmediatamente si `SellPosClose` está habilitado.
3. Siempre que una vela finalizada cierra **por debajo** del nivel inferior, la estrategia:
   - Programa una entrada corta para la siguiente vela siempre que la barra actual ya no esté por debajo de la banda.
   - Cierra cualquier posición larga abierta inmediatamente si `BuyPosClose` está habilitado.
4. Las entradas se ejecutan en la primera actualización de la siguiente vela gracias a `TryExecutePendingEntries`. Esto coincide con el experto original que retrasa la orden hasta que comienza la nueva barra.

El parámetro de retraso de señal `SignalBar` reproduce el desplazamiento `CopyBuffer` original. Un valor de `0` reacciona a la barra cerrada más reciente, mientras que `1` espera una barra extra antes de actuar, dando confirmación adicional.

## Gestión de órdenes

* **Stop-loss / take-profit** – Las distancias se establecen en puntos (`StopLossPoints`, `TakeProfitPoints`) y se convierten a precio usando el paso del instrumento. Ambos niveles se monitorean usando los extremos de las velas para que las tocadas intrabarra activen una salida.
* **Salida basada en tiempo** – Cuando `TimeTrade` es verdadero, la posición se cierra forzosamente después de `HoldingMinutes` minutos, espejando el temporizador `nTime` del código MQL5.
* **Cierres manuales** – Las señales de breakout en dirección opuesta cierran la operación en curso si el indicador `BuyPosClose` o `SellPosClose` correspondiente está habilitado.

## Gestión del dinero

El parámetro `MoneyMode` reproduce la enumeración `MarginMode`:

- `Lot` – volumen fijo igual a `MoneyManagement`.
- `Balance` y `FreeMargin` – usan múltiplos de capital en cuenta o margen libre (`MoneyManagement * capital / precio`).
- `LossBalance` y `LossFreeMargin` – dimensionamiento basado en riesgo que divide la fracción de capital deseada por la distancia del stop.

Si `StopLossPoints` está en cero, los modos de riesgo recurren graciosamente al dimensionamiento basado en precio.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `MoneyManagement` | Coeficiente base usado para dimensionar la posición dependiendo de `MoneyMode`. | `0.1` |
| `MoneyMode` | Modelo de dimensionamiento de posición (`Lot`, `Balance`, `FreeMargin`, `LossBalance`, `LossFreeMargin`). | `Lot` |
| `StopLossPoints` | Distancia del stop-loss expresada en puntos desde el precio de ejecución. | `1000` |
| `TakeProfitPoints` | Distancia del take-profit expresada en puntos desde el precio de ejecución. | `2000` |
| `DeviationPoints` | Parámetro informativo conservado del experto (configuración de deslizamiento en puntos). | `10` |
| `BuyPosOpen` / `SellPosOpen` | Habilitar o deshabilitar entradas largas y cortas. | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir que el breakout opuesto cierre posiciones forzosamente. | `true` |
| `TimeTrade` | Habilitar el filtro de tiempo máximo de tenencia. | `true` |
| `HoldingMinutes` | Vida útil máxima de la posición en minutos. | `720` |
| `OffsetPoints` | Distancia de las bandas de pivote desde la apertura de sesión en puntos. | `200` |
| `SignalBar` | Número de barras para retrasar la evaluación de señal (0 = última barra cerrada). | `1` |
| `CandleType` | Marco temporal principal usado para calcular el indicador. | `TimeSpan.FromHours(1).TimeFrame()` |
| `StartHour` | Hora del día (0-23) que define el precio de apertura de la sesión. | `0` |

## Notas de uso

- La estrategia asume que el valor proporciona un `PriceStep` válido. Si el instrumento carece de ese metadato, se usa un respaldo de `0.0001`.
- Dado que las entradas se activan en la primera actualización de una nueva vela, el precio real de ejecución seguirá el mercado en ese momento, al igual que el experto, lo que puede diferir del precio teórico de apertura en mercados rápidos.
- Para replicar la superposición del indicador original, mantenga el marco temporal del backtest en H1 o inferior, ya que el script MQL5 solo opera en períodos horarios o menores.
- Establezca `SignalBar` en `0` para un comportamiento más receptivo o en `1` (predeterminado) para esperar una barra extra después de un breakout.
