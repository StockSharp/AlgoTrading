# Estratégia TDS Global
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o assessor especialista original do MetaTrader "TDSGlobal" baseado no conceito Triple Screen de Alexander Elder. Avalia velas diárias e combina a inclinação do MACD (12, 23, 9) com um filtro Williams %R de 24 períodos. O sistema busca comprar quando a tendência está virando para cima enquanto o %R mostra condições de sobrevenda, e vender quando a tendência vira para baixo e %R sinaliza sobrecompra.

Sempre que um setup válido é detectado, a estratégia coloca ordens stop além do máximo ou mínimo da sessão anterior. As entradas são afastadas do mercado atual por um buffer configurável para evitar entrar muito próximo do preço, espelhando a lógica de offset original de "16 pontos". Uma vez em uma posição, a estratégia gerencia um stop de proteção, take profit opcional e um trailing stop em passos de preço.

## Lógica de Trading

- **Dados**: Trabalha com velas diárias por padrão (configurável).
- **Filtro de tendência**: Compara os dois valores mais recentes da linha principal do MACD. MACD ascendente implica viés comprado, MACD descendente implica viés vendido.
- **Filtro oscilador**: Usa o valor anterior de Williams %R. Abaixo de `WilliamsBuyLevel` (padrão -75) permite setups comprados, acima de `WilliamsSellLevel` (padrão -25) permite setups vendidos.
- **Entrada**:
  - Comprado: colocar um buy-stop acima do máximo anterior mais um passo de preço. A entrada é elevada a pelo menos `EntryBufferSteps` passos de preço acima do último fechamento para manter uma distância mínima do mercado.
  - Vendido: colocar um sell-stop abaixo do mínimo anterior menos um passo de preço. A ordem é reduzida no máximo ao último fechamento menos os passos de `EntryBufferSteps`.
- **Gestão de risco**:
  - O stop inicial está ancorado ao extremo oposto da vela anterior (máximo para vendidos, mínimo para comprados).
  - A distância do take profit equivale a `TakeProfitSteps` passos de preço. O valor padrão (999) mantém o comportamento próximo à versão MQL que usava um alvo muito amplo.
  - O trailing stop é habilitado quando `TrailingStopSteps` > 0. Segue o fechamento por essa quantidade de passos e só ajusta na direção da negociação.
- **Tratamento de ordens**:
  - Ordens stop existentes são canceladas e atualizadas quando o preço de entrada ou os níveis de proteção precisam ser atualizados.
  - Sinais de tendência opostos removem ordens pendentes que não se alinham mais com a direção do MACD.
  - Quando uma posição é aberta, os níveis pendentes armazenados são reutilizados para inicializar os preços de stop/take profit ao vivo.
- **Escalonamento opcional**: O EA original escalonava a colocação de ordens em pares de forex para evitar ordens pendentes simultâneas. Definir `UseSymbolStagger` como `true` impõe as mesmas janelas de minutos para EURUSD, GBPUSD, USDCHF e USDJPY.

## Parâmetros

- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – Períodos MACD usados para a verificação da inclinação da tendência.
- `WilliamsLength` – Lookback para Williams %R.
- `WilliamsBuyLevel`, `WilliamsSellLevel` – Limiares de sobrevenda/sobrecompra (valores negativos, mais próximos de -100/-0 respectivamente).
- `EntryBufferSteps` – Offset mínimo do mercado atual ao colocar entradas stop (número de passos de preço).
- `TakeProfitSteps` – Distância alvo em passos de preço (definir um número pequeno para ativar um alvo rígido).
- `TrailingStopSteps` – Distância do trailing stop em passos; definir como zero para desabilitar o trailing.
- `UseSymbolStagger` – Habilita as janelas de minutos específicas por símbolo.
- `CandleType` – Período para velas (diário por padrão).

## Notas

- Usar o volume de estratégia para controlar o tamanho do lote; padrão é 1 se nenhum volume for especificado.
- As ordens pendentes e saídas trailing operam em velas completadas, portanto os preenchimentos entre fechamentos de velas são aproximados pelo preço de entrada armazenado.
- O valor padrão do take profit é grande para corresponder ao comportamento do EA original; ajustá-lo quando um alvo finito for necessário.
