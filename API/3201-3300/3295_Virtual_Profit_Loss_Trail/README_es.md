# Estrategia Virtual Profit/Loss Trail
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`VirtualProfitLossTrailStrategy` reproduce dentro de StockSharp el comportamiento del asesor experto de MetaTrader "Virtual Profit Loss Trail". La estrategia nunca abre posiciones nuevas por sí misma. En su lugar, supervisa continuamente la posición actual del valor seleccionado y aplica lógica de protección:

- Una distancia configurable de take-profit expresada en pips.
- Una distancia configurable de stop-loss expresada en pips.
- Un trailing stop virtual que se activa después de alcanzar una ganancia mínima y se desliza con el mercado solo cuando el precio avanza por el paso trailing especificado.

Como los niveles de protección son virtuales, no se envían órdenes stop o limit reales a la bolsa. La estrategia monitoriza actualizaciones de mejor bid/ask y cierra la posición abierta con una orden de mercado cuando se toca cualquiera de los niveles virtuales.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Take-profit (pips)** | Distancia entre el precio de entrada y el objetivo de ganancia. Establecer en `0` para desactivar la salida take-profit. |
| **Stop-loss (pips)** | Distancia entre el precio de entrada y el stop de protección. Establecer en `0` para desactivar la salida stop-loss. |
| **Trailing stop (pips)** | Distancia usada para calcular el trailing stop. Cuando se establece en `0`, la lógica trailing se desactiva por completo. |
| **Trailing step (pips)** | Ganancia adicional que debe obtenerse antes de desplazar más el trailing stop. Usar `0` para mover el trail cada vez que se marca un nuevo máximo/mínimo. |
| **Trailing activation (pips)** | Ganancia mínima que debe bloquearse antes de que el trailing stop se active. Cuando se establece en `0`, trailing empieza inmediatamente después de entrar en la posición. |

Todas las distancias se miden en unidades de pip. La estrategia deriva automáticamente el tamaño de pip desde el paso de precio del valor: para símbolos con tres o cinco decimales, un pip se define como diez pasos de precio; de lo contrario, como un paso.

## Lógica

1. **Suscripción a datos de mercado** - la estrategia se suscribe a datos Level1 para recibir actualizaciones del mejor bid y mejor ask. Solo se procesan actualizaciones finalizadas, asegurando que el algoritmo funcione tanto en tiempo real como durante reproducciones históricas.
2. **Gestión de posición larga** - cuando la posición neta es larga, la estrategia calcula los niveles virtuales de stop-loss, take-profit y trailing stop a partir del precio medio de entrada. Si el mejor bid toca el stop-loss o take-profit, la posición se cierra inmediatamente. Cuando se alcanza la ganancia de activación, el trailing stop sigue el precio hacia arriba. El stop solo avanza cuando se cumple el requisito de paso trailing.
3. **Gestión de posición corta** - la misma lógica se aplica simétricamente usando el mejor ask para salidas de posiciones cortas.
4. **Comportamiento de reinicio** - cuando la posición se cierra por completo, las referencias internas de trailing se reinician para evitar señales accidentales de reentrada.

## Consejos de uso

- Adjunte la estrategia a un conector y valor que ya tenga una posición abierta o que vaya a recibir órdenes de otras estrategias o trading manual. El gestor controlará el tamaño agregado de la posición.
- Asegúrese de que haya datos Level1 disponibles; sin valores actuales bid/ask, los niveles virtuales no pueden evaluarse.
- La estrategia puede combinarse con cualquier estrategia generadora de entradas ejecutando ambas bajo la misma cartera y valor. Solo una instancia debe gestionar la lógica de protección para evitar conflictos.

## Diferencias con el experto MQL

- La versión StockSharp trabaja con posiciones agregadas en lugar de tickets de órdenes individuales. Calcula automáticamente el precio medio de entrada proporcionado por la plataforma.
- El dibujo de líneas visuales y alertas sonoras del experto original se sustituyen por logging dentro de StockSharp. Las acciones de protección son visibles en el diario de la estrategia.
- Se conserva la misma configuración basada en pips, incluido el umbral de activación trailing y el paso trailing incremental.

## Archivos

- `CS/VirtualProfitLossTrailStrategy.cs` - implementación C# de la estrategia.
- `README.md` - esta documentación.
- `README_zh.md` - traducción al chino simplificado.
- `README_ru.md` - traducción al ruso.
