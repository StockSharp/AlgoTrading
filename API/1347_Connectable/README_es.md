# Estrategia Conectable
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia plantilla que puede conectarse a fuentes de señales externas.
Admite direcciones largas y cortas y aplica stop-loss y take-profit basados en porcentaje.

## Detalles

- **Criterios de entrada**: señal externa
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal externa o stop-loss/take-profit
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 1 minuto
  - `StopLossPercent` = 2%
  - `TakeProfitPercent` = 4%
- **Filtros**:
  - Categoría: Otro
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
