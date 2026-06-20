# Gap Fill Reversal Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Reversão por Preenchimento de Gap aproveita gaps noturnos que recuam rapidamente durante a próxima sessão. Quando o preço forma um gap afastando-se do fechamento anterior, mas imediatamente retorna para preencher esse vazio, isso frequentemente sinaliza exaustão do movimento inicial.

Os testes indicam um retorno anual médio de aproximadamente 181%. Funciona melhor no mercado de criptomoedas.

A estratégia entra assim que o gap está completamente fechado e procura uma reversão na direção oposta à abertura. O objetivo é capturar o recuo que ocorre quando traders presos encerram suas posições.

Um stop baseado em percentual define o risco, e as posições fecham quando o momentum diminui ou o stop é atingido.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minutos
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Gap
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
