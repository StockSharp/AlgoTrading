# Estrategia de máquina comercial AIS4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **AIS4 Trade Machine Strategy** es un asistente de operaciones manual que traslada el asesor experto original MetaTrader "AIS4 Trade Machine" a StockSharp. Mantiene el flujo de trabajo de una posición del script: el operador proporciona niveles absolutos de stop-loss y take-profit, emite un comando y la estrategia calcula el tamaño de la operación basándose en el capital de la cuenta corriente y las especificaciones del instrumento. Una vez completada la orden de mercado, la estrategia envía inmediatamente órdenes de protección emparejadas (stop + límite) para que los niveles de riesgo y recompensa solicitados se apliquen en el lado del intercambio.

La estrategia **no** genera señales automáticas. Está diseñado para ejecución discrecional donde el usuario decide cuándo y dónde ingresar o modificar una posición.

## flujo de trabajo manual
1. Asegúrese de que el instrumento conectado exponga `PriceStep`, `StepPrice`, `VolumeStep`, `MinVolume` y `MaxVolume`. Deben convertir el riesgo de precio en tamaño del contrato y alinear el volumen de la orden con los límites de cambio.
2. Antes de enviar un comando, configure `StopPrice` y `TakePrice` en los niveles de precios absolutos que desea utilizar.
3. Cambie `Command` a `Buy` o `Sell`. La estrategia:
   - Comprueba que no haya ninguna otra posición abierta.
   - Verifica que los stop-loss y take-profit solicitados respeten la distancia mínima de tick.
   - Calcula el presupuesto de riesgo a partir de `OrderReserve` × capital de la cartera actual y garantiza que se respete la reserva de capital (`AccountReserve`).
   - Estima el volumen de la orden a partir de la distancia de parada y el valor del tick del instrumento.
   - Envía la orden de mercado y luego envía órdenes de protección emparejadas (`SellStop`+`SellLimit` para posiciones largas, `BuyStop`+`BuyLimit` para posiciones cortas).
4. `Command` se restablece automáticamente a `Wait` después de que se maneja la acción para evitar ejecuciones duplicadas accidentales.

### Gestionar un puesto existente
- Establezca nuevos niveles de precios (use `0` para mantener el valor actual) y cambie `Command` a `Modify`. La estrategia cancela las órdenes de protección anteriores y las reemplaza por otras nuevas que coinciden con los precios actualizados.
- Cambie `Command` a `Close` para liquidar la posición activa en el mercado y cancelar cualquier orden de protección.

## Lógica de gestión de riesgos
- **Reserva de cuenta**: mantiene intacta una fracción del capital máximo. La negociación se bloquea mientras el capital disponible (`equity - peak_equity × (1 - AccountReserve)`) sea menor que el presupuesto de riesgo solicitado.
- **OrderReserve** – fracción del capital actual asignado a la siguiente operación. El presupuesto se transforma en un tamaño de contrato utilizando la distancia de parada y el valor del tick del instrumento (`PriceStep` × `StepPrice`).
- Si el volumen calculado cae por debajo de `MinVolume` o viola `VolumeStep`, el comando se rechaza y se escribe una advertencia en el registro.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Command` | `Wait` | Comando manual para ejecutar (`Buy`, `Sell`, `Modify`, `Close`). Regresa automáticamente a `Wait` después de la manipulación. |
| `StopPrice` | `0` | Nivel absoluto de stop-loss. Debe estar por debajo del precio de entrada para largos y por encima para cortos. |
| `TakePrice` | `0` | Nivel absoluto de obtención de beneficios. Debe estar por encima del precio de entrada para largos y por debajo para cortos. |
| `AccountReserve` | `0.20` | Fracción del patrimonio mantenido como reserva. Los valores más altos requieren un mayor colchón antes de que se acepten nuevas operaciones. |
| `OrderReserve` | `0.04` | Fracción de capital arriesgada por operación. Se utiliza para calcular el tamaño del contrato a partir de la distancia de parada. |
| `CandleType` | `1 minute` período de tiempo | Serie de velas utilizada para observar los últimos precios para validación y registro. |

## Notas y limitaciones
- Solo se admite una posición a la vez, lo que coincide con el diseño original del asesor experto.
- Los comandos que violan la distancia mínima de precio, la reserva de capital o las restricciones de volumen se ignoran y se registra una advertencia en el registro de estrategia.
- Las órdenes de protección se reemplazan con cada modificación o nuevo llenado para mantener los volúmenes sincronizados con el tamaño real de la posición.
- La estrategia se basa en datos de mercado precisos para `PriceStep`/`StepPrice`. Los instrumentos que no proporcionen estos campos no se pueden comercializar de forma segura en este puerto.
