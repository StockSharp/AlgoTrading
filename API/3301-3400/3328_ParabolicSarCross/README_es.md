# Estrategia Parabolic SAR Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port StockSharp del asesor experto de MetaTrader "PSAR Trader EA". Observa cómo el precio interactúa con el indicador Parabolic SAR y reacciona solo cuando el campo de puntos cambia de un lado del cuerpo de la vela al otro. La conversión conserva la lógica original de gestión monetaria: la estrategia puede operar con lote fijo o ajustar dinámicamente el volumen según el balance, aplica stop-loss y take-profit fijos, y activa trailing stop cuando una operación acumula beneficio suficiente.

## Lógica de estrategia
- Construir un indicador Parabolic SAR con aceleración y valores máximos definidos por el usuario sobre la serie de velas seleccionada (30 minutos por defecto).
- Detectar un **giro alcista** cuando el punto SAR se mueve de encima del cuerpo de la vela a debajo. Si no hay posición, enviar una compra a mercado. Si existe una posición corta, cerrarla primero y esperar la siguiente señal para reentrar largo.
- Detectar un **giro bajista** cuando el punto SAR se mueve de debajo del cuerpo de la vela a encima. Si está plano, abrir corto. Si hay una posición larga activa, cerrarla y diferir la entrada hasta la siguiente señal.
- Monitorizar operaciones abiertas en cada vela terminada y ejecutar salidas cuando cualquier nivel protector (stop-loss, take-profit o trailing stop) sea alcanzado por el máximo/mínimo de la vela actual.

## Gestión de riesgo
- **Stop loss:** expresado en puntos (pasos de precio). En largos se coloca por debajo de la entrada; en cortos, por encima.
- **Take profit:** también expresado en puntos. El objetivo refleja el stop en dirección opuesta y cierra toda la posición cuando se alcanza.
- **Trailing stop:** empieza después de que el precio se mueve una cantidad configurable de puntos a favor de la operación. El trailing solo se ajusta en dirección del beneficio, replicando el comportamiento "tighten stops only" del EA original.

## Gestión de volumen
- **Lote fijo:** cuando auto-lote está desactivado, la estrategia envía órdenes con el lote fijo configurado.
- **Lote basado en balance:** cuando auto-lote está activado, el volumen se calcula como `(Account Balance / 1000) * LotsPerThousand` y se alinea al paso de volumen y volumen mínimo del instrumento.

## Parámetros y predeterminados
- `SarStep`: factor de aceleración Parabolic SAR. Predeterminado: `0.02`.
- `SarMaximum`: aceleración máxima Parabolic SAR. Predeterminado: `0.2`.
- `CandleType`: marco temporal para el análisis. Predeterminado: velas de 30 minutos.
- `UseAutoLot`: activa tamaño dinámico de lote. Predeterminado: `false`.
- `FixedLot`: volumen usado cuando auto-lote está desactivado. Predeterminado: `0.1`.
- `LotsPerThousand`: multiplicador para cálculos de auto-lote. Predeterminado: `0.05`.
- `StopLossPoints`: distancia al stop en puntos. Predeterminado: `500`.
- `TakeProfitPoints`: distancia al take profit en puntos. Predeterminado: `1000`.
- `TrailingStartPoints`: umbral de beneficio que activa trailing. Predeterminado: `500`.
- `TrailingDistancePoints`: offset de trailing cuando está activado. Predeterminado: `100`.

## Notas
- La estrategia opera direcciones larga y corta, pero mantiene como máximo una posición abierta.
- Las órdenes protectoras se simulan con datos de velas; picos intravela menores que el marco elegido pueden influir en la calidad de ejecución en trading real.
