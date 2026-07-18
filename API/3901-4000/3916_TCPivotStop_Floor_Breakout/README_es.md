# Estrategia de ruptura de piso TCPivotStop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia TCPivotStop Floor Breakout** es una adaptación directa del MetaTrader asesor experto `gpfTCPivotStop`. La lógica gira en torno
cálculos clásicos de pivote del piso realizados el día de negociación anterior. Al inicio de cada nueva sesión diaria la estrategia:

1. Agrega el máximo, el mínimo y el cierre del día anterior para calcular el punto de pivote más los primeros tres niveles de soporte y resistencia.
2. Comprueba si la última barra horaria completada cruzó el pivote desde arriba o desde abajo.
3. Abre una orden de mercado en la dirección del cruce mientras fija niveles de stop-loss y take-profit que reflejan el
comportamiento del experto original.

Sólo puede haber una posición activa a la vez. La gestión de sesiones opcional permite aplanar la exposición cuando comienza un nuevo día.

## Reglas de trading

- **Período de tiempo**: diseñado para velas de 1 hora (configurable).
- **Cálculo de pivote**: utiliza el máximo, el mínimo y el cierre del día anterior para calcular `Pivot`, `R1`, `R2`, `R3`, `S1`, `S2`, `S3`.
- **Condiciones de entrada**
  - Ingrese *short* cuando la última barra completada se cerró por debajo del pivote mientras que la barra anterior se cerró por encima de él.
  - Ingrese *long* cuando la última barra completada se cerró por encima del pivote mientras que la barra anterior se cerró por debajo de él.
- **Tamaño de posición**: tamaño de lote fijo definido por el parámetro `OrderVolume`.
- **Condiciones de salida**
  - Los precios de stop-loss y take-profit se asignan a los niveles de pivote clásicos.
  - Si el indicador `CloseAtSessionEnd` está habilitado, la estrategia liquida las operaciones abiertas antes de que comience la siguiente sesión.
  - Los niveles de protección se monitorean en los máximos y mínimos de las velas y se ejecutan con órdenes de mercado cuando se tocan.

## Parámetros

| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `OrderVolume` | Tamaño de la operación para las entradas al mercado. | `0.1` |
| `TakeProfitTarget` | Elige qué nivel dinámico actúa como objetivo de ganancias (`1` = más cercano, `3` = más lejano). | `1` |
| `CloseAtSessionEnd` | Cierre cualquier posición abierta una vez que comience una nueva sesión diaria. | `false` |
| `CandleType` | Periodo utilizado para todos los cálculos (por hora por defecto). | `H1` |

## Notas

- La estrategia ejecuta órdenes solo una vez al día cuando hay un nuevo conjunto de pivote disponible, al igual que la fuente EA que se activa en el
primer tick de la sesión diaria.
- La versión MetaTrader recalculó los tamaños de lote utilizando el historial de márgenes de la cuenta. Este puerto mantiene el tamaño de la posición fijo y
delega la gestión del dinero a otros componentes si es necesario.
- Las órdenes de protección se emulan monitoreando los extremos de las velas y enviando órdenes de mercado una vez que se cruza un umbral.

## Archivos

- `CS/TcpFloorPivotBreakoutStrategy.cs` – Implementación en C# de la lógica comercial.
- `README.md` – Documentación en inglés (este archivo).
- `README_zh.md` – Traducción al chino simplificado.
- `README_ru.md` – traducción al ruso.
