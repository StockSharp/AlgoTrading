# Estrategia ALMA & UT Bot Confluence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia ALMA & UT Bot Confluence combina un filtro de media móvil Arnaud Legoux con un stop trailing al estilo UT Bot. Se abre una posición larga cuando el precio está por encima de la EMA a largo plazo y la ALMA, el volumen supera su media, el RSI señala momentum, el ADX confirma la fuerza del tendencia, la vela está por debajo de la banda superior de Bollinger y el UT Bot genera una señal de compra. Las entradas cortas ocurren cuando el UT Bot se vuelve bajista y el precio cruza por debajo de la EMA rápida bajo los mismos filtros. Las salidas utilizan el stop trailing del UT Bot o un stop loss y take profit fijos basados en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: precio > EMA y ALMA, RSI > 30, ADX > 30, precio < banda superior de Bollinger, señal de compra UT Bot, filtros de volumen y ATR, cooldown.
  - Corto: precio cruza por debajo de la EMA rápida con señal de venta UT Bot y filtros.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop trailing UT Bot o stop loss/take profit basado en ATR y salida temporal opcional.
- **Stops**: ATR o trailing.
- **Valores predeterminados**:
  - `FastEmaLength` = 20
  - `EmaLength` = 72
  - `AtrLength` = 14
  - `AdxLength` = 10
  - `RsiLength` = 14
  - `BbMultiplier` = 3.0
  - `StopLossAtrMultiplier` = 5.0
  - `TakeProfitAtrMultiplier` = 4.0
  - `UtAtrPeriod` = 10
  - `UtKeyValue` = 1
  - `VolumeMaLength` = 20
  - `BaseCooldownBars` = 7
  - `MinAtr` = 0.005
- **Filtros**:
  - Categoría: Seguimiento de tendencia con filtro de volatilidad
  - Dirección: Largo/Corto
  - Indicadores: EMA, ALMA, ADX, RSI, Bollinger Bands, UT Bot
  - Stops: ATR o trailing
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
