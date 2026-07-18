# Estrategia Color X Derivative
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia es un port de StockSharp del experto MetaTrader "Exp_ColorXDerivative". Funciona en un marco temporal de velas configurable (velas de 12 horas por defecto) y analiza el histograma de momentum ColorXDerivative. El indicador mide la velocidad de cambio de la fuente de precio elegida durante un desplazamiento fijo, suaviza el resultado con una media móvil y luego clasifica cada barra en uno de cinco estados de color. Las operaciones siguen la misma lógica que en el EA original: el robot compra cuando el momentum alcista se acelera o un movimiento bajista comienza a contraerse, y vende cuando la presión bajista aumenta o una pierna alcista pierde fuerza.

## Lógica del Indicador
1. Convertir cada vela al `AppliedPrice` seleccionado (cierre, apertura, cierre ponderado, Demark, etc.).
2. Calcular la derivada de precio: `(price[0] - price[shift]) * 100 / shift`, donde `shift = DerivativePeriod`.
3. Suavizar la derivada con el método seleccionado (`SMA`, `EMA`, `SMMA`, `LWMA` o `Jurik`). La media móvil Jurik predeterminada reproduce el suavizado JJMA de la implementación MQL.
4. Asignar un estado de color:
   - **0** – derivada &gt; 0 y creciente (fuerte aceleración alcista).
   - **1** – derivada &gt; 0 pero cayendo (momentum alcista perdiendo fuerza).
   - **2** – derivada ≈ 0 (neutral).
   - **3** – derivada &lt; 0 pero creciente (movimiento bajista contrayéndose).
   - **4** – derivada &lt; 0 y cayendo (aceleración bajista).

Un desplazamiento de señal controla qué barra finalizada se evalúa (1 = última barra cerrada, 2 = barra anterior, etc.).

## Reglas de Trading
- **Entrada en largo**: habilitada cuando `EnableLongEntry` es verdadero y:
  - el color actual es 0 mientras que el color anterior no era 0 (momentum gira fuertemente alcista), o
  - el color actual es 3 mientras que el color anterior era 4 o 2 (movimiento bajista comienza a contraerse).
- **Entrada en corto**: habilitada cuando `EnableShortEntry` es verdadero y:
  - el color actual es 4 mientras que el color anterior no era 4 (comienza la aceleración bajista), o
  - el color actual es 1 mientras que el color anterior era 0 o 2 (movimiento alcista se debilita).
- **Salida en largo**: activada cuando el color actual es 1 o 4 y `EnableLongExit` es verdadero.
- **Salida en corto**: activada cuando el color actual es 0 o 3 y `EnableShortExit` es verdadero.

Las órdenes se envían como órdenes de mercado usando el parámetro `OrderVolume`. Los cierres de posición se ejecutan antes de nuevas entradas para emular la lógica secuencial del EA original.

## Gestión de Riesgo
Las distancias opcionales de stop loss y take profit se proporcionan mediante `StopLossTicks` y `TakeProfitTicks`. Cuando cualquier valor supera cero, la estrategia llama a `StartProtection`, convirtiendo ticks en pasos de precio usando el tamaño `Step` del instrumento. La protección de stop/objetivo se ejecuta una vez y es compatible con auto-trading o backtesting.

## Parámetros
- `OrderVolume` – tamaño de la orden de mercado.
- `CandleType` – marco temporal para los cálculos del indicador (predeterminado marco temporal de 12 horas).
- `DerivativePeriod` – distancia en barras usada para el desplazamiento de la derivada.
- `AppliedPrice` – fuente de precio pasada a la derivada (cierre, mediana, ponderado, Demark, etc.).
- `SmoothingMethod` – filtro de suavizado aplicado a la derivada. Valores soportados: SMA, EMA, SMMA, LWMA, Jurik.
- `SmoothingLength` – período del filtro de suavizado.
- `SignalShift` – cuántas barras finalizadas atrás leer los valores de color (1 = barra cerrada más reciente).
- `StopLossTicks` / `TakeProfitTicks` – distancias protectoras opcionales en pasos del instrumento.
- `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – interruptores que coinciden con las entradas originales del EA.

## Notas
- La estrategia reproduce la lógica impulsada por el indicador del EA MetaTrader sin características adicionales de gestión de dinero.
- El suavizado Jurik es la aproximación más cercana al filtro JJMA usado en la biblioteca MQL; otras opciones se mapean a las medias móviles estándar de StockSharp.
- El historial de colores se almacena internamente para que la optimización en `SignalShift` funcione exactamente como en la versión MetaTrader.
