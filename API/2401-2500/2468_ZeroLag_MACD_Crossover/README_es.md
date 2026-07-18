# Estrategia de Cruce ZeroLag MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el algoritmo **ZeroLagEA-AIP** de MetaTrader 5. Utiliza un MACD de zero lag construido a partir de dos medias móviles exponenciales de zero lag. El sistema abre una posición corta cuando el valor del MACD aumenta respecto a la barra anterior y abre una posición larga cuando el MACD disminuye. Si aparece una señal opuesta mientras hay una posición abierta, la posición actual se cierra y se abre una nueva en la siguiente barra.

## Lógica

1. Se calculan dos EMAs de zero lag con períodos configurables.
2. Su diferencia multiplicada por 10 forma el valor del MACD de zero lag.
3. Una operación se ejecuta solo cuando la dirección del MACD cambia entre dos barras consecutivas (opcional).
4. El trading solo se permite entre las horas de inicio y fin configuradas. Todas las posiciones se cierran forzosamente fuera de este horario o en el día de la semana y hora especificados.

## Parámetros

- **Volume** – volumen de la orden.
- **Fast EMA** – período de la EMA rápida de zero lag.
- **Slow EMA** – período de la EMA lenta de zero lag.
- **Use Fresh Signal** – si está habilitado, opera solo en un nuevo cambio de dirección del MACD.
- **Start Hour / End Hour** – límites de la sesión de trading en UTC.
- **Kill Day / Kill Hour** – día de la semana y hora en que se cierran todas las posiciones.
- **Candle Type** – datos de velas usados para los cálculos.

## Notas

La estrategia utiliza la API de alto nivel de StockSharp con `SubscribeCandles` y `Bind` para recibir los valores de los indicadores. Las posiciones se cierran mediante órdenes de mercado.
