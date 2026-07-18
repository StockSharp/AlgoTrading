# Estrategia MACD Waterline Cross Expectator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia va largo cuando la línea de señal MACD cruza por encima del nivel cero y va corto cuando cruza por debajo. La gestión de riesgos utiliza un stop loss y un multiplicador de riesgo-beneficio configurable para establecer la distancia del take profit.

## Lógica
- Calcular el indicador MACD con periodos de EMA rápida, EMA lenta y señal configurables.
- Rastrear el valor de la línea de señal en cada vela completada.
- Cuando la línea de señal cruza de negativo a positivo y la estrategia está lista para comprar, se coloca una orden de mercado larga.
- Cuando la línea de señal cruza de positivo a negativo y la estrategia está lista para vender, se coloca una orden de mercado corta.
- Los niveles de stop loss y take profit se establecen automáticamente para cada nueva posición.

## Parámetros
- **FastEmaPeriod** – longitud de la EMA rápida utilizada en MACD.
- **SlowEmaPeriod** – longitud de la EMA lenta utilizada en MACD.
- **SignalPeriod** – longitud de la EMA de la línea de señal.
- **StopLoss** – distancia al stop loss en unidades de precio absolutas.
- **Volume** – tamaño de orden utilizado para nuevas posiciones.
- **RiskBenefitRatios** – proporciones preestablecidas de 1:5 a 1:1 que definen la distancia del take profit.
- **CandleType** – marco temporal de las velas utilizado por la estrategia.

## Notas
- La estrategia alterna entre operaciones largas y cortas usando una bandera interna.
- Las operaciones se ejecutan a precios de mercado y siempre cierran e invierten la posición actual.
