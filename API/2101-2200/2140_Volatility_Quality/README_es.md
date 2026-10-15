# Estrategia de Calidad de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de muestra que demuestra cómo operar usando los cambios de dirección de un precio mediano suavizado. El experto MQL original usaba el indicador *Volatility Quality*; esta implementación lo aproxima con una media móvil simple del precio mediano.

## Lógica de la estrategia
- Calcular el precio mediano de cada vela `(High + Low) / 2`.
- Suavizar el precio mediano con una Media Móvil Simple (SMA).
- Determinar el color del indicador: los valores en ascenso se tratan como **arriba** (color 0) y los valores en descenso como **abajo** (color 1).
- Cuando el color cambia de arriba a abajo, la estrategia cierra cualquier posición corta y abre una posición larga.
- Cuando el color cambia de abajo a arriba, la estrategia cierra cualquier posición larga y abre una posición corta.
- Se aplica gestión de riesgo básica mediante niveles fijos de stop loss y take profit.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `Length` | Período de suavizado para la SMA aplicada al precio mediano. |
| `Candle Type` | Marco temporal de las velas utilizadas para los cálculos. |

## Aviso legal
Este ejemplo se proporciona con fines educativos. Simplifica el algoritmo original y puede comportarse de manera diferente a la versión MQL. Úselo bajo su propia responsabilidad.
