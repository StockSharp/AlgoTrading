# GBP Estrategia de órdenes pendientes de 9 a.m.
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión de StockSharp del experto original MetaTrader 4 ubicado en `MQL/7687/Gbp9am.mq4`. Recrea la rutina de ruptura de las 9 a.m. de Londres que arma dos órdenes pendientes alrededor del precio actual y mantiene como máximo una operación activa durante la sesión.

## idea comercial

1. En la *hora de visualización* y el *minuto* configurados, la estrategia toma el último cierre de vela como una instantánea del precio.
2. Se coloca un stop de compra por encima del precio instantáneo y un stop de venta por debajo de él. Ambas órdenes comparten el mismo volumen y tienen niveles de stop-loss individuales junto con una distancia de toma de ganancias compartida.
3. Cuando se completa una de las órdenes, la otra se cancela inmediatamente, de modo que solo hay una posición activa.
4. La posición abierta se gestiona con niveles sintéticos de stop-loss y take-profit que se verifican en cada vela completa.
5. Se puede habilitar una hora de cierre diaria para reducir cualquier exposición restante y eliminar las órdenes pendientes después de la sesión de Londres.
6. Si ambas órdenes pendientes se eliminan sin una operación, o el tiempo del mercado se aleja de la hora de observación, la estrategia se rearma al día siguiente exactamente como la versión MetaTrader.

Las compensaciones de pips se aproximan utilizando el paso del precio del instrumento. Si el corredor proporciona pips fraccionarios (3 o 5 dígitos decimales), la lógica se escala automáticamente a incrementos típicos de 0,1 pips.

## Referencia de parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de órdenes (lotes) compartido por ambas órdenes pendientes. |
| `LookHour` | Hora de cambio que representa las 9 a.m. hora de Londres. |
| `LookMinute` | Minuto dentro de la hora de visualización en la que se toma la instantánea. |
| `CloseHour` | Hora en la que se cierran forzosamente todas las posiciones y órdenes pendientes. |
| `UseCloseHour` | Activa o desactiva el procedimiento de cierre diario. |
| `TakeProfitPips` | Distancia objetivo en pips, aplicada simétricamente en ambas direcciones. |
| `BuyDistancePips` | Compensación en pips entre el precio instantáneo y la entrada del stop de compra. |
| `SellDistancePips` | Compensación en pips entre el precio instantáneo y la entrada del stop de venta. |
| `BuyStopLossPips` | Distancia de stop-loss en pips para operaciones largas. |
| `SellStopLossPips` | Distancia de stop-loss en pips para operaciones cortas. |
| `CandleType` | Serie de velas utilizadas para la gestión de tiempos y paradas (predeterminado 1 minuto). |

## Notas de comportamiento

- La estrategia ignora las velas inacabadas para evitar múltiples activadores dentro de la misma barra.
- Los precios de los pedidos se redondean al tick válido más cercano utilizando el paso del precio del valor.
- La puerta de rearme refleja la bandera `clear_to_send` del experto MQL: una vez que se realiza el straddle diario, no se envían nuevas órdenes hasta que ambas órdenes pendientes desaparecen mientras el mercado está fuera de la hora de observación o el reloj llega a la hora anterior a la siguiente señal.
- Cuando `UseCloseHour` está habilitado, la estrategia sale de cualquier operación abierta con una orden de mercado y borra las órdenes pendientes una vez que se alcanza la hora de cierre.
- Los cálculos de pips se basan en velas históricas, por lo tanto, las distancias exactas de parada/objetivo pueden diferir ligeramente del entorno MetaTrader basado en ticks, especialmente en símbolos con grandes diferenciales.

## Gestión de riesgos

La conversión mantiene las paradas y objetivos estáticos originales. No existe un trailing stop ni una lógica de escalado. La protección de posición está activada en `OnStarted` para que desconexiones inesperadas no dejen la cuenta desprotegida.

## Uso

1. Configure los valores `Volume`, `LookHour` y `LookMinute` para que coincidan con la zona horaria de intercambio de su fuente de datos.
2. Ajuste los parámetros de distancia para reflejar la estructura de diferenciales de su corredor.
3. Adjunte la estrategia a un símbolo GBPUSD (u otro par de divisas de su elección) e iníciela antes de la sesión de Londres.
4. Supervise las operaciones resultantes en el gráfico StockSharp que se dibuja automáticamente después del inicio.

La implementación sigue las pautas de `AGENTS.md`: utiliza la suscripción de vela de alto nivel API, emplea parámetros de estrategia con metadatos de la interfaz de usuario y evita el sondeo del historial de bajo nivel.
