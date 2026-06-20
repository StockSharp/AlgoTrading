# Choppiness Index Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
El Choppiness Index mide si el mercado está en tendencia o en rango. Cuando el indicador cae por debajo de un umbral, señala el inicio de una tendencia desde un entorno choppy.

Las pruebas indican un retorno anual promedio de aproximadamente 172%. Funciona mejor en el mercado de divisas.

Esta estrategia entra en la dirección del precio relativo a su media móvil cuando la choppiness disminuye. Sale si la choppiness sube de nuevo por encima del umbral alto o si se activa un stop-loss.

El objetivo es capturar nuevas tendencias que emergen de períodos de consolidación.

## Detalles

- **Criterios de entrada**: Choppiness por debajo de `ChoppinessThreshold` con precio por encima/debajo de la MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Choppiness por encima de `HighChoppinessThreshold` o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Choppiness, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

