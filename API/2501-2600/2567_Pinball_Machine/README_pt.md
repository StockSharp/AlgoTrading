# Estratégia Pinball Machine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Pinball Machine** é uma conversão lúdica do consultor especialista MetaTrader 5 "Pinball machine (barabashkakvn's edition)". Em vez de analisar a estrutura do mercado, a estratégia emula uma máquina de loteria: cada vela terminada aciona várias extrações aleatórias que podem resultar em uma operação se dois números coincidirem. O port do StockSharp mantém o espírito do especialista original enquanto adapta o gerenciamento de dinheiro e a execução para a API de alto nível.

## Lógica de negociação
1. **Gatilho** – a estratégia trabalha no período definido por `Candle Type`. Quando uma vela é concluída, o processo aleatório é executado uma vez.
2. **Extrações aleatórias** – quatro inteiros no intervalo 0–100 são gerados. Uma configuração comprada aparece se o primeiro par coincidir e uma configuração vendida aparece se o segundo par coincidir. Como as extrações são independentes, é possível (embora raro) gerar ambos os sinais na mesma vela.
3. **Elegibilidade de ordem** – a estratégia só coloca uma nova ordem quando nenhuma posição está atualmente aberta. Isso mantém a exposição líquida unilateral, diferente do comportamento de hedge do original MQL.
4. **Distâncias de stop/alvo** – para cada ordem, dois números aleatórios adicionais no intervalo definido por `Min Offset Points` e `Max Offset Points` são produzidos. Eles determinam a distância (em passos de preço) para os níveis de stop loss e take profit em torno do preço de entrada.
5. **Dimensionamento de posição** – o capital em risco é limitado pelo parâmetro `Risk Percent`. A estratégia estima o valor do portfólio (preferindo `CurrentValue`, depois `CurrentBalance`, depois `BeginValue`) e divide o risco permitido pela distância ao stop. Quando o cálculo não é possível ou resultaria em tamanho zero, o fallback é o `Volume` da estratégia (padrão de 1 lote).
6. **Execução de ordem** – as ordens de mercado são emitidas via `BuyMarket` / `SellMarket`. O preço de fechamento da vela é usado como proxy para a cotação de entrada porque os dados de Bid/Ask em nível de tick não estão disponíveis no fluxo de trabalho orientado por velas.
7. **Gestão de operações** – os níveis de stop loss e take profit são verificados em cada vela terminada. Se o preço penetrar um nível, a posição é fechada por uma ordem de mercado, refletindo o comportamento das ordens protetoras na versão MetaTrader.

## Parâmetros
- **Risk Percent** – porcentagem do valor do portfólio que pode ser perdida se o stop loss for acionado. Valores acima de zero habilitam o dimensionamento de posição baseado em risco.
- **Min Offset Points / Max Offset Points** – limites inclusivos (expressos em passos de preço) para selecionar aleatoriamente as distâncias de stop e alvo. Ambos os parâmetros devem permanecer positivos; a implementação os troca automaticamente se o mínimo exceder o máximo.
- **Candle Type** – a série de dados que impulsiona o motor aleatório. Qualquer `DataType` compatível com `SubscribeCandles` pode ser usado (velas de um minuto por padrão).

## Diferenças com a versão MetaTrader
- **Fonte de eventos** – o especialista MT5 trabalha em cada tick. A estratégia StockSharp avalia a loteria aleatória em velas terminadas para seguir a abordagem de API de alto nível recomendada.
- **Hedge** – MetaTrader pode acumular múltiplas posições em ambos os lados. O port se limita a uma única posição líquida (comprada, vendida ou zerada) porque as estratégias StockSharp são tipicamente liquidadas.
- **Gerenciamento de dinheiro** – o original dependia de `CMoneyFixedMargin`. A versão C# reproduz a ideia usando métricas de portfólio e dimensionamento de risco percentual.
- **Colocação de ordem** – loops de slippage explícito e retry são desnecessários no StockSharp e foram removidos. As ordens de mercado são enviadas uma vez que o ambiente reporta prontidão (`IsFormedAndOnlineAndAllowTrading`).

## Notas de uso
- Garantir que o instrumento selecionado exponha um `PriceStep` válido. Se nenhum estiver disponível, a estratégia recorre a um passo de 1 para manter a simulação em execução.
- Como o sistema é intencionalmente aleatório, o desempenho variará bastante entre backtests. Usar a estratégia principalmente para experimentar com infraestrutura, gerenciamento de risco ou aleatoriedade estilo Monte Carlo.
- Ajustar o período da vela para controlar a frequência com que as operações podem aparecer. Velas mais curtas aumentam o número de loterias por sessão.
- A estratégia desenha tanto velas quanto operações executadas em uma área de gráfico quando o charting está disponível, o que ajuda a diagnosticar com que frequência as condições aleatórias são atendidas.

## Notas de conversão
- Arquivo original: `MQL/17744/Pinball machine.mq5`.
- Mantidos todos os controles de entrada (porcentagem de risco, intervalos de stop e alvo) em forma de parâmetro adequados para otimização dentro do StockSharp.
- A semente aleatória usa o padrão da plataforma (`Random()`), que é equivalente à chamada `MathSrand(GetTickCount())` do especialista MetaTrader.
