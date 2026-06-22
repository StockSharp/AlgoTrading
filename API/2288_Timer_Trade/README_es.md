# Estrategia de Negociación por Temporizador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Negociación por Temporizador alterna entre posiciones largas y cortas a intervalos de tiempo fijos. Un temporizador activa órdenes a mercado y cada posición es automáticamente protegida con stop-loss y take-profit.

## Detalles

- **Criterios de entrada**: Evento del temporizador.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Sí, mediante StartProtection.
- **Valores predeterminados**:
  - `TimerInterval` = TimeSpan.FromSeconds(30)
  - `Volume` = 1
  - `StopLossLevel` = 10 puntos
  - `TakeProfitLevel` = 50 puntos
- **Filtros**:
  - Categoría: Temporizador
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
