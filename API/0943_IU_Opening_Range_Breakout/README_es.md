# Estrategia IU de Ruptura del Rango de Apertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia IU Opening Range Breakout monitorea el máximo y mínimo de la primera barra de cada sesión y opera rupturas en cualquier dirección. Los stops usan el extremo de la barra anterior y los objetivos se derivan de una relación riesgo/recompensa configurable. Todas las posiciones se cierran en una hora de finalización definida por el usuario.

## Detalles

- **Criterios de entrada**:
  - Entrar largo cuando el cierre cruza por encima del máximo de la primera barra.
  - Entrar corto cuando el cierre cruza por debajo del mínimo de la primera barra.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop en el mínimo/máximo de la barra anterior.
  - Objetivo basado en la relación riesgo/recompensa.
  - Cerrar todas las posiciones en `EndTime`.
- **Stops**: Sí
- **Valores predeterminados**:
  - `RiskReward` = 2.0
  - `MaxTrades` = 2
  - `EndTime` = 15:00
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
