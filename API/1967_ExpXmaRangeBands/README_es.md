# Estrategia ExpXmaRangeBands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del ejemplo de MetaTrader "Exp_XMA_Range_Bands" utilizando la API de alto nivel de StockSharp. Emplea un Canal de Keltner para definir soporte y resistencia dinámicos basados en una media móvil y el rango verdadero promedio. Las operaciones se activan cuando el precio vuelve a entrar al canal después de haber salido.

## Cómo funciona

1. Construir un Canal de Keltner utilizando:
   - Periodo de EMA `MaLength`
   - Periodo de ATR `RangeLength`
   - Multiplicador del ATR `Deviation`
2. Cuando una vela cierra por encima de la banda superior anterior, se cierra cualquier posición corta. Si la siguiente vela cierra de nuevo dentro del canal (cierre ≤ banda superior actual), se abre una posición larga.
3. Cuando una vela cierra por debajo de la banda inferior anterior, se cierra cualquier posición larga. Si la siguiente vela cierra de nuevo dentro del canal (cierre ≥ banda inferior actual), se abre una posición corta.
4. Los niveles de stop-loss y take-profit se expresan en puntos y se aplican una vez que se entra en una posición.

## Parámetros

- `MaLength` – Periodo de EMA para el centro del canal.
- `RangeLength` – Periodo de ATR utilizado para el ancho del canal.
- `Deviation` – Multiplicador aplicado al ATR para calcular las bandas.
- `StopLoss` – Stop-loss en puntos (convertido a precio mediante `Security.PriceStep`).
- `TakeProfit` – Take-profit en puntos (convertido a precio mediante `Security.PriceStep`).
- `CandleType` – Serie de velas utilizada para los cálculos.

## Indicadores

- KeltnerChannels (EMA + ATR)
