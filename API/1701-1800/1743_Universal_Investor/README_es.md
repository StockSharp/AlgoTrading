# Estrategia de Inversor Universal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Inversor Universal** utiliza el cruce entre la Media Móvil Exponencial (EMA) y la Media Móvil Ponderada Lineal (LWMA) para determinar la dirección del mercado. Confirma la fuerza de la tendencia verificando que ambas medias se muevan en la misma dirección.

## Lógica

- **Entrada de compra**: LWMA está por encima de EMA y ambas medias están subiendo.
- **Entrada de venta**: LWMA está por debajo de EMA y ambas medias están cayendo.
- **Salida de compra**: LWMA cruza por debajo de EMA.
- **Salida de venta**: LWMA cruza por encima de EMA.

La estrategia reduce el tamaño de la posición tras operaciones perdedoras consecutivas cuando el factor de reducción está habilitado.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `MovingPeriod` | Longitud para los cálculos de EMA y LWMA. |
| `DecreaseFactor` | Factor de reducción de lotes tras pérdidas (0 desactiva la reducción). |
| `CandleType` | Tipo de datos de velas para los cálculos. |
| `Volume` | Volumen base de operación desde la configuración de la estrategia. |

## Notas

- Funciona únicamente con velas cerradas.
- Utiliza la API de alto nivel de StockSharp con vinculación de indicadores.
- No se proporciona versión en Python.
