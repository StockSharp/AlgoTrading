# Estrategia Three Kilos BTC 15m
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Three Kilos BTC 15m combina tres Medias Móviles Exponenciales Triples (TEMA) con un filtro Supertrend. Se abre una posición larga cuando la TEMA media cruza por encima de la TEMA corta, permanece por encima de la TEMA lenta y el Supertrend indica una tendencia alcista. Se abre una posición corta cuando la TEMA corta cruza por encima de la TEMA media, permanece por debajo de la TEMA lenta y el Supertrend muestra una tendencia bajista. Un take profit y stop loss de porcentaje fijo gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: TEMA2 cruza por encima de TEMA1, TEMA2 > TEMA3, tendencia alcista en Supertrend.
  - **Corto**: TEMA1 cruza por encima de TEMA2, TEMA2 < TEMA3, tendencia bajista en Supertrend.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Take profit o stop loss.
- **Stops**: Take profit del 1% y stop loss del 1%.
- **Valores predeterminados**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: TEMA, Supertrend, ATR
  - Stops: Take profit y stop loss
  - Complejidad: Moderado
  - Marco temporal: 15m
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
