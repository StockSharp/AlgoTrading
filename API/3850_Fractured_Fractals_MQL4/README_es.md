# Estrategia fracturada Fractals (MT4)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puerto C# detallado del asesor experto clásico MetaTrader 4 `MQL/7696/Fractured_fractals.mq4`. La estrategia vigila a los recién confirmados
Williams niveles fractales, colas de órdenes de parada de ruptura y senderos corren el riesgo de utilizar las oscilaciones fractales anteriores. El tamaño de la posición sigue el
Lógica original de riesgo por operación con la reducción de volumen adaptativa "DecreaseFactor" después de las reducciones.

## Detalles

- **Fuente**: Convertido de `MQL/7696/Fractured_fractals.mq4`.
- **Régimen de mercado**: Continuación de ruptura, funciona en cualquier instrumento que forme estructuras fractales confiables.
- **Tipos de órdenes**: utiliza órdenes de parada para entradas y órdenes de parada de protección para salidas.
- **Dimensionamiento de la posición**: modelo de riesgo porcentual controlado por `MaximumRiskPercent` con amortiguación de la racha de pérdidas a través de `DecreaseFactor`.
- **Parámetros predeterminados**:
  - `MaximumRiskPercent` = 2%
  - `DecreaseFactor` = 3
  - `CandleType` = período de tiempo de 1 hora
- **Indicadores principales**: Detección fractal nativa de cinco barras Williams implementada en la estrategia.
- **Tipo de estrategia**: Ruptura simétrica larga/corta con paradas finales basadas en fractales.

## Lógica de la estrategia

### Detección de fractales

- Mantiene una ventana móvil de cinco máximos y mínimos de velas para reproducir los buffers `iFractals` de MetaTrader.
- Se confirma un nuevo fractal ascendente cuando el máximo medio excede los dos máximos circundantes en cada lado; un fractal hacia abajo requiere el
medio bajo para ser el más bajo en la secuencia de cinco compases.
- Cuando aparece un nuevo fractal, se almacena junto con los tres valores anteriores, reflejando los EA, `pfu` y
`pfu.1` buffers de estilo para comparaciones posteriores y cálculos finales.

### Configuración de entrada

- Las operaciones largas requieren que el fractal ascendente más reciente supere al anterior y el último fractal a la baja para definir un piso de riesgo.
Luego, la estrategia coloca un stop de compra ligeramente por encima del fractal (compensación del diferencial) con un stop de protección por debajo del oponente.
abajo fractal.
- Las operaciones cortas reflejan la lógica: un fractal inferior combinado con un fractal superior genera una parada de venta y una barrera protectora.
deténgase por encima del fractal superior más la extensión.
- Sólo se permite una orden pendiente por dirección. Si la estructura fractal invalida el patrón (por ejemplo, el último fractal no
ya supera la anterior: la orden pendiente se cancela inmediatamente.

### Detener la gestión

- Una vez posicionado, el robot sigue la parada protectora usando el fractal anterior en el lado de entrada, restando/sumando el actual
difundir. La parada sólo se mueve a favor de la operación.
- Cuando la dirección de la posición cambia o se cierra, la orden de parada no utilizada se cancela para evitar una exposición obsoleta.

### Gestión de riesgos

- `CalculateOrderVolume` replica el cálculo de riesgo por operación de EA: el tamaño de la posición es la relación entre la provisión de riesgo monetario y
la distancia entre los niveles de entrada y parada.
- La valoración de la cuenta prefiere `Portfolio.CurrentValue`; si no está disponible, la rutina vuelve a la propiedad `Volume` de la estrategia
multiplicado por el precio.
- Después de dos o más operaciones perdedoras consecutivas, el volumen se reduce en `losses / DecreaseFactor`, emulando el MetaTrader
`DecreaseFactor` comportamiento.

### Seguimiento del ciclo comercial

- Los agregados `OnOwnTradeReceived` completan los ciclos comerciales, rastrean el PnL flotante y actualizan la racha de pérdidas una vez que regresa el volumen.
a plano. Esto mantiene la lógica de riesgo alineada con el experto MT4 donde se utilizó `HistoryTotal` para analizar resultados anteriores.

## Notas de uso

1. Adjunte la estrategia a cualquier par de valores/cartera y elija una resolución `CandleType` adecuada que coincida con la original.
EA configuración.
2. Asegúrese de que las cotizaciones de nivel 1 estén disponibles: la estimación del diferencial se basa en la mejor oferta/demanda; si no está disponible, la estrategia vuelve a
`PriceStep`.
3. Las órdenes de suspensión suponen que el corredor admite paradas del lado del servidor. Reemplace el registro `BuyStop`/`SellStop` con órdenes de mercado si
requerido por su adaptador.
4. Debido a que el procesamiento ocurre al cierre de la vela, las señales fractales intrabar solo actúan al final de cada barra, reproduciendo el
Evaluación barra por barra del asesor experto.
