# Estrategia de Referencia con Lanzamiento de Moneda Aleatorio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el clásico ejemplo de GuruTrader donde la dirección del trade se determina mediante un lanzamiento de moneda.
En cada vela terminada, si no hay ninguna posición abierta, se genera un número pseudoaleatorio y se trata como un lanzamiento de moneda.
Cara abre una posición larga mientras que cruz abre una corta.
Cada trade aplica distancias fijas de take-profit y stop-loss medidas en unidades de precio absoluto.

## Parámetros
- **Take Profit** – distancia desde el precio de entrada para colocar la orden de take-profit.
- **Stop Loss** – distancia desde el precio de entrada para colocar la orden de stop-loss.
- **Use Time Seed** – inicializa el generador aleatorio con la hora actual para obtener resultados diferentes en cada ejecución. Cuando está desactivado, se usa una semilla fija.
- **Candle Type** – tipo de velas procesadas por la estrategia.

## Lógica de Trading
1. Esperar una vela terminada.
2. Asegurarse de que la estrategia puede operar y no hay posición abierta.
3. Generar un valor aleatorio y elegir la dirección basándose en el lanzamiento de moneda.
4. Proteger la posición con las distancias predefinidas de stop-loss y take-profit.

**Advertencia:** Esta estrategia es solo para fines educativos y nunca debe usarse en cuentas reales.
