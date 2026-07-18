# Estrategia WPR de Cruce de Nivel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el oscilador Williams %R al cruzar niveles predefinidos de sobrecompra y sobreventa.

Cuando el indicador cruza por debajo del **Low Level**, señala una posible reversión desde una condición de sobreventa. Cuando cruza por encima del **High Level**, indica una posible reversión desde una condición de sobrecompra. Dependiendo del **Trend Mode** seleccionado, la estrategia puede operar en la dirección del indicador o invertir las señales para operar en contratendencia.

## Parámetros

- `WprPeriod` – período de lookback para Williams %R.
- `HighLevel` – umbral de sobrecompra.
- `LowLevel` – umbral de sobreventa.
- `Trend` – `Direct` opera con las señales del indicador, `Against` las invierte.
- `EnableBuyEntry` / `EnableSellEntry` – permitir entrar en posiciones largas/cortas.
- `EnableBuyExit` / `EnableSellExit` – permitir cerrar posiciones cortas/largas.
- `StopLoss` – valor del stop-loss en unidades de precio.
- `TakeProfit` – valor del take-profit en unidades de precio.
- `CandleType` – marco temporal de las velas usadas para los cálculos.

## Cómo Funciona

1. La estrategia se suscribe a velas y calcula el indicador Williams %R.
2. En cada vela terminada verifica si el indicador cruzó los niveles especificados.
3. Dependiendo de `Trend` y las acciones habilitadas, abre o cierra posiciones usando órdenes de mercado.
4. La protección opcional de stop-loss y take-profit se activa mediante `StartProtection`.

## Notas

- Los comentarios en el código están en inglés.
- Solo se implementa la versión C#; la versión Python se omite intencionalmente.
