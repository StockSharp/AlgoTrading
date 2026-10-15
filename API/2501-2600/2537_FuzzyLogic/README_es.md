# Estrategia FuzzyLogic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia FuzzyLogic replica el asesor experto de MT5 **Fuzzy logic (edición de barabashkakvn)** utilizando el API de alto nivel de StockSharp. El sistema mide la fuerza de la tendencia y el agotamiento del momentum con osciladores de Bill Williams e indicadores de momentum, convierte esas lecturas en grados de pertenencia difusa y los agrega en una puntuación de decisión única entre 0 y 1.

Las acciones de trading se activan cuando la puntuación difusa cruza umbrales calibrados:

- **Decision &gt; 0.75** – abrir una posición corta (agotamiento fuerte / condiciones de sobrecompra).
- **Decision &lt; 0.25** – abrir una posición larga (configuración de reversión alcista fuerte).

Las posiciones se gestionan con distancias fijas de toma de ganancias y stop-loss expresadas en pasos de precio. Cuando se suministra una distancia de trailing stop, el stop protector se convierte en uno de seguimiento.

## Conjunto de indicadores

| Componente | Propósito |
| --- | --- |
| **Oscilador Gator** (construido a partir de líneas Alligator) | Mide la suma de los diferenciales entre mandíbula–dientes y dientes–labios para evaluar la expansión o contracción de la tendencia. |
| **Williams %R (14)** | Detecta niveles de sobrecompra / sobreventa. |
| **Acceleration/Deceleration Oscillator (AC)** | Cuenta cambios consecutivos de momentum para estimar la aceleración de la tendencia. |
| **DeMarker (14)** | Confirma el agotamiento mediante comparaciones de máximos/mínimos. Implementado directamente dentro de la estrategia. |
| **RSI (14)** | Rastrea los oscilaciones clásicas de momentum. |

Las líneas Alligator se calculan con medias móviles suavizadas y se desplazan hacia adelante exactamente como en el asesor experto original para reproducir el oscilador Gator. Los valores de AC se derivan del Awesome Oscillator (diferencia SMA 5/34) menos su media móvil de 5 períodos, proporcionando lecturas idénticas al indicador `iAC` de MT5.

## Lógica de trading

1. El valor de cada indicador se mapea a cinco conjuntos de pertenencia difusa (muy bajista → muy alcista). Las funciones lineales a tramos replican los arrays originales de MT5.
2. Los cinco grupos de pertenencia se ponderan (0.133, 0.133, 0.133, 0.268, 0.333) y se agregan en cuatro contenedores de resumen.
3. La puntuación de decisión difusa se calcula como `Σ summary[x] * (0.2 * (x + 1) - 0.1)`, produciendo valores en el rango `[0, 1]`.
4. Las señales se evalúan una vez por vela cerrada. La estrategia permanece sin posición a menos que la decisión supere los umbrales de entrada.
5. El tamaño de la orden depende de la propiedad `Volume` (predeterminado 1). Los stops protectores se registran a través de `StartProtection`.

## Gestión de riesgo

- **StopLossPoints** – distancia absoluta (en pasos de precio) para el stop protector. Se usa cuando `TrailingStopPoints` es cero.
- **TrailingStopPoints** – si &gt; 0, la distancia del stop-loss cambia a este valor y se activa el modo de seguimiento.
- **TakeProfitPoints** – distancia absoluta para el objetivo de ganancia.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal / tipo de vela utilizado para los cálculos. |
| `BuyThreshold` | Puntuación difusa por debajo de la cual se abre una entrada larga. Predeterminado 0.25. |
| `SellThreshold` | Puntuación difusa por encima de la cual se abre una entrada corta. Predeterminado 0.75. |
| `StopLossPoints` | Distancia del stop-loss en pasos de precio del instrumento. Predeterminado 60. |
| `TakeProfitPoints` | Distancia de toma de ganancias en pasos de precio. Predeterminado 20. |
| `TrailingStopPoints` | Distancia de trailing stop en pasos de precio. Predeterminado 0 (desactivado). |
| `WilliamsPeriod` | Período de lookback para Williams %R. Predeterminado 14. |
| `RsiPeriod` | Período de lookback para RSI. Predeterminado 14. |
| `DeMarkerPeriod` | Período de lookback para el cálculo integrado de DeMarker. Predeterminado 14. |

## Notas de implementación

- El oscilador DeMarker se implementa manualmente porque StockSharp no expone una versión integrada. Los deltas de máximos y mínimos se almacenan en cola para reproducir las sumas de MT5.
- El historial de AC almacena los cinco valores completados más recientes para que la lógica difusa pueda verificar rachas de aceleración consecutivas al igual que `iAC(..., shift)` en MT5.
- Los búferes de mandíbula/dientes/labios del Alligator introducen el mismo desplazamiento hacia adelante (8/5/3 barras) antes de derivar los valores del histograma Gator.
- La estrategia solo abre una nueva posición cuando `Position == 0`, respetando el comportamiento de posición única del asesor experto original.

## Pasos de uso

1. Adjunte la estrategia a una cartera y un valor en Designer/Backtester.
2. Configure la serie de velas deseada a través de `CandleType`.
3. Ajuste los umbrales o las distancias de stop si es necesario.
4. Inicie la estrategia; operará automáticamente cuando la puntuación difusa cruce los niveles configurados.
