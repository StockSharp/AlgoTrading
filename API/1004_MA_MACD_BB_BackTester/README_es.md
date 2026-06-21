# MA MACD BB BackTester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina tres indicadores seleccionables: cruce de media móvil simple, cruce de MACD o ruptura de Bandas de Bollinger. Solo un modo de indicador está activo a la vez, y la dirección de la operación puede ser largo o corto.

## Parámetros
- `CandleType` — marco temporal de las velas.
- `Indicator` — indicador a utilizar (MA, MACD, BB).
- `Direction` — dirección de la operación (Long o Short).
- `MaLength` — período de la media móvil.
- `FastLength` — longitud de la EMA rápida del MACD.
- `SlowLength` — longitud de la EMA lenta del MACD.
- `SignalLength` — longitud de la señal del MACD.
- `BbLength` — período de las Bandas de Bollinger.
- `BbMultiplier` — multiplicador de las Bandas de Bollinger.
- `StartDate` — fecha de inicio.
- `EndDate` — fecha de fin.
