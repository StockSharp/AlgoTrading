# CCI VWAP Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A abordagem CCI VWAP tenta capturar reversões intradiárias quando o momentum e o preço se afastam do preço médio ponderado por volume. Ao observar o Índice de Canal de Commodities ao lado do nível VWAP, o sistema mede a força dos movimentos recentes em relação a um benchmark de valor justo.

Os testes indicam um retorno anual médio de aproximadamente 70%. Funciona melhor no mercado de ações.

Um setup de compra surge quando o CCI cai abaixo de -100 e o mercado negocia abaixo do VWAP, sinalizando que a pressão vendedora pode estar esgotada. Um short ocorre quando o CCI sobe acima de +100 com o preço acima do VWAP, destacando um rally esticado vulnerável a uma correção. As posições são fechadas assim que o preço recupera o VWAP na direção oposta.

Esta estratégia é projetada para day traders que gostam de operar nos extremos mas ainda dependem de níveis objetivos para saídas. O stop-loss definido ajuda a gerenciar o risco se o momentum não reverter rapidamente à média.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: CCI < -100 && Price < VWAP (oversold below VWAP)
  - **Vendido**: CCI > 100 && Price > VWAP (overbought above VWAP)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair do comprado quando o preço subir acima do VWAP
  - **Vendido**: Sair do vendido quando o preço cair abaixo do VWAP
- **Stops**: Sim.
- **Valores padrão**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: CCI VWAP
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

