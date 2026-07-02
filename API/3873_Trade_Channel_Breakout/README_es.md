# Canal comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Trade Channel es una estrategia de reversión de canal convertida del asesor experto MetaTrader "TradeChannel". El sistema dibuja un canal de precios desde el máximo más alto y el mínimo más bajo sobre un número configurable de velas completadas. Cuando el canal deja de expandirse y el precio vuelve a probar uno de sus límites, la estrategia abre una posición en la dirección opuesta, esperando una reversión dentro del rango.

### Ideas centrales
- Utilice los indicadores **Más alto** y **Más bajo** para formar un canal similar a Donchian.
- Exija que el canal esté plano (sin nuevos máximos ni mínimos) antes de abrir una operación.
- Desvanece el toque de resistencia con posiciones cortas y el toque de soporte con posiciones largas.
- Coloque la parada de protección inicial a un rango verdadero promedio (ATR) del punto de ruptura.
- Opcionalmente, siga el stop una vez que la operación se mueva a favor de la posición.

## Parámetros
| Nombre | Descripción | Predeterminado | Optimización |
| --- | --- | --- | --- |
| `Volume` | Volumen comercial en lotes/contratos. | 1 | Habilitado (0.1 → 2.0, paso 0.1) |
| `ChannelLength` | Número de velas terminadas utilizadas para calcular los límites del canal. | 20 | Habilitado (10 → 60, paso 5) |
| `AtrPeriod` | Período del indicador ATR para la colocación de la parada. | 4 | Habilitado (2 → 20, paso 2) |
| `TrailingPoints` | Compensación del trailing stop medida en pasos de precio del instrumento. Establezca en `0` para deshabilitar el seguimiento. | 30 | Habilitado (0 → 100, paso 10) |
| `CandleType` | Tipo de vela y período de tiempo utilizado para los cálculos. | plazo de 30 minutos | — |

## Lógica comercial
1. Suscríbase a la serie de velas configuradas y proporcione tres indicadores: `Highest`, `Lowest` y `ATR`.
2. Espere hasta que todos los indicadores estén completamente formados. Los primeros valores completados inicializan el estado del canal y no se realizan transacciones en esa vela.
3. Por cada vela nueva terminada:
   - Actualice los límites del canal y calcule el pivote `(resistance + support + close) / 3`.
   - Compruebe si la resistencia (o soporte) no ha cambiado en comparación con la vela anterior. Una resistencia plana permite configuraciones cortas, un soporte plano permite configuraciones largas.
   - **Entrada corta:** si la resistencia es plana *y* la vela toca el máximo de resistencia o se cierra entre el pivote y la resistencia, envíe una orden de venta de mercado.
   - **Entrada larga:** si el soporte es plano *y* la vela toca el soporte bajo o se cierra entre el soporte y el pivote, envíe una orden de compra de mercado.
   - Sólo se permite una posición a la vez. La estrategia espera la señal del canal plano mientras no haya operaciones abiertas.
4. Al entrar:
   - Guarde el precio de entrada.
   - Establezca la parada inicial en `resistance + ATR` para cortos y `support − ATR` para largos.
5. Gestionar posiciones abiertas:
   - **Condiciones de salida para largos:**
     - El precio toca el límite superior del canal mientras permanece plano.
     - El mínimo de la vela cruza por debajo del nivel de stop inicial/trailing.
   - **Condiciones de salida para cortos:**
     - El precio toca el límite inferior del canal mientras permanece plano.
     - El máximo de la vela cruza por encima del nivel de stop inicial/de seguimiento.
6. Stop dinámico (si `TrailingPoints` > 0):
   - Convierta la entrada en unidades de precio utilizando el `Security.Step` del instrumento (recurre al valor bruto si el paso no está disponible).
   - Para posiciones largas, una vez que el cierre supere el precio de entrada en el desplazamiento final, mueva el stop a `close − offset`.
   - Para los cortos, una vez que el cierre cae por debajo del precio de entrada según la compensación, mueva el tope a `close + offset`.
   - El trailing stop nunca retrocede; sólo refuerza el nivel de protección.

## Notas
- Todas las decisiones se toman sobre velas terminadas para mantenerse alineadas con la lógica MQL original que usó `High[1]`, `Low[1]` y `Close[1]`.
- La verificación de igualdad entre el límite del canal actual y anterior es tolerante a los cambios de precios de los instrumentos para evitar fallas en el punto flotante.
- Las paradas finales se basan en metadatos `Security.Step` correctos. Si el intercambio no lo proporciona, se utiliza el valor bruto en puntos.
- La estrategia no envía correos electrónicos ni ajusta el tamaño de la posición de forma dinámica, porque esas características eran específicas de la plataforma en la implementación de MQL.
