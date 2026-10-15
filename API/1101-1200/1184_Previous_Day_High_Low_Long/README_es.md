# Previous Day High Low Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Solo largo: cruce ascendente del high o low observado del día UTC anterior, con ADX nativo creciente dentro de una sesión UTC. Sale por objetivo desde ejecución real, stop dinámico o fin de sesión.

## Niveles y entrada

Solo velas terminadas, cinco minutos por defecto. Agrupa high/low por fecha UTC de apertura y congela máximo/mínimo observados al comenzar la siguiente fecha. El día anterior debe empezar con una vela observada a medianoche y las fechas ser consecutivas; no se inventan niveles para un inicio parcial o fecha ausente. Velas actuales no amplían niveles anteriores.

Cruce high: `previousClose <= previousDayHigh && close > previousDayHigh`; low usa el nivel bajo anterior, también hacia arriba. ADX nativo `AdxLength=11` debe estar formado y superar estrictamente su valor formado anterior, sin umbral adicional o filtro direccional DI.

Solo desde cero, compra a mercado `Volume` dentro de `[SessionStart,SessionEnd)` por hora UTC de envío, predeterminado09:30–16:00. Cada vela actualiza cierre, día y ADX: cruces rechazados no se aplazan. Un cruce nuevo permite reentrada tras cerrar, sin límite diario ni espera de seis horas. No abre cortos.

## Protección y cierre

`MaxProfit=150` y `MaxStopLoss=15` son distancias absolutas de precio, no porcentajes ni ticks del instrumento. La compra real inicia entryPrice/highWater; compras parciales adicionales actualizan el precio medio ponderado por volumen, reducciones conservan la base y posición cero la reinicia.

Cada bid positivo ejecutable o close terminado eleva highWater al máximo del anterior y precio actual. Vende toda la posición larga real si precio≤`highWater-MaxStopLoss` o≥`entryPrice+MaxProfit`. Prioridad: fin de sesión, stop dinámico, objetivo. No convierte high de vela en cotización ejecutable.

Temporizador de tiempo de mercado de un minuto, bids y velas cierran en/después de `SessionEnd` o en la siguiente fecha UTC. Horas ordenadas dentro de un día;00:00–24:00 termina en la siguiente medianoche. Una orden pendiente impide duplicados; historia día/ADX sigue avanzando. El callback que cierra retorna sin entrar otra vez.

Monitoreo local, sin stop nativo de bolsa. Protección intravela por cotizaciones requiere Level1 bid; sin él quedan cierres. Ejecución puede diferir del umbral.

| Parámetro | Valor |
|---|---:|
| `MaxProfit` / `MaxStopLoss` | 150 / 15, distancias positivas absolutas |
| `AdxLength` | 11, positivo |
| `SessionStart` / `SessionEnd` | 09:30 / 16:00 UTC, inicio incluido y fin excluido |
| `CandleType` | 5 minutos |

Pruebas C#/Python agrupan días y calculan independientemente Wilder ATR/DI/DX/ADX en el archivo BTC/TON. Verifican cada entrada/salida real, ambos niveles, rechazos ADX, historia parcial inicial, ejecuciones, stops/objetivos por cotizaciones y cierre obligatorio. No demuestran rentabilidad.
