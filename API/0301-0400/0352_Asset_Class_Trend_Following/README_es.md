# Estrategia de Seguimiento de Tendencia por Clase de Activo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue tendencias en múltiples clases de activos. Aplica un filtro de media móvil simple a cada valor del universo y rebalancea la cartera el primer día de negociación de cada mes. Las posiciones se abren únicamente cuando el precio está por encima de la media móvil.

Las pruebas indican un rendimiento anual promedio de aproximadamente 15%. Funciona mejor en carteras de futuros diversificadas.

Al inicio de cada mes, los valores que cotizan por encima de su SMA reciben una asignación de capital igual. Las posiciones se cierran cuando el precio cae por debajo de la SMA o cuando el capital se reasigna en el siguiente rebalanceo.

## Detalles

- **Criterios de entrada**: `Close > SMA`
- **Largo/Corto**: Solo largos
- **Criterios de salida**: `Close <= SMA` o eliminado en el rebalanceo
- **Stops**: Ninguno; el capital se redistribuye mensualmente
- **Valores predeterminados**:
  - `SmaLength` = 210
  - `MinTradeUsd` = 50
  - `CandleType` = daily
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
