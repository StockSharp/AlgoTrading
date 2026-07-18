# Estrategia Smart Ass Trade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Smart Ass Trade es una estrategia de seguimiento de tendencia multitemporal convertida desde la implementación MQL.
Analiza el histograma MACD (OsMA) y medias móviles simples de 20 períodos en gráficos de 5, 15 y 30 minutos.
Un filtro diario de Williams %R bloquea las operaciones en condiciones de sobrecompra o sobreventa.

## Algoritmo
1. Calcular el histograma MACD y SMA(20) en marcos temporales de 5m, 15m y 30m.
2. Definir tendencia alcista cuando el histograma crece y la SMA sube en los tres marcos temporales.
3. Definir tendencia bajista cuando el histograma cae y la SMA baja en los tres marcos temporales.
4. Usar Williams %R diario (período 26) para evitar comprar por encima de -2 o vender por debajo de -98.
5. Cuando todas las condiciones se alinean, abrir una orden de mercado en la dirección correspondiente.
6. El tamaño de posición puede ser fijo u optimizado según el valor de la cuenta.

## Parámetros
- **Hedging** – permite abrir posiciones opuestas simultáneamente.
- **LotsOptimization** – activa el cálculo dinámico del lote.
- **Lots** – volumen de trading fijo cuando la optimización está desactivada.
- **AutomaticTakeProfit** – marcador de posición para take profit dinámico, actualmente no usado.
- **MinimumTakeProfit** – objetivo de beneficio en puntos para modo manual.
- **AutomaticStopLoss** – marcador de posición para stop loss dinámico, actualmente no usado.
- **StopLoss** – stop loss en puntos para modo manual.
- **CandleType** – marco temporal base para suscripciones (por defecto 5 minutos).

## Notas
La estrategia usa la API de alto nivel con llamadas `SubscribeCandles` y `Bind`.
Los valores de take profit y stop loss se han dejado para extensión futura; la versión actual se centra en
la generación de señales y la ejecución de órdenes.
