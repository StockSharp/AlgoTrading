# Estratégia de Reversão em Retração de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Os mercados frequentemente retraem uma parte de um movimento anterior antes de retomar a tendência. Esta estratégia identifica máximas e mínimas recentes de swing e observa o preço testar os níveis de retração de 61.8% ou 78.6%. Essas áreas frequentemente marcam pontos de esgotamento.

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

O algoritmo rastreia swings em uma janela deslizante e calcula os níveis de Fibonacci entre eles. Quando o preço se aproxima de uma retração chave e forma uma vela na direção da tendência original, uma operação é aberta com um stop colocado a um percentual fixo. Os alvos ficam em torno do ponto médio de 50% do swing.

Ao focar em retrações profundas dentro de uma tendência existente, o método busca capturar os estágios iniciais de um movimento de continuação após vendedores ou compradores terem assumido o controle brevemente.

## Detalhes

- **Critérios de entrada**: O preço testa a retração de 61.8% ou 78.6% e imprime uma vela de confirmação.
- **Comprado/Vendido**: Ambos dependendo da tendência.
- **Critérios de saída**: Preço atingindo o nível de 50% ou stop-loss.
- **Stops**: Sim, baseados em percentual.
- **Valores padrão**:
  - `SwingLookbackPeriod` = 20
  - `FibLevelBuffer` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Fibonacci levels
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

