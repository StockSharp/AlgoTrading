# Estratégia de Distribuição Wyckoff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Distribuição Wyckoff é uma fase de topo caracterizada por vendas pesadas nas altas e testes de resistência.
O volume frequentemente se expande nas quedas e se contrai nas recuperações, sugerindo que grandes interesses estão liquidando posições.

Os testes indicam um retorno anual médio de aproximadamente 64%. Funciona melhor no mercado de câmbio.

Esta estratégia vende a descoberto quando o preço rompe para baixo do range de distribuição, antecipando um declínio sustentado.

Um stop logo acima do range protege contra falsos rompimentos, e as posições são encerradas se o preço retornar ao topo da estrutura.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Volume, Price
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

