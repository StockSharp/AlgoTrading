# Estrategia SuperATR de Beneficio en 7 Pasos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un filtro de tendencia ATR adaptativo con un sistema de toma de ganancias de siete etapas. El ATR normalizado por momentum define la fuerza de la tendencia, mientras que las entradas se producen cuando la media móvil corta se alinea con la dirección de la tendencia confirmada.

- **Largo**: Fuerza de tendencia por encima del umbral, precio por encima de la MA corta y MA corta por encima de la MA larga.
- **Corto**: Fuerza de tendencia por debajo del umbral negativo, precio por debajo de la MA corta y MA corta por debajo de la MA larga.
- **Indicadores**: Momentum, Standard Deviation, SMA, ATR.
- **Toma de ganancias**: Cuatro niveles basados en ATR y tres niveles de porcentaje fijo, cada uno cierra una porción de la posición cuando está habilitado.

