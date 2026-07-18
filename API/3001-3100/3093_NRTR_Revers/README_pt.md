# Estratégia NRTR Revers
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia NRTR Revers é uma conversão em C# do expert advisor original do MetaTrader 5 `NRTR_Revers.mq5`. O sistema usa a abordagem Nick Rypock Trailing Reverse (NRTR) para alternar entre viés comprado e vendido dependendo de como o preço interage com as bandas de suporte e resistência projetadas pelo ATR. As decisões de trading são avaliadas no fechamento de cada vela concluída proveniente de uma assinatura de único período.

## Lógica de trading

1. **Projeção ATR** – A estratégia calcula um Average True Range (ATR) com o período configurável. O valor ATR é multiplicado pelo `VolatilityMultiplier` para obter o deslocamento da banda.
2. **Bandas dinâmicas** – Para a direção de tendência atual a estratégia encontra:
   - A mínima mais baixa (ou máxima mais alta) entre as velas que se alinham com a configuração original de janela MQL.
   - Um extremo secundário deslocado mais profundamente na história. A distância entre a banda primária e este extremo secundário é usada juntamente com o limiar `ReversePips` para confirmar reversões fortes.
3. **Mudanças de tendência** – Quando o fechamento anterior se move para fora da banda ATR ou a diferença do extremo secundário excede a distância de reversão, o viés muda (de comprado para vendido ou vice-versa). Se existir uma posição oposta ela é fechada primeiro; caso contrário, uma nova posição na nova direção é aberta imediatamente.
4. **Aguardar posição zerada** – Após emitir uma ordem a mercado oposta para fechar uma posição existente, a estratégia aguarda até que o portfólio esteja zerado antes de enviar a nova ordem de entrada. Este comportamento reflete o expert advisor original.
5. **Gestão de risco** – Níveis de stop-loss, take-profit e trailing stop são definidos em pips e convertidos para preços absolutos usando um valor de ponto ajustado (compatível com símbolos forex de 3 e 5 casas decimais). As atualizações de trailing requerem progresso de preço maior que `TrailingStopPips + TrailingStepPips`, correspondendo à lógica do MT5.

## Parâmetros

- `CandleType` – Período principal para assinatura de dados de preço.
- `AtrPeriod` – Comprimento de média do ATR usado no cálculo da banda.
- `VolatilityMultiplier` – Multiplicador aplicado ao valor ATR para dimensionar o deslocamento a partir do extremo.
- `ReversePips` – Distância adicional baseada em pips que o extremo secundário deve exceder antes que o viés mude.
- `StopLossPips` – Distância de stop protetor em pips a partir do preço de entrada (definir como zero para desabilitar).
- `TakeProfitPips` – Distância do alvo de lucro em pips a partir do preço de entrada (definir como zero para desabilitar).
- `TrailingStopPips` – Distância de ativação do trailing stop medida em pips (definir como zero para desabilitar o trailing).
- `TrailingStepPips` – Distância extra em pips necessária antes que ocorram atualizações de trailing; deve ser positivo quando o trailing está ativo.
- `TradeVolume` – Volume de ordem usado para novas entradas (em lotes/contratos dependendo das configurações do ativo).

## Notas

- Os cálculos de indicadores e as verificações de reversão usam apenas velas concluídas; velas incompletas são ignoradas.
- O valor ATR fornecido pelo binding é equivalente ao ATR da barra anterior usado no EA fonte porque os cálculos ocorrem após a conclusão da vela.
- O cálculo do ponto ajustado maneja automaticamente cotações forex de 3 e 5 casas decimais para manter os parâmetros baseados em pips compatíveis com o script original.
- Nenhum port Python é fornecido por solicitação. A pasta atualmente contém apenas a implementação em C# e a documentação.
