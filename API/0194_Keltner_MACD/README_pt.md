# Keltner Macd Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos Canais Keltner e MACD. Entra comprado quando o preço rompe acima do canal Keltner superior com MACD > Signal. Entra vendido quando o preço rompe abaixo do canal Keltner inferior com MACD < Signal. Sai quando o MACD cruza sua linha de sinal na direção oposta.

Os testes indicam um retorno anual médio de aproximadamente 169%. Funciona melhor no mercado de criptomoedas.

Os rompimentos do Canal Keltner servem como gatilho e o momentum do MACD filtra a direção. A estratégia inicia operações quando ambos os sinais se alinham.

Bom para traders que perseguem expansões de volatilidade com suporte de momentum. Um stop baseado em ATR contém o risco.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > UpperBand && MACD > Signal`
  - Vendido: `Close < LowerBand && MACD < Signal`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento do MACD na direção oposta
- **Stops**: Baseado em ATR usando `AtrMultiplier`
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Keltner Channel, MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

