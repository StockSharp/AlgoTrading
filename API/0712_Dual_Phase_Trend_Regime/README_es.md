# Estrategia de Régimen de Tendencia de Doble Fase
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Régimen de Tendencia de Doble Fase alterna entre osciladores de tendencia lento y rápido según la volatilidad actual. La volatilidad se obtiene de la desviación estándar de los rendimientos y se clasifica en regímenes bajo o alto. Las pendientes de regresión lineal determinan la dirección de la tendencia. Las entradas pueden tomarse en cambios de régimen o cruces de osciladores.

## Parámetros
- Tipo de vela
- Dirección de operación
- Fuente de señal
- Longitud del oscilador lento
- Longitud del oscilador rápido
- Intervalo de recálculo de volatilidad
- Período de volatilidad actual
- Longitud de suavizado de volatilidad
