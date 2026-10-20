# Bollinger Heikin Ashi Entry

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Reversión de Bollinger sobre Heikin Ashi, con toma de beneficio ejecutada sobre la mitad de la posición y seguimiento del resto.

## Reglas de negociación

Cierre HA: `(Open+High+Low+Close)/4`. Primera apertura HA: `(Open+Close)/2`; siguientes: `(apertura HA anterior+cierre HA anterior)/2`. Los extremos HA incluyen apertura/cierre HA y extremos originales. Bollinger usa cierres HA desde la primera vela archivada, no cierres originales. Doji no tiene color. Contacto: `HA low <= lower` / `HA high >= upper`.

Al menos dos velas HA bajistas consecutivas deben tocar cada una su banda inferior. Una vela HA alcista cerrando estrictamente sobre su propia banda inferior abre un largo. Colores y bandas inversos, con cierre bajista bajo la banda superior, abren un corto.

Entrada por `Volume` solo sin posición. Stop inicial en el mínimo/máximo de la vela original terminada anterior, fijado antes de ordenar. `R` es la distancia positiva entre el precio medio ponderado ejecutado y ese stop. A `1R` se cierra la mitad ejecutada a mercado, redondeada hacia abajo por `VolumeStep`. Si cualquier mitad es inferior a `MinVolume`, se cierra todo en el objetivo. Una entrada ejecutada más allá del stop se cierra en la siguiente cotización ejecutable.

Solo una ejecución real del objetivo activa el seguimiento del resto por el mínimo/máximo de la última vela original terminada; nunca se afloja el stop. Bid para largos o ask para cortos activan la salida total entre velas, con prioridad del stop sobre el objetivo. Órdenes pendientes bloquean duplicados. No se usan extremos anteriores para activar niveles creados después. Sin objetivo porcentual fijo ni espera oculta.

Las entradas requieren velas terminadas e indicadores preparados. La posición se gestiona hasta su salida; las órdenes de mercado pendientes impiden envíos duplicados.

Bollinger usa la desviación estándar poblacional de la ventana móvil de cierres, incluida la vela terminada actual.

## Parámetros

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerDeviation` | `2` |
| `CandleType` | `5m` |
