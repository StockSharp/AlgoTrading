# Estrategia de Rompimiento MA Martingale (ID 2861)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Martingale MA Breakout es un puerto del asesor experto original de MetaTrader 5 `Martingale.mq5`. Monitorea cuánto se aleja el precio actual de una media móvil trazada en un marco temporal superior. Cuando la distancia supera un número configurable de pips, la estrategia abre una nueva posición en la dirección del movimiento y la gestiona con lógica fija de stop-loss, take-profit y trailing. El dimensionamiento de posición sigue un ajuste estilo martingale que aumenta el tamaño de la operación después de secuencias perdedoras y lo reduce después de períodos rentables.

Por defecto la estrategia evalúa velas de 6 minutos mientras que la plataforma circundante puede operar en cualquier marco temporal base. Todos los cálculos de indicadores se realizan en el tipo de vela seleccionado, mientras que las órdenes se envían usando ejecución de mercado.

## Lógica de trading

1. Calcular el valor de la media móvil para la vela actual usando el método de suavizado, precio aplicado y desplazamiento seleccionados.
2. Transformar la distancia configurada en pips en un delta de precio absoluto. El tamaño de pip replica el ajuste original de MQL: los símbolos con 3 o 5 decimales multiplican el paso de precio por 10.
3. Cuando la vela cierra:
   - Si el cierre está más de `DistanceFromMaPips` pips por encima de la media móvil desplazada y no hay exposición larga activa, enviar una orden de compra de mercado.
   - Si el cierre está más de `DistanceFromMaPips` pips por debajo de la media móvil desplazada y no hay exposición corta activa, enviar una orden de venta de mercado.
4. Cada vela terminada también actualiza el trailing stop y verifica si el precio de cierre viola el stop-loss o take-profit simulado. Cerrar una posición activa `ResetTradeState`, limpiando todos los niveles almacenados.

## Gestión de capital

- `RiskPercent` convierte en un presupuesto de riesgo monetario usando `Portfolio.CurrentValue` (o `BeginValue` si no se han realizado operaciones). Cuando se especifica un stop-loss, el presupuesto dividido por la distancia del stop y el multiplicador de seguridad estima el volumen máximo asequible.
- Después del dimensionamiento por riesgo, el volumen pasa por `ApplyMartingale`: si el último balance registrado (capturado después de la entrada anterior) es mayor que el balance actual, el volumen aumenta en 1 unidad; si es menor, el volumen disminuye en 1 unidad pero nunca cae por debajo del volumen base de la estrategia.
- La lógica de trailing imita el EA original: una vez que el precio se mueve por `TrailingStopPips + TrailingStepPips` a favor de la posición, el stop se ajusta para mantener el offset de `TrailingStopPips`. La estrategia valida que `TrailingStepPips` sea diferente de cero cuando el trailing está habilitado, reflejando el manejo de errores de MQL.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StopLossPips` | Distancia de stop-loss expresada en pips. Un valor de cero desactiva el stop y el dimensionamiento basado en riesgo. |
| `TakeProfitPips` | Distancia de take-profit en pips. Cero para desactivar. |
| `TrailingStopPips` | Offset del trailing stop en pips. Debe combinarse con `TrailingStepPips`. |
| `TrailingStepPips` | Movimiento de precio adicional requerido antes de que avance el trailing stop. No puede ser cero cuando el trailing está activo. |
| `DistanceFromMaPips` | Distancia mínima entre precio y media móvil desplazada que activa entradas. |
| `CandleType` | Tipo de datos para cálculos de indicadores (por defecto marco temporal de 6 minutos). |
| `MaPeriod` | Período de la media móvil. |
| `MaShift` | Número de barras que la media móvil está desplazada hacia adelante. La estrategia almacena valores históricos de MA para emular el comportamiento de MQL. |
| `MaMethod` | Tipo de suavizado de media móvil: Simple, Exponencial, Suavizado o Ponderado. |
| `MaAppliedPrice` | Precio de vela usado para la media móvil (cierre, apertura, máximo, mínimo, mediano, típico o ponderado). |
| `RiskPercent` | Porcentaje del capital actual asignado al presupuesto de riesgo del stop-loss. |

## Notas de ejecución

- La estrategia funciona exclusivamente en velas terminadas para replicar el procesamiento de "nueva barra" del EA original. `BuyMarket`/`SellMarket` volcará la exposición existente añadiendo el valor absoluto de la posición opuesta.
- Los stops y objetivos se simulan en código porque StockSharp no los gestiona automáticamente en esta conversión. El precio de cierre se usa como proxy para la ejecución a nivel de tick.
- Los ajustes de martingale operan en la instantánea del balance de la cuenta tomada inmediatamente después de cada entrada, similar al EA fuente.
- Si el símbolo carece de un paso de precio o multiplicador válido, se usan valores predeterminados de `0.0001` y `1` para evitar errores de división.

## Diferencias del EA original

- La versión MQL usaba precios bid/ask; este puerto trabaja con precios de cierre de velas porque los ticks de alta frecuencia no están disponibles en la API de alto nivel.
- El dimensionamiento de volumen depende de la equidad del portafolio y el multiplicador de seguridad en lugar del helper `CMoneyFixedMargin`.
- La visualización de gráficos es opcional: cuando hay un área de gráfico disponible, la estrategia dibuja velas; no se plotean indicadores adicionales por defecto.
- La validación de que `TrailingStepPips` debe ser positivo cuando el trailing está habilitado arroja una excepción durante el inicio en lugar de llamar a `Alert`.
