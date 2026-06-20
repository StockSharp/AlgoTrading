# RSI Donchian Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia RSI Donchian busca extremos de momentum que coincidam com rompimentos do Canal Donchian. O índice de força relativa mede condições de sobrecompra e sobrevenda enquanto o canal define os máximos e mínimos recentes de preço.

Os testes indicam um retorno anual médio de aproximadamente 82%. Funciona melhor no mercado de ações.

Um sinal de compra aparece quando o RSI cai abaixo de 30 e o preço rompe acima da banda superior do Donchian. Um sinal de venda se forma quando o RSI sobe acima de 70 e o preço cai pela banda inferior. As saídas ocorrem assim que o preço retorna à linha média do Donchian, sinalizando um retorno ao equilíbrio.

Este método funciona bem para traders ativos que preferem operar contra movimentos de exaustão, mas ainda negociam com níveis claros de rompimento. O stop-loss ajuda a limitar o risco se o momentum não reverter rapidamente.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI < 30 && Close > Donchian High
  - **Vendido**: RSI > 70 && Close < Donchian Low
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando close < Donchian Middle
  - **Vendido**: Sair quando close > Donchian Middle
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `DonchianPeriod` = 20
  - `StopLossPercent` = 2m
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

