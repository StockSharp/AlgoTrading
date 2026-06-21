# Estrategia de ATR No Estacionalizado y Pronóstico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia analiza el rango de negociación promedio de las velas recientes y pronostica el siguiente rango utilizando regresión de tendencia lineal. No realiza operaciones, sino que muestra estadísticas que pueden usarse para decisiones manuales.

## Parámetros

- **SampleSize** – número de velas recientes utilizadas para los cálculos.
- **DesiredRange** – rango objetivo utilizado para la estimación del intervalo de confianza.
- **CandleType** – serie de velas a analizar.

## Indicadores

- SimpleMovingAverage – se utiliza para calcular el rango promedio.
- StandardDeviation – mide la volatilidad del rango.
- Regresión lineal (personalizada) – pronostica el siguiente rango y el MAPE.

## Comportamiento

Para cada vela finalizada, la estrategia:

1. Calcula el rango (máximo menos mínimo) y actualiza el promedio y la desviación estándar.
2. Estima un intervalo de confianza para el rango deseado.
3. Construye una tendencia lineal de los rangos y pronostica el siguiente.
4. Evalúa el error porcentual absoluto medio (MAPE) del pronóstico.

Los valores se registran en la salida de la estrategia y pueden visualizarse en el gráfico.

## Notas

- La estrategia es informativa y no ejecuta órdenes.
- Los rangos se miden en unidades de precio; adapte el parámetro `DesiredRange` a su instrumento.
