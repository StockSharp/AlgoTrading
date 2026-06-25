# Estrategia de Punto de Ruptura Diario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Punto de Ruptura Diario** es un puerto de StockSharp del asesor experto MetaTrader 5 «Daily BreakPoint» (compilación 19498). El algoritmo monitorea la distancia entre el precio actual y la apertura diaria. Cuando el movimiento desde la apertura diaria supera un umbral configurable y la vela anterior cumple estrictos requisitos de tamaño de cuerpo, la estrategia entra en la dirección del rompimiento o revierte la exposición existente dependiendo del indicador `CloseBySignal`.

La estrategia trabaja con dos flujos de datos al mismo tiempo:

1. Velas intradía definidas por el parámetro `CandleType` para la generación de señales.
2. Velas diarias usadas para rastrear el precio de apertura de la sesión más reciente.

## Lógica de trading
1. Cuando termina una nueva vela intradía, la estrategia lee el último precio de apertura diaria y calcula los niveles de rompimiento usando `BreakPointPips` (convertido en precios absolutos a través del tamaño del tick del instrumento).
2. El tamaño del cuerpo de la vela recientemente cerrada debe estar dentro del rango `[LastBarSizeMinPips, LastBarSizeMaxPips]`.
3. **Configuración alcista**
   - La vela debe cerrar por encima de su apertura (`Close > Open`).
   - El cierre debe ser al menos `BreakPointPips` por encima de la apertura diaria.
   - El precio de rompimiento (apertura diaria + punto de ruptura) debe estar dentro del cuerpo de la vela.
   - Si `CloseBySignal = false`, la estrategia abre una posición larga. De lo contrario, cierra cualquier exposición larga abierta y establece una posición corta.
4. **Configuración bajista** refleja el caso alcista: una vela bajista cuyo cierre está al menos `BreakPointPips` por debajo de la apertura diaria y cuyo cuerpo contiene el nivel de rompimiento activa una entrada corta (`CloseBySignal = false`) o una reversión a una posición larga (`CloseBySignal = true`).
5. Las órdenes se envían como órdenes de mercado usando el `OrderVolume` configurado. El tamaño de la posición es acumulativo, por lo que múltiples señales pueden escalar la posición en cualquier dirección.

## Gestión de riesgos
- **Stop Loss / Take Profit**: Objetivos fijos opcionales definidos en pips (`StopLossPips`, `TakeProfitPips`). Cuando se establece en cero, el nivel correspondiente está deshabilitado. La estrategia evalúa los máximos y mínimos de las velas para detectar hits.
- **Stop Trailing**: Habilitado cuando `TrailingStopPips > 0`. Una vez que la ganancia abierta supera `TrailingStopPips + TrailingStepPips`, el stop se arrastra detrás del precio por `TrailingStopPips`. El parámetro de paso previene ajustes frecuentes de stop en mercados planos.
- Todas las distancias de precio se convierten de pips usando el `PriceStep` del instrumento. Para cotizaciones de 3 o 5 decimales, el pip equivale a diez pasos de precio, replicando el comportamiento del asesor experto original.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `OrderVolume` | Volumen base usado para cada orden de mercado. |
| `CloseBySignal` | Si es `true`, la estrategia cierra posiciones existentes y abre la dirección opuesta cuando aparece una señal de rompimiento. |
| `BreakPointPips` | Distancia desde la apertura diaria requerida para confirmar un rompimiento. |
| `LastBarSizeMinPips` / `LastBarSizeMaxPips` | Tamaño mínimo y máximo del cuerpo de la vela disparadora. |
| `TrailingStopPips` | Distancia del stop trailing. Establecer en `0` para deshabilitar el trailing. |
| `TrailingStepPips` | Movimiento adicional requerido antes de cada ajuste de trailing. |
| `StopLossPips` | Stop loss fijo opcional. `0` lo deshabilita. |
| `TakeProfitPips` | Take profit fijo opcional. `0` lo deshabilita. |
| `CandleType` | Serie de velas intradía usada para la generación de señales. |

## Notas de uso
- La estrategia se suscribe automáticamente a las velas intradía y diarias. Asegúrese de que el proveedor de datos admita los marcos temporales solicitados.
- Dado que la lógica evalúa velas terminadas, las órdenes se envían al precio de cierre de la barra de señal.
- La conversión de pip asume precios estilo Forex. Revise los valores predeterminados cuando aplique la estrategia a instrumentos con tamaños de tick no convencionales.
