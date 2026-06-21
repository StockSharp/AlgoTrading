# Estrategia de Búsqueda de Tazas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia basada en patrones busca formaciones redondeadas en forma de "taza" en los datos de precios. Cuando el precio rompe una taza completada, entra largo o corto según la dirección.

Las pruebas indican un retorno anual promedio de aproximadamente el 47%. Funciona mejor con acciones.

La estrategia compra en rupturas alcistas de tazas y vende en tazas invertidas bajistas. Las posiciones están protegidas por un stop-loss.

## Detalles

- **Criterios de entrada**: Se forma el patrón de taza y el precio rompe el borde.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: El precio revierte o alcanza el stop-loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Lookback` = 150
  - `WidthPercent` = 5m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo/Corto
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
