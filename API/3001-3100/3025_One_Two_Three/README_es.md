# Estrategia One Two Three
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia One Two Three negocia rupturas del oscilador Chaikin después de un período extendido de acumulación plana. Emula el experto original de MetaTrader 5 combinando una línea de acumulación/distribución con dos EMA, validando que la presión del mercado ha permanecido neutral durante varias barras, y luego entrando en una fuerte oleada de impulso Chaikin. El port de StockSharp mantiene el dimensionamiento de lotes, la gestión de stops y la lógica de trailing configurables a través de parámetros de estrategia.

## Concepto

- Construir el oscilador Chaikin como la diferencia entre una media móvil exponencial rápida y una lenta aplicada a la línea de acumulación/distribución derivada de las velas entrantes.
- Rastrear las últimas **BarsCount** lecturas del oscilador y clasificar las barras donde el valor absoluto de Chaikin se mantiene dentro de **FlatLevel**.
- Permitir el trading solo cuando más de **FlatPercent** por ciento de esas lecturas almacenadas se mantuvieron dentro del rango plano, señalando una acumulación tranquila.
- Cuando se termina una nueva vela, entrar en la dirección del impulso Chaikin si su magnitud supera **OpenLevel**.

## Reglas de entrada

- **Largo**: El oscilador Chaikin en la vela recién cerrada es mayor o igual a **OpenLevel** y la posición neta actual es no positiva.
- **Corto**: El oscilador Chaikin en la vela recién cerrada es menor o igual al **OpenLevel** negativo y la posición neta actual es no negativa.
- Las órdenes se emiten al mercado. Si la estrategia mantiene una posición opuesta, el tamaño de la orden se incrementa para aplanar la exposición existente antes de establecer la nueva operación.

## Reglas de salida

- Un stop-loss fijo (**StopLossPips**) y take-profit (**TakeProfitPips**) se traducen en desplazamientos de precio usando el paso de precio del instrumento (1 pip = 1 paso de precio) y se aplican inmediatamente después de la entrada.
- Un trailing stop opcional ajusta el stop protector una vez que el precio se mueve a favor de la operación al menos **TrailingStopPips + TrailingStepPips**. El nuevo stop se coloca exactamente **TrailingStopPips** alejado del cierre actual mientras se requiere el buffer de paso para evitar el ajuste prematuro.
- Si el stop o el objetivo son tocados dentro del rango de la vela completada, la posición se cierra al mercado.

## Gestión de riesgo y dinero

- **OrderVolume** controla la cantidad enviada con cada orden de mercado. La estrategia suma o resta automáticamente el tamaño de posición actual al cambiar de dirección para que las reversiones ocurran en una sola operación.
- Establecer cualquiera de los parámetros basados en pips a cero deshabilita ese componente (por ejemplo, un take-profit de cero mantiene las operaciones abiertas hasta que el stop o la señal opuesta ocurra).

## Parámetros

- **OrderVolume** – Volumen base para entradas.
- **StopLossPips** – Distancia, en pips, entre el precio de entrada y el stop protector.
- **TakeProfitPips** – Distancia, en pips, entre el precio de entrada y el objetivo de ganancia.
- **TrailingStopPips** – Distancia, en pips, mantenida entre el precio y el trailing stop. Establecer a cero para deshabilitar el trailing.
- **TrailingStepPips** – Ganancia mínima en pips más allá de la distancia de trailing requerida antes de que el stop se mueva nuevamente.
- **FastLength** – Período de la EMA rápida en el oscilador Chaikin.
- **SlowLength** – Período de la EMA lenta en el oscilador Chaikin.
- **FlatLevel** – Valor absoluto de Chaikin que aún cuenta como comportamiento de mercado plano.
- **OpenLevel** – Magnitud de Chaikin requerida para activar una nueva operación una vez que se satisface la condición plana.
- **BarsCount** – Número de valores recientes de Chaikin a evaluar al calcular la ratio plana.
- **FlatPercent** – Porcentaje mínimo de los valores almacenados que deben mantenerse dentro del rango plano para permitir el trading.
- **CandleType** – Tipo de datos de vela o marco temporal que alimenta los cálculos del indicador.

## Notas

- La lógica de trailing refleja el experto de MetaTrader: si **TrailingStopPips** es distinto de cero, mantenga **TrailingStepPips** positivo para evitar un stop estancado.
- Debido a que las estrategias de StockSharp trabajan con el paso de precio del instrumento, las distancias basadas en pips asumen que un pip equivale a un paso de precio; ajuste los valores del parámetro en consecuencia para instrumentos con diferentes tamaños de tick.
- La estrategia procesa únicamente velas completadas y no intenta reaccionar dentro de la barra, coincidiendo con el experto original que ejecuta en nuevas aperturas de barra.
