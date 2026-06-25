# Estrategia GoldWarrior02b
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia algorítmica convertida del asesor experto de MetaTrader *GoldWarrior02b*.
Combina un medidor de impulso, el Commodity Channel Index (CCI) y un simple detector de oscilación ZigZag
para operar cerca del final de cada bloque de 15 minutos.

La implementación se dirige a la API de alto nivel de StockSharp y se centra en posiciones netas.
El hedging multinivel del script original no es compatible porque StockSharp trabaja con posiciones neteadas.

## Concepto

- Usar un indicador de impulso personalizado que promedia la diferencia entre los precios de apertura y cierre de las velas.
- Evaluar los valores del CCI para detectar reversiones de sobrecompra/sobreventa y picos de momentum fuertes.
- Derivar una dirección de oscilación ZigZag de los máximos y mínimos recientes para evitar operar contra el movimiento dominante.
- Solo evaluar señales durante los segundos finales (>= 45s) de los minutos 14, 29, 44 y 59.
- Aplicar gestión de riesgo dinámica con stop-loss, take-profit, trailing-stop y un objetivo de beneficio global.

## Reglas de Entrada

Una operación se considera solo si no hay posición abierta y la vela actual cierra dentro de
la ventana de tiempo descrita anteriormente.

### Configuración Larga
- La oscilación ZigZag apunta hacia abajo (el mínimo reciente es menor que el anterior).
- Ya sea:
  - El CCI sube por encima de su lectura anterior mientras el CCI anterior estaba por debajo de -50, el CCI actual por debajo de -30,
    el impulso se vuelve positivo y el impulso anterior era negativo.
  - O el CCI cae por debajo de -200, el CCI anterior era aún más bajo, el impulso permanece por debajo del umbral positivo
    y el impulso anterior es más débil que el valor actual.

### Configuración Corta
- La oscilación ZigZag apunta hacia arriba (el máximo reciente es mayor que el anterior).
- Ya sea:
  - El CCI cae por debajo de su lectura anterior mientras el CCI anterior estaba por encima de 50, el CCI actual por encima de 30,
    el impulso se vuelve negativo y el impulso anterior era positivo.
  - O el CCI supera 200, el CCI anterior era más alto, el impulso se mantiene por encima del umbral negativo
    y el impulso anterior es más fuerte que el valor actual.

Si el impulso anterior permanece entre los umbrales de compra y venta configurados, las señales se ignoran.

## Reglas de Salida

- **Stop-loss**: cierra la posición cuando el precio cruza la distancia de stop desde el precio de entrada.
- **Take-profit**: cierra después de alcanzar la distancia de beneficio configurada.
- **Trailing stop**: una vez que el precio avanza por `(TrailingStop + TrailingStep)` puntos, el nivel de trailing sigue al precio
  a una distancia de `TrailingStop` puntos. Cruzar el nivel de trailing sale de la operación.
- **Objetivo de beneficio global**: cierra la posición cuando el PnL no realizado supera el importe especificado (en moneda de la cuenta).

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `BaseVolume` | Tamaño de operación para entradas. | `0.1` |
| `StopLossPoints` | Distancia del stop en puntos. | `100` |
| `TakeProfitPoints` | Distancia del take-profit en puntos. | `150` |
| `TrailingStopPoints` | Distancia base del trailing stop. | `5` |
| `TrailingStepPoints` | Distancia adicional antes de que el trailing stop se active. | `5` |
| `ImpulsePeriod` | Período para los cálculos del CCI e impulso. | `21` |
| `ZigZagDepth` | Mínimo de barras entre nuevas oscilaciones ZigZag. | `12` |
| `ZigZagDeviation` | Movimiento de precio mínimo (en puntos) para confirmar una oscilación. | `5` |
| `ZigZagBackstep` | Mínimo de barras antes de aceptar una nueva oscilación. | `3` |
| `ProfitTarget` | Umbral de beneficio no realizado para cerrar todas las posiciones. | `300` |
| `ImpulseSellThreshold` | Umbral de impulso para cortos (típicamente negativo). | `-30` |
| `ImpulseBuyThreshold` | Umbral de impulso para largos (típicamente positivo). | `30` |
| `CandleType` | Marco temporal usado para los cálculos. | `Marco temporal de 5 minutos` |

## Notas

- El indicador de impulso es una media móvil de la diferencia entre los valores de apertura y cierre de las velas
  escalada por el paso de precio del instrumento.
- Los cálculos de trailing y PnL se basan en `PriceStep` y `StepPrice` del instrumento para convertir
  distancias en puntos a moneda de la cuenta.
- El asesor experto original escala tamaños de posición y despliega niveles de hedging.
  Este puerto de StockSharp mantiene una única posición neta por instrumento, correspondiendo con el modelo de ejecución de StockSharp.
- Para replicar el comportamiento original más fielmente, considera habilitar una suscripción de velas de 15 minutos
  y asegurarte de que la latencia de datos de ticks permita la ejecución poco después del timestamp de cierre.

## Aviso Legal

Esta muestra es para fines educativos. Antes de ejecutar en mercados en vivo, valide la estrategia bajo
condiciones realistas de datos, latencia y comisiones.
