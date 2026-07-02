# EMA Estratégia de cobertura entre concursos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o consultor especialista MetaTrader **EMA_CROSS_CONTEST_HEDGED** dentro de StockSharp. O robô procura um cruzamento de alta/baixa entre uma média móvel exponencial rápida e uma lenta (EMA) e, opcionalmente, verifica o histograma MACD como uma confirmação de tendência. Quando aparece um sinal, a estratégia abre imediatamente uma posição de mercado e coloca uma escada de ordens stop que protegem a negociação, adicionando mais exposição se o preço continuar a tendência.

## Lógica de negociação
- Calcule um EMA curto e um EMA longo na série de velas configurada. Os sinais podem ser obtidos da barra anterior concluída (padrão) ou da barra atual assim que a vela fechar.
- Detecte um **cruzamento de alta** quando o curto EMA subir acima do longo EMA e um **cruzamento de baixa** quando cair abaixo do longo EMA.
- Opcionalmente, exija que a linha MACD esteja acima de zero para negociações longas e abaixo de zero para negociações curtas, replicando o filtro MQL.
- Quando a condição de alta for satisfeita, compre no mercado, anexe metas de stop loss e take-profit e coloque na fila quatro ordens pendentes de buy-stop espaçadas pela distância de hedge.
- Quando a condição de baixa for satisfeita, venda no mercado, anexe metas de risco e coloque na fila quatro ordens pendentes de venda e parada abaixo do preço.
- As ordens pendentes são canceladas após o seu tempo de expiração se não forem acionadas.
- Os trailing stops se estreitam à medida que os lucros abertos crescem, e os cruzamentos opostos podem forçar saídas antecipadas quando `Use Close` está ativado.

## Parâmetros
- **Tipo de vela** – intervalo de tempo usado para todos os cálculos.
- **Volume de Ordens** – volume de negociação da posição inicial e de cada ordem de hedge.
- **Take Profit (pips)** – distância de take-profit em pips.
- **Stop Loss (pips)** – distância stop-loss em pips.
- **Trailing Stop (pips)** – distância do trailing stop (0 desativa o trailing).
- **Nível de Hedge (pips)** – espaçamento entre as ordens pendentes de hedge.
- **Use Fechar** – fecha posições existentes quando ocorre um cruzamento oposto.
- **Use MACD** – requer confirmação de MACD para entradas comerciais.
- **Vencimento(s)** – vida útil para ordens de hedge pendentes.
- **Curto EMA** – comprimento do EMA rápida.
- **Long EMA** – comprimento do EMA lenta (deve ser maior que o EMA rápida).
- **Barra de Sinal** – escolha se deseja avaliar os sinais na barra atual (0) ou na barra anterior (1).

## Notas
- Todos os comentários no código são fornecidos em inglês, conforme solicitado.
- A estrutura de hedge pendente segue o comportamento do consultor especialista MQL original, colocando quatro ordens em etapas de distância igual.
- As conversões de preços de pips levam em consideração os `PriceStep` e `Decimals` do símbolo para corresponder aos cálculos de pontos de MetaTrader.
