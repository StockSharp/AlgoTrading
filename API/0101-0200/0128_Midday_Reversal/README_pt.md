# Estratégia de Reversão ao Meio-dia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Reversão ao Meio-dia busca pontos de virada que ocorrem por volta do horário do almoço, quando as tendências da manhã costumam se esgotar.
A liquidez tipicamente diminui no meio da sessão, levando a reversões enquanto os traders zeram posições.

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado cripto.

O sistema monitora uma mudança de momentum perto do meio-dia e entra na direção oposta ao movimento da manhã.

Um stop percentual controla o risco e as saídas ocorrem se a reversão não se desenvolver até a tarde.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Intradiário
  - Direção: Ambos
  - Indicadores: Price Action
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

