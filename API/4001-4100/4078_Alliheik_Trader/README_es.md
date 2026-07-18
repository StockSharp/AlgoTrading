# Estrategia comercial de Alliheik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto MetaTrader 4 **alliheik.mq4**. La estrategia combina un cuerpo de vela Heiken Ashi con doble suavizado con la media móvil de "mandíbula" Alligator desplazada hacia adelante. Las entradas se producen cuando los buffers de Heiken Ashi se cruzan después del proceso de suavizado. Las salidas se basan en un filtro cruzado de mandíbula, objetivos fijos opcionales y un trailing stop basado en el precio.

## Lógica de trading

- **Construcción Heiken Ashi**
  - Suaviza los precios de apertura, máximo, mínimo y cierre sin procesar con `PreSmoothMethod` / `PreSmoothPeriod`.
  - Construya velas Heiken Ashi clásicas a partir de precios suavizados.
  - Intercambie los buffers alto/bajo dependiendo del color de la vela (alcista mantiene orden bajo/alto, bajista los invierte).
  - Aplique una segunda pasada de suavizado (`PostSmoothMethod` / `PostSmoothPeriod`) a los buffers condicionales. Estos son los valores comparados en las reglas de señales.
- **Definición de señal**
  - **Largo**: el búfer inferior actual está debajo del búfer superior, mientras que la barra anterior tenía la relación opuesta o igual.
  - **Corto**: el búfer inferior actual está por encima del búfer superior mientras que la barra anterior tenía la relación opuesta o igual.
- **Filtro de mandíbula y arrastre**
  - La mandíbula Alligator es una media móvil de `JawsPeriod` barras, desplazadas `JawsShift` barras hacia adelante y alimentadas con `JawsPrice`.
  - `Close[6]` (hace seis compases) debe cruzar la mandíbula antes de que la posición pueda cerrarse automáticamente.
  - Una vez que la diferencia entre `Close[6]` y la mandíbula alcanza los ocho puntos y se invierte a través de la mandíbula, la posición se cierra.
  - Si `TrailingStopPoints` es mayor que cero, el precio de parada sigue a `Close[6]` una vez que la vela esté en el lado rentable de la mandíbula.
- **Paradas y objetivos**
  - `StopLossPoints` y `TakeProfitPoints` son distancias fijas opcionales que se aplican al ingresar.
  - La lógica de seguimiento sobrescribe la parada protectora una vez que se mueve a favor de la operación.

## Parámetros predeterminados

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `CandleType` | `TimeSpan.FromHours(1).TimeFrame()` | Marco de tiempo utilizado para todos los cálculos. |
| `JawsPeriod` | 144 | Longitud del promedio móvil de la mandíbula Alligator. |
| `JawsShift` | 8 | Desplazamiento hacia adelante de la mandíbula (número de barras). |
| `JawsMethod` | Sencillo | Tipo de media móvil para la mandíbula (simple, exponencial, suavizada, ponderada). |
| `JawsPrice` | Cerrar | Componente de precio suministrado a la mandíbula (Cerrar/Abrir/Alto/Bajo/Mediano/Típico/Ponderado). |
| `PreSmoothMethod` | exponencial | Promedio móvil utilizado para suavizar los valores OHLC sin procesar antes de calcular Heiken Ashi. |
| `PreSmoothPeriod` | 21 | Periodo de los promedios previos al suavizado. |
| `PostSmoothMethod` | ponderado | Media móvil aplicada a los buffers condicionales de Heiken Ashi. |
| `PostSmoothPeriod` | 1 | Periodo de los promedios post-alisamiento (1 mantiene los buffers originales). |
| `StopLossPoints` | 0 | Distancia de parada fija en puntos (0 inhabilitaciones). |
| `TrailingStopPoints` | 0 | Distancia de trailing stop basada en `Close[6]` (0 inhabilitaciones). |
| `TakeProfitPoints` | 225 | Distancia de toma de ganancias en puntos (0 inhabilitaciones). |
| `OrderVolume` | 0.1 | Tamaño del lote para entradas. |

## Indicadores utilizados

- MA de pre-suavizado (cuatro series paralelas para apertura, máximo, mínimo y cierre).
- Reconstrucción de Heiken Ashi impulsada por la suavización de precios.
- MA post-suavizado de los buffers condicionales que forman la señal de entrada.
- Alligator promedio de movimiento de mandíbula con tipo, desplazamiento y precio aplicado ajustables.

## Resumen de entrada y salida

- **Ingrese Largo** cuando el buffer inferior suavizado cruza por debajo del buffer superior y la barra anterior no era alcista (condición de cruce descrita anteriormente).
- **Salida larga** cuando:
  - `Close[6]` vuelve a caer por debajo de la mandíbula después de haber estado previamente por encima de ella y la distancia alcanzó ≥ 8 puntos; o
  - Se alcanza el objetivo `TakeProfitPoints`; o
  - Se alcanza la parada `StopLossPoints`/`TrailingStopPoints`.
- **Ingrese Corto** cuando el buffer inferior suavizado cruce por encima del buffer superior y la barra anterior no era bajista.
- **Salida corta** cuando:
  - `Close[6]` vuelve a subir por encima de la mandíbula después de haber estado previamente por debajo de ella y la distancia alcanzada ≥ 8 puntos; o
  - Se alcanza el objetivo `TakeProfitPoints`; o
  - Se alcanza la parada `StopLossPoints`/`TrailingStopPoints`.

## Notas de conversión

- La estrategia aplica una operación por barra, reflejando la verificación `isOrderAllowed()` en el EA original.
- Los objetivos y paradas de protección se simulan internamente porque las estrategias StockSharp no pueden depender de las órdenes MT4 del corredor.
- El promedio móvil de la mandíbula almacena valores históricos para que el desplazamiento hacia adelante replique el comportamiento de `iMA` con `ma_shift = JawsShift`.
- Todos los cálculos utilizan aritmética decimal y vinculaciones de indicadores consistentes con los requisitos API de alto nivel de StockSharp.

## Riesgo y uso

- Diseñado para operaciones largas y cortas con el mismo instrumento.
- Funciona mejor en mercados de tendencia donde el cambio de mandíbula y el suavizado Heiken Ashi pueden resaltar cambios a mediano plazo.
- Considere ajustar `TrailingStopPoints` y `TakeProfitPoints` para que coincidan con la volatilidad del instrumento.
- Siempre realice pruebas retrospectivas y directas en cuentas en papel antes de la implementación real.
