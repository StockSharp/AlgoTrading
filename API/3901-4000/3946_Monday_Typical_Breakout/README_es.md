# Estrategia típica de ruptura del lunes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de ruptura típica del lunes** es una versión de C# del asesor experto MetaTrader `yi1ywioff50qr6` (ID de repositorio 8187). El robot original monitorea las velas cada hora y abre una posición larga todos los lunes cuando la nueva sesión se abre por encima del precio típico de la barra anterior `(high + low + close) / 3`. Esta implementación reproduce la lógica de entrada dentro del marco estratégico de alto nivel StockSharp y agrega parámetros de configuración detallados para el dimensionamiento de posiciones y el control de riesgos.

## Lógica de trading

1. La estrategia se suscribe a la serie de velas configurada (cada hora por defecto).
2. Al inicio de cada vela terminada se comprueba si:
   - La vela pertenece al lunes.
   - La hora de apertura de la vela coincide con el parámetro *Hora de apertura* configurado (por defecto 09:00).
   - No existen posiciones abiertas ni órdenes activas.
   - El precio de apertura de la vela es mayor que el precio típico de la barra anterior.
3. Si se cumplen todas las condiciones, la estrategia envía una orden de compra de mercado con un volumen calculado por el bloque de gestión de dinero. Las distancias protectoras de stop-loss y take-profit se aplican a través de `StartProtection`.

La estrategia nunca abre posiciones cortas y solo realizará una operación por cada vela del lunes calificada.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `FixedVolume` | Tamaño del lote para entradas. Establezca en `0` para habilitar la tabla de escala de acciones. | `0.1` |
| `OpenHour` | Hora de la sesión de negociación (0-23) en la que se evalúan las señales del lunes. | `9` |
| `StopLossPoints` | Distancia en puntos de precio para la parada de protección. `0` desactiva la parada. | `50` |
| `TakeProfitPoints` | Distancia en puntos de precio para el objetivo de ganancias. `0` desactiva el objetivo. | `20` |
| `InitialEquity` | Umbral de capital que activa el escalado de lotes basado en capital. | `600` |
| `EquityStep` | Incremento de capital necesario para aumentar el tamaño de la operación. | `300` |
| `InitialStepVolume` | Tamaño del lote utilizado cuando el capital es al menos `InitialEquity`. | `0.4` |
| `VolumeStep` | Se agregó un tamaño de lote adicional por cada `EquityStep` alcanzado. | `0.2` |
| `CandleType` | Tipo de datos de vela que impulsa la estrategia (cada hora de forma predeterminada). | `1 hour time-frame` |

## Gestión monetaria

- Cuando `FixedVolume` es mayor que cero, la estrategia siempre utiliza el tamaño de lote fijo.
- Cuando `FixedVolume` es igual a cero, la estrategia inspecciona el capital de la cartera:
  - Si el capital es inferior a `InitialEquity`, se utiliza el lote mínimo del instrumento.
  - De lo contrario, el volumen comienza en `InitialStepVolume` y aumenta en `VolumeStep` por cada `EquityStep` de capital adicional.
  - El volumen final está alineado con las restricciones mínimas y de paso del instrumento.

## Gestión del riesgo

`StartProtection` se activa durante `OnStarted`. Las distancias de stop-loss y take-profit se traducen automáticamente de puntos a compensaciones de precios utilizando el instrumento `PriceStep`. Establezca cualquiera de las distancias en cero para desactivar ese componente.

## Notas de uso

- El EA original está diseñado para velas por horas. Los períodos de tiempo más bajos pueden producir múltiples velas de lunes con la misma hora. El puerto conserva el comportamiento de entrada única por vela y seguirá ignorando señales adicionales mientras una posición esté abierta.
- Asegúrese de que la información de la cartera (`Portfolio.CurrentValue`) esté disponible si el bloque de tamaño dinámico está habilitado.
- La estrategia requiere datos de nivel 1 para ejecutar órdenes de mercado y la suscripción de vela correspondiente para el `CandleType` configurado.

## Notas de conversión

- El filtrado de números mágicos de MQL se reemplaza con verificaciones de posición y orden de StockSharp (`Position` y `ActiveOrders`).
- Las comparaciones de tiempo aprovechan `DateTimeOffset` del tiempo de apertura de la vela con `.ToLocalTime()` para mantenerse alineado con el tiempo del gráfico.
- Las órdenes de protección las maneja el ayudante de alto nivel `StartProtection` en lugar de realizarlas manualmente.
