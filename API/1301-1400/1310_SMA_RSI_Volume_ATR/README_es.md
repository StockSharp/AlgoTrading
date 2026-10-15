# Estrategia SMA RSI Volumen ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una Media Móvil Simple (SMA), el Índice de Fuerza Relativa (RSI), confirmación de volumen y un filtro de volatilidad basado en ATR.
Compra cuando el precio está por encima de la SMA, el RSI está en sobreventa, el volumen supera su media móvil por un multiplicador y la volatilidad está aumentando. Vende bajo las condiciones opuestas.

Los stops se gestionan con niveles fijos de take profit y stop loss en porcentaje.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close > SMA` && `RSI < RsiOversold` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
  - **Corto**: `Close < SMA` && `RSI > RsiOverbought` && `Volume > AvgVolume * VolumeThreshold` && `ATR > ATR_{prev}`
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o take-profit
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `SmaLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `VolumeThreshold` = 1.5
  - `AtrLength` = 14
  - `TakeProfitPerc` = 1.5
  - `StopLossPerc` = 0.5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, RSI, Volumen, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
