# Estrategia de bronce
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una versión StockSharp del MetaTrader 4 asesor experto "Bronzew_pan". Opera con un solo instrumento en velas terminadas y combina el oscilador patentado DayImpuls con Williams %R y el índice de canales de productos básicos (CCI) para detectar reversiones de impulso.

## como funciona

1. Se suscribe al tipo de vela configurado y ejecuta DayImpuls, Williams %R y CCI con el mismo período.
2. Mantiene una contabilidad independiente de las exposiciones largas y cortas para emular el comportamiento de cobertura original.
3. Cierra todas las posiciones una vez que el beneficio flotante alcanza `ProfitTarget` o cae por debajo de `LossTarget`.
4. Abre un corto cuando DayImpuls se mantiene por encima de `DayImpulsShortLevel` y disminuye, mientras que Williams %R está por encima de `WilliamsLevelUp` y CCI supera `CciLevel`.
5. Abre una posición larga cuando DayImpuls permanece por debajo de `DayImpulsLongLevel` y sube, mientras que Williams %R está por debajo de `WilliamsLevelDown` y CCI es menor que `-CciLevel`.
6. Si el PnL flotante se mueve más allá de los límites de `PredBand`, la estrategia envía una orden promedio grande multiplicada por `LotMultiplier` para invertir la dirección, reflejando la lógica de recuperación de emergencia de MetaTrader.
7. Los valores individuales de stop-loss y take-profit se monitorean para cestas largas y cortas utilizando distancias de pips convertidas a precios.
8. No se abren nuevas operaciones cuando el saldo de la cuenta cae por debajo de `MinimumBalance` o cuando están activas tanto las cestas largas como las cortas.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Volumen base para entradas. | `0.1` |
| `LongStopLossPips` | Distancia de stop-loss para cestas largas en pips. | `0` |
| `ShortStopLossPips` | Distancia de stop-loss para cestas cortas en pips. | `0` |
| `LongTakeProfitPips` | Distancia de obtención de beneficios para cestas largas en pips. | `0` |
| `ShortTakeProfitPips` | Distancia de obtención de beneficios para cestas cortas en pips. | `0` |
| `IndicatorPeriod` | Longitud utilizada por DayImpuls, Williams %R y CCI. | `14` |
| `CciLevel` | Umbral absoluto CCI que confirma sobrecompra/sobreventa. | `150` |
| `WilliamsLevelUp` | Williams Nivel %R requerido para cortos. | `-15` |
| `WilliamsLevelDown` | Williams Nivel %R requerido para posiciones largas. | `-85` |
| `DayImpulsShortLevel` | Nivel DayImpuls que permite entradas cortas. | `50` |
| `DayImpulsLongLevel` | Nivel DayImpuls que permite entradas largas. | `-50` |
| `ProfitTarget` | Beneficio flotante que cierra cada posición. | `500` |
| `LossTarget` | Pérdida flotante que cierra todas las posiciones. | `-2000` |
| `PredBand` | Banda de ganancias utilizada para desencadenar reversiones promedio. | `100` |
| `LotMultiplier` | Multiplicador aplicado al volumen base durante las reversiones. | `30` |
| `MinimumBalance` | Se requiere un saldo mínimo de cuenta para seguir operando. | `3000` |
| `CandleType` | Plazo utilizado para las suscripciones de velas. | `15m` |

## Notas

- El oscilador DayImpuls replica el doble suavizado original EMA sobre los cuerpos de las velas expresado en puntos.
- Los valores de stop-loss y take-profit son opcionales; la configuración `0` desactiva el lado de protección respectivo.
- La estrategia se basa en velas terminadas e ignora las barras incompletas.
