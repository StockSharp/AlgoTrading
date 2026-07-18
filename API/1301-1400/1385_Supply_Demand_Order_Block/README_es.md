# Estrategia de Rompimiento por Oferta, Demanda y Bloque de Órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de ruptura que usa niveles de soporte y resistencia Donchian con filtro de tendencia EMA y confirmación de pico de volumen. Las posiciones están protegidas por stop loss y trailing stop.

## Detalles

- **Criterios de entrada**: Ruptura del canal Donchian con filtro de tendencia y volumen.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop loss o trailing stop.
- **Stops**: Sí, fijo y trailing.
- **Valores predeterminados**:
  - `Length` = 20
  - `StopLossTicks` = 1000
  - `TrailingStartTicks` = 2000
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian, EMA, SMA
  - Stops: Fijo y Trailing
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
