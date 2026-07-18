# Estrategia de Exp Cronex MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia replica el asesor experto **Exp_CronexMFI**. Suaviza el Índice de Flujo de Dinero (MFI) dos veces y opera **contra** el cruce de las líneas resultantes. El puerto mantiene la filosofía contraria original mientras expone cada configuración como un parámetro de estrategia de StockSharp.

## Cómo funciona
1. Suscribirse a la serie de velas seleccionada (el marco temporal predeterminado es de 4 horas).
2. Calcular el Índice de Flujo de Dinero con el período configurado.
3. Aplicar el método de suavizado elegido dos veces: el primer pase produce la línea Cronex rápida, el segundo pase suaviza la línea rápida nuevamente para construir la línea lenta.
4. Almacenar pares históricos de valores rápidos y lentos con un retraso ajustable (`SignalShift`).
5. Cuando la línea rápida cruza **hacia abajo** a través de la línea lenta, cerrar posiciones cortas (si se permite) y abrir/ampliar una posición larga. Cuando la línea rápida cruza **hacia arriba**, cerrar posiciones largas y abrir/ampliar una posición corta.
6. Las órdenes se envían con el `Volume` de la estrategia y pueden desactivarse de forma independiente para los lados largo y corto.

La estrategia solo evalúa velas terminadas para coincidir con el momento de la implementación de MetaTrader.

## Parámetros
| Nombre | Tipo | Valor predeterminado | Descripción |
| --- | --- | --- | --- |
| `MfiPeriod` | `int` | `25` | Longitud del Índice de Flujo de Dinero. |
| `FastPeriod` | `int` | `14` | Período de la primera etapa de suavizado (línea Cronex rápida). |
| `SlowPeriod` | `int` | `25` | Período de la segunda etapa de suavizado (línea Cronex lenta). |
| `SignalShift` | `int` | `1` | Número de velas completadas para retrasar el procesamiento de señales, reproduciendo el comportamiento `SignalBar` de MQL. |
| `Smoothing` | `SmoothingMethod` | `Simple` | Algoritmo de media móvil utilizado para ambas etapas de suavizado. |
| `EnableLongEntries` | `bool` | `true` | Habilita órdenes a mercado que abren o añaden a posiciones largas. |
| `EnableShortEntries` | `bool` | `true` | Habilita órdenes a mercado que abren o añaden a posiciones cortas. |
| `EnableLongExits` | `bool` | `true` | Permite que las señales cierren la exposición larga existente. |
| `EnableShortExits` | `bool` | `true` | Permite que las señales cierren la exposición corta existente. |
| `CandleType` | `DataType` | `TimeFrame(4h)` | Serie de velas utilizada para los cálculos de indicadores. |
| `Volume` | `decimal` | `1` | Tamaño de orden utilizado al abrir nuevas posiciones. |

## Opciones de suavizado
El indicador MQL original ofrece varios modos de suavizado propietarios. El puerto StockSharp los asigna a medias móviles integradas:

| Concepto MQL | Valor `SmoothingMethod` | Notas |
| --- | --- | --- |
| SMA | `Simple` | Media móvil simple. |
| EMA | `Exponential` | Media móvil exponencial. |
| SMMA | `Smoothed` | Media móvil suavizada (Wilder). |
| LWMA | `Weighted` | Media móvil ponderada linealmente. |
| JJMA / JurX / ParMA / T3 / VIDYA / AMA | `DoubleExponential`, `TripleExponential`, `Hull`, `ZeroLagExponential`, `ArnaudLegoux`, `KaufmanAdaptive` | Seleccionar la aproximación más cercana para suavizado adaptativo. |

## Diferencias vs versión MQL
- La selección de volumen de tick/real de MQL no está disponible; las velas de StockSharp proporcionan datos de volumen agregado.
- La gestión de operaciones se basa únicamente en órdenes a mercado. El asistente de gestión monetaria original que retrasaba la ejecución hasta la siguiente barra se emula a través de `SignalShift`.
- La colocación de stop-loss y take-profit debe configurarse externamente (por ejemplo, mediante reglas de riesgo o módulos de protección).

## Notas de uso
- Elegir una serie de velas que coincida con la liquidez del instrumento; el intervalo predeterminado de 4 horas refleja el EA fuente.
- Ajustar `SignalShift` si desea confirmar un cruce con velas adicionales.
- Combinar la estrategia con reglas de gestión de riesgo (p. ej., `StartProtection`) para limitar las pérdidas.
