# Estrategia de cambio de Krohabor Up3x1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **up3x1 Krohabor D** es una conversión del MetaTrader 4 asesores expertos `up3x1_Krohabor_D.mq4`. Mantiene la idea original de alinear tres promedios móviles simples desplazados (SMA) para detectar rupturas de continuación de tendencia en el período de tiempo activo. La implementación de C# utiliza StockSharp API de alto nivel con suscripciones de velas y enlaces de indicadores, al tiempo que adapta la gestión de riesgos y posiciones al entorno .NET.

## Lógica de trading
- Se calculan tres SMA sobre los precios de cierre del instrumento:
  - Rápido SMA (24 barras predeterminadas)
  - Medio SMA (predeterminado 60 barras)
  - Lento SMA (predeterminado 120 barras)
- Cada media móvil avanza según un número configurable de velas completadas (el valor predeterminado es 6). La estrategia compara el valor desplazado actual y el valor de la vela anterior para cada promedio.
- **Requisitos de entrada larga**:
  - Tanto el valor lento actual como el anterior SMA permanecen por debajo de los valores actual y anterior rápido/medio SMA, lo que indica una separación alcista.
  - El medio SMA está cayendo en relación con el rápido SMA (medio anterior por encima del rápido anterior, medio actual por debajo del rápido actual).
- **Entrada corta** refleja la lógica larga con todas las comparaciones invertidas.
- Sólo se puede abrir una posición a la vez. Cuando no hay ninguna posición activa, la estrategia espera una nueva señal de entrada; en caso contrario gestiona las salidas.

## Reglas de salida y protección
- Las órdenes de protección iniciales se simulan monitoreando los máximos y mínimos de las velas:
  - La distancia de stop-loss se expresa en pasos de precio (por defecto 110 puntos) y se aplica una vez que se abre una posición.
  - La distancia de obtención de beneficios utiliza la misma representación (5 puntos por defecto).
- Un trailing stop (predeterminado de 10 puntos) se activa una vez que las ganancias no realizadas superan el umbral configurado. El stop sigue el mercado a favor de la posición abierta sin retroceder nunca.
- Las salidas de reversión de media móvil cierran la operación cuando el SMA rápido vuelve a cruzar los promedios medio y lento, imitando la lógica de cierre del EA original.
- La reducción dinámica del volumen después de pérdidas consecutivas replica el comportamiento del script MT4: el tamaño de la operación disminuye proporcionalmente al número de operaciones perdedoras respetando un volumen mínimo.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `FastPeriod` | Período del ayuno SMA. |
| `MediumPeriod` | Período del medio SMA. |
| `SlowPeriod` | Periodo de la lentitud SMA. |
| `MaShift` | Número de velas completadas utilizadas para avanzar todas las medias móviles. |
| `Volume` | Volumen base de pedidos para nuevas entradas. |
| `MinVolume` | Volumen mínimo permitido después de ajustes basados en pérdidas. |
| `LossReductionFactor` | El divisor se aplica cuando se reduce el volumen después de operaciones perdedoras consecutivas. |
| `StopLossPoints` | Distancia de stop-loss medida en pasos de precio. |
| `TakeProfitPoints` | Distancia de obtención de beneficios medida en incrementos de precios. |
| `TrailingPoints` | Distancia del trailing-stop y umbral de activación en pasos de precio. |
| `CandleType` | Tipo de datos de vela (período de tiempo) utilizado para el análisis. |

## Notas
- La estrategia utiliza `SubscribeCandles` junto con `Bind` para transmitir las salidas de los indicadores, evitando la recuperación manual del valor del indicador.
- El comportamiento de stop-loss, take-profit y trailing se implementa dentro del ciclo estratégico para permanecer independiente del corredor. En entornos comerciales reales, puede reemplazar estos bloques con órdenes de protección reales si es necesario.
- Todos los comentarios dentro del código fuente están escritos en inglés para cumplir con las pautas del proyecto.
- No se proporcionan pruebas automatizadas; utilice backtesting dentro de StockSharp para validar conjuntos de parámetros para sus instrumentos.
