# Estrategia de Continuación de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia identifica la continuación de la tendencia prevaleciente usando un par de medias móviles exponenciales sobre los datos de precio. Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta, señalando una continuación alcista. Se abre una posición corta cuando la EMA rápida cruza por debajo de la EMA lenta.

## Parámetros
- **Fast EMA Length** – período para la EMA rápida (por defecto: 20).
- **Candle Type** – marco temporal de las velas (por defecto: 4 horas).
- **Stop Loss** – stop loss protector aplicado mediante `StartProtection` (por defecto: 1000).
- **Take Profit** – objetivo de beneficio aplicado mediante `StartProtection` (por defecto: 2000).

## Cómo funciona
1. Al inicio, la estrategia se suscribe a la serie de velas seleccionada y crea dos indicadores EMA.
2. Cada vela completada se procesa para detectar cruces entre la EMA rápida y la lenta.
3. Un cruce de abajo hacia arriba abre una posición larga y cierra cualquier posición corta. El cruce opuesto abre una posición corta y cierra cualquier posición larga.
4. La gestión del riesgo se maneja mediante los parámetros integrados de stop loss y take profit.

Este ejemplo es una conversión simplificada del experto MQL original `Exp_TrendContinuation`.
