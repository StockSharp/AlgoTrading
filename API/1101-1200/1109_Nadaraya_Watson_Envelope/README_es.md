# Estrategia de Envolvente Nadaraya-Watson
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Construye envolventes de regresión kernel Nadaraya-Watson en escala logarítmica. Va largo cuando el precio cruza por encima de la envolvente inferior y opcionalmente va corto cuando el precio cruza por debajo de la envolvente superior.

## Detalles

- **Criterios de entrada**:
  - Largo cuando el cierre cruza por encima de la envolvente inferior.
  - Corto cuando el cierre cruza por debajo de la envolvente superior (en modo Largo/Corto).
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Cruce inverso de la envolvente.
- **Stops**: No.
- **Valores predeterminados**:
  - `LookbackWindow` = 8
  - `RelativeWeighting` = 8
  - `StartRegressionBar` = 25
  - `StrategyType` = Long Only
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Envelope
  - Dirección: Configurable
  - Indicadores: Nadaraya-Watson
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
