# Estrategia de Divergencia AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia busca divergencias alcistas y bajistas entre el Awesome Oscillator (AO) y el precio. Una divergencia alcista ocurre cuando el precio forma un mínimo más bajo mientras el AO forma un mínimo más alto. Una divergencia bajista aparece cuando el precio forma un máximo más alto mientras el AO forma un máximo más bajo.

Cuando se detecta una divergencia alcista, la estrategia abre una posición larga. Una divergencia bajista activa una posición corta. Las posiciones se revierten con señales opuestas.

## Detalles

- **Criterios de entrada**: Divergencia alcista o bajista del AO con el precio.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal de divergencia opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 5 minutos
  - `FastLength` = 5
  - `SlowLength` = 34
  - `Lookback` = 5
  - `UseEma` = false
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: Awesome Oscillator
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
