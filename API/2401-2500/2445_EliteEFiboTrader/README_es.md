# Estrategia Elite eFibo Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Elite eFibo Trader** es una conversión del asesor experto MQL5 "Elite eFibo Trader". Implementa una cuadrícula de promediación basada en Fibonacci que abre una posición inicial de mercado y añade órdenes stop adicionales a distancias fijas. La estrategia opera con datos de tick y gestiona automáticamente los stops de seguimiento a medida que la cuadrícula se expande.

## Cómo funciona
1. Cuando no hay posiciones ni órdenes pendientes activas y el trading está permitido, la estrategia inicia un nuevo ciclo en la dirección seleccionada (compra o venta).
2. La primera orden se envía a mercado usando el volumen configurado para `LotsLevel1`. Se colocan trece órdenes stop adicionales en múltiplos de `LevelDistance` desde el precio actual. Sus volúmenes siguen la secuencia de Fibonacci definida por `LotsLevel2` … `LotsLevel14`.
3. Cada orden ejecutada establece un nivel de stop individual a `StopLossPoints` del precio de entrada. El más alto (para posiciones largas) o más bajo (para posiciones cortas) de estos stops se convierte en el nivel de seguimiento activo para todas las posiciones abiertas.
4. Si el precio alcanza el nivel de seguimiento, la posición completa se cierra y todas las órdenes pendientes restantes se cancelan.
5. El beneficio no realizado se monitorea en la divisa de la cuenta. Una vez que alcanza `MoneyTakeProfit`, la cuadrícula se cierra. Dependiendo de `TradeAgainAfterProfit`, la estrategia se reinicia automáticamente o espera a ser reactivada manualmente.

La estrategia requiere datos de mercado a nivel de tick a través de `SubscribeTrades()` y espera que solo una dirección (`OpenBuy` xor `OpenSell`) esté habilitada a la vez.

## Parámetros
- `OpenBuy` – habilita la versión solo largas de la cuadrícula.
- `OpenSell` – habilita la versión solo cortas de la cuadrícula.
- `TradeAgainAfterProfit` – inicia automáticamente un nuevo ciclo después de tomar beneficios.
- `LevelDistance` – espaciado entre órdenes pendientes, medido en pasos de precio del instrumento.
- `StopLossPoints` – distancia del stop-loss desde cada entrada, medida en pasos de precio.
- `MoneyTakeProfit` – objetivo de beneficio no realizado expresado en divisa de la cuenta.
- `LotsLevel1` … `LotsLevel14` – volúmenes individuales para cada nivel de la cuadrícula. Los valores predeterminados siguen la secuencia de Fibonacci (1, 1, 2, 3, 5, …, 377).

## Detalles de la lógica de trading
- Los desplazamientos de precio se calculan con el `PriceStep` del instrumento; si es cero, la estrategia no colocará órdenes.
- Solo un ciclo de trading está activo a la vez. Todas las órdenes pendientes se crean al inicio del ciclo y permanecen hasta ejecutarse o cancelarse explícitamente.
- Los stops de seguimiento se recalculan cada vez que se llena un nuevo nivel de la cuadrícula o se cierran porciones de la posición. Esto asegura que todas las órdenes compartan el mejor nivel de protección disponible.
- El control de beneficios se basa en el PnL flotante derivado de `Position`, `PositionPrice`, `PriceStep` y `StepPrice`.
- Cuando `TradeAgainAfterProfit` está deshabilitado, la estrategia permanece inactiva después de alcanzar el objetivo monetario hasta que el parámetro se reactive manualmente.

## Notas de uso
- Configure la dirección correcta antes de iniciar (largo o corto). Habilitar ambas direcciones simultáneamente impide el lanzamiento de la cuadrícula.
- Ajuste las distancias entre niveles y los volúmenes según la volatilidad del instrumento y el tamaño del contrato. Los grandes volúmenes de Fibonacci crean un escalado agresivo y deben probarse cuidadosamente con datos históricos.
- Asegúrese de que la cuenta de trading y el bróker soporten órdenes stop en los niveles de precio calculados; de lo contrario, las órdenes pueden ser rechazadas.
