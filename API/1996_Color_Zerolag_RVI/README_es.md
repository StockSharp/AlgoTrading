# Estrategia Color Zerolag RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Índice de Vigor Relativo y su línea de señal.
Compra cuando la línea principal del RVI cruza por debajo de la línea de señal y vende cuando la línea principal cruza por encima de la línea de señal.

## Detalles

- **Criterios de entrada**: Cruce del RVI y la línea de señal
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `RviLength` = 14
  - `SignalLength` = 9
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
  - `CandleType` = 4 horas
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RVI, SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (H4)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
