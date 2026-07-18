# Estrategia de Cruce SMA y EMA DNSE VN301
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el índice VN301 utilizando un cruce entre una EMA de 15 períodos y una SMA de 60 períodos. Sale antes de que finalice la sesión de negociación y aplica un stop porcentual simple para limitar las pérdidas.

Las pruebas indican un retorno anual promedio de aproximadamente el 20%. Funciona mejor en futuros VN30.

Se abre una posición larga cuando la EMA15 cruza por encima de la SMA60 y el precio está por encima de la EMA. Se abre una posición corta en el cruce opuesto. Las posiciones se cierran en señales inversas, al final de la sesión, o cuando el precio se mueve en contra de la entrada más allá del límite de pérdida configurado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA15 cruza por encima de la SMA60 y el precio >= EMA15 antes del cierre.
  - **Corto**: EMA15 cruza por debajo de la SMA60 y el precio <= EMA15 antes del cierre.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto, pérdida máxima o cierre de sesión.
- **Stops**: Sí, pérdida máxima basada en porcentaje.
- **Filtros**:
  - Hora de cierre de sesión.
