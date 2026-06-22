# Estrategia de Cruce Spectral RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Spectral RVI Crossover suaviza el Relative Vigor Index y su línea de señal y opera en sus cruces.
Compra cuando el RVI suavizado cruza por encima de la línea de señal suavizada y vende cuando ocurre lo contrario.

## Detalles

- **Criterios de entrada**: cruce del RVI suavizado con su línea de señal suavizada
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `RviLength` = 14
  - `SignalLength` = 4
  - `SmoothLength` = 20
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RVI, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: 4 horas
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
