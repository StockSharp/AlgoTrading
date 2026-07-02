# Estrategia de duplicar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Double Up es un puerto directo del MetaTrader asesor experto `DoubleUp.mq4`. Combina un oscilador del índice de canal de productos básicos (CCI) con la línea principal del indicador MACD para detectar condiciones de impulso extremas y luego aplica un modelo de tamaño de posición estilo martingala. Siempre que ambos osciladores alcanzan la misma zona extrema, el algoritmo se prepara para operar en la dirección opuesta. Una vez que CCI regresa hacia el punto medio, la estrategia abre una nueva posición larga (después de cerrar los cortos existentes) o abre una nueva posición corta (después de cerrar los largos existentes).

El volumen de cada nueva posición se basa en una progresión exponencial (`baseVolume * 2^lossCounter`). Las salidas perdedoras consecutivas aumentan el exponente, mientras que una salida rentable restablece la progresión según el buffer de espera acumulado. Este comportamiento refleja la lógica piramidal del código original donde las variables `pos` y `wait` controlan el multiplicador aplicado.

## Lógica de trading
- Suscríbase a una sola serie de velas y calcule la línea principal CCI (longitud predeterminada 8) y MACD (rápida 13 predeterminada, 33 lenta, señal 2).
- Multiplique la lectura de MACD por un millón para que su magnitud coincida con el nivel de umbral de CCI.
- Cuando ambos osciladores superen `+Threshold`, prepare la estrategia para una futura entrada corta. Cuando ambos osciladores caigan por debajo de `-Threshold`, prepárelo para una futura entrada larga.
- Una entrada larga pendiente se ejecuta tan pronto como el valor CCI vuelve a estar por debajo de `+Threshold`. Una entrada corta pendiente se ejecuta cuando CCI cae por debajo de `-Threshold` mientras la bandera corta está activa, reproduciendo el orden exacto del código original.
- Antes de abrir una nueva posición, el algoritmo cierra completamente cualquier exposición opuesta. La nueva orden se envía sólo después de que finalizan todas las operaciones de cierre.
- Las operaciones de salida son órdenes de mercado generadas durante las reversiones de señales. La ganancia o pérdida realizada de cada operación cerrada alimenta los contadores de martingala.

## Modelo de dimensionamiento de posiciones
- Las salidas perdedoras incrementan el contador de pérdidas. Una vez que el contador llega a `PreWait`, su valor se agrega al búfer de espera y el contador de pérdidas se restablece a cero.
- Una salida rentable transfiere el valor del buffer de espera (truncado) al contador de pérdidas y borra el buffer. Por lo tanto, los tamaños de las operaciones futuras comienzan a partir de `2^lossCounter` lotes.
- El búfer de espera se inicializa desde `InitialWait` y, por lo demás, está controlado por las reglas anteriores.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `CciPeriod` | 8 | Período del índice del canal de materias primas. |
| `Threshold` | 230 | Nivel absoluto utilizado para detectar extremos del oscilador. |
| `MacdFastPeriod` | 13 | Duración rápida de EMA del cálculo MACD. |
| `MacdSlowPeriod` | 33 | Longitud lenta de EMA del cálculo MACD. |
| `MacdSignalPeriod` | 2 | Longitud de la señal EMA, necesaria para coincidir con la firma de llamada MetaTrader. |
| `BaseVolume` | 0,01 | Multiplicador de volumen inicial antes de aplicar el exponente de martingala. |
| `InitialWait` | 0 | Valor inicial del búfer de espera (variable `wait` en el script original). |
| `PreWait` | 2 | Número mínimo de salidas perdedoras consecutivas requeridas antes de que el búfer de espera acumule el contador de pérdidas. |
| `BackShift` | 0 | Cambio histórico de lecturas de indicadores. En este puerto solo se admite cero. |
| `CandleType` | plazo de 15 minutos | Tipo de vela solicitado desde la fuente de datos. Ajústelo para que coincida con el período de tiempo del gráfico utilizado en MetaTrader. |

## Notas
- Actualmente, el puerto solo admite `BackShift = 0`, lo que refleja la configuración predeterminada del asesor experto original.
- Cada envío y cierre de órdenes utiliza órdenes de mercado. Adjunte protecciones separadas (stop-loss, take-profit) si es necesario.
- Debido a que la estrategia duplica el tamaño de la posición después de perder operaciones, asegúrese de que los límites de margen y los controles de riesgo sean apropiados para el instrumento negociado.
