# Estrategia JS MA Day
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia JS MA Day** opera basándose en una media móvil simple calculada sobre velas diarias usando el precio mediano. La estrategia compara la posición de la media móvil relativa al precio de apertura de cada día y abre posiciones cuando la tendencia de la media móvil confirma un cruce del precio de apertura.

## Indicadores

- Media Móvil Simple (precio mediano)

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `MaPeriod` | Período de la media móvil simple. | `3` |
| `Reverse` | Invierte las señales de negociación. Cuando está habilitado, las señales de compra se convierten en señales de venta y viceversa. | `false` |
| `CandleType` | Tipo de vela usado para los cálculos. Por defecto son velas de marco temporal diario. | `TimeFrame(1 day)` |

## Reglas de entrada

1. Evalúa la media móvil simple (SMA) diaria y los precios de apertura diarios.
2. **Comprar** cuando:
   - La SMA actual está por debajo de la SMA anterior.
   - La SMA actual está por encima del precio de apertura de hoy.
   - La SMA anterior está por debajo de la SMA de hace dos días.
   - La SMA anterior está por encima del precio de apertura del día anterior.
3. **Vender** cuando:
   - La SMA actual está por encima de la SMA anterior.
   - La SMA actual está por debajo del precio de apertura de hoy.
   - La SMA anterior está por encima de la SMA de hace dos días.
   - La SMA anterior está por debajo del precio de apertura del día anterior.
4. Si `Reverse` está habilitado, las condiciones de compra y venta se invierten.

## Reglas de salida

- Las posiciones se cierran llamando a `StartProtection`, que permite configurar órdenes de protección como stop loss o take profit a través de la configuración de la plataforma.

## Notas

- La estrategia procesa solo velas completadas.
- El volumen de las órdenes está definido por la propiedad `Volume` de la clase base.
- Todavía no existe una versión de esta estrategia en Python.
