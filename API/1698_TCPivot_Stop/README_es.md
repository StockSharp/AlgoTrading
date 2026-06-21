# Estrategia TCPivot Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas a través de la línea de pivote diaria. Calcula los niveles de pivote clásicos de los operadores de piso a partir del máximo, mínimo y cierre del día anterior. Se abre una posición larga cuando el precio de cierre cruza por encima del pivote. Se abre una posición corta cuando el precio de cierre cruza por debajo del pivote.

Tras la entrada, el sistema utiliza uno de los niveles de soporte o resistencia tanto como objetivo de beneficio como de stop loss. El nivel se selecciona mediante el parámetro **Target Level**:

- **1** – utiliza `Support1`/`Resistance1`.
- **2** – utiliza `Support2`/`Resistance2`.
- **3** – utiliza `Support3`/`Resistance3`.

Si **Intraday Only** está activado, todas las posiciones abiertas se cierran a las 23:00 hora de la plataforma.

## Detalles

- **Criterios de entrada**
  - **Largo**: cierre anterior ≤ pivote y cierre actual > pivote.
  - **Corto**: cierre anterior ≥ pivote y cierre actual < pivote.
- **Criterios de salida**
  - **Largo**: cierre ≥ nivel de resistencia seleccionado o cierre ≤ nivel de soporte seleccionado.
  - **Corto**: cierre ≤ nivel de soporte seleccionado o cierre ≥ nivel de resistencia seleccionado.
  - Si *Intraday Only* es verdadero, todas las posiciones se cierran a las 23:00.
- **Indicadores**: solo cálculo clásico de pivote.
- **Marco temporal**: configurable; velas de 5 minutos por defecto.
- **Stops**: stop-loss y take-profit basados en el nivel de pivote elegido.
