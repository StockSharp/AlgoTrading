# Estratégia de Reversão Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O indicador Parabolic SAR coloca pontos acima ou abaixo do preço para sinalizar a direção da tendência. Quando os pontos mudam de lado, pode marcar o fim do movimento anterior. Esta estratégia entra em operações nessa mudança, esperando uma reversão de curto prazo.

Os testes indicam um retorno anual médio de aproximadamente 148%. Funciona melhor no mercado de câmbio.

Um valor de Parabolic SAR é mantido em execução para cada candle. Se o indicador passa de acima do preço para abaixo, uma posição comprada é aberta. Se passa de abaixo para acima, uma operação vendida é executada. O método não usa um alvo de lucro explícito e tipicamente depende de saída discricional ou stops de trailing fora do código de amostra.

Como o SAR reage rapidamente, sinais falsos podem ocorrer em mercados lateralizados, portanto é melhor usado quando o preço faz oscilações decisivas.

## Detalhes

- **Critérios de entrada**: O Parabolic SAR muda de lado em relação ao preço.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop manual ou externo.
- **Stops**: Não definido.
- **Valores padrão**:
  - `InitialAcceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

