# Extreme EA (Conversión a StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Extreme EA** es un Asesor Experto de seguimiento de tendencia originalmente escrito para MetaTrader. Combina dos medias móviles con un filtro de Índice de Canal de Materias Primas (CCI) y un módulo de gestión adaptativa del dinero. Este porte mantiene la lógica de trading intacta mientras expone todos los controles importantes a través de la API de alto nivel de StockSharp. La estrategia opera solo en velas cerradas y es compatible con múltiples marcos temporales ejecutando las medias móviles y el CCI en suscripciones de velas independientes.

## Descripción de la Estrategia

1. **Filtro de tendencia:** Dos medias móviles se calculan en el `MaCandleType` configurable. La media rápida rastrea el momentum a corto plazo mientras que la media lenta define la pendiente de la tendencia dominante. La estrategia verifica la pendiente de la media lenta usando los dos valores previos para imitar los desplazamientos del array `CopyBuffer` del código MQL.
2. **Filtro de momentum:** El CCI se evalúa en su propio marco temporal (`CciCandleType`) y fuente de precio. El último valor completado se almacena en caché y se reutiliza hasta que aparece una nueva vela CCI, lo que coincide con el comportamiento de los buffers de MetaTrader.
3. **Reglas de entrada:**
   - Entrar en largo cuando la MA lenta sube, la MA rápida sube y el CCI cae por debajo del nivel inferior.
   - Entrar en corto cuando la MA lenta baja, la MA rápida baja y el CCI sube por encima del nivel superior.
4. **Reglas de salida:**
   - Cerrar todos los largos si la MA lenta deja de subir.
   - Cerrar todos los cortos si la MA lenta deja de bajar.

## Gestión de Riesgo

- **MaximumRisk** controla el tamaño de posición objetivo basado en el capital actual del portafolio y el último precio. Si el volumen calculado es cero o los valores del portafolio no están disponibles, la estrategia recurre al `Volume` configurado o al mínimo de la bolsa.
- **DecreaseFactor** reduce el volumen calculado después de dos o más operaciones perdedoras consecutivas. La reducción refleja la fórmula original `lot = lot - lot * losses / DecreaseFactor`.
- **HistoryDays** limita cuánto tiempo se recuerda una racha de pérdidas. Si una operación de cierre ocurre después del número especificado de días, la racha se reinicia antes de aplicar la reducción.
- **MaxPositions** limita la pirámide acotando la exposición neta por dirección. Cuando se alcanza el límite, las nuevas entradas se suprimen hasta que la exposición disminuye.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `MaximumRisk` | `decimal` | `0.05` | Fracción del capital usado para dimensionar cada nueva operación. |
| `DecreaseFactor` | `decimal` | `6` | Factor de reducción por racha de pérdidas. Establecer en `0` para deshabilitar. |
| `HistoryDays` | `int` | `60` | Número de días preservados al contar pérdidas consecutivas. |
| `MaxPositions` | `int` | `3` | Máximo de entradas simultáneas por dirección. |
| `FastMaPeriod` | `int` | `15` | Período para la media móvil rápida. |
| `SlowMaPeriod` | `int` | `75` | Período para la media móvil lenta. |
| `CciPeriod` | `int` | `12` | Período de lookback para el CCI. |
| `CciUpperLevel` | `decimal` | `50` | Umbral CCI superior usado para cortos. |
| `CciLowerLevel` | `decimal` | `-50` | Umbral CCI inferior usado para largos. |
| `MaCandleType` | `DataType` | `15m` | Marco temporal para ambas medias móviles y ejecución. |
| `CciCandleType` | `DataType` | `30m` | Marco temporal para el filtro CCI. |
| `MaMethod` | `MaMethod` | `Exponential` | Método de suavizado (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaPriceMode` | `AppliedPriceMode` | `Median` | Entrada de precio para las medias móviles. |
| `CciPriceMode` | `AppliedPriceMode` | `Typical` | Entrada de precio para el CCI. |

## Notas de Implementación

- La estrategia se suscribe al marco temporal de medias móviles una vez y opcionalmente a una segunda suscripción para el CCI. Cuando ambos marcos temporales coinciden, una sola suscripción alimenta ambos componentes, reproduciendo el flujo de trabajo original de un solo gráfico.
- Los valores previos de los indicadores se almacenan en campos privados para emular las comparaciones `ma_slow_array[1]`, `ma_slow_array[2]` y `ma_fast_array[0]` sin recurrir a buffers de indicadores manuales.
- El dimensionamiento de posición se normaliza contra el paso de volumen del instrumento, mínimo y máximo para evitar órdenes rechazadas.
- El módulo de riesgo registra los precios de entrada y salida para estimar el PnL realizado por posición completada, lo que reemplaza el bucle `HistoryDealGet` usado en MetaTrader.

## Diferencias respecto a la Versión MQL

- Las funciones específicas de MetaTrader como `FreeMarginCheck`, `MarginCheck` y `HistorySelect` se aproximan con las métricas de portafolio de StockSharp y el rastreador interno de rachas de pérdidas.
- El porte de StockSharp opera en posiciones netas. Las órdenes de cierre aplanan toda la exposición en la dirección relevante, alineándose con el modelo de posición consolidada.
- Las rutinas de registro del EA original se omitieron en favor de los diagnósticos integrados de StockSharp.
