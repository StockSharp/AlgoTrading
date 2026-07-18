# Estrategia del canal comercial ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Trade Channel replica el asesor experto MetaTrader original que negociaba canales de precios con paradas basadas en ATR. Espera a que los límites del canal permanezcan sin cambios y a que la última vela toque o rechace esos niveles. Cuando aparece la configuración, la estrategia abre una posición en la dirección opuesta al toque y aplica un trailing stop adaptativo medido en puntos.

El enfoque busca explotar la reversión a la media en torno a un canal de precios estable. Filtra las señales para que el canal deba estar plano (sin nuevos máximos ni mínimos) antes de ingresar. Las paradas de protección se colocan más allá del canal utilizando el rango verdadero promedio, y una parada dinámica opcional bloquea las ganancias una vez que se desarrolla el movimiento.

## Detalles

- **Criterios de entrada**:
  - Corto: el máximo del canal es igual al máximo del canal anterior y la última vela rompe ese máximo o cierra entre el máximo y el pivote `(high + low + close) / 3`.
  - Largo: el mínimo del canal es igual al mínimo del canal anterior y la última vela rompe ese mínimo o cierra entre el mínimo y el pivote.
- **Largo/Corto**: Ambas direcciones, pero solo una posición a la vez.
- **Criterios de salida**:
  - Largo: el precio toca el máximo del canal mientras el máximo se mantiene sin cambios.
  - Corto: el precio toca el mínimo del canal mientras el mínimo se mantiene sin cambios.
  - El trailing stop opcional se ajusta detrás del mercado una vez que las ganancias superan los `TrailingDistance` puntos.
- **Paradas**: Stop Loss inicial en `channel boundary ± ATR`. El trailing stop lo reemplaza cuando se activa.
- **Valores predeterminados**:
  - `Volume` = 0,1 m
  - `ChannelPeriod` = 20
  - `AtrPeriod` = 4
  - `TrailingDistance` = 30
  - `CandleType` = velas de 30 minutos
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: rango verdadero más alto, más bajo y promedio
  - Paradas: ATR parada, arrastrando
  - Complejidad: Intermedia
  - Plazo: Intradiario (30 minutos)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: medio

## Notas

- `Volume` controla el tamaño del pedido; sólo puede existir una posición a la vez.
- `TrailingDistance` se especifica en puntos (escalones de precio). Establezca en cero para desactivar el trailing stop.
- La estrategia requiere velas históricas para calentar los indicadores más alto/más bajo y ATR antes de operar.
- Las órdenes stop se cancelan automáticamente cuando se cierra la posición o se reinicia la estrategia.
