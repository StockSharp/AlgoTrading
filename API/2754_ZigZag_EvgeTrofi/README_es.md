# Estrategia ZigZag EvgeTrofi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia ZigZag EvgeTrofi porta el clásico asesor experto de MetaTrader a la API de alto nivel de StockSharp. Observa el oscilante más reciente detectado por un proceso de estilo ZigZag y reacciona rápidamente mientras el pivote todavía está fresco.

## Concepto

* El asesor original analiza el primer punto no nulo del buffer ZigZag y decide si el último oscilante confirmado fue un máximo o un mínimo.
* Un máximo oscilante genera una entrada larga de forma predeterminada. Activar **SignalReverse** invierte la lógica.
* Las posiciones se abren solo mientras el nuevo pivote se considera reciente. El parámetro **Urgency** limita el número de barras después de un pivote cuando se pueden iniciar operaciones.
* Las posiciones existentes en la dirección opuesta se aplanan inmediatamente antes de colocar nuevas órdenes. La estrategia puede escalar en la misma dirección en barras consecutivas mientras la ventana de urgencia está abierta.

Este port mantiene el comportamiento contrario: los nuevos máximos desencadenan operaciones largas mientras que los mínimos frescos desencadenan cortos, imitando la configuración original.

## Cómo funciona

1. Dos indicadores móviles (`Highest` y `Lowest`) aproximan la lógica de profundidad de ZigZag de MetaTrader.
2. Cada vez que el precio imprime un nuevo extremo por encima/debajo de esas bandas y el movimiento excede **Deviation** (en pasos de precio), se registra un pivote.
3. El algoritmo rastrea cuántas barras pasaron desde el pivote. Una vez que el contador excede **Urgency**, la señal expira.
4. En cada vela cerrada durante la ventana activa, la estrategia entra usando `VolumePerTrade`. La exposición opuesta se cierra primero, por lo que los giros de posición ocurren limpiamente.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Depth` | 17 | Ventana en barras para buscar atrás máximos/mínimos oscilantes. Refleja la entrada de profundidad de ZigZag. |
| `Deviation` | 7 | Desplazamiento mínimo de precio en puntos (multiplicado por el paso de precio del símbolo) requerido para aceptar un nuevo pivote. |
| `Backstep` | 5 | Barras que deben transcurrir antes de que el indicador pueda cambiar a la dirección de pivote opuesta. |
| `Urgency` | 2 | Número máximo de barras después del pivote cuando se permiten entradas. |
| `SignalReverse` | `false` | Invierte el mapeo de máximos/mínimos a señales largas/cortas. |
| `CandleType` | Velas de 5 minutos | Marco temporal usado para el análisis. Ajuste al gráfico que desea reflejar. |
| `VolumePerTrade` | 0.10 | Tamaño de orden enviado en cada entrada. Coincide con la entrada de lotes original. |

## Notas de trading

* La lógica **no** incluye stops ni objetivos. El control de riesgo debe agregarse mediante overlays o configuraciones de portafolio si es necesario.
* Dado que el sistema puede agregar a una posición cada barra dentro de la ventana de urgencia, el tamaño de la posición puede crecer rápidamente en tendencias fuertes.
* Use profundidades mayores en símbolos volátiles para evitar excesivos pivotes. Profundidades menores hacen la estrategia más reactiva pero más ruidosa.
* Cuando **SignalReverse** es true, el comportamiento se convierte en seguimiento de ruptura: los máximos oscilantes desencadenan cortos y los mínimos oscilantes desencadenan largos.

## Archivos

* `CS/ZigZagEvgeTrofiStrategy.cs` – Implementación en C# de la estrategia.
* La versión en Python no se proporciona intencionalmente.
