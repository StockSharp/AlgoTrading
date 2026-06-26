# Estratégia Autotrader Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Autotrader Momentum** é uma conversão do expert advisor do MetaTrader 5 *Autotrader Momentum (edição de barabashkakvn)*. O algoritmo avalia o momentum recente comparando o preço de fechamento da barra de monitoramento com o preço de fechamento de uma barra de referência histórica. Quando um desvio de momentum de alta é detectado, a estratégia compra; quando aparece um desvio de baixa, vende. Todas as ordens são executadas ao preço de mercado usando a API de trading de alto nível do StockSharp.

A implementação mantém o foco original no controle de risco baseado em pontos. As distâncias de stop-loss, take-profit e trailing-stop são definidas em pips e automaticamente traduzidas em deslocamentos de preço com base no `PriceStep` do instrumento. O suporte para cotações de três e cinco casas decimais é preservado aplicando o mesmo ajuste de 10x usado no código MQL. A lógica de trailing é avaliada em cada vela finalizada antes que novas entradas sejam consideradas, garantindo que a gestão de risco espelhe o comportamento do EA de priorizar saídas protetoras.

## Lógica de trading
1. Assinar o `CandleType` configurado e processar apenas velas finalizadas, correspondendo à lógica de "nova barra" do EA original.
2. Manter uma janela deslizante de preços de fechamento de tamanho `max(CurrentBarIndex, ComparableBarIndex) + 1`.
3. Comparar o fechamento da barra monitorada (`CurrentBarIndex`, padrão 0) com o fechamento da barra histórica (`ComparableBarIndex`, padrão 15).
4. Se o fechamento monitorado for maior que o fechamento de referência, fechar qualquer exposição vendida e comprar o volume de trading configurado.
5. Se o fechamento monitorado for menor que o fechamento de referência, fechar qualquer exposição comprada e vender o volume de trading configurado.
6. Cada entrada recalcula o preço médio de entrada e atualiza os níveis de stop-loss, take-profit e trailing-stop.

Como as estratégias do StockSharp trabalham com uma posição líquida, as reversões combinam o volume necessário para fechar a exposição oposta com o volume base configurado. Isso corresponde ao comportamento MQL que primeiro fechava o lado oposto e depois abria uma nova ordem do tamanho solicitado.

## Parâmetros
- `CandleType` – Período usado para comparação de preços. Padrão: 1 hora.
- `TradeVolume` – Volume base da ordem de mercado. Aplicado em cada sinal além de qualquer volume necessário para reverter uma posição existente.
- `StopLossPips` – Distância de stop protetor em pips. Definir como 0 para desabilitar o stop-loss fixo.
- `TakeProfitPips` – Distância do alvo de lucro em pips. Definir como 0 para desabilitar o take-profit fixo.
- `TrailingStopPips` – Distância mantida pelo trailing stop. Definir como 0 para desabilitar o trailing.
- `TrailingStepPips` – Movimento favorável mínimo necessário antes de avançar o trailing stop. Deve ser positivo quando o trailing estiver habilitado.
- `CurrentBarIndex` – Índice da vela de monitoramento (0 = barra finalizada mais recente).
- `ComparableBarIndex` – Índice da barra histórica usada para comparação de momentum.

Todas as configurações baseadas em pips são convertidas em deslocamentos de preço usando o `PriceStep` do instrumento. Se o step representa três ou cinco dígitos decimais, o deslocamento é multiplicado por 10 para reproduzir a definição de pip do MetaTrader.

## Gestão de risco
- **Stops e alvos fixos:** Quando `StopLossPips` ou `TakeProfitPips` são maiores que zero, a estratégia mantém os níveis de preço correspondentes relativos ao preço de entrada médio.
- **Trailing Stop:** Habilitado quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos. A lógica de trailing move o stop protetor apenas após o preço ter se movido pelo menos `TrailingStopPips + TrailingStepPips` do preço de entrada médio, replicando o requisito do EA que garantia que o movimento fosse grande o suficiente antes de ajustar o stop.
- **Reinicialização de estado:** Sempre que a posição retorna a zero—seja por saídas impulsionadas pela estratégia ou intervenção externa—o estado de risco armazenado em cache é limpo para evitar níveis obsoletos de stop ou take-profit.

## Notas de implementação
- A estratégia se baseia exclusivamente na API de mercado de alto nível do StockSharp (`BuyMarket`, `SellMarket`) e evita coleções de indicadores para permanecer fiel às diretrizes de conversão.
- Os preços de fechamento são armazenados em uma lista deslizante simples para que `CurrentBarIndex` e `ComparableBarIndex` possam ser alterados em tempo de execução sem necessidade de reinício.
- Como o StockSharp opera sobre uma posição líquida, os níveis de stop-loss e take-profit são rastreados para a exposição agregada. Quando ordens adicionais são adicionadas na mesma direção, o código recalcula um preço de entrada médio ponderado por volume antes de atualizar os níveis de risco.
- Os ajustes de trailing-stop e saídas protetoras são processados antes dos novos sinais em cada vela, evitando que novas entradas sejam avaliadas quando uma saída já foi emitida para aquela barra.

## Referência da estratégia original
- **Fonte:** `MQL/22409/Autotrader Momentum.mq5`
- **Autor:** barabashkakvn (comunidade MetaTrader)
