# Estrategia Universal MA Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Universal MA Cross** es una conversión directa del asesor experto MQL5 original "UniversalMACrossEA" al framework de estrategias de alto nivel de StockSharp. El algoritmo compara una media móvil rápida y una lenta que pueden configurarse con diferentes métodos de cálculo y fuentes de precio. Filtros opcionales controlan cómo se confirman las señales, si las operaciones se revierten inmediatamente, cómo se realiza la gestión del riesgo y cuándo se permite operar a la estrategia.

## Lógica de trading
### Procesamiento de indicadores
* Se calculan dos medias móviles sobre la serie de velas seleccionada. Cada media puede usar su propio período, método de suavizado (SMA, EMA, SMMA o LWMA) y tipo de precio (cierre, apertura, máximo, mínimo, mediana, típico o ponderado).
* El parámetro **MinCrossDistance** requiere que las medias rápida y lenta diverjan al menos el número especificado de unidades de precio en la barra de cruce.
* Cuando **ConfirmedOnEntry** está habilitado, el cruce se valida en la barra completada anterior (equivalente a usar índices de barra 2 y 1 en el EA original). Si está deshabilitado, la barra finalizada actual se compara con la barra anterior, replicando el comportamiento del "modo tick" de la versión MQL.
* Configurar **ReverseCondition** intercambia las señales alcistas y bajistas para que las reglas puedan invertirse sin cambiar ninguna configuración del indicador.

### Reglas de entrada
1. Para una entrada larga, la media rápida debe cruzar por encima de la media lenta al menos **MinCrossDistance**. Para una entrada corta, la media rápida debe cruzar por debajo de la media lenta esa distancia.
2. Cuando **StopAndReverse** está habilitado y llega una señal opuesta, la posición activa se cierra antes de considerar nuevas órdenes.
3. Si **OneEntryPerBar** es verdadero, la estrategia recuerda el tiempo de barra de la última entrada y rechaza abrir otra operación durante la misma vela.
4. El volumen de cada orden se configura mediante el parámetro **Volume**.

### Gestión de posiciones
* Los niveles de stop-loss y take-profit se miden en unidades de precio. Se ignoran cuando **PureSar** es verdadero, coincidiendo con el modo "Pure SAR" del experto original.
* La lógica de trailing stop se activa después de que el precio se mueve **TrailingStop + TrailingStep** desde el precio de entrada. Cada movimiento adicional de al menos **TrailingStep** puntos ajusta el stop en la distancia **TrailingStop** especificada. El trailing no funciona en modo "Pure SAR".
* Los niveles de protección se monitorean en cada vela finalizada. Si el rango de la vela viola el nivel de stop-loss o take-profit, la posición se cierra por orden de mercado.

### Filtro de sesión
* Cuando **UseHourTrade** está habilitado, la estrategia opera solo cuando la hora de apertura de la vela está entre **StartHour** y **EndHour** (inclusive). La gestión del trailing stop continúa ejecutándose fuera de ese intervalo, pero no se ejecutan nuevas entradas ni acciones de stop-and-reverse.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `FastMaPeriod`, `SlowMaPeriod` | Períodos de las medias móviles rápida y lenta. |
| `FastMaType`, `SlowMaType` | Métodos de media móvil: Simple, Exponencial, Suavizada (RMA) o Ponderada Lineal. |
| `FastPriceType`, `SlowPriceType` | Fuentes de precio alimentadas en las medias. |
| `StopLoss`, `TakeProfit` | Distancias de protección en unidades de precio absolutas. Establecer en 0 para deshabilitar. |
| `TrailingStop`, `TrailingStep` | Desplazamiento del trailing stop y movimiento extra mínimo requerido antes de desplazar el stop. |
| `MinCrossDistance` | Distancia mínima entre las medias en la barra de cruce. |
| `ReverseCondition` | Intercambiar reglas alcistas y bajistas. |
| `ConfirmedOnEntry` | Usar solo barras completadas para validación. |
| `OneEntryPerBar` | Permitir como máximo una entrada por vela. |
| `StopAndReverse` | Cerrar la posición actual y revertir en señales opuestas. |
| `PureSar` | Deshabilitar la lógica de stop-loss, take-profit y trailing. |
| `UseHourTrade`, `StartHour`, `EndHour` | Filtro de tiempo para sesiones de trading (horas 0–23). |
| `Volume` | Volumen de orden para cada posición. |
| `CandleType` | Tipo de datos de velas suscrito para cálculos. |

## Notas de conversión
* Las órdenes de protección se manejan internamente verificando los máximos y mínimos de las velas, porque las estrategias de StockSharp operan sobre velas finalizadas en lugar de eventos de tick sin procesar. Esto refleja el comportamiento del experto original mientras se mantiene dentro de la API de alto nivel.
* Los ajustes del trailing stop siguen la implementación MQL, requiriendo un movimiento de **TrailingStop + TrailingStep** antes de que el stop se desplace.
* No se proporciona versión en Python en esta conversión según lo solicitado.
