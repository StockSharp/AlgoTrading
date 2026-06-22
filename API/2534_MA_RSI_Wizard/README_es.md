# Estrategia MA + RSI Wizard
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es el port de StockSharp del experto de MetaTrader 5 "MQL5 Wizard MA RSI" de la carpeta `MQL/17489`. El robot original combina un filtro de media móvil con un filtro RSI y abre operaciones cuando la suma ponderada de los filtros cruza umbrales configurables. La versión en C# mantiene la misma estructura expresando la lógica con la API de alto nivel de StockSharp y los ayudantes modernos de gestión de riesgo.

El bot funciona en cualquier instrumento que proporcione velas OHLCV. Evalúa una media móvil que puede retrasarse por un número de barras definido por el usuario y un RSI que puede alimentarse con diferentes fuentes de precio. Ambos indicadores contribuyen a una puntuación compuesta. Una posición se abre una vez que la puntuación supera el umbral de apertura y se cierra cuando la puntuación contraria alcanza el umbral de cierre. Las configuraciones opcionales de distancia, stop loss y take profit replican los parámetros de gestión de dinero del Asesor Experto original.

## Indicadores y puntuación

* **Media Móvil** – período configurable, método (simple, exponencial, suavizado, linealmente ponderado), fuente de precio y desplazamiento hacia adelante. Cuando el precio de cierre está por encima de la media desplazada, la puntuación MA es igual a 100, de lo contrario es 0.
* **Índice de Fuerza Relativa (RSI)** – período y fuente de precio configurables. La contribución RSI crece linealmente de 0 cuando RSI = 50 a 100 cuando RSI = 100 para señales largas, y refleja el mismo comportamiento para señales cortas.
* **Puntuación compuesta** – las puntuaciones MA y RSI se ponderan por `MaWeight` y `RsiWeight`. El valor final es la media ponderada `score = (maScore * MaWeight + rsiScore * RsiWeight) / (MaWeight + RsiWeight)` que permanece dentro del intervalo [0;100] como en la versión MetaTrader.
* **Filtro de distancia de precio** – `PriceLevelPoints` define la distancia mínima entre el cierre de la vela y la media móvil desplazada (convertida a precio usando el paso del instrumento). Las señales más cercanas que el umbral se ignoran.

## Reglas de operación

1. Cada vela terminada actualiza los indicadores y puntuaciones.
2. Si la puntuación opuesta supera `ThresholdClose`, la posición actual se cierra a mercado.
3. Entrada larga – permitida cuando no hay exposición larga, la puntuación larga es al menos `ThresholdOpen`, el tiempo de espera (`ExpirationBars`) ha pasado, y se satisface el filtro de distancia de precio. El tamaño de la orden es `Volume + |Position|`, lo que automáticamente cambia una posición corta si es necesario.
4. Entrada corta – simétrica a la lógica larga.
5. `StartProtection` opcional aplica stop loss y take profit usando puntos absolutos de precio.

## Gestión de riesgo

La estrategia activa `StartProtection` una vez que comienza. Las distancias se definen en puntos de precio (`StopLevelPoints`, `TakeLevelPoints`) y se traducen con el `Security.PriceStep` actual. Ambos valores pueden establecerse en cero para deshabilitar la protección correspondiente. El parámetro de tiempo de espera evita re-entradas inmediatas en la misma dirección, emulando la configuración de vencimiento de órdenes pendientes del EA original.

## Parámetros

| Parámetro | Descripción | Valor predeterminado |
|-----------|-------------|---------|
| `CandleType` | Serie de datos usada para el análisis. | Marco temporal de 15 minutos |
| `ThresholdOpen` | Puntuación ponderada mínima para abrir una posición. | 55 |
| `ThresholdClose` | Puntuación opuesta mínima para cerrar una posición. | 100 |
| `PriceLevelPoints` | Distancia requerida entre precio y MA desplazada (en puntos). | 0 |
| `StopLevelPoints` | Distancia del stop loss (puntos). | 50 |
| `TakeLevelPoints` | Distancia del take profit (puntos). | 50 |
| `ExpirationBars` | Tiempo de espera en barras antes de re-entrar en la misma dirección. | 4 |
| `MaPeriod` | Período de la media móvil. | 20 |
| `MaShift` | Desplazamiento hacia adelante aplicado a la salida de la MA (barras). | 3 |
| `MaMethods` | Método de media móvil (Simple, Exponencial, Suavizado, LinearWeighted). | Simple |
| `MaAppliedPrice` | Fuente de precio para la MA. | Close |
| `MaWeight` | Peso asignado a la puntuación MA. | 0.8 |
| `RsiPeriod` | Período RSI. | 3 |
| `RsiAppliedPrice` | Fuente de precio para el RSI. | Close |
| `RsiWeight` | Peso asignado a la puntuación RSI. | 0.5 |

## Notas

* La estrategia funciona estrictamente en velas terminadas e ignora actualizaciones parciales.
* Establecer ambos pesos de indicador en cero deshabilita el trading porque la puntuación combinada ya no puede alcanzar los umbrales.
* El tiempo de espera (`ExpirationBars`) igual a cero permite múltiples entradas en la misma dirección sin esperar.
* Porque StockSharp ejecuta órdenes de mercado por defecto, la expiración de órdenes pendientes del EA original se representa por el mecanismo de tiempo de espera en lugar de la cancelación real de órdenes.
