# Estrategia de Pirámide Rijfie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición larga inicial cuando el oscilador Stochastic cruza por encima de un nivel bajo configurable. Luego agrega nuevas posiciones cada vez que el precio baja un porcentaje fijo mientras se mantiene por encima de un filtro EMA y un precio mínimo. Un temporizador opcional puede cerrar todas las posiciones en un momento especificado.

## Parámetros
- Tipo de vela
- Nivel bajo del Stochastic
- Precio máximo para la primera entrada
- Precio mínimo permitido
- Período EMA
- Nivel de paso en porcentaje
- Cerrar posiciones a la hora
- Hora de cierre
- Minuto de cierre
