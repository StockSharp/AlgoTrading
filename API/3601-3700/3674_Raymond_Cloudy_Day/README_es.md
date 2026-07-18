# Estrategia del día nublado de Raymond
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Raymond Cloudy Day es una estrategia de seguimiento de rupturas que reconstruye la lógica comercial del asesor experto original **"Raymond Cloudy Day for EA"** MQL5. El algoritmo deriva un conjunto de niveles de referencia de una vela de marco temporal más alto y los utiliza para detectar la reanudación del impulso en el marco temporal de ejecución. El puerto StockSharp mantiene las reglas comerciales originales al tiempo que expone cada componente como parámetros de estrategia configurables.

## Datos de mercado
- **Velas de señal**: el período de tiempo en el que se ejecutan las operaciones. La estrategia se suscribe a esta serie para señales de entrada y gestión de posiciones.
- **Velas pivotantes**: el período de tiempo más alto utilizado para calcular los niveles de Raymond. Por defecto, esta es una vela diaria, que reproduce la entrada MQL5 `RayMondTimeframe`.

Ambas suscripciones se registran automáticamente a través de `GetWorkingSecurities`, por lo que la estrategia solicita los flujos de datos necesarios tan pronto como se inicia.

## Cálculo del nivel de Raymond
Para cada vela pivote terminada, la estrategia almacena los cuatro niveles centrales definidos por el EA original:

\[
\begin{alineado}
TradeSS &= \frac{Alto + Bajo + Abrir + Cerrar}{4} \\
PivotRange &= Alto - Bajo \\
ETB &= TradeSS + 0.382 \times PivotRange \\
ETS &= ComercioSS - 0,382 \times PivotRange \\
TPB1 &= ComercioSS + 0.618 \times PivotRange \\
TPS1 &= ComercioSS - 0,618 \times PivotRange \\
TPB2 &= ComercioSS + PivotRange \\
TPS2 &= TradeSS - PivotRange
\end{alineado}
\]

La implementación StockSharp mantiene la instantánea más reciente de estos valores y registra cada actualización, lo que permite al usuario monitorear cómo evolucionan los niveles con el tiempo.

## Lógica de entrada
Una vez que los niveles de Raymond están disponibles, la estrategia evalúa cada vela de señal terminada:

1. **Configuración larga**: si el mínimo de la vela cae por debajo de `TPS1` y el cierre regresa por encima del nivel, la estrategia ingresa en una posición larga. Esto refleja la condición EA `Low[1] < TPS1 && Close[1] > TPS1` y captura el rechazo alcista del nivel.
2. **Configuración corta**: si la vela permanece completamente por encima de `TPS1` pero cierra por debajo, la estrategia abre una posición corta (que coincide con la regla original, aunque asimétrica).

Antes de realizar una nueva orden, el algoritmo cancela las órdenes pendientes y, si es necesario, cierra la posición opuesta para que solo quede activa una operación direccional.

## Gestión del riesgo
Raymond Cloudy Day utiliza compensaciones protectoras simétricas medidas en garrapatas:

- **Stop-loss** – posicionado `ProtectiveOffsetTicks` debajo de la entrada larga (o encima de la entrada corta).
- **Take-profit** – posicionado `ProtectiveOffsetTicks` encima de la entrada larga (o debajo de la entrada corta).

Las compensaciones se multiplican por el `PriceStep` del instrumento para convertir ticks en distancias de precios absolutas. Cada vela de señal completada activa una verificación que cierra la posición cuando se alcanza cualquiera de los niveles de protección. Cuando la estrategia es plana, el estado de protección interna se restablece para evitar niveles obsoletos.

## Parámetros
| Nombre | Descripción | Predeterminado | Notas |
|------|-------------|---------|-------|
| `TradeVolume` | Volumen de pedidos utilizado para cada entrada. | `1` | Sincronizado con la propiedad `Volume` al inicio. |
| `ProtectiveOffsetTicks` | Distancia en ticks tanto para stop-loss como para take-profit. | `500` | Multiplicado por `PriceStep` para obtener precios absolutos. |
| `SignalCandleType` | Tipo de vela que produce señales comerciales. | `1 hour` período de tiempo | Se puede establecer en cualquier `DataType` que represente velas. |
| `PivotCandleType` | Mayor plazo para los cálculos del nivel de Raymond. | `1 day` período de tiempo | Coincide con la entrada `RayMondTimeframe` del MQL EA. |

Todos los parámetros admiten rangos de optimización y metadatos descriptivos para StockSharp Designer.

## Notas adicionales
- La estrategia requiere que `PriceStep` esté definido por la seguridad conectada. Si falta, se omiten las entradas comerciales y se registra una advertencia.
- La visualización del gráfico suma las velas de ejecución junto con las operaciones ejecutadas. Se pueden agregar dibujos personalizados adicionales si lo desea.
- La implementación evita el sondeo directo del valor del indicador y procesa solo velas terminadas, cumpliendo con las pautas del proyecto en `AGENTS.md`.

## Detalles específicos originales del EA conservados
- Fórmulas y multiplicadores de nivel Raymond (`0.382`, `0.618`, `1.0`).
- Lógica de entrada basada en la obtención de beneficios de la primera venta (`TPS1`).
- Compensaciones simétricas de stop-loss y take-profit de 500 puntos convertidas en ticks en el entorno StockSharp.

Con estos componentes, la estrategia StockSharp se comporta de manera idéntica a la fuente EA y al mismo tiempo proporciona una configuración rica y un registro adecuado para futuras investigaciones y automatización.
