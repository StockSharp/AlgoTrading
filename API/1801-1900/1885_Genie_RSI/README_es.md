# Estrategia Genie RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones de sobrecompra y sobreventa usando el Índice de Fuerza Relativa (RSI). Cuando el RSI sube por encima de 80 la estrategia abre una posición corta; cuando el RSI cae por debajo de 20 abre una posición larga. Niveles opcionales de take profit y stop trailing gestionan el riesgo después de la entrada.

La estrategia está diseñada para mercados oscilantes donde el precio se mueve frecuentemente entre soporte y resistencia. Funciona en cualquier marco temporal, definido por el parámetro `CandleType`.

## Detalles

- **Criterios de entrada**  
  - **Largo**: El valor del RSI cruza por debajo de 20 en una vela finalizada y no hay posición abierta.  
  - **Corto**: El valor del RSI cruza por encima de 80 en una vela finalizada y no hay posición abierta.
- **Criterios de salida**  
  - **Largo**: El RSI sube por encima de 80, el precio alcanza la distancia de take profit, o el precio toca el nivel de stop trailing.  
  - **Corto**: El RSI cae por debajo de 20, el precio alcanza la distancia de take profit, o el precio toca el nivel de stop trailing.
- **Indicadores**: RSI.
- **Parámetros**:  
  - `RSI Period` – longitud del indicador RSI.  
  - `Take Profit` – distancia en unidades de precio para el objetivo de ganancia.  
  - `Trailing Stop` – distancia en unidades de precio para el stop trailing.  
  - `Candle Type` – marco temporal de las velas procesadas.
- **Gestión de posición**: Usa órdenes de mercado para entradas y salidas. El stop trailing se recalcula en cada vela finalizada.
