# Estrategia de Bloqueo (Lock)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Bloqueo recrea el asesor experto de "bloqueo" clásico de MetaTrader: siempre mantiene un par cubierto de posiciones largas y cortas y sigue reciclándolos hasta que se satisface una condición de bloqueo de beneficios. El algoritmo está diseñado para instrumentos con tamaños de tick pequeños donde se puede aplicar un take-profit fijo basado en pips.

## Flujo de trabajo de trading

1. **Cobertura inicial** – tan pronto como los datos de mercado estén disponibles, la estrategia abre una posición larga y corta con el mismo volumen. Si ambas órdenes se ejecutan, el volumen usado para la próxima cobertura se multiplica por el factor `LotExponential`.
2. **Gestión del take-profit** – cada pierna almacena su precio de entrada. Cuando el cierre de la vela se mueve por `TakeProfitPips` (convertido a ticks del instrumento) desde la entrada, la pierna se cierra con una orden de mercado. El lado opuesto permanece abierto, preservando el comportamiento de cobertura de la versión MQL.
3. **Re-cobertura** – siempre que el número total de piernas activas sea uno o cero, la estrategia abre inmediatamente un nuevo par. Si no hay piernas abiertas, el volumen base se restablece a `LotSize` antes de crear el nuevo par.
4. **Control de volumen** – el método helper `AdjustVolume` aplica las restricciones de la bolsa: redondea los volúmenes al `VolumeStep` de la seguridad, los limita por `MinVolume` y `MaxVolume`, y cancela el escalado si el valor ajustado llega a cero.

## Condición de bloqueo de beneficios

La lógica MQL original monitorea el balance de la cuenta versus el capital: cuando el balance supera el capital en `ExcessBalanceOverEquity` y el capital está al menos `MinProfit` por encima del último balance bloqueado, cada pierna se cierra. La implementación en C# refleja este comportamiento rastreando el capital observado cuando la estrategia está plana y tratándolo como el balance en ejecución. Una vez que se activa la condición, todas las piernas se liquidan y el balance de referencia se actualiza antes de que el ciclo se reinicie con `LotSize`.

## Parámetros

- `LotSize` – volumen base para el primer ciclo de cobertura (predeterminado: `0.1m`).
- `TakeProfitPips` – distancia en pips para cerrar cada pierna (predeterminado: `100`). Un valor de `0` desactiva la salida automática.
- `LotExponential` – multiplicador aplicado al volumen actual después de que ambas piernas abren con éxito (predeterminado: `2m`).
- `ExcessBalanceOverEquity` – brecha tolerada entre balance y capital antes de asegurar beneficios (predeterminado: `3000m`).
- `MinProfit` – crecimiento adicional del capital que debe lograrse antes de cerrar todas las piernas (predeterminado: `500m`).
- `CandleType` – marco temporal que impulsa la lógica de la estrategia (predeterminado: marco temporal de 1 minuto).

## Notas de implementación

- El tamaño del pip se recalcula a partir de `Security.PriceStep` y `Security.Decimals`, por lo que la estrategia se adapta a símbolos FX de 3/5 dígitos así como a futuros o acciones estándar.
- Las órdenes se colocan usando ejecución de mercado, reflejando el comportamiento del experto MQL que envía órdenes de mercado con take-profits del lado del broker.
- La estrategia mantiene un historial completo de piernas cubiertas, lo que permite múltiples posiciones apiladas en cada lado, exactamente como el script fuente lo permitía.
