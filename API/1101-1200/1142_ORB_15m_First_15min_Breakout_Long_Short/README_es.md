# ORB 15m – Rompimiento de los Primeros 15 Minutos (Largo/Corto)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra al cierre de la primera barra de 15 minutos después de la apertura de la sesión en hora de Estocolmo. Una primera barra alcista activa una operación larga; una barra bajista activa una operación corta. El tamaño de la posición se calcula a partir del porcentaje de riesgo y la distancia al stop.

## Detalles

- **Criterios de entrada**: operar en la primera barra de 15 minutos después de la apertura de la sesión; largo si la barra cierra por encima de su apertura, corto si cierra por debajo.
- **Criterios de salida**: stop-loss en el extremo opuesto de la barra de referencia; take profit opcional en `RMultiple` veces el riesgo, o al final de la sesión.
- **Largo/Corto**: Ambos.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RiskPct = 1`
  - `TpTenR = true`
  - `RMultiple = 10`
  - `SessionOpenHour = 15`
  - `SessionOpenMinute = 30`
  - `SessionEndHour = 22`
  - `SessionEndMinute = 0`
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
