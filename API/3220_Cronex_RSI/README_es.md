# Estrategia de Cronex RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Cronex RSI** recrea el asesor experto Exp_CronexRSI.mq5 en la API de alto nivel de StockSharp. La pila de indicadores combina un Índice de Fuerza Relativa (RSI) clásico con dos medias móviles secuenciales para reducir el ruido. Las decisiones de trading se basan en cruces entre las curvas RSI suavizadas rápida y lenta, con permisos de entrada/salida configurables que coinciden con los parámetros MQL5 originales.

## Lógica de trading

1. Construir el RSI desde el precio aplicado y el período de retroceso seleccionados.
2. Suavizar el valor RSI con una media móvil *rápida*, luego suavizar el resultado con una media móvil *lenta*.
3. Evaluar cruces con un desplazamiento de confirmación configurable:
   - Cuando la curva rápida estaba por encima de la curva lenta una barra antes y cae por debajo en la barra confirmada, la estrategia cierra posiciones cortas y, si está habilitado, abre una posición larga.
   - Cuando la curva rápida estaba por debajo de la curva lenta y cruza por encima en la barra confirmada, la estrategia cierra posiciones largas y puede entrar en operaciones cortas.
4. Los volúmenes son simétricos en ambas direcciones. Cuando una nueva señal revierte la posición, la estrategia primero cubre la exposición existente y luego abre el nuevo lado usando el volumen base configurado.

Por defecto la estrategia espera una vela completamente cerrada antes de actuar sobre una señal, reproduciendo el comportamiento `SignalBar = 1` de Exp_CronexRSI. Establecer el desplazamiento en cero procesa el cruce inmediatamente en la barra de cierre.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `RsiPeriod` | Período de retroceso RSI. |
| `FastPeriod` | Longitud de la media móvil de suavizado rápido. |
| `SlowPeriod` | Longitud de la segunda media móvil de suavizado. |
| `SignalShift` | Número de barras completadas utilizadas para confirmación (0 reacciona instantáneamente). |
| `SmoothingMethod` | Tipo de media móvil aplicado durante ambas etapas de suavizado (simple, exponencial, suavizada, ponderada linealmente, ponderada por volumen). |
| `AppliedPrice` | Componente de precio pasado al RSI (cierre, apertura, máximo, mínimo, mediana, típico, ponderado). |
| `CandleType` | Serie de velas procesada por la estrategia. |
| `TradeVolume` | Tamaño de orden base utilizado para nuevas entradas. |
| `EnableLongEntry` / `EnableShortEntry` | Permitir abrir posiciones largas/cortas. |
| `EnableLongExit` / `EnableShortExit` | Permitir cerrar posiciones en respuesta a señales opuestas. |

## Notas de implementación

- El método de suavizado utiliza clases de media móvil de StockSharp. La opción `VolumeWeighted` también cubre los estilos VIDYA/AMA de MQL5 aplicando un sustituto pragmático ponderado por volumen.
- La selección de precio aplicado coincide con las entradas del indicador Cronex y refleja el asistente utilizado dentro del asesor experto original.
- Todos los valores de indicadores se procesan a través de instancias `DecimalIndicatorValue` para permanecer compatibles con la tubería de indicadores de StockSharp mientras se evita el sondeo directo de valores.
- La estrategia redimensiona automáticamente su historial interno cuando cambia el desplazamiento de confirmación, asegurando que la lógica de cruce mantenga la estructura exacta de retroceso de la versión MQL5.

## Uso

1. Adjuntar la estrategia a una cartera y valor en el diseñador de StockSharp o mediante código.
2. Configurar el marco temporal de velas, el estilo de suavizado y los permisos de trading para que coincidan con la configuración preferida de Cronex RSI.
3. Lanzar la estrategia. Se suscribirá a la serie de velas seleccionada, actualizará la combinación RSI/MA y enviará órdenes a mercado en cruces confirmados.
4. Usar los asistentes de gráficos integrados para visualizar las curvas de indicadores y las operaciones ejecutadas para mayor validación.
