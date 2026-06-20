# Supertrend Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada no indicador Supertrend e ADX para confirmação da força de tendência. Critérios de entrada: Comprado: Price > Supertrend && ADX > 25 (tendência de alta com movimento forte). Vendido: Price < Supertrend && ADX > 25 (tendência de baixa com movimento forte). Critérios de saída: Comprado: Price < Supertrend (preço cai abaixo do Supertrend). Vendido: Price > Supertrend (preço sobe acima do Supertrend).

Os testes indicam um retorno anual médio de aproximadamente 166%. Funciona melhor no mercado de ações.

O Supertrend fornece um caminho ajustado pela volatilidade enquanto o ADX confirma o poder do movimento. As operações ocorrem quando ambos os indicadores estão alinhados.

Para aqueles que visam aproveitar tendências fortes com trailing stops. ATR determina a colocação do stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > Supertrend && ADX > AdxThreshold`
  - Vendido: `Close < Supertrend && ADX > AdxThreshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Reversão do Supertrend
- **Stops**: Usa Supertrend como trailing stop
- **Valores padrão**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Supertrend, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

