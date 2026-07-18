# HFT Spreader para FORTS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia replica el comportamiento de un spreader de alta frecuencia en el mercado FORTS. Monitorea continuamente el libro de órdenes y coloca órdenes limitadas en ambos lados del mercado para capturar el diferencial bid-ask.

## Lógica de la Estrategia
- Suscribirse a actualizaciones del libro de órdenes en tiempo real.
- Cuando no hay posición abierta y el diferencial es suficientemente amplio (determinado por `SpreadMultiplier`), la estrategia coloca:
  - Una orden limitada de compra un tick por encima del mejor bid.
  - Una orden limitada de venta un tick por debajo del mejor ask.
- Si existe una posición y no hay órdenes activas, coloca una única orden limitada en el lado opuesto para cerrar y revertir la posición.
- Las órdenes se cancelan y reemplazan cuando los mejores precios se mueven para mantenerlas en la cima del libro.

## Parámetros
- `SpreadMultiplier` – diferencial requerido en ticks para colocar ambas órdenes de compra y venta. El valor predeterminado es 4 ticks.
- `Volume` – volumen de la orden. El valor predeterminado es 1 lote.

## Notas de Uso
- Diseñada para instrumentos con tamaños de tick pequeños, como futuros en la bolsa FORTS.
- Utiliza únicamente órdenes limitadas; no se envían órdenes de mercado excepto por el mecanismo de protección si es necesario.
- Asegurar suficiente liquidez y un entorno de baja latencia para una operación efectiva.
