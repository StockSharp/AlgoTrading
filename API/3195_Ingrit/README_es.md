# Estrategia Ingrit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Ingrit es una conversión del asesor experto de MetaTrader 5 `Ingrit.mq5`. La estrategia observa velas de cinco minutos y reacciona cuando una vela fuerte a contratendencia es seguida por un amplio breakout medido contra un swing de catorce barras atrás. Las órdenes se colocan a mercado con distancias configurables de stop-loss, take-profit y trailing stop expresadas en pips. Las señales pueden invertirse opcionalmente o forzarse a aplanar la exposición opuesta antes de entrar en una nueva operación.

## Lógica de la estrategia
### Detección de breakout
* La estrategia procesa únicamente velas finalizadas del marco temporal seleccionado (por defecto 5 minutos).
* Para una configuración **long**, la vela anterior debe cerrar bajista y la distancia entre el máximo de la vela 14 barras atrás y el mínimo de la vela anterior debe superar `StepPips` (tras convertir pips a unidades de precio).
* Para una configuración **short**, la vela anterior debe cerrar alcista y la distancia entre el máximo de la vela anterior y el mínimo de la vela 14 barras atrás debe superar `StepPips`.
* Habilitar `ReverseSignals` intercambia las condiciones long y short, recreando el modo de reversión opcional del robot original.

### Gestión de operaciones
* Las órdenes de mercado se envían usando el `Volume` de la estrategia. Cuando `CloseOppositePositions` está habilitado, el tamaño solicitado se incrementa en el valor absoluto de la posición actual para que las reversiones cierren la exposición existente en la misma operación.
* Un stop-loss fijo y take-profit (si es mayor que cero) se adjuntan inmediatamente después de la entrada. Ambas distancias se convierten desde pips usando el paso de precio del instrumento y se adaptan automáticamente a cotizaciones FX de tres y cinco dígitos.
* El trailing stop se activa una vez que el beneficio no realizado supera `TrailingStopPips + TrailingStepPips`. Para posiciones long el stop sigue por debajo del cierre; para posiciones short sigue por encima del cierre. Cada actualización mantiene el stop al menos `TrailingStepPips` lejos del nivel de trailing anterior para evitar modificaciones rápidas.

### Comportamiento adicional
* El trailing puede desactivarse estableciendo `TrailingStopPips` en cero. Si el trailing está activo, el paso debe permanecer positivo (la estrategia realiza la misma validación que la versión MQL).
* Todos los cálculos se ejecutan sobre velas completadas; no se requiere procesamiento intrabar en StockSharp.
* La estrategia no crea órdenes pendientes: cada señal se ejecuta con una orden de mercado y los niveles de protección se simulan internamente.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal utilizado para construir velas para la lógica de breakout. Por defecto: marco temporal de 5 minutos. |
| `StopLossPips` | Distancia del stop-loss en pips. Un valor de `0` deshabilita el stop fijo. |
| `TakeProfitPips` | Distancia del take-profit en pips. Un valor de `0` deshabilita el objetivo fijo. |
| `TrailingStopPips` | Distancia base del trailing stop en pips. Establecer en `0` para deshabilitar el trailing. |
| `TrailingStepPips` | Distancia extra en pips que debe ganarse antes de que el trailing stop se mueva de nuevo. Debe ser positivo cuando el trailing está habilitado. |
| `StepPips` | Distancia mínima de swing en pips entre la vela de referencia y la última vela antes de que se active una señal. |
| `ReverseSignals` | Si es `true`, intercambia las condiciones long y short (modo de reversión). |
| `CloseOppositePositions` | Si es `true`, amplía la orden de mercado para aplanar cualquier exposición opuesta antes de abrir la nueva posición. |
| `Volume` | Propiedad de la estrategia que define el tamaño base de la orden. Combinar con `CloseOppositePositions` para controlar el comportamiento de reversión. |

## Notas
* Los valores de pip se derivan del paso de precio del instrumento. Cuando el instrumento usa tres o cinco decimales, la estrategia multiplica el paso por diez para que un pip sea igual a la definición estándar de FX.
* No hay una versión en Python para esta estrategia en el repositorio.
