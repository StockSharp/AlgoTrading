# Estrategia AltrTrend Signal v2.2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un porte de StockSharp del asesor experto de MetaTrader **Exp_AltrTrend_Signal_v2_2**. Recrea la lógica
del canal adaptativo del indicador AltrTrend Signal original y ejecuta operaciones en barras retrasadas tal como la versión MQL5.
El valor ADX contrae o amplía el canal de modo que las rupturas solo se activan cuando la fuerza de la tendencia las respalda.

## Cómo funciona

1. Se calcula un canal dinámico en cada vela completada del marco temporal configurado. El ancho del canal se define por el
   máximo y mínimo del precio dentro de un lookback que se expande o contrae según el valor ADX anterior (`KPeriod / ADX`).
2. Los límites internos (`smin`, `smax`) se acercan al centro en `KPercent`. El precio debe cerrar fuera de estos límites
   internos para establecer un estado de tendencia direccional.
3. Cuando la tendencia pasa de bajista a alcista y el cierre está por encima del límite superior, se genera una señal de compra.
   Un giro bajista por debajo del límite inferior emite una señal de venta. Las señales se ejecutan en la barra definida por el
   retraso `SignalBar`, coincidiendo con el comportamiento del asesor experto original.
4. Los niveles opcionales de stop-loss y take-profit se asignan de puntos a pasos de precio para que las salidas de protección
   imiten la colocación de órdenes original con valores fijos de SL/TP.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia anterior era bajista o neutral, el precio cierra por encima del límite superior contraído y las
    entradas largas están habilitadas. Las posiciones cortas pueden cerrarse automáticamente si está permitido.
  - **Corto**: La tendencia anterior era alcista o neutral, el precio cierra por debajo del límite inferior contraído y las
    entradas cortas están habilitadas. Las posiciones largas pueden cerrarse automáticamente si está permitido.
- **Criterios de salida**:
  - Señal de ruptura opuesta cuando las salidas están permitidas para la dirección actual.
  - Distancias de stop-loss o take-profit expresadas en pasos de precio.
- **Largo/Corto**: Dirección dual con interruptores independientes de habilitación/deshabilitación para entradas y salidas.
- **Gestión de riesgos**:
  - `StopLossPoints` y `TakeProfitPoints` replican el módulo MM original aplicando salidas basadas en distancia después de que
    se ejecutan las órdenes de mercado.
- **Configuración del indicador**:
  - `KPercent` controla cuánto se contraen los bordes del canal hacia el rango medio.
  - `KStop` mantiene el valor de proyección de flecha original para gráficos y logging.
  - `KPeriod` es el lookback base antes de la modulación ADX.
  - `AdxPeriod` establece la longitud del Índice Direccional Promedio que adapta el ancho del canal.
  - `SignalBar` retrasa la ejecución de órdenes por el número especificado de velas completadas.
- **Mercados recomendados**:
  - Funciona mejor en instrumentos con fases de swing claras donde la fuerza de la tendencia varía con el tiempo (pares de forex
    principales, oro y futuros de índices). El marco temporal predeterminado es H1 como en la plantilla MQL5.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco temporal usado para construir el canal adaptativo. |
| `KPercent` | Porcentaje que acerca los límites internos del canal hacia adentro. |
| `KStop` | Multiplicador para los precios de flecha proyectados (mantenido por compatibilidad). |
| `KPeriod` | Número base de velas examinadas antes del ajuste ADX. |
| `AdxPeriod` | Período del Índice Direccional Promedio que impulsa el ancho del canal. |
| `SignalBar` | Número de velas completadas que se esperan antes de ejecutar una señal. |
| `AllowBuyEntries` / `AllowSellEntries` | Habilitar o deshabilitar la apertura de posiciones en cada dirección. |
| `AllowBuyExits` / `AllowSellExits` | Permitir el cierre automático de posiciones en señales opuestas. |
| `StopLossPoints` | Distancia del stop-loss medida en pasos de precio (0 deshabilita). |
| `TakeProfitPoints` | Distancia del take-profit medida en pasos de precio (0 deshabilita). |

Este porte mantiene los interruptores discrecionales y los parámetros de riesgo del asesor experto original, facilitando la
reproducción del mismo comportamiento dentro de StockSharp Designer, Shell o Runner.
