# Estrategia Medico Action Zone Self Adjust TF Versión 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de EMA con confirmación de marco temporal superior. Se abre una posición cuando la EMA rápida cruza por encima de la EMA lenta y el cierre del marco temporal superior está por encima de la EMA rápida. La posición se invierte con la señal contraria.

## Detalles

- **Criterios de entrada**: La EMA rápida cruza por encima de la EMA lenta con el cierre del marco temporal superior sobre la EMA rápida.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto con confirmación.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
