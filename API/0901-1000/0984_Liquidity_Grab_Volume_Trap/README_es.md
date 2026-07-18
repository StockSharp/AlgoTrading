# Estrategia de Trampa de Volumen por Captura de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia espera una captura de liquidez bajista en volumen plano que forma una brecha de valor razonable. Cuando el precio cierra por encima de la parte superior de la brecha mientras el volumen se mantiene cerca de su media móvil, coloca una orden limitada de compra en la parte inferior de la brecha con stop loss y take profit simétricos.

## Detalles

- **Condición de entrada**: `Close[2] < Open[1]` && `Close > High[1]` && ruptura bajista con volumen plano
- **Criterios de salida**: stop loss a la altura de la brecha por debajo del fondo, take profit en `High[1]`
- **Tipo**: Reversión
- **Indicadores**: SMA de Volumen
- **Marco temporal**: 1 minuto (predeterminado)
