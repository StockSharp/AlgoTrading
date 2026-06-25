# Estrategia Exp RSIOMA V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Exp RSIOMA V2 es una conversión del asesor experto original de MetaTrader 5 que opera en el oscilador RSIOMA (Índice de Fuerza Relativa de la Media Móvil). La estrategia reproduce las mismas ideas dentro de la API de alto nivel de StockSharp: los datos de precio se suavizan, se convierten en una serie de momentum y se alimentan en un acumulador estilo RSI. Las decisiones de trading se toman cuando el oscilador cambia de dirección o cruza zonas predefinidas.

## Lógica de trading
1. **Preprocesamiento de precio** – el precio de vela seleccionado (cierre por defecto) se suaviza con una de cuatro familias de medias móviles (simple, exponencial, suavizada o lineal ponderada).
2. **Cálculo de momentum** – el precio suavizado se compara con el valor de `MomentumPeriod` barras atrás para obtener el impulso de momentum.
3. **Computación RSIOMA** – los componentes de momentum positivo y negativo se acumulan con un suavizado exponencial de longitud `RsiomaLength`, produciendo el valor RSIOMA en el rango `[0; 100]`.
4. **Evaluación de señales** – las velas cerradas más recientes se inspeccionan según el `Mode` elegido:
   - **Breakdown** – reacciona cuando RSIOMA abandona los niveles de tendencia principal (`MainTrendLong` / `MainTrendShort`). Cuando el oscilador sale de la zona superior, los cortos se cierran y se permiten entradas largas; salir de la zona inferior realiza la acción opuesta.
   - **Twist** – busca puntos de giro. Una compra ocurre cuando la pendiente de RSIOMA cambia de descendente a ascendente, mientras que las ventas reaccionan a una transición de ascendente a descendente.
   - **CloudTwist** – emula la lógica de nube coloreada del indicador MT5. Los trades se abren cuando RSIOMA regresa de extremos de sobrecompra/sobreventa de vuelta dentro del canal, y las posiciones opuestas se cierran al mismo tiempo.

Las señales se evalúan en la barra especificada por `SignalBar` (por defecto: la vela completamente cerrada anterior), asegurando que solo se usen datos confirmados.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
|--------|-------------|----------------------|
| `OrderVolume` | Volumen de orden predeterminado utilizado por las órdenes de mercado. | `1` |
| `CandleType` | Serie de datos de velas procesada por la estrategia. | Marco temporal de `4 horas` |
| `EnableLongEntries` / `EnableShortEntries` | Permitir la apertura de nuevas posiciones largas/cortas. | `true` |
| `EnableLongExits` / `EnableShortExits` | Permitir el cierre de posiciones largas/cortas existentes. | `true` |
| `Mode` | Lógica de trading (Breakdown, Twist o CloudTwist). | `Breakdown` |
| `PriceSmoothing` | Media móvil aplicada al precio antes de RSIOMA. | `Exponential` |
| `RsiomaLength` | Período de promediado RSIOMA. | `14` |
| `MomentumPeriod` | Retraso entre muestras al computar momentum. | `1` |
| `AppliedPrice` | Precio de vela usado para el oscilador (cierre, apertura, mediana, DeMark, etc.). | `Close` |
| `MainTrendLong` / `MainTrendShort` | Niveles RSIOMA que definen zonas de sobrecompra/sobreventa. | `60` / `40` |
| `SignalBar` | Número de barras cerradas atrás que deben analizarse. | `1` |

## Notas de implementación
- Solo se admiten las familias de suavizado disponibles en StockSharp (simple, exponencial, suavizada y lineal ponderada). Los modos avanzados de la versión MT5 (JJMA, VIDYA, AMA, …) no están incluidos.
- Los promedios RSI se inicializan usando los primeros `RsiomaLength` valores de momentum para reflejar la inicialización de MetaTrader. Después se aplica una actualización exponencial, coincidiendo con el comportamiento del asesor experto original.
- Las posiciones siempre se cierran antes de emitir una entrada opuesta. Los permisos de entrada (`EnableLongEntries`, `EnableShortEntries`) y los permisos de salida (`EnableLongExits`, `EnableShortExits`) proporcionan control total sobre las direcciones permitidas.
- `SignalBar = 0` se puede usar para reaccionar a la vela finalizada actual; valores más altos reproducen la capacidad de MT5 de esperar varias barras antes de actuar.

## Uso
1. Agregar la estrategia a un proyecto StockSharp y asignar el instrumento que desea operar.
2. Configurar la suscripción de velas a través de `CandleType` (por defecto velas de 4 horas) y ajustar umbrales si el símbolo usa características de volatilidad diferentes.
3. Seleccionar el modo de señal preferido dependiendo de si desea entradas estilo ruptura (`Breakdown`), giros de momentum (`Twist`) o cambios de color de nube (`CloudTwist`).
4. Iniciar la estrategia. Durante la ejecución la estrategia se suscribe a la serie de velas elegida, computa la cadena RSIOMA y emite órdenes de mercado cuando se satisfacen las condiciones.
