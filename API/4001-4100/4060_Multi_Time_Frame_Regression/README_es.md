# Estrategia de regresión de múltiples marcos temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de múltiples marcos temporales que combina canales de regresión lineal en velas M1, M5 y H1. La pendiente de regresión del canal H1 define la tendencia dominante, mientras que los canales M5 y M1 proporcionan ubicaciones de entrada precisas cerca del soporte y la resistencia.

## Lógica comercial

- **Fuentes de datos**: nueve períodos de tiempo de velas estándar (M1, M5, M15, M30, H1, H4, D1, W1, MN1).
- **Indicadores**: cada feed es procesado por un canal de regresión lineal de longitud configurable. El canal proporciona una línea central y bandas superior/inferior simétricas basadas en la desviación máxima de los cierres recientes.
- **Filtro de tendencias**: la estrategia sólo considera operaciones cortas cuando la pendiente del canal H1 es negativa y operaciones largas cuando es positiva.
- **Entrada**:
  - **Corto**: los últimos máximos M5 y M1 perforan sus bandas superiores del canal, mientras que la pendiente H1 es negativa.
  - **Largo**: los últimos mínimos de M5 y M1 alcanzan sus bandas de canal inferiores, mientras que la pendiente H1 es positiva.
- **Manejo de órdenes**: las entradas se ejecutan con órdenes de mercado utilizando el volumen configurado. Los objetivos de limitación de pérdidas y toma de ganancias se derivan del ancho medio y la línea central del canal M5, respectivamente.
- **Salida**: las posiciones se cierran en las velas M1 cuando el precio alcanza el stop protector o el objetivo de la línea central.
- **Gestión de posiciones**: como máximo hay una posición de mercado abierta en cualquier momento.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `EnableTrading` | Permite que la estrategia realice pedidos cuando está habilitada. |
| `BarsToCount` | Número de barras utilizadas en cada canal de regresión (50 por defecto). |
| `Volume` | Volumen de órdenes de mercado en lotes. |

## Notas

- Las ventanas de regresión más largas proporcionan pendientes del canal más suaves pero reacciones más lentas.
- La visualización de pendiente de marcos de tiempo múltiples es útil para monitorear la alineación en intervalos más altos, aunque solo las entradas de pendiente H1.
- Los niveles de protección se recalculan cada vez que se forma una nueva vela M5; La recalibración frecuente mantiene el riesgo estrechamente vinculado a la geometría actual del canal.
