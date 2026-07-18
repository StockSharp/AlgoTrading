# Estrategia Stochastic Automatizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera utilizando el **Oscilador Stochastic** en el marco temporal de velas seleccionado. Espera a que %K y %D entren en zonas extremas y luego actúa en los cruces para abrir posiciones. Un take profit y stop loss fijos protegen cada operación, mientras que un trailing stop asegura las ganancias.

## Lógica

1. **Entrada**
   - **Largo:**
     - Tanto %K como %D están por debajo de `OverSold` hace dos velas.
     - %D estaba por encima de %K hace dos velas y por debajo de %K hace una vela.
     - %D está subiendo.
   - **Corto:**
     - Tanto %K como %D están por encima de `OverBought` hace dos velas.
     - %D estaba por debajo de %K hace dos velas y por encima de %K hace una vela.
     - %D está bajando.
2. **Salida**
   - La posición se cierra cuando el Stochastic sale de la zona extrema o %D gira en la dirección opuesta.
   - Un trailing stop sale si el precio retrocede en `TrailingStop`.
   - Se aplican `TakeProfit` y `StopLoss` globales a cada operación.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal para los cálculos del Stochastic. |
| `KPeriod` | Período de retrospección para la línea %K. |
| `DPeriod` | Período de suavizado para la línea %D. |
| `Slowing` | Suavizado adicional para %K. |
| `OverBought` | Umbral superior que indica mercado sobrecomprado. |
| `OverSold` | Umbral inferior que indica mercado sobrevendido. |
| `TakeProfit` | Distancia desde la entrada para el objetivo de ganancia (unidades de precio). |
| `StopLoss` | Distancia desde la entrada para el stop de protección (unidades de precio). |
| `TrailingStop` | Distancia de seguimiento una vez que la operación se mueve a favor (unidades de precio). |

## Indicadores

- `StochasticOscillator`

## Notas

- Los comentarios en el código están en inglés.
- La versión de Python se omite intencionalmente.
