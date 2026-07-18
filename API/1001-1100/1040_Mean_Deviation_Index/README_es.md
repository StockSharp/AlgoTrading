# Estrategia del Índice de Desviación Media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el Índice de Desviación Media (MDX) para operar desviaciones respecto a una EMA filtrada por ATR.
Se abre una posición larga cuando el MDX sube por encima del nivel especificado,
y una posición corta cuando cae por debajo del nivel negativo.

## Detalles

- **Entrada**:
  - Largo cuando MDX > Level
  - Corto cuando MDX < -Level
- **Salida**: señal opuesta.
- **Indicadores**: EMA y ATR.
