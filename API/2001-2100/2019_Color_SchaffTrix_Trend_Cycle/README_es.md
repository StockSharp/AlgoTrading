# Estrategia de Ciclo de Tendencia Color Schaff TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el oscilador **Schaff Trend Cycle** calculado sobre el MACD basado en TRIX. El oscilador identifica cambios cíclicos de tendencia y genera señales de trading cuando el ciclo cruza niveles predefinidos.

## Cómo funciona

1. Se calculan dos osciladores TRIX con diferentes longitudes para construir una serie MACD.
2. Los valores del MACD se procesan mediante una doble transformación estocástica para producir el Schaff Trend Cycle (STC).
3. Se abre una posición larga cuando el STC cruza por encima del nivel alto y una posición corta cuando cruza por debajo del nivel bajo.
4. Las posiciones existentes se cierran cuando ocurre un cruce opuesto.

## Parámetros

- **Fast TRIX** – longitud del oscilador TRIX rápido.
- **Slow TRIX** – longitud del oscilador TRIX lento.
- **Cycle** – período utilizado en los cálculos estocásticos.
- **High Level / Low Level** – umbrales superior e inferior para el STC.
- **Stop Loss % / Take Profit %** – parámetros de control de riesgo expresados en porcentaje.
- **Buy/Sell Open/Close** – habilitar o deshabilitar las operaciones correspondientes.

## Notas

La estrategia utiliza datos de velas del marco temporal seleccionado (por defecto 4 horas) y ejecuta órdenes de mercado. La protección está habilitada con valores de stop-loss y take-profit. Todo el procesamiento de indicadores se realiza mediante la API de alto nivel con enlaces automáticos.

Utilice esta estrategia con fines educativos y realice backtesting exhaustivo antes de operar en vivo.
