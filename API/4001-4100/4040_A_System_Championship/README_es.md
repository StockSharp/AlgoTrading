# Una estrategia de campeonato del sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto del MetaTrader 4 asesor experto "Un sistema: edición final de la estrategia del campeonato" (archivo `ACB6.MQ4`).
- Detecta rupturas alcistas o bajistas en un período de tiempo primario configurable y confirma el impulso con precios de oferta y demanda en vivo.
- Utiliza un marco de tiempo secundario para dimensionar la distancia del trailing stop, reproduciendo la lógica multiproceso del EA original a través de dos flujos de velas.
- Implementa los bloques de parada de acciones globales, pausa comercial y tamaño de riesgo adaptable que estaban codificados en el robot de origen.

## Suscripciones de datos
- Se suscribe a dos series de velas (`PrimaryTimeFrame`, `SecondaryTimeFrame`) para reconstruir los rangos de precios utilizados para objetivos y topes dinámicos.
- Suscríbase a cotizaciones de nivel 1 para leer la mejor oferta/demanda que activan entradas, comprobaciones de stop-loss, toma de ganancias y salida de retroceso.

## Condiciones de entrada
1. Espere a que termine la vela principal y calcule su rango multiplicado por `TakeFactor`.
2. Vaya largo cuando:
   - La vela cierra por encima de su punto medio.
   - El precio de venta actual rompe el máximo de la vela.
   - La distancia entre la oferta y el mínimo de la vela supera `MinStopDistance`.
3. Vaya en corto cuando las condiciones reflejadas sean ciertas para la ruptura a la baja.
4. Omita las entradas si la distancia de obtención de beneficios calculada es menor que el espacio mínimo entre paradas.

## Gestión de salidas
- **Niveles de protección iniciales**: el stop está anclado al mínimo/máximo de la vela anterior, mientras que la toma de ganancias es igual al precio de entrada más/menos el rango multiplicado por `TakeFactor`.
- **Salida de retroceso** (`FallLimit`/`FallFactor`):
  - Realice un seguimiento de la excursión máxima favorable para la posición activa.
  - Si el movimiento actual cae por debajo de `FallLimit * maxMove` *y* el movimiento ya avanzó más allá de `FallFactor * target`, cierre la operación en el mercado.
- **Parada móvil** (`TrailFactor`):
  - La distancia final es igual al rango del período de tiempo secundario multiplicado por `TrailFactor`.
  - El stop solo se mueve en la dirección comercial y nunca cruza la toma de ganancias o el espacio mínimo entre stop.
- **Paradas duras**: el precio que toca los niveles de parada o toma mantenidos da como resultado una liquidación inmediata utilizando órdenes de mercado.

## Gestión de riesgos
- **Tamaño de posición dinámico**: combina `RiskPerTrade` con el valor de pip derivado de `Security.StepSize` y `Security.StepPrice`. El volumen resultante se redondea según las restricciones de intercambio y nunca baja de `BaseVolume`.
- **Ajuste estadístico**: el ratio `LossesExpected/TradesExpected` del EA original modula el riesgo por operación comparándolo con el ratio de pérdida realizada.
- **Parada de equidad** (`SystemStop`): rastrea el pico de equidad y deshabilita nuevas operaciones si el valor actual cae por debajo de `SystemStop * peak`. Los registros informativos marcan la parada de activación y recuperación.
- **Pausa comercial** (`TradePause`): aplica una ventana de enfriamiento después de cada orden de mercado, al igual que la implementación de MT4.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `PrimaryTimeFrame` | 1 dia | Se utiliza un plazo más alto para la detección de fugas. |
| `SecondaryTimeFrame` | 4 horas | Marco temporal que proporciona el rango del trailing stop. |
| `TakeFactor` | 0,8 | Multiplicador aplicado al rango de velas principal al crear la toma de ganancias. |
| `TrailFactor` | 10 | Multiplicador aplicado al rango de velas secundarias al actualizar el trailing stop. |
| `FallLimit` | 0,5 | Relación del beneficio máximo que permite la salida del retroceso. |
| `FallFactor` | 0,4 | Participación mínima del objetivo total que debe alcanzarse antes de que se permita una salida de retroceso. |
| `RiskPerTrade` | 0,02 | Fracción del capital asignado a cada operación antes de ajustes. |
| `BaseVolume` | 1 | El tamaño de la orden alternativa se utiliza cuando el tamaño del riesgo produce un volumen menor. |
| `MinStopDistance` | 0 | Restricción de distancia de parada de cambio expresada en unidades de precio. |
| `TradePause` | 5 minutos | Período de espera después de cualquier orden ejecutada. |
| `SystemStop` | 0,8 | Factor de reducción para el stop de acciones de la cartera (por ejemplo, 0,8 = reducción permitida del 20 %). |
| `LossesExpected` | 20 | Número esperado de operaciones perdedoras para el ajuste de riesgo. |
| `TradesExpected` | 85 | Número esperado de operaciones totales para el ajuste de riesgo. |

## Notas de implementación
- Los hilos de bloqueo/cobertura de la versión MQL se omiten porque las estrategias StockSharp operan en una posición neta. El control de riesgos y la lógica de seguimiento proporcionan un mecanismo de protección del capital equivalente.
- Los niveles de detener y tomar se rastrean dentro de la estrategia en lugar de utilizar órdenes nativas separadas para mantenerse alineados con el motor de backtesting.
- Asegúrese de que la seguridad conectada exponga `StepSize`, `StepPrice`, `MinVolume` y `VolumeStep`; de lo contrario, el tamaño vuelve a ser `BaseVolume`.
- La estrategia debe ejecutarse con cotizaciones en tiempo real habilitadas; de lo contrario, solo se ejecutarán las actualizaciones impulsadas por velas y la lógica de parada reaccionará con la latencia de la vela.
