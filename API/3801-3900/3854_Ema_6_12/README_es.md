# EMA Estrategia cruzada 6/12
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto MetaTrader que intercambia el cruce entre un EMA(6) rápido y un {PH002}}(12) lento. Se suscribe a velas horarias de forma predeterminada, calcula ambas medias móviles y espera un cruce confirmado al cierre de una vela antes de actuar.

## Lógica de trading

- **Entrada:**
  - Aparece una señal alcista cuando EMA(6) cruza por encima de EMA(12). La estrategia abre una posición larga si no hay ninguna posición activa.
  - Aparece una señal bajista cuando EMA(6) cruza por debajo de EMA(12). La estrategia abre una posición corta si no hay una posición activa.
- **Salir:**
  - Cuando `UseCloseSignals` está habilitado (comportamiento predeterminado), la estrategia cierra la posición actual una vez que se detecta un cruce opuesto. Espera el próximo cruce antes de abrir una nueva operación, reflejando al asesor experto original.
  - Las protecciones opcionales de toma de ganancias y trailing stop se administran a través del asistente integrado `StartProtection` de StockSharp.
- **Tamaño de posición:**
  - Los pedidos utilizan el parámetro `OrderVolume` (1 lote predeterminado). Los volúmenes se alinean con la configuración de seguridad antes de enviar pedidos.

## Gestión del riesgo

- **Parada dinámica:** Convierte la configuración original de "puntos" en incrementos de precios. Cuando es mayor que cero, el stop se desplaza automáticamente en la dirección de la operación una vez que la posición se vuelve rentable.
- **Take de ganancias:** Expresado en incrementos de precio. Establezca en cero para desactivar.
- La estrategia nunca promedia hacia abajo o pirámides. Sólo se permite una posición por símbolo.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Marco de tiempo utilizado para construir velas y EMA. El valor predeterminado es 1 hora. |
| `OrderVolume` | Tamaño comercial en lotes. |
| `ShortEmaLength` | Periodo para la EMA rápida (por defecto 6). |
| `LongEmaLength` | Período para el EMA lento (predeterminado 12). |
| `UseCloseSignals` | Cierre la posición actual en un cruce opuesto (predeterminado: habilitado). |
| `TrailingStopSteps` | Distancia de seguimiento en pasos de precio. Zero desactiva el seguimiento. |
| `TakeProfitSteps` | Tome la distancia de beneficio en pasos de precio. Zero lo desactiva. |

## Notas

- Las señales se procesan únicamente en velas terminadas para evitar el ruido dentro de la barra.
- Los valores EMA anteriores se restablecen cada vez que la posición vuelve a cero, lo que garantiza una detección limpia para el siguiente cruce.
- Todos los comentarios del código están escritos en inglés y la sangría utiliza tabulaciones de acuerdo con las pautas del proyecto.
