# Estrategia de Barra Interior Pequeña
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Barra Interior Pequeña busca un patrón compacto de barra interior seguido de un cambio de momentum entre dos velas consecutivas. El experto original de MetaTrader 5 fue traducido a la API de alto nivel de StockSharp y ahora opera solo en velas completadas. El enfoque está diseñado para traders que prefieren entradas de estilo ruptura activadas por fases de volatilidad comprimida.

## Definición del patrón
La estrategia evalúa las dos velas completadas más recientes:

1. **Condición de barra interior** – la última vela finalizada debe estar completamente contenida dentro del rango de la anterior.
2. **Filtro de ratio de rango** – el rango de la barra madre (hace dos barras) debe ser al menos un múltiplo configurable del rango de la barra interior. El ratio predeterminado es 2:1.
3. **Filtros direccionales** –
   - Una configuración larga requiere una barra interior alcista formándose en la mitad inferior de la barra madre junto con una barra madre bajista.
   - Una configuración corta requiere una barra interior bajista formándose en la mitad superior de la barra madre junto con una barra madre alcista.
4. La inversión opcional intercambia las interpretaciones larga y corta mientras mantiene los mismos requisitos geométricos.

## Manejo de posición
El parámetro `OpenMode` refleja el comportamiento del EA original:

- **AnySignal** – envía una nueva orden de mercado en cada señal. Cuando existe una posición opuesta, se compensa parcialmente porque StockSharp usa cuentas de compensación.
- **SwingWithRefill** – aplana la exposición opuesta antes de entrar y permite múltiples adiciones en la misma dirección.
- **SingleSwing** – mantiene como máximo una operación direccional; las nuevas señales se ignoran mientras hay una posición alineada abierta.

Tanto las entradas largas como las cortas pueden habilitarse de forma independiente. El trading de inversión simplemente invierte qué configuración produce órdenes largas o cortas.

## Parámetros
| Nombre | Predeterminado | Descripción |
|--------|----------------|-------------|
| `CandleType` | Marco temporal de 1 hora | Suscripción de velas usada para la detección de patrones. |
| `RangeRatioThreshold` | 2.0 | Ratio mínimo de rango madre a interior. |
| `EnableLong` | true | Permitir operaciones alcistas. |
| `EnableShort` | true | Permitir operaciones bajistas. |
| `ReverseSignals` | false | Intercambiar las direcciones de patrón larga y corta. |
| `OpenMode` | SwingWithRefill | Controla cómo se maneja la exposición existente ante una nueva señal. |

## Lógica de trading
1. Suscribirse a la serie de velas configurada y esperar a las barras finalizadas.
2. Mantener las últimas dos velas completadas para evaluar el patrón.
3. Cuando el patrón y los filtros de ratio se alinean, determinar la señal direccional, aplicando opcionalmente la inversión.
4. Confirmar que el trading está permitido (`IsFormedAndOnlineAndAllowTrading`) y que la dirección relevante está habilitada.
5. Calcular el tamaño de la orden según el `OpenMode` seleccionado y enviar una orden de mercado usando el volumen base de la estrategia.
6. Actualizar el historial de velas interno para que la vela más nueva forme parte del próximo ciclo de evaluación.

## Notas de implementación
- La estrategia usa `StartProtection()` para habilitar el gestor de riesgo integrado (sin valores predefinidos de stop o take-profit). Pueden añadirse filtros adicionales externamente si es necesario.
- El estado del indicador no se almacena en colecciones; solo se mantienen las dos últimas velas según se requiere para el patrón.
- El algoritmo depende únicamente de velas completadas, evitando cálculos intra-barra en línea con las mejores prácticas de la API de alto nivel.
