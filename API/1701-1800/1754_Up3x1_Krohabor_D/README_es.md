# Estrategia Up3x1 Krohabor D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia utiliza tres medias móviles simples (rápida, media, lenta) para identificar la dirección del tendencia. Se abre una posición larga cuando la MA rápida cruza por encima de la MA media y tanto la MA rápida como la MA media están por encima de la MA lenta en las barras actual y anterior. Se abre una posición corta cuando la MA rápida cruza por debajo de la MA media y tanto la MA rápida como la MA media están por debajo de la MA lenta en las barras actual y anterior.

Las posiciones se protegen con niveles de take profit, stop loss y trailing stop opcional. Las órdenes se ejecutan a precios de mercado.

## Parámetros
- **Volume** – tamaño de la orden.
- **Fast Period** – período de la SMA rápida.
- **Middle Period** – período de la SMA media.
- **Slow Period** – período de la SMA lenta.
- **Take Profit** – distancia al objetivo de beneficio en unidades de precio.
- **Stop Loss** – distancia al stop de protección en unidades de precio.
- **Trailing Stop** – distancia para la activación del trailing stop en unidades de precio.
- **Candle Type** – marco temporal de las velas usadas para los cálculos.

## Señales
- **Compra** – la MA rápida cruza por encima de la MA media y ambas MAs permanecen por encima de la MA lenta.
- **Venta** – la MA rápida cruza por debajo de la MA media y ambas MAs permanecen por debajo de la MA lenta.

## Protecciones
- Los niveles de take profit y stop loss se establecen en la entrada.
- Cuando está habilitado, el trailing stop mueve el stop de protección en la dirección de la operación a medida que el precio avanza.

## Notas
Esta es una conversión directa de la estrategia MQL original a StockSharp usando la API de alto nivel e indicadores integrados.
