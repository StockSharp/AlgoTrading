# Estrategia Martin 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Conversión del asesor experto MetaTrader 5 «Martin 1» a la API de estrategia de alto nivel de StockSharp. El algoritmo mantiene continuamente exposición y usa pasos de martingala de estilo cobertura para recuperar drawdowns mientras realiza pirámide en tendencias rentables.

## Lógica de trading

1. **Exposición inicial** – cuando la estrategia está plana, abre inmediatamente una posición en la dirección definida por `StartDirection`, independientemente del filtro de tiempo. El tamaño base de la orden se toma de `InitialVolume` después de redondear al paso de volumen del instrumento.
2. **Filtro de ventana de tiempo** – cuando `UseTradingHours` está habilitado, solo las acciones de escala (pirámide o cobertura) se permiten entre `StartHour` y `EndHour` inclusive, usando el tiempo del exchange contenido en las marcas de tiempo de las velas.
3. **Pirámide de ganadores** – cada posición abierta se evalúa en cada vela terminada. Si la ganancia flotante de una posición larga supera la distancia de take-profit y permanece positiva, se envía una orden larga adicional con el volumen actual. Las posiciones cortas se comportan simétricamente. El precio de la nueva orden se asume como el cierre de la vela actual.
4. **Martingala de cobertura** – cuando la dirección inicial es larga y una posición larga pierde más de `(StopLossPips × tamaño de pip × (índice de multiplicación + 1))`, la estrategia abre una orden corta opuesta. Antes de colocar la cobertura, el volumen se multiplica por `LotMultiplier`, se redondea al paso permitido, y el contador de multiplicación aumenta. La misma lógica se aplica a la inversa para la dirección inicial corta. La cobertura se detiene una vez que se alcanzan los pasos de `MaxMultiplications`.
5. **Objetivo de ganancia global** – la ganancia no realizada en todas las posiciones restantes (convertida a dinero usando `PriceStep`/`StepPrice`) se suma. Si supera `MinProfit`, cada posición abierta se cierra emitiendo una orden de mercado en la dirección opuesta, y el estado de la martingala se reinicia.

## Gestión de riesgos y dinero

- El tamaño del pip se calcula a partir del paso de precio del instrumento. Las cotizaciones de tres y cinco dígitos multiplican el paso por diez para emular el ajuste de pip original de MetaTrader.
- Los volúmenes se redondean hacia abajo al `VolumeStep` más cercano. Si el valor redondeado cae por debajo del paso, la orden se omite.
- El contador de martingala y el volumen actual se reinician siempre que el libro quede plano, ya sea de forma natural o después de alcanzar el objetivo de ganancia global.
- La estimación de ganancia ignora comisiones y swaps, reflejando el comportamiento del script original que dependía puramente del PnL flotante.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Tipo de vela que impulsa todos los cálculos. | Marco temporal de 1 minuto |
| `UseTradingHours` | Habilita o deshabilita el filtro de ventana de tiempo. | `true` |
| `StartHour` | Hora inclusiva cuando el filtro de tiempo permite nuevas acciones de escala. | 2 |
| `EndHour` | Hora inclusiva cuando las acciones de escala se detienen. | 21 |
| `LotMultiplier` | Factor aplicado al volumen actual antes de abrir una cobertura. | 1.6 |
| `MaxMultiplications` | Número máximo de pasos de cobertura que pueden activarse. | 5 |
| `StartDirection` | Dirección de la primera orden después de que la estrategia quede plana. | Buy |
| `MinProfit` | Ganancia flotante (en dinero) que fuerza el cierre de todas las posiciones. | 1.5 |
| `InitialVolume` | Volumen base para la primera orden y estado de reinicio. | 0.1 |
| `StopLossPips` | Distancia en pips que activa la siguiente cobertura de martingala. | 40 |
| `TakeProfitPips` | Distancia en pips que activa una entrada de pirámide. | 100 |

## Notas de implementación

- `ProcessCandle` usa el pipeline de suscripción de velas de alto nivel (`SubscribeCandles().Bind(...)`) y opera estrictamente en velas terminadas, cumpliendo con las pautas de la plataforma.
- La exposición cubierta se rastrea internamente con dos listas FIFO para que la estrategia pueda emular el comportamiento de cobertura de MetaTrader incluso en cuentas de compensación.
- La conversión de ganancia depende de `Security.PriceStep` y `Security.StepPrice`. Cuando esos valores no están disponibles, la diferencia en precio se multiplica directamente por el volumen negociado como alternativa.
- La estrategia mantiene el trading continuamente; deshabilitar el filtro de tiempo o establecer horas amplias hará que el algoritmo se comporte como el asesor experto original siempre activo.
