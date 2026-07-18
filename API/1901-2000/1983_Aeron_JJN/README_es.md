# Estrategia de Ruptura Aeron JJN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del asesor experto original Aeron JJN. Observa una vela de reversión fuerte y coloca una orden stop en la apertura de la última vela opuesta. El stop y el objetivo se establecen a un ATR de distancia, y un trailing stop opcional protege las posiciones abiertas.

Las pruebas muestran que la idea funciona mejor en los principales pares Forex usando velas de 1 minuto.

Se coloca una orden buy stop cuando la vela anterior es bajista con cuerpo mayor que **DojiDiff1** y la vela actual es alcista pero aún por debajo de la última apertura bajista significativa. Una orden sell stop usa las condiciones espejo. Las órdenes pendientes se eliminan tras **ResetTime** minutos si permanecen sin ejecutar.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Vela anterior bajista, vela actual alcista y cierra por debajo de la última apertura bajista.
  - **Corto**: Vela anterior alcista, vela actual bajista y cierra por encima de la última apertura alcista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Stop-loss y take-profit basados en ATR.
  - Trailing stop opcional en pips.
- **Stops**: Sí, stop inicial y objetivo basados en ATR más trailing opcional.
- **Filtros**:
  - Las órdenes pendientes expiran tras el tiempo configurado.

## Parámetros

- `AtrPeriod` – período de cálculo del ATR.
- `DojiDiff1` – umbral de tamaño de cuerpo para la vela anterior.
- `DojiDiff2` – umbral de tamaño de cuerpo al buscar la última vela opuesta.
- `TrailSl` – activar trailing stop.
- `TrailPips` – distancia de trailing en pips.
- `ResetTime` – minutos antes de cancelar órdenes stop.
- `CandleType` – marco temporal de trabajo.
