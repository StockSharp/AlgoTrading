# Estrategia Color Zerolag HLR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión en C# del experto MQL5 `Exp_ColorZerolagHLR`. Combina múltiples osciladores Hi-Lo Range (HLR) con diferentes longitudes y pesos, luego aplica un suavizado exponencial para construir líneas de tendencia rápidas y lentas. Los cruces entre estas líneas generan señales de trading.

## Descripción general
- Construye cinco valores HLR usando el máximo más alto y el mínimo más bajo durante períodos especificados.
- Pondera cada HLR y los suma para producir una línea de tendencia rápida.
- Aplica suavizado sin retraso para derivar una línea de tendencia lenta.
- Opera cuando la línea rápida cruza la línea lenta.

## Parámetros
- `Smoothing` – factor de suavizado EMA.
- `Factor1`..`Factor5` – pesos para cada componente HLR.
- `HlrPeriod1`..`HlrPeriod5` – períodos de lookback para los cálculos HLR.
- `BuyPosOpen`/`SellPosOpen` – permiten abrir posiciones largas o cortas.
- `BuyPosClose`/`SellPosClose` – permiten cerrar posiciones existentes.
- `CandleType` – marco temporal de las velas.

## Indicadores
- Highest, Lowest (cinco pares cada uno).

## Lógica de trading
- Si la línea rápida anterior estaba por encima de la línea lenta y ahora cruza hacia abajo, la estrategia abre una posición larga y cierra cualquier corta.
- Si la línea rápida anterior estaba por debajo de la línea lenta y ahora cruza hacia arriba, la estrategia abre una posición corta y cierra cualquier larga.

Use esta plantilla como punto de partida y ajuste los parámetros o la gestión del riesgo según sus necesidades.
