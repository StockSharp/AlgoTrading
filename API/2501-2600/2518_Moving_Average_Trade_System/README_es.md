# Estrategia del Sistema de Trading de Medias Móviles (2518)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un port de StockSharp del asesor experto de MetaTrader "Moving Average Trade System". Analiza la tendencia usando cuatro medias móviles simples (SMA) calculadas sobre el precio mediano de la vela. El sistema espera un cruce confirmado entre las medias a medio y largo plazo mientras las medias más rápidas confirman la alineación de la tendencia. Una vez llega la confirmación, la estrategia invierte su posición en la dirección de la nueva tendencia y gestiona el riesgo con stop-loss fijo, toma de ganancias y offsets de trailing stop definidos en pasos de precio.

## Lógica de Trading

1. **Indicadores**
   - `SMA(5)` (rápida) sobre precio mediano.
   - `SMA(20)` (media) sobre precio mediano.
   - `SMA(40)` (señal) sobre precio mediano.
   - `SMA(60)` (lenta) sobre precio mediano.

2. **Entrada larga**
   - `SMA(5) > SMA(20) > SMA(40)`.
   - `SMA(40)` está por encima de `SMA(60)` al menos `SlopeThresholdSteps` pasos de precio.
   - `SMA(40)` cruzó por encima de `SMA(60)` en la barra actual (la `SMA(40)` anterior estaba por debajo o igual a la SMA lenta).
   - Si hay una posición corta abierta, la estrategia compra suficiente volumen para cerrarla y establecer el tamaño largo deseado.

3. **Entrada corta**
   - `SMA(5) < SMA(20) < SMA(40)`.
   - `SMA(40)` está por debajo de `SMA(60)` al menos `SlopeThresholdSteps` pasos de precio.
   - `SMA(40)` cruzó por debajo de `SMA(60)` en la barra actual (la `SMA(40)` anterior estaba por encima o igual a la SMA lenta).
   - Si hay una posición larga abierta, la estrategia vende suficiente volumen para cerrarla y establecer el tamaño corto deseado.

4. **Gestión de riesgos** (evaluada solo cuando no se activa nueva entrada en la barra):
   - **Salida por tendencia:** cerrar largos cuando `SMA(40) <= SMA(60)` y cerrar cortos cuando `SMA(40) >= SMA(60)`.
   - **Toma de ganancias:** salir una vez que el precio alcanza la distancia de toma de ganancias configurada desde el precio de entrada.
   - **Stop-loss:** salir si el precio se mueve contra la posición la distancia de stop-loss configurada.
   - **Trailing stop:** una vez que el precio avanza más allá de la entrada, seguir el stop de protección por `TrailingStopSteps` pasos de precio usando el máximo más alto (para largos) o el mínimo más bajo (para cortos) desde la entrada.

Todos los offsets de stop y ganancia se miden en **pasos de precio** (el `PriceStep` del instrumento). Si el instrumento no reporta un paso de precio, se usa un valor de `1` como fallback.

## Parámetros

| Nombre | Descripción | Valor predeterminado | Optimizable |
| --- | --- | --- | --- |
| `Volume` | Volumen de orden utilizado al abrir nuevas posiciones. | `1` | No |
| `TakeProfitSteps` | Distancia al objetivo de toma de ganancias medida en pasos de precio. | `50` | Sí |
| `StopLossSteps` | Distancia al stop de protección medida en pasos de precio. | `50` | Sí |
| `TrailingStopSteps` | Offset del trailing stop en pasos de precio (`0` deshabilita el trailing). | `11` | Sí |
| `SlopeThresholdSteps` | Separación mínima entre `SMA(40)` y `SMA(60)` para validar un rompimiento (en pasos de precio). | `1` | Sí |
| `FastPeriod` | Longitud de la SMA rápida. | `5` | Sí |
| `MediumPeriod` | Longitud de la SMA media. | `20` | Sí |
| `SignalPeriod` | Longitud de la SMA de señal (comparada con la SMA lenta). | `40` | Sí |
| `SlowPeriod` | Longitud de la SMA lenta que define la tendencia de fondo. | `60` | Sí |
| `CandleType` | Serie de velas usada para los cálculos del indicador. | `Marco temporal de 1h` | No |

## Notas de Implementación

- Los indicadores están vinculados a la suscripción de velas a través de la API de alto nivel `Bind`, asegurando que los cálculos sean dirigidos por eventos y no dependan del acceso manual al buffer.
- El precio mediano se usa para todos los cálculos de SMA, replicando el comportamiento del EA original de MetaTrader.
- La gestión de posiciones almacena el precio de llenado real usando `OnNewMyTrade` para recalcular los niveles de stop-loss, toma de ganancias y trailing stop después de cada llenado.
- Al invertir una posición, la estrategia envía una única orden de mercado que cierra la exposición existente y abre la nueva, imitando el comportamiento compatible con cobertura del algoritmo original.
- Todos los comentarios dentro del archivo fuente C# están escritos en inglés, según lo requieren las pautas del repositorio.

## Consejos de Uso

- Configure el parámetro `Volume` según el tamaño del lote del instrumento o el multiplicador del contrato.
- Ajuste las distancias de stop y ganancia para que coincidan con la volatilidad del instrumento (los valores predeterminados reflejan la configuración de MetaTrader de 50 pips de stop/toma de ganancias y 11 pips de trailing stop en pares de divisas).
- El parámetro `SlopeThresholdSteps` puede establecerse en `0` para eliminar el filtro de espaciado adicional y reaccionar a cualquier cruce de `SMA(40)`/`SMA(60)`.
- Para backtesting o trading en vivo, asegúrese de que el instrumento proporcione un `PriceStep` válido; de lo contrario, la estrategia tratará una unidad de precio como un solo paso.
