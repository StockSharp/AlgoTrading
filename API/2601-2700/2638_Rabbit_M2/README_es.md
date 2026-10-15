# Estrategia Rabbit M2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Rabbit M2 es una estrategia de seguimiento de tendencia que combina osciladores de momentum, rupturas de Donchian y dimensionamiento adaptativo de posiciones. La versión original de MetaTrader 5 de Peter Byrom alterna entre regímenes de compra y venta basados en medias móviles exponenciales (EMAs) de un marco temporal superior. Dentro del régimen activo, la estrategia espera oscilaciones de Williams %R confirmadas por el Commodity Channel Index (CCI) antes de abrir una operación. Las posiciones están protegidas con objetivos fijos de stop loss y take profit y se cierran forzosamente cuando el precio viola el límite opuesto del canal Donchian. Tras cada salida rentable por encima de un objetivo de beneficio configurable, la estrategia aumenta el tamaño base de la orden y duplica el umbral de beneficio, imitando la lógica de escalado del asesor experto MQL.

## Indicadores y datos de mercado
- **EMA rápida (40) y EMA lenta (80)** calculadas en velas de 1 hora, dirigen la dirección de trading y cierran operaciones en cambios de régimen.
- **Commodity Channel Index (14)** medido en el marco temporal principal, confirma el momentum de sobrecompra o sobreventa.
- **Williams %R (50)** en el marco temporal principal proporciona el disparador cuando cruza los niveles -20/-80.
- **Canal Donchian (100)** derivado del marco temporal principal, define salidas por ruptura cuando el precio viola el máximo o mínimo de las 100 barras anteriores.
- **Stop loss y take profit fijos** se establecen a 50 pips de distancia del precio de entrada (el tamaño del pip se adapta a instrumentos de 3/5 dígitos).

Se requieren dos flujos de datos: el marco temporal principal configurable para los cálculos de CCI/Williams %R/Donchian y un flujo dedicado de 1 hora para el filtro de tendencia EMA.

## Reglas de trading
### Control de régimen
1. Cuando la EMA de 40 períodos en el feed H1 cae por debajo de la EMA de 80 períodos, todas las posiciones largas se cierran y solo se permiten configuraciones cortas.
2. Cuando la EMA de 40 períodos sube por encima de la EMA de 80 períodos, todas las posiciones cortas se cierran y solo se permiten configuraciones largas.

### Criterios de entrada
- **Entrada corta**
  - Williams %R cae por debajo de -20 mientras su valor anterior estaba entre -20 y 0.
  - CCI está por encima del nivel de venta (por defecto 101).
  - El régimen corto está activo y el volumen actual de la posición neta está por debajo del límite `MaxOpenPositions`.
- **Entrada larga**
  - Williams %R sube por encima de -80 mientras su valor anterior estaba entre -100 y -80.
  - CCI está por debajo del nivel de compra (por defecto 99).
  - El régimen largo está activo y el volumen actual de la posición neta está por debajo del límite `MaxOpenPositions`.

En cada entrada, la estrategia cierra la exposición contraria (si la hay) y abre la nueva posición con el volumen base actual.

### Criterios de salida
1. Stop loss y take profit se evalúan en cada vela finalizada: los largos salen si el mínimo cruza el stop o el máximo alcanza el objetivo; los cortos se comportan inversamente.
2. Independientemente del stop/objetivo, los cortos salen cuando el precio cierra por encima del máximo de las 100 barras anteriores y los largos cuando el precio cierra por debajo del mínimo de las 100 barras anteriores.
3. Un cambio de régimen (cruce de la EMA rápida sobre la EMA lenta) liquida inmediatamente la exposición existente.

### Lógica de dimensionamiento de posición
- El volumen base de la orden comienza desde `InitialVolume` (por defecto 0.01) y sigue los límites del exchange (paso/mín/máx).
- Tras cada beneficio realizado mayor que `BigWinTarget`, el volumen base aumenta en `VolumeStep` y el umbral se duplica, preservando el patrón de crecimiento en cascada del asesor experto original.
- El parámetro `MaxOpenPositions` limita la exposición neta. En el port StockSharp las posiciones se compensan, por lo que alcanzar el límite significa que no se añade volumen adicional hasta que la exposición disminuya.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `CciSellLevel` | 101 | Valor mínimo de CCI requerido para confirmar una configuración corta. |
| `CciBuyLevel` | 99 | Valor máximo de CCI requerido para confirmar una configuración larga. |
| `CciPeriod` | 14 | Período del Commodity Channel Index en el marco temporal principal. |
| `DonchianPeriod` | 100 | Ventana de retroceso para el canal Donchian usado en la lógica de salida. |
| `MaxOpenPositions` | 1 | Máximos múltiplos de posición neta permitidos del volumen base. |
| `BigWinTarget` | 1.50 | Beneficio (en moneda de la cuenta) necesario para escalar el volumen. |
| `VolumeStep` | 0.01 | Incremento añadido al volumen base tras una ganancia calificada. |
| `WprPeriod` | 50 | Longitud del oscilador Williams %R. |
| `FastEmaPeriod` | 40 | Período de la EMA rápida en el feed de tendencia de 1 hora. |
| `SlowEmaPeriod` | 80 | Período de la EMA lenta en el feed de tendencia de 1 hora. |
| `TakeProfitPips` | 50 | Distancia del take profit en pips. |
| `StopLossPips` | 50 | Distancia del stop loss en pips. |
| `InitialVolume` | 0.01 | Volumen de orden inicial antes de las reglas de escalado. |
| `CandleType` | Velas de 15 minutos | Marco temporal principal para los cálculos de CCI/Williams %R/Donchian. |

## Notas de implementación
- El port StockSharp emula el stop loss y take profit de MT5 monitoreando máximos/mínimos de velas en lugar de colocar órdenes adjuntas al broker.
- Los pasos de precio y los cálculos de pips se ajustan automáticamente para instrumentos de 3 o 5 decimales multiplicando el tamaño del tick reportado por 10.
- La estrategia se basa en actualizaciones de PnL realizado para detectar «grandes ganancias»; asegúrese de que las operaciones se reporten de vuelta a la estrategia para que funcione el escalado.
