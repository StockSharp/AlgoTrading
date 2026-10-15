# Estrategia de Señal de Cruce de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en el cruce de dos Medias Móviles Exponenciales (EMA). Una EMA más rápida y una más lenta se calculan a partir de la serie de velas elegida. Cuando la EMA rápida cruza por encima de la EMA lenta, la estrategia puede cerrar cualquier posición corta existente y opcionalmente abrir una posición larga. Cuando la EMA rápida cruza por debajo de la EMA lenta, puede cerrar una posición larga y opcionalmente abrir una posición corta.

Para gestionar el riesgo, la estrategia permite colocar órdenes de take profit y stop loss después de abrir una nueva posición. Ambas distancias se especifican en ticks. Estas órdenes de protección se cancelan y recrean en cada nueva entrada.

La estrategia proporciona interruptores separados para habilitar o deshabilitar entradas largas y cortas, así como para cerrar independientemente posiciones largas y cortas en la señal opuesta. Todos los cálculos usan únicamente velas finalizadas.

## Parámetros
- **Fast Period** – longitud de la EMA rápida.
- **Slow Period** – longitud de la EMA lenta.
- **Candle Type** – marco temporal de las velas usadas para los cálculos.
- **Allow Buy Open** – abrir largo cuando la EMA rápida cruza por encima de la EMA lenta.
- **Allow Sell Open** – abrir corto cuando la EMA rápida cruza por debajo de la EMA lenta.
- **Allow Buy Close** – cerrar largo cuando la EMA rápida cruza por debajo de la EMA lenta.
- **Allow Sell Close** – cerrar corto cuando la EMA rápida cruza por encima de la EMA lenta.
- **Take Profit Ticks** – distancia de take profit en ticks desde el precio de entrada.
- **Stop Loss Ticks** – distancia de stop loss en ticks desde el precio de entrada.
