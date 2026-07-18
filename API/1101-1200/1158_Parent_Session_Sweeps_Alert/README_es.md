# Alerta de Barridos de Sesión Principal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia monitorea las sesiones diarias y detecta cuando la sesión actual barre el máximo o mínimo de la sesión anterior. Cuando ocurre un barrido y la vela cierra de vuelta dentro del rango anterior, se abre una operación en dirección opuesta con una relación riesgo-beneficio configurable.

## Detalles

- **Criterios de entrada**: Barrido del máximo/mínimo de la sesión anterior con filtro opcional de cierre de vela.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop en el extremo de la sesión o objetivo basado en el riesgo-beneficio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MinRiskReward` = 1
  - `UseCandleFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Price Action
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
