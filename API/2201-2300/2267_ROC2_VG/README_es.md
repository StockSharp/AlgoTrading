# Estrategia ROC2 VG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Recrea el experto **Exp_ROC2_VG** de MetaTrader en StockSharp.  
Se comparan dos líneas de tasa de cambio con períodos y tipos de cálculo configurables.  
Se abre una posición larga cuando la primera línea cruza por debajo de la segunda;  
se abre una posición corta en el cruce opuesto. La opción `Invert` intercambia las líneas.

## Detalles

- **Entrada larga**: anterior up > anterior down Y actual up <= actual down.
- **Entrada corta**: anterior up < anterior down Y actual up >= actual down.
- **Salida**: la señal de reversión invierte la posición inmediatamente con órdenes de mercado.
- **Marco temporal**: tipo de vela parametrizado, por defecto 4 horas.
- **Indicadores**: cada línea puede usar cálculos tipo Momentum o ROC:
  - Momentum = `precio - precio anterior`
  - ROC = `((precio / anterior) - 1) * 100`
  - ROCP = `(precio - anterior) / anterior`
  - ROCR = `precio / anterior`
  - ROCR100 = `(precio / anterior) * 100`
- **Parámetros predeterminados**:
  - `RocPeriod1` = 8, `RocType1` = Momentum
  - `RocPeriod2` = 14, `RocType2` = Momentum
  - `Invert` = false

La estrategia revierte el tamaño de posición cuando cambian las señales.
