# Estrategia de Robot Autónomo de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Robot Autónomo de 5 Minutos opera en un marco temporal de 5 minutos.
Abre una posición larga cuando el precio está en tendencia alcista y la presión compradora supera a la vendedora,
y abre una posición corta en condiciones opuestas.

## Detalles

- **Criterios de entrada**: Tendencia alcista (cierre por encima de la SMA de 50 períodos y por encima del cierre de 6 barras atrás) con volumen comprador mayor que el vendedor.
- **Criterios de salida**: Reversión de posición ante señal opuesta.
- **Stops**: Stop-loss del 3% y take profit del 29% desde el precio de entrada.
- **Valores predeterminados**:
  - `MaLength` = 50
  - `VolumeLength` = 10
  - `StopLossPercent` = 3
  - `TakeProfitPercent` = 29
  - `CandleType` = 5m
