# Laptrend_1 Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Laptrend_1 reproduce la lógica del asesor experto MetaTrader **Laptrend_1.mq4**. La estrategia combina un filtro de canal LabTrend de múltiples marcos temporales, confirmación de impulso de Fisher Transform y una verificación de fuerza de tendencia ADX en velas de 15 minutos. Las órdenes se abren solo cuando las direcciones de LabTrend del marco temporal superior (H1) y del marco temporal de señal (M15) coinciden, la transformada de Fisher confirma el movimiento y el ADX muestra una tendencia de fortalecimiento. Las posiciones se cierran cuando el impulso se revierte, la dirección de LabTrend cambia o el mercado pasa a un régimen plano donde ADX y los componentes DI convergen.

## Lógica de trading
- **Datos primarios**: las velas de 15 minutos impulsan entradas/salidas, mientras que las velas de 1 hora alimentan el filtro LabTrend a largo plazo.
- **Canal LabTrend**: el código recrea el indicador `LabTrend1_v2.1` creando canales estilo Donchian sobre las últimas barras `ChannelLength` y estrechándolos con `RiskFactor`. Un cierre por encima de la banda superior marca una tendencia alcista; un cierre por debajo de la banda inferior marca una tendencia bajista. Las tendencias M15 y H1 deben alinearse con las operaciones abiertas.
- **Transformación de Fisher**: una transformación de Fisher personalizada (`Fisher_Yur4ik`) rastrea el impulso en el período de tiempo M15. Los cruces por cero invierten el sesgo alcista/bajista, mientras que atravesar ±0,25 produce señales de salida.
- Filtro **ADX**: el índice direccional promedio de 15 minutos debe aumentar y el componente DI dominante debe estar de acuerdo con la operación propuesta. Cuando ADX, +DI y –DI caen dentro de `Delta` puntos entre sí, la estrategia trata el mercado como si estuviera plano, restablece las banderas de impulso y liquida las posiciones abiertas.
- **Gestión de posiciones**: las nuevas posiciones cierran cualquier exposición opuesta y negocian un volumen configurable. Las salidas se desencadenan por reversiones de LabTrend, salidas de Fisher o una condición de mercado plana.

## Gestión del riesgo
- **Stop Loss / Take Profit** – Configurable en puntos del instrumento (MetaTrader “pips”). Se evalúan contra los máximos y mínimos de las velas para imitar las órdenes de protección del EA original.
- **Trailing Stop**: una vez que el precio se mueve a favor de la operación, un trailing stop rastrea el cierre a una distancia igual a `TrailingStopPoints`. Cruzar el nivel final desencadena una salida inmediata del mercado.
- **Volumen**: todos los pedidos utilizan el parámetro fijo `Volume` (lotes).

## Parámetros
- `Volume` – Tamaño del pedido en lotes. Predeterminado 1.
- `AdxPeriod` – ADX período de suavizado. Predeterminado 14.
- `FisherLength` – Ventana para la transformación de Fisher. Predeterminado 10.
- `ChannelLength` – Barras utilizadas para el canal LabTrend. Predeterminado 9.
- `RiskFactor` – Factor de estrechamiento del canal de LabTrend (rango del indicador original 1..10). Predeterminado 3.
- `Delta`: diferencia máxima entre los valores ADX y DI antes de que el mercado se etiquete como plano. Predeterminado 7.
- `StopLossPoints` – Distancia de parada de pérdidas en puntos. Por defecto 100.
- `TakeProfitPoints` – Distancia de obtención de beneficios en puntos. Por defecto 40.
- `TrailingStopPoints`: distancia del trailing stop en puntos. Por defecto 100.
- `SignalCandleType` – Serie de velas para cálculos de señales (predeterminado M15).
- `TrendCandleType`: serie de velas para el filtro LabTrend de período de tiempo más alto (H1 predeterminado).

## Notas
- La implementación original MQL funcionó en cada tick entrante; este puerto procesa velas M15 completadas, lo que mantiene la lógica determinista y al mismo tiempo respeta los cálculos del indicador.
- Stop Loss, Take Profit y Trailing Outs se ejecutan con órdenes de mercado cuando el máximo/mínimo de la vela supera los umbrales configurados. Esto refleja el comportamiento de MetaTrader órdenes de protección sin mantener órdenes de límite/detención explícitas.
- Asegúrese de que la fuente de datos proporcione las series de velas de 15 minutos y 1 hora definidas en los parámetros antes de iniciar la estrategia.
