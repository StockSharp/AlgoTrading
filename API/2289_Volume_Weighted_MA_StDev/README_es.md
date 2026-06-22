# Estrategia de MA Ponderada por Volumen con Desviación Estándar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica una Media Móvil Ponderada por Volumen (VWMA) con un filtro de desviación estándar. Mide el impulso de la VWMA y abre una posición larga cuando el movimiento al alza supera un umbral de desviación configurable. Se abre una posición corta cuando el movimiento a la baja cruza el umbral negativo. El enfoque intenta capturar movimientos direccionales fuertes confirmados por el volumen.

## Parámetros
- Tipo de vela
- Longitud de VWMA
- Período de StdDev
- K1
- K2
