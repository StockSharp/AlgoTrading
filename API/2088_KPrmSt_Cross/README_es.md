# Estrategia de Cruce KPrmSt
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia KPrmSt Cross es un port del experto de MetaTrader 5 `exp_kprmst.mq5`. Utiliza un oscilador similar al Stochastic conocido como KPrmSt para capturar reversiones cuando la línea principal del oscilador cruza la línea de señal.

La estrategia se suscribe a velas de un marco temporal configurable y calcula el indicador `Stochastic` (utilizado como aproximación de KPrmSt). Cuando la línea %K cruza por debajo de la línea %D, abre una posición larga; cuando %K cruza por encima de %D, abre una posición corta. Las posiciones existentes se revierten en consecuencia.

## Parámetros
- `Candle Type` – marco temporal de las velas usadas para los cálculos.
- `K Period` – número de barras para calcular la línea principal.
- `D Period` – período para suavizar la línea de señal.
- `Slowing` – suavizado adicional aplicado a %K.
- `Stop Loss` – pérdida protectora en unidades de precio. Establecer en 0 para deshabilitar.
- `Take Profit` – beneficio objetivo en unidades de precio. Establecer en 0 para deshabilitar.

## Lógica de trading
1. La estrategia solo escucha velas finalizadas.
2. Los valores del oscilador Stochastic se almacenan para detectar cruces.
3. Cuando %K cae por debajo de %D después de haber estado por encima, se abre una posición larga o se cierra la corta.
4. Cuando %K sube por encima de %D después de haber estado por debajo, se abre una posición corta o se cierra la larga.
5. Los niveles opcionales de stop loss y take profit cierran la posición cuando se alcanzan.

## Notas
- El indicador KPrmSt del experto original se aproxima mediante el indicador `Stochastic` de StockSharp.
- Las opciones de gestión de dinero del script original no están implementadas.
- La estrategia requiere un feed de datos de mercado y enrutamiento de órdenes compatible con StockSharp.
