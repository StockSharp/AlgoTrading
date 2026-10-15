# Estrategia RAVIiAO (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia RAVIiAO** reproduce el MetaTrader 4 asesor experto "RAVIiAO" dentro del StockSharp alto nivel API. el sistema
espera a que se cierre una nueva vela, evalúa la pendiente del oscilador RAVI junto con Bill Williams' Aceleración/Desaceleración (AC)
oscilador, y abre una posición inmediatamente en el mercado cuando ambos indicadores coinciden en la dirección de la tendencia. El puerto mantiene el
conjunto de parámetros original (períodos de media móvil, umbral, distancias de stop-loss/take-profit y volumen de órdenes) que permiten a los operadores
para replicar el comportamiento heredado sin ajustes manuales.

## Flujo de trabajo principal
1. **Suscripción a velas**: la estrategia se suscribe a un período de tiempo configurable (velas de 30 minutos de forma predeterminada).
2. **Actualizaciones de indicadores**: en cada vela terminada, actualiza dos promedios móviles simples para construir el oscilador y los feeds RAVI.
la misma vela en un Awesome Oscillator + par de suavizado de 5 períodos para obtener el valor AC.
3. **Preparación de señal**: la última vela terminada se almacena como "barra 1", mientras que el valor anterior se convierte en "barra 2", coincidiendo con el
`iCustom(...,1)` y `iCustom(...,2)` llamadas desde MetaTrader.
4. **Decisión de entrada**: se abre una posición larga cuando tanto AC como RAVI aumentan por encima de sus valores anteriores y confirman una
entorno alcista (`AC[1] > AC[2] > 0` y `RAVI[1] > RAVI[2] > Threshold`). Las operaciones cortas utilizan las condiciones reflejadas.
5. **Gestión de riesgos**: tan pronto como se ejecuta una orden, la estrategia registra niveles estáticos de stop-loss y take-profit expresados en
puntos del instrumento (es decir, `StopLossPoints * PriceStep`). Las velas se monitorean para detectar violaciones dentro de la barra utilizando sus precios altos/bajos.
6. **Restablecimiento de estado**: cuando se alcanza un nivel de protección, la posición se cierra con una orden de mercado y los buffers internos se restablecen.
para la próxima oportunidad.

## Reglas de trading
- **Entradas largas**
  - Valor de CA anterior por encima del valor de CA anterior y ambos mayores que cero.
  - Lectura de RAVI anterior por encima del umbral y del valor de RAVI anterior.
  - Ninguna posición activa en el momento de la señal.
- **Entradas cortas**
  - Valor de CA anterior por debajo del valor de CA anterior y ambos por debajo de cero.
  - Lectura de RAVI anterior por debajo del umbral negativo y por debajo del valor de RAVI anterior.
  - No hay posición activa cuando se dispara la señal.
- **La posición sale**
  - Los niveles estáticos de stop-loss y take-profit se expresan en puntos brutos, convertidos en compensaciones de precios a través del instrumento `PriceStep`.
  - Las infracciones se detectan con velas extremas (mínimo para paradas largas, máximo para paradas cortas, etc.) y se cierran inmediatamente a través del mercado.
órdenes para emular las órdenes de protección de MetaTrader.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Periodo de tiempo utilizado para la suscripción de velas (predeterminado 30 minutos). |
| `FastLength` | Longitud media de movimiento rápido utilizada en el oscilador RAVI. |
| `SlowLength` | Longitud media de movimiento lento utilizada en el oscilador RAVI. |
| `Threshold` | Porcentaje mínimo absoluto de RAVI para validar la continuación de una tendencia. |
| `StopLossPoints` | Distancia de stop-loss en puntos del instrumento (multiplicada por `PriceStep`). |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos del instrumento. |
| `TradeVolume` | Volumen de órdenes de mercado para cada entrada. |

## Notas de conversión
- El puerto StockSharp almacena los dos valores de indicador más recientes para que la decisión en la vela *n* reutilice el `AC[1]` y
`RAVI[1]` valores de MetaTrader (es decir, resultados de la barra anterior), preservando el estilo de ejecución de "nueva barra" de EA.
- AC se reconstruye a través de la diferencia entre Awesome Oscillator y su promedio móvil simple de 5 períodos, igualando el MT4
cadena de cálculo.
- Las paradas y los objetivos se evalúan frente a los extremos de las velas en lugar de colocar órdenes de protección pendientes; esto refleja el efecto
del manejo SL/TP integrado de MetaTrader manteniendo la implementación idiomática para StockSharp.

## Consejos de uso
- Asegúrese de que el instrumento seleccionado exponga un `PriceStep` correcto; de lo contrario, las distancias de protección no coincidirán con la versión MT4.
- Optimice los parámetros `Threshold`, `FastLength` y `SlowLength` al adaptar la estrategia a mercados con diferentes
características de volatilidad.
- Combine la estrategia con StockSharp protecciones a nivel de cartera o conector para obtener seguridad adicional durante las operaciones en vivo.
