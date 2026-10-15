# Estrategia de Arcoíris de Medias Móviles (Stormer)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traza un arcoíris de doce medias móviles. Las operaciones se abren cuando se confirma la tendencia y el precio toca una de las medias.

Una posición larga se abre cuando el precio marca un nuevo máximo, todas las medias centrales apuntan hacia arriba y la vela cierra por encima de la media de todas las medias. Una posición corta se abre cuando ocurren las condiciones opuestas.

El stop loss se fija en la media móvil tocada anteriormente. El take profit se calcula como un múltiplo de la distancia entre el precio de entrada y el stop loss.

## Detalles

- **Indicadores**: 12 medias móviles de tipo configurable.
- **Largo**: Tendencia alcista, nuevo máximo y precio de toque previo.
- **Corto**: Tendencia bajista, nuevo mínimo y precio de toque previo.
- **Salidas**: Stop loss en la media tocada, objetivo = entrada ± distancia * factor. Salida de reversión opcional cuando la tendencia muestra señales de giro.
- **Parámetros**: tipo de media móvil, longitudes, factor objetivo, opciones de reversión.
- **Marco temporal**: Cualquiera.
