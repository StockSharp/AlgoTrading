# EMA Estrategia de retroceso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de retroceso EMA es una adaptación de alto nivel del asesor experto MetaTrader "Ema". Observa un par de promedios móviles exponenciales (EMA) con los períodos 5 y 10 calculados sobre los precios medios de las velas. Cuando aparece un cruce alcista o bajista, la estrategia espera a que el precio retroceda hacia el extremo de la vela anterior antes de entrar en la dirección del cruce. Los niveles fijos de toma de ganancias y límite de pérdidas medidos en puntos de precio gestionan el riesgo una vez que la posición está abierta.

## Lógica de trading
1. Suscríbase a la serie de velas configurada (predeterminado: período de tiempo de 5 minutos) y calcule dos EMA en el precio medio `(high + low) / 2`.
2. Detecta un cruce alcista cuando el EMA rápido cruza por encima del EMA lento, o un cruce bajista cuando el EMA rápido cruza por debajo del {PH003}} lento.
3. Armar una entrada de retroceso después de que ocurra el cruce:
   - Para una configuración larga, espere hasta que el precio de cierre retroceda hasta el máximo de la vela anterior menos el desplazamiento `MoveBackPoints` mientras el EMA rápido permanece por encima del EMA lento en al menos dos puntos de precio.
   - Para una configuración corta, espere hasta que el precio de cierre vuelva al mínimo de la vela anterior más el `MoveBackPoints` compensado mientras el EMA lento se mantiene por encima del EMA rápido en al menos dos puntos de precio.
4. Cuando se cumpla la condición de retroceso, envíe una orden de mercado con el volumen comercial configurado.
5. Al ingresar, calcule los niveles estáticos de toma de ganancias y límite de pérdidas usando las configuraciones `TakeProfitPoints` y `StopLossPoints`, convertidos en compensaciones de precio absoluto del precio de entrada.
6. Supervise cada vela terminada y cierre la posición una vez que el máximo o mínimo de la vela toque el nivel de toma de ganancias o de stop-loss.

## Parámetros
| Nombre | Predeterminado | Descripción |
|------|---------|-------------|
| `TradeVolume` | `0.1` | Volumen utilizado para cada orden de mercado. |
| `FastLength` | `5` | Periodo de la EMA rápida aplicado a los precios medianos. |
| `SlowLength` | `10` | Período de lentitud EMA aplicado a los precios medios. |
| `MoveBackPoints` | `3` | Distancia de retroceso, en puntos de precio, medida desde el extremo de la vela anterior. |
| `TakeProfitPoints` | `5` | Distancia de obtención de beneficios, en puntos de precio. |
| `StopLossPoints` | `20` | Distancia de stop-loss, en puntos de precio. |
| `CandleType` | `5m` | Marco de tiempo utilizado para la suscripción de velas y los cálculos de indicadores. |

## Notas
- Sólo se procesan velas completamente formadas para evitar señales prematuras.
- La estrategia alinea automáticamente la propiedad `Strategy.Volume` con el parámetro `TradeVolume` al inicio.
- Todos los cálculos se basan en el instrumento `PriceStep` para convertir distancias basadas en puntos en precios absolutos.
- La estrategia abre como máximo una posición a la vez y requiere un nuevo cruce EMA antes de preparar otra operación.
