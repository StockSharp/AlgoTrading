# RSI Estrategia de nube dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **RSI Estrategia de Nube Dual** es una StockSharp adaptación del asesor experto MetaTrader "RSI Nube Dual EA".
Se negocia con una serie de velas configurables y analiza dos cálculos del índice de fuerza relativa (RSI): un análisis rápido.
y una línea lenta. Las señales se generan cuando el RSI rápido entra, permanece dentro o sale de una sobreventa/sobrecompra definida.
zona, o cuando la línea rápida cruza la línea lenta. La estrategia puede opcionalmente invertir sus señales y puede restringirse
a operación sólo larga o sólo corta.

La estrategia opera únicamente con órdenes de mercado. Cuando se recibe una nueva señal, la posición existente en el lado opuesto
La dirección se cierra antes de abrir una nueva posición. El tamaño de la posición se controla a través de un único parámetro de volumen.

## Lógica de señal
1. **Señal de entrada** – se activa cuando el rápido RSI cruza hacia la zona:
   - Largo: RSI anterior por encima del nivel inferior y RSI actual por debajo de él.
   - Breve: RSI anterior debajo del nivel superior y RSI actual encima de él.
2. **Siendo señal** – se activa mientras el RSI rápido permanezca dentro de la zona:
   - Largo: rápido RSI por debajo del nivel inferior.
   - Corto: rápido RSI por encima del nivel superior.
3. **Señal de salida**: se activa cuando el RSI rápido sale de la zona:
   - Largo: RSI anterior por debajo del nivel inferior y RSI actual por encima de él.
   - Breve: RSI anterior por encima del nivel superior y RSI actual por debajo de él.
4. **Señal de cruce** – utiliza el comportamiento de nube dual:
   - Largo: RSI rápido cruzando por encima del RSI lento.
   - Breve: RSI rápido cruzando por debajo del RSI lento.

Se puede habilitar cualquier combinación de las cuatro condiciones. Al menos una condición debe estar activa para que se produzcan entradas.
Cuando la opción **Inversa** está habilitada, las señales largas y cortas se intercambian.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| **Tipo de vela** | La serie de velas utilizada para los cálculos (predeterminado: 1 hora). |
| **Rápido RSI / Lento RSI** | Períodos para los cálculos rápidos y lentos RSI. |
| **Nivel superior / Nivel inferior** | RSI umbrales para las zonas de sobrecompra y sobreventa. |
| **Volumen de pedido** | Volumen de órdenes de mercado. |
| **Usar Entrada / Estar / Salir / Cruzar** | Alterna para cada familia de señales. |
| **Velas Cerradas** | Si está habilitado, las señales solo se evalúan en velas terminadas. |
| **Reversa** | Intercambia señales largas y cortas. |
| **Modo comercial** | Limita el comercio a largo, corto o en ambas direcciones. |

## Notas de uso
- La estrategia se suscribe a una única serie de velas y ejecuta dos indicadores RSI vinculados a través del nivel alto API.
- Sólo se utilizan órdenes de mercado; cualquier exposición abierta en la dirección opuesta se cierra antes de realizar una nueva operación.
- La configuración predeterminada coincide con el asesor experto original (rápido RSI 5, lento RSI 15, niveles 25/75).
- Combine los interruptores de señal para reproducir las combinaciones de indicadores de la versión MetaTrader.
