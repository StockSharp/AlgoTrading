# Estrategia automática de KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Auto KDJ es una conversión directa del MetaTrader 4 asesores expertos `AutoKdj.mq4` creado por *senlin ge*. El sistema comercializa un único símbolo y evalúa el oscilador estocástico suavizado conocido como **KDJ** (también llamado %K, %D, %J). La implementación de StockSharp recrea la misma lógica de indicador y las opciones de administración de dinero expuestas en el asesor experto original, al tiempo que aprovecha las características de alto nivel de API, como suscripciones de velas, vinculación de indicadores y órdenes de protección automáticas.

KDJ está construido sobre el oscilador estocástico. Primero calcula un valor Stochastic sin procesar (RSV), lo suaviza en la línea %K, suaviza %K nuevamente en la línea %D y usa su diferencia (conocida como *KDC* en el código fuente) para detectar cambios en el impulso. Auto KDJ abre como máximo una posición de mercado a la vez y aplica inmediatamente las protecciones solicitadas de stop-loss/take-profit.

## Construcción del indicador
1. **Cálculo del RSV**: para cada vela terminada, se recopilan el máximo más alto y el mínimo más bajo de `KDJ Length` velas. El RSV se calcula como:
\[
RSV = \frac{\text{Cerrar} - \text{LowestLow}}{\text{HighestHigh} - \text{LowestLow}} \times 100
\]
2. **Suavizado %K**: los valores RSV se promedian durante `Smooth %K` períodos para obtener la línea %K.
3. **Suavizado %D**: los valores %K se promedian durante `Smooth %D` períodos para producir la línea %D.
4. **Señal KDJ**: el algoritmo analiza `K - D` (el búfer *KDC* de la versión MQL) y la pendiente de %K para generar entradas y salidas.

Esta canalización se implementa con el indicador `Stochastic` de StockSharp configurando el período y suavizando los parámetros para reflejar los buffers MetaTrader.

## Reglas de trading
Las señales se evalúan una vez por vela terminada. La estrategia se niega a abrir otra posición mientras haya una operación abierta o una orden de salida pendiente, lo que coincide con el comportamiento del asesor experto MQL.

### Condiciones de entrada
- **Compre** cuando se cumpla una de las siguientes condiciones:
  - `K - D` cruza de negativo a positivo.
  - `K - D` ya es positivo y %K está aumentando (`K_current > K_previous`).
- **Vender** cuando se cumpla una de las siguientes condiciones:
  - `K - D` cruza de positivo a negativo.
  - `K - D` ya es negativo y %K está cayendo (`K_current < K_previous`).

### Condiciones de salida
- **Cierre largo** cuando `K - D` cruce por debajo de cero o cuando %K comience a caer.
- **Cierre corto** cuando `K - D` cruce por encima de cero o cuando %K comience a subir.

Cuando la posición se aplana, la estrategia registra si la operación fue rentable o no. Las pérdidas consecutivas influyen en el tamaño de la siguiente posición exactamente de la misma manera que la lógica `DecreaseFactor` del MQL EA.

## Gestión monetaria
El asesor experto original proporciona un interruptor `whichmethod` para combinar el comportamiento de stop-loss y take-profit, además de una rutina dinámica de tamaño de lote basada en el uso del margen y las rachas de pérdidas. El puerto StockSharp reproduce estas capacidades como parámetros individuales:

- **Conmutaciones de stop-loss/take-profit**: los indicadores booleanos independientes permiten habilitar o deshabilitar cada tramo protector. Cuando está activo, `StartProtection` adjunta las salidas protectoras y maneja la ejecución del mercado.
- **Volumen basado en riesgo**: el tamaño del pedido comienza en `Base Volume` y se puede aumentar para satisfacer la fracción solicitada de `Maximum Risk` de la cartera. El consumo de margen se aproxima a través del tamaño del contrato del instrumento y el apalancamiento configurado, que emula el cálculo MT4 `AccountFreeMargin * MaximumRisk * Leverage / 100000`.
- **Reducción de la racha de pérdidas**: después de dos o más operaciones perdedoras consecutivas, la siguiente orden se reduce en `volume * losses / DecreaseFactor`, coincidiendo con la rutina de caída de volumen original.

Todos los volúmenes se normalizan utilizando los valores `VolumeStep`, `MinVolume` y `MaxVolume` del valor para garantizar que el tamaño del pedido enviado sea negociable.

## Parámetros
| Parámetro | Descripción | Predeterminado | Optimización |
|-----------|-------------|---------|--------------|
| **Tipo de vela** | Tipo de datos/período de tiempo de las velas de entrada. | plazo de 15 minutos | – |
| **Duración de KDJ** | Período retrospectivo para el cálculo del RSV. | 30 | 10 → 60 paso 5 |
| **Suave %K** | Suavizado aplicado a la línea %K. | 3 | 1 → 10 paso 1 |
| **Suave %D** | Suavizado aplicado a la línea %D. | 6 | 1 → 15 paso 1 |
| **Detener pérdidas (pips)** | Distancia para el tope de protección. | 100 | 0 → 300 paso 10 |
| **Obtener ganancias (pips)** | Distancia para la toma de ganancias protectora. | 200 | 0 → 400 paso 10 |
| **Habilitar Stop Loss** | Alternar para el tramo de stop-loss. | Habilitado | – |
| **Habilitar toma de ganancias** | Alternar para el tramo de toma de ganancias. | Habilitado | – |
| **Volumen base** | Volumen mínimo antes del ajuste de riesgo. | 0.1 | – |
| **Riesgo máximo** | Fracción de capital asignado por operación. | 0,4 | 0,0 → 1,0 paso 0,1 |
| **Factor de disminución** | Reducción de volumen tras rachas de pérdidas. | 0.3 | 0,0 → 5,0 paso 0,5 |
| **Apalancamiento** | Apalancamiento de cuenta utilizado en el modelo de margen. | 100 | 10 → 500 paso 10 |

## Notas de uso
1. Configure la seguridad y conexión deseadas en StockSharp Designer, Shell o Runner.
2. Ajuste el tipo de vela para que coincida con el período de tiempo utilizado en MetaTrader.
3. Establezca preferencias de stop-loss/take-profit a través de los interruptores booleanos para reproducir el comportamiento `whichmethod`:
   - Desactive ambas piernas para "sin SL, sin TP".
   - Habilite solo el tramo de toma de ganancias o límite de pérdidas para reflejar los modos de protección parcial.
4. Opcionalmente, ajuste `Base Volume`, `Maximum Risk`, `Decrease Factor` y `Leverage` para reflejar la configuración de su corredor.
5. Inicia la estrategia. El asistente del gráfico traza automáticamente velas, el indicador KDJ y ejecuta operaciones para su verificación.

## Diferencias en comparación con la versión MQL
- El indicador personalizado `kdj.mq4` se reemplaza con el indicador `Stochastic` integrado de StockSharp configurado para proporcionar buffers idénticos, eliminando la necesidad de archivos externos.
- El tamaño de la posición utiliza el capital de la cartera, el tamaño del contrato y el apalancamiento proporcionados por la definición de seguridad StockSharp. Los corredores con diferentes multiplicadores de contrato pueden ajustar `Base Volume` o `Maximum Risk` en consecuencia.
- Las salidas protectoras dependen de `StartProtection`, que envía órdenes de mercado cuando se activa y registra el precio de ejecución. Esto ofrece el mismo comportamiento funcional que los parámetros `OrderSend` + detener/tomar en MetaTrader sin dejar de ser idiomático para StockSharp.
- La reducción del riesgo después de pérdidas consecutivas se rastrea a través de operaciones ejecutadas en lugar de escanear todo el historial comercial en cada tick, lo que mejora el rendimiento y mantiene los resultados idénticos.

## Pruebas
La estrategia se validó comparando los puntos de entrada/salida generados con la lógica MQL original en datos de muestra de EURUSD. Los comerciantes aún deben realizar pruebas de avance u optimización en su entorno objetivo para confirmar que el puerto se comporta como se espera con las especificaciones del contrato y el modelo de ejecución de su corredor.
