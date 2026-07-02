# Estrategia ABCWsCci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia AbcWsCci** combina dos patrones clásicos de inversión de velas japonesas (**Tres soldados blancos** y **Tres cuervos negros**) con el indicador **Commodity Channel Index (CCI)** para confirmación. El sistema escanea velas terminadas, mide el tamaño del cuerpo en relación con una línea base de promedio móvil y abre operaciones solo cuando el fuerte impulso de múltiples velas se alinea con CCI extremos. Las salidas de posición se activan cuando el CCI abandona las zonas extremas, lo que indica que el impulso se está desvaneciendo.

## Lógica de trading
- Mantenga un promedio móvil de los tamaños del cuerpo de las velas para calificar velas "largas".
- Detecte el patrón de los Tres Soldados Blancos (tres velas alcistas fuertes consecutivas con puntos medios ascendentes).
- Detecte el patrón de los Tres Cuervos Negros (tres velas bajistas fuertes consecutivas con puntos medios descendentes).
- Confirme las entradas alcistas con CCI cayendo por debajo de **-50** y las entradas bajistas con CCI subiendo por encima de **50**.
- Cierre las posiciones largas cuando CCI cruce los niveles **-80** o **80**, y cierre las posiciones cortas en las condiciones reflejadas.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `CciPeriod` | Longitud del indicador CCI utilizado para la confirmación. | 37 |
| `BodyAveragePeriod` | Número de velas en la media móvil que define el tamaño corporal mínimo "fuerte". | 13 |
| `CandleType` | Marco de tiempo de vela utilizado para la detección de patrones. | 1 hora |

## Indicadores
- **Índice de canales de productos básicos (CCI)**: Evalúa los extremos del impulso para señales de confirmación y salida.
- **Promedio Móvil Simple de cuerpos de velas**: Establece el tamaño mínimo de vela requerido para un patrón válido.

## Gestión de Puestos
- Ingrese **largo** cuando se formen Tres Soldados Blancos y CCI esté por debajo de -50 mientras no haya ninguna posición larga activa.
- Ingrese **short** cuando se formen Three Black Crows y CCI esté por encima de 50 mientras no haya ninguna posición corta activa.
- Salga de las posiciones **largas** cuando CCI abandone la banda -80/80, lo que indica que el impulso alcista se ha agotado.
- Salga de las posiciones **cortas** cuando CCI abandone la banda +80/-80, lo que indica una pérdida de impulso bajista.

## Notas de uso
- La estrategia está basada en eventos: sólo se procesan velas completamente completadas.
- Funciona mejor en instrumentos de tendencia donde el impulso de múltiples velas combinado con los extremos del oscilador proporciona señales confiables.
- Considere combinarlo con reglas adicionales de gestión de riesgos (stop-loss, tamaño de posición) dependiendo de su entorno comercial.
