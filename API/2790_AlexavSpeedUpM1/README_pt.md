# Estratégia Alexav SpeedUp M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do assessor especialista "Alexav SpeedUp M1" do MetaTrader 5 para a API de alto nível do StockSharp.
- Projetado para mercados rápidos no período de 1 minuto (padrão) e reage a corpos de vela incomumente grandes.
- Abre uma única posição líquida na direção do corpo da vela forte e a gerencia com stop-loss fixo, take-profit e um trailing stop escalonado.
- Usa entradas baseadas em pips que são automaticamente convertidas em distâncias de preço de acordo com o tamanho do tick do instrumento e a precisão decimal.

## Ideia original vs. implementação no StockSharp
- O EA original abria operações longas e curtas simultaneamente em contas de hedge. As estratégias do StockSharp operam em um ambiente de netagem, por isso este port mantém apenas uma posição de cada vez e entra na direção da vela grande.
- A lógica do trailing stop segue a versão MT5: espera o preço se mover `TrailingStop + TrailingStep` antes de mover o stop mais perto pela distância de trailing, e só atualiza quando o preço avança pelo menos um trailing step além do stop anterior.
- As distâncias em pips são convertidas em unidades de preço multiplicando pelo tamanho mínimo do tick. Para símbolos Forex com 3 ou 5 casas decimais, o código multiplica o tick por 10 para emular o tratamento de pips do MT5.

## Regras de entrada
1. Trabalhar com velas terminadas do período selecionado (padrão: 1 minuto).
2. Medir o corpo da vela: `abs(Close - Open)`.
3. Se o corpo exceder `MinimumBodySizePips * pipSize` e não houver posição ativa, entrar na direção do corpo da vela:
   - Vela de alta → abrir posição comprada.
   - Vela de baixa → abrir posição vendida.

## Regras de saída
- **Stop-loss** – colocado a `StopLossPips * pipSize` do preço de entrada. Desabilitado quando o parâmetro é zero.
- **Take-profit** – colocado a `TakeProfitPips * pipSize` da entrada. Desabilitado quando o parâmetro é zero.
- **Trailing stop** – habilitado quando `TrailingStopPips > 0` e `TrailingStepPips > 0`.
  - Ativa após a operação ganhar pelo menos `TrailingStopPips + TrailingStepPips` pips.
  - Para operações compradas, o stop é movido para `Close - TrailingStopPips * pipSize` quando a condição é atendida e o preço avançou pelo menos um trailing step além do stop anterior.
  - Para operações vendidas, o stop é movido para `Close + TrailingStopPips * pipSize` usando a mesma condição de passo.

## Parâmetros
- `OrderVolume` – tamanho da operação em lotes (padrão `0.1`).
- `StopLossPips` – distância do stop-loss em pips (padrão `30`).
- `TakeProfitPips` – distância do take-profit em pips (padrão `90`).
- `TrailingStopPips` – distância do trailing stop em pips (padrão `10`).
- `TrailingStepPips` – movimento favorável mínimo antes de o trailing stop ser atualizado (padrão `5`). Deve ser maior que zero quando o trailing stop estiver habilitado.
- `MinimumBodySizePips` – tamanho mínimo do corpo (em pips) necessário para acionar uma operação (padrão `100`).
- `CandleType` – período para velas (padrão `1 Minute`).

## Visualização
- A estratégia desenha automaticamente a série de velas selecionada e as próprias operações na área do gráfico quando disponível, simplificando a inspeção de sinais durante os testes.

## Notas de uso
- A configuração padrão espelha as configurações do MT5. Ajuste as distâncias em pips para se adequar à volatilidade do instrumento negociado.
- Como apenas uma posição líquida é suportada, evite executar a estratégia em contas de hedge que esperam posições longas e curtas simultâneas.
- Para mercados com tamanhos de tick maiores, reduza as entradas baseadas em pips proporcionalmente para manter distâncias de preço comparáveis.
