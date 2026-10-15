# Estratégia de Captura de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Captura de Mercado reproduz a lógica do especialista original do MetaTrader 5. O algoritmo constrói uma grade dinâmica em torno de um preço central em movimento e abre operações no estilo de cobertura sempre que o preço oscila em torno desse centro. As posições são distribuídas acima e abaixo do centro com metas de lucro fixas, enquanto marcos de patrimônio da conta controlam quando liquidar as operações com maiores perdas.

## Regras de trading
- **Linha central** – a estratégia armazena um nível central interno que começa no fechamento do primeiro candle processado. Quando o mercado se move além do espaçamento de grade configurado, o centro é deslocado passo a passo para acompanhar o preço.
- **Curto inicial** – uma posição vendida opcional pode ser aberta imediatamente após o início para corresponder ao comportamento do script MQL.
- **Entradas compradas** – uma operação comprada é permitida quando o último fechamento está acima do centro e o candle anterior operou abaixo dele. Uma verificação de proximidade garante que nenhuma outra operação comprada esteja ativa perto do mesmo nível.
- **Entradas vendidas** – uma operação vendida é permitida quando o último fechamento está abaixo do centro e o candle anterior operou acima dele. O mesmo filtro de proximidade evita o empilhamento de vendas idênticas.
- **Take profit** – cada operação armazena um nível alvo que está a um múltiplo fixo do passo de preço do instrumento a partir do preço de entrada. Máximas dos candles (para comprados) ou mínimas (para vendidos) que atingem o alvo acionam uma saída de mercado.
- **Gerenciamento de patrimônio** – a estratégia monitora o patrimônio do portfólio. Após um ganho percentual configurável, fecha uma série das operações com pior desempenho para garantir lucros. Outro limiar percentual define quando reduzir o risco durante a queda liquidando operações perdedoras. Cada vez que um limiar é acionado, a linha de base de patrimônio é recalculada.

## Parâmetros
- `Enable Long` / `Enable Short` – permitir ou bloquear operações em cada direção.
- `Grid Steps` – espaçamento entre níveis de grade medido em passos de preço.
- `Take Profit Steps` – distância de take profit medida em passos de preço.
- `Open Initial Short` – habilitar a primeira ordem vendida colocada logo após o início.
- `Use Equity Target` – ativar a regra de crescimento de patrimônio para cortar operações perdedoras.
- `Track Drawdown` – ativar a regra de redução para cortar operações perdedoras durante a queda.
- `Equity Gain %` / `Equity Loss %` – percentuais de mudança de patrimônio que acionam as regras acima.
- `Loss Trades Up` / `Loss Trades Down` – número máximo de operações perdedoras fechadas quando cada regra é acionada.
- `Candle Type` – período ou tipo de candle personalizado usado para o processo de decisão.
- `Volume` (propriedade da estratégia) – tamanho da operação para cada ordem de mercado.

## Notas
- A estratégia mantém um registro interno de operações abertas para imitar o estilo de cobertura do especialista original enquanto trabalha com o modelo de posição líquida do StockSharp.
- Os parâmetros de distância são multiplicados pelo passo de preço do ativo; garanta que o instrumento selecionado exponha um valor `PriceStep` válido.
- A lógica opera apenas em candles finalizados. Selecione um tipo de candle que corresponda ao horizonte de trading pretendido, de grades de curtíssimo prazo a grades de swing mais amplas.
