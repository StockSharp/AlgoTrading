# Estrategia Color BB Candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza las Bandas de Bollinger para clasificar las velas en zonas alcistas, bajistas o neutras. Abre una posición larga cuando el precio cierra por encima de la banda superior, abre una posición corta cuando el precio cierra por debajo de la banda inferior, y cierra cualquier posición cuando el precio regresa entre las bandas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio de cierre cruza por encima de la banda superior desde afuera.
  - **Corto**: El precio de cierre cruza por debajo de la banda inferior desde afuera.
- **Criterios de salida**: El precio regresa entre las bandas superior e inferior.
- **Indicadores**: Bandas de Bollinger.
- **Valores predeterminados**:
  - `BollingerPeriod` = 100
  - `BollingerDeviation` = 1.0
  - `CandleType` = marco temporal de 4 horas
- **Dirección**: Largo y corto.
- **Stops**: Ninguno.
- **Complejidad**: Moderado.
- **Marco temporal**: Medio plazo.
