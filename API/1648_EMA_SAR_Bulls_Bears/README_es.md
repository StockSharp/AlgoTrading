# Estrategia EMA SAR Bulls Bears
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una Media Móvil Exponencial (EMA) rápida y lenta, Parabolic SAR e indicadores Bulls/Bears Power. Solo opera durante una ventana intradía configurada y utiliza protecciones simples de ganancia y pérdida.

Una posición corta se abre cuando EMA3 está por debajo de EMA34, el Parabolic SAR está por encima del máximo de la vela, y Bears Power es negativo pero creciente. Una posición larga se abre cuando EMA3 está por encima de EMA34, el SAR está por debajo del mínimo de la vela, y Bulls Power es positivo pero decreciente.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA3 por encima de EMA34, SAR por debajo del mínimo de la vela, Bulls Power > 0 y disminuyendo.
  - **Corto**: EMA3 por debajo de EMA34, SAR por encima del máximo de la vela, Bears Power < 0 y aumentando.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stop/take activado.
- **Stops**: Sí, take-profit absoluto (400 puntos) y stop-loss (2000 puntos).
- **Filtros**:
  - Opera solo entre las 09:00 y las 17:00.
  - Funciona con velas de 15 minutos.
