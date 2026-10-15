# Estrategia ORB VWAP Braid Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rompimiento del rango de apertura con confirmación de VWAP y filtro Braid.

## Reglas
- Opera entre las 09:35 y las 11:00 hora del exchange
- Una operación por día
- Largo cuando el precio cierra por encima del máximo del rango de apertura, por encima del VWAP y el filtro Braid es alcista
- Corto cuando el precio cierra por debajo del mínimo del rango de apertura, por debajo del VWAP y el filtro Braid es bajista
- Stop-loss en el lado opuesto del rango
- Take profit en dos veces el riesgo limitado por los niveles del día anterior o del pre-mercado

## Indicadores
- Promedio Móvil Ponderado por Volumen (VWAP)
- Media Móvil Exponencial (3, 7, 14)
- Rango Verdadero Promedio (14)
