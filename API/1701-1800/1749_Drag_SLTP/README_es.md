# Estrategia de Gestión Drag SL/TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca automáticamente órdenes de stop-loss y take-profit a una distancia fija del precio de la operación ejecutada. Es útil cuando las posiciones manuales deben protegerse inmediatamente después de la entrada.

## Parámetros

- **Auto Set SL** (`bool`): habilitar la colocación automática de stop-loss.
- **SL Points** (`decimal`): distancia del stop-loss en pasos de precio.
- **Auto Set TP** (`bool`): habilitar la colocación automática de take-profit.
- **TP Points** (`decimal`): distancia del take-profit en pasos de precio.

## Comportamiento

Cuando la estrategia se inicia, llama a `StartProtection` con las distancias seleccionadas. Cualquier posición abierta mientras la estrategia está en ejecución recibirá inmediatamente las órdenes de protección correspondientes. Las distancias se miden en pasos de precio (`Security.PriceStep`).

La estrategia en sí no genera señales de trading; simplemente gestiona órdenes de protección para posiciones abiertas manualmente o por otras estrategias.

## Notas

- Diseñada para uso con la API de alto nivel.
- Solo el estado de vela finalizada debe activar acciones de trading en versiones extendidas.
- No se implementa la función de arrastre gráfico del script MQL original.
