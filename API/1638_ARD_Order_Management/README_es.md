# Estrategia ARD de Gestión de Órdenes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que utiliza el indicador DeMarker cruzando un umbral de 0.5 para abrir posiciones.

Cuando el DeMarker cae por debajo del umbral después de estar por encima, la estrategia compra. Cuando el DeMarker sube por encima del umbral después de estar por debajo, vende. La salida se produce en la señal opuesta. No se utiliza stop-loss ni take-profit.

## Detalles

- **Criterios de entrada**:
  - Largo: `DeMarker cruza por debajo de Threshold`
  - Corto: `DeMarker cruza por encima de Threshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `DeMarkerPeriod` = 2
  - `Threshold` = 0.5
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: DeMarker
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
