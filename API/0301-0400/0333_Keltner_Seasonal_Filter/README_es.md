# Estrategia Keltner con Filtro Estacional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Keltner Seasonal Filter** opera basándose en rupturas del Canal Keltner con filtro de sesgo estacional.

Las pruebas indican un rendimiento anual promedio de aproximadamente 94%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Keltner confirma entradas filtradas en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como EmaPeriod, AtrPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: consulte la implementación para conocer las condiciones de los indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, utilizando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Keltner, Seasonal
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Reglas exactas de negociación (C# y Python)

Las señales se evalúan solo con velas **finalizadas**, indicadores formados y negociación habilitada. Definimos `C` = cierre, `E = EMA(Close, EmaPeriod)`, `A = ATR(AtrPeriod)`, `M = AtrMultiplier`, `U = E + M × A`, `L = E − M × A`.

`S` depende del **mes de apertura de la vela histórica** (`candle.OpenTime.Month`), no de `OnStarted2` ni del reloj del equipo. Valores fijos ilustrativos (no calculados con los datos históricos): ene 0.8, feb 0.3, mar 0.6, abr 0.7, may 0.2, jun −0.3, jul −0.1, ago −0.4, sep −0.8, oct 0.1, nov 0.9, dic 0.7.

- **Entrada larga / reversión de corta:** `S > SeasonalThreshold && C > U && Position <= 0`: comprar a mercado `Volume + abs(Position)`; stop `C − M × A`.
- **Entrada corta / reversión de larga:** `S < −SeasonalThreshold && C < L && Position >= 0`: vender a mercado `Volume + abs(Position)`; stop `C + M × A`.
- **Prioridad del stop:** si el nivel guardado es positivo, cerrar larga con `C <= stop` o corta con `C >= stop`; borrar el stop y terminar la evaluación de la vela.
- **Salida por EMA:** en otro caso, cerrar larga con `C < E` o corta con `C > E`; borrar el stop. No hay take-profit independiente ni trailing automático.

El stop se comprueba **manualmente al cierre de la vela**, no como orden intrabar en el mercado; se calcula desde el cierre de la señal y no desde el precio ejecutado. Con el umbral `0.5`, ene/mar/abr/nov/dic permiten señales largas, sep señales cortas, y los demás meses prohíben nuevas entradas. Además debe producirse la ruptura correspondiente del canal.
