# ADX y estrategia de maestría
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es un puerto StockSharp del MetaTrader experto **ADX_MA (fortrader)**.
Combina un filtro de media móvil suavizada (SMMA) con el índice direccional medio (ADX)
de modo que las operaciones se realicen sólo cuando la tendencia sea confirmada por un cruce y sea lo suficientemente fuerte
según ADX. El puerto mantiene la gestión de riesgos asimétrica del robot original:
Las posiciones largas utilizan distancias amplias de toma de ganancias y de seguimiento, mientras que las posiciones cortas emplean distancias más estrictas.
objetivos y protección.

## Lógica de trading

1. Cree una media móvil suavizada sobre los precios medios de las velas y un ADX con los periodos configurados.
2. Evalúe señales en velas cerradas solo para imitar la lógica MQL4 (`iClose(...,1)` / `iClose(...,2)`).
3. Entre en largo cuando la vela anterior cierre por encima de la SMMA, la vela antes de que cierre por debajo de la
mismo valor SMMA y la lectura anterior de ADX está por encima del umbral.
4. Entre en corto cuando la vela anterior cierre por debajo de la SMMA, la vela antes de que cierre por encima de la
mismo valor SMMA y ADX está por encima del umbral.
5. Una vez en posición, las salidas son impulsadas por:
   - Voltear la media móvil en la dirección opuesta.
   - Niveles individuales de stop-loss y take-profit medidos en pips.
   - Distancias de trailing stop opcionales que aumentan a favor del comercio.

Todas las compensaciones de precios se convierten de pips utilizando el paso de precio del valor. Si el instrumento no
informar un paso válido, se utiliza un valor de 1 como respaldo seguro.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `SMMA Period` | Longitud de la media móvil suavizada (por defecto 21). |
| `ADX Period` | Longitud del índice direccional promedio (predeterminado 14). |
| `ADX Threshold` | Valor mínimo ADX requerido para permitir entradas (predeterminado 16). |
| `Long Take Profit (pips)` | Distancia de toma de ganancias para posiciones de compra (por defecto 1300 pips). |
| `Long Stop Loss (pips)` | Distancia de stop-loss para posiciones de compra (por defecto 30 pips). |
| `Long Trailing Stop (pips)` | Distancia del trailing-stop para posiciones de compra (por defecto 270 pips). |
| `Short Take Profit (pips)` | Distancia de toma de ganancias para posiciones de venta (por defecto 160 pips). |
| `Short Stop Loss (pips)` | Distancia de stop-loss para posiciones de venta (por defecto 50 pips). |
| `Short Trailing Stop (pips)` | Distancia del trailing-stop para posiciones de venta (por defecto 20 pips). |
| `Volume` | Volumen de pedidos utilizado para nuevas entradas (por defecto 0,1). |
| `Candle Type` | Serie de velas primarias para cálculos (período de tiempo predeterminado de 1 minuto). |

Todos los parámetros están expuestos para su optimización. Los valores predeterminados coinciden con la configuración original EA.

## Notas

- Los trailingstops se activan solo después de que el precio se mueve al menos la distancia configurada desde la entrada.
- Las señales opuestas cierran la posición activa antes de abrir una nueva.
- La estrategia dibuja automáticamente velas, indicadores y operaciones propias en el gráfico si hay un área del gráfico disponible.
- No existen pruebas automatizadas para este puerto; utilice backtesting manual para validar el comportamiento de sus instrumentos.
