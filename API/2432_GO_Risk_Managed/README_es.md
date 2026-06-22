# Estrategia GO Risk Managed
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port en C# del script original de MetaTrader "GO". Calcula un oscilador personalizado a partir de medias móviles de los precios de apertura, máximo, mínimo y cierre, y lo utiliza para determinar la dirección del mercado.

## Lógica de la estrategia

1. Se construyen cuatro medias móviles con el mismo período y método para las series Open, High, Low y Close.
2. El valor *GO* se calcula en cada vela completada:
   
   `GO = ((MA_close - MA_open) + (MA_high - MA_open) + (MA_low - MA_open) + (MA_close - MA_low) + (MA_close - MA_high)) * Volume`
3. Cuando el valor GO se vuelve positivo, se cierran todas las posiciones cortas y se abre una nueva posición larga.
4. Cuando el valor GO se vuelve negativo, se cierran todas las posiciones largas y se abre una nueva posición corta.
5. Solo se permite una operación por barra. Se toman nuevas entradas hasta que el número total de posiciones abiertas alcance **Max Positions**.

## Parámetros

- **Risk %** – porcentaje del capital de la cuenta utilizado para calcular el volumen de la operación.
- **Max Positions** – número máximo de posiciones abiertas permitidas en una dirección.
- **MA Type** – tipo de media móvil (SMA, EMA, DEMA, TEMA, WMA, VWMA).
- **MA Period** – período para todas las medias móviles.
- **Candle Type** – serie de velas utilizada para los cálculos de indicadores.

## Notas

La implementación utiliza la API de alto nivel de StockSharp. Se suscribe a velas, vincula indicadores y los dibuja en el gráfico. El volumen de la operación se ajusta según el porcentaje de riesgo especificado y los límites de volumen del instrumento.
