# Estrategia de Reversión de Momentum de Cuatro Barras
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Reversión de Momentum de Cuatro Barras entra en largo cuando el cierre ha estado por debajo del cierre de hace `Lookback` barras durante al menos `BuyThreshold` velas consecutivas dentro de la ventana de tiempo seleccionada. La posición se cierra una vez que el precio rompe por encima del máximo de la vela anterior.

## Detalles

- **Criterios de entrada**: `BuyThreshold` cierres consecutivos por debajo del cierre de hace `Lookback` barras dentro de la ventana de tiempo.
- **Criterios de salida**: Precio de cierre mayor que el máximo de la vela anterior.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `BuyThreshold` = 4
  - `Lookback` = 4
  - `StartTime` = 2014-01-01
  - `EndTime` = 2099-01-01
