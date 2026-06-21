# FON60DK
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre posiciones largas cuando la línea Tillson T3 sube por encima de la banda superior del Optimized Trend Tracker (OTT) y Williams %R confirma impulso alcista. La posición se cierra cuando Tillson T3 cae por debajo de la banda OTT opuesta y Williams %R entra en territorio de sobreventa.

## Detalles

- **Criterios de entrada**: `T3 > OTT_up` && `Williams %R > -20`
- **Criterios de salida**: `T3_SAT < OTT_dn_SAT` && `Williams %R < -70`
- **Tipo**: Seguimiento de tendencia
- **Indicadores**: Tillson T3, OTT, Williams %R
- **Marco temporal**: 1 minuto (por defecto)
- **Stops**: Ninguno
