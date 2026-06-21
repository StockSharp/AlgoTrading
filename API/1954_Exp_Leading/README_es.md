# Estrategia Exp Leading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de cruce basado en el indicador personalizado **Leading** descrito por John F. Ehlers en *Cybernetics Analysis for Stock and Futures*. El indicador calcula dos líneas:

1. **NetLead** – filtro líder suavizado controlado por los coeficientes `Alpha1` y `Alpha2`.
2. **EMA** – una media móvil exponencial simple con un factor constante de 0.5.

La estrategia opera sobre velas cerradas del marco temporal seleccionado. Cuando la línea NetLead cruza **por debajo** de la línea EMA, se anticipa una reversión alcista y se abre una posición larga. Por el contrario, cuando NetLead cruza **por encima** de la línea EMA, se abre una posición corta. La posición anterior, si existe, se cierra implícitamente cuando se envía una orden opuesta.

## Parámetros

- `Alpha1` – coeficiente para el cálculo intermedio del líder. Predeterminado: `0.25`.
- `Alpha2` – factor de suavizado aplicado al resultado líder. Predeterminado: `0.33`.
- `CandleType` – tipo de datos de vela usado para los cálculos. Predeterminado: marco temporal de 4 horas.
- `StopLoss` – stop loss en unidades absolutas de precio. Predeterminado: `1000`.
- `TakeProfit` – take profit en unidades absolutas de precio. Predeterminado: `2000`.

## Lógica de Trading

1. Cada vela cerrada actualiza los valores de NetLead y EMA.
2. Si la barra anterior mostró NetLead por encima de EMA y la última barra muestra NetLead por debajo de EMA, se envía una orden de mercado de **compra**.
3. Si la barra anterior mostró NetLead por debajo de EMA y la última barra muestra NetLead por encima de EMA, se envía una orden de mercado de **venta**.
4. Se utiliza `StartProtection` para aplicar automáticamente las reglas de stop-loss y take-profit.

Este ejemplo está destinado a fines educativos para demostrar cómo una estrategia de MetaTrader puede portarse a la API de alto nivel de StockSharp.
