# Rango Técnico (Estrategia)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia calcula un rango técnico compuesto a partir de medias móviles, tasa de cambio, pendiente del PPO y RSI. Las posiciones largas se abren cuando el rango supera un umbral superior, y las cortas cuando cae por debajo de un umbral inferior.

## Detalles

- **Criterios de entrada**: rango > UpperThreshold → largo; rango < LowerThreshold → corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = velas de 1 minuto
