# Ichimoku Estrategia 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación directa del MetaTrader asesor experto `ichimok2005` diseñado para la API de alto nivel de StockSharp. Se centra en identificar rupturas decisivas por encima o por debajo de la línea Ichimoku Senkou Span B y confirma el impulso a través de cuerpos de velas consecutivos.

## Lógica de trading

### Configuración larga
1. Evalúe las últimas `Shift + 2` velas completadas (el `Shift` predeterminado es `1`, por lo que el algoritmo observa las tres barras anteriores).
2. Requerir que:
   - La vela de referencia más antigua (`Shift + 2`) se abrió debajo de Senkou Span B.
   - La vela de referencia intermedia (`Shift + 1`) se abrió por encima de Senkou Span B y cerró por encima de ella.
   - La vela de referencia más reciente (`Shift`) se abrió y cerró por encima de Senkou Span B.
   - Las dos últimas velas de referencia son alcistas (el precio de cierre es mayor que el precio de apertura).
3. Asegúrese de que Ichimoku Chinkou Span no quede atrapado dentro de la nube cuando Senkou Span A esté por debajo de Senkou Span B. Esto imita el filtro de asesor experto original que evita fases de mercado congestionadas.
4. Si la estrategia actualmente tiene una posición corta, se cierra. De lo contrario, se abre una nueva operación larga, siempre que la señal anterior no fuera ya larga.

### Configuración corta
1. Refleje las condiciones largas en la dirección opuesta:
   - La vela `Shift + 2` debe abrirse sobre Senkou Span B.
   - La vela `Shift + 1` debe abrirse y cerrarse debajo de Senkou Span B.
   - La vela `Shift` debe abrirse y cerrarse debajo de Senkou Span B.
   - Las dos últimas velas de referencia son bajistas (el precio de cierre es menor que el precio de apertura).
2. El Chinkou Span debe permanecer fuera de la nube cuando el Senkou Span A está por debajo del Senkou Span B.
3. Cierre cualquier posición larga existente y luego abra una nueva posición corta si la señal anterior no era corta.

Las posiciones se gestionan con las órdenes de protección de StockSharp. Stop Loss y Take Profit se miden en pasos de precio y se convierten a distancias absolutas utilizando el `PriceStep` del instrumento. Las órdenes de protección se registran con salidas de mercado para replicar el comportamiento MetaTrader de utilizar paradas de mercado.

## Dimensionamiento de posiciones

El asesor original admitía dos modos de tamaño:
- **Volumen fijo** (`UseMoneyManagement = false`): las operaciones se ejecutan con el parámetro `OrderVolume` (por defecto 0,1 lotes).
- **Gestión de dinero** (`UseMoneyManagement = true`): la estrategia utiliza el valor actual de la cartera y el porcentaje `MaximumRisk` para derivar el tamaño del pedido. El resultado se ajusta al paso del lote del valor y nunca cae por debajo de un solo paso.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `StopLossPoints` | Distancia de stop-loss en pasos de precio. | 30 |
| `TakeProfitPoints` | Distancia de obtención de beneficios en pasos de precio. | 60 |
| `Shift` | Número de barras utilizadas como compensación al validar la estructura de ruptura. | 1 |
| `OrderVolume` | Tamaño de operación fijo cuando la administración del dinero está deshabilitada. | 0.1 |
| `MaximumRisk` | Porcentaje de cartera utilizado para dimensionar los pedidos cuando la administración del dinero está habilitada. | 10 |
| `UseMoneyManagement` | Permite dimensionar las posiciones en función del riesgo. | falso |
| `TenkanPeriod` | Período tenkan-sen del indicador Ichimoku. | 9 |
| `KijunPeriod` | Período Kijun-sen del indicador Ichimoku. | 26 |
| `SenkouBPeriod` | Período Senkou Span B del indicador Ichimoku. | 52 |
| `CandleType` | Marco de tiempo para todos los cálculos (el valor predeterminado es velas por hora). | 1 hora |

## Notas

- Sólo se procesan velas completadas, garantizando que los valores Ichimoku sean definitivos.
- La estrategia realiza un seguimiento de la última dirección ejecutada (`_lastSignal`) para evitar repetir órdenes idénticas en señales consecutivas, coincidiendo con el comportamiento experto MetaTrader.
- Si el instrumento no publica `PriceStep`, las distancias de stop-loss y take-profit se tratan como valores de precio absoluto.
