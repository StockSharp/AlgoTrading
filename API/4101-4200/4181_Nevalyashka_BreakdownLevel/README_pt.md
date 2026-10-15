# Estratégia de nível de detalhamento de Nevalyashka
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Nevalyashka Breakdown Level é uma conversão direta do consultor especialista MT4 *Nevalyashka_BreakdownLevel*. O sistema constrói um intervalo de abertura entre dois tempos configuráveis ​​e negocia rompimentos desse intervalo. Quando um rompimento falha e a negociação é interrompida, a estratégia imediatamente inverte a direção usando um multiplicador martingale para recuperar a perda. As negociações lucrativas bloqueiam quaisquer entradas adicionais durante o resto do dia de negociação, correspondendo ao comportamento original EA.

## Conceitos-chave
- **Intervalo de abertura:** Maior máximo e menor mínimo impressos entre `RangeStart` e `RangeEnd` definem o canal de breakout para o dia atual.
- **Entradas de breakout:** Uma posição longa é aberta quando o preço de fechamento excede a faixa máxima; uma posição curta é aberta quando cai abaixo do intervalo mínimo.
- **Ordens de proteção:** O stop loss é sempre colocado no lado oposto do intervalo. O take-profit é posicionado a uma distância igual à largura do intervalo.
- **Movimento de equilíbrio:** Quando ativado, o stop é movido para o preço de entrada assim que a negociação avança até a metade do caminho em direção ao alvo.
- **Martingale recuperação:** Após um stop-loss, a estratégia inverte a direção, multiplica o volume do pedido por `MartingaleMultiplier` e usa um tamanho alvo/stop simétrico para recuperar a perda anterior.
- **Bloqueio diário:** Qualquer fechamento lucrativo (take-profit ou saída manual acima de zero) impede novas negociações até que o dia de negociação mude.
- **Flatização forçada:** Quando `OrdersCloseTime` for posterior a `RangeEnd`, todas as posições abertas serão fechadas nesse horário e novas entradas serão bloqueadas pelo restante do dia.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `RangeStart` | Hora de início (inclusive) do intervalo de referência. | `04:00` |
| `RangeEnd` | Hora final (inclusive) do intervalo de referência. | `09:00` |
| `OrdersCloseTime` | Hora do dia para fechar posições à força. Quando esse horário for posterior a `RangeEnd`, ele também bloqueará novas negociações posteriormente. | `23:30` |
| `OrderVolume` | Volume usado para cada negociação de breakout. | `0.1` |
| `MartingaleMultiplier` | Multiplicador aplicado à próxima ordem após um stop loss para recuperar a perda anterior. | `2` |
| `UseBreakeven` | Permite mover o stop para o ponto de equilíbrio quando a negociação tiver percorrido metade da distância alvo. | `true` |
| `CandleType` | Tipo de vela usado para construir o alcance e gerar sinais. | `1 hour` velas |

## Regras de negociação
1. **Cálculo de intervalo**: Para cada novo dia de negociação, a estratégia registra os máximos e mínimos das velas finalizadas entre `RangeStart` e `RangeEnd` (inclusive).
2. **Condições de entrada**:
   - Opere comprado quando o preço de fechamento da vela atual estiver acima da máxima registrada.
   - Opere vendido quando o preço de fechamento da vela atual estiver abaixo do mínimo registrado.
   - As entradas serão ignoradas se uma reversão do martingale estiver pendente, uma negociação lucrativa já tiver ocorrido no mesmo dia ou se a hora atual tiver passado de `OrdersCloseTime` (quando `OrdersCloseTime > RangeEnd`).
3. **Gerenciamento de riscos**:
   - O stop loss está ancorado no lado oposto da faixa de abertura.
   - O take-profit é definido pelo preço de entrada mais/menos a largura do intervalo de abertura.
   - Quando `UseBreakeven` está ativado, o stop se move para o preço de entrada após metade da distância alvo ter sido percorrida.
4. **Martingale reversão**:
   - Se o stop loss for atingido, a posição é fechada, o volume é multiplicado por `MartingaleMultiplier` e uma ordem de mercado imediata na direção oposta é enviada.
   - O novo stop e o alvo são colocados a uma distância igual à perda por lote dividida pelo multiplicador, correspondendo à lógica de recuperação do EA original.
5. **Bloqueio comercial diário**:
   - Se uma negociação fechar com lucro não negativo ou a meta for atingida, nenhuma nova negociação será permitida até que a data de negociação seja alterada.
6. **Saída forçada**:
   - Quando `OrdersCloseTime` estiver após a janela do intervalo e o horário atual atingir esse valor, todas as posições abertas serão achatadas e o dia será bloqueado.

## Notas
- A estratégia usa o StockSharp API (`Strategy.SubscribeCandles().Bind(...)`) de alto nível para ficar próximo das convenções da estrutura.
- Todos os cálculos com estado (limites de intervalo, ordens pendentes de martingale, estado de equilíbrio) são armazenados dentro da classe de estratégia para evitar pesquisas históricas.
- A conversão preserva o comportamento original do EA de contar os dias de negociação por data do calendário e gerenciar as etapas do martingale imediatamente após uma parada.
