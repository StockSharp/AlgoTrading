# OBV Modificado con Detección de Divergencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia suaviza el On-Balance Volume (OBV) con una media móvil seleccionable y genera una línea de señal. Las operaciones ocurren cuando el OBV suavizado cruza la señal. Además, la estrategia registra divergencias regulares y ocultas entre el precio y el OBV mediante detección de fractales.

## Detalles

- **Criterios de entrada**: OBV-M cruza por encima/debajo de la línea de señal.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `MaType` = Exponential
  - `ObvMaLength` = 7
  - `SignalLength` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: OBV, MA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
