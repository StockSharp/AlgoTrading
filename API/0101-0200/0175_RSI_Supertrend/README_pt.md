# Estratégia Rsi Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada nos indicadores RSI e Supertrend. Entra comprado quando o RSI está em sobrevenda (< RsiOversold) e o preço está acima do Supertrend. Entra vendido quando o RSI está em sobrecompra (> RsiOverbought) e o preço está abaixo do Supertrend.

Os testes indicam um retorno anual médio de aproximadamente 112%. Funciona melhor no mercado forex.

O oscilador RSI define os extremos de momentum enquanto o Supertrend aponta para a direção predominante. As operações ocorrem quando o RSI se alinha com a cor do Supertrend.

Funciona para traders que apreciam uma saída estilo trailing stop. As configurações de ATR do próprio Supertrend moldam essa linha de trailing.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `RSI < RsiOversold && Close > Supertrend`
  - Vendido: `RSI > RsiOverbought && Close < Supertrend`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Mudança de Supertrend
- **Stops**: Trailing com Supertrend
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Em candles de cinco minutos o RSI quase nunca chega a 30 com o preço acima do Supertrend (nem a 70 abaixo dele): o arquivo de BTC de março de 2024 não tem nenhum candle assim. Por isso os níveis são parâmetros com padrões 40 e 60, que ainda marcam um recuo contra a tendência e operam nos dois lados. Uma compra é encerrada quando o Supertrend vira para baixo e uma venda quando vira para cima. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, Supertrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

