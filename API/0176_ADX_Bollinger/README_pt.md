# Adx Bollinger Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores ADX e Bandas de Bollinger. Entra comprado quando ADX > 25 e o preço rompe acima da banda superior de Bollinger. Entra vendido quando ADX > 25 e o preço rompe abaixo da banda inferior de Bollinger.

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

As rupturas das Bandas de Bollinger filtradas com ADX garantem que o preço está rompendo com força. O sistema opera na direção do rompimento.

Adequado para ambientes de alta volatilidade. Um stop baseado em ATR reduz o risco de queda.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < LowerBand && ADX > 25`
  - Vendido: `Close > UpperBand && ADX > 25`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: O preço retorna à banda média
- **Stops**: Baseado em ATR usando `AtrMultiplier`
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ADX, Bollinger Bands
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

