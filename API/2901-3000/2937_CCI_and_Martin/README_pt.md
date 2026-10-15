# Estratégia CCI and Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia CCI and Martin busca reversões bruscas após uma curta sequência baixista ou altista e confirma o movimento com o Índice de Canal de Commodities (CCI). A lógica replica o consultor especialista original do MetaTrader 5 enquanto usa a API de alto nível do StockSharp. A estratégia trabalha apenas com velas terminadas e pode operar em qualquer instrumento para o qual valores de CCI e passos de preço estejam disponíveis.

## Regras de Trading
- **Configuração altista**
  - As velas `-2` e `-1` devem ser ambas baixistas (abertura maior que fechamento).
  - A vela `0` deve fechar acima de sua abertura e acima da abertura da vela `-1`.
  - O CCI na vela `-1` deve estar abaixo de `+5`, abaixo do valor da vela `-2`, e tanto `-2` quanto `-3` devem mostrar uma sequência decrescente. O CCI atual (vela `0`) deve girar para cima acima do valor anterior.
  - Quando todas as condições se cumprem e nenhuma posição está aberta, a estratégia entra em um trade comprado.
- **Configuração baixista**
  - As velas `-2` e `-1` devem ser ambas altistas (abertura menor que fechamento).
  - A vela `0` deve fechar abaixo de sua abertura e abaixo da abertura da vela `-1`.
  - O CCI na vela `-1` deve estar acima de `-5`, acima do valor da vela `-2`, e tanto `-2` quanto `-3` devem formar uma sequência crescente. O CCI atual (vela `0`) deve girar para baixo abaixo do valor anterior.
  - Quando todas as condições se cumprem e nenhuma posição está aberta, a estratégia entra em um trade vendido.

O algoritmo monitora apenas velas completadas. A implementação MQL original esperava 40 segundos após a abertura do minuto para evitar sinais prematuros; o uso de velas terminadas torna este filtro desnecessário.

## Gestão de Risco
- As distâncias de **stop-loss** e **take-profit** são definidas em pips. São convertidas em offsets de preço multiplicando o passo de preço do instrumento por dez quando o passo corresponde a uma cotação de 3 ou 5 dígitos, refletindo o cálculo original de pips.
- O **trailing stop** se torna ativo depois que o preço avança pela distância do trailing stop mais o passo de trailing. O stop é então movido para manter a distância de trailing e apenas avança quando a melhoria de preço supera o passo configurado.
- Se o stop-loss ou o take-profit for definido como zero, a saída respectiva é desabilitada. O trailing requer que tanto a distância do stop quanto o passo sejam positivos.

## Gestão de Volume
Dois motores opcionais de dimensionamento de posição podem alterar o tamanho do lote após cada trade.
- **Escalonamento Martingale** multiplica o volume atual pelo coeficiente martingale assim que o número de perdas consecutivas atinge o gatilho. O escalonamento para após o número configurado de passos martingale. Qualquer trade lucrativo redefine o volume ao valor inicial.
- **Ajustes por passos** incrementam o volume em uma quantidade fixa, seja após perdas ou após lucros, dependendo do modo selecionado. O incremento é normalizado ao passo de volume do instrumento e limitado pelo parâmetro de volume máximo. Quando o limite é excedido ou um trade não cumpre a condição de gatilho, o volume retorna ao tamanho inicial.

O consultor especialista original proíbe habilitar a lógica martingale e de passos simultaneamente; o port em C# aplica a mesma restrição.

## Parâmetros
- `CandleType` – série de velas usada para análise.
- `CciPeriod` – comprimento de média para o Índice de Canal de Commodities.
- `InitialVolume` – tamanho base da ordem antes de qualquer escalonamento.
- `StopLossPips` – distância do stop-loss expressa em pips.
- `TakeProfitPips` – distância do take-profit expressa em pips.
- `TrailingStopPips` – distância do trailing stop em pips (0 desabilita o trailing).
- `TrailingStepPips` – melhoria mínima de preço necessária antes que o trailing stop se mova.
- `EnableMartingale` – ativa o escalonamento estilo martingale após perdas.
- `MartingaleCoefficient` – multiplicador aplicado ao volume atual para trades martingale.
- `MartingaleTriggerLosses` – número de trades perdedores consecutivos necessários antes do escalonamento.
- `MartingaleMaxSteps` – número máximo de multiplicações martingale.
- `EnableStepAdjustments` – habilita incrementos de volume baseados em passos.
- `StepVolumeIncrement` – incremento absoluto aplicado quando a regra de passos é ativada.
- `StepVolumeMax` – limite superior para o volume baseado em passos.
- `StepAdjustmentMode` – seleciona se o incremento por passos é ativado após uma perda ou após um lucro.

## Notas
- A estratégia assume que as ordens de mercado são executadas próximas ao preço solicitado. A lógica protetora recalcula os stops em cada vela terminada para emular o trailing baseado em ticks do EA original.
- Se o passo de preço do instrumento não corresponde à cotação FX clássica, a conversão de pips ainda funciona, mas as distâncias baseadas em pips podem representar valores monetários diferentes.
