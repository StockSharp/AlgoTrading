# Estrategia 2pb Ideal MA ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Implementa el experto MQL "Exp_2pbIdealMA_ReOpen" usando la API de alto nivel de StockSharp.
- Opera un cruce contrario entre una media móvil ideal simple y una media móvil ideal de triple etapa.
- Añade a posiciones ganadoras cuando el precio avanza un número configurable de ticks y opcionalmente cierra posiciones en señales opuestas.

## Indicadores
- **2pb Ideal 1 MA** – media móvil ideal simple con dos períodos de ponderación. Reacciona rápidamente y define el sesgo a corto plazo.
- **2pb Ideal 3 MA** – triple cascada del mismo filtro ideal (etapas X, Y, Z). Reacciona más lentamente y representa la tendencia de fondo.

## Lógica de negociación
1. Suscribirse a la serie de velas seleccionada (por defecto H4) y evaluar señales solo en velas cerradas.
2. Almacenar los valores del filtro `SignalBarShift` barras atrás (por defecto 1). Usar el par de valores en los desplazamientos `SignalBarShift` y `SignalBarShift + 1` para detectar cruces.
3. **Entrada larga** – cuando el filtro rápido estaba por encima del filtro lento hace dos barras y cayó por debajo hace una barra (cruce bajista), abrir una posición larga si las entradas largas están habilitadas y no hay posición abierta.
4. **Entrada corta** – cuando el filtro rápido estaba por debajo del filtro lento hace dos barras y subió por encima hace una barra (cruce alcista), abrir una posición corta si las entradas cortas están habilitadas y no hay posición abierta.
5. **Reentradas** – mientras una posición es rentable, añadir una orden más de `PositionVolume` una vez que el precio se mueva `PriceStepTicks * Security.PriceStep` en la dirección de la operación. El número de adiciones por dirección está limitado por `MaxReEntries`.
6. **Salidas** – si aparece el cruce opuesto y el indicador de salida respectivo está habilitado, cerrar la posición abierta antes de considerar nuevas entradas.
7. Aplicar stop loss y take profit opcionales usando las distancias de ticks configuradas.

## Parámetros
- `CandleType` – marco temporal de la serie de velas de trabajo.
- `PositionVolume` – volumen base para entradas y reentradas (también asignado a `Strategy.Volume`).
- `StopLossTicks` / `TakeProfitTicks` – distancias de protección expresadas en ticks; convertidas a precio usando `Security.PriceStep`.
- `PriceStepTicks` – número de ticks requeridos entre órdenes de reentrada sucesivas.
- `MaxReEntries` – número máximo de operaciones adicionales por dirección.
- `EnableBuyEntries` / `EnableSellEntries` – permitir la apertura de posiciones largas o cortas.
- `EnableBuyExits` / `EnableSellExits` – cerrar posiciones existentes cuando aparece la señal opuesta.
- `SignalBarShift` – número de barras atrás usadas para evaluar el cruce (imita el original `SignalBar`).
- `Period1`, `Period2` – ponderaciones para la media móvil ideal simple.
- `PeriodX1`, `PeriodX2`, `PeriodY1`, `PeriodY2`, `PeriodZ1`, `PeriodZ2` – ponderaciones para cada etapa de la media móvil ideal triple.

## Gestión del riesgo
- Las protecciones de stop loss y take profit se activan a través de `StartProtection` si las distancias de ticks correspondientes son mayores que cero.
- La estrategia no abre nuevas operaciones mientras un posición opuesta todavía está abierta, reflejando el comportamiento MQL.

## Notas
- Funciona con cualquier instrumento que proporcione `Security.PriceStep`; la configuración predeterminada apunta a velas H4.
- No se proporciona versión en Python, acorde con la solicitud original.
