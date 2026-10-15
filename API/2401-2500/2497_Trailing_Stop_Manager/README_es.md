# Estrategia Trailing Stop Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el controlador de trailing stop del experto MetaTrader `MQL/17263/TrailingStop.mq5`. Se centra en automatizar la gestión del stop-loss después de que ya se ha abierto una entrada.

## Idea original
- **Fuente**: El experto TrailingStop de Vladimir Karputov para cuentas de cobertura.
- **Concepto**: En el primer tick el EA abría posiciones tanto largas como cortas, luego ajustaba los niveles de stop-loss de forma independiente para cada lado usando distancias basadas en pips.
- **Objetivo**: Demostrar cómo hacer trailing de stops con una distancia de activación y un paso de actualización configurables.

## Adaptación a StockSharp
- **Compatibilidad con netting**: Las estrategias de StockSharp operan sobre la posición neta, por lo que este port gestiona una dirección a la vez. Para hacer trailing de ambos lados simultáneamente, inicia dos instancias de la estrategia.
- **Actualizaciones basadas en ticks**: La estrategia se suscribe a ticks de trades (`DataType.Ticks`) para reflejar los ajustes dirigidos por ticks de MetaTrader.
- **Conversión de pips**: Multiplica los valores de pip configurados por `Security.PriceStep` (recurre a 1 si el mercado no proporciona un paso) para convertir las entradas en offsets de precio absolutos.
- **Auto-entrada opcional**: Un parámetro permite enviar una orden de mercado inmediata al iniciar, lo cual es útil para demostraciones rápidas o pruebas manuales.

## Lógica de trading
1. **Inicio**
   - Lee el paso de precio del instrumento y se suscribe a datos de ticks.
   - Opcionalmente envía una orden de mercado según el parámetro `Initial Direction`.
2. **Seguimiento de entrada**
   - Cada trade propio reinicia el estado del trailing y almacena el precio de ejecución real como nueva referencia.
3. **Activación**
   - Para posiciones largas el motor de trailing se activa solo después de que el precio avanza `Trailing Stop (pips)` desde la entrada. Para cortos requiere una caída equivalente.
4. **Ajuste del stop**
   - Una vez activado, el nivel del stop equivale al precio del tick actual menos/más la distancia de activación.
   - El stop se mueve solo si el último tick lo empuja hacia adelante al menos `Trailing Step (pips)`.
   - Un paso cero significa que el stop se actualiza en cada tick favorable.
5. **Salida**
   - Cuando el precio vuelve al nivel de trailing o lo supera, la estrategia cierra la posición restante con una orden de mercado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| **Trailing Stop (pips)** | Distancia de activación en pips. Debe ser mayor que cero. |
| **Trailing Step (pips)** | Movimiento favorable mínimo en pips antes de avanzar el stop nuevamente. Puede ser cero. |
| **Initial Direction** | Orden de mercado opcional colocada durante `OnStarted` (`None`, `Long`, `Short`). |

## Notas adicionales
- El experto original usaba valores de bid/ask. Esta versión en C# usa el último precio de trade como una aproximación cercana, que es suficiente para la mayoría de los instrumentos líquidos.
- No se incluye lógica de take-profit ni de nueva entrada. Puedes combinar este componente con otra estrategia de señales o lanzarlo manualmente después de abrir una posición.
- Si el broker proporciona pasos de pip fraccionarios, asegúrate de que `Security.PriceStep` los refleje; de lo contrario, ajusta los valores de pip para que coincidan con el tamaño real del tick.
- No hay pruebas automatizadas para este módulo, así que valida en un feed de demo antes de desplegar capital real.
