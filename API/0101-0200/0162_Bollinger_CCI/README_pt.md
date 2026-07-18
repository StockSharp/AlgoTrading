# Estratégia Bollinger Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia - Bollinger Bands + CCI. Compra quando o preço está abaixo da banda inferior de Bollinger e o CCI está abaixo de -100 (sobrevendido). Vende quando o preço está acima da banda superior de Bollinger e o CCI está acima de 100 (sobrecomprado).

Os testes indicam um retorno anual médio de aproximadamente 73%. Funciona melhor no mercado de criptomoedas.

As bandas de Bollinger mapeiam os limites de volatilidade, e o CCI mede a distância da média. Rompimentos além de uma banda com confirmação do CCI acionam operações.

Adequado para mercados voláteis onde as tendências se expandem rapidamente. Stops baseados em ATR são aplicados para segurança.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && CCI < CciOversold`
  - Vendido: `Close > UpperBand && CCI > CciOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O preço retorna à banda do meio
- **Stops**: Baseados em ATR usando `StopLoss`
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

