# Estrategia Last ZZ50
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Last ZZ50 reproduce el asesor experto "Last ZZ50" de Vladimir Karputov para MetaTrader.
Utiliza el indicador ZigZag para rastrear los tres puntos de inflexión más recientes y coloca órdenes pendientes en el punto medio de las dos últimas piernas del ZigZag.
El enfoque intenta unirse a los breakouts desde el último swing mientras cancela o reposiciona órdenes cada vez que la estructura del ZigZag cambia.

## Lógica de trading
- **Detección de pivotes** – Un indicador ZigZag (profundidad 12, desviación 5, backstep 3 por defecto) proporciona los últimos pivotes etiquetados A (más reciente), B y C.
- **Orden del tramo BC** – Cuando el pivote C difiere de B y el nuevo pivote A no invalida la dirección del tramo, la estrategia coloca una orden pendiente en `(B + C) / 2`.
  - Si el tramo BC está subiendo la orden es larga, de lo contrario es corta.
  - El tipo límite versus stop se selecciona según el precio actual relativo al punto medio.
- **Orden del tramo AB** – La misma lógica de punto medio se aplica al tramo AB, nuevamente usando órdenes límite o stop dependiendo del precio actual.
- **Filtro de sesión** – El trading está limitado a un día de la semana configurable y ventana intradía (por defecto lunes 09:01 a viernes 21:01). Fuera de la ventana la estrategia cancela órdenes pendientes y puede opcionalmente aplanar cualquier posición.
- **Salida con trailing** – Una vez que una posición gana más que la suma de los umbrales de trailing stop y trailing step, una orden stop protectora se arrastra detrás del precio para asegurar ganancias.

## Gestión de riesgos
- El volumen de órdenes pendientes es igual al parámetro multiplicador por el volumen mínimo negociable del instrumento.
- Tanto las órdenes AB como BC se cancelan y recrean cada vez que los pivotes del ZigZag cambian, evitando que órdenes obsoletas queden en el libro.
- Los trailing stops solo se activan después de que la posición está cómodamente en ganancia, reduciendo salidas prematuras en condiciones agitadas.

## Parámetros
- `LotMultiplier` – Multiplicador aplicado al volumen mínimo negociable al enviar órdenes.
- `ZigZagDepth`, `ZigZagDeviation`, `ZigZagBackstep` – Valores de configuración para el indicador ZigZag.
- `TrailingStopPips`, `TrailingStepPips` – Distancia y umbral de activación para el trailing stop medido en pips.
- `StartDay`, `EndDay`, `StartTime`, `EndTime` – Límites de la sesión de trading.
- `CloseOutsideSession` – Si se deben aplanar posiciones cuando el filtro de tiempo está inactivo.
- `CandleType` – Serie de velas usada para cálculos del ZigZag (por defecto 1 hora).

## Indicadores
- **ZigZag** – Proporciona puntos pivote que impulsan la colocación de órdenes y la validación de estructura.
