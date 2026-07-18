# Estrategia de Precio Extremo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Precio Extremo** replica el asesor experto de MetaTrader `Price_Extreme_Strategy` mediante la API de alto nivel de StockSharp. El sistema monitorea un canal deslizante derivado del máximo más alto y el mínimo más bajo durante un número configurable de velas completadas. Se generan señales de ruptura cuando la vela de referencia seleccionada cierra por encima del límite superior o por debajo del límite inferior. La lógica puede invertirse opcionalmente para transformar las condiciones de ruptura en entradas de contra-tendencia.

Esta conversión mantiene el flujo de trabajo de trading orientado a eventos. Las órdenes se envían inmediatamente después del cierre de cada vela finalizada, replicando el comportamiento del algoritmo MQL original que reaccionaba en el tick de apertura de la siguiente barra.

## Lógica del indicador

El canal de Precio Extremo se reconstruye en cada vela finalizada usando los indicadores `Highest` y `Lowest` de StockSharp:

- `Highest` rastrea el máximo de los altos durante las últimas *N* velas.
- `Lowest` rastrea el mínimo de los bajos durante las últimas *N* velas.

Estos búferes emulan el estudio personalizado `Price_Extreme_Indicator` incluido con el asesor experto original. La longitud del indicador se expone a través del parámetro **Level Length**.

Un parámetro separado **Signal Shift** define qué vela cerrada se usa para evaluar la condición de ruptura. Un shift de `1` significa "usar la vela que acaba de cerrar" (por defecto). Valores mayores permiten esperar confirmación adicional haciendo referencia a barras más antiguas.

## Reglas de trading

1. Recalcular los valores del canal superior e inferior para cada vela finalizada.
2. Recuperar la vela especificada por **Signal Shift** del búfer de historial interno.
3. Generar intenciones direccionales:
   - **Ruptura alcista**: el cierre de la vela está por encima del valor del canal superior.
   - **Ruptura bajista**: el cierre de la vela está por debajo del valor del canal inferior.
4. Aplicar inversión opcional con **Reverse Signals**:
   - Si está desactivado, operar en la dirección de la ruptura (comprar en ruptura alcista, vender en ruptura bajista).
   - Si está activado, intercambiar las reacciones (vender en ruptura alcista, comprar en ruptura bajista).
5. Respetar los permisos **Enable Long** y **Enable Short** antes de enviar órdenes.
6. Cerrar automáticamente cualquier posición opuesta antes de abrir una nueva operación para que solo exista una posición neta en todo momento.

## Gestión de riesgos

La estrategia proporciona manejo de stop-loss y take-profit que replica los controles basados en puntos de la versión MQL:

- **Stop Loss** y **Take Profit** se expresan en pasos de precio (`Security.PriceStep`).
- Los precios objetivo se recalculan cuando cambia el tamaño de la posición neta.
- Si una vela finalizada supera los niveles de protección (mínimo por debajo del stop para largos, máximo por encima del stop para cortos, etc.), la posición se cierra mediante orden de mercado y los objetivos de protección se borran.
- `StartProtection()` se activa durante `OnStarted` para aprovechar las salvaguardas integradas de StockSharp.

## Parámetros

| Parámetro | Descripción | Predeterminado | Grupo |
|-----------|-------------|----------------|-------|
| `LevelLength` | Número de velas completadas consideradas al calcular el canal extremo. | 5 | Indicator |
| `SignalShift` | Índice de la vela cerrada usada para la validación de ruptura (1 = última vela cerrada). | 1 | Indicator |
| `EnableLong` | Permite comprar cuando es `true`. | `true` | Trading |
| `EnableShort` | Permite vender cuando es `true`. | `true` | Trading |
| `ReverseSignals` | Invierte las reacciones de ruptura (comprar en bajada, vender en subida). | `false` | Trading |
| `OrderVolume` | Volumen enviado con cada orden de mercado. Debe ser mayor que cero. | 1 | Trading |
| `StopLossPoints` | Distancia del stop-loss medida en pasos de precio. Un valor de `0` desactiva el stop. | 0 | Risk |
| `TakeProfitPoints` | Distancia del take-profit medida en pasos de precio. Un valor de `0` desactiva el objetivo. | 0 | Risk |
| `CandleType` | Marco temporal principal para la suscripción de datos. | Velas de 5 minutos | Data |

Todos los parámetros usan `StrategyParam<T>` con metadatos de UI para que puedan optimizarse o modificarse desde el Designer.

## Guía de uso

1. Adjuntar la estrategia a un instrumento y establecer el **Candle Type** para que coincida con el marco temporal usado en la configuración original de MetaTrader.
2. Ajustar **Level Length** si se desea un canal de Precio Extremo más amplio o más estrecho.
3. Configurar **Signal Shift** para controlar cuántas velas cerradas esperar antes de evaluar la ruptura.
4. Seleccionar las direcciones de operación deseadas mediante **Enable Long**, **Enable Short** y **Reverse Signals**.
5. Definir **Order Volume**, **Stop Loss** y **Take Profit** según las preferencias de riesgo. Recuerde que ambos valores de protección operan en pasos de precio.
6. Iniciar la estrategia. Las velas, las bandas del indicador y las operaciones ejecutadas se grafican automáticamente cuando hay un área de gráfico disponible.

## Notas adicionales

- La estrategia opera intencionalmente sobre una sola posición neta, replicando la lógica de cobertura del experto MQL al aplanar el lado opuesto antes de entrar en una nueva operación.
- Los stops y objetivos de protección se evalúan en velas completadas. En trading en vivo, esto aproxima las órdenes de protección del lado del servidor usadas por el script original.
- No se incluye versión en Python, según lo solicitado.
