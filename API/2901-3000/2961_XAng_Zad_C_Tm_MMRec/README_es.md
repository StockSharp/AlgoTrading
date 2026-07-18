# Estrategia XAng Zad C TM MM Rec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port en C# del asesor experto de MetaTrader **Exp_XAng_Zad_C_Tm_MMRec**. Opera con envolventes de precio adaptativas calculadas por el indicador personalizado *XAng Zad C* y agrega una ventana de trading basada en tiempo junto con un contador simple de gestión de dinero. El objetivo es capturar rupturas cuando las líneas adaptativas superior e inferior se cruzan entre sí, escalando dinámicamente el tamaño de posición después de un número configurable de operaciones perdedoras.

### Lógica principal
- **Indicador** – el indicador XAng Zad C produce un canal adaptativo superior e inferior. La versión C# reproduce el cálculo del envolvente y soporta varios suavizadores de media móvil (SMA, EMA, SMMA, LWMA). Los suavizadores exóticos del script original recurren a EMA.
- **Señales de entrada** – cuando la vela anterior muestra la línea superior por encima de la inferior y la barra actual cierra con la línea superior cayendo por debajo de la inferior, se detecta una ruptura alcista. La configuración opuesta produce una ruptura bajista. El parámetro `SignalShift` define cuántas velas cerradas atrás deben compararse.
- **Señales de salida** – indicadores opcionales permiten cerrar largos cuando la línea superior regresa bajo la inferior y cerrar cortos en el evento inverso. Las posiciones también se cierran inmediatamente cuando la ventana de trading configurada finaliza.
- **Gestión de dinero** – la estrategia mantiene una lista de resultados de operaciones históricas. Si las operaciones perdedoras más recientes de `BuyLossTrigger` (o `SellLossTrigger`) aparecen dentro de las últimas `BuyTotalTrigger` (o `SellTotalTrigger`) operaciones, la siguiente posición usa el volumen reducido. De lo contrario se restaura el volumen normal.
- **Control de riesgo** – los objetivos estáticos de stop-loss y take-profit se aplican en múltiplos del paso de precio del instrumento. Si alguno de los niveles se alcanza durante la vela, la posición se aplana al precio correspondiente.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `NormalVolume` | Tamaño de orden predeterminado usado cuando no hay una racha perdedora reciente. |
| `ReducedVolume` | Tamaño de orden aplicado después de una secuencia de operaciones perdedoras. |
| `BuyTotalTrigger` / `SellTotalTrigger` | Número de operaciones históricas inspeccionadas al evaluar el contador de pérdidas. |
| `BuyLossTrigger` / `SellLossTrigger` | Operaciones perdedoras requeridas (dentro de la ventana anterior) para cambiar al volumen reducido. |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir entradas largas o cortas. |
| `EnableBuyExit` / `EnableSellExit` | Permitir señales de salida automáticas basadas en cruces del canal. |
| `UseTradingWindow` | Habilitar el filtro de tiempo. Fuera de la ventana todas las posiciones se cierran y no se envían nuevas órdenes. |
| `WindowStart` / `WindowEnd` | Horas de inicio y fin de la ventana de trading diaria (UTC). La ventana puede abarcar medianoche. |
| `StopLoss` | Distancia del stop-loss expresada en múltiplos de `Security.PriceStep`. Establezca en `0` para deshabilitar. |
| `TakeProfit` | Distancia del objetivo de ganancia expresada en múltiplos de `Security.PriceStep`. Establezca en `0` para deshabilitar. |
| `SignalShift` | Número de velas ya cerradas usadas para la comparación de cruce. |
| `CandleType` | Tipo de datos de vela usado para el indicador (predeterminado: velas de 4 horas). |
| `SmoothMethods` | Suavizador de media móvil dentro del indicador. Los valores no soportados usan automáticamente EMA. |
| `MaLength` | Longitud de suavizado para el indicador. |
| `MaPhase` | Parámetro de fase adicional retenido del indicador original (actualmente informativo). |
| `Ki` | Ratio que controla cuán rápidamente reaccionan los envolventes adaptativos a los cambios de precio. |
| `AppliedPrices` | Fuente de precio usada para alimentar el indicador (cierre, apertura, mediana, etc.). |

## Notas en comparación con la versión MQL5
- Los asistentes de gestión de dinero de MetaTrader dependían del historial de operaciones global. La versión C# rastrea las operaciones completadas localmente y aplica la misma lógica de disparo.
- El dimensionamiento de lotes se expresa directamente como volumen de estrategia. Ajuste `NormalVolume`/`ReducedVolume` para que coincida con la cantidad objetivo para su plataforma.
- Las ventanas de tiempo se configuran con valores `TimeSpan`. Cuando `WindowStart` es igual a `WindowEnd`, el trading se deshabilita (coincidiendo con el comportamiento de ventana de ancho cero del script original).
- La estrategia asume reversiones de posición completas y no mantiene posiciones parciales de señales anteriores.
- Los tipos de suavizado no soportados (JJMA, JurX, ParMA, T3, VIDYA, AMA) usan EMA por defecto. Considere extender `CreateMovingAverage` si requiere una alternativa específica.

## Consejos de uso
1. Elija un tipo de vela que coincida con el marco temporal del indicador usado en MetaTrader (predeterminado: H4).
2. Ajuste las distancias de stop-loss y take-profit basadas en el tamaño de tick del instrumento para aproximar los valores basados en puntos del EA original.
3. Optimice los disparadores de gestión de dinero para reflejar la volatilidad del activo y su tolerancia al riesgo.
4. Monitoree el comportamiento del indicador en un gráfico (líneas del canal superior/inferior) para confirmar que el indicador reconstruido cumple las expectativas antes del trading en vivo.
