# Estrategia de Indicador de Volumen de Ticks Ergodic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica el True Strength Index (TSI) a los datos de velas y lo compara con una línea de señal de media móvil exponencial. Se abre una posición larga cuando el TSI cruza por encima de la línea de señal, mientras que se abre una posición corta cuando cruza por debajo.

## Parámetros

- **Candle Type** – marco temporal de las velas usadas para los cálculos.
- **Short Length** – período de suavizado rápido del TSI.
- **Long Length** – período de suavizado lento del TSI.
- **Signal Length** – período de la EMA utilizada como línea de señal.

## Lógica

1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular el TSI para cada vela finalizada.
3. Procesar el TSI a través de una EMA para obtener una línea de señal.
4. Cuando el TSI cruza por encima de la línea de señal, entrar largo (cerrando cualquier posición corta).
5. Cuando el TSI cruza por debajo de la línea de señal, entrar corto (cerrando cualquier posición larga).

La estrategia es una adaptación del ejemplo MQL "exp_ergodic_ticks_volume_indicator.mq5" y utiliza únicamente indicadores integrados de StockSharp.
