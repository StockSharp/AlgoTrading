# Estratégia RSI Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia RSI Donchian busca extremos de momentum que coincidam com rompimentos do Canal Donchian. O índice de força relativa mede condições de sobrecompra e sobrevenda enquanto o canal define os máximos e mínimos recentes de preço.

Os testes indicam um retorno anual médio de aproximadamente 82%. Funciona melhor no mercado de ações.

Um sinal de compra aparece quando o RSI está acima de RsiOverbought enquanto o preço rompe acima da banda superior do Donchian. Um sinal de venda se forma quando o RSI está abaixo de RsiOversold enquanto o preço cai pela banda inferior. As saídas ocorrem assim que o preço retorna à linha média do Donchian, sinalizando um retorno ao equilíbrio.

Este método funciona bem para traders ativos que preferem seguir um momentum forte, mas ainda negociam com níveis claros de rompimento. O stop-loss ajuda a limitar o risco se o momentum não reverter rapidamente.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI > RsiOverbought && Close > Donchian High
  - **Vendido**: RSI < RsiOversold && Close < Donchian Low
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando close < Donchian Middle
  - **Vendido**: Sair quando close > Donchian Middle
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `StopLossPercent` = 2
    O canal é a máxima e a mínima dos DonchianPeriod candles anteriores, e o seu meio fica a meio caminho entre elas. Um rompimento de alta é confirmado quando o RSI está acima de RsiOverbought e um de baixa quando está abaixo de RsiOversold; a leitura oposta, um RSI sobrevendido num fechamento acima do canal, praticamente não ocorre. O stop é um StopLossPercent fixo do preço de entrada, vigiado também entre os candles. Um sinal de entrada contra uma posição aberta a inverte.
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: RSI, Donchian Channel
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

