# Estrategia Exp Fishing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en una posición cuando el cierre de la vela completada difiere de su apertura en al menos **Price Step**. Si la diferencia es positiva, compra; si es negativa, vende.

Después de abrir una posición, cada movimiento adicional de **Price Step** a favor de la operación dispara una orden de mercado adicional en la misma dirección hasta **Max Orders**. Stop-loss y take-profit de protección se aplican a cada posición usando distancias absolutas de precio.

## Parámetros

- **Price Step** – movimiento mínimo de precio (en unidades absolutas) requerido para abrir o agregar a una posición.  
- **Max Orders** – número máximo de órdenes de mercado permitidas en una dirección.  
- **Stop Loss** – distancia desde el precio de entrada donde se coloca un stop de protección.  
- **Take Profit** – distancia desde el precio de entrada donde se coloca el objetivo de ganancia.  
- **Candle Type** – marco temporal de velas usado para cálculos (por defecto 1 minuto).

## Lógica de Trading

1. Esperar una vela terminada.
2. Si no hay posición abierta:
   - Comprar si `Close - Open >= Price Step`.
   - Vender si `Open - Close >= Price Step`.
3. Cuando existe una posición:
   - Si el precio avanza `Price Step` desde la última entrada, agregar otra orden en la misma dirección.
   - Dejar de agregar órdenes una vez que el número alcanza **Max Orders**.
4. Stop-loss y take-profit se gestionan automáticamente para cada orden.

La estrategia está adaptada del experto MQL5 "Exp Fishing" y demuestra un enfoque simple de seguimiento de tendencia estilo rejilla.
