# Estrategia Diff TF MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Esta estrategia es un port a StockSharp del asesor experto de MetaTrader "Diff_TF_MA_EA".
- Las señales de trading provienen de comparar una media móvil simple calculada en un marco temporal superior con otra media móvil reescalada al marco temporal de trading.
- El código mantiene solo las velas terminadas, refleja las reglas de cruce originales y cierra cualquier exposición opuesta antes de abrir una nueva posición.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `MaPeriod` | Longitud de la media móvil simple calculada en el marco temporal superior. |
| `CandleType` | Marco temporal de trading usado para la generación de órdenes. |
| `HigherCandleType` | Marco temporal superior que proporciona la media móvil de referencia. |
| `ReverseSignals` | Invierte las reglas de cruce (comprar en cruce bajista y vender en cruce alcista). |
| `Volume` | Volumen de la estrategia usado por las llamadas `BuyMarket`/`SellMarket` (establecido mediante la propiedad base `Strategy.Volume`). |

## Lógica de trading
1. Suscribirse tanto al marco temporal de trading (`CandleType`) como al marco temporal superior (`HigherCandleType`).
2. Construir una media móvil simple con longitud `MaPeriod` en el marco temporal superior.
3. Convertir la longitud del marco temporal superior en el marco temporal de trading multiplicando por la relación de duraciones del marco temporal y ejecutar otra media móvil en las velas de trading.
4. Almacenar los dos últimos valores completados para ambas medias móviles y verificar los cruces en cada vela de trading terminada.
5. Abrir o revertir a una posición larga cuando la MA del marco temporal superior cruza por encima de la MA de trading (a menos que `ReverseSignals` sea `true`).
6. Abrir o revertir a una posición corta cuando la MA del marco temporal superior cruza por debajo de la MA de trading (a menos que `ReverseSignals` sea `true`).
7. Las posiciones se aplanan y giran enviando suficiente volumen para compensar cualquier exposición existente.

## Notas de uso
- Elegir marcos temporales compatibles: el marco temporal superior generalmente debe ser más grande que el marco temporal de trading para que la longitud reescalada sea significativa.
- El volumen predeterminado es `1`. Ajustar `Strategy.Volume` antes de iniciar la estrategia si se requiere otro tamaño.
- Los stops y take-profits de la versión de MetaTrader no están reproducidos; la gestión de riesgos se puede adjuntar a través de las protecciones de StockSharp si es necesario.
- Cuando `ReverseSignals` está habilitado, las acciones alcistas y bajistas se intercambian mientras el resto de la lógica permanece sin cambios.
