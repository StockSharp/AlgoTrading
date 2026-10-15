# Estrategia de ruptura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de ruptura es un sistema de ruptura de Donchian-canal convertido del MetaTrader5 asesor experto original `BreakoutStrategy.mq5`. En cada barra completada, la estrategia monitorea el máximo más alto y el mínimo más bajo a través de una ventana retrospectiva configurable y realiza operaciones una vez que el precio supera esos límites. Las posiciones abiertas están protegidas por un canal de seguimiento derivado de un segundo cálculo Donchian, que refleja la lógica de seguimiento utilizada en el experto fuente.

## Lógica comercial

1. **Canal de entrada**: los precios más altos y más bajos sobre `EntryPeriod` barras se retrasan `EntryShift` barras para evitar el uso de la barra actual en el cálculo del desglose.
2. **Detección de ruptura**: se activa una ruptura larga cuando el máximo de la barra toca la banda superior desplazada más un paso de precio. Se activa una ruptura corta cuando el mínimo de la barra toca la banda inferior desplazada menos un paso de precio.
3. **Salir del canal**: los precios más altos y más bajos sobre `ExitPeriod` barras se retrasan en `ExitShift` barras. La línea media opcional puede ajustar el trailing stop seleccionando el máximo (para largos) o el mínimo (para cortos) entre las bandas exterior y media, replicando la opción "usar línea media" de EA.
4. **Gestión de posiciones**: la estrategia cierra una posición larga existente cuando la barra baja atraviesa el nivel final y cierra una posición corta cuando la barra alta toca el nivel final corto. Las señales opuestas aplanan cualquier exposición existente antes de entrar en la nueva dirección.
5. **Tamaño del riesgo**: el tamaño de la posición se deriva de `RiskPerTrade`. La estrategia obtiene el capital de la cartera, convierte la distancia de parada en dinero utilizando los instrumentos `PriceStep` y `StepPrice` y solicita el mayor volumen permitido que mantenga la pérdida cerca del porcentaje configurado. Los volúmenes están alineados con el instrumento `VolumeStep`, `VolumeMin` y `VolumeMax`.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `CandleType` | Tipo de datos que describe la serie de velas utilizada por la estrategia. El valor predeterminado son velas de 1 hora. |
| `EntryPeriod` | Ventana retrospectiva del canal de ruptura. |
| `EntryShift` | Número de barras completadas utilizadas como compensación al evaluar el canal. `1` reproduce el comportamiento original de EA. |
| `ExitPeriod` | Ventana retrospectiva para el canal de salida final. |
| `ExitShift` | Desplazamiento en barras aplicado al canal final. |
| `UseMiddleLine` | Cuando está habilitada, la línea media Donchian participa en el cálculo del trailing stop, coincidiendo con la opción MQL5. |
| `RiskPerTrade` | Fracción del capital de la cartera arriesgada por operación (por ejemplo, `0.01` por 1%). |

## Notas

- Todos los comentarios dentro de la implementación de C# están escritos en inglés según lo exigen las pautas del repositorio.
- La estrategia utiliza StockSharp funciones API de alto nivel: suscripciones de velas, Donchian canales (indicadores `Highest`/`Lowest`) e indicadores de cambio para evitar buffers manuales.
- No se proporcionan pruebas automatizadas para esta conversión; valide el comportamiento en su propio entorno antes de implementarlo en producción.
