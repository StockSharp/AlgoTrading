# Estrategia de Cruce TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos indicadores TRIX (Oscilador de Media Móvil Triple Exponencial) con diferentes períodos para detectar posibles reversiones. Se abre una posición larga cuando el TRIX rápido forma un mínimo local mientras el TRIX lento está subiendo. Se abre una posición corta cuando el TRIX rápido forma un máximo local mientras el TRIX lento está bajando.

## Parámetros

- **Fast TRIX Period** – período del indicador TRIX rápido.
- **Slow TRIX Period** – período del indicador TRIX lento.
- **Take Profit** – objetivo de beneficio en unidades de precio absolutas.
- **Stop Loss** – pérdida máxima en unidades de precio absolutas.
- **Candle Type** – marco temporal o tipo de datos para las velas.

## Lógica de Trading

1. Suscribirse al tipo de vela seleccionado.
2. Calcular los valores de TRIX rápido y lento en cada vela finalizada.
3. Entrar largo cuando el valor del TRIX rápido es mayor que su valor anterior, el valor anterior es menor que el valor previo a él, y el TRIX lento está subiendo.
4. Entrar corto cuando el valor del TRIX rápido es menor que su valor anterior, el valor anterior es mayor que el valor previo a él, y el TRIX lento está bajando.
5. Solo se mantiene una posición a la vez.
6. Las protecciones de stop loss y take profit se aplican automáticamente.

## Notas

La estrategia es una adaptación de un script MQL5 y demuestra cómo trabajar con indicadores TRIX dentro de StockSharp.
