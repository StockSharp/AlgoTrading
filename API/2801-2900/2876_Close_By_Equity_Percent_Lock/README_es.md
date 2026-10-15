# Estrategia de Bloqueo de Capital por Porcentaje
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- **Categoría**: Gestión de riesgo / automatización a nivel de cuenta.
- **Fuente original**: Asesor experto MQL5 "Close by Equity Percent" (#20880).
- **Propósito**: Monitorear el capital de la cuenta frente al último balance plano y liquidar todas las posiciones abiertas una vez que el capital crece a un múltiplo configurable de ese balance.
- **Instrumentos**: Cualquier instrumento ya operado por otras estrategias o traders manuales dentro del mismo portafolio.

## Idea central
El asesor experto MQL original compara el capital actual de la cuenta con el saldo de la cuenta (que solo cambia después de que las posiciones están planas). Cuando el capital alcanza o supera `Balance * EquityPercentFromBalance`, el script cierra todas las posiciones abiertas para asegurar ganancias. Este port de StockSharp mantiene la misma lógica de protección de cuenta mientras se integra con la API de estrategia de alto nivel.

## Cómo funciona
1. Cuando la estrategia inicia, toma una instantánea del valor actual del portafolio. Esta actúa como referencia de "saldo" mientras la cuenta está plana.
2. La estrategia se suscribe a velas de 1 minuto (configurable mediante `CandleType`) en el instrumento configurado. El flujo de velas solo se usa como temporizador para activar comprobaciones de capital.
3. En cada vela completada:
   - Si todas las posiciones están planas, la instantánea del saldo se actualiza al último valor del portafolio.
   - El capital actual (`Portfolio.CurrentValue`) se compara con `balanceSnapshot * EquityPercentFromBalance`.
   - Cuando el capital alcanza o supera el umbral, todas las posiciones abiertas en el portafolio se cierran mediante `ClosePosition(position.Security)`.
4. La instantánea del saldo se actualiza de nuevo una vez que todas las posiciones están cerradas, permitiendo que el ciclo reinicie.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ------ | ---- | -------------- | ----------- |
| `EquityPercentFromBalance` | decimal | 1.20 | Múltiplo de capital que debe alcanzarse antes de liquidar todas las posiciones. El valor `1.20` significa "cerrar todo cuando el capital sea el 120% del último balance plano". |
| `CandleType` | `DataType` | Vela de 1 minuto | Flujo de datos usado únicamente para activar comprobaciones periódicas de capital. Ajuste para que coincida con la cadencia que prefiere para monitorear el capital. |

## Notas de implementación
- Usa `Strategy.ClosePosition(Security)` para cada posición abierta, reflejando el bucle `PositionClose` en la versión MQL.
- Rastrea la instantánea del saldo solo después de que todas las posiciones están planas, reproduciendo cómo el script MQL dependía de `AccountBalance` (que se actualiza después de que las posiciones se cierran).
- La estrategia es a nivel de cuenta: no abre posiciones por sí misma, e intentará cerrar **todas** las posiciones dentro del portafolio conectado independientemente del símbolo.
- Requiere que tanto `Portfolio` como `Security` estén asignados antes de comenzar. El instrumento solo se usa para suscribirse a velas que proporcionan eventos de temporización.

## Directrices de uso
1. Adjunte la estrategia al portafolio que desea proteger y establezca el instrumento cuyo flujo de velas desea usar como temporizador (por ejemplo, un instrumento muy líquido).
2. Ajuste `EquityPercentFromBalance` al múltiplo de toma de ganancias que se adapte a su plan de riesgo.
3. Inicie la estrategia. Cada vez que el capital alcance el múltiplo especificado del último balance plano, todas las posiciones abiertas en el portafolio se cerrarán automáticamente.
4. Después de la liquidación, la instantánea del saldo se actualiza, por lo que el próximo ciclo de ganancias esperará nuevamente a que el capital crezca el porcentaje configurado antes de activar otro cierre.

## Ejemplo práctico
- Instantánea de balance inicial = 10.000 USD.
- `EquityPercentFromBalance = 1.2` → capital objetivo = 12.000 USD.
- Las posiciones abiertas se aprecian hasta que el capital llega a 12.050 USD.
- La estrategia cierra todas las posiciones abiertas; la instantánea del saldo se actualiza una vez que el portafolio está plano (por ejemplo, a 12.000 USD).
- El siguiente ciclo espera a que el capital supere 12.000 * 1.2 = 14.400 USD antes de actuar de nuevo.
