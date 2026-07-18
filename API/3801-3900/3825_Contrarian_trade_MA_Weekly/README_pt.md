# Estratégia Semanal Contrarian Trade MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um sistema contrário semanal convertido do consultor especialista MQL "Contrarian_trade_MA" original. A estratégia analisa os extremos semanais das velas juntamente com uma média móvel simples para atenuar os movimentos esticados no início de uma nova semana.

## Lógica de negociação

- **Fonte de dados**: velas semanais fornecidas pelo parâmetro `CandleType` (o padrão é um período de 7 dias).
- **Extremos históricos**: os indicadores `Highest` e `Lowest` rastreiam a máxima e a mínima das `CalcPeriod` semanas anteriores concluídas, excluindo a vela atualmente avaliada.
- **Filtro de média móvel**: uma média móvel simples de comprimento `MaPeriod` aplicada a fechamentos semanais atua como um filtro direcional.
- **Regras de inscrição**:
  - **Compre** quando o fechamento da semana anterior for superior à máxima monitorada (`highest < previousClose`) ou quando a média móvel estiver acima da abertura semanal atual.
  - **Venda** quando o fechamento da semana anterior for inferior ao mínimo monitorado (`lowest > previousClose`) ou quando a média móvel estiver abaixo da abertura semanal atual.
  - Apenas uma posição pode ser aberta por vez; sinais opostos são ignorados até que a negociação existente seja fechada.
- **Regras de saída**:
  - A posição é fechada após ser mantida por sete dias (604.800 segundos), independentemente da direção.
  - Uma parada protetora é avaliada em cada vela semanal concluída. A distância de parada é calculada a partir de `StopLossPoints * PriceStep` (volta para `1` se os metadados do instrumento não especificarem uma etapa).

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CalcPeriod` | `4` | Número de semanas completas usadas para calcular o máximo mais alto e o mínimo mais baixo. |
| `MaPeriod` | `7` | Período da média móvel simples aplicada aos fechamentos semanais. |
| `StopLossPoints` | `300` | Distância do preço de entrada ao stop loss, medida em etapas de preço. Defina como `0` para desativar a parada. |
| `Volume` | `0.5` | Tamanho do pedido em lotes enviado por `BuyMarket`/`SellMarket`. |
| `CandleType` | `7 days` | Prazo para as velas que conduzem todos os cálculos. |

## Notas adicionais

- A estratégia recupera automaticamente a etapa de preço de `Security.PriceStep`. Forneça esse valor nos metadados do instrumento para um posicionamento preciso do stop loss.
- `StartProtection()` está habilitado para rastrear mudanças de posição inesperadas realizadas fora da estratégia.
- Como a lógica opera em velas semanais concluídas, os preenchimentos são simulados no fechamento semanal da barra de sinal durante a execução no modo de teste.
