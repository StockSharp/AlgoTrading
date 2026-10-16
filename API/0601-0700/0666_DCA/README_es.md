# Estrategia DCA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia compra una cantidad fija de la moneda cotizada a intervalos regulares, ya sea en un día de la semana elegido o después de un número fijo de velas. Opcionalmente cierra todas las posiciones tras una fecha de fin especificada.

### Planificación y volumen ejecutados

Valores predeterminados: `CandleType=5m`, `CapitalToInvest=200` **unidades de moneda cotizada**, `StartDate=DateTime.MinValue`, `EndDate=DateTime.MaxValue`, `CloseAllOnLastCandle=false`, `UseDayOfWeek=false`, `DayOfWeek=Wednesday`, `EveryXCandles=72`.

Solo opera con velas terminadas cuya fecha cumple `StartDate <= fecha < EndDate`. El modo de intervalo compra **cada 72 velas elegibles**; el modo semanal compra **la primera vela elegible de cada miércoles**, una vez por fecha. Calcula `200 / Close` y redondea **hacia abajo** al `Security.VolumeStep`, respetando los límites del instrumento. Solo crea **compras de mercado** acumulativas, sin cruces EMA ni cortos. Al llegar a EndDate deja de comprar; si `CloseAllOnLastCandle=true`, cierra **todo el saldo real** una vez, en la primera vela elegible. No hay stop independiente y puede haber deslizamiento. Los valores numéricos predeterminados se definieron en esta implementación porque el README original no los detallaba.



## Parámetros
- Tipo de vela
- Capital a invertir por operación
- Fecha de inicio
- Fecha de fin
- Cerrar todo en la última vela
- Usar modo de día de la semana
- Día de la semana
- Cada X velas
