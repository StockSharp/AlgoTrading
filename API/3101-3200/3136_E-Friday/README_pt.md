# Estratégia E-Friday
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Converte o assessor especialista MetaTrader original `E-Friday.mq5` para a API de alto nível do StockSharp.
- Opera apenas quando o período do gráfico é **H1 ou inferior**; caso contrário, a estratégia registra um aviso e permanece flat.
- Entra em posições de forma contrária: uma vela de baixa abre uma posição longa e uma vela de alta abre uma posição curta.
- Desabilita completamente o trading todas as sextas-feiras para corresponder ao comportamento original de proteção de fim de semana.
- Restringe o trading a uma janela de tempo configurável e pode forçar o fechamento de posições após o fim da sessão.

## Lógica de negociação
1. A cada vela finalizada, a estratégia verifica o horário atual da bolsa:
   - se o dia for sexta-feira, ignora qualquer ação;
   - se a hora for anterior à hora de início configurada, aguarda;
   - se a janela de fechamento estiver habilitada e a hora ultrapassar a hora de fim, achata todas as posições e ignora novas entradas.
2. Quando o trading é permitido, a última vela completada impulsiona o sinal:
   - se `Open > Close` (corpo de baixa) a estratégia prepara uma entrada longa;
   - se `Open < Close` (corpo de alta) a estratégia prepara uma entrada curta;
   - preços de abertura e fechamento iguais cancelam qualquer ação pendente.
3. Antes de entrar em uma nova posição, a exposição atual é achatada, portanto nunca há mais de uma posição líquida.

## Gestão de posição
- **Tamanho do lote** – retirado de `TradeVolume` e enviado para ordens `BuyMarket` / `SellMarket`.
- **Stop loss e take profit** – medidos em pips. Os pips são calculados a partir de `Security.PriceStep` e multiplicados por `10` quando o instrumento tem 3 ou 5 casas decimais, exatamente como na versão MQL.
- **Trailing stop** – ativa-se quando o preço se move `TrailingStopPips + TrailingStepPips` a favor da posição. O stop é ajustado para `preço atual - trailing stop` (longo) ou `preço atual + trailing stop` (curto).
- As saídas são avaliadas usando os extremos da vela:
  - uma posição longa fecha se a mínima da vela tocar o stop ou a máxima alcançar o take profit;
  - uma posição curta fecha se a máxima da vela tocar o stop ou a mínima alcançar o take profit.
- Após a hora de fim de sessão (quando `UseCloseHour = true`) toda posição aberta é fechada via ordens de mercado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período das velas processadas. Deve definir um `TimeSpan` positivo e não deve exceder uma hora. |
| `TradeVolume` | Volume de ordem em lotes. Deve ser positivo. |
| `StopLossPips` | Distância do preço de entrada ao stop protetor, expressa em pips. Defina como zero para desabilitar o stop inicial. |
| `TakeProfitPips` | Distância do preço de entrada à meta de lucro em pips. Defina como zero para desabilitar a meta. |
| `TrailingStopPips` | Distância do trailing stop em pips. Funciona em conjunto com `TrailingStepPips`. |
| `TrailingStepPips` | Progresso adicional mínimo (em pips) necessário antes de o trailing stop ser ajustado. Deve ser positivo quando o trailing stop estiver habilitado. |
| `StartHour` | Hora (horário da bolsa) a partir da qual a estratégia pode começar a abrir posições. |
| `UseCloseHour` | Habilita ou desabilita o fechamento forçado após a hora de fim. |
| `EndHour` | Hora (horário da bolsa) após a qual a estratégia para de negociar e fecha posições existentes. |

## Notas de implementação
- Usa `SubscribeCandles` e a API de alto nível `Bind` para que indicadores possam ser adicionados posteriormente se necessário.
- Valida a configuração de trailing na inicialização: quando um trailing stop é solicitado, o passo de trailing deve ser estritamente positivo.
- A conversão de pips replica a lógica original do EA (`PriceStep * 10` para símbolos de 3/5 dígitos) para manter distâncias de stop-loss consistentes.
- A versão StockSharp avalia stops e metas uma vez por vela finalizada. O EA original rodava em cada tick; portanto, o port StockSharp pode sair alguns ticks mais tarde, mas a lógica permanece equivalente.
- A estratégia chama explicitamente `CloseActivePosition` quando a janela de sessão termina. O script MQL continha a mesma ideia, mas retornava antes de alcançar a rotina de fechamento; a versão C# implementa o comportamento pretendido.
- Logs informativos (`AddInfoLog` / `AddWarningLog`) são usados para expor períodos de trading ignorados na interface do usuário.
