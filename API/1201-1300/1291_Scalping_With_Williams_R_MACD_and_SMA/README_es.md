# Estrategia de Scalping con Williams %R, MACD y SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping que utiliza Williams %R, el histograma MACD y una media móvil simple en velas de un minuto.

## Detalles

- **Criterios de entrada**: Williams %R cruza los niveles de activación y el histograma MACD cambia de signo en la dirección de la tendencia.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El histograma invierte su dirección.
- **Stops**: No.
- **Valores predeterminados**:
  - `WilliamsLength` = 140
  - `MacdFast` = 24
  - `MacdSlow` = 52
  - `MacdSignal` = 9
  - `SmaLength` = 7
