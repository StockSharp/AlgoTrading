# Estratégia de Reversão Upthrust
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Reversão Upthrust é a companheira baixista do spring e ocorre quando o preço rompe brevemente acima da resistência mas cai rapidamente de volta.
O movimento elimina os compradores tardios antes de reverter para baixo.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

Esta estratégia vende a descoberto assim que o preço cai de volta abaixo do nível de rompimento, esperando que a oferta supere a demanda.

Um stop logo acima da máxima do upthrust gerencia o risco e as posições são encerradas se o preço se recuperar acima desse nível.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Wyckoff
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

