# Estrategia de Cruce RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Cruce RVI utiliza el Índice de Vigor Relativo y un filtro de media móvil.
Compra cuando el RVI cruza por encima de su línea de señal mientras el precio está por debajo de la EMA, y vende cuando el RVI cruza por debajo de la señal mientras el precio está por encima de la EMA.

## Detalles

- **Criterios de entrada**: RVI cruzando su línea de señal con filtro EMA vs VWMA
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `RviLength` = 10
  - `SignalLength` = 10
  - `EmaLength` = 31
  - `VwmaLength` = 1
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RVI, SMA, EMA, VWMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
