# Estrategia de Seguimiento de Tendencia Triple EMA + QQE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina dos líneas TEMA con un filtro QQE.
Abre posiciones largas cuando el precio está por encima de ambas líneas TEMA y QQE da una señal alcista.
Las posiciones cortas se abren en condiciones opuestas.
Un stop de seguimiento en puntos protege las operaciones abiertas.

## Detalles

- **Criterios de entrada**: Alineación de TEMA con cruce de QQE.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop de seguimiento.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, QQE
  - Stops: Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
