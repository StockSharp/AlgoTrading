# Estrategia E-Friday
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convierte el asesor experto MetaTrader original `E-Friday.mq5` a la API de alto nivel de StockSharp.
- Opera solo cuando el marco temporal del gráfico es **H1 o inferior**; de lo contrario, la estrategia registra una advertencia y permanece plana.
- Entra en posiciones de forma contraria: una vela bajista abre una posición larga y una vela alcista abre una posición corta.
- Deshabilita completamente el trading todos los viernes para coincidir con el comportamiento original de protección de fin de semana.
- Restringe el trading a una ventana de tiempo configurable y puede forzar el cierre de posiciones después del fin de sesión.

## Lógica de trading
1. En cada vela finalizada, la estrategia comprueba el tiempo de cambio actual:
   - si el día es viernes, omite cualquier acción;
   - si la hora es anterior a la hora de inicio configurada, espera;
   - si la ventana de cierre está habilitada y la hora supera la hora de fin, aplana todas las posiciones y omite nuevas entradas.
2. Cuando el trading está permitido, la última vela completada impulsa la señal:
   - si `Open > Close` (cuerpo bajista) la estrategia prepara una entrada larga;
   - si `Open < Close` (cuerpo alcista) la estrategia prepara una entrada corta;
   - precios de apertura y cierre iguales cancelan cualquier acción pendiente.
3. Antes de entrar en una nueva posición, la exposición actual se aplana, por lo que nunca hay más de una posición neta.

## Gestión de posición
- **Tamaño del lote** – tomado de `TradeVolume` y enviado a órdenes `BuyMarket` / `SellMarket`.
- **Stop loss y take profit** – medidos en pips. Los pips se calculan desde `Security.PriceStep` y se multiplican por `10` cuando el instrumento tiene 3 o 5 decimales, exactamente como en la versión MQL.
- **Trailing stop** – se activa una vez que el precio se mueve `TrailingStopPips + TrailingStepPips` a favor de la posición. El stop se ajusta a `precio actual - trailing stop` (largo) o `precio actual + trailing stop` (corto).
- Las salidas se evalúan usando los extremos de la vela:
  - una posición larga cierra si el mínimo de la vela toca el stop o el máximo alcanza el take profit;
  - una posición corta cierra si el máximo de la vela toca el stop o el mínimo alcanza el take profit.
- Después de la hora de fin de sesión (cuando `UseCloseHour = true`) toda posición abierta se cierra mediante órdenes de mercado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de las velas procesadas. Debe definir un `TimeSpan` positivo y no debe exceder una hora. |
| `TradeVolume` | Volumen de orden en lotes. Debe ser positivo. |
| `StopLossPips` | Distancia desde el precio de entrada al stop protector, expresada en pips. Establezca en cero para deshabilitar el stop inicial. |
| `TakeProfitPips` | Distancia desde el precio de entrada al objetivo de beneficio en pips. Establezca en cero para deshabilitar el objetivo. |
| `TrailingStopPips` | Distancia del trailing stop en pips. Trabaja junto con `TrailingStepPips`. |
| `TrailingStepPips` | Progreso adicional mínimo (en pips) requerido antes de que el trailing stop se ajuste. Debe ser positivo cuando el trailing stop está habilitado. |
| `StartHour` | Hora (tiempo de cambio) cuando la estrategia puede comenzar a abrir posiciones. |
| `UseCloseHour` | Habilita o deshabilita el cierre forzado después de la hora de fin. |
| `EndHour` | Hora (tiempo de cambio) después de la cual la estrategia deja de operar y cierra posiciones existentes. |

## Notas de implementación
- Usa `SubscribeCandles` y la API de alto nivel `Bind` para que los indicadores puedan añadirse más tarde si es necesario.
- Valida la configuración de trailing al inicio: cuando se solicita un trailing stop, el paso de trailing debe ser estrictamente positivo.
- La conversión de pips sigue la lógica original del EA (`PriceStep * 10` para símbolos de 3/5 dígitos) para mantener consistentes las distancias del stop-loss.
- La versión de StockSharp evalúa stops y objetivos una vez por vela finalizada. El EA original funcionaba en cada tick, por lo tanto el port de StockSharp puede salir algunos ticks después, pero la lógica sigue siendo equivalente.
- La estrategia llama explícitamente a `CloseActivePosition` cuando termina la ventana de sesión. El script MQL contenía la misma idea pero retornaba antes de llegar a la rutina de cierre; la versión en C# implementa el comportamiento previsto.
- Los registros informativos (`AddInfoLog` / `AddWarningLog`) se usan para mostrar los períodos de trading omitidos en la interfaz de usuario.
