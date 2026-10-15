# Estrategia de Solo Compra con EMA y BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición larga cuando el precio cierra por encima de la EMA.
El stop loss inicial se coloca en la banda inferior de Bollinger y se mueve a la EMA si el precio cierra por encima de la banda superior.
El take profit se establece usando una relación beneficio/riesgo basada en la distancia a la banda.
Tras alcanzarse el take profit, la estrategia espera a que el precio cruce por debajo de la EMA antes de permitir una nueva entrada.

## Detalles
- **Criterios de entrada:** Cierre por encima de la EMA sin bloqueo activo y sin posición abierta.
- **Largo/Corto:** Solo largos.
- **Criterios de salida:** El precio cruza por debajo del nivel de stop o alcanza el take profit.
- **Stops:** Stop inicial en la banda inferior, desplazándose a la EMA tras un movimiento fuerte.
- **Valores predeterminados:** Longitud EMA = 40, desviación de banda = 0.7, relación beneficio/riesgo = 3.
