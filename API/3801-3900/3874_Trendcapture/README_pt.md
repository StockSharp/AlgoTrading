# Estratégia de captura de tendências 3874
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia Trendcapture** é uma versão StockSharp de alto nível do MetaTrader consultor especialista `MQL/7772/Trendcapture.mq4`. O EA original observa a direção da tendência Parabolic SAR e espera que um ambiente ADX fraco entre em novas posições. Após cada negociação fechada, ele decide se mantém ou inverte a direção da negociação, dependendo do lucro realizado, e uma vez que uma posição aberta ganha alguns pontos, ela puxa o stop para o ponto de equilíbrio.

Esta porta mantém o comportamento intacto enquanto depende dos auxiliares de ordem e ligações de indicadores de StockSharp. Todos os sinais são processados ​​em velas concluídas em um período configurável.

## Lógica de negociação

1. **Configuração do indicador**
   - Parabolic SAR (`ParabolicSar`) com etapa e limite de aceleração configuráveis.
   - Índice direcional médio (`AverageDirectionalIndex`) para o valor de força da tendência principal.
2. **Seleção de entrada**
   - Apenas uma posição pode ser aberta por vez.
   - Uma entrada longa é permitida quando:
     - A direção desejada (derivada da última negociação fechada) aponta para compra.
     - A vela atual fecha acima do valor SAR.
     - A linha principal ADX está abaixo de `20`, indicando o regime de variação exigido pelo código original.
   - Uma entrada curta reflete as regras (a direção desejada aponta para a venda, preço de fechamento abaixo de SAR, ADX abaixo de `20`).
3. **Gerenciamento de saídas**
   - Após cada preenchimento, a estratégia envia ordens de stop-loss e take-profit em distâncias `StopLossPoints` e `TakeProfitPoints` (convertidas por meio da etapa de preço do título).
   - Quando o lucro flutuante atinge `GuardPoints`, o stop ativo é reemitido ao preço de entrada para garantir um piso de equilíbrio.
   - O fechamento de negociações aciona uma atualização de direção: negociações lucrativas mantêm o mesmo viés, negociações perdedoras ou planas o invertem, reproduzindo a verificação `OrderProfit()` do especialista.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Tipo de dados Candle usado para cálculos de indicadores. | Período de 1 hora |
| `SarStep` | Fator de aceleração inicial de Parabolic SAR. | `0.02` |
| `SarMax` | Fator de aceleração máximo para Parabolic SAR. | `0.2` |
| `AdxPeriod` | Período de suavização de ADX. | `14` |
| `TakeProfitPoints` | Distância de lucro expressa em etapas de preço. | `180` |
| `StopLossPoints` | Distância de stop-loss expressa em etapas de preço. | `50` |
| `GuardPoints` | Limite de lucro (em etapas de preço) necessário antes de mover o stop para o ponto de equilíbrio. | `5` |
| `MaximumRisk` | Fator de escala de volume; `0.03` reproduz o tamanho original do lote. | `0.03` |

## Notas de uso

- Certifique-se de que o título selecionado exponha `PriceStep` (ou pelo menos `MinStep`) para que as distâncias dos pontos sejam convertidas em valores de preço corretamente.
- A propriedade base `Volume` representa o tamanho do lote usado quando `MaximumRisk` é igual a `0.03`. Aumentar o fator de risco dimensiona o volume enviado proporcionalmente.
- Como o EA negocia no mercado e coloca imediatamente ordens de proteção, não há entradas pendentes no livro quando a estratégia está ociosa.
- O guarda do ponto de equilíbrio cancela e reemite o stop de proteção ao preço de entrada; isso reflete a chamada `OrderModify` original que moveu o stop loss para o ponto de equilíbrio.

## Arquivos

- `CS/TrendcaptureStrategy.cs` – implementação StockSharp de alto nível do Trendcapture EA.
- `README_zh.md` – Tradução chinesa deste documento.
- `README_ru.md` – Tradução russa deste documento.
