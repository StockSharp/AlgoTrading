# Estrategia HMA Plus Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de media móvil Hull adaptativa que ajusta su período en función de la volatilidad o el volumen. Abre posiciones largas o cortas cuando la pendiente del HMA apunta en la dirección de la tendencia durante condiciones de mercado activas.

## Detalles

- **Criterios de entrada**: Señales basadas en HMA adaptativa, ATR o volumen.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, ATR, Volume
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

