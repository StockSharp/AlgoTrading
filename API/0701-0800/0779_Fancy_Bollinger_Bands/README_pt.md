# Bollinger Bands Sofisticadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos de Bollinger Bands. Compra quando o preço de fechamento cruza acima da banda superior e vende quando o preço de fechamento cruza abaixo da banda inferior.

## Detalhes

- **Condições de entrada**:
  - **Comprado**: fechamento cruza acima da banda superior
  - **Vendido**: fechamento cruza abaixo da banda inferior
- **Condições de saída**: cruzamento inverso
- **Tipo**: Rompimento
- **Indicadores**: Bollinger Bands
- **Período**: 1 minuto (padrão)
- **Stops**: nenhum

**Regra verificável e padrões.** As bandas de Bollinger usam os últimos 20 fechamentos concluídos, **incluindo o candle de sinal**, com dois desvios-padrão populacionais. `BollingerPeriod=20`, `DeviationMultiplier=2`, `CandleType=1 minuto`. Sem posição, compra quando o fechamento cruza acima da banda superior e vende a descoberto quando cruza abaixo da banda inferior. Encerra **toda a posição comprada** quando o fechamento retorna para dentro da banda superior e **toda a vendida** quando retorna para dentro da banda inferior. As bandas anterior e atual usam somente os dados fechados dos respectivos candles. Sem stops, alvos ou reversão no mesmo candle.
