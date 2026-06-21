# Estrategia de Ruptura al Alza con Retraso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra largo cuando la sesión abre con un gap alcista que supera un umbral y ha transcurrido un número específico de barras desde la entrada anterior. La posición se mantiene durante un número fijo de barras.

## Detalles

- **Criterios de entrada**: gap alcista mayor que el umbral y período de retraso satisfecho
- **Largo/Corto**: Largo
- **Criterios de salida**: al expirar el período de mantenimiento
- **Stops**: No
- **Valores predeterminados**:
  - `GapThreshold` = 1
  - `DelayPeriods` = 0
  - `HoldingPeriods` = 7
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
