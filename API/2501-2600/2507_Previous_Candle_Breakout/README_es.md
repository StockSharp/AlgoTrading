# Estrategia de Ruptura de la Vela Anterior
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto clásico "BreakOut" de MetaTrader de Soubra2003. Monitorea el máximo y el mínimo de la
vela completada más reciente y reacciona cuando el cierre actual rompe esos niveles de referencia. El enfoque es totalmente
simétrico: las posiciones largas se abren en rupturas alcistas, y las posiciones cortas se abren en roturas bajistas. Buffers
opcionales de stop-loss y take-profit expresados en unidades de precio permiten al usuario limitar el riesgo o bloquear ganancias.

## Descripción General

- Se suscribe a una única serie de velas (marco temporal de 1 hora por defecto).
- Almacena el máximo y el mínimo de la vela anterior para actuar como disparadores de ruptura.
- Opera solo al cierre de la vela para reflejar la lógica basada en ticks del original sin depender de datos dentro de la barra.
- Soporta tanto operaciones largas como cortas y siempre permanece plano cuando no hay condición de ruptura activa.

## Reglas de Trading

1. **Entrada por ruptura / reversión**
   - Cuando el cierre de la vela actual terminada es estrictamente superior al máximo de la vela anterior:
     - Cualquier posición corta abierta se cierra a mercado.
     - Se abre inmediatamente una nueva posición larga (la reversión ocurre dentro del mismo paso de procesamiento de la vela).
   - Cuando el cierre es estrictamente inferior al mínimo de la vela anterior:
     - Cualquier posición larga abierta se cierra a mercado.
     - Se abre posteriormente una nueva posición corta.
2. **Salidas de protección (opcional)**
   - Si se configura un offset de stop-loss (> 0), la estrategia sale de un largo cuando el cierre cae `offset` unidades por
     debajo del precio de entrada, o sale de un corto cuando el cierre sube `offset` unidades por encima del precio de entrada.
   - Si se configura un offset de take-profit (> 0), la estrategia sale de un largo cuando el cierre sube `offset` unidades por
     encima del precio de entrada, o sale de un corto cuando el cierre cae `offset` unidades por debajo del precio de entrada.
3. **Reinicio del estado**
   - Después de que cada vela es procesada, el máximo y mínimo más recientes se convierten en los nuevos niveles de referencia de ruptura.

## Parámetros

- **Candle Type** – tipo de datos usado para la suscripción (por defecto marco temporal horario). Establézcalo al tamaño de barra
  que coincide con el gráfico usado en MetaTrader para el asesor experto original.
- **Stop Loss** – distancia en unidades de precio absolutas entre el precio de entrada y el stop de protección. Mantener en `0`
  para deshabilitar el manejo del stop-loss.
- **Take Profit** – distancia en unidades de precio absolutas entre el precio de entrada y el objetivo de beneficio. Mantener en
  `0` para deshabilitar el manejo del take-profit.

## Notas

- Los cálculos de stop-loss y take-profit se realizan en precios de cierre de velas. La versión MQL4 original adjuntaba niveles
  de SL/TP estáticos a las órdenes; en StockSharp las salidas se simulan enviando órdenes a mercado una vez que se cumplen los
  umbrales.
- Use incrementos de precio específicos del instrumento al configurar los offsets. Por ejemplo, si el instrumento opera con un
  tamaño de tick de 0.01 y desea un stop de 20 ticks, configure el parámetro de stop-loss en `0.20`.
- Debido a que la lógica siempre referencia la vela inmediatamente anterior, la estrategia funciona mejor en instrumentos en
  tendencia o durante sesiones de alta volatilidad donde las rupturas son significativas.

## Origen

- **Fuente**: `MQL/17306/BreakOut.mq4` (asesor experto BreakOut de Soubra2003)
- **Autor**: https://www.mql5.com/en/users/soubra2003
