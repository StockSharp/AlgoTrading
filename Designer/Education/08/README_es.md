# Esquema de Estrategia Avanzada con Múltiples Marcos Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este archivo ilustra un esquema de estrategia complejo que utiliza velas de diferentes marcos temporales, diseñado específicamente para la plataforma Designer de StockSharp. Este ejemplo emplea condiciones variadas en múltiples ramas para ejecutar operaciones basadas en datos históricos de precios.

## Detalles de la Estrategia

El esquema se divide en dos ramas principales, cada una utilizando velas de cinco minutos comparadas con los extremos históricos de precios para tomar decisiones de trading:

### Primera Rama — Extremos Históricos
- **Condición de Compra**: La estrategia inicia una orden de compra si el precio de cierre de una vela de cinco minutos es mayor que el precio más alto de los últimos 20 días.
- **Condición de Venta**: Se ejecuta una orden de venta si el precio de cierre de una vela de cinco minutos es menor que el precio más bajo de los últimos 10 días.

### Segunda Rama — Condiciones Inversas
- **Condición de Venta**: Ejecuta una orden de venta si el precio de cierre de una vela de cinco minutos es menor que el precio más bajo de los últimos 20 días.
- **Condición de Compra**: Inicia una compra si el precio de cierre de una vela de cinco minutos es mayor que el precio más alto de los últimos 10 días.

## Características y Cambios Específicos de Versión
- **Apariencia del Bloque Bandera**: En la versión 5 de Designer, se ha actualizado la apariencia del bloque bandera.
- **Adaptaciones de la Estrategia**: También en la versión 5, la estrategia ha sido modificada para incluir dos bloques tanto para señales de venta como de compra. Este ajuste se debe a un cambio en la forma en que las señales activan las acciones en la versión más reciente de Designer.

Este esquema proporciona un marco para implementar y probar estrategias que reaccionan ante movimientos de precios significativos, comparando las acciones de precio a corto plazo con los registros de precios a largo plazo. El enfoque de múltiples ramas permite a los traders experimentar con diferentes respuestas estratégicas basadas en los mismos datos subyacentes.
