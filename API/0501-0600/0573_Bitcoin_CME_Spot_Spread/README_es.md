# Diferencial Bitcoin CME-Spot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera el diferencial entre los futuros de Bitcoin de CME y el spot de Bitfinex BTCUSD usando Bandas de Bollinger.
Largo cuando el diferencial cae por debajo de la banda inferior, corto cuando sube por encima de la banda superior.
Las posiciones se reducen escalonadamente en cuatro niveles de take-profit y se cierran tras un número fijo de barras.

## Detalles

- **Datos**: Futuros de Bitcoin de CME y spot Bitfinex BTCUSD.
- **Entrada**: Largo en diferencial sobrevendido, corto en diferencial sobrecomprado.
- **Salida**: Take-profits escalonados o cierre tras las barras de retención.
- **Instrumentos**: Futuros de Bitcoin.
- **Riesgo**: Salidas parciales y cierre temporizado.
