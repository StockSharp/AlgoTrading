# Estratégia de Random Hedg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Random Hedg** é uma portagem de alto nível do StockSharp do expert advisor MetaTrader "Random Hedg". O EA original abre simultaneamente uma ordem de compra a mercado e uma de venda a mercado, gerenciando ambas as posições com uma mistura de stop loss fixo, take profit, ponto de equilíbrio e lógica de trailing. A conversão mantém esse comportamento central enquanto expõe cada configuração como parâmetro de estratégia para que o bot possa ser ajustado ou otimizado diretamente no StockSharp Designer.

## Lógica de negociação
1. **Hedge inicial** – quando a estratégia não tem posição, envia imediatamente duas ordens a mercado (compra e venda) usando o mesmo volume configurável. Ambas as posições recebem um stop loss e um take profit expressos em pips.
2. **Proteção de ponto de equilíbrio** – após o preço mover-se favoravelmente a uma posição pelo número configurado de pips, o nível de stop é deslocado para o ponto de equilíbrio mais um offset opcional (posições compradas) ou menos o offset (posições vendidas). Isso replica o interruptor "mover para sem perda" do EA.
3. **Trailing stop** – quando o lucro supera a distância de trailing, o stop acompanha o preço. Para posições compradas, o stop segue o preço mais alto menos a distância de trailing; para vendidas, segue o preço mais baixo mais a distância.
4. **Saídas protetoras** – cada posição é fechada quando seu take profit ou stop loss é tocado. Opcionalmente, a estratégia pode liquidar ambas as posições se uma vela fechar abaixo da Banda de Bollinger inferior, recriando o filtro de saída do código original.
5. **Reinício de ciclo** – após ambas as posições serem fechadas, a estratégia redefine seus rastreadores internos e aguarda a próxima vela para abrir um novo par coberto.

## Parâmetros
- `HedgeVolume` – volume utilizado para abrir ambas as posições de hedge (padrão 0,1 contratos).
- `StopLossPips` – distância do stop loss protetor (padrão 200 pips).
- `TakeProfitPips` – distância do take profit (padrão 200 pips).
- `TrailingStopPips` – passo de trailing aplicado quando uma posição se torna lucrativa (padrão 40 pips).
- `BreakEvenTriggerPips` – lucro necessário antes de mover o stop para o ponto de equilíbrio (padrão 10 pips).
- `BreakEvenOffsetPips` – lucro adicional garantido quando ocorre o deslocamento para o ponto de equilíbrio (padrão 5 pips).
- `EnableTrailing` – ativa ou desativa o gerenciamento do trailing stop.
- `EnableBreakEven` – ativa ou desativa a função de ponto de equilíbrio.
- `EnableExitStrategy` – ativa o filtro de liquidação baseado nas Bandas de Bollinger.
- `BollingerPeriod` – período das Bandas de Bollinger usadas para a saída opcional (padrão 20 velas).
- `BollingerWidth` – multiplicador de largura das Bandas de Bollinger (padrão 2).
- `CandleType` – série de dados de velas usada para executar a lógica (padrão período de 30 minutos).

## Notas de implementação
- A conversão usa a API de alto nível `Strategy` com assinaturas de velas e o mecanismo `BindEx` para calcular as Bandas de Bollinger dinamicamente.
- O estado interno rastreia o preço de entrada, volume e níveis protetores dinâmicos para cada posição. Isso permite que a versão C# imite os auxiliares de gestão monetária do EA original sem depender de identificadores de ordem específicos da plataforma.
- Os volumes de ordens pendentes são rastreados separadamente para que as execuções possam ser classificadas como entradas ou saídas mesmo quando operações de compra e venda ocorrem consecutivamente.
- A estratégia espera uma conta com capacidade de hedge pois mantém exposição comprada e vendida ao mesmo tempo, assim como o expert advisor original.
- Funções de trailing baseado em dinheiro e take profit por percentual do código MQL são intencionalmente omitidas. Dependem de dados de saldo específicos do corretor e raramente eram usadas na prática; a versão StockSharp foca no gerenciamento central de ação do preço.

## Arquivos
- `CS/RandomHedgStrategy.cs` – implementação principal em C# com comentários inline detalhados em inglês.
- `README.md` – esta documentação (inglês).
- `README_ru.md` – tradução para o russo.
- `README_zh.md` – tradução para o chinês simplificado.
