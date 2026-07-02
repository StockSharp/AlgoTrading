# Estrategia de energía de las olas EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Wave Power EA** es una adaptación de C# del asesor experto MQL4 "Wave Power EA1". El robot original construye una posición en
dirección de una señal estocástica o MACD y luego agrega órdenes de mercado adicionales cada número fijo de pips mientras ajusta la
nivel de toma de ganancias compartido. La versión StockSharp reproduce este comportamiento utilizando la estrategia de alto nivel API, enlace de indicador
y ayudantes de pedidos integrados. Todos los comentarios permanecen en inglés según sea necesario.

## Cómo funciona la estrategia

1. **Selección de señal** – la primera operación se abre solo cuando uno de los filtros del indicador genera una dirección:
   - `Stochastic` – %K cruza %D dentro de regiones de sobreventa/sobrecompra.
   - `MacdSlope` – MACD línea que sube por encima o cae por debajo de su valor anterior.
   - `CciLevels` – CCI cae por debajo de –120 o sube por encima de +120.
   - `AwesomeBreakout` – Oscilador impresionante que rompe el mínimo/máximo histórico adaptativo que se capturó durante la inicialización.
   - `RsiMa` – el rápido SMA cruza el lento SMA mientras que RSI confirma el impulso (por encima/por debajo de 50).
   - `SmaTrend` – un ventilador 15/20/25/50 SMA apuntando en la misma dirección con una diferencia de pendiente mínima.

2. **Expansión de la red**: después de que se completa la primera orden de mercado, la estrategia recuerda el precio de ejecución. Cada vez que el mercado se mueve
por `GridStepPips` contra la posición actual y no se excede el recuento máximo de órdenes, la estrategia envía un nuevo mercado
ordene en la *misma* dirección. Cada nueva capa multiplica el volumen por el parámetro `Multiplier`.

3. **Objetivos compartidos**: cada nueva orden recalcula un precio común de obtención de ganancias y (opcionalmente) de límite de pérdidas. Cuando el número de
las órdenes activas se acercan al umbral `OrdersToProtect`, la distancia de obtención de beneficios se reemplaza por `ReboundProfitPrimary`.
Una vez superado el umbral, la distancia cambia a `ReboundProfitSecondary` para fomentar una recuperación más rápida.

4. **Monitoreo de cesta**: en cada cierre de vela, la estrategia convierte las pérdidas y ganancias abiertas en pips por lote. Si el beneficio de rebote o
Cuando se alcanzan los umbrales de protección contra pérdidas, toda la cesta se liquida mediante órdenes de mercado. Lo mismo ocurre cuando el mayor
la operación es anterior a `OrdersTimeAliveSeconds` o cuando la operación el viernes está deshabilitada.

5. **Ciclo de vida**: una vez que la canasta está plana, todos los contadores internos se reinician, lo que permite que la siguiente señal comience un nuevo promedio
ciclo.

En comparación con el EA original, este puerto evita intencionalmente abrir posiciones opuestas (de cobertura) después de un cierto número de cuadrículas.
capas. Todas las entradas adicionales siguen la dirección inicial. El resto de las normas de gestión del dinero, la lógica de protección y
Los filtros de indicador siguen siendo compatibles con la implementación de referencia MQL4.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `EntryLogic` | Modo indicador utilizado para el primer pedido. |
| `CandleType` | Plazo que alimenta todos los indicadores (predeterminado: 1 hora). |
| `InitialVolume` | Volumen del primer pedido en lotes/contratos. |
| `GridStepPips` | Distancia mínima en pips entre capas de la cuadrícula. |
| `MaxOrders` | Número máximo de pedidos simultáneos en la cesta. |
| `TakeProfitPips` | Distancia de toma de ganancias compartida en pips (0 desactiva el objetivo). |
| `StopLossPips` | Distancia de stop-loss compartida en pips (0 desactiva el stop). |
| `Multiplier` | Multiplicador de volumen aplicado a cada pedido adicional. |
| `SecureProfitProtection` | Habilita la lógica del beneficio de rebote. |
| `OrdersToProtect` | Número de órdenes necesarias antes de que comience la protección contra rebotes. |
| `ReboundProfitPrimary` | Beneficio por lote (en pips) para la primera etapa de protección. |
| `ReboundProfitSecondary` | Beneficio por lote (en pips) una vez que se excede el recuento de órdenes protegidas. |
| `LossProtection` | Habilita la guardia de pérdida flotante. |
| `LossThreshold` | Pérdida por lote (en pips) que activa la guardia cuando la canasta está llena. |
| `ReverseCondition` | Invierte señales de compra/venta. |
| `TradeOnFriday` | Permite abrir nuevos pedidos los viernes. |
| `OrdersTimeAliveSeconds` | Vida útil máxima del pedido más reciente en segundos (0 desactiva el temporizador). |
| `TrendSlopeThreshold` | Diferencia de pendiente mínima SMA utilizada por la lógica `SmaTrend`. |

## Consejos de uso

1. Adjunte la estrategia a un valor con un paso de precio configurado para que la conversión de pips funcione correctamente.
2. Ajuste `GridStepPips`, `Multiplier` y `MaxOrders` según la volatilidad del instrumento y la política de margen del corredor.
3. Habilite los bloques de protección cuando ejecute una cuenta real para evitar pérdidas descontroladas durante tendencias prolongadas.
4. La estrategia se basa en velas cerradas; elija un período de tiempo que refleje el ritmo comercial deseado (el EA original usa M30
y combinaciones H1, pero las velas H1 predeterminadas funcionan bien).
5. Debido a que no se implementa la cobertura después de la quinta capa, considere reducir `MaxOrders` si necesita el original exacto
comportamiento.

## Archivos

- `CS/WavePowerEAStrategy.cs` – StockSharp implementación de la lógica de red Wave Power EA.
- `README.md` / `README_ru.md` / `README_zh.md` – documentación en inglés, ruso y chino.

La versión de Python se omite intencionalmente según los requisitos de la tarea.
