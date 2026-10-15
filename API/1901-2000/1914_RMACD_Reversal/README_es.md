# Estrategia de Reversión RMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia utiliza el indicador Moving Average Convergence Divergence (MACD) para generar señales de reversión. Cuatro modos diferentes definen cómo se detectan las entradas:

1. **Breakdown** – entra en largo cuando el histograma MACD cruza por debajo de cero y entra en corto cuando cruza por encima de cero.
2. **MacdTwist** – busca un cambio en la dirección del MACD comparando los dos últimos valores del histograma.
3. **SignalTwist** – monitorea la línea de señal en busca de cambios de dirección.
4. **MacdDisposition** – entra cuando el histograma MACD cruza la línea de señal.

La estrategia siempre utiliza órdenes de mercado e invierte las posiciones cuando aparece una nueva señal opuesta.

## Parámetros
- **Fast Length** – período para la EMA rápida dentro del MACD.
- **Slow Length** – período para la EMA lenta dentro del MACD.
- **Signal Length** – período de suavizado para la línea de señal.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.
- **Mode** – selecciona el algoritmo de entrada descrito anteriormente.

## Notas
- Las señales se evalúan solo en velas terminadas.
- La estrategia almacena valores anteriores del MACD internamente en lugar de solicitar datos históricos.
- No se utiliza stop-loss ni take-profit explícito; las posiciones se cierran solo con señales opuestas.
