# Estrategia DCA de Reversión a la Media con Bollinger Band
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra una cantidad fija en dólares cuando el precio cruza por debajo de la banda inferior de Bollinger o el primer día de cada mes. Todas las posiciones se cierran en una fecha especificada.

### Condiciones exactas C# y Python

Predeterminados: `InvestmentAmount=200` unidades de **moneda cotizada**, `OpenDate=DateTime.MinValue`, `CloseDate=DateTime.MaxValue`, `StrategyMode=Combined`, `BollingerPeriod=20`, `BollingerMultiplier=2`, `CandleType=5 minutos`. En **velas terminadas**, la banda inferior es SMA20 menos dos desviaciones típicas. Compra Bollinger si antes `Close >= banda inferior previa` y ahora `Close < banda inferior actual`. Compra Monthly **una vez por mes** en la primera vela elegible tras formar el indicador. Modos `Bollinger` (solo cruce), `Monthly` (una vez/mes) y `Combined` (cualquiera, una orden si coinciden). Dentro de `OpenDate <= fecha < CloseDate`, el volumen es `floor((InvestmentAmount / Close) / VolumeStep) × VolumeStep`, respetando mínimos y máximos. Solo acumula **largos**, nunca cortos. Desde CloseDate liquida la **posición restante completa** en la primera vela elegible y deja de comprar. Sin stop, short ni cruce EMA; el deslizamiento puede alterar el importe ejecutado. El README anterior no definía valores numéricos predeterminados; estos se han fijado en esta implementación.



## Parámetros
- `InvestmentAmount` - monto invertido cada vez
- `OpenDate` - fecha de inicio de compras
- `CloseDate` - fecha para cerrar todas las posiciones
- `StrategyMode` - reversión a la media BB, DCA mensual o combinado
- `BollingerPeriod` - período de las Bollinger Bands
- `BollingerMultiplier` - multiplicador de desviación estándar
- `CandleType` - marco temporal para el cálculo de Bollinger

## Indicadores
- Bollinger Bands
