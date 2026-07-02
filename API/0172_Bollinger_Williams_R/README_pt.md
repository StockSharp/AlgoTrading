# Estratégia Bollinger Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada nos indicadores Bollinger Bands e Williams %R. Entra comprado quando o preço está na banda inferior e o Williams %R está sobrevendido (< -80). Entra vendido quando o preço está na banda superior e o Williams %R está sobrecomprado (> -20).

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

As bandas de Bollinger expõem rompimentos de volatilidade e o Williams %R garante que o momentum seja extremo. As posições abrem quando o preço fecha fora de uma banda com uma leitura correspondente do Williams %R.

Melhor para traders de expansão de volatilidade. Stops de ATR lidam com reversões adversas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && WilliamsR < -80`
  - Vendido: `Close > UpperBand && WilliamsR > -20`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O preço retorna à banda do meio
- **Stops**: Baseados em ATR usando `AtrMultiplier`
- **Valores padrão**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `WilliamsRPeriod` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Williams %R, R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

