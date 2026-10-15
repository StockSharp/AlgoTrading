# Estrategia Exp Skyscraper Fix ColorAML
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia recrea el asesor experto de MetaTrader 5 **Exp_Skyscraper_Fix_ColorAML** dentro del framework StockSharp. Combina
dos generadores de señales independientes:

1. **Skyscraper Fix** – un canal basado en ATR que pinta regímenes alcistas o bajistas dependiendo de la dirección de las bandas
   adaptativas.
2. **ColorAML** – un oscilador adaptativo de niveles de mercado que compara rangos fractales locales para detectar fases de
   expansión o contracción.

La implementación MQL original gestionaba dos magic numbers separados y podía mantener posiciones hedgeadas simultáneamente.
Las estrategias de StockSharp operan sobre una posición neta, por lo que las señales conflictivas simplemente se compensan entre
sí y la última entrada define la exposición. El README destaca estas diferencias para que los usuarios alineen sus expectativas
al hacer backtesting o al operar con la variante convertida.

## Parámetros
### Módulo Skyscraper Fix
- **SkyscraperCandleType** – marco temporal utilizado para construir el indicador Skyscraper Fix. Por defecto: velas `4h`.
- **SkyscraperEnableLongEntry / SkyscraperEnableShortEntry** – permiten al módulo abrir posiciones largas o cortas.
- **SkyscraperEnableLongExit / SkyscraperEnableShortExit** – permiten al módulo cerrar operaciones abiertas en la dirección
  correspondiente.
- **SkyscraperLength** – número de muestras ATR utilizadas para determinar el tamaño del paso escalonado. Por defecto: `10` barras.
- **SkyscraperMultiplier** – coeficiente aplicado al paso basado en ATR. Por defecto: `0.9`.
- **SkyscraperPercentage** – desplazamiento porcentual opcional aplicado a la línea media (0 desactiva el desplazamiento).
- **SkyscraperMode** – elige entre la construcción del canal basada en High/Low o en Close.
- **SkyscraperSignalBar** – número de velas completadas a examinar al leer el búfer de colores. Los valores deben ser al menos `1`.
- **SkyscraperVolume** – volumen de la orden de mercado solicitada en cada entrada.
- **SkyscraperStopLoss / SkyscraperTakeProfit** – distancias de protección expresadas en pasos de precio.

### Módulo ColorAML
- **ColorAmlCandleType** – marco temporal utilizado por el oscilador ColorAML. Por defecto: velas `4h`.
- **ColorAmlEnableLongEntry / ColorAmlEnableShortEntry** – habilitan nuevas entradas largas o cortas.
- **ColorAmlEnableLongExit / ColorAmlEnableShortExit** – habilitan órdenes de cierre para la dirección respectiva.
- **ColorAmlFractal** – longitud del rango fractal utilizado para construir los niveles adaptativos. Por defecto: `6` barras.
- **ColorAmlLag** – parámetro de lag que controla el suavizado exponencial. Por defecto: `7`.
- **ColorAmlSignalBar** – número de velas completadas a inspeccionar en el búfer de colores.
- **ColorAmlVolume** – volumen de la orden para entradas impulsadas por ColorAML.
- **ColorAmlStopLoss / ColorAmlTakeProfit** – distancias de protección en pasos de precio.

## Lógica de trading
La estrategia se suscribe a las series de velas solicitadas para cada módulo y evalúa solo las velas terminadas. Ambos indicadores
están implementados en C# siguiendo las definiciones matemáticas del código MQL original:

- **Skyscraper Fix** calcula un canal similar a SuperTrend. Cuando el búfer de color se vuelve **teal (0)** el módulo cierra
  cualquier exposición corta (si está permitido) y, cuando el color anterior era diferente, prepara una entrada larga. Cuando el
  búfer cambia a **firebrick (1)** cierra largos y programa una entrada corta.
- **ColorAML** compara rangos fractales para construir una línea de nivel adaptativo. El color `2` señala expansión alcista,
  cerrando cortos y opcionalmente abriendo largos. El color `0` señala contracción bajista, cerrando largos y opcionalmente
  abriendo cortos. El color neutro `1` mantiene la postura actual.

Cada entrada utiliza órdenes de mercado dimensionadas como `VolumenConfigurado + |posición actual|`. Esto asegura que una orden
de reversión simultáneamente cierre la exposición opuesta y establezca la nueva posición cuando el hedging no está disponible.

## Gestión de riesgo
`StartProtection()` se activa al inicio. Siempre que un módulo abre una nueva posición, la estrategia almacena el precio de
entrada y calcula los niveles de stop-loss y take-profit usando la configuración específica del módulo. Las velas subsiguientes
desencadenan salidas si su máximo o mínimo perfora los umbrales configurados. Establecer las distancias a cero desactiva la
lógica de protección.

## Notas de implementación
- Los cálculos de Skyscraper Fix y ColorAML fueron portados directamente y se ejecutan en búferes internos de velas. No es
  necesario agregar indicadores externos manualmente a la estrategia.
- StockSharp mantiene una única posición neta por estrategia. Como resultado, las operaciones simultáneas largas y cortas del EA
  original se netean. Los usuarios que dependían del hedging deben tener en cuenta esta diferencia.
- Solo se procesan las velas completadas. `SignalBar` debe ser al menos `1`; la evaluación intrabar (tick a tick) no se reproduce.
- Los stops se aplican monitoreando los extremos de las velas en lugar de órdenes del lado del servidor, lo que coincide con el
  comportamiento del framework convertido.

## Uso
1. Adjunte la estrategia al instrumento y al portafolio deseados.
2. Configure los parámetros de ambos módulos, alineando los tipos de velas con los datos disponibles.
3. Inicie la estrategia. Se suscribirá automáticamente a las velas necesarias, calculará los colores de los indicadores y
   colocará órdenes de mercado según las señales del módulo.
4. Monitoree el log o los gráficos para observar cambios de régimen, eventos de gestión de riesgo manual y operaciones ejecutadas.
