# Estratégia de Bloqueio (Lock)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Bloqueio recria o assessor especialista clássico de "lock" do MetaTrader: sempre mantém um par hedgeado de posições compradas e vendidas e continua reciclando-os até que uma condição de bloqueio de lucro seja satisfeita. O algoritmo é projetado para instrumentos com tamanhos de tick pequenos onde um take-profit fixo baseado em pips pode ser aplicado.

## Fluxo de trabalho de trading

1. **Hedge inicial** – assim que os dados de mercado estiverem disponíveis, a estratégia abre uma posição comprada e vendida com o mesmo volume. Se ambas as ordens forem preenchidas, o volume usado para o próximo hedge é multiplicado pelo fator `LotExponential`.
2. **Gestão do take-profit** – cada perna armazena seu preço de entrada. Quando o fechamento da vela se move por `TakeProfitPips` (convertido para ticks do instrumento) desde a entrada, a perna é fechada com uma ordem de mercado. O lado oposto permanece aberto, preservando o comportamento tipo hedge da versão MQL.
3. **Re-hedge** – sempre que o número total de pernas ativas for um ou zero, a estratégia imediatamente abre um novo par. Se não houver pernas abertas, o volume base é redefinido para `LotSize` antes de criar o novo par.
4. **Controle de volume** – o método auxiliar `AdjustVolume` aplica as restrições da bolsa: arredonda os volumes para o `VolumeStep` do ativo, limita-os por `MinVolume` e `MaxVolume`, e cancela o escalonamento se o valor ajustado se tornar zero.

## Condição de bloqueio de lucro

A lógica MQL original monitora o saldo da conta versus o capital: quando o saldo excede o capital por `ExcessBalanceOverEquity` e o capital está pelo menos `MinProfit` acima do último saldo bloqueado, cada perna é fechada. A implementação em C# espelha esse comportamento rastreando o capital observado quando a estratégia está plana e tratando-o como o saldo em execução. Assim que a condição é acionada, todas as pernas são liquidadas e o saldo de referência é atualizado antes do ciclo reiniciar com `LotSize`.

## Parâmetros

- `LotSize` – volume base para o primeiro ciclo de hedge (padrão: `0.1m`).
- `TakeProfitPips` – distância em pips para fechar cada perna (padrão: `100`). Um valor de `0` desativa a saída automática.
- `LotExponential` – multiplicador aplicado ao volume atual após ambas as pernas abrirem com sucesso (padrão: `2m`).
- `ExcessBalanceOverEquity` – gap tolerado entre saldo e capital antes de garantir lucros (padrão: `3000m`).
- `MinProfit` – crescimento adicional do capital que deve ser alcançado antes de fechar todas as pernas (padrão: `500m`).
- `CandleType` – timeframe que impulsiona a lógica da estratégia (padrão: timeframe de 1 minuto).

## Notas de implementação

- O tamanho do pip é recalculado a partir de `Security.PriceStep` e `Security.Decimals`, portanto a estratégia se adapta a símbolos FX de 3/5 dígitos bem como a futuros ou ações padrão.
- As ordens são colocadas usando execução de mercado, refletindo o comportamento do especialista MQL que envia ordens de mercado com take-profits do lado do broker.
- A estratégia mantém um histórico completo de pernas hedgeadas, o que permite múltiplas posições empilhadas em cada lado, exatamente como o script fonte permitia.
