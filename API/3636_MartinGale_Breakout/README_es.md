# Estrategia de ruptura de MartinGale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de ruptura de MartinGale** es un sistema de seguimiento de rupturas convertido del MetaTrader 4 asesor experto *MartinGaleBreakout*. El robot original ingresa a posiciones después de detectar velas anormalmente grandes y aplica un mecanismo de recuperación estilo martingala para recuperar pérdidas anteriores. Este puerto StockSharp reproduce el comportamiento utilizando la estrategia de alto nivel API con suscripciones de velas y parámetros de administración de dinero.

La estrategia monitorea una serie de velas configurables, buscando velas cuyo rango sea al menos tres veces mayor que el rango promedio de las diez barras anteriores. Cuando dicha vela se cierra con fuerza en una dirección, la estrategia abre una posición de mercado en esa dirección. Si la posición se cierra con una pérdida que excede un umbral configurable, el modo de recuperación aumenta la distancia de toma de ganancias para compensar la reducción realizada.

## Lógica de trading
1. Suscríbete a la serie de velas seleccionada (velas de 15 minutos por defecto).
2. Mantenga las 11 velas terminadas más recientes para evaluar una volatilidad anormal.
3. Detecta una ruptura alcista cuando:
   - La vela actual es tres veces mayor que el rango promedio de las diez velas anteriores.
   - La vela cierra en la mitad superior de su rango.
4. Detecte una ruptura bajista utilizando las condiciones simétricas.
5. Abra una posición de mercado en la dirección de ruptura si:
   - Actualmente no hay ningún otro puesto disponible.
   - La exposición de capital estimada está por debajo del porcentaje de saldo configurado.
6. Cerrar posiciones y restablecer objetivos de pérdidas/ganancias cuando:
   - El beneficio flotante alcanza el umbral de obtención de beneficios.
   - La pérdida flotante alcanza el umbral de stop-loss.
7. Cuando se produzca un stop-loss, cambie al modo de recuperación:
   - Aumente la distancia de toma de ganancias mediante el multiplicador configurado.
   - Amplíe el límite de stop-loss al porcentaje máximo permitido.
   - Continúe operando hasta alcanzar el siguiente objetivo, luego restablezca la configuración base.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TakeProfitPoints` | Distancia base de toma de ganancias expresada en puntos del instrumento. | `50` |
| `BalancePercentageAvailable` | Participación máxima del saldo de la cuenta que se puede asignar a una sola operación. | `50%` |
| `TakeProfitBalancePercent` | Beneficio objetivo como porcentaje del saldo de la cuenta. | `0.1%` |
| `StopLossBalancePercent` | Reducción máxima antes de desencadenar la recuperación. | `10%` |
| `StartRecoveryFactor` | Parte del stop-loss utilizado antes de activar el modo de recuperación. | `0.2` |
| `TakeProfitPointsMultiplier` | Multiplicador aplicado a la distancia de obtención de beneficios durante la recuperación. | `1` |
| `CandleType` | Serie de velas utilizadas para cálculos de ruptura. | `15-minute` |

## Dimensionamiento de posiciones y control de riesgos
- La estrategia calcula el volumen requerido para lograr la toma de ganancias monetaria configurada utilizando el tamaño y el valor del tick del instrumento.
- Los volúmenes están normalizados para las restricciones de intercambio (paso, mínimo, máximo).
- La exposición de capital estimada no debe exceder el porcentaje de saldo configurado.
- El modo de recuperación expande dinámicamente el objetivo de obtención de ganancias después de una pérdida, emulando el comportamiento martingala original mientras mantiene las posiciones limitadas a una única operación abierta.

## Notas
- La estrategia se basa en la información del saldo de la cartera; inicialícelo con una conexión de cartera antes de comenzar.
- El manejo de comisiones refleja el EA original al centrarse en las pérdidas y ganancias flotantes derivadas de la posición actual.
- No se utilizan órdenes pendientes; las entradas y salidas se realizan únicamente con órdenes de mercado.
