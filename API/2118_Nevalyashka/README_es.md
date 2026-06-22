# Estrategia Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema simple de alternancia largo/corto con dimensionamiento de posición martingala.

## Lógica de la estrategia

1. Al inicio se abre una posición corta.
2. Se adjuntan un take profit y un stop loss fijos a la posición.
3. Cada vez que la posición se cierra (por stop o por objetivo):
   - La siguiente operación se abre en la dirección opuesta.
   - Si la operación anterior terminó con pérdida, el volumen de la orden se multiplica por `LotMultiplier`.
   - Si la operación anterior terminó con ganancia, el volumen se restablece al `Volume` base.
4. Los pasos 2‑3 se repiten indefinidamente.

## Parámetros

- `Volume` – volumen de orden base utilizado para la primera operación y después de operaciones ganadoras.
- `LotMultiplier` – multiplicador aplicado al volumen después de una operación perdedora.
- `TakeProfit` – distancia del objetivo de ganancia en puntos de precio.
- `StopLoss` – distancia del stop loss en puntos de precio.

## Notas

- Las órdenes de protección se gestionan a través de `StartProtection`.
- La estrategia no depende de datos de mercado; solo reacciona a cambios en la posición.
