# Estrategia Exp Iin MA Signal MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un port de StockSharp del experto de MetaTrader "Exp_Iin_MA_Signal_MMRec". La estrategia escucha las señales de cruce producidas por un par de medias móviles configurables (el indicador original Iin_MA_Signal) y aplica un esquema de dimensionamiento de posición adaptativo con reducción basada en pérdidas.

## Descripción general

- **Generación de señales**: las medias móviles rápida y lenta se evalúan en el tipo de vela seleccionado y el precio aplicado. Se crea una señal de compra cuando la media rápida cruza por encima de la lenta, mientras que se produce una señal de venta en el cruce opuesto. El parámetro `SignalBar` pospone la ejecución por el número especificado de barras completamente cerradas, reproduciendo el retraso del búfer de indicador utilizado en la versión MQL.
- **Gestión de posición**: `BuyPosOpen` y `SellPosOpen` habilitan o deshabilitan las entradas largas y cortas. Cuando aparece una señal opuesta y el indicador `BuyPosClose` o `SellPosClose` correspondiente está habilitado, la estrategia cierra la exposición actual o revierte directamente a la nueva dirección.
- **Control de riesgo**: `StopLossPoints` y `TakeProfitPoints` se traducen a distancias de precio usando `Security.PriceStep` y se comprueban contra los extremos de la vela antes de procesar señales nuevas.
- **Gestión de dinero**: las últimas operaciones se rastrean por separado para largos y cortos. Cuando el número de operaciones perdedoras dentro de la ventana `BuyTotalTrigger`/`SellTotalTrigger` alcanza el umbral de pérdidas respectivo, la estrategia cambia de `NormalVolume` a `ReducedVolume`. El parámetro `MoneyMode` define cómo se interpreta el valor de volumen (lotes fijos, porcentaje de saldo, o porcentaje de riesgo basado en stop).

## Parámetros

- `FastPeriod`, `SlowPeriod` – longitudes de las medias móviles rápida y lenta.
- `FastType`, `SlowType` – tipos de medias móviles (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `VolumeWeighted`).
- `FastPrice`, `SlowPrice` – precio aplicado para cada media (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`).
- `SignalBar` – número de barras cerradas entre una señal detectada y el envío de la orden.
- `BuyPosOpen`, `SellPosOpen` – interruptores para abrir posiciones largas/cortas.
- `BuyPosClose`, `SellPosClose` – interruptores para cerrar o revertir una posición existente en la señal opuesta.
- `BuyTotalTrigger`, `SellTotalTrigger` – cuántas operaciones recientes se inspeccionan para el contador de pérdidas.
- `BuyLossTrigger`, `SellLossTrigger` – número mínimo de pérdidas dentro de la ventana inspeccionada que activa el volumen reducido.
- `NormalVolume`, `ReducedVolume` – volumen primario y de respaldo (o factor de riesgo, dependiendo de `MoneyMode`).
- `StopLossPoints`, `TakeProfitPoints` – distancias de stop loss y take profit en puntos del instrumento.
- `MoneyMode` – interpretación de los valores de volumen (`Lot`, `Balance`, `FreeMargin`, `BalanceRisk`, `FreeMarginRisk`). Los modos basados en saldo usan `Portfolio.CurrentValue`, mientras que los modos basados en riesgo dividen la cantidad de riesgo por la distancia calculada del stop.
- `CandleType` – serie de velas utilizada para los cálculos del indicador.

## Lógica de Señales

1. Cada vela terminada alimenta las medias móviles con el precio aplicado elegido.
2. La diferencia entre los valores actuales y anteriores de las medias móviles define un evento de cruce.
3. Las señales se ponen en cola, y la entrada más antigua se ejecuta una vez que el tamaño de la cola supera `SignalBar`.
4. Cuando se ejecuta una señal de compra:
   - Si existe una posición corta y `SellPosClose` está habilitado, la estrategia calcula el PnL realizado para ese trade corto. Luego revierte a un largo (si `BuyPosOpen` está habilitado) o simplemente cierra la exposición.
   - Si no hay posición abierta y `BuyPosOpen` está habilitado, se abre un nuevo largo con el volumen calculado.
5. Las señales de venta reflejan el flujo de trabajo de compra.

## Detalles de Gestión de Dinero

- El historial de operaciones se almacena como una cola FIFO limitada por `BuyTotalTrigger` / `SellTotalTrigger`.
- Una operación perdedora (PnL negativo) incrementa el contador de pérdidas. Cuando el contador alcanza `BuyLossTrigger` o `SellLossTrigger`, la siguiente posición usa `ReducedVolume`.
- `MoneyMode = Lot` trata los valores de volumen como cantidades brutas.
- `MoneyMode = Balance` y `FreeMargin` multiplican el valor configurado por `Portfolio.CurrentValue` y dividen por el precio de cierre actual para obtener la cantidad.
- `MoneyMode = BalanceRisk` y `FreeMarginRisk` multiplican el valor configurado por `Portfolio.CurrentValue` y dividen por la distancia del stop-loss. Si la distancia del stop es cero, el respaldo es idéntico al cálculo del porcentaje de saldo.
- Si la información del portafolio no está disponible, el volumen calculado toma el valor predeterminado de cero para evitar órdenes accidentales.

## Manejo de Riesgo

- Los niveles de stop-loss y take-profit se recalculan en cada vela usando el precio de entrada y el valor de punto. Si un nivel se toca dentro del rango de la vela, la posición se cierra antes de que se procesen nuevas señales.
- Las acciones de cierre siempre registran el resultado de la operación, asegurando que las colas de gestión de dinero permanezcan sincronizadas con los cierres reales.

## Notas

- Asegúrate de que `StopLossPoints` y `TakeProfitPoints` sean compatibles con el tamaño de tick del instrumento; la estrategia los multiplica por `Security.PriceStep`.
- Cuando `MoneyMode` depende de datos del portafolio, la estrategia espera que el objeto `Portfolio` exponga `CurrentValue`.
- El algoritmo opera sobre una base de posición neta: los holdings largos y cortos simultáneos no están soportados.
