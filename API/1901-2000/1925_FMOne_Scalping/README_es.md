# Estrategia de Scalping FmOne
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Scalping FmOne es una traducción simplificada del asesor experto FMOneEA de MetaTrader 4. La estrategia combina una media móvil exponencial rápida y una lenta con el indicador MACD para capturar el momentum a corto plazo en cualquier marco temporal.

## Cómo Funciona
1. Las EMA rápida y lenta definen la dirección actual de la tendencia.
2. El histograma MACD confirma el momentum en la dirección de la tendencia.
3. Se abre una orden de compra cuando la EMA rápida está por encima de la EMA lenta y el histograma MACD es positivo.
4. Se abre una orden de venta cuando la EMA rápida está por debajo de la EMA lenta y el histograma MACD es negativo.
5. Cada posición está protegida con niveles de stop-loss y take-profit configurables. El trailing stop se puede activar para seguir movimientos rentables.

## Parámetros
- **FastMaPeriod** – Longitud de la EMA rápida.
- **SlowMaPeriod** – Longitud de la EMA lenta.
- **MacdSignalPeriod** – Período de la línea de señal del indicador MACD.
- **StopLossPercent** – Tamaño del stop-loss en porcentaje del precio de entrada.
- **TakeProfitPercent** – Tamaño del take-profit en porcentaje del precio de entrada.
- **EnableTrailingStop** – Activa la gestión del trailing stop.
- **CandleType** – Marco temporal para las velas entrantes.

## Notas
Este port se enfoca en la lógica central del EA original. Las funciones avanzadas como los ciclos de redención y la automatización del break-even de la versión MQL se omiten intencionalmente para mantener el ejemplo legible.
