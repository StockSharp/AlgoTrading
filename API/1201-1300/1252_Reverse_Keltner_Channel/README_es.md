# Estrategia de Canal Keltner Inverso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra cuando el precio vuelve a entrar en el canal Keltner desde el exterior y apunta a la banda opuesta, con filtro ADX opcional.

La estrategia va largo cuando el precio cruza la banda inferior de Keltner desde abajo y cierra en la banda superior o en un stop colocado a la mitad del ancho del canal. Las operaciones cortas son simétricas. Un filtro ADX puede restringir las operaciones a regímenes de tendencia débil o fuerte.

## Detalles

- **Criterios de entrada**: El precio cruza la banda exterior de Keltner hacia el canal, filtro ADX opcional.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Banda opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 2m
  - `StopLossFactor` = 0.5m
  - `AdxLength` = 14
  - `AdxThreshold` = 25m
  - `UseAdxFilter` = true
  - `WeakTrendOnly` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Keltner, ADX
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
