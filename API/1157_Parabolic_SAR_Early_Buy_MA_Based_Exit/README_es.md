# Estrategia Parabolic SAR de Compra Anticipada con Salida Basada en MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Parabolic SAR para entrar en operaciones cuando el indicador cambia de lado respecto al precio. Una media móvil simple proporciona una regla de salida adicional: las posiciones largas se cierran cuando el precio cae por debajo de la media móvil mientras el SAR está por encima del precio.

## Detalles

- **Criterios de entrada**: El SAR cambia de lado respecto al precio.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Para posiciones largas, salir cuando SAR > precio y precio < MA.
- **Stops**: No definidos.
- **Valores predeterminados**:
  - `Acceleration` = 0.02
  - `AccelerationStep` = 0.02
  - `MaxAcceleration` = 0.2
  - `MaPeriod` = 11
  - `CandleType` = 5 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, SMA
  - Stops: Ninguno
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
