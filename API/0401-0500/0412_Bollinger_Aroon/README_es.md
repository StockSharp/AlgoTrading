# Estrategia de Bollinger Aroon
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Bollinger Aroon busca retrocesos dentro de una tendencia alcista fuerte.
Cuando el precio se estira por debajo de la banda inferior de Bollinger pero el valor
de Aroon Up permanece elevado, el sistema asume que la tendencia está intacta y busca
una reversión hacia la media. Solo opera en largo, buscando capturar el rebote
después de una caída temporal.

La configuración se activa después de que una vela terminada cierra por debajo de la
banda inferior mientras *Aroon Up* supera el nivel de confirmación. La posición
permanece abierta hasta que la lectura de Aroon cae por debajo de un umbral de stop
o el precio sube hasta la banda superior. El ancho de banda se adapta a la
volatilidad, permitiendo que la estrategia opere en mercados tranquilos y activos
por igual.

Las pruebas retrospectivas en los principales pares cripto muestran que el enfoque
destaca durante tendencias fuertes con sacudidas ocasionales. Dado que las entradas
requieren tanto expansión de volatilidad como una lectura persistente de Aroon Up,
las señales falsas se reducen en comparación con una reversión Bollinger simple.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Cierre por debajo de la banda inferior Y `Aroon Up` > nivel de confirmación.
  - **Corto**: no se utiliza.
- **Criterios de salida**:
  - Cierre toca la banda superior O `Aroon Up` < nivel de stop.
- **Stops**: Basados en indicador; sin stop fijo por defecto.
- **Valores predeterminados**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `AroonLength` = 288
  - `AroonConfirmation` = 90
  - `AroonStop` = 70
- **Filtros**:
  - Categoría: Reversión a la media dentro de tendencia
  - Dirección: Solo largos
  - Indicadores: Bollinger Bands, Aroon
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
