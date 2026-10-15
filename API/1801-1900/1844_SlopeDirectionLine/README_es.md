# Estrategia SlopeDirectionLine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el comportamiento del Asesor Experto *Slope Direction Line*. Analiza la pendiente de una línea de regresión lineal construida sobre precios de cierre. Se abre una posición larga cuando la pendiente de regresión se vuelve positiva después de ser negativa, mientras que se abre una posición corta cuando se vuelve negativa después de ser positiva. Las posiciones opuestas se cierran con cada cambio de dirección. Los porcentajes opcionales de stop-loss y take-profit protegen las posiciones a través del mecanismo integrado `StartProtection`.

## Detalles
- **Indicador** – `LinearRegression` de StockSharp. La estrategia utiliza el componente `LinearRegSlope` como señal.
- **Señal** – cruce de la pendiente por cero. Una pendiente positiva indica una tendencia alcista; una pendiente negativa señala una tendencia bajista.
- **Entrada/Salida** – cuando la pendiente cambia de signo, la posición actual se cierra y, si está permitido, se abre una nueva posición en la dirección de la pendiente.
- **Control de riesgo** – `StartProtection` se configura con porcentajes de take-profit y stop-loss definidos por el usuario.

## Parámetros
| Nombre | Descripción |
|--------|-------------|
| `CandleType` | Marco temporal utilizado para construir velas. |
| `Length` | Número de barras utilizadas en el cálculo de regresión lineal. |
| `TakeProfitPercent` | Distancia porcentual al take-profit desde el precio de entrada. |
| `StopLossPercent` | Distancia porcentual al stop-loss desde el precio de entrada. |
| `AllowLong` | Permitir abrir posiciones largas. |
| `AllowShort` | Permitir abrir posiciones cortas. |

## Uso
1. Agregar la estrategia a una aplicación StockSharp.
2. Configurar los parámetros según el marco temporal y el riesgo deseados.
3. Iniciar la estrategia y monitorear las operaciones en el gráfico.
