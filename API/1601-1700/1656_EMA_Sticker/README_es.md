# Estrategia EMA Sticker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza una Media Móvil Exponencial (EMA) para seguir tendencias a corto plazo. Se abre una posición larga cuando el precio de cierre cruza por encima de la EMA, y una posición corta cuando cruza por debajo. Niveles opcionales de stop-loss y take-profit fijos ayudan a gestionar el riesgo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close > EMA`.
  - **Corto**: `Close < EMA`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta o niveles de stop configurados alcanzados.
- **Stops**: Sí, stop-loss y take-profit opcionales en unidades de precio.
- **Valores predeterminados**:
  - `MA period` = 5.
  - `Stop loss` = 0.001.
  - `Take profit` = 0.001.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único
  - Stops: Sí
  - Complejidad: Simple
  - Marco temporal: Corto plazo
