# Estrategia de Canal de Cruce Porcentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Canal de Cruce Porcentual se origina del asesor experto de MetaTrader 5 *Percentage_Crossover_Channel_EA*. Se basa en un canal personalizado construido alrededor de una media móvil rápida y reacciona a toques de banda o cruces de la línea media. Esta implementación de StockSharp sigue la misma lógica mientras usa la API de alto nivel para procesar velas completadas.

## Construcción del canal
El indicador subyacente construye un canal dinámico alrededor del precio seleccionado (cierre por defecto):

1. Calcular el precio base usando el modo **Applied Price** configurado.
2. Aplicar una media móvil simple de 1 período para obtener el precio de referencia a corto plazo.
3. Calcular dos límites usando el parámetro **Percent** (p. ej., 50 → ±0,5%).
4. Limitar la línea media anterior dentro de los nuevos límites para obtener el valor medio actual.
5. Las bandas superior e inferior son el valor medio limitado multiplicado por los factores ±porcentaje.

Esta recursión permite que el canal se retrase durante tendencias fuertes mientras mantiene un envolvente ajustado cuando el precio consolida.

## Lógica de trading
Hay dos modos de señal diferentes disponibles:

- **Modo de toque de banda (por defecto):**
  - Entrada larga cuando el mínimo de la vela anterior estaba por encima de la banda inferior y la última vela completada la toca o perfora.
  - Entrada corta cuando el máximo de la vela anterior estaba por debajo de la banda superior y la última vela completada la toca o perfora.
- **Modo de cruce de línea media (TradeOnMiddleCross = true):**
  - Entrada larga cuando el precio cruza la línea media de arriba hacia abajo.
  - Entrada corta cuando el precio cruza la línea media de abajo hacia arriba.

El indicador **ReverseSignals** intercambia las reglas largas y cortas. La estrategia siempre cierra y revierte las posiciones existentes enviando una única orden a mercado cuyo volumen equivale al **OrderVolume** configurado más el valor absoluto de la posición actual.

## Gestión de riesgos
Los niveles protectores opcionales emulan la configuración original de stop-loss y take-profit de MT5:

- **StopLossPoints** – distancia en pasos de precio restada (largo) o añadida (corto) del precio de entrada estimado.
- **TakeProfitPoints** – distancia en pasos de precio añadida (largo) o restada (corto) del precio de entrada.

Si algún parámetro es cero, la protección correspondiente se desactiva. Los stops se evalúan en cada vela terminada comparando máximos y mínimos de la vela con los niveles almacenados. No se aplica lógica de trailing.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de vela al que suscribirse (marco temporal de 15 minutos por defecto). |
| `Percent` | Ancho del canal en porcentaje del precio (convertido a factores ±porcentaje/100). |
| `PriceMode` | Precio aplicado para el canal. Opciones: Close, Open, High, Low, Median (H+L)/2, Typical (H+L+C)/3, Weighted (H+L+2C)/4, Average (O+H+L+C)/4. |
| `TradeOnMiddleCross` | Cambiar entre lógica de toque de banda y lógica de cruce de línea media. |
| `ReverseSignals` | Invertir las condiciones largas y cortas. |
| `StopLossPoints` | Distancia del stop protector expresada en pasos de precio del instrumento. |
| `TakeProfitPoints` | Distancia del objetivo de ganancia expresada en pasos de precio del instrumento. |
| `OrderVolume` | Volumen base para entradas a mercado. La estrategia añade la posición abierta absoluta para revertir en una transacción. |

## Notas de implementación
- Las órdenes se emiten solo después de que las velas terminen, lo que refleja el asesor experto de MT5 que actuaba al comienzo de la siguiente barra usando los datos de la barra anterior.
- El indicador de canal se recrea dentro de la estrategia sin almacenar colecciones históricas, confiando en variables de estado escalares.
- Los stops y objetivos de protección se verifican manualmente para replicar el manejo de órdenes específico de la plataforma desde MT5.
- Asegurarse de que el instrumento seleccionado exponga un `PriceStep` válido; de lo contrario, las distancias de stop-loss y take-profit serán ignoradas.
