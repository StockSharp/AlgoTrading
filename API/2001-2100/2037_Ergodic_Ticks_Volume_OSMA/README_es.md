# Estrategia Ergodic Ticks Volume OSMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia adapta el experto MQL5 "Exp_Ergodic_Ticks_Volume_OSMA" a StockSharp. El experto original utiliza un indicador personalizado para evaluar el momentum del volumen de ticks. En esta versión, el indicador personalizado se aproxima con el histograma MACD.

La estrategia busca incrementos o decrementos consecutivos en el histograma:
- Dos pasos ascendentes desencadenan una entrada larga y cierran cualquier posición corta.
- Dos pasos descendentes desencadenan una entrada corta y cierran cualquier posición larga.

Se utiliza `StartProtection()` para evitar conflictos con posiciones existentes cuando la estrategia inicia.

## Parámetros
- `FastLength` – período EMA rápido para el MACD. Predeterminado: 12.
- `SlowLength` – período EMA lento para el MACD. Predeterminado: 26.
- `SignalLength` – período EMA de señal para el MACD. Predeterminado: 9.
- `CandleType` – marco temporal de las velas, predeterminado 8 horas.

## Lógica de trading
1. Suscribirse a las velas del `CandleType` seleccionado.
2. Calcular el histograma MACD para cada vela finalizada.
3. Si el histograma crece durante dos barras consecutivas, cerrar cortos y comprar.
4. Si el histograma cae durante dos barras consecutivas, cerrar largos y vender.
5. Continuar procesando con cada nueva vela.
