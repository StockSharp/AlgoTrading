# Estrategia Karakatica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Karakatica es un sistema de seguimiento de tendencias a mediano plazo que fue adaptado del asesor experto original MetaTrader 4 "Exp_karakatica". La estrategia opera **EUR/USD en el marco temporal M15** de forma predeterminada y utiliza un motor de señales personalizado que emula el comportamiento del indicador "iKarakatica" original con un modelo cruzado de media móvil. El cruce se recalcula en cada barra y el período de la señal se reoptimiza continuamente para seguir el régimen reciente más rentable.

La estrategia ingresa al mercado con órdenes de mercado solo cuando no hay ninguna posición abierta actualmente. Las órdenes de protección (stop-loss y take-profit) se adjuntan automáticamente a través del subsistema de protección StockSharp.

## Lógica de trading
1. **Generación de señales**: la estrategia calcula una media móvil simple (SMA) de los precios de cierre de las velas. Aparece una señal alcista cuando la vela anterior cerró por debajo o en el SMA mientras que la última vela terminada cierra por encima de él. Se produce una señal bajista cuando la vela anterior cierra por encima o en el SMA y la última vela cierra por debajo de él. Las señales siempre se evalúan en la barra completada *anterior* para reflejar la implementación MT4 que usó valores shift=1 del indicador `iKarakatica`.
2. **Gestión de posiciones** –
   - Si aparece una señal contraria mientras una posición está abierta, la posición se cierra inmediatamente con una orden de mercado.
   - Se permiten nuevas operaciones sólo cuando no existe ninguna posición y la estrategia no está bloqueada por la etapa de optimización. Las operaciones consecutivas en la misma dirección se bloquean hasta que el mercado produzca una señal opuesta confirmada.
3. **Tamaño del pedido**: el tamaño de la posición se deriva del parámetro `Risk` configurado. El algoritmo convierte el riesgo en un volumen deseado en función del valor actual de la cartera y luego lo alinea con el paso de volumen del instrumento, imitando el método de cálculo de lotes del asesor experto original.
4. **Protección comercial**: las distancias de limitación de pérdidas y obtención de beneficios se establecen en puntos de precio. Se traducen a precios absolutos multiplicando el valor en puntos por el paso del precio del instrumento.

## Optimización adaptativa
El asesor experto vuelve a optimizar continuamente el período de señal para adaptarse al comportamiento cambiante del mercado:

1. Cada `ReoptimizeEvery` barras, la estrategia lanza una simulación histórica que cubre `OptimizationDepth` barras anteriores.
2. Para cada período candidato en el rango `[OptimizationStart, OptimizationEnd]` con un paso `OptimizationStep`, el backtester simula un modelo cruzado de media móvil simple:
   - El simulador realiza un seguimiento de una posición virtual activa y actualiza sus ganancias cada vez que se activa la señal opuesta.
   - Se mantienen contadores de ganancias separados para operaciones largas y cortas, además de las ganancias combinadas.
3. Después de escanear a todos los candidatos, la estrategia aplica las siguientes reglas:
   - Si los beneficios tanto a largo como a corto son negativos, el comercio en ambas direcciones se desactiva hasta el siguiente ciclo de optimización.
   - Si los mejores resultados largos y cortos son iguales, se utiliza el mejor período general y ambas direcciones permanecen habilitadas.
   - De lo contrario, solo permanece habilitada la dirección con mayor beneficio y se selecciona el mejor período correspondiente.

La optimización requiere al menos `OptimizationDepth + OptimizationEnd + 2` velas completadas para comenzar. Hasta que se recopile suficiente historia, la estrategia retrasa el comercio.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimizable |
| ---- | ----------- | ------- | ----------- |
| `Risk` | Porcentaje del valor de la cartera (por 1000 unidades) que define el volumen de pedido objetivo. | 0,5 | si |
| `StopLossPoints` | Distancia de stop-loss en puntos de precio. | 50 | si |
| `TakeProfitPoints` | Distancia de obtención de beneficios en puntos de precio. | 150 | si |
| `Period` | Período activo SMA utilizado para la generación de señal. Actualizado automáticamente por el optimizador. | 70 | si |
| `OptimizationDepth` | Número de barras históricas utilizadas para el backtest en muestra. | 250 | No |
| `ReoptimizeEvery` | Frecuencia de ejecuciones de optimización medidas en barras terminadas. | 50 | No |
| `OptimizationStart` | Periodo mínimo considerado durante la optimización. | 10 | No |
| `OptimizationStep` | Paso entre períodos vecinos. | 5 | No |
| `OptimizationEnd` | Período máximo considerado durante la optimización. | 150 | No |
| `CandleType` | Tipo de datos de velas (el valor predeterminado es un período de tiempo de 15 minutos). | Velas de marco temporal M15 | No |

## Notas de uso
- La estrategia fue diseñada para EUR/USD en un marco temporal de 15 minutos. Al realizar la transferencia a un instrumento diferente, revise el valor en puntos, el paso de volumen y los supuestos de dispersión.
- Asegúrese de que el feed de datos proporcione las mejores cotizaciones de oferta y demanda. Se utilizan para estimar el diferencial comercial durante el proceso de optimización. Cuando las cotizaciones no están disponibles, el algoritmo recurre a un diferencial de precio único.
- Debido a que la lógica de optimización requiere varios cientos de barras históricas, permita que la estrategia precargue datos antes de habilitar el comercio en vivo.

## Archivos
- `CS/KarakaticaStrategy.cs` – StockSharp implementación de la estrategia.
- `README.md` – Descripción en inglés (este archivo).
- `README_ru.md` – Descripción rusa.
- `README_zh.md` – Descripción china.
