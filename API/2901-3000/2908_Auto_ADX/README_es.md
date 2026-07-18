# Estrategia Auto ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Auto ADX** es un port directo del asesor experto de MetaTrader `Auto ADX.mq5` a la API de alto nivel de StockSharp. La estrategia evalúa la fortaleza del Índice Direccional Promedio (ADX) y la relación entre los componentes +DI y -DI para determinar la dirección del trade. Reproduce los controles de riesgo originales, incluyendo stop-loss, take-profit, señales reversibles y trailing stops basados en pips, mientras adopta conceptos de StockSharp como suscripciones de velas y vínculos de indicadores.

## Lógica de Trading
- **Fuente de Velas** – La estrategia se suscribe a un tipo de vela configurable (predeterminado: marco temporal de 1 hora) y procesa solo las velas terminadas para evitar el ruido intrabarra.
- **Cálculo ADX** – Un único indicador `AverageDirectionalIndex` se vincula a través de `BindEx`, dando acceso al valor ADX suavizado así como a las líneas +DI y -DI.
- **Entrada Larga** – Activada cuando:
  - +DI es mayor que -DI (momentum direccional positivo),
  - ADX está por encima del nivel ADX configurable, y
  - ADX está subiendo comparado con la vela anterior.
- **Entrada Corta** – Activada cuando:
  - -DI es mayor que +DI (momentum direccional negativo),
  - ADX está por debajo del nivel configurado, y
  - ADX está bajando versus la vela anterior.
- **Modo Inverso** – Cuando `ReverseSignals` está habilitado (comportamiento predeterminado), las posiciones abiertas se cierran si:
  - Una posición larga ve a +DI caer por debajo de -DI **o** el ADX decrece,
  - Una posición corta ve a +DI subir por encima de -DI **o** el ADX sube.
- **Dimensionamiento de Posición** – Las órdenes se emiten con el `Volume` de la estrategia. El manejo de reversión depende de `ClosePosition()` para salir de toda la exposición antes de que se considere una nueva señal.

## Gestión de Riesgo
- **Stop-Loss / Take-Profit** – Convertidos de entradas en pips a distancias de precio absolutas usando el `PriceStep` del instrumento. El ayudante `StartProtection` de StockSharp coloca las órdenes protectoras con ejecución de mercado opcional.
- **Trailing Stop** – La lógica original de trailing basada en pips se replica:
  - El trailing se activa solo después de que la ganancia no realizada supera la distancia de trailing.
  - El nivel de stop se mueve en pasos de tamaño pip (`TrailingStepPips`).
  - Una posición larga sale si el precio imprime por debajo del trailing stop; una corta sale cuando el precio sube por encima del trailing stop.
- **Conversión de Pip** – Para imitar la implementación MQL, el tamaño del pip es igual a `PriceStep`, multiplicado por 10 cuando el instrumento usa precios de 3 o 5 decimales. Esto mantiene el comportamiento consistente entre símbolos forex.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `StopLossPips` | 50 | Distancia del stop protector en pips. Establecer en cero para deshabilitar el stop-loss. |
| `TakeProfitPips` | 50 | Distancia del objetivo de ganancia en pips. Establecer en cero para deshabilitar el take-profit. |
| `TrailingStopPips` | 5 | Tamaño del trailing stop en pips. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | 5 | Ganancia incremental mínima (en pips) antes de desplazar el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `AdxPeriod` | 14 | Período de promedio para el indicador ADX. |
| `AdxLevel` | 30 | Umbral de fortaleza ADX que filtra las entradas. |
| `ReverseSignals` | true | Habilita el cierre de posiciones existentes cuando la relación DI o la pendiente ADX cambia. |
| `CandleType` | 1 hora | Tipo de vela usado para análisis y trading. |

## Notas de Implementación
- `BindEx` se usa para acceder al `AverageDirectionalIndexValue` completo, asegurando que nunca dependemos de la recuperación manual de valores de indicadores.
- La lógica de trailing lleva registro del último nivel de stop y lo mueve solo cuando el precio progresa por al menos `TrailingStepPips` a favor de la posición, replicando el comportamiento de paso de trailing MQL.
- Todos los comentarios en línea en el código fuente C# están en inglés para satisfacer las pautas del repositorio.
- La estrategia es autónoma dentro de `API/2908_Auto_ADX/CS/AutoAdxStrategy.cs`; no hay contraparte Python según los requisitos.

## Consejos de Uso
1. Adjuntar la estrategia a un instrumento con metadatos `PriceStep` correctos para que la conversión de pips siga siendo precisa.
2. Ajustar `AdxLevel` para que coincida con el perfil de volatilidad del instrumento negociado — umbrales más altos reducen la frecuencia de señales.
3. Cuando el trailing está deshabilitado (`TrailingStopPips = 0`), `TrailingStepPips` se ignora, reproduciendo el comportamiento del asesor experto original.
4. Hacer backtesting en múltiples mercados para validar las distancias de protección basadas en pips y confirmar que el filtrado de pendiente ADX coincide con las expectativas.
