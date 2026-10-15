# Morning/Evening Star com estratégia de confirmação de IMF
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica a lógica do especialista MetaTrader `Expert_AMS_ES_MFI`, combinando padrões de reversão de múltiplas velas com confirmação de impulso do Money Flow Index (MFI). Ele monitora as formações de três velas Morning Star e Evening Star no período de tempo selecionado e filtra os sinais usando limites de MFI para confirmar o esgotamento da oscilação atual antes de entrar nas negociações. As reversões de momentum detectadas pelos cruzamentos das IMFs também são usadas para fechar posições abertas.

## Lógica de negociação
- **Fonte de dados**: Candles finalizadas do timeframe configurado e seus valores MFI associados.
- **Indicadores**:
  - Índice de fluxo de dinheiro (MFI) – o período é configurável (padrão 49).
- **Regras de inscrição**:
  - **Longo**: detecta um padrão Morning Star (vela forte de baixa, vela intermediária de corpo pequeno, vela forte de alta fechando acima do ponto médio da primeira) e exige que o MFI da vela anterior esteja abaixo do limite de confirmação de alta (padrão 40).
  - **Venda**: detecta um padrão Evening Star (vela forte de alta, vela intermediária de corpo pequeno, vela forte de baixa fechando abaixo do ponto médio da primeira) e exige que o MFI da vela anterior esteja acima do limite de confirmação de baixa (padrão 60).
  - Ao inverter posições, a estratégia primeiro fecha a exposição oposta antes de abrir a nova negociação.
- **Regras de saída**:
  - **Saída Longa**: Feche a posição quando a IMF cruzar acima do nível de saída superior (padrão 70) ou cair abaixo do nível de saída inferior (padrão 30), sinalizando impulso de sobrecompra ou uma reversão falhada.
  - **Saída Curta**: Fecha a posição quando a IMF cruza acima do nível de saída inferior (padrão 30) ou acima do nível de saída superior (padrão 70), sinalizando crescente impulso de alta.
- **Tipo de Ordem**: Ordens de mercado utilizando o volume da estratégia configurado no ambiente StockSharp.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `CandleType` | Prazo das velas utilizadas para análise. | Velas de 1 hora |
| `MfiPeriod` | Período do indicador IMF. | 49 |
| `BullishMfiThreshold` | Nível MFI que confirma os sinais da Morning Star. | 40 |
| `BearishMfiThreshold` | Nível MFI que confirma os sinais do Evening Star. | 60 |
| `UpperExitLevel` | Nível MFI usado para detecção de saída de sobrecompra. | 70 |
| `LowerExitLevel` | Nível MFI usado para detecção de saída de sobrevenda. | 30 |

Todos os parâmetros podem ser otimizados dentro do StockSharp Designer/Optimizer.

## Notas de uso
1. Anexe a estratégia à segurança desejada e defina o `CandleType` para corresponder ao período do gráfico do especialista MQL original.
2. Configure os parâmetros de risco, como volume da estratégia ou tamanho do pedido específico da corretora, por meio da plataforma StockSharp.
3. Habilite a estratégia. Ele assinará automaticamente velas, calculará valores de IMF e gerenciará posições de acordo com as regras acima.

## Origem
A estratégia é uma conversão direta do consultor especialista MQL5 localizado em `MQL/323`, preservando seu padrão e lógica de decisão baseada em MFI enquanto o adapta ao StockSharp API de alto nível.
