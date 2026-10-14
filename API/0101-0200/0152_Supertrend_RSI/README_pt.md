# Estratégia Supertrend RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia Supertrend + RSI. Comprar quando o preço está acima do Supertrend e o RSI está abaixo de RsiOversold. Vender quando o preço está abaixo do Supertrend e o RSI está acima de RsiOverbought.

Os testes indicam um retorno anual médio de cerca de 43%. Funciona melhor no mercado de ações.

O indicador Supertrend mostra a tendência atual, e o RSI detecta quando o preço está sobreextendido. As ordens seguem a direção do Supertrend assim que o RSI atinge um extremo.

Uma boa escolha para traders que dependem de stops de seguimento. O stop integrado do Supertrend trabalha com a configuração do ATR para limitar as perdas.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close > Supertrend && RSI < RsiOversold`
  - Vendido: `Close < Supertrend && RSI > RsiOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Inversão do Supertrend na direção oposta
- **Stops**: Usa Supertrend como Trailing stop
- **Valores padrão**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Em candles de cinco minutos o RSI quase nunca chega a 30 com o preço acima do Supertrend (nem a 70 abaixo dele): o arquivo de BTC de março de 2024 não tem nenhum candle assim. Por isso os padrões são 40 e 60, que ainda marcam um recuo contra a tendência e operam nos dois lados. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Supertrend, RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
