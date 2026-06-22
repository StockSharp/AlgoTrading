# Estrategia RSI Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el asesor experto original **iRSISign** de MQL5 en la API de alto nivel de StockSharp. Combina el Índice de Fuerza Relativa (RSI) con el Rango Verdadero Promedio (ATR) para generar señales de entrada y salida.

El sistema escucha velas finalizadas de un marco temporal definido por el usuario. Cuando el RSI cruza por encima del umbral inferior, señala una posible reversión alcista y abre una posición larga o cierra un corto existente. Por el contrario, cuando el RSI cae por debajo del umbral superior, entra en una posición corta o cierra un largo activo. El ATR se calcula pero se usa solo como contexto adicional, reflejando el indicador original que mostraba flechas de señal desplazadas por ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El valor anterior del RSI estaba por debajo de `DownLevel` y el RSI actual cruza por encima.
  - **Corto**: El valor anterior del RSI estaba por encima de `UpLevel` y el RSI actual cruza por debajo.
- **Largo/Corto**: Ambas direcciones están permitidas y pueden habilitarse de forma independiente.
- **Criterios de salida**:
  - La señal opuesta cierra la posición actual si el indicador de cierre correspondiente está habilitado.
- **Stops**: No implementados. La gestión del riesgo puede añadirse externamente si es necesario.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `UpLevel` = 70
  - `DownLevel` = 30
  - `CandleType` = velas de 1 hora
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, ATR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Flexible
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `RsiPeriod` | Longitud del RSI. |
| `AtrPeriod` | Longitud del ATR. |
| `UpLevel` | Umbral superior del RSI que genera señales de venta. |
| `DownLevel` | Umbral inferior del RSI que genera señales de compra. |
| `CandleType` | Marco temporal de velas usado para los cálculos. |
| `BuyOpen` | Habilitar apertura de posiciones largas. |
| `SellOpen` | Habilitar apertura de posiciones cortas. |
| `BuyClose` | Permitir cierre de largos existentes con señal opuesta. |
| `SellClose` | Permitir cierre de cortos existentes con señal opuesta. |

La estrategia está pensada como ejemplo educativo que demuestra cómo traducir la lógica simple de MQL5 al marco de estrategias de alto nivel de StockSharp.
