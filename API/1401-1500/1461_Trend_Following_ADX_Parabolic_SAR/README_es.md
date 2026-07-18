# Estrategia de Seguimiento de Tendencia ADX Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza ADX con movimiento direccional y Parabolic SAR para seguir tendencias. Las posiciones largas ocurren cuando ADX está por encima de un umbral, +DI supera a -DI y el precio está por encima de la línea SAR. Las señales cortas usan la configuración opuesta.

## Detalles

- **Criterios de entrada**: ADX > umbral con cruce de DI y precio > SAR.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, Parabolic SAR
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
