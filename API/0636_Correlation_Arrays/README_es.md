# Estrategia de Arrays de Correlación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula una matriz de correlación deslizante para hasta seis valores. Registra los niveles de correlación usando umbrales configurables para ayudar a evaluar las relaciones entre activos. La estrategia es solo de análisis y no coloca operaciones.

## Detalles
- **Criterios de entrada**: Ninguno (solo análisis)
- **Largo/Corto**: Ninguno
- **Criterios de salida**: Ninguno
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `LookbackPeriod` = 100
  - `PositiveWeak` = 0.3
  - `PositiveMedium` = 0.5
  - `PositiveStrong` = 0.7
  - `NegativeWeak` = -0.3
  - `NegativeMedium` = -0.5
  - `NegativeStrong` = -0.7
- **Filtros**:
  - Categoría: Análisis estadístico
  - Dirección: Ninguno
  - Indicadores: Correlación
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
