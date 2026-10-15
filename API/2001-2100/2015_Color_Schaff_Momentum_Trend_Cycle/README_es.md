# Estrategia de Ciclo de Tendencia de Momentum Color Schaff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza el Color Schaff Momentum Trend Cycle (STC) para detectar reversiones de tendencia cuando el indicador sale de zonas de sobrecompra o sobreventa.

## Detalles

- **Criterios de entrada**:
  - Comprar cuando el color STC anterior estaba por encima de la zona superior (>5) y el color actual cae por debajo de 6, cerrando cualquier posición corta.
  - Vender cuando el color STC anterior estaba por debajo de la zona inferior (<2) y el color actual sube por encima de 1, cerrando cualquier posición larga.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: La señal inversa cierra la posición opuesta.
- **Stops**: Sin stop loss ni take profit explícito.
- **Valores predeterminados**:
  - `FastMomentum` = 23
  - `SlowMomentum` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true

