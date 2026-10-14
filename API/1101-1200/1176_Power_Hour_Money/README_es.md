# Estrategia Power Hour Money
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera durante sesiones seleccionadas de Nueva York y abre posiciones cuando todos los marcos temporales principales coinciden.
Se abre una posición larga cuando las velas de mes, semana, día y hora cierran por encima de su apertura.
Se abre una posición corta cuando todas cierran por debajo.
Los trailing stops opcionales protegen las ganancias y las posiciones pueden cerrarse a las 16:45.

## Detalles

- **Entrada**: largo cuando todos los marcos temporales son verdes, corto cuando todos son rojos.
- **Filtro de sesión**: NY 9:30-11:30, extendida 8:00-16:00 o todas las sesiones.
- **Trailing stop**: porcentual para el lado largo y corto.
- **Fin del día**: cierre opcional de todas las posiciones a las 16:45.

## Cálculo y ejecución

C# y Python usan el mismo modelo causal: tras cada vela base terminada, su cierre es el cierre conocido de las velas en formación de mes, semana, día y hora. Se compara estrictamente con cada apertura. Colores mixtos o igualdad mantienen la posición. Nunca se consultan los cierres finales futuros de las velas superiores.

El mes, la semana que empieza el lunes y el día siguen el calendario de Nueva York, incluido el horario de verano. Las horas se identifican por UTC para distinguir la hora repetida en otoño. Tras observar un cambio de período, la primera vela base disponible proporciona la apertura. Un período ya iniciado al comenzar el historial queda desconocido hasta su siguiente límite; el primer precio observado no sustituye la apertura ausente. Se requieren las cuatro aperturas antes de entrar.

`CandleType` predeterminado: cinco minutos. `SessionStart=09:30` y `SessionEnd=11:30` usan tiempo de mercado de Nueva York, inicio incluido y fin excluido. Configure `08:00`–`16:00` para la sesión extendida o `IsAllSessions=true`. `IsAllSessions`, `IsTrailingEnabled` e `IsEndOfDayCloseEnabled` son `false` por defecto; `LongTrailingPercent=2`, `ShortTrailingPercent=2`, `EndOfDayTime=16:45`.

La alineación entra con `Volume` desde posición cero o invierte completamente con `Volume + abs(Position)`. Una orden pendiente bloquea órdenes adicionales; no hay espera obligatoria. El trailing opcional parte de la ejecución real: cierra largos cuando el bid actual alcanza `máximo bid observado * (1 - LongTrailingPercent / 100)` y cortos cuando el ask alcanza `mínimo ask observado * (1 + ShortTrailingPercent / 100)`. Los precios ejecutados también actualizan el extremo favorable. Ambos porcentajes deben estar estrictamente entre 0 y 100. Una inversión reinicia el extremo.

El cierre diario opcional envía una orden de mercado por toda la posición en el primer evento de tiempo de mercado a partir del límite y bloquea entradas hasta medianoche en Nueva York. Las cotizaciones y un temporizador de un minuto verifican el límite independientemente de las sesiones y la formación de velas. Ambas salidas requieren eventos de mercado y no garantizan un precio exacto ante saltos.
