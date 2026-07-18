# Estrategia de Acción de Precio Pura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de acción de precio simplificada que detecta Ruptura de Estructura (BOS) y Cambio de Estructura de Mercado (MSS) a partir de máximos y mínimos recientes.

La estrategia entra largo en BOS y corto en MSS con porcentajes fijos de stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: BOS para largo, MSS para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Porcentaje fijo.
- **Valores predeterminados**:
  - `Length` = 5
  - `SlPercent` = 1m
  - `TpPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
