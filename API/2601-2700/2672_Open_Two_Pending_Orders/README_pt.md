# Estratégia Abrir Duas Ordens Pendentes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia replica o assessor especialista MetaTrader que simultaneamente coloca uma ordem buy stop e uma ordem sell stop ao redor do spread atual. Funciona em um único instrumento e usa chamadas de API de alto nível do StockSharp para subscrever o livro de ordens, gerenciar ordens pendentes e lidar com controles de risco de portfólio. Assim que uma ordem pendente é preenchida, a ordem oposta é cancelada e a posição ativa é gerenciada com regras de stop-loss, take-profit e trailing stop.

## Lógica de Trading
1. Subscrever o livro de ordens e ler os melhores preços bid e ask.
2. Quando não há posição aberta ou ordem de entrada ativa, calcular o volume de entrada e colocar duas ordens stop:
   - Buy stop em *ask + EntryOffsetPoints × PriceStep*.
   - Sell stop em *bid − EntryOffsetPoints × PriceStep*.
3. Quando uma ordem stop é executada:
   - Cancelar a ordem pendente oposta.
   - Armazenar o preço de execução como o novo preço de entrada.
   - Calcular os níveis iniciais de stop-loss e take-profit em passos de preço relativos ao preenchimento.
4. Enquanto a posição está ativa, monitorar o livro de ordens:
   - Fechar posições compradas quando o bid atinge o nível de stop-loss ou take-profit.
   - Fechar posições vendidas quando o ask atinge o nível de stop-loss ou take-profit.
   - Ativar o trailing stop após o preço se mover a favor da operação pela distância de trailing e deslizar o nível de stop de acordo.
5. Quando a posição retorna a plana, redefinir o estado interno e colocar um novo par de ordens stop.

As saídas são executadas com ordens de mercado assim que um nível protetor é tocado. Isso mantém a lógica próxima à implementação MQL sem depender de APIs de modificação de ordens de nível inferior.

## Gestão de Capital
A estratégia pode usar um volume fixo ou dimensionamento dinâmico baseado em risco:
- **Volume Fixo** – usar o tamanho de lote constante definido pelo parâmetro `FixedVolume`.
- **Gestão de Capital** – se habilitado, calcular o volume a partir do capital do portfólio, a porcentagem de risco e a distância do stop-loss em passos de preço. Os volumes são arredondados para o passo de volume do instrumento e limitados entre os valores mínimo e máximo do instrumento.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `UseMoneyManagement` | Habilita o dimensionamento de posição baseado em risco. Padrão: `true`. |
| `RiskPercent` | Porcentagem do capital do portfólio a ser arriscada por operação quando a gestão de capital está ativa. Padrão: `2`. |
| `FixedVolume` | Tamanho de lote usado quando a gestão de capital está desabilitada. Padrão: `1`. |
| `StopLossPoints` | Distância de stop-loss em passos de preço a partir do preço de entrada. Padrão: `100`. |
| `TakeProfitPoints` | Distância de take-profit em passos de preço a partir do preço de entrada. Padrão: `300`. |
| `TrailingStopPoints` | Distância do trailing stop em passos de preço. Um valor de `0` desabilita o trailing. Padrão: `50`. |
| `EntryOffsetPoints` | Distância em passos de preço usada para colocar as ordens pendentes afastadas do spread. Padrão: `50`. |
| `SlippagePoints` | Margem extra em passos de preço reservada para deslizamento. Atualmente informativo e não usado diretamente. Padrão: `5`. |

## Notas
- A estratégia depende do feed do livro de ordens. Certifique-se de que os dados de profundidade de mercado estão disponíveis para o instrumento selecionado.
- A execução de stop-loss e take-profit usa ordens de mercado assim que o bid/ask cruza o nível, correspondendo ao comportamento da lógica de trailing stop do MQL original.
- Os trailing stops começam apenas após o preço ter se movido pela distância de trailing configurada a partir da entrada.
- O código usa indentação com tabulação, comentários em inglês e métodos de alto nível do StockSharp de acordo com as diretrizes do projeto.
