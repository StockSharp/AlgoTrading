# Estrategia CHO With Flat
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el cruce del **Chaikin Oscillator** y su media móvil. Se utiliza un filtro de Bandas de Bollinger para evitar operar durante mercados planos.

## Parámetros
- **Candle Type** – marco temporal de las velas de entrada.
- **Fast Period** – período rápido del Chaikin Oscillator.
- **Slow Period** – período lento del Chaikin Oscillator.
- **MA Period** – período de la media móvil aplicada al oscilador.
- **MA Type** – tipo de media móvil para la línea de señal.
- **Bollinger Period** – período de las Bandas de Bollinger.
- **Std Deviation** – desviación estándar para las Bandas de Bollinger.
- **Flat Threshold** – ancho mínimo de banda (en puntos) para considerar el mercado activo.

## Lógica de trading
1. Calcular el Chaikin Oscillator y su media móvil.
2. Construir Bandas de Bollinger sobre el precio para detectar mercado plano.
3. Omitir operaciones si el ancho de la banda de Bollinger está por debajo de `Flat Threshold`.
4. **Comprar** cuando el oscilador cruza por debajo de su línea de señal.
5. **Vender** cuando el oscilador cruza por encima de su línea de señal.

La dirección de la posición siempre sigue el último cruce mientras el filtro plano evita operar en condiciones de mercado lateral.
