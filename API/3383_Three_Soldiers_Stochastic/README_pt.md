# Estratégia de Três Soldados Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o especialista MetaTrader `Expert_ABC_WS_Stoch.mq5`, que combina padrões clássicos de reversão de três velas com confirmação do oscilador Stochastic. Um sinal longo requer a formação de alta dos "Três Soldados Brancos" junto com uma linha de sinal de sobrevenda Stochastic, enquanto um sinal curto depende dos "Três Corvos Negros" de baixa confirmados por um Stochastic sobrecomprado. A lógica de saída monitora cruzamentos da linha de sinal através de bandas configuráveis ​​para fechar posições.

## Lógica de negociação

1. **Detecção de padrões**
   - Acompanhe as últimas três velas concluídas.
   - Identifique os *Três Soldados Brancos* quando todas as três velas estiverem otimistas e cada fechamento for maior que o anterior.
   - Identifique os *Três Corvos Negros* quando todas as três velas estiverem em baixa e cada fechamento for menor que o anterior.
2. **Confirmação do oscilador**
   - Calcule um oscilador Stochastic com períodos `%K`, `%D` e `Slowing` idênticos ao especialista original (47, 9, 13 por padrão).
   - Use a linha de sinal (`%D`) como confirmação:
     - Insira longo se o valor da linha de sinal anterior estiver abaixo do limite de sobrevenda (padrão `30`).
     - Insira short se o valor da linha de sinal anterior estiver acima do limite de sobrecompra (padrão `70`).
3. **Condições de saída**
   - Feche uma negociação longa quando a linha de sinal ultrapassar os limites de saída inferior ou superior (padrão `20` e `80`).
   - Feche uma negociação a descoberto quando a linha de sinal voltar abaixo desses limites.
   - Ambas as verificações de saída dependem dos valores da linha de sinal anterior e anterior para detectar cruzamentos genuínos.

## Parâmetros

| Nome | Padrão | Descrição |
|------|---------|-------------|
| `CandleType` | `1h` período de tempo | Prazo para assinatura da vela. |
| `StochKPeriod` | `47` | Período de lookback para `%K`. |
| `StochDPeriod` | `9` | Comprimento médio móvel da linha de sinal. |
| `StochSlowing` | `13` | Suavização adicional aplicada a `%K`. |
| `OversoldLevel` | `30` | Nível de linha de sinal necessário para confirmar uma entrada longa. |
| `OverboughtLevel` | `70` | Nível de linha de sinal necessário para confirmar uma entrada curta. |
| `ExitLowerLevel` | `20` | Limite inferior usado para cruzamentos de saída longos. |
| `ExitUpperLevel` | `80` | Limite superior usado para cruzamentos de saída curtos. |

Todos os parâmetros numéricos suportam intervalos de otimização correspondentes ao modelo MetaTrader, para que o comportamento possa ser ajustado por meio do Strategy Designer.

## Gerenciamento de ordens

- A estratégia inverte posições quando um sinal oposto aparece adicionando o tamanho absoluto da posição atual ao `Volume` configurado.
- `StartProtection()` está habilitado para integração com os controles de risco da plataforma, embora nenhum nível explícito de stop-loss ou take-profit seja aplicado por padrão.

## Visualização

Quando executada dentro do Strategy Designer, a estratégia desenha:

- Preço das velas para o símbolo e período selecionados.
- O oscilador Stochastic configurado.
- Marcadores comerciais para destacar entradas e saídas.

## Notas de uso

- Confirme se o instrumento fornece histórico suficiente para o oscilador Stochastic aquecer antes de esperar sinais.
- Considere combinar a estratégia com filtros de risco adicionais (volatilidade, filtros de sessão, etc.) ao implantar ao vivo.
- Os limites são expostos como parâmetros, permitindo a experimentação rápida com diferentes bandas de confirmação sem edição de código.
