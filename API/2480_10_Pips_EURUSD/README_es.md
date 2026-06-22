# Estrategia de 10 Pips EURUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de 10 Pips EURUSD** es un sistema de ruptura que reproduce la lógica del Expert Advisor original de MetaTrader. Observa la vela completada más reciente y coloca órdenes stop por encima y por debajo de ese rango. Las órdenes se dimensionan en pips, ajustadas al tamaño de tick del instrumento actual, y opcionalmente gestionadas por un trailing stop. La implementación usa suscripciones de velas de alto nivel de StockSharp junto con actualizaciones del libro de órdenes para mantener el comportamiento cercano a la versión MQL mientras permanece neutral con el broker.

## Lógica de la estrategia
1. Suscribirse al tipo de vela seleccionado y esperar hasta que una nueva barra se active.
2. Capturar el máximo y mínimo de la vela anterior cuando esa barra termina. Las órdenes pendientes se cancelan en este momento porque el EA original las limita a una barra.
3. En el primer tick de la nueva barra verificar que:
   - La apertura actual se encuentra dentro del rango de la vela anterior (filtrado de gaps).
   - El precio actual está al menos tres unidades pip alejado de ambos extremos (un indicador del nivel de stop del broker).
4. Calcular el spread actual usando el mejor bid/ask. Si no hay datos de nivel 1, la estrategia recurre al tamaño del pip.
5. Colocar dos órdenes stop pendientes:
   - **Buy Stop**: activación en `máximo anterior + 2 × spread` con stop loss por debajo del precio de entrada en `StopLossPips` y, si el trailing está desactivado, take profit en `máximo anterior + 2 × spread + TakeProfitPips`.
   - **Sell Stop**: activación en `mínimo anterior − spread` con niveles de salida simétricos.
6. Tan pronto como la vela se completa, o ambas órdenes se llenan/cancelan, el proceso se repite para la siguiente barra.

### Gestión de posición
- Mientras una posición está abierta, la estrategia monitorea el mejor bid/ask en cada actualización del libro de órdenes.
- Si el trailing está desactivado, la posición se cierra cuando el precio toca el stop o take-profit fijo.
- Si el trailing está habilitado:
  - Para operaciones largas, el trailing stop se activa una vez que el precio avanza `TrailingStopPips`. El stop se establece en `bid − TrailingStopPips` y se mueve cada vez que el precio mejora al menos `TrailingStepPips`.
  - Para operaciones cortas, la lógica refleja el lado largo usando el precio ask.
- Las salidas manuales reinician todos los niveles de protección y mantienen viva cualquier orden stop del lado contrario pendiente hasta que la vela termine, reproduciendo el comportamiento straddle del EA.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `0.01` | Volumen de la orden en lotes (o unidades de contrato para símbolos no FX). |
| `StopLossPips` | `50` | Distancia entre la entrada y el stop de protección, expresada en pips. |
| `TakeProfitPips` | `150` | Distancia al take-profit en pips, usada solo cuando el trailing está desactivado. |
| `UseTrailing` | `false` | Habilita la lógica del trailing stop. |
| `TrailingStopPips` | `50` | Distancia inicial para el trailing stop, medida en pips. |
| `TrailingStepPips` | `25` | Ganancia mínima (en pips) requerida para mover un trailing stop activo. |
| `CandleType` | `marco temporal de 15 minutos` | Serie de velas usada para detectar los niveles de ruptura. |

## Notas y recomendaciones
- El tamaño del pip se deriva automáticamente de `Security.PriceStep` y emula el ajuste de dígitos MQL, por lo que la estrategia se adapta a símbolos FX de 3 y 5 dígitos.
- Todas las distancias se recalculan en unidades de precio antes de colocar órdenes, lo que mantiene la estrategia compatible con activos no FX siempre que el tamaño del tick esté definido.
- El fallback del nivel de stop mínimo (tres unidades pip) imita el comportamiento del EA original cuando el broker no reporta un nivel de stop.
- Como las órdenes pendientes expiran al final de cada vela, deberías ejecutar la estrategia en el marco temporal deseado sin gaps en el flujo de velas entrante.
- La gestión del riesgo es crucial. Considera probar con spreads realistas y modelos de comisión antes de operar con capital real.
