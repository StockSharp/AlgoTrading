# Estrategia de notificación Cross MA ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 "CrossMA". Opera el cruce entre dos promedios móviles simples y protege cada operación con un stop loss basado en el rango verdadero promedio (ATR). Además de la lógica original, la estrategia registra mensajes de información detallada en lugar de enviar correos electrónicos.

## Lógica de trading
1. La estrategia se suscribe a la serie de velas configuradas y calcula una media móvil simple rápida y lenta junto con un indicador ATR.
2. Cuando el SMA rápido cruza por encima del SMA lento, se cierra cualquier exposición corta y se abre una posición larga. El stop loss se coloca un ATR por debajo del precio de entrada.
3. Cuando el SMA rápido cruza por debajo del SMA lento, se cierra cualquier exposición larga y se abre una posición corta. El stop loss se coloca un ATR por encima del precio de entrada.
4. En cada vela terminada se comprueba el precio de parada. Si el precio toca el nivel stop, la posición se cierra inmediatamente en el mercado.

## Gestión del riesgo
- El tamaño de la posición se calcula a partir del capital de la cuenta y el parámetro `Maximum Risk`. Si la información sobre el capital no está disponible, la estrategia vuelve al valor `Base Volume`.
- Después de dos o más operaciones perdedoras consecutivas, el tamaño de la posición se reduce proporcionalmente al `Decrease Factor`, reproduciendo el comportamiento original de MetaTrader.
- Todos los volúmenes están normalizados al paso de volumen de seguridad para garantizar tamaños de pedido válidos.

## Notificaciones
En lugar de enviar correos electrónicos, la estrategia escribe mensajes de registro claros cada vez que se abren o cierran órdenes mediante señales o paradas. El registro se puede desactivar mediante el parámetro `Enable Notifications`.

## Parámetros
- **Tipo de vela**: tipo de vela utilizada para los cálculos del indicador.
- **Período rápido SMA**: período de la media móvil rápida (predeterminado 4).
- **Período lento SMA** – período del promedio móvil lento (predeterminado 12).
- **ATR Período**: número de velas utilizadas por ATR para el cálculo de parada (predeterminado 6).
- **Volumen base**: volumen mínimo negociado cuando el tamaño basado en el riesgo no está disponible (predeterminado 0,1).
- **Riesgo máximo**: fracción del capital asignado a cada operación (predeterminado 0,02).
- **Factor de disminución**: reduce el tamaño de la posición después de perder operaciones (predeterminado 3).
- **Habilitar notificaciones**: habilita el registro de acciones comerciales.
