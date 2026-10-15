# Estrategia de Seguimiento de Tendencia con Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue la tendencia utilizando una media móvil y señales simples de velas.
Compra cuando el precio está por encima de la media móvil con una vela alcista rompiendo la resistencia pivot, y vende cuando el precio está por debajo de la media móvil con una vela bajista rompiendo el soporte pivot.

## Detalles

- **Criterios de entrada**: vela alcista/bajista por encima/por debajo de la MA rompiendo niveles pivot
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `MaPeriod` = 10
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
