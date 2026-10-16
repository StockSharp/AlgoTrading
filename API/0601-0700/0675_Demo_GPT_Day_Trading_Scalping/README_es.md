# Demo GPT - Estrategia de Scalping de Day Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo tras dos velas verdes consecutivas cuando la segunda abre por encima del cierre de la primera y el volumen supera su media de 20 barras. Cierra la posición en dos velas rojas consecutivas donde la segunda abre por debajo del cierre de la primera bajo la misma condición de volumen. Calcula EMA(20), VWAP y niveles Camarilla como referencia.

### Reglas ejecutadas y valores predeterminados

Predeterminados: `EmaLength=20`, `VolumeLookback=20`, `CandleType=1 minuto`. Tras al menos **20 velas terminadas anteriores**, `volumeHigh = TotalVolume actual > media de los 20 volúmenes anteriores`, excluyendo la vela actual de su media. Solo desde plano, **compra largo** cuando las dos velas son verdes (`Close > Open`), la actual tiene **`Open > Close previo`** y se confirma el volumen. Cierra **toda la posición larga restante** cuando ambas son rojas (`Close < Open`), `Open actual < Close previo` y hay volumen confirmado. Sin hueco estricto de apertura o filtro de volumen **no hay señal**, nunca abre cortos. Solo para referencia: EMA20 de cierres; VWAP diario `sum(precio típico × volumen)/sum(volumen)`, reiniciado cada día; Camarilla del día anterior `H3/L3 = cierre anterior ± 1.1 × (máximo − mínimo) / 4`. No filtran operaciones. Sin stop-loss adicional.

