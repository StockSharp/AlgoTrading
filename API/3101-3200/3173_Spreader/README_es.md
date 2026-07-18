# Estrategia de Spreader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Spreader** es un enfoque de trading en pares inspirado en el asesor experto original de MetaTrader "Spreader". La estrategia monitoriza dos instrumentos con correlación positiva y busca beneficiarse de divergencias a corto plazo manteniendo un perfil neutral al mercado. Una vez que la posición combinada alcanza el objetivo monetario deseado, la estrategia cierra ambas patas y espera la siguiente configuración.

El algoritmo está diseñado para velas de un minuto por defecto, replicando el comportamiento del EA original, pero el marco temporal puede ajustarse cuando la estrategia se carga en Designer, Shell o el ejecutor de la API.

## Lógica de trading

1. **Preparación de datos**
   - Se suscribe a las velas del instrumento principal (el asignado a la estrategia) y del instrumento secundario.
   - Almacena los últimos `2 * ShiftLength + 1` valores de cierre para cada instrumento. La longitud de desplazamiento predeterminada es 30 barras.
   - Solo reacciona a velas completadas y requiere que ambos instrumentos produzcan una barra con el mismo tiempo de apertura.

2. **Filtro de tendencia**
   - Calcula los cambios de precio entre el cierre actual y el cierre `ShiftLength` barras atrás, así como el cambio entre las muestras medias y más antiguas para ambos instrumentos.
   - Si los dos cambios de cualquiera de los instrumentos tienen el mismo signo, la estrategia lo interpreta como una tendencia persistente y omite la operación.

3. **Verificación de correlación**
   - Asegura que el signo del último cambio en ambos instrumentos sea idéntico. Si el signo difiere, la correlación se considera negativa y no se abre ningún spread.

4. **Alineación de volatilidad**
   - Calcula la magnitud absoluta de las oscilaciones recientes (`a` para la pata principal, `b` para la secundaria) y usa su ratio para escalar el volumen de cobertura. Los ratios fuera del rango `[0.3, 3]` son rechazados porque indican comportamiento inestable.

5. **Entrada**
   - Elige la dirección de la pata principal comparando las oscilaciones normalizadas: si el movimiento principal es más fuerte, la estrategia compra el instrumento principal y vende la pata secundaria; de lo contrario, vende la pata principal y compra la secundaria.
   - Las órdenes se envían con ejecución de mercado y los volúmenes se normalizan para respetar el paso de lote y los límites mínimos y máximos de cada instrumento.

6. **Gestión de posiciones**
   - Si solo la pata secundaria está abierta (por ejemplo, debido a problemas de conectividad), la estrategia abre la pata principal faltante en la dirección opuesta para restaurar la cobertura.
   - Si solo queda la pata principal, se cierra inmediatamente para evitar exposición direccional.
   - Cuando ambas patas están presentes, la estrategia monitoriza el beneficio flotante combinado y cierra ambas posiciones una vez alcanzado el objetivo monetario configurado.

7. **Verificaciones de seguridad**
   - El trading se desactiva cuando el tamaño del contrato (multiplicador) de los dos instrumentos difiere, ya que el EA original asume especificaciones contractuales iguales.
   - Todas las solicitudes de trading se ignoran hasta que la estrategia esté conectada, sincronizada y autorizada para operar por el entorno de alojamiento (`IsFormedAndOnlineAndAllowTrading`).

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `SecondSecurity` | Instrumento que forma la pata de cobertura del spread. Este parámetro es obligatorio. |
| `PrimaryVolume` | Volumen de orden base para el instrumento principal. El volumen secundario se escala usando el ratio de oscilación. |
| `TargetProfit` | Beneficio absoluto, expresado en la moneda de la cuenta, tras el cual se cierran ambas patas. |
| `ShiftLength` | Número de barras usadas al comparar oscilaciones recientes. La estrategia usa `2 * ShiftLength + 1` velas de cada instrumento. |
| `CandleType` | Series de velas usadas para el análisis. Por defecto marco temporal de 1 minuto. |

## Consejos

- La estrategia funciona mejor en instrumentos con correlación positiva estable y perfiles de volatilidad similares (por ejemplo, pares de divisas muy relacionados o futuros sobre índices).
- Las especificaciones del contrato deben estar alineadas (tamaño del tick, paso de lote, margen); de lo contrario, la normalización del volumen puede reducir significativamente el tamaño de las órdenes.
- Dado que la estrategia depende de datos de velas, asegúrese de que ambos instrumentos reciban actualizaciones de barra sincronizadas del proveedor de datos.

## Requisitos

- Dos instrumentos líquidos con correlación positiva.
- Acceso a datos de mercado y permisos de trading para ambos instrumentos a través de los conectores de StockSharp.
- Cartera asignada a la instancia de la estrategia.
