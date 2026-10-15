# Estratégia Chaos Trader Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Chaos Trader Lite replica as técnicas de entrada dos três homens sábios de Bill Williams usando a API de alto nível do StockSharp. Ela analisa cada vela finalizada do período configurado (1 hora por padrão) e coloca ordens stop quando qualquer uma das seguintes condições é atendida:

1. **Primeiro Homem Sábio – Barra divergente**: detecta velas divergentes altistas ou baixistas e requer uma distância mínima entre o preço e a linha dos lábios do Alligator.
2. **Segundo Homem Sábio – Aceleração do Awesome Oscillator**: aguarda cinco leituras consecutivas do Awesome Oscillator que mostrem momentum acelerado.
3. **Terceiro Homem Sábio – Rompimento de fractal**: confirma um fractal duas velas atrás e verifica se o preço está operando suficientemente longe da linha dos dentes do Alligator antes de enfileirar uma ordem de rompimento.

Sempre que um setup comprado aparece, a estratégia cancela os sell stops existentes, fecha posições vendidas, coloca um novo buy stop logo acima do máximo anterior e registra um stop protetor abaixo da vela. O oposto acontece para setups vendidos. Os stops protetores são monitorados em cada barra; se o preço cruzar o nível armazenado, a posição é encerrada a mercado.

## Indicadores e cálculos

- **Lábios do Alligator**: média móvel suavizada de 5 períodos do preço mediano deslocada três velas para frente. A estratégia mantém uma fila para que o valor alinhado com a barra atual corresponda à implementação do MetaTrader.
- **Dentes do Alligator**: média móvel suavizada de 8 períodos do preço mediano deslocada cinco velas para frente. O valor deslocado impulsiona o filtro do terceiro homem sábio.
- **Awesome Oscillator**: o indicador integrado do StockSharp (SMA de 5 vs 34 do preço mediano) fornece a série de momentum usada pelo segundo homem sábio.
- **Fractais**: o código inspeciona a máxima/mínima da vela que está duas barras atrás da última barra. Um fractal válido requer que essa vela seja mais alta (ou mais baixa) do que as duas velas em cada lado.

## Lógica de trading

1. Subscrever o tipo de vela solicitado e processar apenas velas finalizadas.
2. Atualizar os indicadores Alligator e Awesome Oscillator e armazenar valores deslocados.
3. Avaliar as condições dos homens sábios:
   - A barra divergente deve fechar na metade superior (para altistas) ou inferior (para baixistas) da vela e mostrar uma distância dos lábios maior que `MagnitudePips * PriceStep`.
   - A aceleração do AO requer cinco valores: `AO[1] > AO[2] > AO[3] > AO[4]` e `AO[4] < AO[5]` para comprados, espelhado para vendidos.
   - O rompimento de fractal verifica se o preço fecha acima (ou abaixo) do fractal confirmado e acima (ou abaixo) dos dentes do Alligator mais o limiar de magnitude.
4. Quando um setup está ativo, colocar uma ordem `BuyStop` ou `SellStop` com volume `Volume` na máxima da vela mais um passo de preço (ou mínima menos um passo). Cancelar o stop oposto e aplainar posições contrárias.
5. Atualizar os níveis de stop-loss armazenados: stops comprados seguem para cima, stops vendidos para baixo. Se uma vela perfurar o stop armazenado, a estratégia sai da posição aberta a mercado.

## Parâmetros

- `MagnitudePips` *(padrão 10)* – distância mínima em pips entre a barra divergente e os lábios do Alligator.
- `UseFirstWiseMan` *(padrão true)* – habilitar ou desabilitar a entrada por barra divergente.
- `UseSecondWiseMan` *(padrão true)* – habilitar ou desabilitar a entrada por aceleração do Awesome Oscillator.
- `UseThirdWiseMan` *(padrão true)* – habilitar ou desabilitar a entrada por rompimento de fractal.
- `Volume` *(padrão 0.01)* – tamanho de ordem para entradas stop.
- `CandleType` *(padrão 1 hora)* – tipo de dados processado pela estratégia.

## Notas

- As verificações de bid/ask do código MQL4 original são aproximadas com o preço de fechamento da vela no StockSharp.
- As rotinas de validação de margem e volume do MetaTrader são omitidas porque o StockSharp trata a validação de ordens internamente.
- As ordens stop são canceladas quando o setup oposto aparece para evitar ordens pendentes conflitantes, correspondendo ao comportamento `CloseAll` do EA.
