# Estrategia de Momentum Largo + Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de momentum opera tanto posiciones largas como cortas en un marco temporal de 3 horas. Las configuraciones largas requieren que el precio se mantenga por encima de las medias móviles de 100 y 500 períodos y pueden filtrarse por RSI, ADX, ATR y alineación de tendencia. Las entradas cortas buscan que el precio rompa por debajo de la Banda de Bollinger inferior mientras permanece bajo ambas medias, con confirmación ATR opcional y la posibilidad de bloquear cortos durante tendencias alcistas fuertes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: precio por encima de MA100 y MA500, alineación de tendencia opcional, RSI por encima de su valor suavizado, ADX por encima de su valor suavizado y ATR por encima de su valor suavizado.
  - **Corto**: precio por debajo de MA100 y MA500, por debajo de la Banda de Bollinger inferior, RSI por debajo del umbral, ATR por encima de su valor suavizado y bloqueo de tendencia alcista opcional.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: stop-loss en `slPercentLong`% por debajo de la entrada; cierre anticipado si el precio cae por debajo de MA500.
  - **Corto**: stop-loss y take-profit basados en porcentajes `slPercentShort` y `tpPercentShort`.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `slPercentLong = 3`
  - `slPercentShort = 3`
  - `tpPercentShort = 4`
  - `rsiLengthLong = 14`
  - `rsiLengthShort = 14`
  - `adxLength = 14`
  - `atrLength = 14`
  - `bbLength = 20`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
