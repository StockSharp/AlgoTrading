# Estrategia de Ruptura Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura usando Canales Donchian con filtros de volatilidad y volumen.

La estrategia compra cuando el precio cierra por encima del canal Donchian superior y la tendencia está confirmada por una EMA y RSI por encima de 50. Las posiciones cortas se toman cuando el precio rompe por debajo del canal inferior. Las posiciones se cierran ante una señal Donchian opuesta o cuando se activa un stop basado en ATR.

## Detalles

- **Criterios de entrada**: Ruptura del canal Donchian con filtros de EMA, RSI, volatilidad y volumen.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Ruptura opuesta o stop ATR.
- **Stops**: Basado en ATR.
- **Valores predeterminados**:
  - `EntryLength` = 20
  - `ExitLength` = 10
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `EmaLength` = 50
  - `VolumeSmaLength` = 20
  - `AtrSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Donchian, ATR, EMA, RSI, Volumen
  - Stops: Stop ATR
  - Complejidad: Intermedio
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
