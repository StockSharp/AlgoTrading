# Estrategia de Ruptura BollTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto original **BollTrade** operando rupturas de las Bandas de Bollinger con un búfer de pips configurable y dimensionamiento de posición opcional basado en el balance. Las órdenes se abren solo en velas completadas y se gestionan con niveles fijos de stop-loss y toma de ganancias.

## Concepto

- Se suscribe al marco temporal primario configurable y calcula un envelope de Bandas de Bollinger con el período y desviación especificados.
- Añade un desplazamiento adicional (`Band Offset`) medido en pips por encima de la banda superior y por debajo de la banda inferior para reducir entradas prematuras.
- Abre una posición **larga** cuando el cierre de la vela termina por debajo de la banda inferior menos el desplazamiento.
- Abre una posición **corta** cuando el cierre de la vela termina por encima de la banda superior más el desplazamiento.
- Solo puede estar activa una posición en cualquier momento. La estrategia espera a que la operación actual termine antes de evaluar nuevas entradas.

## Gestión de Operaciones

- Los niveles de stop-loss y toma de ganancias se establecen inmediatamente después de una entrada. Se expresan en múltiplos de pips y se evalúan en cada vela completada. Si el precio toca cualquiera de los niveles, la posición se cierra a mercado.
- Si `Scale Volume` está habilitado, el volumen negociado crece (o se reduce) con el balance de la cuenta. La línea base de escalado es el valor inicial del portafolio dividido por el tamaño base del lote, imitando la implementación MQL original. El volumen está limitado a 500 lotes para mantener el riesgo bajo control, igual que en el código fuente.
- El tamaño del pip se deriva del paso de precio del instrumento. Para pasos muy pequeños (símbolos tipo forex), el código multiplica el paso por 10 para convertir pasos de pip fraccionarios en pips estándar, coincidiendo con el comportamiento de la versión MetaTrader.

## Parámetros

| Nombre | Descripción | Valor predeterminado |
| ---- | ----------- | ------- |
| `Candle Type` | Marco temporal usado para las velas de señal. | Marco temporal de 15 minutos |
| `Bollinger Period` | Número de barras en el cálculo de las Bandas de Bollinger. | 4 |
| `Bollinger Deviation` | Multiplicador de ancho para las Bandas de Bollinger. | 2 |
| `Band Offset` | Desplazamiento adicional en pips añadido fuera de ambas bandas antes de activar señales. | 3 |
| `Take Profit (pips)` | Distancia al objetivo de ganancia en unidades de pips. | 3 |
| `Stop Loss (pips)` | Distancia al stop de protección en unidades de pips. | 20 |
| `Base Volume` | Volumen predeterminado en lotes cuando el escalado está deshabilitado. | 1 |
| `Scale Volume` | Cuando está habilitado, escala el tamaño de posición con el balance de la cuenta. | Habilitado |

## Notas de uso

- Funciona mejor en símbolos forex o CFD donde los desplazamientos basados en pips proporcionan niveles de ruptura claros, pero también puede ejecutarse en futuros o acciones siempre que su `PriceStep` esté configurado.
- La estrategia procesa solo velas terminadas, por lo que los picos intrabarra que revierten antes del cierre de la barra no activarán entradas.
- Dado que las salidas se manejan con stops y objetivos fijos, asegúrese de que esas distancias sean apropiadas para el marco temporal seleccionado y la volatilidad del instrumento.
- El EA original dependía de stops del lado del broker. Este puerto monitorea los extremos de las velas para emular el mismo comportamiento de protección dentro de StockSharp.

## Archivos

- `CS/BollTradeStrategy.cs` – implementación en C# de la estrategia.
- `README.md` – documentación en inglés (este archivo).
- `README_ru.md` – documentación en ruso.
- `README_zh.md` – documentación en chino.
