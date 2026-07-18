# Estrategia Snowieso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una **Media Móvil Ponderada Lineal (LWMA)** rápida y lenta con **MACD** y la **Media Móvil Adaptativa de Kaufman (KAMA)** para confirmar la dirección de la tendencia.

## Cómo Funciona
1. Suscribirse a las velas del marco temporal elegido.
2. Calcular los valores de Fast LWMA, Slow LWMA, MACD y KAMA.
3. **Entrada larga**: ocurre cuando la LWMA rápida cruza por encima de la LWMA lenta, el histograma del MACD es positivo y la KAMA sube.
4. **Entrada corta**: ocurre cuando la LWMA rápida cruza por debajo de la LWMA lenta, el histograma del MACD es negativo y la KAMA cae.
5. Se aplica un stop loss y take profit fijos mediante `StartProtection`.

La estrategia cierra posiciones opuestas antes de abrir nuevas y visualiza indicadores y operaciones en un gráfico.

## Parámetros
- `FastLength` – período de la LWMA rápida.
- `SlowLength` – período de la LWMA lenta.
- `MacdFast`, `MacdSlow`, `MacdSignal` – configuración del MACD.
- `KamaLength` – período de lookback para KAMA.
- `StopLossPoints` – stop loss absoluto en puntos de precio.
- `TakeProfitPoints` – take profit absoluto en puntos de precio.
- `CandleType` – marco temporal de las velas procesadas.

## Uso
Despliegue la estrategia en el instrumento seleccionado. El algoritmo se suscribe automáticamente a las velas y gestiona posiciones basándose en señales de indicadores. Se utiliza la API de alto nivel para el enlace de datos y la ejecución de órdenes.
