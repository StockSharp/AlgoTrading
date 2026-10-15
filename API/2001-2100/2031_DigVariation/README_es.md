# Estrategia DigVariation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia está inspirada en el ejemplo MQL5 *DigVariation*. Aproxima el indicador usando una media móvil simple (SMA) y abre operaciones cuando la dirección de la SMA cambia.

## Lógica
- La SMA se calcula sobre las velas entrantes.
- Si los valores anteriores de la SMA muestran una pendiente ascendente y el último valor continúa al alza, la estrategia abre una posición larga.
- Si los valores anteriores de la SMA muestran una pendiente descendente y el último valor continúa a la baja, la estrategia abre una posición corta.
- Las posiciones existentes se cierran cuando la tendencia se revierte.

## Parámetros
- **Period** – período de cálculo de la SMA.
- **BuyOpen** – habilitar entradas largas.
- **SellOpen** – habilitar entradas cortas.
- **BuyClose** – permitir cerrar posiciones largas.
- **SellClose** – permitir cerrar posiciones cortas.
- **StopLoss** – valor de protección contra pérdidas (pasado a `StartProtection`).
- **TakeProfit** – valor objetivo de ganancia (pasado a `StartProtection`).

## Notas
Esta es una conversión simplificada. Utiliza una SMA estándar en lugar del indicador DigVariation personalizado original.
