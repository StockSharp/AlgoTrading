# Estrategia Laguerre ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica un filtro Laguerre a los componentes +DI y -DI del indicador Average Directional Index (ADX). El suavizado reduce el ruido en el movimiento direccional y destaca los cambios repentinos en el dominio entre compradores y vendedores. Cuando el +DI suavizado por Laguerre cruza por debajo del -DI suavizado, el sistema entra en una posición larga, esperando una reversión alcista. Por el contrario, cuando el +DI suavizado cruza por encima del -DI suavizado, el sistema abre una posición corta.

Las posiciones se cierran cuando los valores suavizados actuales indican que el lado opuesto ha tomado el control. El método está diseñado como un enfoque contratendencia, desvaneciendo los extremos de corto plazo en el índice direccional.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Laguerre +DI cruza por debajo de Laguerre –DI.
  - **Corto**: Laguerre +DI cruza por encima de Laguerre –DI.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Laguerre –DI se mueve por encima de Laguerre +DI.
  - **Corto**: Laguerre +DI se mueve por encima de Laguerre –DI.
- **Stops**: Sin stops fijos, solo protección de posición predeterminada.
- **Valores predeterminados**:
  - `ADX Period` = 14.
  - `Gamma` = 0.764 (factor de suavizado Laguerre).
  - `Candle Type` = marco temporal de 4 horas.
- **Filtros**:
  - Categoría: Contratendencia
  - Dirección: Ambos
  - Indicadores: ADX
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
