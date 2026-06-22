# Estrategia de Trading Zonal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el Awesome Oscillator (AO) y el Accelerator Oscillator (AC) para capturar cambios en el impulso del mercado.

## Lógica
- Comprar cuando tanto AO como AC suben por encima de sus valores anteriores y al menos uno de ellos ha girado hacia arriba desde la barra anterior mientras ambos osciladores son positivos.
- Vender cuando tanto AO como AC caen por debajo de sus valores anteriores y al menos uno de ellos ha girado hacia abajo desde la barra anterior mientras ambos osciladores son negativos.
- Cerrar una posición larga cuando AO y AC giran hacia abajo.
- Cerrar una posición corta cuando AO y AC giran hacia arriba.

## Parámetros
- **Candle Type** – serie de velas fuente para los cálculos.
- **Take Profit** – valor fijo de take-profit en unidades de precio.

La estrategia opera una sola posición a la vez usando órdenes de mercado.
