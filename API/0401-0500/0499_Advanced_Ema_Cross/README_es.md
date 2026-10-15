# Estrategia Avanzada de Cruce de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia va largo cuando una EMA de corto plazo cruza por encima de una EMA de largo plazo, filtrando señales con ATR normalizado, fuerza de tendencia ADX y una verificación de dirección de SuperTrend. Los niveles de stop-loss y take-profit se adaptan en función de la fortaleza del USD inferida a partir de una EMA de 50 períodos.

## Detalles

- **Criterios de entrada**:
  - La EMA corta cruza por encima de la EMA larga.
  - ATR normalizado por encima de los umbrales según la dirección de la tendencia.
  - SuperTrend confirma mercado alcista o bajista.
- **Criterios de salida**:
  - Cruce inverso de EMA o ADX por encima del umbral tras un período mínimo de mantenimiento.
  - Stop-loss o take-profit alcanzado.
- **Indicadores**: EMA, ATR, ADX, SuperTrend, SMA (volumen).
- **Stops**: Stop-loss y take-profit porcentuales dinámicos.
- **Tipo**: Seguimiento de tendencia.
- **Marco temporal**: 30 minutos (predeterminado).
