# Estratégia Estrela da Manhã/Véspera CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o MetaTrader 5 Expert Advisor **Expert_AMS_ES_CCI** usando o StockSharp API de alto nível. Ele verifica os padrões de reversão de três velas Morning Star e Evening Star e requer confirmação do Commodity Channel Index (CCI) antes de abrir novas posições. A lógica funciona apenas com velas finalizadas e opera no título primário especificado nas configurações da estratégia.

## Lógica de negociação
- **Entrada longa Morning Star**
  - Detecte três velas consecutivas que formam um padrão Morning Star:
    - Vela 1: corpo de baixa forte (tamanho do corpo maior que o corpo médio na janela selecionada).
    - Vela 2: vela de corpo pequeno com intervalo menor que a Vela 1.
    - Vela 3: fecha acima do ponto médio da Vela 1.
  - Confirme se o valor CCI na barra de sinal é menor que o limite de entrada negativo (padrão −50).
- **Entrada curta do Evening Star**
  - Detecte um padrão válido do Evening Star:
    - Vela 1: corpo forte de alta.
    - Vela 2: vela de corpo pequeno que fica acima da Vela 1.
    - Vela 3: fecha abaixo do ponto médio da Vela 1.
  - Confirme se o valor CCI na barra de sinal é maior que o limite de entrada positivo (padrão +50).
- **Regras de saída de posição**
  - Fechar posições curtas quando CCI cruzar novamente acima de −NeutralThreshold ou cair abaixo de +NeutralThreshold (padrão ±80).
  - Feche posições longas quando CCI cruzar abaixo de +NeutralThreshold ou cair abaixo de −NeutralThreshold.
  - Nenhuma regra adicional de stop-loss ou take-profit está incorporada; os usuários podem adicionar proteções externas, se necessário.

## Indicadores
- **Índice de canal de commodities (CCI)** – filtro de confirmação, período padrão 25.
- **Média Móvel Simples dos corpos das velas** – calcula o tamanho médio do corpo nas últimas velas *BodyAveragePeriod* (padrão 5) para validar a força do padrão.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `CciPeriod` | Número de barras usadas no cálculo CCI. | 25 | Otimizável. |
| `BodyAveragePeriod` | Número de velas usadas para medir o tamanho médio do corpo. | 5 | Otimizável. |
| `EntryThreshold` | Valor absoluto de CCI necessário para novas negociações. | 50 | Valor positivo; a estratégia verifica ±EntryThreshold. |
| `NeutralThreshold` | Nível CCI absoluto que define a zona de saída. | 80 | Valor positivo; a estratégia verifica ±NeutralThreshold. |
| `CandleType` | Tipo de vela (prazo) utilizado para análise. | Período de 1 hora | Altere para corresponder à resolução desejada. |

## Notas
- A estratégia assina atualizações de velas via `SubscribeCandles` e usa `Bind` para receber valores de indicadores.
- As negociações são executadas com ordens de mercado usando `BuyMarket` e `SellMarket`.
- Todos os comentários no código são escritos em inglês, conforme necessário.
- Para ampliar o gerenciamento de risco, combine a estratégia com `StartProtection` ou módulos personalizados de gerenciamento de dinheiro.
