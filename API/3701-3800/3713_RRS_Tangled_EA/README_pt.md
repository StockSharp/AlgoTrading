# Estratégia RRS emaranhada EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia RRS Tangled EA** é uma versão StockSharp do MetaTrader 4 consultor especialista "RRS Tangled EA". O sistema original escolhe aleatoriamente a direção e o símbolo da negociação, ao mesmo tempo que limita o número de ordens simultâneas e protege o lucro flutuante através de trailing stops e limites de risco rigorosos. A versão convertida concentra-se no instrumento atualmente selecionado, reproduzindo a entrada aleatória, o rastreamento e o comportamento de gerenciamento de risco usando o StockSharp API de alto nível.

## Lógica principal
1. Assine a série de velas configuradas e aguarde as velas concluídas.
2. Em cada barra:
   - Atualize os níveis de trailing stop para cestas longas e curtas existentes.
   - Verifique as distâncias de stop-loss e take-profit usando os máximos e mínimos das velas.
   - Avalie o lucro flutuante de todas as entradas abertas; feche tudo se ultrapassar o limite de dinheiro em risco.
   - Se a negociação for permitida, o spread for aceitável e o número de entradas estiver abaixo do limite, sorteie um número inteiro aleatório em `[0, 3]`.
   - Abra um novo comprado quando o valor aleatório for `1`, ou um novo vendido quando o valor for `2`, usando um volume aleatório entre os limites configurados.
3. Os trailing stops seguem o melhor bid/ask quando o preço se move pela distância de ativação, bloqueando os lucros se o preço retroceder pelo trailing gap.
4. A gestão de risco pode funcionar na modalidade de dinheiro fixo ou como uma porcentagem do saldo da conta corrente. Quando a perda flutuante excede o valor configurado, todas as posições são achatadas imediatamente.

## Parâmetros
| Nome | Descrição |
|------|-------------|
| `MinVolume` | Limite inferior para o volume de negociação gerado aleatoriamente. |
| `MaxVolume` | Limite superior para o volume de negociação aleatório. |
| `TakeProfitPips` | Distância alvo em pips, aplicada ao preço médio de entrada da cesta. |
| `StopLossPips` | Distância de parada protetora em pips, medida a partir do preço médio de entrada. |
| `TrailingStartPips` | Distância de lucro necessária antes que a lógica final seja ativada. |
| `TrailingGapPips` | Gap mantido entre o trailing stop e o melhor preço de compra/venda. |
| `MaxSpreadPips` | Spread máximo permitido antes de abrir uma nova entrada aleatória. |
| `MaxOpenTrades` | Número máximo de entradas simultâneas em ambas as direções. |
| `RiskManagementMode` | Alterna entre o tratamento de risco de dinheiro fixo e percentual de saldo. |
| `RiskAmount` | Quantidade de risco (moeda ou percentual) monitorado em relação ao PnL flutuante. |
| `TradeComment` | Comentário opcional para escrituração contábil, mantido para compatibilidade com a fonte EA. |
| `Notes` | Texto informativo exibido dentro da string de status da estratégia. |
| `CandleType` | Série de velas usadas para tomada de decisões. |

## Diferenças da versão MQL
- As negociações são executadas no instrumento atribuído à estratégia, em vez de selecionar aleatoriamente símbolos da observação de mercado MetaTrader. Isso mantém a implementação compatível com as estratégias de segurança única de StockSharp.
- O gerenciamento de pedidos é realizado em cestas longas/curtas agregadas, refletindo como o EA original agrupava as posições com os mesmos números mágicos.
- O controle de spread depende do melhor lance/venda mais recente da carteira de pedidos, em vez das chamadas `MarketInfo` de MetaTrader.

## Notas de uso
- Certifique-se de que o corretor ou simulador conectado forneça cotações de compra e venda para que os cálculos de spread e trailing permaneçam precisos.
- Defina `MinVolume` e `MaxVolume` dentro da faixa de volume permitida do instrumento. A estratégia ajusta automaticamente o volume aleatório à etapa e aos limites de volume do símbolo.
- A lógica de gerenciamento de risco fecha *todas* as negociações imediatamente quando a perda flutuante excede o limite configurado; nenhuma nova posição será aberta até a próxima vela.
