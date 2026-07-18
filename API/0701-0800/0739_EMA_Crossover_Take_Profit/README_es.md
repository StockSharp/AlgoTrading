# Estrategia de Cruce EMA con Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce de las medias móviles exponenciales (EMA) de 20 y 50 períodos. Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta, y una posición corta en el cruce opuesto.

Tras una entrada, se calculan cuatro niveles de take profit a partir del rango de la vela de señal. La posición se cierra cuando el precio alcanza cualquiera de estos niveles o cuando se activa el stop loss. Las velas se resaltan en verde cuando la EMA rápida está por encima de la EMA lenta y en rojo cuando está por debajo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA20 cruza por encima de EMA50.
  - **Corto**: EMA20 cruza por debajo de EMA50.
- **Take Profit**: Cuatro objetivos basados en multiplicadores del rango anterior.
- **Stops**: Stop loss del 3% desde el precio de entrada.
- **Indicadores**: EMA20, EMA50, EMA200.
- **Marco temporal**: Configurable mediante parámetro.
- **Dirección**: Largo y Corto.
