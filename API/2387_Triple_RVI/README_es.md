# Estrategia Triple RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera utilizando el **Relative Vigor Index (RVI)** en tres marcos temporales diferentes. Las tendencias del RVI a largo plazo actúan como filtros, mientras que el marco temporal más corto se usa para las entradas. Se abre una posición larga cuando el RVI a corto plazo cruza por debajo de su línea de señal mientras ambos marcos temporales superiores permanecen alcistas. Se abre una posición corta cuando el RVI a corto plazo cruza por encima de su línea de señal y ambos marcos temporales superiores son bajistas. Las posiciones se cierran cuando cualquier marco temporal indica un cambio de tendencia contra la posición actual.

## Parámetros
- **RviPeriod** – período para calcular el RVI.
- **CandleType1** – marco temporal del filtro RVI más alto.
- **CandleType2** – marco temporal del filtro RVI intermedio.
- **CandleType3** – marco temporal de operación donde se generan las señales de entrada.
- **Volume** – tamaño de la orden para órdenes de mercado.

## Notas
- Solo se procesan las velas cerradas.
- La estrategia utiliza la API de alto nivel de StockSharp.
- Los marcos temporales predeterminados corresponden a velas de 30, 15 y 5 minutos.
