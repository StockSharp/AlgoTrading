# Estrategia Vlt Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta períodos de volatilidad muy baja y prepara órdenes de ruptura. Cuando el rango de la vela actual se convierte en el más pequeño del período de lookback especificado, la estrategia coloca órdenes stop de compra y venta alrededor de la vela anterior.

## Parámetros
- **Period** – período de lookback para el cálculo del rango mínimo.
- **Pending level** – distancia en ticks desde el máximo/mínimo de la vela anterior para colocar las órdenes stop.
- **Stop loss** – stop de protección en ticks.
- **Take profit** – objetivo de beneficio en ticks.
- **Candle type** – marco temporal utilizado para el análisis.

## Lógica
1. Para cada vela finalizada, calcular su rango (`High - Low`).
2. Rastrear el rango más pequeño durante las últimas *Period* velas.
3. Cuando el rango actual establece un nuevo mínimo, cancelar las órdenes existentes y colocar órdenes stop por encima y por debajo de la vela anterior con el desplazamiento indicado.
4. `StartProtection` gestiona el stop-loss y el take-profit una vez que se abre una posición.
