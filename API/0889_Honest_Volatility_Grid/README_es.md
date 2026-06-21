# Honest Volatility Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en múltiples niveles de Keltner Channel para construir una rejilla de volatilidad. Escala en posiciones largas y cortas a través de bandas predefinidas y sale mediante niveles opuestos o un stop bruto.

## Detalles

- **Criterios de entrada**: El precio alcanza los niveles configurados del canal Keltner.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Canal opuesto o stop bruto.
- **Stops**: Stop bruto opcional.
- **Valores predeterminados**:
  - `EmaPeriod` = 200
  - `Multiplier` = 1.0
  - `LEntry1Level` = -2
  - `SEntry1Level` = 2
  - `RawStopLevel` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Rejilla
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
