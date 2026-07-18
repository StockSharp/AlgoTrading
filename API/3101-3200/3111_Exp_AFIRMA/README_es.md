# Estrategia Exp AFIRMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General

La **Estrategia Exp AFIRMA** reproduce el asesor experto de MetaTrader `Exp_AFIRMA.mq5` usando la API de alto nivel de
StockSharp. El sistema original se basa en el indicador AFIRMA (Adaptive Finite Impulse Response Moving Average) que combina
un suavizador FIR con ventana y una previsión ARMA corta. La versión de StockSharp mantiene la misma lógica de mercado: abre
posiciones largas cuando el componente ARMA gira al alza y cierra o invierte cuando la previsión cae al lado bajista.

Las decisiones de trading se toman en velas completadas de un marco temporal configurable (por defecto: H4). La estrategia
evalúa los valores ARMA de varias barras cerradas para confirmar un cambio de pendiente. Las órdenes se colocan a mercado
con stops y objetivos de protección opcionales implementados a través de la gestión de riesgos de StockSharp.

## Lógica de Trading

1. **Cálculo del indicador**
   - El `AfirmaIndicator` integrado recrea el filtro AFIRMA de dos etapas. Un suavizador FIR con ventana (longitud = `Taps`,
     ancho de banda = `Periods`) produce una media móvil base.
   - La previsión ARMA se calcula a través de los mismos coeficientes de mínimos cuadrados que en el fuente MQL. El indicador
     expone los valores FIR y ARMA; la estrategia solo consume el componente ARMA.
2. **Evaluación de señales**
   - En cada vela terminada se almacena el valor ARMA más reciente. El parámetro `SignalBar` (por defecto: 1) especifica
     cuántas barras ya cerradas deben omitirse.
   - **Setup alcista**: el valor ARMA previo es menor que su predecesor (`ARMA[t-2] < ARMA[t-3]`) y el valor más reciente
     está por encima del anterior (`ARMA[t-1] > ARMA[t-2]`). Esto cierra la exposición corta y abre/extiende una posición
     larga si está permitido.
   - **Setup bajista**: el valor ARMA previo es mayor que su predecesor mientras el valor más reciente está por debajo.
     Esto cierra la exposición larga y abre/extiende una posición corta si está permitido.
3. **Gestión de posiciones**
   - Solo se mantiene una posición. Las nuevas entradas llevan la posición hacia `±TradeVolume`. La exposición existente se
     cierra antes de invertir.
   - La protección de riesgo opcional usa `StartProtection` con distancias de stop-loss y take-profit basadas en precio.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Tamaño de posición base usado para entradas largas y cortas. |
| `CandleType` | Marco temporal/tipo de datos solicitado del adaptador de datos de mercado (por defecto: velas de 4 horas). |
| `Periods` | Ancho de banda recíproco de la etapa FIR (`1 / (2 * Periods)`), idéntico a la entrada del EA original. |
| `Taps` | Número de coeficientes FIR. Ajustado internamente al valor impar más cercano si es necesario. |
| `Window` | Función de ventana aplicada al filtro FIR (`Rectangular`, `Hanning1`, `Hanning2`, `Blackman`, `BlackmanHarris`). |
| `SignalBar` | Número de barras ya cerradas para mirar atrás en busca de confirmación. `1` corresponde a la última barra completamente cerrada. |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir apertura de posiciones largas/cortas. |
| `EnableBuyExits` / `EnableSellExits` | Permitir cierre de posiciones largas/cortas en señales opuestas. |
| `StopLossPoints` | Stop de protección opcional expresado en unidades de precio. |
| `TakeProfitPoints` | Objetivo de protección opcional expresado en unidades de precio. |

## Notas sobre la Conversión

- Las opciones de gestión de dinero (`MM`, `MMMode`, `Deviation_`) de la versión MetaTrader son reemplazadas con el
  parámetro más simple `TradeVolume`.
- El EA original envía valores de stop-loss y take-profit en puntos. Aquí se proporcionan en unidades de precio absolutas.
  Convierta puntos a precio multiplicando por el paso de precio apropiado.
- Cuando `SignalBar = 1`, la estrategia lee los últimos tres valores ARMA **completados** y abre órdenes en la siguiente
  barra. Establecer `SignalBar = 0` sigue funcionando pero usa la barra más recientemente cerrada.
- La implementación del indicador AFIRMA coincide con la matemática original, incluyendo los tipos de ventana y fórmulas de
  coeficientes soportados.

## Consejos de Uso

1. Conecte la estrategia a un instrumento y cartera, configure `TradeVolume` y seleccione el marco temporal a través de
   `CandleType`.
2. Habilite o deshabilite las direcciones largo/corto según su plan de trading.
3. Establezca `StopLossPoints` y `TakeProfitPoints` si desea gestión de riesgo automatizada; de lo contrario déjelos en
   cero para operar sin salidas fijas.
4. Monitorice el gráfico generado para verificar las líneas AFIRMA y las operaciones ejecutadas al ajustar `Periods`,
   `Taps` y `SignalBar`.
