# Estrategia de Tendencia T3MA Alarm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la idea del indicador T3MA-ALARM. Aplica una media móvil exponencial doblemente suavizada para detectar cambios en la dirección de la tendencia.

Cuando la media móvil suavizada gira hacia arriba, abre una posición larga. Cuando gira hacia abajo, abre una posición corta. Opcionalmente, una señal opuesta puede cerrar la posición actual. Los niveles de stop loss y take profit se establecen como distancias absolutas de precio desde el precio de entrada.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `MaPeriod` | Período de la media móvil exponencial. |
| `MaShift` | Número de barras utilizadas para detectar el cambio de dirección. |
| `StopLoss` | Distancia de precio para el stop loss de protección. Establezca `0` para deshabilitar. |
| `TakeProfit` | Distancia de precio para el take profit. Establezca `0` para deshabilitar. |
| `ReverseOnSignal` | Cerrar una posición opuesta cuando aparece una nueva señal. |
| `CandleType` | Tipo de vela utilizada para los cálculos. |

## Señales

* **Compra** – la dirección de la MA suavizada cambia de bajista a alcista.
* **Venta** – la dirección de la MA suavizada cambia de alcista a bajista.

Las posiciones se cierran ya sea por una señal opuesta (cuando está habilitado) o cuando se alcanzan los niveles de stop loss / take profit.
