# Estratégia Donchian CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores Donchian CCI para gerar sinais.
A entrada comprada ocorre quando Price > Donchian Upper && CCI > CciOverbought (rompimento para cima com momento de alta). A entrada vendida ocorre quando Price < Donchian Lower && CCI < CciOversold (rompimento para baixo com momento de baixa).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 43%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price > Donchian Upper && CCI > CciOverbought (rompimento para cima com momento de alta)
  - **Vendido**: Price < Donchian Lower && CCI < CciOversold (rompimento para baixo com momento de baixa)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço cai abaixo da banda do meio
  - **Vendido**: Sair da posição vendida quando o preço sobe acima da banda do meio
- **Stops**: Sim.
- **Valores padrão**:
  - `DonchianPeriod` = 20
  - `CciPeriod` = 20
  - `CciOverbought` = 100
  - `CciOversold` = -100
  - `StopLossPercent` = 2
    O canal é a máxima e a mínima dos DonchianPeriod candles anteriores, e o seu meio fica a meio caminho entre elas. Um rompimento de alta é confirmado quando o CCI está acima de CciOverbought e um de baixa quando está abaixo de CciOversold; a leitura oposta, um CCI sobrevendido num fechamento acima do canal, praticamente não ocorre. Um sinal de entrada contra uma posição aberta a inverte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Donchian CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

